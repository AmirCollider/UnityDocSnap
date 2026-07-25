// ==========================================
// DocSnapOutputPath
// Decides whether a configured export destination is
// somewhere Unity DocSnap may actually write.
//
// The output folder used to be a free-text field with
// no rules at all: whatever a user typed into Project
// Settings, or a build agent passed to -docsnapOutput,
// was created and exported into. Most values are fine.
// A few are quietly destructive, and the two worst are
// also the two most natural things to type.
//
//   "Assets/Docs"
//     Unity imports everything under Assets/. A full
//     export of a real project is thousands of HTML and
//     JSON files, so the Editor spends the next several
//     minutes generating a .meta for every one of them -
//     and then the NEXT export walks Assets/, finds the
//     output of the last one, and documents it. Each run
//     documents every previous run. The folder grows
//     superlinearly and nothing in the tool ever said no.
//
//   "Library/Docs" (or Temp/, or Logs/)
//     Unity owns these and deletes them. An export
//     written there is not a snapshot, it is a file
//     waiting to be thrown away - usually right after
//     the user has closed the Editor believing their
//     documentation is safe.
//
// Two more are rejected because the one place the tool
// deletes files (DocSnapExportService.PruneDir /
// CleanLegacyOutput) resolves paths relative to the
// output root: the project root itself, and any folder
// that CONTAINS the project.
//
// Everything here is pure - it takes the project root as
// an argument rather than asking Unity for it - so the
// rules can be tested directly, which is what
// DocSnapOutputPathTests does.
// ==========================================
using System;
using System.IO;

namespace AmirCollider.UnityDocSnap.Editor
{
    // ==========================================
    // Why a path was rejected. The enum, rather than a
    // message, is what the validator returns: the rule is
    // language-free logic, and the wording belongs to
    // whichever surface reports it (Project Settings, the
    // export window, the command line).
    // ==========================================
    internal enum DocSnapOutputPathVerdict
    {
        Ok,
        Empty,
        InsideAssets,
        InsideUnityManagedFolder,
        InsideProjectSettings,
        InsidePackages,
        IsProjectRoot,
        ContainsProject,
        IsFilesystemRoot,
        Unusable
    }

    internal static class DocSnapOutputPath
    {
        // Folders Unity or the IDE deletes and regenerates.
        // None of them is a place a snapshot can survive.
        //
        // Deliberately NOT here: Build/. A build output folder
        // is a perfectly sensible home for documentation that
        // ships beside the build - it is the example in
        // DocSnapAPI's own header - and nothing wipes it
        // without being asked.
        private static readonly string[] UnityManagedFolders = { "Library", "Temp", "Logs", "obj" };

        // ==========================================
        // Resolve
        // A configured value (empty, project-relative, or
        // absolute) reduced to one absolute path, without
        // creating anything. Returns null when the value
        // cannot be resolved at all - a path with invalid
        // characters, or one long enough to throw.
        // ==========================================
        public static string Resolve(string configured, string projectRoot)
        {
            if (string.IsNullOrEmpty(projectRoot)) { return null; }

            try
            {
                string root = Path.GetFullPath(projectRoot);
                if (string.IsNullOrEmpty(configured) || configured.Trim().Length == 0)
                {
                    return Path.GetFullPath(Path.Combine(root, DocSnapConstants.DefaultOutputFolderName));
                }

                string trimmed = configured.Trim();
                return Path.IsPathRooted(trimmed)
                    ? Path.GetFullPath(trimmed)
                    : Path.GetFullPath(Path.Combine(root, trimmed));
            }
            catch (Exception)
            {
                // ArgumentException / NotSupportedException / PathTooLongException
                // are all "the user typed something that is not a path",
                // which is a verdict rather than a crash.
                return null;
            }
        }

        // ==========================================
        // Validate
        // The verdict for an already-resolved absolute path.
        //
        // Comparison is case-insensitive on every platform,
        // not just the ones with case-insensitive file
        // systems. On Linux "assets/docs" and "Assets/Docs"
        // really are different folders - but a project that
        // has both is pathological, and treating them as one
        // costs a user nothing while catching the far more
        // likely case of a path typed with the wrong case.
        // ==========================================
        public static DocSnapOutputPathVerdict Validate(string resolvedAbsolute, string projectRoot)
        {
            if (string.IsNullOrEmpty(resolvedAbsolute)) { return DocSnapOutputPathVerdict.Unusable; }
            if (string.IsNullOrEmpty(projectRoot)) { return DocSnapOutputPathVerdict.Unusable; }

            string output, root;
            try
            {
                output = Normalize(Path.GetFullPath(resolvedAbsolute));
                root = Normalize(Path.GetFullPath(projectRoot));
            }
            catch (Exception)
            {
                return DocSnapOutputPathVerdict.Unusable;
            }

            if (output.Length == 0) { return DocSnapOutputPathVerdict.Empty; }
            if (IsFilesystemRoot(output)) { return DocSnapOutputPathVerdict.IsFilesystemRoot; }
            if (Same(output, root)) { return DocSnapOutputPathVerdict.IsProjectRoot; }

            // The output folder holding the project is the mirror
            // image of the project holding the output, and it is
            // worse: a prune inside it is a prune over the project.
            if (IsInside(root, output)) { return DocSnapOutputPathVerdict.ContainsProject; }

            if (IsInsideOrEqual(output, Join(root, "Assets"))) { return DocSnapOutputPathVerdict.InsideAssets; }
            if (IsInsideOrEqual(output, Join(root, "ProjectSettings"))) { return DocSnapOutputPathVerdict.InsideProjectSettings; }
            if (IsInsideOrEqual(output, Join(root, "Packages"))) { return DocSnapOutputPathVerdict.InsidePackages; }

            foreach (string managed in UnityManagedFolders)
            {
                if (IsInsideOrEqual(output, Join(root, managed))) { return DocSnapOutputPathVerdict.InsideUnityManagedFolder; }
            }

            return DocSnapOutputPathVerdict.Ok;
        }

