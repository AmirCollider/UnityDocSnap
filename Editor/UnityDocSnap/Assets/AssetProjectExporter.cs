// ==========================================
// AssetProjectExporter
// Walks every file inside a chosen Assets
// folder (recursively) and builds a rich,
// self-contained JsonValue entry per file:
// identity, size, a visual preview, importer
// settings, and type-specific extras (Prefab
// contents, Material shader properties,
// script metadata) - metadata only, never a
// copy of the file itself.
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
        // payload, plus the manifest index entries
        // needed for cross-page linking.
        // ==========================================
        public static JsonValue ExportFolder(string folderPath, string folderKey, out List<ManifestAssetIndexEntry> indexEntries, out int fileCount)
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
            }

            root.Set("files", filesArr);
            fileCount = filePaths.Count;
            root.Set("fileCount", fileCount);
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
        // pixels for plain PNG/JPG, importer-reported
        // dimensions plus a mini-thumbnail for other
        // texture formats, or a generic type icon for
        // everything else.
        // ==========================================
        private static void ReadPreview(string path, string absolutePath, AssetImporter importer, Type mainType, JsonValue node)
        {
            TextureImporter textureImporter = importer as TextureImporter;
            if (textureImporter != null)
            {
                int width = 0, height = 0;
                try { textureImporter.GetSourceTextureWidthAndHeight(out width, out height); }
                catch { /* not available for this format/context */ }

                if (DocSnapSettings.GenerateThumbnails)
                {
                    if (RawDecodableImageExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
                    {
                        int rawWidth, rawHeight;
                        string thumb = ThumbnailGenerator.TryGetImageThumbnailBase64(absolutePath, DocSnapConstants.DefaultThumbnailMaxDimension, out rawWidth, out rawHeight);
                        if (thumb != null)
                        {
                            node.Set("thumbnailBase64", thumb);
                            if (width <= 0 || height <= 0) { width = rawWidth; height = rawHeight; }
                        }
                    }
                    if (!node.Has("thumbnailBase64"))
                    {
                        string icon = ThumbnailGenerator.TryGetIconBase64(AssetDatabase.LoadMainAssetAtPath(path));
                        if (icon != null) { node.Set("thumbnailBase64", icon); }
                    }
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
    }
}
