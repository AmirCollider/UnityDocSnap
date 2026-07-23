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
using AmirCollider.UnityDocSnap.Editor.Summary;
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
            string jsonFile = DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.SceneJsonPrefix + sceneName + ".json";

            WriteText(outputRoot, jsonFile, sceneData.ToString());
            WriteText(outputRoot, DocSnapSummaryWriter.SummaryRelative(htmlFile), DocSnapSummaryWriter.RenderScene(sceneData));

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

            ShowExportComplete(outputRoot,
                "Exported scene \"" + sceneName + "\" (" + goCount + " GameObjects).",
                "シーン「" + sceneName + "」をエクスポートしました(GameObject " + goCount + "個)。",
                "سین «" + sceneName + "» اکسپورت شد (" + goCount + " گیم‌آبجکت).");
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
            JsonValue folderData;
            try
            {
                folderData = AssetProjectExporter.ExportFolder(folderPath, folderKey, out indexEntries, out fileCount, false, null, outputRoot, ReportAssetProgress);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            string htmlFile = DocSnapConstants.AssetsSubFolder + "/" + folderKey + ".html";
            string jsonFile = DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.FolderJsonPrefix + folderKey + ".json";

            WriteText(outputRoot, jsonFile, folderData.ToString());
            WriteText(outputRoot, DocSnapSummaryWriter.SummaryRelative(htmlFile), DocSnapSummaryWriter.RenderFolder(folderData));

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

            ShowExportComplete(outputRoot,
                "Exported folder \"" + folderPath + "\" (" + fileCount + " files).",
                "フォルダ「" + folderPath + "」をエクスポートしました(" + fileCount + "ファイル)。",
                "پوشه‌ی «" + folderPath + "» اکسپورت شد (" + fileCount + " فایل).");
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
                string jsonFile = DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.SceneJsonPrefix + sceneName + ".json";
                WriteText(outputRoot, jsonFile, sceneData.ToString());
                WriteText(outputRoot, DocSnapSummaryWriter.SummaryRelative(htmlFile), DocSnapSummaryWriter.RenderScene(sceneData));

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
            JsonValue folderData;
            try
            {
                folderData = AssetProjectExporter.ExportFolder("Assets", rootFolderKey, out indexEntries, out fileCount, false, null, outputRoot, ReportAssetProgress);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            string assetHtmlFile = DocSnapConstants.AssetsSubFolder + "/" + rootFolderKey + ".html";
            string assetJsonFile = DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.FolderJsonPrefix + rootFolderKey + ".json";
            WriteText(outputRoot, assetJsonFile, folderData.ToString());
            WriteText(outputRoot, DocSnapSummaryWriter.SummaryRelative(assetHtmlFile), DocSnapSummaryWriter.RenderFolder(folderData));

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

            ShowExportComplete(outputRoot,
                "Exported full project: " + scenePages.Count + " scene(s), " + fileCount + " file(s).",
                "プロジェクト全体をエクスポートしました:シーン" + scenePages.Count + "件、ファイル" + fileCount + "件。",
                "کل پروژه اکسپورت شد: " + scenePages.Count + " سین، " + fileCount + " فایل.");
        }

        // ==========================================
        // ExportFullProjectWithFiles
        // Same pass as ExportFullProject, but also
        // mirrors every referenced asset's actual file
        // bytes (plus its .meta file, when present)
        // into the output's files/ folder. The default
        // exports above stay metadata-only; this is an
        // explicit, separately-named opt-in for anyone
        // who also wants a portable copy of the
        // underlying content next to the site.
        // ==========================================
        public static void ExportFullProjectWithFiles()
        {
            string outputRoot = PrepareOutput();
            string physicalFilesRoot = Path.Combine(outputRoot, DocSnapConstants.FilesSubFolder);
            Directory.CreateDirectory(physicalFilesRoot);

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
                string jsonFile = DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.SceneJsonPrefix + sceneName + ".json";
                WriteText(outputRoot, jsonFile, sceneData.ToString());
                WriteText(outputRoot, DocSnapSummaryWriter.SummaryRelative(htmlFile), DocSnapSummaryWriter.RenderScene(sceneData));

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

                AssetProjectExporter.CopyPhysicalFile(scenePath, physicalFilesRoot);
            }

            string rootFolderKey = AssetProjectExporter.FolderKey("Assets");
            List<ManifestAssetIndexEntry> indexEntries;
            int fileCount;
            JsonValue folderData;
            try
            {
                folderData = AssetProjectExporter.ExportFolder("Assets", rootFolderKey, out indexEntries, out fileCount, true, physicalFilesRoot, outputRoot, ReportAssetProgress);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            string assetHtmlFile = DocSnapConstants.AssetsSubFolder + "/" + rootFolderKey + ".html";
            string assetJsonFile = DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.FolderJsonPrefix + rootFolderKey + ".json";
            WriteText(outputRoot, assetJsonFile, folderData.ToString());
            WriteText(outputRoot, DocSnapSummaryWriter.SummaryRelative(assetHtmlFile), DocSnapSummaryWriter.RenderFolder(folderData));

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

            foreach (KeyValuePair<string, JsonValue> page in scenePages)
            {
                WriteText(outputRoot, page.Key, ScenePageRenderer.Render(page.Value, manifest, page.Key));
            }
            WriteText(outputRoot, assetHtmlFile, AssetPageRenderer.Render(folderData, manifest, assetHtmlFile));
            RefreshIndexAndManifest(outputRoot, manifest);

            ShowExportComplete(outputRoot,
                "Exported full project with files: " + scenePages.Count + " scene(s), " + fileCount + " file(s) (assets copied to \"" + DocSnapConstants.FilesSubFolder + "/\").",
                "ファイル付きでプロジェクト全体をエクスポートしました:シーン" + scenePages.Count + "件、ファイル" + fileCount + "件(アセットは\u201cfiles/\u201dにコピー済み)。",
                "کل پروژه به‌همراه فایل‌ها اکسپورت شد: " + scenePages.Count + " سین، " + fileCount + " فایل (فایل‌ها توی «" + DocSnapConstants.FilesSubFolder + "/» کپی شدن).");
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
            WriteText(outputRoot, DocSnapConstants.ProjectSummaryFileName, DocSnapSummaryWriter.RenderProjectIndex(manifest));
            DocSnapManifest.WritePublicJson(manifest, Path.Combine(outputRoot, DocSnapConstants.DataSubFolder, DocSnapConstants.ManifestFileName));
            PruneStaleOutput(outputRoot, manifest);
        }

        // ==========================================
        // ReportAssetProgress
        // Drives the Editor progress bar during an
        // asset pass. A full-project export walks every
        // file in the project and previously gave zero
        // feedback, so Unity simply looked frozen.
        // ==========================================
        private static void ReportAssetProgress(int processed, int total, string currentPath)
        {
            if (total <= 0) { return; }
            EditorUtility.DisplayProgressBar(
                DocSnapConstants.ToolName,
                "Exporting assets  " + processed + " / " + total + "\n" + currentPath,
                (float)processed / total);
        }

        // ==========================================
        // PruneStaleOutput
        // Deletes scene/asset pages - and their .md
        // summaries - whose source no longer appears in
        // the manifest. Without this a renamed or deleted
        // Scene left its old page behind forever, and the
        // sidebar linked to a document that no longer
        // described anything in the project.
        // ==========================================
        private static void PruneStaleOutput(string outputRoot, ManifestState manifest)
        {
            try
            {
                var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (ManifestSceneEntry scene in manifest.scenes)
                {
                    live.Add(scene.htmlFile);
                    live.Add(DocSnapSummaryWriter.SummaryRelative(scene.htmlFile));
                }
                foreach (ManifestFolderEntry folder in manifest.assetFolders)
                {
                    live.Add(folder.htmlFile);
                    live.Add(DocSnapSummaryWriter.SummaryRelative(folder.htmlFile));
                }

                PruneFolder(outputRoot, DocSnapConstants.ScenesSubFolder, live);
                PruneFolder(outputRoot, DocSnapConstants.AssetsSubFolder, live);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Unity DocSnap] Could not prune stale output: " + ex.Message);
            }
        }

        private static void PruneFolder(string outputRoot, string subFolder, HashSet<string> liveFiles)
        {
            string absolute = Path.Combine(outputRoot, subFolder);
            if (!Directory.Exists(absolute)) { return; }

            foreach (string pattern in new[] { "*.html", "*" + DocSnapConstants.SummaryFileExtension })
            {
                foreach (string file in Directory.GetFiles(absolute, pattern, SearchOption.TopDirectoryOnly))
                {
                    string relative = subFolder + "/" + Path.GetFileName(file);
                    if (!liveFiles.Contains(relative)) { File.Delete(file); }
                }
            }
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
            Directory.CreateDirectory(Path.Combine(Path.Combine(outputRoot, DocSnapConstants.SiteAssetsSubFolder), DocSnapConstants.ThumbsSubFolder));

            File.WriteAllText(Path.Combine(outputRoot, DocSnapConstants.SiteAssetsSubFolder, DocSnapConstants.StyleFileName), DocSnapSiteAssets.StyleCss);
            File.WriteAllText(Path.Combine(outputRoot, DocSnapConstants.SiteAssetsSubFolder, DocSnapConstants.ScriptFileName), DocSnapSiteAssets.AppJs);

            // Remove the standalone "success" page older versions
            // opened in a browser after every export - the export
            // now confirms with an in-Editor popup instead.
            TryDelete(Path.Combine(outputRoot, "export_complete.html"));

            return outputRoot;
        }

        private static void TryDelete(string absolutePath)
        {
            try { if (File.Exists(absolutePath)) { File.Delete(absolutePath); } }
            catch { /* best-effort cleanup only */ }
        }

        private static void WriteText(string outputRoot, string relativeFile, string content)
        {
            string fullPath = Path.Combine(outputRoot, relativeFile.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, content);
        }

        // ==========================================
        // ShowExportComplete
        // A single in-Editor confirmation popup for a
        // successful export - no browser tab, no external
        // "success" web page. The message is trilingual
        // (EN/JA/FA) so it stays friendly, and the primary
        // button reveals the output folder for anyone who
        // wants to open index.html straight away. Failures
        // still use their own EditorUtility.DisplayDialog,
        // so a real problem still looks like one.
        // ==========================================
        private static void ShowExportComplete(string outputRoot, string messageEn, string messageJa, string messageFa)
        {
            string message =
                messageEn + "\n\n" +
                messageJa + "\n\n" +
                messageFa + "\n\n" +
                "index.html \u2192 full site   \u00B7   summary.md \u2192 simple / AI-friendly";

            bool reveal = EditorUtility.DisplayDialog(
                DocSnapConstants.ToolName + "  \u2705",
                message,
                "Open Output Folder",
                "Close");

            if (reveal) { RevealOutput(outputRoot); }
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
