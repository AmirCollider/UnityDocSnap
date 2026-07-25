// ==========================================
// DocSnapOutputPathTests
// The output folder is the one setting that can do
// real damage to a user's project, and the damage is
// slow rather than loud: an export written into
// Assets/ is thousands of files Unity then imports,
// and the NEXT export documents them, and the one
// after that documents those. By the time anyone
// notices, the folder is enormous and the settings
// value that caused it looks perfectly reasonable.
//
// So every rule gets a test, including the ones that
// must NOT fire - a validator that rejects
// "Build/Docs" would break the tool's own documented
// CI example, and a validator that rejects
// "AssetsExtra" because it starts with "Assets" would
// be the kind of over-matching that makes people turn
// a safety check off.
// ==========================================
using AmirCollider.UnityDocSnap.Editor;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class DocSnapOutputPathTests
    {
        // A project root that exists only as a string. Nothing
        // here touches the disk: these are path rules, and a
        // rule that needed a real folder to be testable would
        // not get tested.
        private const string Root = "/home/dev/MyGame";

        private static DocSnapOutputPathVerdict Verdict(string outputPath)
        {
            return DocSnapOutputPath.Validate(outputPath, Root);
        }

        // ==========================================
        // The destinations that must be allowed.
        // ==========================================
        [Test]
        public void DefaultOutputFolder_IsAccepted()
        {
            Assert.AreEqual(DocSnapOutputPathVerdict.Ok, Verdict(Root + "/UnityDocSnap_Output"));
        }

        [Test]
        public void BuildFolder_IsAccepted()
        {
            // DocSnapAPI's own header documents
            // "-docsnapOutput Build/Docs". A validator that
            // refused it would make the tool's headline CI
            // example fail.
            Assert.AreEqual(DocSnapOutputPathVerdict.Ok, Verdict(Root + "/Build/Docs"));
        }

        [Test]
        public void FolderOutsideTheProject_IsAccepted()
        {
            Assert.AreEqual(DocSnapOutputPathVerdict.Ok, Verdict("/home/dev/GameDocs"));
        }

        [Test]
        public void FolderWhoseNameMerelyStartsWithAssets_IsAccepted()
        {
            // "AssetsExtra" is not inside "Assets". A prefix
            // test without the separator check would say it is.
            Assert.AreEqual(DocSnapOutputPathVerdict.Ok, Verdict(Root + "/AssetsExtra"));
            Assert.AreEqual(DocSnapOutputPathVerdict.Ok, Verdict(Root + "/LibraryOfDocs"));
        }

        // ==========================================
        // Inside Assets — the destructive one.
        // ==========================================
        [Test]
        public void InsideAssets_IsRejected()
        {
            Assert.AreEqual(DocSnapOutputPathVerdict.InsideAssets, Verdict(Root + "/Assets/Docs"));
        }

        [Test]
        public void TheAssetsFolderItself_IsRejected()
        {
            Assert.AreEqual(DocSnapOutputPathVerdict.InsideAssets, Verdict(Root + "/Assets"));
        }

        [Test]
        public void InsideAssets_IsRejectedRegardlessOfCase()
        {
            // macOS and Windows both resolve "assets" to the
            // real Assets folder, so a lower-case spelling is
            // the same destructive destination.
            Assert.AreEqual(DocSnapOutputPathVerdict.InsideAssets, Verdict(Root + "/assets/docs"));
        }

        [Test]
        public void InsideAssets_IsRejectedWithATrailingSeparator()
        {
            Assert.AreEqual(DocSnapOutputPathVerdict.InsideAssets, Verdict(Root + "/Assets/Docs/"));
        }

        [Test]
        public void InsideAssets_IsRejectedWithBackslashSeparators()
        {
            // A path typed on Windows, or read back from a
            // settings file written there. Both sides are
            // normalised the same way, so this holds whichever
            // platform the tests run on.
            Assert.AreEqual(DocSnapOutputPathVerdict.InsideAssets,
                DocSnapOutputPath.Validate("C:\\dev\\MyGame\\Assets\\Docs", "C:\\dev\\MyGame"));
        }

        // ==========================================
        // Folders Unity throws away.
        // ==========================================
        [Test]
        public void InsideLibrary_IsRejected()
        {
            Assert.AreEqual(DocSnapOutputPathVerdict.InsideUnityManagedFolder, Verdict(Root + "/Library/Docs"));
        }

        [Test]
        public void InsideTempLogsOrObj_IsRejected()
        {
            Assert.AreEqual(DocSnapOutputPathVerdict.InsideUnityManagedFolder, Verdict(Root + "/Temp"));
            Assert.AreEqual(DocSnapOutputPathVerdict.InsideUnityManagedFolder, Verdict(Root + "/Logs/docs"));
            Assert.AreEqual(DocSnapOutputPathVerdict.InsideUnityManagedFolder, Verdict(Root + "/obj/docs"));
        }

        [Test]
        public void InsideProjectSettings_IsRejected()
        {
            Assert.AreEqual(DocSnapOutputPathVerdict.InsideProjectSettings, Verdict(Root + "/ProjectSettings/Docs"));
        }

        [Test]
        public void InsidePackages_IsRejected()
        {
            Assert.AreEqual(DocSnapOutputPathVerdict.InsidePackages, Verdict(Root + "/Packages/Docs"));
        }

        // ==========================================
        // Destinations the prune would operate over.
        // ==========================================
        [Test]
        public void TheProjectRootItself_IsRejected()
        {
            Assert.AreEqual(DocSnapOutputPathVerdict.IsProjectRoot, Verdict(Root));
        }

        [Test]
        public void AFolderContainingTheProject_IsRejected()
        {
            Assert.AreEqual(DocSnapOutputPathVerdict.ContainsProject, Verdict("/home/dev"));
        }

        [Test]
        public void AFilesystemRoot_IsRejected()
        {
            // "/" resolves to the Unix root on Linux/macOS and to
            // the current drive's root on Windows, so this one
            // assertion exercises whichever shape the running
            // platform actually produces.
            Assert.AreEqual(DocSnapOutputPathVerdict.IsFilesystemRoot, Verdict("/"));
        }

        // ==========================================
        // Resolution.
        // ==========================================
        [Test]
        public void EmptyConfiguration_ResolvesToTheDefaultFolder()
        {
            string resolved = DocSnapOutputPath.Resolve("", Root);

            StringAssert.Contains(DocSnapConstants.DefaultOutputFolderName, resolved);
            Assert.AreEqual(DocSnapOutputPathVerdict.Ok, DocSnapOutputPath.Validate(resolved, Root));
        }

        [Test]
        public void ARelativeConfiguration_ResolvesAgainstTheProjectRoot()
        {
            string resolved = DocSnapOutputPath.Resolve("Docs/Snapshot", Root);

            StringAssert.Contains("Snapshot", resolved);
            Assert.AreEqual(DocSnapOutputPathVerdict.Ok, DocSnapOutputPath.Validate(resolved, Root));
        }

        [Test]
        public void ARelativeConfigurationPointingIntoAssets_IsStillCaught()
        {
            // The realistic way this setting goes wrong: the user
            // types a project-relative path, not an absolute one.
            string resolved;
            DocSnapOutputPathVerdict verdict =
                DocSnapOutputPath.ResolveAndValidate("Assets/Documentation", Root, out resolved);

            Assert.AreEqual(DocSnapOutputPathVerdict.InsideAssets, verdict);
        }

        [Test]
        public void ARelativePathThatClimbsOut_IsResolvedBeforeBeingJudged()
        {
            // "Assets/../Docs" is not inside Assets, and must not
            // be judged as though it were - which a textual
            // prefix test on the unresolved string would.
            string resolved;
            DocSnapOutputPathVerdict verdict =
                DocSnapOutputPath.ResolveAndValidate("Assets/../Docs", Root, out resolved);

            Assert.AreEqual(DocSnapOutputPathVerdict.Ok, verdict);
        }

        [Test]
        public void IsAcceptable_AgreesWithValidate()
        {
            Assert.IsTrue(DocSnapOutputPath.IsAcceptable("Docs", Root));
            Assert.IsFalse(DocSnapOutputPath.IsAcceptable("Assets/Docs", Root));
        }

        // ==========================================
        // ProjectRelativeOrNull — what the exclude filter
        // uses to keep an export from documenting itself.
        // ==========================================
        [Test]
        public void ProjectRelative_IsTheAssetsPathWhenOutputSitsInsideTheProject()
        {
            string relative = DocSnapOutputPath.ProjectRelativeOrNull(Root + "/Assets/Docs", Root);

            Assert.AreEqual("Assets/Docs", relative);
        }

        [Test]
        public void ProjectRelative_IsNullForAnOutputOutsideTheProject()
        {
            Assert.IsNull(DocSnapOutputPath.ProjectRelativeOrNull("/home/dev/GameDocs", Root));
        }

        [Test]
        public void ProjectRelative_IsNullForTheProjectRootItself()
        {
            Assert.IsNull(DocSnapOutputPath.ProjectRelativeOrNull(Root, Root));
        }

        // ==========================================
        // An excluded output folder is what actually stops
        // the compounding export, so the two pieces are
        // checked together rather than only apart.
        // ==========================================
        [Test]
        public void AnExcludeRuleBuiltFromTheOutputPath_RemovesItFromTheWalk()
        {
            string relative = DocSnapOutputPath.ProjectRelativeOrNull(Root + "/Assets/Docs", Root);
            DocSnapExcludeFilter filter = DocSnapExcludeFilter.Parse("").AddPrefix(relative);

            Assert.IsTrue(filter.IsExcluded("Assets/Docs"));
            Assert.IsTrue(filter.IsExcluded("Assets/Docs/V1.0.0/index.html"));
            Assert.IsFalse(filter.IsExcluded("Assets/Scripts/Player.cs"));
        }

        [Test]
        public void AddPrefix_IsReportedLikeAnyOtherRule()
        {
            // export-info states what an export left out. A rule
            // the tool added itself is still something it left
            // out, so it belongs in that list.
            DocSnapExcludeFilter filter = DocSnapExcludeFilter.Parse("Assets/Plugins").AddPrefix("Assets/Docs");

            CollectionAssert.Contains(filter.Patterns, "Assets/Docs");
            CollectionAssert.Contains(filter.Patterns, "Assets/Plugins");
        }

        [Test]
        public void AddPrefix_IgnoresEmptyAndDuplicateRules()
        {
            DocSnapExcludeFilter filter = DocSnapExcludeFilter.Parse("Assets/Docs")
                .AddPrefix("Assets/Docs")
                .AddPrefix("")
                .AddPrefix(null);

            Assert.AreEqual(1, filter.Patterns.Count);
        }
    }
}
