// ==========================================
// DocSnapDeleteVersionTests
//
// DeleteVersion and ClearOutputRoot are the second
// and third things in Unity DocSnap that remove a
// directory tree, and the only two a person asks for
// on purpose. The shelf is the only copy of every
// earlier export, so the whole of the risk is here:
// deleting a folder that is not ours, deleting more
// than was asked for, or - quieter and worse -
// deleting the folder and leaving the registry
// believing in it, which produces a snapshot the tool
// offers and cannot open.
// ==========================================
using System.IO;
using AmirCollider.UnityDocSnap.Editor.Export;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class DocSnapDeleteVersionTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "DocSnapDeleteTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_root)) { Directory.Delete(_root, true); } }
            catch { /* a temp folder that outlives one test run is harmless */ }
        }

        // A version folder is only recognised by the stylesheet every
        // export writes into it, so a realistic fixture carries one.
        private string MakeVersion(string name, params string[] extraFiles)
        {
            string folder = Path.Combine(_root, name);
            Write(folder, DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.StyleFileName, "/* css */");
            foreach (string f in extraFiles) { Write(folder, f, "x"); }
            return folder;
        }

        private static void Write(string folder, string relative, string content)
        {
            string full = Path.Combine(folder, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, content);
        }

        private static VersionsState Registry(params string[] versions)
        {
            var state = new VersionsState();
            foreach (string v in versions) { state.versions.Add(new VersionSnapshot { version = v }); }
            state.activeVersion = versions.Length > 0 ? versions[versions.Length - 1] : "";
            return state;
        }

        // ==========================================
        // The ordinary case
        // ==========================================
        [Test]
        public void DeleteVersion_RemovesTheFolderAndTheEntryTogether()
        {
            MakeVersion("V1.0.0", "index.html");
            MakeVersion("V1.0.1", "index.html");
            VersionsState state = Registry("V1.0.0", "V1.0.1");

            string error;
            Assert.IsTrue(DocSnapVersioning.DeleteVersion(_root, state, "V1.0.0", out error), error);

            Assert.IsFalse(Directory.Exists(Path.Combine(_root, "V1.0.0")));
            Assert.AreEqual(1, state.versions.Count);
            Assert.AreEqual("V1.0.1", state.versions[0].version);

            // And nothing else moved.
            Assert.IsTrue(File.Exists(Path.Combine(_root, "V1.0.1", "index.html")));
        }

        // One at a time is the whole design: the caller names a
        // version, and exactly that version goes.
        [Test]
        public void DeleteVersion_TakesOnlyTheOneItWasGiven()
        {
            MakeVersion("V1.0.0");
            MakeVersion("V1.0.1");
            MakeVersion("V1.0.2");
            VersionsState state = Registry("V1.0.0", "V1.0.1", "V1.0.2");

            string error;
            DocSnapVersioning.DeleteVersion(_root, state, "V1.0.1", out error);

            Assert.IsTrue(Directory.Exists(Path.Combine(_root, "V1.0.0")));
            Assert.IsFalse(Directory.Exists(Path.Combine(_root, "V1.0.1")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_root, "V1.0.2")));
            Assert.AreEqual(2, state.versions.Count);
        }

        // ==========================================
        // The refusals
        // ==========================================

        // A folder without the proof is not ours, and the version
        // name that resolved to it must not be the thing that finds
        // out. The registry keeps its entry too - deleting half of a
        // pair is how a snapshot becomes unopenable.
        [Test]
        public void DeleteVersion_RefusesAFolderItCannotProveItOwns()
        {
            string stranger = Path.Combine(_root, "V1.0.0");
            Directory.CreateDirectory(stranger);
            File.WriteAllText(Path.Combine(stranger, "somebody-elses-work.txt"), "keep me");

            VersionsState state = Registry("V1.0.0");

            string error;
            Assert.IsFalse(DocSnapVersioning.DeleteVersion(_root, state, "V1.0.0", out error));
            Assert.IsNotEmpty(error);
            Assert.IsTrue(File.Exists(Path.Combine(stranger, "somebody-elses-work.txt")));
            Assert.AreEqual(1, state.versions.Count, "the entry stays with the folder it describes");
        }

        // Somebody deleting the folder by hand is a supported thing
        // to do, and the entry is then the only thing left to clear.
        [Test]
        public void DeleteVersion_ClearsTheEntryWhenTheFolderIsAlreadyGone()
        {
            VersionsState state = Registry("V1.0.0");

            string error;
            Assert.IsTrue(DocSnapVersioning.DeleteVersion(_root, state, "V1.0.0", out error), error);
            CollectionAssert.IsEmpty(state.versions);
        }

        // The active version is what a single-Scene export writes
        // into. Left pointing at a deleted folder, the next quick
        // export silently recreates it.
        [Test]
        public void DeleteVersion_MovesTheActiveVersionOff()
        {
            MakeVersion("V1.0.0");
            MakeVersion("V1.0.1");
            VersionsState state = Registry("V1.0.0", "V1.0.1");
            Assert.AreEqual("V1.0.1", state.activeVersion);

            string error;
            DocSnapVersioning.DeleteVersion(_root, state, "V1.0.1", out error);

            Assert.AreEqual("V1.0.0", state.activeVersion);
        }

        // ==========================================
        // Clearing the output folder
        // ==========================================

        // Only from an empty shelf. This is what makes "delete
        // everything" reachable only by deleting each snapshot
        // deliberately, having read which one it was.
        [Test]
        public void ClearOutputRoot_RefusesWhileASnapshotRemains()
        {
            MakeVersion("V1.0.0");
            VersionsState state = Registry("V1.0.0");

            Assert.IsFalse(DocSnapVersioning.CanClearOutputRoot(state));

            string error;
            Assert.IsFalse(DocSnapVersioning.ClearOutputRoot(_root, state, out error));
            Assert.IsTrue(Directory.Exists(Path.Combine(_root, "V1.0.0")));
        }

        // The output root is chosen by the user and can hold other
        // things - "Build/Docs" is the tool's own documented CI
        // example. Only what DocSnap wrote goes.
        [Test]
        public void ClearOutputRoot_LeavesTheFolderAndAnythingElseInIt()
        {
            MakeVersion("V1.0.0");
            File.WriteAllText(Path.Combine(_root, DocSnapConstants.RootVersionsFileName), "<html>");
            File.WriteAllText(Path.Combine(_root, DocSnapConstants.RootRedirectFileName), "<html>");
            File.WriteAllText(Path.Combine(_root, "notes.txt"), "mine");
            Directory.CreateDirectory(Path.Combine(_root, "OtherTool"));
            File.WriteAllText(Path.Combine(_root, "OtherTool", "report.html"), "theirs");

            var state = new VersionsState();   // empty shelf

            string error;
            Assert.IsTrue(DocSnapVersioning.ClearOutputRoot(_root, state, out error), error);

            Assert.IsTrue(Directory.Exists(_root), "the folder itself is never removed");
            Assert.IsFalse(Directory.Exists(Path.Combine(_root, "V1.0.0")));
            Assert.IsFalse(File.Exists(Path.Combine(_root, DocSnapConstants.RootVersionsFileName)));
            Assert.IsFalse(File.Exists(Path.Combine(_root, DocSnapConstants.RootRedirectFileName)));

            Assert.IsTrue(File.Exists(Path.Combine(_root, "notes.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_root, "OtherTool", "report.html")),
                "a folder without the proof is somebody else's and is left where it is");
        }
    }
}
