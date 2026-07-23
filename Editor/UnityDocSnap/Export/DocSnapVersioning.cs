// ==========================================
// DocSnapVersioning
// Turns the single, overwritten output folder
// into a growing shelf of versioned snapshots.
//
// Each export lands in its own folder named
// "V<major>.<minor>.<patch>". Minor and patch are
// single base-10 digits that roll over:
//   V1.0.0 → V1.0.9 → V1.1.0 → … → V1.9.9 → V2.0.0
// The very first export is V1.0.0. The user can
// also type a custom version name in the export
// window; anything is accepted as long as it does
// not collide with an existing folder.
//
// A small registry (Library/UnityDocSnap/
// versions_state.json) records one VersionSnapshot
// per version - counts, timing, the file/scene/
// package inventory - so the Changes page can diff
// any two versions without re-opening old folders,
// and so the newest version is always known.
// ==========================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor.Export
{
    // ==========================================
    // VersionFileEntry / VersionSceneEntry /
    // VersionPackageEntry
    // The per-version inventory the Changes page
    // diffs. Deliberately tiny and flat so a whole
    // project's registry stays small and round-trips
    // through Unity's JsonUtility.
    // ==========================================
    [Serializable]
    internal sealed class VersionFileEntry
    {
        public string path;       // "Assets/…" project-relative
        public long size;         // bytes
        public string signature;  // size + last-write ticks (cheap "changed?" fingerprint)
    }

    [Serializable]
    internal sealed class VersionSceneEntry
    {
        public string name;
        public int gameObjectCount;
    }

    [Serializable]
    internal sealed class VersionPackageEntry
    {
        public string name;
        public string version;
        public bool updateAvailable;
    }

    // ==========================================
    // VersionSnapshot
    // Everything worth remembering about one export.
    // ==========================================
    [Serializable]
    internal sealed class VersionSnapshot
    {
        public string version = "";          // "V1.0.3"
        public string exportedUtc = "";      // ISO-8601 UTC
        public string exportedLocal = "";    // human, in the machine's local zone
        public string timeZone = "";         // "UTC+03:30 · Iran Standard Time"
        public int sceneCount;
        public int assetCount;               // non-.meta files under Assets/
        public int packageCount;
        public int packagesUpdatable;
        public string defaultLanguage = "en";
        public string defaultTheme = "light";
        public bool withFiles;
        public bool hasBackup;
        public string changesBaseVersion = ""; // version this export's Changes page diffs against ("" = none)

        public List<VersionFileEntry> files = new List<VersionFileEntry>();
        public List<VersionSceneEntry> scenes = new List<VersionSceneEntry>();
        public List<VersionPackageEntry> packages = new List<VersionPackageEntry>();
    }

    [Serializable]
    internal sealed class VersionsState
    {
        public string activeVersion = "";                 // the folder single-item exports currently write into
        public List<VersionSnapshot> versions = new List<VersionSnapshot>();
    }

    internal static class DocSnapVersioning
    {
        // ==========================================
        // Registry load / save (Library-local, never
        // part of the published output).
        // ==========================================
        public static string RegistryAbsolutePath()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, DocSnapConstants.VersionsStateRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        public static VersionsState LoadRegistry()
        {
            string path = RegistryAbsolutePath();
            if (!File.Exists(path)) { return new VersionsState(); }
            try
            {
                VersionsState state = JsonUtility.FromJson<VersionsState>(File.ReadAllText(path));
                if (state == null) { state = new VersionsState(); }
                state.versions = state.versions ?? new List<VersionSnapshot>();
                return state;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Unity DocSnap] Could not read versions registry, starting fresh. " + ex.Message);
                return new VersionsState();
            }
        }

        public static void SaveRegistry(VersionsState state)
        {
            string path = RegistryAbsolutePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(state, true));
        }

        // ==========================================
        // FindSnapshot — the recorded snapshot for a
        // version string, or null.
        // ==========================================
        public static VersionSnapshot FindSnapshot(VersionsState state, string version)
        {
            if (state == null || string.IsNullOrEmpty(version)) { return null; }
            return state.versions.Find(v => v.version == version);
        }

        // ==========================================
        // UpsertSnapshot — records/updates one version's
        // snapshot, keeping the list ordered oldest→newest.
        // ==========================================
        public static void UpsertSnapshot(VersionsState state, VersionSnapshot snapshot)
        {
            state.versions.RemoveAll(v => v.version == snapshot.version);
            state.versions.Add(snapshot);
            state.versions.Sort((a, b) => CompareVersions(a.version, b.version));
        }

        // ==========================================
        // ExistingVersionNames — every version folder that
        // physically exists in the output root plus every
        // one the registry remembers, unioned. Reading the
        // disk too means a folder created outside the
        // registry still blocks a name collision.
        // ==========================================
        public static HashSet<string> ExistingVersionNames(string outputRoot, VersionsState state)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (state != null)
            {
                foreach (VersionSnapshot v in state.versions) { names.Add(v.version); }
            }
            try
            {
                if (Directory.Exists(outputRoot))
                {
                    foreach (string dir in Directory.GetDirectories(outputRoot))
                    {
                        string name = Path.GetFileName(dir);
                        if (name.StartsWith(DocSnapConstants.VersionFolderPrefix, StringComparison.Ordinal) && ParseVersion(name) != null)
                        {
                            names.Add(name);
                        }
                    }
                }
            }
            catch { /* best-effort disk read */ }
            return names;
        }

        // ==========================================
        // NextVersion — the next unused sequential version
        // after the newest one that exists. Empty shelf →
        // V1.0.0. Skips any name already taken (so a custom
        // name never gets overwritten by the sequence).
        // ==========================================
        public static string NextVersion(string outputRoot, VersionsState state)
        {
            HashSet<string> taken = ExistingVersionNames(outputRoot, state);

            int[] highest = null;
            foreach (string name in taken)
            {
                int[] parsed = ParseVersion(name);
                if (parsed == null) { continue; }
                if (highest == null || CompareParsed(parsed, highest) > 0) { highest = parsed; }
            }

            int[] next = highest == null ? new[] { 1, 0, 0 } : Increment(highest);
            string candidate = Format(next);
            // Walk forward over any custom-named collisions.
            int guard = 0;
            while (taken.Contains(candidate) && guard++ < 100000)
            {
                next = Increment(next);
                candidate = Format(next);
            }
            return candidate;
        }

        // ==========================================
        // Increment — patch++ with 0→9 roll-over into
        // minor, then minor into major. Major has no cap.
        // ==========================================
        private static int[] Increment(int[] v)
        {
            int major = v[0], minor = v[1], patch = v[2];
            patch++;
            if (patch > 9) { patch = 0; minor++; }
            if (minor > 9) { minor = 0; major++; }
            return new[] { major, minor, patch };
        }

        // ==========================================
        // ParseVersion — "V1.2.3" → {1,2,3}, or null if it
        // is not a DocSnap version folder name.
        // ==========================================
        public static int[] ParseVersion(string name)
        {
            if (string.IsNullOrEmpty(name)) { return null; }
            string core = name.StartsWith(DocSnapConstants.VersionFolderPrefix, StringComparison.OrdinalIgnoreCase)
                ? name.Substring(DocSnapConstants.VersionFolderPrefix.Length)
                : name;
            string[] parts = core.Split('.');
            if (parts.Length != 3) { return null; }
            var nums = new int[3];
            for (int i = 0; i < 3; i++)
            {
                int n;
                if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out n) || n < 0) { return null; }
                nums[i] = n;
            }
            return nums;
        }

        public static string Format(int[] v)
        {
            return DocSnapConstants.VersionFolderPrefix + v[0] + "." + v[1] + "." + v[2];
        }

        // ==========================================
        // CompareVersions — orders two version names.
        // Non-standard (custom) names sort after parseable
        // ones, alphabetically among themselves, so the
        // sequence stays predictable.
        // ==========================================
        public static int CompareVersions(string a, string b)
        {
            int[] pa = ParseVersion(a);
            int[] pb = ParseVersion(b);
            if (pa != null && pb != null) { return CompareParsed(pa, pb); }
            if (pa != null) { return -1; }
            if (pb != null) { return 1; }
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareParsed(int[] a, int[] b)
        {
            for (int i = 0; i < 3; i++)
            {
                if (a[i] != b[i]) { return a[i].CompareTo(b[i]); }
            }
            return 0;
        }

        // ==========================================
        // IsValidCustomName — a user-typed version name is
        // accepted when it is non-empty and safe as a single
        // folder name (no path separators or invalid chars).
        // It does not have to follow the V#.#.# scheme.
        // ==========================================
        public static bool IsValidCustomName(string name)
        {
            if (string.IsNullOrEmpty(name)) { return false; }
            if (name.IndexOf('/') >= 0 || name.IndexOf('\\') >= 0) { return false; }
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (name.IndexOf(c) >= 0) { return false; }
            }
            return name != "." && name != "..";
        }

        // ==========================================
        // NewestVersion — the newest version name recorded,
        // or "" when nothing has been exported yet.
        // ==========================================
        public static string NewestVersion(VersionsState state)
        {
            string newest = "";
            foreach (VersionSnapshot v in state.versions)
            {
                if (newest == "" || CompareVersions(v.version, newest) > 0) { newest = v.version; }
            }
            return newest;
        }

        // ==========================================
        // VersionFolderAbsolute — the on-disk folder for a
        // version name inside the output root.
        // ==========================================
        public static string VersionFolderAbsolute(string outputRoot, string version)
        {
            return Path.Combine(outputRoot, version);
        }
    }
}
