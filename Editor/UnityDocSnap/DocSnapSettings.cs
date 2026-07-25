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
        private const string KeyDefaultLang = "UnityDocSnap.DefaultLanguage";
        private const string KeyDefaultTheme = "UnityDocSnap.DefaultTheme";
        private const string KeyWindowLang = "UnityDocSnap.WindowLanguage";
        private const string KeyExcludes = "UnityDocSnap.ExcludePatterns";
        private const string KeyAiBundle = "UnityDocSnap.WriteAiBundle";
        private const string KeyVendorFolders = "UnityDocSnap.VendorFolders";
        private const string KeySiteSkin = "UnityDocSnap.SiteSkin";

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
        // DefaultSiteLanguage
        // Which language the generated site opens in the
        // first time a reader visits it (before they pick
        // one themselves): "en", "ja" or "fa". The export
        // window lets a Japanese or Persian user set this
        // once so the site is friendly out of the box.
        // ==========================================
        public static string DefaultSiteLanguage
        {
            get
            {
                string raw = EditorUserSettings.GetConfigValue(KeyDefaultLang);
                return string.IsNullOrEmpty(raw) ? "en" : raw;
            }
            set { EditorUserSettings.SetConfigValue(KeyDefaultLang, string.IsNullOrEmpty(value) ? "en" : value); }
        }

        // ==========================================
        // DefaultSiteTheme
        // Which colour theme the generated site opens in
        // the first time a reader visits it: "light" or
        // "dark". A reader can still flip it in the site's
        // own sidebar; this is only the initial default.
        // ==========================================
        public static string DefaultSiteTheme
        {
            get
            {
                string raw = EditorUserSettings.GetConfigValue(KeyDefaultTheme);
                return string.IsNullOrEmpty(raw) ? "light" : raw;
            }
            set { EditorUserSettings.SetConfigValue(KeyDefaultTheme, string.IsNullOrEmpty(value) ? "light" : value); }
        }

        // ==========================================
        // WindowLanguage
        // The language the export window's own labels are
        // drawn in ("en" / "ja" / "fa"), so the window is
        // as usable for a Japanese or Persian user as the
        // site it produces.
        // ==========================================
        public static string WindowLanguage
        {
            get
            {
                string raw = EditorUserSettings.GetConfigValue(KeyWindowLang);
                return string.IsNullOrEmpty(raw) ? "en" : raw;
            }
            set { EditorUserSettings.SetConfigValue(KeyWindowLang, string.IsNullOrEmpty(value) ? "en" : value); }
        }

        // ==========================================
        // ExcludePatterns
        // Paths this project never wants documented, one
        // per line (see DocSnapExcludeFilter for the
        // syntax). Empty = document everything, which is
        // the behaviour every earlier version had.
        //
        // This is the highest-leverage setting in the tool
        // for a real project: most of a full export's time
        // and output size goes into imported Asset Store
        // content nobody asked to have documented, and one
        // line here removes it from the file walk, the
        // folder tree, the search index, the change diff
        // and the counts at once.
        // ==========================================
        public static string ExcludePatterns
        {
            get { return EditorUserSettings.GetConfigValue(KeyExcludes) ?? ""; }
            set { EditorUserSettings.SetConfigValue(KeyExcludes, value ?? ""); }
        }

        // ==========================================
        // WriteAiBundle
        // Whether each export also writes the single
        // concatenated summary/ai-bundle.md. On by
        // default - it is the file the README tells
        // people to hand to an assistant, and building
        // it costs one pass over files already written.
        // ==========================================
        public static bool WriteAiBundle
        {
            get
            {
                string raw = EditorUserSettings.GetConfigValue(KeyAiBundle);
                return raw == null ? true : raw == "1";
            }
            set { EditorUserSettings.SetConfigValue(KeyAiBundle, value ? "1" : "0"); }
        }

        // ==========================================
        // VendorFolders
        // Extra folders under Assets/ that hold content this
        // project RECEIVED rather than wrote, one per line -
        // an Asset Store vendor folder, a shared submodule.
        //
        // Unlike ExcludePatterns these are still fully
        // documented; they are only separated out in the health
        // report, so "8 broken references" can say how many are
        // actually yours to fix. Unity's own install locations
        // (TextMesh Pro, Settings, Samples, XR, Plugins, …) are
        // built into DocSnapVendorPaths and do not need listing.
        // ==========================================
        public static string VendorFolders
        {
            get { return EditorUserSettings.GetConfigValue(KeyVendorFolders) ?? ""; }
            set
            {
                EditorUserSettings.SetConfigValue(KeyVendorFolders, value ?? "");
                DocSnapVendorPaths.InvalidateCache();
            }
        }

        // ==========================================
        // SiteSkin
        // Which visual skin the generated site opens with:
        // "auto" (default), "cozy" or "lite".
        //
        // "auto" measures the exporting machine (RAM, cores, GPU)
        // and how heavy the project is, and picks accordingly -
        // cozy when there is room for it, lite when there is not.
        // The two explicit values skip the measurement for someone
        // who already knows what they want. Either way a reader can
        // still switch inside the site itself; this only decides
        // what it opens with.
        // ==========================================
        public static string SiteSkin
        {
            get
            {
                string raw = EditorUserSettings.GetConfigValue(KeySiteSkin);
                return string.IsNullOrEmpty(raw) ? "auto" : raw;
            }
            set { EditorUserSettings.SetConfigValue(KeySiteSkin, string.IsNullOrEmpty(value) ? "auto" : value); }
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
