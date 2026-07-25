// ==========================================
// DocSnapLegacyCleanupTests
// CleanLegacyOutput recursively deletes three whole
// directories by name - "assets", "assets_ui" and
// "files" - because older versions of Unity DocSnap
// wrote them. Two of those three names are names other
// software uses constantly.
//
// PruneStaleOutput already refused to run anywhere it
// could not prove it owned; this one did not, and the
// two sat in the same file doing the same class of
// thing. These tests pin the guard down from both
// sides: it still cleans a real DocSnap version
// folder, and it will not touch a folder that merely
// happens to be pointed at.
// ==========================================
using System.IO;
using AmirCollider.UnityDocSnap.Editor.Export;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class DocSnapLegacyCleanupTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "DocSnapLegacyTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_root)) { Directory.Delete(_root, true); } }
            catch { /* a temp folder that outlives one test run is harmless */ }
        }

        // The ownership proof: the stylesheet an export writes
        // into every version folder before anything else happens.
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
            string full = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(full) || Directory.Exists(full);
        }

        // ==========================================
        // Ownership.
        // ==========================================
        [Test]
        public void AFolderThatIsNotOurs_IsRecognisedAsSuch()
        {
            Assert.IsFalse(DocSnapExportService.IsDocSnapVersionFolder(_root));

            MakeVersionFolder();

            Assert.IsTrue(DocSnapExportService.IsDocSnapVersionFolder(_root));
        }

        [Test]
        public void AFolderThatIsNotOurs_IsLeftCompletelyAlone()
        {
            // The scenario this exists for: an output path typed
            // into Project Settings that points at a web project,
            // a Downloads folder, or anything else with an
            // "assets" folder in it.
            Write("assets/site.css", "not ours");
            Write("files/report.pdf", "not ours");
            Write("assets_ui/theme.css", "not ours");
            Write("export_complete.html", "not ours");

            DocSnapExportService.CleanLegacyOutput(_root);

            Assert.IsTrue(Exists("assets/site.css"), "a folder we do not own must not be cleaned");
            Assert.IsTrue(Exists("files/report.pdf"));
            Assert.IsTrue(Exists("assets_ui/theme.css"));
            Assert.IsTrue(Exists("export_complete.html"));
        }

        [Test]
        public void AMissingFolder_IsNotAnError()
        {
            string missing = Path.Combine(_root, "does-not-exist");

            Assert.DoesNotThrow(() => DocSnapExportService.CleanLegacyOutput(missing));
        }

        // ==========================================
        // …while still doing its job.
        // ==========================================
        [Test]
        public void OurOwnVersionFolder_IsStillCleaned()
        {
            MakeVersionFolder();
            Write("assets/old.html", "stale");
            Write("assets_ui/old.css", "stale");
            Write("files/old.png", "stale");
            Write("export_complete.html", "stale");

            DocSnapExportService.CleanLegacyOutput(_root);

            Assert.IsFalse(Exists("assets"), "the pre-rename folders are what this method is for");
            Assert.IsFalse(Exists("assets_ui"));
            Assert.IsFalse(Exists("files"));
            Assert.IsFalse(Exists("export_complete.html"));
        }

        [Test]
        public void TheCurrentOutputLayout_IsNeverTouched()
        {
            // "folders", "theme" and "source-files" are the
            // present-day names of the three folders above. A
            // cleanup that confused the old names with the new
            // ones would delete the export it just wrote.
            MakeVersionFolder();
            Write(DocSnapConstants.AssetsSubFolder + "/Assets.html", "current");
            Write(DocSnapConstants.ScenesSubFolder + "/Main.html", "current");
            Write(DocSnapConstants.DataSubFolder + "/scene-Main.json", "current");
            Write("source-files/Assets/Art/logo.png", "current");

            DocSnapExportService.CleanLegacyOutput(_root);

            Assert.IsTrue(Exists(DocSnapConstants.AssetsSubFolder + "/Assets.html"));
            Assert.IsTrue(Exists(DocSnapConstants.ScenesSubFolder + "/Main.html"));
            Assert.IsTrue(Exists(DocSnapConstants.DataSubFolder + "/scene-Main.json"));
            Assert.IsTrue(Exists("source-files/Assets/Art/logo.png"));
            Assert.IsTrue(Exists(DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.StyleFileName));
        }
    }
}
