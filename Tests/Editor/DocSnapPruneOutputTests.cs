// ==========================================
// DocSnapPruneOutputTests
// PruneStaleOutput is the only code in Unity
// DocSnap that deletes files, and the version
// folder it deletes them from is the artefact the
// user keeps. Everything else the tool gets wrong
// can be fixed by exporting again; this cannot.
//
// It was also, until these tests, the only part of
// the tool with no tests at all - which is how the
// fresh-manifest case below stayed a bug: a
// manifest that failed to load reads as "nothing
// has ever been exported", and the pruner's whole
// premise is that anything it does not recognise is
// stale.
// ==========================================
using System.IO;
using AmirCollider.UnityDocSnap.Editor.Export;
using AmirCollider.UnityDocSnap.Editor.Manifest;
using AmirCollider.UnityDocSnap.Editor.Summary;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class DocSnapPruneOutputTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "DocSnapPruneTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_root)) { Directory.Delete(_root, true); } }
            catch { /* a temp folder that outlives one test run is harmless */ }
        }

        // ==========================================
        // Helpers
        // ==========================================

        // The pruner refuses to touch a folder that does not carry
        // the stylesheet it writes itself, so a realistic fixture
        // has to include it.
        private void MakeVersionFolder()
        {
            Write(DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.StyleFileName, "/* css */");
        }

        private void Write(string relative, string content)
        {
            string full = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, content);
        }

        private bool Exists(string relative)
        {
            return File.Exists(Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static ManifestState ManifestWithScene(string sceneKey, bool loadedFromDisk)
        {
            var state = new ManifestState { loadedFromDisk = loadedFromDisk };
            state.scenes.Add(new ManifestSceneEntry
            {
                sceneName = sceneKey,
                sceneKey = sceneKey,
                scenePath = "Assets/" + sceneKey + ".unity",
                htmlFile = DocSnapConstants.ScenesSubFolder + "/" + sceneKey + ".html",
                jsonFile = DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.SceneJsonPrefix + sceneKey + ".json"
            });
            return state;
        }

        // Two Scenes on disk, but only one of them in the manifest.
        private void WriteTwoScenesOnDisk()
        {
            MakeVersionFolder();
            Write(DocSnapConstants.ScenesSubFolder + "/Kept.html", "<html></html>");
            Write(DocSnapConstants.ScenesSubFolder + "/Other.html", "<html></html>");
            Write(DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.SceneJsonPrefix + "Kept.json", "{}");
            Write(DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.SceneJsonPrefix + "Other.json", "{}");
            Write(DocSnapSummaryWriter.SceneSummaryMarkdown("Other"), "# Other");
        }

        // ==========================================
        // The data-loss case.
        //
        // The manifest lives in Library/, which is
        // machine-local, git-ignored and routinely deleted
        // outright to make Unity behave. Losing it must never
        // cost the user a previous export: a blank manifest
        // knows about nothing, so treating "not in the
        // manifest" as "stale" would delete everything.
        // ==========================================
        [Test]
        public void FreshManifest_DeletesNothing()
        {
            WriteTwoScenesOnDisk();

            DocSnapExportService.PruneStaleOutput(_root, ManifestWithScene("Kept", loadedFromDisk: false));

            Assert.IsTrue(Exists(DocSnapConstants.ScenesSubFolder + "/Kept.html"));
            Assert.IsTrue(Exists(DocSnapConstants.ScenesSubFolder + "/Other.html"),
                "A manifest that was never loaded must not authorise deleting another export's pages.");
            Assert.IsTrue(Exists(DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.SceneJsonPrefix + "Other.json"));
            Assert.IsTrue(Exists(DocSnapSummaryWriter.SceneSummaryMarkdown("Other")));
        }

        // The normal case still has to work, or the guard above
        // would just be a way of never cleaning up at all.
        [Test]
        public void LoadedManifest_RemovesOnlyUnlistedFiles()
        {
            WriteTwoScenesOnDisk();

            DocSnapExportService.PruneStaleOutput(_root, ManifestWithScene("Kept", loadedFromDisk: true));

            Assert.IsTrue(Exists(DocSnapConstants.ScenesSubFolder + "/Kept.html"));
            Assert.IsFalse(Exists(DocSnapConstants.ScenesSubFolder + "/Other.html"));
            Assert.IsFalse(Exists(DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.SceneJsonPrefix + "Other.json"));
            Assert.IsFalse(Exists(DocSnapSummaryWriter.SceneSummaryMarkdown("Other")));
        }

        // The pre-existing ownership proof: without the stylesheet
        // DocSnap itself writes, the folder is not one of ours and
        // nothing in it may be touched.
        [Test]
        public void FolderWithoutOwnershipProof_DeletesNothing()
        {
            Write(DocSnapConstants.ScenesSubFolder + "/Other.html", "<html></html>");

            DocSnapExportService.PruneStaleOutput(_root, ManifestWithScene("Kept", loadedFromDisk: true));

            Assert.IsTrue(Exists(DocSnapConstants.ScenesSubFolder + "/Other.html"));
        }

        [Test]
        public void MissingOutputFolder_DoesNotThrow()
        {
            string missing = Path.Combine(_root, "nope");
            Assert.DoesNotThrow(() =>
                DocSnapExportService.PruneStaleOutput(missing, ManifestWithScene("Kept", loadedFromDisk: true)));
        }

        // ==========================================
        // Thumbnails.
        //
        // theme/thumbs/ was purely additive: a preview was
        // written as <guid>.png and nothing ever removed it, so
        // re-exporting onto a version folder - which "Update
        // Previous Export" does by design, repeatedly -
        // accumulated a preview for every asset the project had
        // ever held. Nothing links to them, so the folder just
        // grew, invisibly.
        // ==========================================
        [Test]
        public void Thumbnails_ForAssetsNoLongerExported_AreRemoved()
        {
            MakeVersionFolder();
            string thumbs = DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.ThumbsSubFolder;
            Write(thumbs + "/aaaa1111.png", "png");
            Write(thumbs + "/bbbb2222.png", "png");

            var state = ManifestWithScene("Kept", loadedFromDisk: true);
            state.assetIndex.Add(new ManifestAssetIndexEntry { guid = "aaaa1111", folderKey = "Assets" });

            DocSnapExportService.PruneStaleOutput(_root, state);

            Assert.IsTrue(Exists(thumbs + "/aaaa1111.png"), "A thumbnail for a still-exported asset must survive.");
            Assert.IsFalse(Exists(thumbs + "/bbbb2222.png"), "A thumbnail for an asset no longer exported is stale.");
        }

        // A Scene-only export leaves the asset index untouched and
        // empty. An empty live set would read as "none of these are
        // current" and take the whole folder with it.
        [Test]
        public void Thumbnails_AreKeptWhenNoAssetsWereExported()
        {
            MakeVersionFolder();
            string thumbs = DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.ThumbsSubFolder;
            Write(thumbs + "/aaaa1111.png", "png");

            DocSnapExportService.PruneStaleOutput(_root, ManifestWithScene("Kept", loadedFromDisk: true));

            Assert.IsTrue(Exists(thumbs + "/aaaa1111.png"));
        }

        // The stylesheet and script the pruner uses as its own
        // ownership proof live in theme/ beside thumbs/. Sweeping
        // the nested folder must not reach up into the parent.
        [Test]
        public void PruningThumbnails_LeavesTheSiteAssetsAlone()
        {
            MakeVersionFolder();
            Write(DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.ScriptFileName, "// js");
            Write(DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.SearchIndexFileName, "// index");
            Write(DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.ThumbsSubFolder + "/dead.png", "png");

            var state = ManifestWithScene("Kept", loadedFromDisk: true);
            state.assetIndex.Add(new ManifestAssetIndexEntry { guid = "alive", folderKey = "Assets" });

            DocSnapExportService.PruneStaleOutput(_root, state);

            Assert.IsTrue(Exists(DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.StyleFileName));
            Assert.IsTrue(Exists(DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.ScriptFileName));
            Assert.IsTrue(Exists(DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.SearchIndexFileName));
            Assert.IsFalse(Exists(DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.ThumbsSubFolder + "/dead.png"));
        }

        // The Packages summary and the AI bundle belong to the
        // export as a whole, not to any one Scene or folder, so a
        // single-item export must not sweep them away.
        [Test]
        public void ProjectWideSummaries_SurvivePruning()
        {
            MakeVersionFolder();
            Write(DocSnapSummaryWriter.PackagesSummaryMarkdown(), "# packages");
            Write(DocSnapSummaryWriter.PackagesSummaryJson(), "{}");
            Write(DocSnapConstants.SummarySubFolder + "/" + DocSnapConstants.AiBundleFileName, "# bundle");

            DocSnapExportService.PruneStaleOutput(_root, ManifestWithScene("Kept", loadedFromDisk: true));

            Assert.IsTrue(Exists(DocSnapSummaryWriter.PackagesSummaryMarkdown()));
            Assert.IsTrue(Exists(DocSnapSummaryWriter.PackagesSummaryJson()));
            Assert.IsTrue(Exists(DocSnapConstants.SummarySubFolder + "/" + DocSnapConstants.AiBundleFileName));
        }

        // The public data/manifest.json is written by the exporter
        // itself rather than being listed as any scope's output.
        [Test]
        public void PublicManifestJson_SurvivesPruning()
        {
            MakeVersionFolder();
            Write(DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.ManifestFileName, "{}");

            DocSnapExportService.PruneStaleOutput(_root, ManifestWithScene("Kept", loadedFromDisk: true));

            Assert.IsTrue(Exists(DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.ManifestFileName));
        }

        // A folder's own outputs follow the same rule Scenes do.
        [Test]
        public void FolderOutputs_AreKeptWhileListed()
        {
            MakeVersionFolder();
            var state = new ManifestState { loadedFromDisk = true };
            state.assetFolders.Add(new ManifestFolderEntry
            {
                folderPath = "Assets",
                folderKey = "Assets",
                htmlFile = DocSnapConstants.AssetsSubFolder + "/Assets.html",
                jsonFile = DocSnapConstants.DataSubFolder + "/" + DocSnapConstants.FolderJsonPrefix + "Assets.json"
            });

            Write(DocSnapConstants.AssetsSubFolder + "/Assets.html", "<html></html>");
            Write(DocSnapConstants.AssetsSubFolder + "/Gone.html", "<html></html>");
            Write(DocSnapSummaryWriter.FolderSummaryMarkdown("Assets"), "# assets");

            DocSnapExportService.PruneStaleOutput(_root, state);

            Assert.IsTrue(Exists(DocSnapConstants.AssetsSubFolder + "/Assets.html"));
            Assert.IsTrue(Exists(DocSnapSummaryWriter.FolderSummaryMarkdown("Assets")));
            Assert.IsFalse(Exists(DocSnapConstants.AssetsSubFolder + "/Gone.html"));
        }
    }
}
