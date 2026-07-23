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
using AmirCollider.UnityDocSnap.Editor.Packages;
using AmirCollider.UnityDocSnap.Editor.SceneExport;
using AmirCollider.UnityDocSnap.Editor.Search;
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
                gameObjectCount = goCount,
                sourceSignature = SceneSignature(scenePath)
            });
            DocSnapManifest.ReplaceSearchRecordsForScope(manifest, sceneName, DocSnapSearchIndex.BuildSceneRecords(sceneData, sceneName, htmlFile));
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
                fileCount = fileCount,
                sourceSignature = FolderSignature(folderPath)
            });
            DocSnapManifest.ReplaceSearchRecordsForScope(manifest, folderKey, DocSnapSearchIndex.BuildFolderRecords(folderData, folderKey, htmlFile));
            DocSnapManifest.Save(manifest);

            WriteText(outputRoot, htmlFile, AssetPageRenderer.Render(folderData, manifest, htmlFile));
            RefreshIndexAndManifest(outputRoot, manifest);

            ShowExportComplete(outputRoot,
                "Exported folder \"" + folderPath + "\" (" + fileCount + " files).",
                "フォルダ「" + folderPath + "」をエクスポートしました(" + fileCount + "ファイル)。",
                "پوشه‌ی «" + folderPath + "» اکسپورت شد (" + fileCount + " فایل).");
        }

        // ==========================================
        // ExportFullProject / ExportFullProjectWithFiles
        // / UpdatePreviousExport
        // Three thin entry points over one shared pass
        // (ExportProject). "With Files" also mirrors the
        // real asset bytes into source-files/; "Update"
        // reuses any Scene/folder whose source has not
        // changed since the last export instead of
        // re-scanning it. Previously the first two were
        // ~80% identical copy-paste, which is exactly how
        // one path silently drifts from the other.
        // ==========================================
        public static void ExportFullProject()
        {
            ExportProject(false, false);
        }

        public static void ExportFullProjectWithFiles()
        {
            ExportProject(true, false);
        }

        public static void UpdatePreviousExport()
        {
            ExportProject(false, true);
        }

        // ==========================================
        // ExportProject
        // The single implementation behind all three
        // full-project actions.
        //   copyFiles   - also copy real asset bytes into
        //                 source-files/ (the "With Files"
        //                 opt-in; DocSnap is metadata-only
        //                 otherwise).
        //   incremental - reuse a Scene's / the Assets
        //                 folder's existing output when a
        //                 cheap source fingerprint shows
        //                 nothing changed, instead of
        //                 re-opening the Scene or re-reading
        //                 every asset. The heavy work is
        //                 skipped; the still-current data
        //                 JSON is parsed back and every page
        //                 is re-rendered cheaply so sidebars
        //                 and cross-links stay consistent.
        // ==========================================
        private static void ExportProject(bool copyFiles, bool incremental)
        {
            string outputRoot = PrepareOutput();
            string physicalFilesRoot = null;
            if (copyFiles)
            {
                physicalFilesRoot = Path.Combine(outputRoot, DocSnapConstants.FilesSubFolder);
                Directory.CreateDirectory(physicalFilesRoot);
            }

            ManifestState manifest = DocSnapManifest.Load();

            int reusedScenes = 0;
            int exportedScenes = 0;
            var scenePages = new List<KeyValuePair<string, JsonValue>>();

            foreach (string scenePath in FindAllScenePaths())
            {
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                string htmlFile = DocSnapConstants.ScenesSubFolder + "/" + sceneName + ".html";
                string jsonFile = DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.SceneJsonPrefix + sceneName + ".json";
                string signature = SceneSignature(scenePath);

                ManifestSceneEntry prior = DocSnapManifest.FindScene(manifest, scenePath);
                JsonValue sceneData = null;

                if (incremental && CanReuse(outputRoot, prior != null ? prior.sourceSignature : null, signature, jsonFile, htmlFile))
                {
                    sceneData = TryLoadJson(outputRoot, jsonFile);
                }

                if (sceneData == null)
                {
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
                    WriteText(outputRoot, jsonFile, sceneData.ToString());
                    WriteSceneSummaries(outputRoot, sceneName, sceneData);
                    if (copyFiles) { AssetProjectExporter.CopyPhysicalFile(scenePath, physicalFilesRoot); }
                    exportedScenes++;
                }
                else
                {
                    reusedScenes++;
                }

                DocSnapManifest.UpsertScene(manifest, new ManifestSceneEntry
                {
                    sceneName = sceneName,
                    scenePath = scenePath,
                    htmlFile = htmlFile,
                    jsonFile = jsonFile,
                    exportedUtc = sceneData.Get("exportedUtc").AsString(""),
                    gameObjectCount = (int)sceneData.Get("totalGameObjects").AsNumber(),
                    sourceSignature = signature
                });
                DocSnapManifest.ReplaceSearchRecordsForScope(manifest, sceneName, DocSnapSearchIndex.BuildSceneRecords(sceneData, sceneName, htmlFile));
                scenePages.Add(new KeyValuePair<string, JsonValue>(htmlFile, sceneData));
            }

            // ----- Assets folder pass -----
            string rootFolderKey = AssetProjectExporter.FolderKey("Assets");
            string assetHtmlFile = DocSnapConstants.AssetsSubFolder + "/" + rootFolderKey + ".html";
            string assetJsonFile = DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.FolderJsonPrefix + rootFolderKey + ".json";
            string folderSignature = FolderSignature("Assets");

            ManifestFolderEntry priorFolder = DocSnapManifest.FindFolder(manifest, rootFolderKey);
            JsonValue folderData = null;
            int fileCount = 0;
            bool reusedAssets = false;

            // The asset pass is only reused when files were NOT requested:
            // a with-files export must always copy the real bytes.
            if (incremental && !copyFiles && CanReuse(outputRoot, priorFolder != null ? priorFolder.sourceSignature : null, folderSignature, assetJsonFile, assetHtmlFile))
            {
                folderData = TryLoadJson(outputRoot, assetJsonFile);
                if (folderData != null)
                {
                    fileCount = (int)folderData.Get("fileCount").AsNumber();
                    reusedAssets = true;
                }
            }

            if (folderData == null)
            {
                List<ManifestAssetIndexEntry> indexEntries;
                try
                {
                    folderData = AssetProjectExporter.ExportFolder("Assets", rootFolderKey, out indexEntries, out fileCount, copyFiles, physicalFilesRoot, outputRoot, ReportAssetProgress);
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
                WriteText(outputRoot, assetJsonFile, folderData.ToString());
                WriteFolderSummaries(outputRoot, rootFolderKey, folderData);
                DocSnapManifest.ReplaceAssetIndexForFolder(manifest, rootFolderKey, indexEntries);
            }
            // When reused, the asset->page cross-link index for this folder
            // is already in the loaded manifest, so it is deliberately left
            // untouched here.

            DocSnapManifest.UpsertFolder(manifest, new ManifestFolderEntry
            {
                folderPath = "Assets",
                folderKey = rootFolderKey,
                htmlFile = assetHtmlFile,
                jsonFile = assetJsonFile,
                exportedUtc = folderData.Get("exportedUtc").AsString(""),
                fileCount = fileCount,
                sourceSignature = folderSignature
            });
            DocSnapManifest.ReplaceSearchRecordsForScope(manifest, rootFolderKey, DocSnapSearchIndex.BuildFolderRecords(folderData, rootFolderKey, assetHtmlFile));

            // Packages are project-global; refreshed on every full pass.
            DocSnapManifest.SetPackages(manifest, DocSnapPackagesReader.ReadInstalledPackages());

            DocSnapManifest.Save(manifest);

            // Render every page now that the manifest (and therefore every
            // sidebar + cross-link) reflects this complete pass.
            foreach (KeyValuePair<string, JsonValue> page in scenePages)
            {
                WriteText(outputRoot, page.Key, ScenePageRenderer.Render(page.Value, manifest, page.Key));
            }
            WriteText(outputRoot, assetHtmlFile, AssetPageRenderer.Render(folderData, manifest, assetHtmlFile));
            RefreshIndexAndManifest(outputRoot, manifest);

            string reuseNote = incremental
                ? "  (" + reusedScenes + " scene(s) reused, " + exportedScenes + " re-scanned" + (reusedAssets ? ", assets reused" : ", assets re-scanned") + ")"
                : "";
            string filesNoteEn = copyFiles ? " (assets copied to \"" + DocSnapConstants.FilesSubFolder + "/\")" : "";
            string filesNoteFa = copyFiles ? " (فایل‌ها توی «" + DocSnapConstants.FilesSubFolder + "/» کپی شدن)" : "";
            string headEn = incremental ? "Updated previous export" : (copyFiles ? "Exported full project with files" : "Exported full project");
            string headJa = incremental ? "前回のエクスポートを更新しました" : "プロジェクト全体をエクスポートしました";
            string headFa = incremental ? "خروجی قبلی بروزرسانی شد" : "کل پروژه اکسپورت شد";

            ShowExportComplete(outputRoot,
                headEn + ": " + scenePages.Count + " scene(s), " + fileCount + " file(s)" + filesNoteEn + "." + reuseNote,
                headJa + ":シーン" + scenePages.Count + "件、ファイル" + fileCount + "件" + (copyFiles ? "(アセットは source-files/ にコピー済み)" : "") + "。",
                headFa + ": " + scenePages.Count + " سین، " + fileCount + " فایل" + filesNoteFa + ".");
        }

        // ==========================================
        // SceneSignature / FolderSignature
        // Cheap "did the source change?" fingerprints for
        // the incremental update. A Scene's is its .unity
        // file's size + last-write time. A folder's is the
        // file count plus the newest last-write time across
        // every file under it (asset AND .meta, so an
        // import-setting change is caught too). Both are
        // pure filesystem stats - no asset loads, no Scene
        // opens - so computing them is negligible next to
        // the work they let an update skip.
        // ==========================================
        private static string SceneSignature(string scenePath)
        {
            try
            {
                string abs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", scenePath));
                var fi = new FileInfo(abs);
                return fi.Exists ? fi.Length + ":" + fi.LastWriteTimeUtc.Ticks : "";
            }
            catch { return ""; }
        }

        private static string FolderSignature(string folderPath)
        {
            try
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string absolute = Path.GetFullPath(Path.Combine(projectRoot, folderPath));
                if (!Directory.Exists(absolute)) { return ""; }

                long newest = 0;
                int count = 0;
                foreach (string file in Directory.GetFiles(absolute, "*", SearchOption.AllDirectories))
                {
                    string name = Path.GetFileName(file);
                    if (name.StartsWith(".", StringComparison.Ordinal)) { continue; }
                    count++;
                    long ticks = File.GetLastWriteTimeUtc(file).Ticks;
                    if (ticks > newest) { newest = ticks; }
                }
                return count + ":" + newest;
            }
            catch { return ""; }
        }

        // ==========================================
        // CanReuse / FileExists / TryLoadJson
        // The incremental-reuse primitives: a prior export
        // with a matching signature whose data JSON and HTML
        // are both still on disk can be reused by parsing the
        // JSON straight back into a JsonValue tree.
        // ==========================================
        private static bool CanReuse(string outputRoot, string priorSignature, string currentSignature, string jsonFile, string htmlFile)
        {
            return !string.IsNullOrEmpty(priorSignature)
                && priorSignature == currentSignature
                && FileExists(outputRoot, jsonFile)
                && FileExists(outputRoot, htmlFile);
        }

        private static bool FileExists(string outputRoot, string relativeFile)
        {
            return File.Exists(Path.Combine(outputRoot, relativeFile.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static JsonValue TryLoadJson(string outputRoot, string relativeFile)
        {
            try
            {
                string full = Path.Combine(outputRoot, relativeFile.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(full)) { return null; }
                JsonValue parsed;
                return JsonValue.TryParse(File.ReadAllText(full), out parsed) ? parsed : null;
            }
            catch { return null; }
        }

        // ==========================================
        // HasPreviousExport
        // Whether any prior export state exists, so the
        // menu can tell the user there is nothing to update
        // yet and offer a full export instead.
        // ==========================================
        public static bool HasPreviousExport()
        {
            ManifestState manifest = DocSnapManifest.Load();
            return manifest.scenes.Count > 0 || manifest.assetFolders.Count > 0;
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

            // The client-side search index (theme/search-index.js) is rebuilt
            // from the manifest's stored records on every export, so it always
            // reflects everything exported so far - not just this run's item.
            WriteText(outputRoot, DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.SearchIndexFileName, DocSnapSearchIndex.WriteSearchIndexJs(manifest));

            // The Packages page + its summary are only written once a
            // full-project pass has recorded package data; a single Scene /
            // folder export keeps whatever was recorded last.
            if (manifest.packages != null && manifest.packages.Count > 0)
            {
                WriteText(outputRoot, DocSnapConstants.PackagesFileName, PackagesPageRenderer.Render(manifest));
                WriteText(outputRoot, DocSnapSummaryWriter.PackagesSummaryMarkdown(), DocSnapSummaryWriter.RenderPackages(manifest));
                WriteText(outputRoot, DocSnapSummaryWriter.PackagesSummaryJson(), DocSnapSummaryWriter.RenderPackagesJson(manifest));
            }

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

                // The Packages summary (when present) lives in summary/ too;
                // keep it out of the prune sweep so a Scene/folder-only export
                // never deletes it.
                liveSummary.Add(DocSnapSummaryWriter.PackagesSummaryMarkdown());
                liveSummary.Add(DocSnapSummaryWriter.PackagesSummaryJson());

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
