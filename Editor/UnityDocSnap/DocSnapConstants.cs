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

        // Must match "version" in package.json. It drifted once
        // (the constant said 0.6.2 while the package said 0.6.4,
        // so every generated manifest.json, export-info.json,
        // summary footer, dashboard badge and About window
        // reported a version the package had not been on for two
        // releases). PackageVersionTests now fails the build if
        // the two ever disagree again.
        public const string Version = "0.9.0";
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

        // Byte copies of the files the Changes page lists,
        // so each entry is downloadable for review:
        //   changes-files/new/Assets/…  the file as it is in
        //                               THIS export (copied
        //                               from the live project)
        //   changes-files/old/Assets/…  the file as it was in
        //                               the compared version
        //                               (copied from that
        //                               version's source-files/,
        //                               when it has one)
        public const string ChangesFilesSubFolder = "changes-files";
        public const string ChangesFilesNewSubFolder = "new";
        public const string ChangesFilesOldSubFolder = "old";

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

        // ==========================================
        // Project health / issues page.
        //
        // The dashboard used to say "8 broken references
        // in Assets" and link to the Assets page - a page
        // with thousands of rows on it. Knowing a project
        // has eight broken references but not WHERE is
        // barely better than not being told, so every
        // finding is now recorded with the exact object,
        // component and field it sits on, listed on its
        // own page, and linked straight to the card that
        // holds it.
        // ==========================================
        public const string IssuesFileName = "issues.html";
        public const string IssuesSummaryName = "issues";

        // A project that is broken enough to produce tens of
        // thousands of findings does not need every one of them
        // enumerated - it needs the first few hundred and an
        // honest note that there are more. The cap also keeps the
        // manifest state file (and the issues page) bounded.
        public const int MaxIssuesPerScope = 400;
        public const int MaxIssuesRendered = 2000;

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

        // The one-file hand-off: every summary in the export
        // concatenated into a single Markdown document, so a
        // whole project can be dropped into an AI assistant as
        // one paste instead of a dozen. Capped (see
        // MaxAiBundleCharacters) so it stays inside a normal
        // context window rather than being silently unusable.
        public const string AiBundleFileName = "ai-bundle.md";
        public const int MaxAiBundleCharacters = 600000;

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

        // How many sibling fields one nested struct / managed
        // reference may contribute before the walk stops. This
        // limit already existed as a bare "guard > 1000" inside
        // UniversalReflector; naming it makes it visible, and the
        // reflector now marks the node as truncated instead of
        // silently returning a partial object.
        public const int MaxGenericFieldsPerObject = 1000;

        // How deep a GameObject hierarchy is walked. Every other
        // recursion in the tool was capped; this one was not, and
        // a deep enough chain (a procedural rig, a generated tree)
        // threw StackOverflowException - which .NET cannot catch,
        // so it took the entire Unity Editor down rather than
        // failing one export.
        public const int MaxHierarchyDepth = 256;

        // Total wall-clock budget, per export, for waiting on
        // Unity's asynchronous asset-preview renderer. Previously
        // each asset was allowed to block the main thread for up
        // to AssetPreviewTimeoutMs on its own, so a project with
        // thousands of non-image assets could spend well over half
        // an hour asleep with a frozen Editor. Once the budget is
        // spent the export keeps going and simply falls back to
        // type icons.
        public const int AssetPreviewTotalBudgetMs = 20000;

        // How many assets are processed between calls to Unity's
        // unused-asset unloader. A full export loads every asset
        // in the project to read its metadata; without this the
        // whole project stayed resident in memory at once.
        public const int AssetUnloadInterval = 250;

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

        // The largest file whose bytes are read to fingerprint it for
        // the Changes diff. Above this, size + last-write time is used
        // instead: reading a multi-gigabyte asset to answer "did it
        // change?" costs more than the answer is worth, and a file that
        // large changing size is the normal case anyway.
        public const long MaxHashedFileBytes = 128L * 1024L * 1024L;
    }
}
