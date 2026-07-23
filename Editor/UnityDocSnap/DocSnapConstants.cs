// ==========================================
// DocSnapConstants
// Shared constants for menu paths, output
// layout, and safety limits used across the
// Unity DocSnap editor tool.
// ==========================================
namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapConstants
    {
        // ==========================================
        // Identity
        // ==========================================
        public const string ToolName = "Unity DocSnap";
        public const string Version = "0.6.0";
        public const string GithubUrl = "https://github.com/AmirCollider/UnityDocSnap";
        public const string Author = "AmirCollider";

        // ==========================================
        // Menu paths (must match README.md exactly)
        // ==========================================
        public const string MenuRoot = "Unity DocSnap/";
        public const string MenuExportWindow = MenuRoot + "Export… (DocSnap Window)";
        public const string MenuExportScene = MenuRoot + "Export Scene";
        public const string MenuExportAssetInfoEntire = MenuRoot + "Export Asset Info/Entire Assets Folder";
        public const string MenuExportAssetInfoSelected = MenuRoot + "Export Asset Info/Selected Folder...";
        public const string MenuExportFullProject = MenuRoot + "Export Full Project";
        public const string MenuExportFullProjectWithFiles = MenuRoot + "Export Full Project With Files";
        public const string MenuUpdatePreviousExport = MenuRoot + "Update Previous Export";
        public const string MenuOpenOutputFolder = MenuRoot + "Open Output Folder";
        public const string MenuAbout = MenuRoot + "About Unity DocSnap";

        public const string AssetsContextRoot = "Assets/Unity DocSnap/";

        // ==========================================
        // Output layout (must match README.md exactly)
        //
        // Folder names are chosen to read clearly at a
        // glance and to never collide with each other or
        // with Unity's own "Assets" folder:
        //   scenes/       one browsable page + one plain
        //                 summary per Scene
        //   folders/      one page + summary per exported
        //                 Assets sub-folder
        //   data/         the full, structured JSON (the
        //                 "advanced" machine-readable form)
        //   theme/        the site's own CSS/JS + thumbs
        //                 (renamed from "assets_ui", which
        //                 was easily confused with the
        //                 asset pages and with Unity Assets)
        //   source-files/ optional verbatim asset byte
        //                 copies (renamed from "files")
        // ==========================================
        public const string DefaultOutputFolderName = "UnityDocSnap_Output";

        // ==========================================
        // Versioned output (see DocSnapVersioning).
        //
        // Every export now lands in its own versioned
        // sub-folder inside the output root, named
        // "V<major>.<minor>.<patch>" where minor and
        // patch roll over 0→9 (V1.0.0 … V1.0.9, V1.1.0
        // … V1.9.9, V2.0.0 …). The output root itself
        // keeps a versions.html landing page plus a tiny
        // index.html that redirects to the newest one,
        // and a versions_state.json registry (in Library)
        // that records a snapshot of every version so the
        // Changes page can diff any two of them without
        // re-opening old folders.
        // ==========================================
        public const string VersionFolderPrefix = "V";
        public const string RootVersionsFileName = "versions.html";
        public const string RootRedirectFileName = "index.html";
        public const string VersionsStateRelativePath = "Library/UnityDocSnap/versions_state.json";

        // Per-version export metadata: a machine-readable
        // export-info.json plus a plain, readable copy, both
        // written into the version folder, and surfaced on
        // the dashboard as an "Export Info" card.
        public const string ExportInfoFileName = "export-info.json";
        public const string ExportInfoReadableName = "export-info.txt";

        // The Changes (diff-vs-a-previous-version) page and
        // the whole-project .unitypackage backup, both
        // optional and both living inside the version folder.
        public const string ChangesFileName = "changes.html";
        public const string BackupFileName = "project-backup.unitypackage";

        public const string ScenesSubFolder = "scenes";
        public const string AssetsSubFolder = "folders";
        public const string DataSubFolder = "data";
        public const string SiteAssetsSubFolder = "theme";
        public const string FilesSubFolder = "source-files";
        public const string ThumbsSubFolder = "thumbs";
        public const string IndexFileName = "index.html";
        public const string StyleFileName = "style.css";
        public const string ScriptFileName = "app.js";
        public const string ManifestFileName = "manifest.json";
        public const string EntireProjectFolderKey = "Assets";

        // ==========================================
        // Packages page + client-side search index.
        // packages.html lists every UPM package the
        // project depends on (Unity's own vs third
        // party / Git). search-index.js is a tiny JS
        // file that assigns a flat, lightweight record
        // list to a global so the site's search box
        // works even under the file:// origin, where a
        // fetch() of an external .json is blocked.
        // ==========================================
        public const string PackagesFileName = "packages.html";
        public const string SearchIndexFileName = "search-index.js";
        public const string PackagesSummaryName = "packages";

        // A hard ceiling on how many records the search
        // index ever contains, so an enormous project can
        // never produce a search file big enough to hang
        // the browser tab that loads it.
        public const int MaxSearchRecords = 20000;

        // ==========================================
        // Simple, AI-friendly summary output.
        //
        // Every export writes a short summary of each
        // Scene / folder in TWO forms - readable Markdown
        // (.md) and structured, compact JSON (.json) - all
        // gathered in one obvious place: the summary/
        // folder. These are the small files (a few hundred
        // lines) meant to be handed to an AI assistant;
        // data/ still holds the exhaustive every-field
        // JSON. A project-level summary.md at the output
        // root ties them together.
        //
        // Names inside summary/ and data/ share the same
        // self-describing prefixes: "scene-<Name>" and
        // "folder-<Key>".
        // ==========================================
        public const string SummarySubFolder = "summary";
        public const string SummaryMarkdownExtension = ".md";
        public const string SummaryJsonExtension = ".json";
        public const string ProjectSummaryFileName = "summary.md";
        public const string SceneJsonPrefix = "scene-";
        public const string FolderJsonPrefix = "folder-";

        // ==========================================
        // Internal, regeneratable roundtrip state
        // (kept out of the clean output folder)
        // ==========================================
        public const string InternalStateRelativePath = "Library/UnityDocSnap/manifest_state.json";

        // ==========================================
        // Safety limits for reflection/rendering
        // ==========================================
        public const int MaxArrayElementsRendered = 50;
        public const int MaxNestedArrayElementsRendered = 10;
        public const int MaxGenericRecursionDepth = 14;
        public const int DefaultThumbnailMaxDimension = 256;

        // ==========================================
        // Page-weight limits
        // A single folder node that renders thousands
        // of fully-expanded asset cards produces an
        // HTML page no browser can lay out. Capping
        // per node keeps every page interactive; the
        // full, uncapped data always remains in
        // data/folder-*.json.
        // ==========================================
        public const int MaxAssetsRenderedPerFolderNode = 300;
        public const int AssetPreviewTimeoutMs = 400;
    }
}
