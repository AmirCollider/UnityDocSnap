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
            WriteSceneSummaries(outputRoot, sceneName, sceneData);

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
            WriteFolderSummaries(outputRoot, folderKey, folderData);

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
                WriteSceneSummaries(outputRoot, sceneName, sceneData);

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
            WriteFolderSummaries(outputRoot, rootFolderKey, folderData);

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
                WriteSceneSummaries(outputRoot, sceneName, sceneData);

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
            WriteFolderSummaries(outputRoot, rootFolderKey, folderData);

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
        // Keeps each managed output folder holding exactly
        // the files the current manifest describes, and
        // nothing else. Any file it does not recognise is
        // deleted, which cleans up in one sweep: pages and
        // summaries for a Scene/folder that was renamed or
        // removed, AND files left behind under an older
        // version's naming (e.g. data/scene_*.json before
        // the scene-*.json rename, or a Scene's .md when
        // summaries still lived beside the HTML). The
        // manifest lists every Scene/folder ever exported
        // in this project, so a single-item export never
        // deletes another item's still-valid output.
        // ==========================================
        private static void PruneStaleOutput(string outputRoot, ManifestState manifest)
        {
            try
            {
                var liveScenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var liveFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var liveData = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var liveSummary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                liveData.Add(DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.ManifestFileName);

                foreach (ManifestSceneEntry scene in manifest.scenes)
                {
                    liveScenes.Add(scene.htmlFile);
                    liveData.Add(scene.jsonFile);
                    liveSummary.Add(DocSnapSummaryWriter.SceneSummaryMarkdown(scene.sceneName));
                    liveSummary.Add(DocSnapSummaryWriter.SceneSummaryJson(scene.sceneName));
                }
                foreach (ManifestFolderEntry folder in manifest.assetFolders)
                {
                    liveFolders.Add(folder.htmlFile);
                    liveData.Add(folder.jsonFile);
                    liveSummary.Add(DocSnapSummaryWriter.FolderSummaryMarkdown(folder.folderKey));
                    liveSummary.Add(DocSnapSummaryWriter.FolderSummaryJson(folder.folderKey));
                }

                PruneDir(outputRoot, DocSnapConstants.ScenesSubFolder, liveScenes);
                PruneDir(outputRoot, DocSnapConstants.AssetsSubFolder, liveFolders);
                PruneDir(outputRoot, DocSnapConstants.DataSubFolder, liveData);
                PruneDir(outputRoot, DocSnapConstants.SummarySubFolder, liveSummary);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Unity DocSnap] Could not prune stale output: " + ex.Message);
            }
        }

        // ==========================================
        // PruneDir
        // Deletes every top-level file in one managed
        // output folder that is not in that folder's live
        // set. Only DocSnap-owned folders are ever passed
        // here, and each is rewritten in full on every
        // export, so an unrecognised file is always a
        // stale leftover.
        // ==========================================
        private static void PruneDir(string outputRoot, string subFolder, HashSet<string> liveFiles)
        {
            string absolute = Path.Combine(outputRoot, subFolder);
            if (!Directory.Exists(absolute)) { return; }

            foreach (string file in Directory.GetFiles(absolute, "*", SearchOption.TopDirectoryOnly))
            {
                string relative = subFolder + "/" + Path.GetFileName(file);
                if (!liveFiles.Contains(relative)) { File.Delete(file); }
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
            Directory.CreateDirectory(Path.Combine(outputRoot, DocSnapConstants.SummarySubFolder));
            Directory.CreateDirectory(Path.Combine(outputRoot, DocSnapConstants.SiteAssetsSubFolder));
            Directory.CreateDirectory(Path.Combine(Path.Combine(outputRoot, DocSnapConstants.SiteAssetsSubFolder), DocSnapConstants.ThumbsSubFolder));

            File.WriteAllText(Path.Combine(outputRoot, DocSnapConstants.SiteAssetsSubFolder, DocSnapConstants.StyleFileName), DocSnapSiteAssets.StyleCss);
            File.WriteAllText(Path.Combine(outputRoot, DocSnapConstants.SiteAssetsSubFolder, DocSnapConstants.ScriptFileName), DocSnapSiteAssets.AppJs);

            CleanLegacyOutput(outputRoot);
            return outputRoot;
        }

        // ==========================================
        // WriteSceneSummaries / WriteFolderSummaries
        // Every export writes the "simple" summary of a
        // Scene / folder in both forms - readable Markdown
        // and structured JSON - into the summary/ folder.
        // ==========================================
        private static void WriteSceneSummaries(string outputRoot, string sceneName, JsonValue sceneData)
        {
            WriteText(outputRoot, DocSnapSummaryWriter.SceneSummaryMarkdown(sceneName), DocSnapSummaryWriter.RenderScene(sceneData));
            WriteText(outputRoot, DocSnapSummaryWriter.SceneSummaryJson(sceneName), DocSnapSummaryWriter.RenderSceneJson(sceneData));
        }

        private static void WriteFolderSummaries(string outputRoot, string folderKey, JsonValue folderData)
        {
            WriteText(outputRoot, DocSnapSummaryWriter.FolderSummaryMarkdown(folderKey), DocSnapSummaryWriter.RenderFolder(folderData));
            WriteText(outputRoot, DocSnapSummaryWriter.FolderSummaryJson(folderKey), DocSnapSummaryWriter.RenderFolderJson(folderData));
        }

        // ==========================================
        // CleanLegacyOutput
        // Removes artefacts from older Unity DocSnap
        // versions that current exports no longer produce
        // and which PruneStaleOutput does not cover:
        // the browser "success" page, and the pre-rename
        // sibling folders (assets/ -> folders/,
        // assets_ui/ -> theme/, files/ -> source-files/).
        // Their names are unused by the current tool, so
        // deleting them only clears stale output.
        // ==========================================
        private static void CleanLegacyOutput(string outputRoot)
        {
            TryDelete(Path.Combine(outputRoot, "export_complete.html"));
            foreach (string legacyDir in new[] { "assets", "assets_ui", "files" })
            {
                TryDeleteDir(Path.Combine(outputRoot, legacyDir));
            }
        }

        private static void TryDelete(string absolutePath)
        {
            try { if (File.Exists(absolutePath)) { File.Delete(absolutePath); } }
            catch { /* best-effort cleanup only */ }
        }

        private static void TryDeleteDir(string absolutePath)
        {
            try { if (Directory.Exists(absolutePath)) { Directory.Delete(absolutePath, true); } }
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
