// ==========================================
// DocSnapSettings
// Project-scoped configuration (output path,
// custom logo, thumbnail toggle) persisted via
// EditorUserSettings so values never leak
// between different Unity projects on the
// same machine the way EditorPrefs would.
// ==========================================
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapSettings
    {
        private const string KeyOutputPath = "UnityDocSnap.OutputPath";
        private const string KeyLogoPath = "UnityDocSnap.CustomLogoPath";
        private const string KeyThumbnails = "UnityDocSnap.GenerateThumbnails";

        // ==========================================
        // OutputRootPath
        // Project-relative or absolute path to the
        // export destination; empty means "use the
        // default UnityDocSnap_Output next to Assets".
        // ==========================================
        public static string OutputRootPath
        {
            get { return EditorUserSettings.GetConfigValue(KeyOutputPath) ?? ""; }
            set { EditorUserSettings.SetConfigValue(KeyOutputPath, value ?? ""); }
        }

        // ==========================================
        // CustomLogoAbsolutePath
        // Optional path to the user's own logo image;
        // empty means "use the built-in mascot mark".
        // ==========================================
        public static string CustomLogoAbsolutePath
        {
            get { return EditorUserSettings.GetConfigValue(KeyLogoPath) ?? ""; }
            set { EditorUserSettings.SetConfigValue(KeyLogoPath, value ?? ""); }
        }

        // ==========================================
        // GenerateThumbnails
        // On by default so exported Asset pages show
        // real image previews instead of a placeholder
        // icon. Turn this off for DocSnap's stricter
        // "pixels never leave your project" mode (see
        // README.md roadmap).
        // ==========================================
        public static bool GenerateThumbnails
        {
            get
            {
                string raw = EditorUserSettings.GetConfigValue(KeyThumbnails);
                return raw == null ? true : raw == "1";
            }
            set { EditorUserSettings.SetConfigValue(KeyThumbnails, value ? "1" : "0"); }
        }

        // ==========================================
        // ResolveOutputRootAbsolute
        // Resolves the effective, absolute output
        // folder, creating it on first use.
        // ==========================================
        public static string ResolveOutputRootAbsolute()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string configured = OutputRootPath;

            string resolved = string.IsNullOrEmpty(configured)
                ? Path.Combine(projectRoot, DocSnapConstants.DefaultOutputFolderName)
                : (Path.IsPathRooted(configured) ? configured : Path.Combine(projectRoot, configured));

            Directory.CreateDirectory(resolved);
            return resolved;
        }
    }
}
