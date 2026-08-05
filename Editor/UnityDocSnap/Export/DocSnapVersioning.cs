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

        // A hash of the file's actual bytes, and the authority on
        // whether the file really changed.
        //
        // signature alone answered "did the filesystem touch this?",
        // which is not the same question. Unity re-stamps assets it
        // manages on its own schedule - the clearest case being
        // "TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF
        // - Fallback.asset", which the dynamic font atlas rewrites
        // whenever a glyph is rendered - so a file nobody had opened
        // showed up under "Modified" on nearly every Changes page.
        // Reporting a change that did not happen is the same class of
        // bug as missing one that did.
        //
        // Computed only when the cheap signature says something may
        // have moved: an unchanged size+timestamp reuses the hash
        // already recorded, so a repeat export reads no file bytes at
        // all. Empty on snapshots written before 0.8.1, and the diff
        // falls back to signature for those rather than calling every
        // file modified during the upgrade.
        public string contentHash;
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
        public string defaultLanguage = DocSnapLanguages.Fallback;
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
        // HasFileChanged
        // Whether a file present in both versions actually
        // changed.
        //
        // The size + last-write signature answers "did the
        // filesystem touch this?", which is a different question,
        // and Unity touches assets it manages on its own schedule.
        // The clearest case is
        // "Assets/TextMesh Pro/Resources/Fonts & Materials/
        // LiberationSans SDF - Fallback.asset", which the dynamic
        // font atlas rewrites whenever a glyph is rendered - so it
        // turned up under "Modified" on nearly every Changes page of
        // a project nobody had edited. Reporting a change that did
        // not happen is the same class of bug as missing one that
        // did: either way the page stops being trustworthy.
        //
        // The content hash is the authority whenever both sides have
        // one. A snapshot written before hashes existed has none, and
        // falling back to the signature there is better than calling
        // every file in the project modified once, during the upgrade.
        // ==========================================
        public static bool HasFileChanged(VersionFileEntry old, VersionFileEntry current)
        {
            if (old == null || current == null) { return old != current; }

            if (!string.IsNullOrEmpty(old.contentHash) && !string.IsNullOrEmpty(current.contentHash))
            {
                return !string.Equals(old.contentHash, current.contentHash, StringComparison.OrdinalIgnoreCase);
            }
            return old.signature != current.signature;
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
            if (name == "." || name == "..") { return false; }

            // Path.GetInvalidFileNameChars() is the whole check the runtime
            // offers, and on Windows it is not enough: a name ending in '.'
            // or a space is silently TRIMMED when the directory is created,
            // so "V1." becomes a folder called "V1" while the registry, the
            // versions page and every link keep saying "V1." - which then
            // collides with the real V1 the next time one is made. Rejecting
            // it up front is the only way the name the user typed and the
            // folder they get are ever the same thing.
            char last = name[name.Length - 1];
            if (last == '.' || last == ' ') { return false; }

            // Reserved DOS device names, still reserved on modern Windows,
            // with or without an extension ("CON", "COM1.txt", …). Creating
            // one fails outright, so an export would die at the very last
            // step with an unexplained IO error.
            return !IsReservedDeviceName(name);
        }

        private static readonly string[] ReservedDeviceNames =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        private static bool IsReservedDeviceName(string name)
        {
            int dot = name.IndexOf('.');
            string stem = dot >= 0 ? name.Substring(0, dot) : name;
            foreach (string reserved in ReservedDeviceNames)
            {
                if (string.Equals(stem, reserved, StringComparison.OrdinalIgnoreCase)) { return true; }
            }
            return false;
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

        // ==========================================
        // DeleteVersion
        // Removes one snapshot: its folder from disk and its
        // entry from the registry.
        //
        // Both halves, or neither. The registry in Library/ is
        // what the shelf cap counts, so a folder deleted without
        // its entry leaves a version the tool believes in and
        // cannot open, and an entry deleted without its folder
        // leaves a folder nothing will ever clean up. (Deleting
        // the folder BY HAND is a third case and is deliberately
        // left alone - the registry keeps counting, which is what
        // stops hand-deletion being a way around the cap on the
        // editions that have one.)
        //
        // Refuses to touch a folder that is not one of ours. The
        // proof is the same one PruneDir uses - the version-pinned
        // theme/style.css every export writes - because this is the
        // only other place in the tool that deletes a directory
        // tree, and a version name that somehow resolved to
        // somewhere else must not be the thing that discovers it.
        // A folder that is already gone is not an error: the entry
        // still goes.
        //
        // The registry is CHANGED but not written; the caller saves.
        // A method that both deletes and persists cannot be tested
        // without a test writing over the developer's own shelf.
        // ==========================================
        public static bool DeleteVersion(string outputRoot, VersionsState state, string version, out string error)
        {
            error = "";
            if (state == null || string.IsNullOrEmpty(version)) { error = "No version to delete."; return false; }

            string folder = VersionFolderAbsolute(outputRoot, version);

            try
            {
                if (Directory.Exists(folder))
                {
                    if (!IsDocSnapVersionFolder(folder))
                    {
                        error = "\"" + folder + "\" does not look like a Unity DocSnap version folder, so it was left alone.";
                        return false;
                    }
                    Directory.Delete(folder, true);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            state.versions.RemoveAll(v => string.Equals(v.version, version, StringComparison.OrdinalIgnoreCase));

            // The active version is what a single-Scene export writes
            // into. Pointing it at a folder that no longer exists would
            // have the next quick export silently create it again.
            if (string.Equals(state.activeVersion, version, StringComparison.OrdinalIgnoreCase))
            {
                state.activeVersion = NewestVersion(state);
            }

            return true;
        }

        // ==========================================
        // CanClearOutputRoot
        // The output folder may only be cleared once the shelf is
        // empty.
        //
        // One button that deletes everything is a different
        // proposition from one that deletes a snapshot, and the
        // difference is that nobody can undo it. Requiring the
        // shelf to be empty first means the whole thing can only
        // ever be reached by deleting each snapshot deliberately,
        // one at a time, having read what each one was - which is
        // the point at which somebody knows what they are throwing
        // away.
        // ==========================================
        public static bool CanClearOutputRoot(VersionsState state)
        {
            return state != null && state.versions.Count == 0;
        }

        // ==========================================
        // ClearOutputRoot
        // Empties the output root of the things DocSnap put in it:
        // the versions landing page, the redirect, and any
        // remaining version folder.
        //
        // The ROOT ITSELF is never deleted, and neither is anything
        // in it this tool did not write. The folder is chosen by the
        // user and can be shared with other output - "Build/Docs" is
        // the tool's own documented example - so removing the
        // directory would be removing somebody else's work with it.
        // ==========================================
        public static bool ClearOutputRoot(string outputRoot, VersionsState state, out string error)
        {
            error = "";
            if (!CanClearOutputRoot(state))
            {
                error = "Delete the remaining snapshots first.";
                return false;
            }

            try
            {
                if (!Directory.Exists(outputRoot)) { return true; }

                foreach (string file in Directory.GetFiles(outputRoot, "*", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(file);
                    if (string.Equals(name, DocSnapConstants.RootVersionsFileName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(name, DocSnapConstants.RootRedirectFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                    }
                }

                foreach (string folder in Directory.GetDirectories(outputRoot))
                {
                    if (IsDocSnapVersionFolder(folder)) { Directory.Delete(folder, true); }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            state.activeVersion = "";
            return true;
        }

        // The ownership proof both deletions above rely on, and the
        // same one PruneDir uses: the version-pinned theme/style.css
        // that every export writes into every version folder before
        // anything else happens.
        private static bool IsDocSnapVersionFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder)) { return false; }
            return File.Exists(Path.Combine(Path.Combine(folder, DocSnapConstants.SiteAssetsSubFolder),
                DocSnapConstants.StyleFileName));
        }
    }
}
