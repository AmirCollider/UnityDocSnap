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
using System.Text;
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

            ShowSuccessPage(outputRoot,
                "Exported scene \"" + sceneName + "\" (" + goCount + " GameObjects).",
                "シーン「" + sceneName + "」をエクスポートしました(GameObject " + goCount + "個)。",
                "سین «" + sceneName + "» اکسپورت شد (" + goCount + " گیم‌آبجکت).");
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

            ShowSuccessPage(outputRoot,
                "Exported folder \"" + folderPath + "\" (" + fileCount + " files).",
                "フォルダ「" + folderPath + "」をエクスポートしました(" + fileCount + "ファイル)。",
                "پوشه‌ی «" + folderPath + "» اکسپورت شد (" + fileCount + " فایل).");
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

            ShowSuccessPage(outputRoot,
                "Exported full project: " + scenePages.Count + " scene(s), " + fileCount + " file(s).",
                "プロジェクト全体をエクスポートしました:シーン" + scenePages.Count + "件、ファイル" + fileCount + "件。",
                "کل پروژه اکسپورت شد: " + scenePages.Count + " سین، " + fileCount + " فایل.");
            RevealOutput(outputRoot);
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

                AssetProjectExporter.CopyPhysicalFile(scenePath, physicalFilesRoot);
            }

            string rootFolderKey = AssetProjectExporter.FolderKey("Assets");
            List<ManifestAssetIndexEntry> indexEntries;
            int fileCount;
            JsonValue folderData = AssetProjectExporter.ExportFolder("Assets", rootFolderKey, out indexEntries, out fileCount, true, physicalFilesRoot);
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

            foreach (KeyValuePair<string, JsonValue> page in scenePages)
            {
                WriteText(outputRoot, page.Key, ScenePageRenderer.Render(page.Value, manifest, page.Key));
            }
            WriteText(outputRoot, assetHtmlFile, AssetPageRenderer.Render(folderData, manifest, assetHtmlFile));
            RefreshIndexAndManifest(outputRoot, manifest);

            ShowSuccessPage(outputRoot,
                "Exported full project with files: " + scenePages.Count + " scene(s), " + fileCount + " file(s) (assets copied to \"" + DocSnapConstants.FilesSubFolder + "/\").",
                "ファイル付きでプロジェクト全体をエクスポートしました:シーン" + scenePages.Count + "件、ファイル" + fileCount + "件(アセットは\u201cfiles/\u201dにコピー済み)。",
                "کل پروژه به‌همراه فایل‌ها اکسپورت شد: " + scenePages.Count + " سین، " + fileCount + " فایل (فایل‌ها توی «files/» کپی شدن).");
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
        // ShowSuccessPage
        // Writes a small, on-brand confirmation page
        // into the output folder and opens it in the
        // system browser - a friendlier, trilingual
        // (EN/JA/FA) alternative to a plain native OK
        // dialog for a *successful* export, reusing the
        // site's own assets_ui/style.css so it always
        // matches the exported site's look. Failures
        // still use EditorUtility.DisplayDialog, since a
        // native, unmissable system dialog is the right
        // tone for something going wrong.
        // ==========================================
        private static void ShowSuccessPage(string outputRoot, string messageEn, string messageJa, string messageFa)
        {
            var sb = new StringBuilder(1024);
            sb.Append("<!doctype html>\n<html lang=\"en\" dir=\"ltr\">\n<head>\n<meta charset=\"utf-8\">\n");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
            sb.Append("<title>").Append(DocSnapConstants.ToolName).Append(" - Export Complete</title>\n");
            sb.Append("<link rel=\"stylesheet\" href=\"assets_ui/style.css\">\n</head>\n<body style=\"background:var(--cream);\">\n");
            sb.Append("<div style=\"max-width:560px;margin:60px auto;padding:0 20px;\">\n");
            sb.Append("<div class=\"ds-card\" style=\"text-align:center;border-top:5px solid var(--mint-strong);\">\n");
            sb.Append("<div style=\"font-size:52px;line-height:1;margin-bottom:6px;\">\u2705</div>\n");
            sb.Append("<h1 style=\"font-size:22px;margin-bottom:18px;\">Export Complete! \u2728</h1>\n");
            sb.Append("<p style=\"font-size:14px;margin:10px 0;\">").Append(HtmlPageBuilder.Escape(messageEn)).Append("</p>\n");
            sb.Append("<p style=\"font-size:14px;margin:10px 0;\">").Append(HtmlPageBuilder.Escape(messageJa)).Append("</p>\n");
            sb.Append("<p style=\"font-size:14px;margin:10px 0;\" dir=\"rtl\">").Append(HtmlPageBuilder.Escape(messageFa)).Append("</p>\n");
            sb.Append("<div class=\"ds-badge mint\" style=\"margin-top:14px;\">\uD83C\uDF70 ").Append(DocSnapConstants.ToolName).Append(" v").Append(DocSnapConstants.Version).Append("</div>\n");
            sb.Append("</div>\n</div>\n</body>\n</html>\n");

            string resultPath = Path.Combine(outputRoot, "export_complete.html");
            File.WriteAllText(resultPath, sb.ToString());
            Application.OpenURL(new Uri(resultPath).AbsoluteUri);
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
