// ==========================================
// DocSnapSettings
// The tool's configuration, split by who it
// actually belongs to.
//
//   • Settings that describe the PROJECT - what it
//     excludes, where it exports to, which folders
//     are somebody else's code, what the site opens
//     with - live in ProjectSettings/
//     UnityDocSnapSettings.json via
//     DocSnapSettingsStore. They are committed, so a
//     team shares one answer and a CI agent gets the
//     same one; see that file for the full reasoning.
//
//   • Settings that describe the PERSON at the
//     keyboard - the language the export window is
//     drawn in, and the absolute path to a logo on
//     their own disk - stay in EditorUserSettings,
//     which is where per-user data belongs.
//
// Callers see one flat API either way; which store
// backs a given property is decided here and
// nowhere else.
// ==========================================
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapSettings
    {
        // --- Project-scoped keys (committed JSON file) ---
        // Short, readable names: this file is meant to be
        // opened in a pull request, not decoded.
        private const string KeyOutputPath = "outputPath";
        private const string KeyThumbnails = "generateThumbnails";
        private const string KeyDefaultLang = "defaultSiteLanguage";
        private const string KeyDefaultTheme = "defaultSiteTheme";
        private const string KeyExcludes = "excludePatterns";
        private const string KeyAiBundle = "writeAiBundle";
        private const string KeyVendorFolders = "vendorFolders";
        private const string KeySiteSkin = "siteSkin";
        private const string KeyEmbedFonts = "embedFonts";

        // --- Per-user keys (EditorUserSettings, as before) ---
        private const string KeyLogoPath = "UnityDocSnap.CustomLogoPath";
        private const string KeyWindowLang = "UnityDocSnap.WindowLanguage";

        // The legacy EditorUserSettings names, kept only so the
        // one-time migration below can find values a user
        // configured before 0.10.0.
        private const string LegacyOutputPath = "UnityDocSnap.OutputPath";
        private const string LegacyThumbnails = "UnityDocSnap.GenerateThumbnails";
        private const string LegacyDefaultLang = "UnityDocSnap.DefaultLanguage";
        private const string LegacyDefaultTheme = "UnityDocSnap.DefaultTheme";
        private const string LegacyExcludes = "UnityDocSnap.ExcludePatterns";
        private const string LegacyAiBundle = "UnityDocSnap.WriteAiBundle";
        private const string LegacyVendorFolders = "UnityDocSnap.VendorFolders";
        private const string LegacySiteSkin = "UnityDocSnap.SiteSkin";

        // The committed settings file, relative to the project root.
        public const string ProjectSettingsRelativePath = "ProjectSettings/UnityDocSnapSettings.json";

        private static DocSnapSettingsStore _store;

        // ==========================================
        // Store
        // Resolved once per domain reload. The first
        // resolution also migrates anything the user had
        // configured under the old per-user keys, so
        // upgrading does not silently reset a project's
        // exclude list to empty.
        // ==========================================
        internal static DocSnapSettingsStore Store
        {
            get
            {
                if (_store == null)
                {
                    _store = new DocSnapSettingsStore(Path.Combine(ProjectRoot(), ProjectSettingsRelativePath.Replace('/', Path.DirectorySeparatorChar)));
                    MigrateLegacyUserSettings(_store);
                }
                return _store;
            }
        }

        // ==========================================
        // MigrateLegacyUserSettings
        // Pre-0.10.0 installs kept every setting in
        // EditorUserSettings. Adopt those values once, but
        // only for keys the project file does not already
        // answer - a repository that has committed its own
        // settings must win over whatever happens to be in
        // the local user's file.
        // ==========================================
        private static void MigrateLegacyUserSettings(DocSnapSettingsStore store)
        {
            bool adopted = false;
            adopted |= store.ImportMissing(KeyOutputPath, EditorUserSettings.GetConfigValue(LegacyOutputPath));
            adopted |= store.ImportMissing(KeyThumbnails, EditorUserSettings.GetConfigValue(LegacyThumbnails));
            adopted |= store.ImportMissing(KeyDefaultLang, EditorUserSettings.GetConfigValue(LegacyDefaultLang));
            adopted |= store.ImportMissing(KeyDefaultTheme, EditorUserSettings.GetConfigValue(LegacyDefaultTheme));
            adopted |= store.ImportMissing(KeyExcludes, EditorUserSettings.GetConfigValue(LegacyExcludes));
            adopted |= store.ImportMissing(KeyAiBundle, EditorUserSettings.GetConfigValue(LegacyAiBundle));
            adopted |= store.ImportMissing(KeyVendorFolders, EditorUserSettings.GetConfigValue(LegacyVendorFolders));
            adopted |= store.ImportMissing(KeySiteSkin, EditorUserSettings.GetConfigValue(LegacySiteSkin));
            if (adopted) { store.Save(); }
        }

        // ==========================================
        // ProjectRoot — the folder that holds Assets/
        // and ProjectSettings/.
        // ==========================================
        private static string ProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string GetProject(string key, string fallback)
        {
            string raw = Store.Get(key);
            return string.IsNullOrEmpty(raw) ? fallback : raw;
        }

        private static bool GetProjectBool(string key, bool fallback)
        {
            string raw = Store.Get(key);
            if (string.IsNullOrEmpty(raw)) { return fallback; }
            return raw == "1" || raw == "true" || raw == "True";
        }

        // ==========================================
        // OutputRootPath
        // Project-relative or absolute path to the
        // export destination; empty means "use the
        // default UnityDocSnap_Output next to Assets".
        // ==========================================
        public static string OutputRootPath
        {
            get { return Store.Get(KeyOutputPath) ?? ""; }
            set { Store.Set(KeyOutputPath, value ?? ""); }
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
            get { return GetProjectBool(KeyThumbnails, true); }
            set { Store.Set(KeyThumbnails, value ? "1" : "0"); }
        }

        // ==========================================
        // EmbedFonts
        // Whether the branded web fonts are embedded in
        // each version folder's stylesheet.
        //
        // They are ~570 KB of base64, and every version
        // folder gets its own copy so that each export
        // stays a self-contained thing you can zip and send
        // - which is the right default, and is why this is
        // on. It is also 570 KB per export, so a project
        // that keeps twenty snapshots is carrying 11 MB of
        // identical font data. Turning this off drops the
        // embedded faces and lets the site fall back to the
        // system UI stack, which every machine already has:
        // the layout is unchanged, the branded rounded face
        // is not.
        // ==========================================
        public static bool EmbedFonts
        {
            get { return GetProjectBool(KeyEmbedFonts, true); }
            set { Store.Set(KeyEmbedFonts, value ? "1" : "0"); }
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
            get { return GetProject(KeyDefaultLang, "en"); }
            set { Store.Set(KeyDefaultLang, string.IsNullOrEmpty(value) ? "en" : value); }
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
            get { return GetProject(KeyDefaultTheme, "light"); }
            set { Store.Set(KeyDefaultTheme, string.IsNullOrEmpty(value) ? "light" : value); }
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
            get { return Store.Get(KeyExcludes) ?? ""; }
            set { Store.Set(KeyExcludes, value ?? ""); }
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
            get { return GetProjectBool(KeyAiBundle, true); }
            set { Store.Set(KeyAiBundle, value ? "1" : "0"); }
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
            get { return Store.Get(KeyVendorFolders) ?? ""; }
            set
            {
                Store.Set(KeyVendorFolders, value ?? "");
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
            get { return GetProject(KeySiteSkin, "auto"); }
            set { Store.Set(KeySiteSkin, string.IsNullOrEmpty(value) ? "auto" : value); }
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
