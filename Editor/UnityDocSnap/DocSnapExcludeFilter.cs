// ==========================================
// DocSnapExcludeFilter
// User-configurable "don't document this"
// rules, applied to every project-relative
// asset path an export touches.
//
// The single most common complaint about a
// whole-project documentation pass is that it
// spends most of its time - and most of its
// output size - on imported Asset Store content
// the user did not write and does not want
// documented. One line in Project Settings
// ("Assets/Plugins") now removes that entirely,
// from the file walk, the folder tree, the
// change diff and the counts alike.
//
// Pattern syntax is intentionally tiny:
//   Assets/Plugins        prefix match - that
//                         folder and everything
//                         under it
//   *.psd                 wildcard match against
//                         the whole path
//   Assets/Art/*/Raw      '*' matches any run of
//                         characters, '?' any one
// Matching is case-insensitive and separator
// agnostic, so a Windows-typed pattern works on
// macOS and vice versa.
// ==========================================
using System;
using System.Collections.Generic;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal sealed class DocSnapExcludeFilter
    {
        private readonly List<string> _prefixes = new List<string>();
        private readonly List<string> _wildcards = new List<string>();

        public bool IsEmpty
        {
            get { return _prefixes.Count == 0 && _wildcards.Count == 0; }
        }

        // The patterns this filter was built from, normalised
        // and de-duplicated. Recorded in export-info.json so a
        // reader can always tell what an export deliberately
        // left out - an undocumented omission is worse than no
        // omission at all.
        public readonly List<string> Patterns = new List<string>();

        // ==========================================
        // Parse
        // Accepts the raw multi-line / semicolon-separated
        // text a user types into Project Settings. Blank
        // lines and lines starting with '#' are ignored so
        // the field can carry comments.
        // ==========================================
        public static DocSnapExcludeFilter Parse(string raw)
        {
            var filter = new DocSnapExcludeFilter();
            if (string.IsNullOrEmpty(raw)) { return filter; }

            string[] pieces = raw.Split(new[] { '\n', '\r', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string piece in pieces)
            {
                string pattern = Normalize(piece);
                if (pattern.Length == 0) { continue; }
                if (pattern.StartsWith("#", StringComparison.Ordinal)) { continue; }
                if (filter.Patterns.Contains(pattern)) { continue; }

                filter.Patterns.Add(pattern);
                if (pattern.IndexOf('*') >= 0 || pattern.IndexOf('?') >= 0)
                {
                    filter._wildcards.Add(pattern);
                }
                else
                {
                    filter._prefixes.Add(pattern);
                }
            }
            return filter;
        }

        // ==========================================
        // AddPrefix
        // Adds one folder-prefix rule after parsing. Used
        // for rules the tool derives rather than the user
        // types; it goes into Patterns like any other,
        // because an export that quietly leaves something
        // out is worse than one that says what it left out.
        // ==========================================
        public DocSnapExcludeFilter AddPrefix(string prefix)
        {
            string pattern = Normalize(prefix);
            if (pattern.Length == 0) { return this; }
            if (Patterns.Contains(pattern)) { return this; }

            Patterns.Add(pattern);
            _prefixes.Add(pattern);
            return this;
        }

        // ==========================================
        // FromSettings
        // The project's configured rules, plus one the tool
        // adds itself: its own output folder, when that
        // folder sits inside Assets.
        //
        // This is a second line of defence, not the first.
        // An output folder inside Assets is refused outright
        // before an export starts (DocSnapOutputPath), so in
        // a correctly configured project this adds nothing.
        // It matters for the project that was configured that
        // way before the rule existed and whose settings file
        // still says so, and it is the difference between an
        // export that documents its own previous output -
        // every run compounding the last - and one that does
        // not. Cheap, and the failure it prevents is not one
        // a user can easily undo.
        // ==========================================
        public static DocSnapExcludeFilter FromSettings()
        {
            DocSnapExcludeFilter filter = Parse(DocSnapSettings.ExcludePatterns);

            string outputRelative = DocSnapOutputPath.ProjectRelativeOrNull(
                DocSnapSettings.ResolveOutputRootAbsoluteWithoutCreating(),
                DocSnapSettings.ProjectRootAbsolute());
            if (!string.IsNullOrEmpty(outputRelative)) { filter.AddPrefix(outputRelative); }

            return filter;
        }

        // ==========================================
        // IsExcluded
        // True when a project-relative path ("Assets/…")
        // is covered by any rule.
        // ==========================================
        public bool IsExcluded(string projectRelativePath)
        {
            if (IsEmpty || string.IsNullOrEmpty(projectRelativePath)) { return false; }
            string path = Normalize(projectRelativePath);

            for (int i = 0; i < _prefixes.Count; i++)
            {
                string prefix = _prefixes[i];
                if (path.Equals(prefix, StringComparison.OrdinalIgnoreCase)) { return true; }
                if (path.Length > prefix.Length
                    && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    && path[prefix.Length] == '/')
                {
                    return true;
                }
            }

            for (int i = 0; i < _wildcards.Count; i++)
            {
                if (WildcardMatch(path, _wildcards[i])) { return true; }
            }
            return false;
        }

        private static string Normalize(string value)
        {
            return value == null ? "" : value.Replace('\\', '/').Trim().TrimEnd('/');
        }

        // ==========================================
        // WildcardMatch
        // Iterative '*' / '?' glob with backtracking -
        // no regex, so a user-typed pattern can never
        // throw or catastrophically backtrack. Both sides
        // are compared case-insensitively.
        // ==========================================
        public static bool WildcardMatch(string text, string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) { return false; }
            if (text == null) { text = ""; }

            int t = 0, p = 0;
            int starP = -1, starT = 0;

            while (t < text.Length)
            {
                if (p < pattern.Length && (pattern[p] == '?' || SameChar(pattern[p], text[t])))
                {
                    t++;
                    p++;
                }
                else if (p < pattern.Length && pattern[p] == '*')
                {
                    starP = p;
                    starT = t;
                    p++;
                }
                else if (starP >= 0)
                {
                    p = starP + 1;
                    t = ++starT;
                }
                else
                {
                    return false;
                }
            }

            while (p < pattern.Length && pattern[p] == '*') { p++; }
            return p == pattern.Length;
        }

        private static bool SameChar(char a, char b)
        {
            return char.ToLowerInvariant(a) == char.ToLowerInvariant(b);
        }
    }
}
