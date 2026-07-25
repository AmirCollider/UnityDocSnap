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
using AmirCollider.UnityDocSnap.Editor.Manifest;

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

        // Document only the Scenes listed (and enabled) in
        // File > Build Settings, instead of every .unity file
        // under Assets/.
        //
        // Opening a Scene is the most expensive single thing an
        // export does, and a mature project is full of Scenes
        // nobody wants documented: test beds, sandboxes, and
        // sample Scenes that arrived with an imported package.
        // This is the difference between documenting the game
        // and documenting everything that happens to be in the
        // folder.
        public bool scenesInBuildOnly;

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

        // A value unique to this export run, baked into every
        // page. The site's scripts remember it: the first time a
        // browser opens a page from a run it has not seen before,
        // any language/theme the reader picked while viewing an
        // OLDER export is discarded in favour of this run's
        // defaults. Without it, re-exporting with the same
        // defaults as last time never showed those defaults to
        // an exporter who had ever clicked the site's own
        // language/theme buttons.
        public static string ExportStamp = "";

        // Which visual skin the generated site opens with ("cozy" or
        // "lite") and the measurement behind that choice. Measured
        // once per export from the exporting machine plus the weight
        // of this project (see DocSnapCapability), then re-checked in
        // the browser against whatever machine is actually reading
        // the page. Null until the first page is rendered.
        public static DocSnapCapabilityReport Capability;

        public static void Reset()
        {
            DefaultLanguage = "en";
            DefaultTheme = "light";
            HasChangesPage = false;
            ChangesBaseVersion = "";
            VersionLabel = "";
            ExportStamp = "";
            Capability = null;
        }

        public static void Apply(DocSnapExportOptions options, string versionLabel)
        {
            DefaultLanguage = NormalizeLang(options.defaultLanguage);
            DefaultTheme = options.defaultTheme == "dark" ? "dark" : "light";
            HasChangesPage = options.recordChanges;
            ChangesBaseVersion = options.recordChanges ? options.changesBaseVersion : "";
            VersionLabel = versionLabel ?? "";
            ExportStamp = System.DateTime.UtcNow.Ticks.ToString("x");
            Capability = null;
        }

        // ==========================================
        // ResolveCapability
        // The skin verdict for this export, measured on first use
        // and reused for every page in the run so all of them agree.
        //
        // A project setting can force the answer: someone who wants
        // the cozy look on a machine we judged tight, or the lite
        // look on a machine we judged fine, should not have to fight
        // a heuristic - and either way the reader can still switch in
        // the site itself.
        // ==========================================
        public static DocSnapCapabilityReport ResolveCapability(ManifestState manifest)
        {
            if (Capability != null) { return Capability; }

            int gameObjects = 0;
            int files = 0;
            if (manifest != null)
            {
                foreach (ManifestSceneEntry scene in manifest.scenes) { gameObjects += scene.gameObjectCount; }
                foreach (ManifestFolderEntry folder in manifest.assetFolders) { files += folder.fileCount; }
            }

            DocSnapCapabilityReport report = DocSnapCapability.Measure(gameObjects, files);

            string forced = DocSnapSettings.SiteSkin;
            if (forced == DocSnapCapability.SkinCozy || forced == DocSnapCapability.SkinLite)
            {
                report.Skin = forced;
                // The measured reasons are kept even when overridden: the
                // site still shows them if the reader ends up on cozy
                // against them, which is the honest thing to do.
                if (forced == DocSnapCapability.SkinLite) { report.Reasons.Clear(); }
            }

            Capability = report;
            return Capability;
        }

        // The skin every page in this run is rendered with.
        public static string Skin
        {
            get { return Capability != null ? DocSnapCapability.Normalize(Capability.Skin) : DocSnapCapability.SkinLite; }
        }

        public static string NormalizeLang(string lang)
        {
            return lang == "ja" || lang == "fa" ? lang : "en";
        }
    }
}
