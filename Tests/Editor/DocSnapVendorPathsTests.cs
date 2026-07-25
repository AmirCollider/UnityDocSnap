// ==========================================
// DocSnapVendorPathsTests
// The split between "my content" and "what Unity
// and my packages installed into Assets/" is what
// makes the health report worth reading. Get it
// wrong in one direction and a real broken
// reference is filed as someone else's problem;
// wrong in the other and the reader is handed
// eight findings they cannot act on.
// ==========================================
using AmirCollider.UnityDocSnap.Editor;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class DocSnapVendorPathsTests
    {
        [SetUp]
        [TearDown]
        public void ResetProjectVendorFolders()
        {
            // The folder list is cached against the raw settings
            // string, and these tests write that string.
            DocSnapSettings.VendorFolders = "";
            DocSnapVendorPaths.InvalidateCache();
        }

        // The exact case that started this: eight findings, seven in
        // Assets/Settings and one in Assets/TextMesh Pro, none of them
        // the author's to fix and none of them deletable.
        [Test]
        public void UnityInstalledFolders_AreNotMine()
        {
            Assert.IsTrue(DocSnapVendorPaths.IsVendor("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"));
            Assert.IsTrue(DocSnapVendorPaths.IsVendor("Assets/Settings/URP-Balanced.asset"));
            Assert.IsTrue(DocSnapVendorPaths.IsVendor("Assets/Samples/XR/Something.prefab"));
            Assert.IsTrue(DocSnapVendorPaths.IsVendor("Assets/Plugins/Vendor/lib.dll"));
        }

        [Test]
        public void TheProjectsOwnContent_IsMine()
        {
            Assert.IsFalse(DocSnapVendorPaths.IsVendor("Assets/Scripts/PlayerController.cs"));
            Assert.IsFalse(DocSnapVendorPaths.IsVendor("Assets/Scenes/MainMenu.unity"));
            Assert.IsFalse(DocSnapVendorPaths.IsVendor("Assets/Art/Backgrounds/street.png"));
        }

        // A folder must match on a path BOUNDARY. "Assets/SettingsUI"
        // is the author's own folder that merely starts with the same
        // letters, and quietly excusing its broken references would be
        // the worse of the two failure directions.
        [Test]
        public void APrefixThatIsNotAFolderBoundary_DoesNotMatch()
        {
            Assert.IsFalse(DocSnapVendorPaths.IsVendor("Assets/SettingsUI/Panel.prefab"));
            Assert.IsFalse(DocSnapVendorPaths.IsVendor("Assets/PluginsOfMine/thing.asset"));
        }

        [Test]
        public void TheFolderItself_CountsAsVendor()
        {
            Assert.IsTrue(DocSnapVendorPaths.IsVendor("Assets/TextMesh Pro"));
            Assert.IsTrue(DocSnapVendorPaths.IsVendor("Assets/TextMesh Pro/"));
        }

        [Test]
        public void AnInstalledPackage_IsNeverMine()
        {
            Assert.IsTrue(DocSnapVendorPaths.IsVendor("Packages/com.unity.render-pipelines.universal/Runtime/Thing.asset"));
        }

        [Test]
        public void MatchingIgnoresCaseAndSeparatorStyle()
        {
            Assert.IsTrue(DocSnapVendorPaths.IsVendor("assets/textmesh pro/x.asset"));
            Assert.IsTrue(DocSnapVendorPaths.IsVendor("Assets\\TextMesh Pro\\x.asset"));
        }

        [Test]
        public void TheProjectCanAddItsOwnVendorFolders()
        {
            Assert.IsFalse(DocSnapVendorPaths.IsVendor("Assets/AssetStore/CoolKit/thing.prefab"));

            DocSnapSettings.VendorFolders = "Assets/AssetStore\n# a comment\n";
            DocSnapVendorPaths.InvalidateCache();

            Assert.IsTrue(DocSnapVendorPaths.IsVendor("Assets/AssetStore/CoolKit/thing.prefab"));
            Assert.IsFalse(DocSnapVendorPaths.IsVendor("Assets/Scripts/Mine.cs"));
        }

        [Test]
        public void Describe_NamesTheFolderThatMatched()
        {
            Assert.AreEqual("Assets/TextMesh Pro", DocSnapVendorPaths.Describe("Assets/TextMesh Pro/x.asset"));
            Assert.AreEqual("Packages/", DocSnapVendorPaths.Describe("Packages/com.unity.foo/x.asset"));
            Assert.AreEqual("", DocSnapVendorPaths.Describe("Assets/Scripts/Mine.cs"));
        }

        [Test]
        public void Classify_ReturnsTheOwnerTokensTheReportUses()
        {
            Assert.AreEqual(DocSnapVendorPaths.OwnerVendor, DocSnapVendorPaths.Classify("Assets/Settings/URP.asset"));
            Assert.AreEqual(DocSnapVendorPaths.OwnerMine, DocSnapVendorPaths.Classify("Assets/Scripts/Mine.cs"));
        }

        // An empty or missing path is not evidence of anything, and
        // guessing "vendor" there would silently hide real findings.
        [Test]
        public void AnEmptyPath_IsNotTreatedAsVendor()
        {
            Assert.IsFalse(DocSnapVendorPaths.IsVendor(""));
            Assert.IsFalse(DocSnapVendorPaths.IsVendor(null));
        }
    }
}
