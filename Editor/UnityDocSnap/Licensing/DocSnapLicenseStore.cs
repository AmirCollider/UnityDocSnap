// ==========================================
// DocSnapLicenseStore
// Where an activated licence lives on disk.
//
// EditorPrefs, deliberately - not the project's
// committed settings file, and not EditorUserSettings
// either.
//
//   • Not ProjectSettings/UnityDocSnapSettings.json:
//     that file is meant to be committed and read in a
//     pull request. A licence key belongs to a person,
//     not to a repository, and committing one is how a
//     single purchase ends up on every machine that
//     clones the project - including the ones outside
//     the company. Every other setting in this tool
//     was moved INTO that file in 0.10.0 for good
//     reasons; this is the one that must stay out.
//
//   • Not EditorUserSettings: that is per-project too
//     (it writes into the project's own Library), so
//     activating in one project would leave every
//     other project on the same machine unlicensed.
//     The licence is bound to the MACHINE, so the
//     storage should be as well.
//
// EditorPrefs is per-user and per-machine, spanning
// every project that user opens. Activate once, and
// every Unity project on that machine is Pro - which
// is what somebody who bought a licence for their
// machine reasonably expects.
// ==========================================
using UnityEditor;

namespace AmirCollider.UnityDocSnap.Editor.Licensing
{
    internal static class DocSnapLicenseStore
    {
        // The key itself, kept so the token can be renewed
        // silently when it nears expiry without asking the
        // customer to find their email again.
        private const string KeyLicenseKey = "UnityDocSnap.LicenseKey";

        // The signed token from the last successful
        // activation or renewal. This is what actually
        // unlocks Pro; the key above only obtains it.
        private const string KeyToken = "UnityDocSnap.LicenseToken";

        // When the Editor last managed to reach the licence
        // server, as UTC ticks. Drives the quiet background
        // renewal and nothing else - notably, being unable to
        // reach the server does not itself take anything away.
        private const string KeyLastCheck = "UnityDocSnap.LicenseLastCheckUtc";

        // Whether the customer has dismissed the export
        // window's Pro panel. A pitch that cannot be turned
        // off stops being a pitch and becomes an irritation,
        // and an irritated evaluator does not buy.
        private const string KeyUpsellCollapsed = "UnityDocSnap.UpsellCollapsed";

        public static string LicenseKey
        {
            get { return EditorPrefs.GetString(KeyLicenseKey, ""); }
            set { EditorPrefs.SetString(KeyLicenseKey, value ?? ""); }
        }

        public static string Token
        {
            get { return EditorPrefs.GetString(KeyToken, ""); }
            set { EditorPrefs.SetString(KeyToken, value ?? ""); }
        }

        public static long LastCheckUtcTicks
        {
            get
            {
                long ticks;
                return long.TryParse(EditorPrefs.GetString(KeyLastCheck, "0"), out ticks) ? ticks : 0;
            }
            set { EditorPrefs.SetString(KeyLastCheck, value.ToString()); }
        }

        public static bool UpsellCollapsed
        {
            get { return EditorPrefs.GetBool(KeyUpsellCollapsed, false); }
            set { EditorPrefs.SetBool(KeyUpsellCollapsed, value); }
        }

        // ==========================================
        // Clear
        // Forgets this machine's activation entirely.
        //
        // Used both by "Deactivate this machine" and by the
        // recovery path for a token that cannot possibly be
        // ours (a bad signature, another product). Keeping a
        // value that will never verify only guarantees the
        // same failure on every future check.
        // ==========================================
        public static void Clear()
        {
            EditorPrefs.DeleteKey(KeyLicenseKey);
            EditorPrefs.DeleteKey(KeyToken);
            EditorPrefs.DeleteKey(KeyLastCheck);
        }
    }
}
