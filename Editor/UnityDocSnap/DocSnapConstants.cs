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
        public const string Version = "0.2.1";
        public const string GithubUrl = "https://github.com/AmirCollider/UnityDocSnap";
        public const string Author = "AmirCollider";

        // ==========================================
        // Menu paths (must match README.md exactly)
        // ==========================================
        public const string MenuRoot = "Unity DocSnap/";
        public const string MenuExportScene = MenuRoot + "Export Scene";
        public const string MenuExportAssetInfoEntire = MenuRoot + "Export Asset Info/Entire Assets Folder";
        public const string MenuExportAssetInfoSelected = MenuRoot + "Export Asset Info/Selected Folder...";
        public const string MenuExportFullProject = MenuRoot + "Export Full Project";
        public const string MenuExportFullProjectWithFiles = MenuRoot + "Export Full Project With Files";
        public const string MenuOpenOutputFolder = MenuRoot + "Open Output Folder";
        public const string MenuAbout = MenuRoot + "About Unity DocSnap";

        public const string AssetsContextRoot = "Assets/Unity DocSnap/";

        // ==========================================
        // Output layout (must match README.md exactly)
        // ==========================================
        public const string DefaultOutputFolderName = "UnityDocSnap_Output";
        public const string ScenesSubFolder = "scenes";
        public const string AssetsSubFolder = "assets";
        public const string DataSubFolder = "data";
        public const string SiteAssetsSubFolder = "assets_ui";
        public const string FilesSubFolder = "files";
        public const string IndexFileName = "index.html";
        public const string StyleFileName = "style.css";
        public const string ScriptFileName = "app.js";
        public const string ManifestFileName = "manifest.json";
        public const string EntireProjectFolderKey = "Assets";

        // ==========================================
        // Internal, regeneratable roundtrip state
        // (kept out of the clean output folder)
        // ==========================================
        public const string InternalStateRelativePath = "Library/UnityDocSnap/manifest_state.json";

        // ==========================================
        // Safety limits for reflection/rendering
        // ==========================================
        public const int MaxArrayElementsRendered = 200;
        public const int MaxGenericRecursionDepth = 14;
        public const int DefaultThumbnailMaxDimension = 256;
    }
}
