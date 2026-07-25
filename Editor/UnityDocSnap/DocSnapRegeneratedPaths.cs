// ==========================================
// DocSnapRegeneratedPaths
// Files whose bytes move without anybody having
// edited them.
//
// The Changes page answers one question: what did
// somebody do to this project since the last export?
// A handful of files under Assets/ are rewritten by
// Unity itself, on its own schedule, and they arrive
// at that question as a lie.
//
// The clearest case, and the one this exists for:
//
//   Assets/TextMesh Pro/Resources/Fonts & Materials/
//   LiberationSans SDF - Fallback.asset
//
// TextMesh Pro's dynamic font assets carry their
// glyph table and atlas texture inside the .asset
// itself. Whenever TMP renders a character that is
// not in the atlas yet - which includes the Editor's
// own UI drawing text during startup - it adds the
// glyph and serialises the asset back to disk. Open
// Unity, close Unity, and the file has genuinely
// different bytes. Nobody touched it.
//
// DocSnapVersioning.HasFileChanged already promotes
// the content hash over the size + timestamp
// signature, which correctly stopped reporting files
// the filesystem merely touched. This one defeats
// that too, because the bytes really are different -
// so it appeared under "Modified" on nearly every
// Changes page of a project nobody had edited, and it
// came back the moment a reader dismissed it.
//
// A diff that always contains the same one entry
// trains its reader to skim past the list, which
// costs far more than the entry itself. So these are
// classified and listed separately - stated, never
// silently dropped, exactly as an excluded folder is.
//
// Matching reuses DocSnapExcludeFilter's glob so the
// pattern syntax a project already knows ('*', '?',
// case-insensitive, separator-agnostic) is the same
// one here.
// ==========================================
using System;
using System.Collections.Generic;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapRegeneratedPaths
    {
        // ==========================================
        // The built-in list.
        //
        // Deliberately narrow. Every entry has to be a file
        // Unity rewrites WITHOUT the user asking, because the
        // whole value of this list is that a reader can trust
        // what it hides. Lightmaps, NavMesh data and occlusion
        // culling data are all regenerated files too, and none
        // of them belong here: they change when somebody presses
        // Bake, which is an authored change and exactly the kind
        // of thing a Changes page exists to show.
        //
        // TextMesh Pro ships under two folder names depending on
        // how it was installed, and both are covered.
        // ==========================================
        private static readonly string[] BuiltInPatterns =
        {
            // Dynamic font assets: glyph table + atlas texture live
            // inside the .asset and are rewritten as glyphs render.
            "Assets/TextMesh Pro/Resources/Fonts & Materials/*.asset",
            "Assets/TextMeshPro/Resources/Fonts & Materials/*.asset",

            // Sprite assets are serialised the same way and churn for
            // the same reason.
            "Assets/TextMesh Pro/Sprite Assets/*.asset",
            "Assets/TextMeshPro/Sprite Assets/*.asset"
        };

        // ==========================================
        // IsRegenerated
        // True when a project-relative path is one Unity
        // rewrites on its own.
        // ==========================================
        public static bool IsRegenerated(string projectRelativePath)
        {
            if (string.IsNullOrEmpty(projectRelativePath)) { return false; }
            string path = Normalize(projectRelativePath);

            List<string> patterns = Patterns();
            for (int i = 0; i < patterns.Count; i++)
            {
                if (DocSnapExcludeFilter.WildcardMatch(path, patterns[i])) { return true; }
            }
            return false;
        }

        // ==========================================
        // Describe
        // Which pattern caught a path, so the page can answer
        // "why is this not counted as a change?" rather than
        // asking the reader to take it on trust. "" when the
        // path is not classified.
        // ==========================================
        public static string Describe(string projectRelativePath)
        {
            if (string.IsNullOrEmpty(projectRelativePath)) { return ""; }
            string path = Normalize(projectRelativePath);

            List<string> patterns = Patterns();
            for (int i = 0; i < patterns.Count; i++)
            {
                if (DocSnapExcludeFilter.WildcardMatch(path, patterns[i])) { return patterns[i]; }
            }
            return "";
        }

        // ==========================================
        // Patterns
        // The built-in list plus whatever the project added,
        // normalised and de-duplicated. Cached against the raw
        // settings string for the same reason
        // DocSnapVendorPaths caches its own: this is called
        // once per file in the diff, and the setting only
        // changes when somebody edits it.
        // ==========================================
        private static string _cachedRaw;
        private static List<string> _cachedPatterns;

        public static List<string> Patterns()
        {
            string raw = DocSnapSettings.RegeneratedPaths ?? "";
            if (_cachedPatterns != null && _cachedRaw == raw) { return _cachedPatterns; }

            var patterns = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string builtIn in BuiltInPatterns)
            {
                if (seen.Add(builtIn)) { patterns.Add(builtIn); }
            }

            foreach (string piece in raw.Split(new[] { '\n', '\r', ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string pattern = Normalize(piece);
                if (pattern.Length == 0 || pattern.StartsWith("#", StringComparison.Ordinal)) { continue; }
                if (seen.Add(pattern)) { patterns.Add(pattern); }
            }

            _cachedRaw = raw;
            _cachedPatterns = patterns;
            return patterns;
        }

        private static string Normalize(string value)
        {
            return value == null ? "" : value.Replace('\\', '/').Trim().TrimEnd('/');
        }

        // Test seam, and the hook the settings setter calls:
        // the pattern list is cached against the raw settings
        // string, and a change to that setting has to be noticed.
        public static void InvalidateCache()
        {
            _cachedRaw = null;
            _cachedPatterns = null;
        }
    }
}
