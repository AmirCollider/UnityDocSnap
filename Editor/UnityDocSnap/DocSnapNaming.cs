// ==========================================
// DocSnapNaming
// The single place that turns a project path
// (a Scene's asset path, an Assets sub-folder)
// into the name its output files are written
// under.
//
// Two rules matter here and both used to be
// missing:
//   1. The name must be SAFE as a file name on
//      every OS. A Scene called "Level: 1/2" is
//      perfectly legal in Unity on macOS/Linux
//      and cannot be written to disk verbatim
//      on Windows.
//   2. The name must be UNIQUE. Two Scenes named
//      Main.unity in different folders both used
//      to resolve to scenes/Main.html, so the
//      second export silently overwrote the
//      first and the sidebar showed two entries
//      pointing at one page. Same for folder
//      keys: "UI/Menu" and "UI_Menu" both
//      flattened to "UI_Menu".
//
// Disambiguation is deliberately lazy: a name is
// only given a short hash suffix when it would
// otherwise be ambiguous, so the overwhelmingly
// common case keeps the friendly, readable
// "scenes/Main.html" it always had.
// ==========================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapNaming
    {
        // Long enough to stay readable, short enough that
        // "<name>-<hash>.html" never trips a path-length
        // limit on Windows when nested in a deep output root.
        private const int ShortHashLength = 6;
        private const int MaxNameLength = 96;

        // Characters that are legal in a file name on some OS
        // but hostile in a file:// URL, an HTML id, or the
        // other OS. Collapsed to '_' alongside whatever
        // Path.GetInvalidFileNameChars() reports.
        private const string ExtraUnsafeChars = "#%?*:\"<>|";

        // Windows refuses these as file names regardless of
        // extension, even on a drive that is otherwise fine.
        private static readonly HashSet<string> ReservedDeviceNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CON", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            };

        // ==========================================
        // StableHashHex
        // FNV-1a over the UTF-16 code units. Deliberately
        // NOT string.GetHashCode(): that is randomised per
        // process on modern runtimes, so an anchor or file
        // name built from it would change every time Unity
        // restarted and every cross-link in a previously
        // exported site would rot.
        // ==========================================
        public static string StableHashHex(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                if (value != null)
                {
                    foreach (char c in value)
                    {
                        hash ^= c;
                        hash *= 16777619;
                    }
                }
                return hash.ToString("x8", CultureInfo.InvariantCulture);
            }
        }

        public static string ShortHash(string value)
        {
            return StableHashHex(value).Substring(0, ShortHashLength);
        }

        // ==========================================
        // NormalizePath
        // One canonical spelling for a project path so the
        // same folder never hashes two different ways.
        // ==========================================
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) { return ""; }
            return path.Replace('\\', '/').Trim().TrimEnd('/');
        }

        // ==========================================
        // IsSafeFileNameChar / SanitizeFileName
        // Everything unsafe collapses to '_'. Spaces, dashes
        // and dots are kept, because Unity project names use
        // them constantly and mangling them would make every
        // exported page unrecognisable.
        // ==========================================
        public static bool IsSafeFileNameChar(char c)
        {
            if (c < 0x20 || c == 0x7F) { return false; }
            if (c == '/' || c == '\\') { return false; }
            if (ExtraUnsafeChars.IndexOf(c) >= 0) { return false; }
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                if (c == invalid) { return false; }
            }
            return true;
        }

        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) { return "unnamed"; }

            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                sb.Append(IsSafeFileNameChar(c) ? c : '_');
            }

            // A trailing dot or space is silently dropped by
            // Windows when the file is created, which would make
            // the written name differ from the name recorded in
            // the manifest - and therefore break every link to it.
            string result = sb.ToString().Trim().TrimEnd('.', ' ');
            if (result.Length == 0) { return "unnamed"; }
            if (result.Length > MaxNameLength) { result = result.Substring(0, MaxNameLength).TrimEnd('.', ' '); }
            if (result.Length == 0) { return "unnamed"; }
            if (ReservedDeviceNames.Contains(result)) { result = "_" + result; }
            return result;
        }

        // ==========================================
        // SceneKey
        // The base name every output file for one Scene is
        // written under: scenes/<key>.html, data/scene-<key>.json,
        // summary/scene-<key>.md|json.
        //
        // allScenePaths is the full list of Scenes in the
        // project (cheap: one AssetDatabase.FindAssets call).
        // It is what makes the "only disambiguate when needed"
        // rule possible - a single Scene called Main keeps the
        // plain "Main", and the moment a second Main.unity
        // appears anywhere in the project BOTH become
        // "Main-<hash>" so neither can quietly overwrite the
        // other. Passing null keeps the plain name, which is
        // only correct for callers that genuinely have no
        // project context (the tests).
        // ==========================================
        public static string SceneKey(string scenePath, ICollection<string> allScenePaths)
        {
            string baseName = SafeFileNameWithoutExtension(scenePath);
            string safe = SanitizeFileName(baseName);

            if (allScenePaths == null || allScenePaths.Count <= 1) { return safe; }

            int sameName = 0;
            foreach (string other in allScenePaths)
            {
                if (string.Equals(SanitizeFileName(SafeFileNameWithoutExtension(other)), safe, StringComparison.OrdinalIgnoreCase))
                {
                    sameName++;
                    if (sameName > 1) { break; }
                }
            }

            return sameName > 1 ? safe + "-" + ShortHash(NormalizePath(scenePath)) : safe;
        }

        // ==========================================
        // FolderKey
        // "Assets/Images/Backgrounds" -> "Images_Backgrounds",
        // the root "Assets" folder -> "Assets".
        //
        // Flattening '/' to '_' is only injective while no
        // segment already contains '_': without that check
        // "UI/Menu" and "UI_Menu" produce the same key and
        // therefore the same output files. When the path is
        // ambiguous (or sanitising had to change a character)
        // a short hash of the original path is appended, so
        // the friendly key survives for every ordinary folder
        // and only genuinely ambiguous ones pay for it.
        // ==========================================
        public static string FolderKey(string folderPath)
        {
            string normalized = NormalizePath(folderPath);
            if (normalized.Length == 0 || normalized.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return DocSnapConstants.EntireProjectFolderKey;
            }

            string relative = normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring("Assets/".Length)
                : normalized;

            string flattened = relative.Replace('/', '_');
            string safe = SanitizeFileName(flattened);

            bool ambiguous = relative.IndexOf('_') >= 0 || !string.Equals(safe, flattened, StringComparison.Ordinal);
            return ambiguous ? safe + "-" + ShortHash(normalized) : safe;
        }

        // ==========================================
        // SafeFileNameWithoutExtension
        // Path.GetFileNameWithoutExtension throws on a path
        // containing characters it considers invalid, which
        // is exactly the input this class exists to handle.
        // ==========================================
        private static string SafeFileNameWithoutExtension(string path)
        {
            string normalized = NormalizePath(path);
            if (normalized.Length == 0) { return ""; }

            int slash = normalized.LastIndexOf('/');
            string fileName = slash >= 0 ? normalized.Substring(slash + 1) : normalized;

            int dot = fileName.LastIndexOf('.');
            return dot > 0 ? fileName.Substring(0, dot) : fileName;
        }
    }
}
