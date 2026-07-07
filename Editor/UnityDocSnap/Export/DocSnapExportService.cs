// ==========================================
// DocSnapExportService
// The one place that wires everything together
// for a menu action: gather data, update the
// manifest, write data/*.json, render HTML,
// and keep the dashboard current.
// ==========================================
using System;
using System.Collections.Generic;
using System.IO;
using AmirCollider.UnityDocSnap.Editor.Assets;
using AmirCollider.UnityDocSnap.Editor.Html;
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Manifest;
using AmirCollider.UnityDocSnap.Editor.SceneExport;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor.Export
{
    internal static class DocSnapExportService
    {
        // ==========================================
        // ExportScene
        // Exports one Scene, refreshing its own page
        // and the dashboard.
        // ==========================================
        public static void ExportScene(string scenePath)
        {
            string outputRoot = PrepareOutput();
            ManifestState manifest = DocSnapManifest.Load();

            JsonValue sceneData;
            int goCount;
            try
            {
                sceneData = SceneHierarchyExporter.ExportScene(scenePath, out goCount);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(DocSnapConstants.ToolName, "Could not export scene:\n" + scenePath + "\n\n" + ex.Message, "OK");
                return;
            }

            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            string htmlFile = DocSnapConstants.ScenesSubFolder + "/" + sceneName + ".html";
            string jsonFile = DocSnapConstants.DataSubFolder + "/scene_" + sceneName + ".json";

            WriteText(outputRoot, jsonFile, sceneData.ToString());

            DocSnapManifest.UpsertScene(manifest, new ManifestSceneEntry
            {
                sceneName = sceneName,
                scenePath = scenePath,
                htmlFile = htmlFile,
                jsonFile = jsonFile,
                exportedUtc = sceneData.Get("exportedUtc").AsString(""),
                gameObjectCount = goCount
            });
            DocSnapManifest.Save(manifest);

            WriteText(outputRoot, htmlFile, ScenePageRenderer.Render(sceneData, manifest, htmlFile));
            RefreshIndexAndManifest(outputRoot, manifest);

            EditorUtility.DisplayDialog(DocSnapConstants.ToolName, "Exported scene \"" + sceneName + "\" (" + goCount + " GameObjects).", "OK");
            RevealOutput(outputRoot);
        }

        // ==========================================
        // ExportFolder
        // Exports one Assets folder (recursively),
        // refreshing its own page and the dashboard.
        // ==========================================
        public static void ExportFolder(string folderPath)
        {
            string outputRoot = PrepareOutput();
            ManifestState manifest = DocSnapManifest.Load();

            string folderKey = AssetProjectExporter.FolderKey(folderPath);
            List<ManifestAssetIndexEntry> indexEntries;
            int fileCount;
            JsonValue folderData = AssetProjectExporter.ExportFolder(folderPath, folderKey, out indexEntries, out fileCount);

            string htmlFile = DocSnapConstants.AssetsSubFolder + "/" + folderKey + ".html";
            string jsonFile = DocSnapConstants.DataSubFolder + "/assets_" + folderKey + ".json";

            WriteText(outputRoot, jsonFile, folderData.ToString());

            DocSnapManifest.ReplaceAssetIndexForFolder(manifest, folderKey, indexEntries);
            DocSnapManifest.UpsertFolder(manifest, new ManifestFolderEntry
            {
                folderPath = folderPath,
                folderKey = folderKey,
                htmlFile = htmlFile,
                jsonFile = jsonFile,
                exportedUtc = folderData.Get("exportedUtc").AsString(""),
                fileCount = fileCount
            });
            DocSnapManifest.Save(manifest);

            WriteText(outputRoot, htmlFile, AssetPageRenderer.Render(folderData, manifest, htmlFile));
            RefreshIndexAndManifest(outputRoot, manifest);

            EditorUtility.DisplayDialog(DocSnapConstants.ToolName, "Exported folder \"" + folderPath + "\" (" + fileCount + " files).", "OK");
            RevealOutput(outputRoot);
        }

        // ==========================================
        // ExportFullProject
        // Exports every Scene plus the entire Assets
        // folder in a single consistent pass, so every
        // page's sidebar and every cross-link is fresh
        // at the same moment.
        // ==========================================
        public static void ExportFullProject()
        {
            string outputRoot = PrepareOutput();
            ManifestState manifest = DocSnapManifest.Load();

            var scenePages = new List<KeyValuePair<string, JsonValue>>();
            foreach (string scenePath in FindAllScenePaths())
            {
                JsonValue sceneData;
                int goCount;
                try
                {
                    sceneData = SceneHierarchyExporter.ExportScene(scenePath, out goCount);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Unity DocSnap] Skipped scene " + scenePath + ": " + ex.Message);
                    continue;
                }

                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                string htmlFile = DocSnapConstants.ScenesSubFolder + "/" + sceneName + ".html";
                string jsonFile = DocSnapConstants.DataSubFolder + "/scene_" + sceneName + ".json";
                WriteText(outputRoot, jsonFile, sceneData.ToString());

                DocSnapManifest.UpsertScene(manifest, new ManifestSceneEntry
                {
                    sceneName = sceneName,
                    scenePath = scenePath,
                    htmlFile = htmlFile,
                    jsonFile = jsonFile,
                    exportedUtc = sceneData.Get("exportedUtc").AsString(""),
                    gameObjectCount = goCount
                });
                scenePages.Add(new KeyValuePair<string, JsonValue>(htmlFile, sceneData));
            }

            string rootFolderKey = AssetProjectExporter.FolderKey("Assets");
            List<ManifestAssetIndexEntry> indexEntries;
            int fileCount;
            JsonValue folderData = AssetProjectExporter.ExportFolder("Assets", rootFolderKey, out indexEntries, out fileCount);
            string assetHtmlFile = DocSnapConstants.AssetsSubFolder + "/" + rootFolderKey + ".html";
            string assetJsonFile = DocSnapConstants.DataSubFolder + "/assets_" + rootFolderKey + ".json";
            WriteText(outputRoot, assetJsonFile, folderData.ToString());

            DocSnapManifest.ReplaceAssetIndexForFolder(manifest, rootFolderKey, indexEntries);
            DocSnapManifest.UpsertFolder(manifest, new ManifestFolderEntry
            {
                folderPath = "Assets",
                folderKey = rootFolderKey,
                htmlFile = assetHtmlFile,
                jsonFile = assetJsonFile,
                exportedUtc = folderData.Get("exportedUtc").AsString(""),
                fileCount = fileCount
            });
            DocSnapManifest.Save(manifest);

            // Render every page now that the manifest (and therefore every
            // sidebar + cross-link) reflects this complete pass.
            foreach (KeyValuePair<string, JsonValue> page in scenePages)
            {
                WriteText(outputRoot, page.Key, ScenePageRenderer.Render(page.Value, manifest, page.Key));
            }
            WriteText(outputRoot, assetHtmlFile, AssetPageRenderer.Render(folderData, manifest, assetHtmlFile));
            RefreshIndexAndManifest(outputRoot, manifest);

            EditorUtility.DisplayDialog(DocSnapConstants.ToolName,
                "Exported full project: " + scenePages.Count + " scene(s), " + fileCount + " file(s).", "OK");
            RevealOutput(outputRoot);
        }

        // ==========================================
        // FindAllScenePaths
        // Every .unity Scene asset under Assets/,
        // sorted by name. Scoped to Assets only so a
        // Scene bundled inside another installed
        // package (Packages/…, which Unity treats as
        // read-only) never gets listed here - opening
        // one throws "not allowed to open a scene in
        // a read-only package".
        // ==========================================
        public static List<string> FindAllScenePaths()
        {
            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
            paths.Sort(StringComparer.OrdinalIgnoreCase);
            return paths;
        }

        // ==========================================
        // RefreshIndexAndManifest
        // Regenerates index.html and data/manifest.json
        // from the current manifest state - cheap
        // enough to do on every single export.
        // ==========================================
        private static void RefreshIndexAndManifest(string outputRoot, ManifestState manifest)
        {
            WriteText(outputRoot, DocSnapConstants.IndexFileName, IndexPageRenderer.Render(manifest));
            DocSnapManifest.WritePublicJson(manifest, Path.Combine(outputRoot, DocSnapConstants.DataSubFolder, DocSnapConstants.ManifestFileName));
        }

        // ==========================================
        // PrepareOutput
        // Ensures the output folder tree and the
        // (version-pinned) shared CSS/JS assets exist.
        // ==========================================
        private static string PrepareOutput()
        {
            string outputRoot = DocSnapSettings.ResolveOutputRootAbsolute();
            Directory.CreateDirectory(Path.Combine(outputRoot, DocSnapConstants.ScenesSubFolder));
            Directory.CreateDirectory(Path.Combine(outputRoot, DocSnapConstants.AssetsSubFolder));
            Directory.CreateDirectory(Path.Combine(outputRoot, DocSnapConstants.DataSubFolder));
            Directory.CreateDirectory(Path.Combine(outputRoot, DocSnapConstants.SiteAssetsSubFolder));

            File.WriteAllText(Path.Combine(outputRoot, DocSnapConstants.SiteAssetsSubFolder, DocSnapConstants.StyleFileName), DocSnapSiteAssets.StyleCss);
            File.WriteAllText(Path.Combine(outputRoot, DocSnapConstants.SiteAssetsSubFolder, DocSnapConstants.ScriptFileName), DocSnapSiteAssets.AppJs);

            return outputRoot;
        }

        private static void WriteText(string outputRoot, string relativeFile, string content)
        {
            string fullPath = Path.Combine(outputRoot, relativeFile.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, content);
        }

        // ==========================================
        // RevealOutput
        // Opens the output folder in the OS file
        // browser so the freshly written site is one
        // click away from "index.html".
        // ==========================================
        public static void RevealOutput(string outputRoot)
        {
            EditorUtility.RevealInFinder(Path.Combine(outputRoot, DocSnapConstants.IndexFileName));
        }
    }
}
