// ==========================================
// AssetProjectExporter
// Walks every file inside a chosen Assets
// folder (recursively) and builds a rich,
// self-contained JsonValue entry per file:
// identity, size, a visual preview, importer
// settings, and type-specific extras (Prefab
// contents, Material shader properties,
// script metadata) - metadata only, never a
// copy of the file itself, plus a folderTree
// grouping those same files back into real
// directories for folder-by-folder navigation.
// Physical file bytes are only ever copied out
// when a caller explicitly opts in via
// copyPhysicalFiles (see CopyPhysicalFile) -
// "Export Full Project With Files" is currently
// the only caller that does.
// ==========================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Manifest;
using AmirCollider.UnityDocSnap.Editor.Reflection;
using AmirCollider.UnityDocSnap.Editor.SceneExport;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor.Assets
{
    internal static class AssetProjectExporter
    {
        private static readonly HashSet<string> RawDecodableImageExtensions =
            new HashSet<string> { ".png", ".jpg", ".jpeg" };

        // ==========================================
        // ExportFolder
        // Entry point: recursively lists every asset
        // under folderPath and builds the full export
        // payload (including a folderTree for real
        // directory-by-directory navigation), plus the
        // manifest index entries needed for cross-page
        // linking. When copyPhysicalFiles is true, each
        // file's actual bytes are additionally mirrored
        // into physicalFilesOutputRoot (used by
        // "Export Full Project With Files" only - every
        // other caller leaves this false, so DocSnap's
        // default behavior stays metadata-only).
        // ==========================================
        public static JsonValue ExportFolder(string folderPath, string folderKey, out List<ManifestAssetIndexEntry> indexEntries, out int fileCount, bool copyPhysicalFiles = false, string physicalFilesOutputRoot = null)
        {
            indexEntries = new List<ManifestAssetIndexEntry>();

            var root = JsonValue.Obj();
            root.Set("folderPath", folderPath);
            root.Set("folderKey", folderKey);
            root.Set("exportedUtc", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            List<string> filePaths = CollectFilePaths(folderPath);
            var filesArr = JsonValue.Arr();
            string htmlFile = DocSnapConstants.AssetsSubFolder + "/" + folderKey + ".html";

            foreach (string path in filePaths)
            {
                JsonValue entry = BuildAssetEntry(path);
                filesArr.Add(entry);

                string guid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid))
                {
                    indexEntries.Add(new ManifestAssetIndexEntry
                    {
                        guid = guid,
                        folderKey = folderKey,
                        htmlFile = htmlFile,
                        anchor = "asset-" + guid,
                        name = Path.GetFileName(path)
                    });
                }

                if (copyPhysicalFiles && !string.IsNullOrEmpty(physicalFilesOutputRoot))
                {
                    // Record the site-relative location of the copied
                    // bytes on the entry itself. Without this the HTML
                    // renderer has no way to reference files/ at all,
                    // so every copied image/audio file was orphaned.
                    if (CopyPhysicalFile(path, physicalFilesOutputRoot))
                    {
                        entry.Set("physicalFile", DocSnapConstants.FilesSubFolder + "/" + path.Replace('\\', '/'));
                    }
                }
            }

            root.Set("files", filesArr);
            fileCount = filePaths.Count;
            root.Set("fileCount", fileCount);
            root.Set("hasPhysicalFiles", copyPhysicalFiles && !string.IsNullOrEmpty(physicalFilesOutputRoot));
            root.Set("folderTree", BuildFolderTree(folderPath, filePaths));
            return root;
        }

        // ==========================================
        // FolderKey
        // Turns "Assets/Images/Backgrounds" into
        // "Images_Backgrounds" (root Assets folder
        // itself becomes "Assets"), matching the
        // output file naming described in README.md.
        // ==========================================
        public static string FolderKey(string folderPath)
        {
            string normalized = folderPath.Replace('\\', '/').TrimEnd('/');
            if (normalized.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return DocSnapConstants.EntireProjectFolderKey;
            }
            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("Assets/".Length);
            }
            return normalized.Replace('/', '_');
        }

        // ==========================================
        // CollectFilePaths
        // Recursively lists every non-folder asset
        // under the given path, sorted for stable,
        // diff-friendly output between exports.
        // ==========================================
        private static List<string> CollectFilePaths(string folderPath)
        {
            var results = new List<string>();
            var seen = new HashSet<string>();
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || seen.Contains(path)) { continue; }
                seen.Add(path);
                if (AssetDatabase.IsValidFolder(path)) { continue; }
                results.Add(path);
            }
            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
        }

        // ==========================================
        // BuildFolderTree
        // Regroups the flat, already-sorted file list
        // back into the real folder structure it lives
        // in, so the output site can offer folder-by-
        // folder navigation instead of one giant list.
        // Stores file paths only (not full entries) -
        // the HTML renderer resolves each path back to
        // its full JsonValue entry from the "files"
        // array, so no asset data is ever duplicated in
        // the exported JSON.
        // ==========================================
        private static JsonValue BuildFolderTree(string rootFolderPath, List<string> filePaths)
        {
            string normalizedRoot = rootFolderPath.Replace('\\', '/').TrimEnd('/');
            var rootNode = new FolderNode(normalizedRoot, PathLeafName(normalizedRoot));

            foreach (string path in filePaths)
            {
                string normalizedPath = path.Replace('\\', '/');
                string relative = normalizedPath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase)
                    ? normalizedPath.Substring(normalizedRoot.Length + 1)
                    : normalizedPath;

                string[] segments = relative.Split('/');
                FolderNode current = rootNode;
                string currentPath = normalizedRoot;
                for (int i = 0; i < segments.Length - 1; i++)
                {
                    currentPath = currentPath + "/" + segments[i];
                    current = current.GetOrAddChild(segments[i], currentPath);
                }
                current.FilePaths.Add(normalizedPath);
            }

            return rootNode.ToJson();
        }

        // ==========================================
        // PathLeafName
        // Last path segment, e.g. "Assets/Images/
        // Backgrounds" -> "Backgrounds".
        // ==========================================
        private static string PathLeafName(string path)
        {
            int slash = path.LastIndexOf('/');
            return slash >= 0 ? path.Substring(slash + 1) : path;
        }

        // ==========================================
        // FolderNode
        // Lightweight in-memory tree used only while
        // building one export pass; converted to a
        // JsonValue once fully populated via ToJson().
        // ==========================================
        private sealed class FolderNode
        {
            public readonly string FolderPath;
            public readonly string FolderName;
            public readonly List<string> FilePaths = new List<string>();
            private readonly List<FolderNode> _children = new List<FolderNode>();
            private readonly Dictionary<string, FolderNode> _childLookup = new Dictionary<string, FolderNode>(StringComparer.OrdinalIgnoreCase);

            public FolderNode(string folderPath, string folderName)
            {
                FolderPath = folderPath;
                FolderName = folderName;
            }

            public FolderNode GetOrAddChild(string name, string path)
            {
                FolderNode child;
                if (!_childLookup.TryGetValue(name, out child))
                {
                    child = new FolderNode(path, name);
                    _childLookup[name] = child;
                    _children.Add(child);
                }
                return child;
            }

            public int CountFilesRecursive()
            {
                int count = FilePaths.Count;
                foreach (FolderNode child in _children) { count += child.CountFilesRecursive(); }
                return count;
            }

            public JsonValue ToJson()
            {
                var node = JsonValue.Obj();
                node.Set("folderName", FolderName);
                node.Set("folderPath", FolderPath);
                node.Set("directFileCount", FilePaths.Count);
                node.Set("totalFileCount", CountFilesRecursive());

                var filesArr = JsonValue.Arr();
                foreach (string p in FilePaths) { filesArr.Add(JsonValue.Str(p)); }
                node.Set("filePaths", filesArr);

                var childrenSorted = new List<FolderNode>(_children);
                childrenSorted.Sort((a, b) => string.Compare(a.FolderName, b.FolderName, StringComparison.OrdinalIgnoreCase));

                var subfoldersArr = JsonValue.Arr();
                foreach (FolderNode child in childrenSorted) { subfoldersArr.Add(child.ToJson()); }
                node.Set("subfolders", subfoldersArr);

                return node;
            }
        }

        // ==========================================
        // BuildAssetEntry
        // Builds the full entry for one file: basic
        // info, a preview, importer settings, and any
        // type-specific extras.
        // ==========================================
        private static JsonValue BuildAssetEntry(string path)
        {
            var node = JsonValue.Obj();
            string fileName = Path.GetFileName(path);
            string extension = Path.GetExtension(path).ToLowerInvariant();
            string guid = AssetDatabase.AssetPathToGUID(path);
            Type mainType = AssetDatabase.GetMainAssetTypeAtPath(path);

            node.Set("path", path);
            node.Set("fileName", fileName);
            node.Set("extension", extension);
            node.Set("guid", guid);
            node.Set("mainType", mainType != null ? mainType.Name : "Unknown");

            string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            ReadFileSystemInfo(absolutePath, node);

            AssetImporter importer = AssetImporter.GetAtPath(path);
            ReadPreview(path, absolutePath, importer, mainType, node);

            if (importer != null)
            {
                node.Set("importerType", importer.GetType().Name);
                node.Set("importerFields", UniversalReflector.ReadTopLevelFields(importer));
            }

            bool handledByExtra = false;
            if (mainType == typeof(Material))
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null)
                {
                    node.Set("materialInfo", MaterialInfoReader.ReadMaterial(material));
                    handledByExtra = true;
                }
            }
            else if (extension == ".prefab")
            {
                GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefabRoot != null)
                {
                    int count;
                    node.Set("prefabRoot", SceneHierarchyExporter.BuildStandaloneGameObjectTree(prefabRoot, out count));
                    node.Set("prefabGameObjectCount", count);
                }
                handledByExtra = true;
            }
            else if (extension == ".cs")
            {
                node.Set("scriptInfo", ReadScriptInfo(absolutePath));
                handledByExtra = true;
            }

            if (!handledByExtra && mainType != null && mainType != typeof(Texture2D) && mainType != typeof(Sprite))
            {
                UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                if (mainAsset != null)
                {
                    node.Set("assetFields", UniversalReflector.ReadTopLevelFields(mainAsset));
                }
            }

            return node;
        }

        // ==========================================
        // ReadFileSystemInfo
        // Raw file size and last-modified time - the
        // one piece of data that never comes from the
        // AssetDatabase itself.
        // ==========================================
        private static void ReadFileSystemInfo(string absolutePath, JsonValue node)
        {
            try
            {
                var info = new FileInfo(absolutePath);
                if (info.Exists)
                {
                    node.Set("fileSizeBytes", info.Length);
                    node.Set("lastModifiedUtc", info.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                }
            }
            catch
            {
                // best-effort only
            }
        }

        // ==========================================
        // ReadPreview
        // Chooses the best available visual: exact
        // pixels for plain PNG/JPG when GenerateThumbnails
        // is on, importer-reported dimensions always, and
        // a generic type icon fallback for every asset -
        // images included - so no asset card is ever left
        // with the bare placeholder glyph.
        // ==========================================
        private static void ReadPreview(string path, string absolutePath, AssetImporter importer, Type mainType, JsonValue node)
        {
            TextureImporter textureImporter = importer as TextureImporter;
            if (textureImporter != null)
            {
                int width = 0, height = 0;
                try { textureImporter.GetSourceTextureWidthAndHeight(out width, out height); }
                catch { /* not available for this format/context */ }

                if (DocSnapSettings.GenerateThumbnails && RawDecodableImageExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
                {
                    int rawWidth, rawHeight;
                    string thumb = ThumbnailGenerator.TryGetImageThumbnailBase64(absolutePath, DocSnapConstants.DefaultThumbnailMaxDimension, out rawWidth, out rawHeight);
                    if (thumb != null)
                    {
                        node.Set("thumbnailBase64", thumb);
                        if (width <= 0 || height <= 0) { width = rawWidth; height = rawHeight; }
                    }
                }

                // Generic type icon fallback: kept on regardless of the
                // GenerateThumbnails toggle above, matching the non-image
                // branch below, so an image asset always gets at least a
                // visual instead of the bare placeholder glyph.
                if (!node.Has("thumbnailBase64"))
                {
                    string icon = ThumbnailGenerator.TryGetIconBase64(AssetDatabase.LoadMainAssetAtPath(path));
                    if (icon != null) { node.Set("thumbnailBase64", icon); }
                }

                if (width > 0 && height > 0)
                {
                    node.Set("imageWidth", width);
                    node.Set("imageHeight", height);
                }
                return;
            }

            // Non-image assets: this is a small generic type icon (script,
            // audio speaker, model cube, …), not a preview of file content,
            // so it stays on regardless of the pixels-preview setting above.
            UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (mainAsset != null)
            {
                string icon = ThumbnailGenerator.TryGetIconBase64(mainAsset);
                if (icon != null) { node.Set("thumbnailBase64", icon); }
            }
        }

        // ==========================================
        // ReadScriptInfo
        // Lightweight, text-based heuristic for .cs
        // files: class name and base type, without
        // dumping the entire source into the export.
        // ==========================================
        private static JsonValue ReadScriptInfo(string absolutePath)
        {
            var node = JsonValue.Obj();
            try
            {
                string text = File.ReadAllText(absolutePath);
                Match match = Regex.Match(text, @"\bclass\s+(\w+)\s*(:\s*([\w\.<>,\s]+))?");
                if (match.Success)
                {
                    string baseTypes = match.Groups[3].Success ? match.Groups[3].Value.Trim() : "";
                    node.Set("className", match.Groups[1].Value);
                    node.Set("baseTypes", baseTypes);
                    node.Set("isMonoBehaviour", baseTypes.Contains("MonoBehaviour"));
                    node.Set("isScriptableObject", baseTypes.Contains("ScriptableObject"));
                    node.Set("isEditorScript", text.Contains("UnityEditor"));
                }
                node.Set("lineCount", text.Split('\n').Length);
            }
            catch
            {
                // best-effort only; absence of scriptInfo is not fatal
            }
            return node;
        }

        // ==========================================
        // CopyPhysicalFile
        // Mirrors one asset's actual file bytes (and,
        // if present, its sibling .meta file) into the
        // output's files/ tree, preserving the same
        // relative path it has under Assets/, so
        // "Export Full Project With Files" can hand
        // someone a self-contained, re-importable copy
        // of the project content alongside the metadata
        // site. Only ever invoked when a caller opts in
        // explicitly - every other export path in
        // DocSnap never touches file bytes at all.
        // ==========================================
        internal static bool CopyPhysicalFile(string assetPath, string physicalFilesOutputRoot)
        {
            try
            {
                string sourceAbsolute = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
                if (!File.Exists(sourceAbsolute)) { return false; }

                string destinationAbsolute = Path.Combine(physicalFilesOutputRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(destinationAbsolute));
                File.Copy(sourceAbsolute, destinationAbsolute, true);

                string sourceMeta = sourceAbsolute + ".meta";
                if (File.Exists(sourceMeta))
                {
                    File.Copy(sourceMeta, destinationAbsolute + ".meta", true);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Unity DocSnap] Could not copy physical file for \"" + assetPath + "\": " + ex.Message);
                return false;
            }
        }
    }
}