        // ==========================================
        // ResolveAndValidate — the two steps together, which
        // is how every caller actually uses them.
        // ==========================================
        public static DocSnapOutputPathVerdict ResolveAndValidate(string configured, string projectRoot, out string resolved)
        {
            resolved = Resolve(configured, projectRoot);
            if (resolved == null) { return DocSnapOutputPathVerdict.Unusable; }
            return Validate(resolved, projectRoot);
        }

        public static bool IsAcceptable(string configured, string projectRoot)
        {
            string ignored;
            return ResolveAndValidate(configured, projectRoot, out ignored) == DocSnapOutputPathVerdict.Ok;
        }

        // ==========================================
        // ProjectRelativeOrNull
        // The "Assets/…" spelling of an output folder that
        // sits inside the project's Assets folder, or null
        // when it does not.
        //
        // Validate refuses such a path, so in a correctly
        // configured project this always returns null. It
        // exists for the project that was ALREADY configured
        // that way before this rule existed, and for a
        // session override that slipped through: the asset
        // walk feeds it to the exclude filter so that, even
        // in the worst case, an export cannot document its
        // own output.
        // ==========================================
        public static string ProjectRelativeOrNull(string resolvedAbsolute, string projectRoot)
        {
            if (string.IsNullOrEmpty(resolvedAbsolute) || string.IsNullOrEmpty(projectRoot)) { return null; }

            string output, root;
            try
            {
                output = Normalize(Path.GetFullPath(resolvedAbsolute));
                root = Normalize(Path.GetFullPath(projectRoot));
            }
            catch (Exception)
            {
                return null;
            }

            if (!IsInside(output, root)) { return null; }
            return output.Substring(root.Length + 1);
        }

        // ==========================================
        // Path helpers.
        //
        // Every comparison goes through Normalize first so
        // that "C:\Proj\Assets\", "C:/Proj/Assets" and
        // "C:\proj\assets" are one path. A trailing separator
        // is dropped because "Assets/" and "Assets" must not
        // be able to disagree about whether one contains the
        // other.
        // ==========================================
        private static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path)) { return ""; }
            string normalized = path.Replace('\\', '/').TrimEnd();
            // A lone "/" (or "C:/") is a root and must keep its
            // separator; anything longer loses a trailing one.
            while (normalized.Length > 1 && normalized.EndsWith("/", StringComparison.Ordinal)
                   && !IsFilesystemRoot(normalized))
            {
                normalized = normalized.Substring(0, normalized.Length - 1);
            }
            return normalized;
        }

        private static string Join(string root, string child)
        {
            return root + "/" + child;
        }

        private static bool Same(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        // True when `candidate` sits strictly below `parent`.
        private static bool IsInside(string candidate, string parent)
        {
            if (string.IsNullOrEmpty(candidate) || string.IsNullOrEmpty(parent)) { return false; }
            if (Same(candidate, parent)) { return false; }

            // A root already ends in '/', so appending another
            // would produce "//" and never match.
            string prefix = parent.EndsWith("/", StringComparison.Ordinal) ? parent : parent + "/";
            return candidate.Length > prefix.Length
                && candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsInsideOrEqual(string candidate, string parent)
        {
            return Same(candidate, parent) || IsInside(candidate, parent);
        }

        // ==========================================
        // IsFilesystemRoot
        // "/" on Unix, "C:/" on Windows. Exporting to one is
        // never intended and would put a version folder - and
        // a prune - at the top of the disk.
        // ==========================================
        private static bool IsFilesystemRoot(string normalized)
        {
            if (string.IsNullOrEmpty(normalized)) { return false; }
            if (normalized == "/") { return true; }
            // "C:" and "C:/" alike.
            if (normalized.Length == 2 && normalized[1] == ':') { return true; }
            if (normalized.Length == 3 && normalized[1] == ':' && normalized[2] == '/') { return true; }
            return false;
        }
    }
}
