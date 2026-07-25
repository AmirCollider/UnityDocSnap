// ==========================================
// DocSnapVendorPaths
// Splits "things I made" from "things Unity and my
// packages dropped into Assets/".
//
// The health report was honest but useless without
// this: a project would report eight findings, seven
// of them in Assets/Settings (the render-pipeline
// assets a Unity template creates) and one in
// Assets/TextMesh Pro (TMP Essentials). None of them
// is the author's to fix, none can be deleted, and
// they sat at the top of the list on every export
// burying anything that actually was the author's
// problem. A count you are expected to ignore trains
// you to ignore the count.
//
// Classification is by path prefix, because that is
// what these folders actually are: fixed install
// locations a package or template writes into. The
// built-in list covers what Unity itself ships; the
// project can add its own (an Asset Store vendor
// folder, a shared submodule) in Project Settings.
// ==========================================
using System;
using System.Collections.Generic;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapVendorPaths
    {
        public const string OwnerMine = "mine";
        public const string OwnerVendor = "vendor";

        // ==========================================
        // Folders Unity's own templates, packages and
        // "import Essentials" buttons install into. Every one
        // of these is content a project receives rather than
        // writes, and none of it is safe to hand-edit.
        //
        //   TextMesh Pro       TMP Essentials / Examples
        //   Settings           URP / HDRP pipeline assets from
        //                      a Unity template
        //   TutorialInfo       the "Learn" template scaffolding
        //   Samples            package sample imports
        //   XR, XRI            XR plug-in management + the
        //                      Interaction Toolkit's own assets
        //   Standard Assets    Unity's legacy bundle
        //   Plugins            the conventional third-party drop
        //   Editor Default
        //   Resources, Gizmos  Unity's magic Editor folders
        // ==========================================
        private static readonly string[] BuiltInVendorFolders =
        {
            "Assets/TextMesh Pro",
            "Assets/TextMeshPro",
            "Assets/Settings",
            "Assets/TutorialInfo",
            "Assets/Samples",
            "Assets/XR",
            "Assets/XRI",
            "Assets/Standard Assets",
            "Assets/Plugins",
            "Assets/Editor Default Resources",
            "Assets/Gizmos"
        };

        // ==========================================
        // Classify
        // OwnerVendor when the project-relative path sits
        // under a vendor folder, OwnerMine otherwise.
        //
        // Anything outside Assets/ (a Packages/… path, or a
        // label that is not a path at all) counts as vendor:
        // an installed package is the definition of code
        // somebody else maintains.
        // ==========================================
        public static string Classify(string projectRelativePath)
        {
            return IsVendor(projectRelativePath) ? OwnerVendor : OwnerMine;
        }

        public static bool IsVendor(string projectRelativePath)
        {
            if (string.IsNullOrEmpty(projectRelativePath)) { return false; }
            string path = Normalize(projectRelativePath);

            if (path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase)) { return true; }

            foreach (string folder in VendorFolders())
            {
                if (IsUnderOrEqual(path, folder)) { return true; }
            }
            return false;
        }

        // ==========================================
        // VendorFolders
        // The built-in list plus whatever the project added,
        // normalised and de-duplicated. Cached per raw
        // settings string: this is called once per finding
        // and the settings value only changes when someone
        // edits it.
        // ==========================================
        private static string _cachedRaw;
        private static List<string> _cachedFolders;

        public static List<string> VendorFolders()
        {
            string raw = DocSnapSettings.VendorFolders ?? "";
            if (_cachedFolders != null && _cachedRaw == raw) { return _cachedFolders; }

            var folders = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string builtIn in BuiltInVendorFolders)
            {
                if (seen.Add(builtIn)) { folders.Add(builtIn); }
            }

            foreach (string piece in raw.Split(new[] { '\n', '\r', ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string folder = Normalize(piece);
                if (folder.Length == 0 || folder.StartsWith("#", StringComparison.Ordinal)) { continue; }
                if (seen.Add(folder)) { folders.Add(folder); }
            }

            _cachedRaw = raw;
            _cachedFolders = folders;
            return folders;
        }

        // ==========================================
        // Describe
        // Which vendor folder a path fell under, for the
        // "why is this not my file?" question a reader will
        // reasonably ask. "" when the path is the project's
        // own.
        // ==========================================
        public static string Describe(string projectRelativePath)
        {
            if (string.IsNullOrEmpty(projectRelativePath)) { return ""; }
            string path = Normalize(projectRelativePath);
            if (path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase)) { return "Packages/"; }
            foreach (string folder in VendorFolders())
            {
                if (IsUnderOrEqual(path, folder)) { return folder; }
            }
            return "";
        }

        private static bool IsUnderOrEqual(string path, string folder)
        {
            if (path.Equals(folder, StringComparison.OrdinalIgnoreCase)) { return true; }
            return path.Length > folder.Length
                && path.StartsWith(folder, StringComparison.OrdinalIgnoreCase)
                && path[folder.Length] == '/';
        }

        private static string Normalize(string value)
        {
            return value == null ? "" : value.Replace('\\', '/').Trim().TrimEnd('/');
        }

        // Test seam: the folder list is cached against the raw
        // settings string, and a test that changes the setting
        // needs the cache to notice.
        public static void InvalidateCache()
        {
            _cachedRaw = null;
            _cachedFolders = null;
        }
    }
}
