// ==========================================
// DocSnapEditionSweepTests
// The half of the edition gate that touches disk.
//
// ClampOptions decides what an export WRITES, and it was
// tested. Nothing tested what the version folder ended up
// HOLDING, and the two are not the same question the
// moment an export lands on a folder that already has
// content - which is what "Update Previous Export" does
// by design, what picking an existing version does, and
// what every export does once the shelf cap is reached.
//
// The bug those tests could not see: a folder built while
// Pro was active kept its project-backup.unitypackage,
// its source-files/ mirror and its changes.html forever,
// because the export only ever wrote files and never
// removed one it had not written this time. A Free export
// onto that folder was byte-for-byte as complete as a Pro
// one, and said so in its own export-info.txt.
//
// Every assertion here is written as an invariant over
// DocSnapLicense.Edition rather than against a hard-coded
// "Free", because the edition on the machine running the
// tests is not something a test can choose: there is no
// way to forge a valid token, and a developer with a real
// Pro key activated must not see red tests because of it.
// "Nothing this edition may not produce is left behind"
// is true for all three tiers and still fails loudly on
// the bug.
// ==========================================
using System.IO;
using AmirCollider.UnityDocSnap.Editor.Export;
using AmirCollider.UnityDocSnap.Editor.Licensing;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class DocSnapEditionSweepTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "DocSnapSweepTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_root)) { Directory.Delete(_root, true); } }
            catch { /* a temp folder that outlives one test run is harmless */ }
        }

        // ==========================================
        // Fixture
        // ==========================================

        // The sweep refuses any folder that does not carry the
        // stylesheet the exporter writes itself, so a realistic
        // fixture has to include it.
        private void MakeVersionFolder()
        {
            Write(DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.StyleFileName, "/* css */");
        }

        // A version folder as a paid export leaves it: a backup, a
        // source-files/ mirror, a Changes page and the byte copies
        // that page links to.
        private void MakePaidArtefacts()
        {
            Write(DocSnapConstants.BackupFileName, "unitypackage bytes");
            Write(DocSnapConstants.FilesSubFolder + "/Assets/Player.cs", "// asset bytes");
            Write(DocSnapConstants.ChangesFileName, "<html>changes</html>");
            Write(DocSnapConstants.ChangesFilesSubFolder + "/"
                + DocSnapConstants.ChangesFilesOldSubFolder + "/Assets/Player.cs", "// old bytes");
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
        // The sweep
        // ==========================================

        // The whole bug, in one assertion per artefact: after a full
        // export has swept the folder it is re-using, nothing that
        // belongs to a feature this edition does not hold is still
        // sitting in it.
        [Test]
        public void Sweep_LeavesNoArtefactOfAFeatureThisEditionLacks()
        {
            MakeVersionFolder();
            MakePaidArtefacts();

            var report = new DocSnapGateReport();
            DocSnapEditionGate.SweepUnlicensedArtefacts(_root, report, "en");

            AssertGone(DocSnapFeature.ProjectBackup, DocSnapConstants.BackupFileName);
            AssertGone(DocSnapFeature.IncludeFiles, DocSnapConstants.FilesSubFolder);
            AssertGone(DocSnapFeature.ChangesPage, DocSnapConstants.ChangesFileName);
            AssertGone(DocSnapFeature.ChangesPage, DocSnapConstants.ChangesFilesSubFolder);
        }

        private void AssertGone(DocSnapFeature feature, string relative)
        {
            if (DocSnapLicense.Has(feature))
            {
                // The other direction, and it matters just as much: a
                // customer who still holds the feature keeps the file.
                // Not ticking a box on one run says nothing about
                // wanting the previous run's output deleted, and a
                // licence check that removed a Pro customer's backup
                // would be a far worse bug than the one this fixes.
                Assert.IsTrue(Exists(relative),
                    relative + " was removed even though this edition holds " + feature + ".");
                return;
            }

            Assert.IsFalse(Exists(relative),
                relative + " survived the sweep, but " + feature + " is not in the "
                + DocSnapEditionMatrix.DisplayName(DocSnapLicense.Edition) + " edition.");
        }

        // A removal the customer is not told about is how somebody
        // discovers their backup is gone by looking for it.
        [Test]
        public void Sweep_ReportsEveryRemoval()
        {
            MakeVersionFolder();
            MakePaidArtefacts();

            var report = new DocSnapGateReport();
            DocSnapEditionGate.SweepUnlicensedArtefacts(_root, report, "en");

            int expected = 0;
            if (!DocSnapLicense.Has(DocSnapFeature.ProjectBackup)) { expected++; }
            if (!DocSnapLicense.Has(DocSnapFeature.IncludeFiles)) { expected++; }
            if (!DocSnapLicense.Has(DocSnapFeature.ChangesPage)) { expected++; }

            Assert.AreEqual(expected, report.Removed.Count,
                "The sweep removed a different number of things than it reported.");
            if (expected > 0)
            {
                Assert.IsFalse(report.IsEmpty, "A report holding removals must not read as empty.");
                Assert.IsNotEmpty(report.Summary("en"), "Removals must reach the export-complete message.");
            }
        }

        // Nothing was there, so nothing is reported. A "removed" line
        // for a file that never existed would send somebody looking
        // for a backup they never had.
        [Test]
        public void Sweep_OnAFreshFolderRemovesAndReportsNothing()
        {
            MakeVersionFolder();

            var report = new DocSnapGateReport();
            DocSnapEditionGate.SweepUnlicensedArtefacts(_root, report, "en");

            Assert.AreEqual(0, report.Removed.Count);
            Assert.IsTrue(report.IsEmpty);
        }

        // The same ownership proof CleanLegacyOutput insists on.
        // "source-files" and "changes-files" are ordinary directory
        // names, and a mistyped output path must never become a
        // recursive delete of somebody else's folder.
        [Test]
        public void Sweep_RefusesAFolderThatIsNotAVersionFolder()
        {
            MakePaidArtefacts();   // deliberately WITHOUT theme/style.css

            var report = new DocSnapGateReport();
            DocSnapEditionGate.SweepUnlicensedArtefacts(_root, report, "en");

            Assert.IsTrue(Exists(DocSnapConstants.BackupFileName));
            Assert.IsTrue(Exists(DocSnapConstants.FilesSubFolder));
            Assert.IsTrue(Exists(DocSnapConstants.ChangesFileName));
            Assert.AreEqual(0, report.Removed.Count);
        }

        [Test]
        public void Sweep_SurvivesAMissingFolderAndANullReport()
        {
            // Neither is expected, and both used to be one null
            // reference away from taking an export down with them.
            Assert.DoesNotThrow(() => DocSnapEditionGate.SweepUnlicensedArtefacts(
                Path.Combine(_root, "no-such-folder"), new DocSnapGateReport(), "en"));
            Assert.DoesNotThrow(() => DocSnapEditionGate.SweepUnlicensedArtefacts(_root, null, "en"));
            Assert.DoesNotThrow(() => DocSnapEditionGate.SweepUnlicensedArtefacts(null, new DocSnapGateReport(), "en"));
        }

        // ==========================================
        // What the snapshot is allowed to claim
        //
        // FinalizeVersion carries hasBackup / withFiles forward from
        // the previous snapshot when this run did not re-establish
        // them - correct for a single-Scene export, which leaves a
        // real backup untouched. It used to carry them forward
        // unconditionally, so once the sweep (or the customer's own
        // Explorer window) removed the file, export-info.txt went on
        // saying "Project backup: yes" over a folder that had none.
        //
        // That is the one error in the export record somebody could
        // act on and lose work over: a snapshot promising a backup is
        // a snapshot somebody plans a risky refactor around.
        // ==========================================

        [Test]
        public void HasBackupOnDisk_IsAboutTheFileAndNotTheRecord()
        {
            Assert.IsFalse(DocSnapExportService.HasBackupOnDisk(_root));
            Write(DocSnapConstants.BackupFileName, "unitypackage bytes");
            Assert.IsTrue(DocSnapExportService.HasBackupOnDisk(_root));
        }

        [Test]
        public void HasSourceFilesOnDisk_TreatsAnEmptyMirrorAsNoMirror()
        {
            Assert.IsFalse(DocSnapExportService.HasSourceFilesOnDisk(_root));

            // The directory survives an interrupted export. "Included
            // file bytes: yes" over an empty folder is the same lie in
            // a smaller font.
            Directory.CreateDirectory(Path.Combine(_root, DocSnapConstants.FilesSubFolder));
            Assert.IsFalse(DocSnapExportService.HasSourceFilesOnDisk(_root));

            Write(DocSnapConstants.FilesSubFolder + "/Assets/Player.cs", "// asset bytes");
            Assert.IsTrue(DocSnapExportService.HasSourceFilesOnDisk(_root));
        }

        [Test]
        public void OnDiskChecks_HandleAnEmptyPath()
        {
            Assert.IsFalse(DocSnapExportService.HasBackupOnDisk(null));
            Assert.IsFalse(DocSnapExportService.HasBackupOnDisk(""));
            Assert.IsFalse(DocSnapExportService.HasSourceFilesOnDisk(null));
            Assert.IsFalse(DocSnapExportService.HasSourceFilesOnDisk(""));
        }
    }
}
