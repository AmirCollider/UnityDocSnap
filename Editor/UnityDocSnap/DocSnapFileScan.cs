// ==========================================
// DocSnapFileScan
// The one filesystem walk the whole tool uses.
//
// It replaces the three separate
// Directory.GetFiles(..., SearchOption.AllDirectories)
// calls that used to live in AssetProjectExporter,
// DocSnapExportService and DocSnapExportInfo, each
// with its own slightly different idea of what to
// skip. That single API call has three problems this
// walk fixes:
//
//   • It aborts the ENTIRE enumeration the moment one
//     sub-directory cannot be read (a permission
//     error, a folder locked by another process), so
//     one bad folder produced an empty export instead
//     of a nearly-complete one.
//   • It follows directory symlinks / Windows
//     junctions without any cycle protection, and
//     symlinked asset folders are common in Unity
//     projects - a loop walked until it ran out of
//     path length.
//   • It does not honour Unity's own "this folder is
//     not part of the project" rules, so hidden
//     folders, "Foo~" folders and CVS folders were
//     counted as assets even though Unity never
//     imports them. The counts and the change diff
//     both drifted from what the Editor actually sees.
// ==========================================
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapFileScan
    {
        // A symlink loop produces an unbounded chain of
        // distinct paths, so a "have I seen this path"
        // set cannot catch it - only a depth ceiling can.
        // 64 is far past any real Unity project layout.
        private const int MaxDirectoryDepth = 64;

        // A hard stop so a pathological tree can never hang
        // the Editor indefinitely. Reaching it is reported.
        private const int MaxDirectoriesVisited = 200000;

        // ==========================================
        // IsIgnoredDirectory
        // Unity's own import rules: a folder whose name
        // starts with '.', ends with '~', or is named "cvs"
        // is invisible to the AssetDatabase. Anything under
        // one of them is not an asset, so counting it made
        // every total disagree with the Project window.
        // ==========================================
        public static bool IsIgnoredDirectory(string directoryName)
        {
            if (string.IsNullOrEmpty(directoryName)) { return true; }
            if (directoryName.StartsWith(".", StringComparison.Ordinal)) { return true; }
            if (directoryName.EndsWith("~", StringComparison.Ordinal)) { return true; }
            return string.Equals(directoryName, "cvs", StringComparison.OrdinalIgnoreCase);
        }

        // ==========================================
        // IsIgnoredFile
        // Hidden dotfiles and .meta sidecars are never
        // assets in their own right.
        // ==========================================
        public static bool IsIgnoredFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) { return true; }
            if (fileName.StartsWith(".", StringComparison.Ordinal)) { return true; }
            return fileName.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
        }

        // ==========================================
        // EnumerateFiles
        // Every non-ignored file under absoluteRoot, as
        // absolute paths, sorted for stable diff-friendly
        // output. An unreadable sub-directory is skipped
        // (and reported once) instead of failing the walk.
        // ==========================================
        public static List<string> EnumerateFiles(string absoluteRoot)
        {
            var results = new List<string>();
            if (string.IsNullOrEmpty(absoluteRoot) || !Directory.Exists(absoluteRoot)) { return results; }

            int visited = 0;
            int unreadable = 0;
            bool hitDepthLimit = false;

            // Depth-first with an explicit stack: the recursion
            // this replaces could not be depth-capped without
            // risking a StackOverflowException on the way there.
            var pending = new Stack<DirectoryLevel>();
            pending.Push(new DirectoryLevel(absoluteRoot, 0));

            while (pending.Count > 0)
            {
                DirectoryLevel level = pending.Pop();
                if (++visited > MaxDirectoriesVisited) { break; }

                try
                {
                    foreach (string file in Directory.GetFiles(level.Path))
                    {
                        if (IsIgnoredFile(Path.GetFileName(file))) { continue; }
                        results.Add(file);
                    }

                    if (level.Depth >= MaxDirectoryDepth)
                    {
                        hitDepthLimit = true;
                        continue;
                    }

                    foreach (string dir in Directory.GetDirectories(level.Path))
                    {
                        if (IsIgnoredDirectory(Path.GetFileName(dir))) { continue; }
                        pending.Push(new DirectoryLevel(dir, level.Depth + 1));
                    }
                }
                catch (Exception)
                {
                    // One unreadable folder must not cost the whole
                    // export; count it and carry on.
                    unreadable++;
                }
            }

            if (unreadable > 0)
            {
                Debug.LogWarning("[Unity DocSnap] Skipped " + unreadable + " folder(s) that could not be read under \"" + absoluteRoot + "\".");
            }
            if (hitDepthLimit)
            {
                Debug.LogWarning("[Unity DocSnap] Stopped descending past " + MaxDirectoryDepth
                    + " folder levels under \"" + absoluteRoot + "\" (a symlink loop or an extremely deep tree).");
            }

            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
        }

        // ==========================================
        // ProjectRootAbsolute
        // The folder that contains Assets/, resolved once
        // per call so no caller has to re-derive it.
        // ==========================================
        public static string ProjectRootAbsolute()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        // ==========================================
        // ToProjectRelative
        // An absolute path under the project root as the
        // forward-slash "Assets/…" form the AssetDatabase
        // and every DocSnap output uses.
        // ==========================================
        public static string ToProjectRelative(string projectRoot, string absolutePath)
        {
            string full = absolutePath.Replace('\\', '/');
            string root = projectRoot.Replace('\\', '/').TrimEnd('/');
            if (full.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
            {
                return full.Substring(root.Length + 1);
            }
            return full;
        }

        // ==========================================
        // EnumerateProjectRelativeFiles
        // The form every caller in this tool actually
        // wants: project-relative paths under one folder,
        // already filtered by the export's exclude rules.
        // ==========================================
        public static List<string> EnumerateProjectRelativeFiles(string projectRelativeFolder, DocSnapExcludeFilter filter)
        {
            string projectRoot = ProjectRootAbsolute();
            string absolute = Path.GetFullPath(Path.Combine(projectRoot, projectRelativeFolder));

            var results = new List<string>();
            foreach (string file in EnumerateFiles(absolute))
            {
                string relative = ToProjectRelative(projectRoot, file);
                if (filter != null && filter.IsExcluded(relative)) { continue; }
                results.Add(relative);
            }
            return results;
        }

        // ==========================================
        // DirectoryLevel
        // One entry on the walk stack: where to look and
        // how deep it already is.
        // ==========================================
        private struct DirectoryLevel
        {
            public readonly string Path;
            public readonly int Depth;

            public DirectoryLevel(string path, int depth)
            {
                Path = path;
                Depth = depth;
            }
        }
    }
}
