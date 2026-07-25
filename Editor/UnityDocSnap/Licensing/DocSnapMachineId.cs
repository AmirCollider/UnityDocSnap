// ==========================================
// DocSnapMachineId
// The stable, anonymous name this machine is known
// by to the licence server.
//
// A licence key is bound to one machine, so something
// has to identify "this machine" the same way twice.
// Unity already computes exactly that -
// SystemInfo.deviceUniqueIdentifier - and it is
// derived from hardware, so it survives a reinstall of
// the Editor, a new project, and a fresh clone of the
// repository.
//
// What is NOT sent is that value. It is a hardware
// fingerprint, it is the same one every other Unity
// tool on the machine can read, and a licence server
// has no business holding a database of them: the
// server only ever needs to answer "is this the same
// machine as last time?", and a salted SHA-256 answers
// that exactly as well while being useless to anybody
// who steals the table. So the raw identifier stays on
// the machine and only its hash leaves.
//
// Alongside it goes a short human label - "DESKTOP-A1B2
// (Windows)" - because the one screen where this
// matters is the customer's own device list, and
// "which of these two hashes is my laptop" is not a
// question anybody should have to answer.
// ==========================================
using System;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor.Licensing
{
    internal static class DocSnapMachineId
    {
        // Mixed into the hash so the value stored server-side is
        // specific to this product. Without it, the same machine
        // hashes identically for anybody else who happened to
        // pick SHA-256 of deviceUniqueIdentifier, which makes the
        // stored value a cross-product identifier rather than an
        // opaque one. Not a secret - it ships in the package -
        // and it does not need to be: its whole job is domain
        // separation.
        private const string Salt = "unity-docsnap.v1.machine";

        // Where the fallback identifier lives when Unity cannot
        // report a hardware one. EditorPrefs is per-user and
        // per-machine, which is exactly the scope wanted.
        private const string FallbackKey = "UnityDocSnap.MachineFallbackId";

        private static string _cached;

        // ==========================================
        // Current
        // The hashed identifier for this machine, computed
        // once per domain reload.
        // ==========================================
        public static string Current
        {
            get
            {
                if (string.IsNullOrEmpty(_cached)) { _cached = Compute(); }
                return _cached;
            }
        }

        private static string Compute()
        {
            string raw = null;
            try { raw = SystemInfo.deviceUniqueIdentifier; }
            catch { raw = null; }

            // Unity documents this exact sentinel for platforms
            // where no hardware identifier is available. Treating
            // it as a real value would give every such machine the
            // same fingerprint, so one activated key would appear
            // to be the same machine for everybody on that
            // platform - which is the one failure mode this whole
            // file exists to avoid.
            if (string.IsNullOrEmpty(raw) || raw == SystemInfo.unsupportedIdentifier)
            {
                raw = FallbackIdentifier();
            }

            return Hash(Salt + "|" + raw);
        }

        // ==========================================
        // FallbackIdentifier
        // A random identifier generated once and kept in
        // EditorPrefs, for the platforms Unity cannot
        // fingerprint.
        //
        // It is weaker than the hardware one - clearing the
        // Editor's preferences produces a new machine, and a
        // customer who does that has to move their activation
        // across. That is a support email; a shared
        // fingerprint would be a licence that activates
        // everywhere at once, so this is the right way to
        // fail.
        // ==========================================
        private static string FallbackIdentifier()
        {
            string stored = EditorPrefs.GetString(FallbackKey, "");
            if (!string.IsNullOrEmpty(stored)) { return stored; }

            string generated = Guid.NewGuid().ToString("N");
            EditorPrefs.SetString(FallbackKey, generated);
            return generated;
        }

        // ==========================================
        // Label
        // The short, readable name shown next to this
        // machine in the customer's own device list.
        //
        // Deliberately coarse: the device name and the OS
        // family, nothing else. It is there so somebody can
        // tell their desktop from their laptop when
        // releasing a seat, not so a licence server can
        // profile a machine.
        // ==========================================
        public static string Label()
        {
            string name = "";
            try { name = SystemInfo.deviceName ?? ""; } catch { name = ""; }
            if (string.IsNullOrEmpty(name) || name == SystemInfo.unsupportedIdentifier) { name = "Unity Editor"; }

            string platform =
                Application.platform == RuntimePlatform.WindowsEditor ? "Windows" :
                Application.platform == RuntimePlatform.OSXEditor ? "macOS" :
                Application.platform == RuntimePlatform.LinuxEditor ? "Linux" : "Unity";

            // Long device names exist and the label is only ever a
            // hint, so it is bounded before it is sent rather than
            // after it has been stored.
            if (name.Length > 40) { name = name.Substring(0, 40); }
            return name + " (" + platform + ")";
        }

        // ==========================================
        // Hash — lowercase hex SHA-256, the same shape the
        // Worker computes, so the two always agree.
        // ==========================================
        internal static string Hash(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""));
                var sb = new StringBuilder(digest.Length * 2);
                foreach (byte b in digest) { sb.Append(b.ToString("x2")); }
                return sb.ToString();
            }
        }
    }
}
