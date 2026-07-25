// ==========================================
// ChangesPageDiffTests
// Which bucket a changed file lands in on the
// Changes page.
//
// The bug these hold shut: TextMesh Pro's dynamic font
// asset re-serialises its glyph atlas whenever a new
// character is rendered, so its bytes differ after
// nothing more than opening and closing the Editor. It
// therefore appeared under "Modified" on essentially
// every diff of a project nobody had edited.
//
// The fix moves such files to their own group, and the
// risk it introduces is the opposite failure: quietly
// moving a change somebody DID make out of the counts.
// That would be worse, and it would look fine. So these
// assert the split in both directions, and assert that
// nothing is ever dropped entirely.
// ==========================================
using System.Collections.Generic;
using AmirCollider.UnityDocSnap.Editor.Export;
using AmirCollider.UnityDocSnap.Editor.Html;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public class ChangesPageDiffTests
    {
        private const string TmpFallback =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";

        [SetUp]
        [TearDown]
        public void ResetProjectPatterns()
        {
            DocSnapSettings.ClearSessionOverrides();
            DocSnapRegeneratedPaths.InvalidateCache();
        }

        // ------------------------------------------
        // Helpers
        // ------------------------------------------
        private static VersionFileEntry File(string path, string hash, long size = 100)
        {
            return new VersionFileEntry
            {
                path = path,
                size = size,
                // A signature distinct from the hash on purpose: these
                // tests are about the hash being the authority, and a
                // signature that happened to match would hide a
                // regression in that.
                signature = size + ":" + hash.GetHashCode(),
                contentHash = hash
            };
        }

        private static VersionSnapshot Snapshot(string version, params VersionFileEntry[] files)
        {
            var snap = new VersionSnapshot { version = version };
            snap.files.AddRange(files);
            return snap;
        }

        private static List<string> Paths(List<ChangesPageRenderer.FileDiff> diffs)
        {
            var paths = new List<string>();
            foreach (ChangesPageRenderer.FileDiff d in diffs) { paths.Add(d.path); }
            return paths;
        }

        private static void Run(VersionSnapshot baseSnap, VersionSnapshot current,
            out List<string> added, out List<string> removed, out List<string> modified, out List<string> regenerated)
        {
            var a = new List<ChangesPageRenderer.FileDiff>();
            var r = new List<ChangesPageRenderer.FileDiff>();
            var m = new List<ChangesPageRenderer.FileDiff>();
            var g = new List<ChangesPageRenderer.FileDiff>();
            ChangesPageRenderer.DiffFiles(baseSnap, current, a, r, m, g);
            added = Paths(a);
            removed = Paths(r);
            modified = Paths(m);
            regenerated = Paths(g);
        }

        // ------------------------------------------
        // The reported bug
        // ------------------------------------------

        [Test]
        public void TmpFontAsset_WithChangedBytes_LeavesTheModifiedList()
        {
            VersionSnapshot before = Snapshot("V1.0.0", File(TmpFallback, "aaa"));
            VersionSnapshot after = Snapshot("V1.0.1", File(TmpFallback, "bbb"));

            List<string> added, removed, modified, regenerated;
            Run(before, after, out added, out removed, out modified, out regenerated);

            CollectionAssert.IsEmpty(modified, "Unity's own churn must not be counted as an edit");
            CollectionAssert.Contains(regenerated, TmpFallback);
        }

        // Separated, never silently dropped. A diff that omitted a
        // real byte change altogether would be a worse bug than the
        // noise it set out to fix.
        [Test]
        public void TmpFontAsset_IsStillListed()
        {
            VersionSnapshot before = Snapshot("V1.0.0", File(TmpFallback, "aaa"));
            VersionSnapshot after = Snapshot("V1.0.1", File(TmpFallback, "bbb"));

            List<string> added, removed, modified, regenerated;
            Run(before, after, out added, out removed, out modified, out regenerated);

            Assert.AreEqual(1, added.Count + removed.Count + modified.Count + regenerated.Count,
                "the file must appear exactly once, in exactly one bucket");
        }

        // ------------------------------------------
        // The opposite failure: a real change must survive
        // ------------------------------------------

        [Test]
        public void AnOrdinaryEditedFile_StaysModified()
        {
            VersionSnapshot before = Snapshot("V1.0.0", File("Assets/Scripts/Player.cs", "aaa"));
            VersionSnapshot after = Snapshot("V1.0.1", File("Assets/Scripts/Player.cs", "bbb"));

            List<string> added, removed, modified, regenerated;
            Run(before, after, out added, out removed, out modified, out regenerated);

            CollectionAssert.Contains(modified, "Assets/Scripts/Player.cs");
            CollectionAssert.IsEmpty(regenerated);
        }

        // Only files present in BOTH versions are classified. A TMP
        // font asset appearing for the first time means the package
        // was just installed, which is somebody's doing and exactly
        // the kind of thing to report.
        [Test]
        public void ANewlyAddedTmpFontAsset_IsStillReportedAsAdded()
        {
            VersionSnapshot before = Snapshot("V1.0.0");
            VersionSnapshot after = Snapshot("V1.0.1", File(TmpFallback, "aaa"));

            List<string> added, removed, modified, regenerated;
            Run(before, after, out added, out removed, out modified, out regenerated);

            CollectionAssert.Contains(added, TmpFallback);
            CollectionAssert.IsEmpty(regenerated);
        }

        [Test]
        public void ADeletedTmpFontAsset_IsStillReportedAsRemoved()
        {
            VersionSnapshot before = Snapshot("V1.0.0", File(TmpFallback, "aaa"));
            VersionSnapshot after = Snapshot("V1.0.1");

            List<string> added, removed, modified, regenerated;
            Run(before, after, out added, out removed, out modified, out regenerated);

            CollectionAssert.Contains(removed, TmpFallback);
            CollectionAssert.IsEmpty(regenerated);
        }

        // An unchanged TMP asset is not a change of any kind. The
        // classifier must not manufacture an entry for a file whose
        // hash matched.
        [Test]
        public void AnUnchangedTmpFontAsset_ProducesNoEntryAtAll()
        {
            VersionSnapshot before = Snapshot("V1.0.0", File(TmpFallback, "aaa"));
            VersionSnapshot after = Snapshot("V1.0.1", File(TmpFallback, "aaa"));

            List<string> added, removed, modified, regenerated;
            Run(before, after, out added, out removed, out modified, out regenerated);

            Assert.AreEqual(0, added.Count + removed.Count + modified.Count + regenerated.Count);
        }

        // ------------------------------------------
        // Mixed, and the "nothing changed" reading
        // ------------------------------------------

        [Test]
        public void RealEditsAndChurn_AreSeparated()
        {
            VersionSnapshot before = Snapshot("V1.0.0",
                File(TmpFallback, "aaa"),
                File("Assets/Scripts/Player.cs", "ccc"));
            VersionSnapshot after = Snapshot("V1.0.1",
                File(TmpFallback, "bbb"),
                File("Assets/Scripts/Player.cs", "ddd"));

            List<string> added, removed, modified, regenerated;
            Run(before, after, out added, out removed, out modified, out regenerated);

            CollectionAssert.AreEqual(new[] { "Assets/Scripts/Player.cs" }, modified);
            CollectionAssert.AreEqual(new[] { TmpFallback }, regenerated);
        }

        // The page prints "no differences detected" from the added /
        // removed / modified counts. A diff whose only entry is
        // Unity's churn has to reach that line, or the page would
        // show no differences and never say so.
        [Test]
        public void ADiffOfNothingButChurn_ReadsAsEmpty()
        {
            VersionSnapshot before = Snapshot("V1.0.0", File(TmpFallback, "aaa"));
            VersionSnapshot after = Snapshot("V1.0.1", File(TmpFallback, "bbb"));

            List<string> added, removed, modified, regenerated;
            Run(before, after, out added, out removed, out modified, out regenerated);

            Assert.AreEqual(0, added.Count + removed.Count + modified.Count,
                "the counts the \"no differences\" line reads must all be zero");
            Assert.AreEqual(1, regenerated.Count);
        }

        // ------------------------------------------
        // Project-configured patterns reach the diff
        // ------------------------------------------

        [Test]
        public void AProjectAddedPattern_MovesAFileToTheRegeneratedGroup()
        {
            DocSnapSettings.SetSessionOverride("regeneratedPaths", "Assets/Generated/*.asset");

            VersionSnapshot before = Snapshot("V1.0.0", File("Assets/Generated/atlas.asset", "aaa"));
            VersionSnapshot after = Snapshot("V1.0.1", File("Assets/Generated/atlas.asset", "bbb"));

            List<string> added, removed, modified, regenerated;
            Run(before, after, out added, out removed, out modified, out regenerated);

            CollectionAssert.IsEmpty(modified);
            CollectionAssert.Contains(regenerated, "Assets/Generated/atlas.asset");
        }

        // A caller that does not want the split (nothing does today,
        // but the parameter is nullable and the contract should be
        // real) gets everything in "modified" rather than a crash.
        [Test]
        public void ANullRegeneratedList_KeepsEverythingInModified()
        {
            VersionSnapshot before = Snapshot("V1.0.0", File(TmpFallback, "aaa"));
            VersionSnapshot after = Snapshot("V1.0.1", File(TmpFallback, "bbb"));

            var a = new List<ChangesPageRenderer.FileDiff>();
            var r = new List<ChangesPageRenderer.FileDiff>();
            var m = new List<ChangesPageRenderer.FileDiff>();
            ChangesPageRenderer.DiffFiles(before, after, a, r, m, null);

            CollectionAssert.Contains(Paths(m), TmpFallback);
        }
    }
}
