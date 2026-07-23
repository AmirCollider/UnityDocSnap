// ==========================================
// DocSnapExportOptions
// One value object carrying every choice the
// export window offers, so the export service
// has a single, explicit input instead of a
// growing list of boolean parameters.
//
// DocSnapRenderContext is the small, static
// side-channel the HTML renderers read while a
// page is being built: the site's default
// language + theme (embedded into every page so
// it opens the way the exporter chose) and
// whether a Changes page exists this run (so the
// sidebar can link to it). It is set once per
// export, on the Editor's single thread, right
// before any page is rendered.
// ==========================================
namespace AmirCollider.UnityDocSnap.Editor.Export
{
    // ==========================================
    // VersionTarget
    // NewVersion   — allocate the next sequential (or a
    //                custom-named) folder, a full fresh
    //                export.
    // ExistingVersion — write into a previously exported
    //                version folder, refreshing it in
    //                place (incremental where possible).
    // ==========================================
    internal enum VersionTarget
    {
        NewVersion,
        ExistingVersion
    }

    internal sealed class DocSnapExportOptions
    {
        // What language / theme the produced site opens in.
        public string defaultLanguage = "en";   // "en" | "ja" | "fa"
        public string defaultTheme = "light";   // "light" | "dark"

        // Which folder this export writes to.
        public VersionTarget versionTarget = VersionTarget.NewVersion;
        public string customVersionName = "";   // optional; blank = auto sequential
        public string existingVersion = "";      // used when versionTarget == ExistingVersion

        // Copy the real asset bytes into source-files/ too.
        public bool includeFiles;

        // Also export a whole-project .unitypackage backup.
        public bool makeBackup;

        // Build a Changes page diffing this export against
        // changesBaseVersion (must be an existing version).
        public bool recordChanges;
        public string changesBaseVersion = "";

        // ==========================================
        // Default — sensible options for a plain
        // "Export Full Project" with no window (menu
        // shortcuts and single-item exports reuse this).
        // ==========================================
        public static DocSnapExportOptions Default(bool includeFiles = false)
        {
            return new DocSnapExportOptions
            {
                defaultLanguage = DocSnapSettings.DefaultSiteLanguage,
                defaultTheme = DocSnapSettings.DefaultSiteTheme,
                versionTarget = VersionTarget.NewVersion,
                includeFiles = includeFiles
            };
        }
    }

    // ==========================================
    // DocSnapRenderContext
    // Static, export-scoped rendering hints read by
    // HtmlPageBuilder / IndexPageRenderer while a page
    // is assembled. Reset at the start of every export.
    // ==========================================
    internal static class DocSnapRenderContext
    {
        public static string DefaultLanguage = "en";
        public static string DefaultTheme = "light";
        public static bool HasChangesPage;
        public static string ChangesBaseVersion = "";
        public static string VersionLabel = "";

        public static void Reset()
        {
            DefaultLanguage = "en";
            DefaultTheme = "light";
            HasChangesPage = false;
            ChangesBaseVersion = "";
            VersionLabel = "";
        }

        public static void Apply(DocSnapExportOptions options, string versionLabel)
        {
            DefaultLanguage = NormalizeLang(options.defaultLanguage);
            DefaultTheme = options.defaultTheme == "dark" ? "dark" : "light";
            HasChangesPage = options.recordChanges;
            ChangesBaseVersion = options.recordChanges ? options.changesBaseVersion : "";
            VersionLabel = versionLabel ?? "";
        }

        public static string NormalizeLang(string lang)
        {
            return lang == "ja" || lang == "fa" ? lang : "en";
        }
    }
}
