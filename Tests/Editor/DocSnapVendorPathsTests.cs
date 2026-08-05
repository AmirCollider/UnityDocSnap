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

        // ==========================================
        // What the file IS, not only where it sits.
        //
        // The report that started this said "7 broken references in
        // your files" and listed seven fields of a URP Renderer2D
        // asset - probeVolumeResources.probeVolumeDebugShader and
        // friends - in a project that keeps its render-pipeline
        // assets under Assets/+_Game/Settings/ rather than in the
        // root folder Unity's template made. Every one of those
        // fields is written by Unity and broken by Unity's own
        // upgrader; not one of them can be reached from an
        // Inspector. Filing them as the author's work because of
        // where the file was moved to is the failure this pair of
        // rules exists to prevent, and path alone cannot see it.
        // ==========================================
        [Test]
        public void AUnityGeneratedAsset_IsNotMineWhereverItIsFiled()
        {
            const string moved = "Assets/+_KsoSherPluss/Settings/Renderer2D.asset";

            Assert.IsFalse(DocSnapVendorPaths.IsVendor(moved),
                "the path alone says nothing - which is exactly the case this covers");

            Assert.IsTrue(DocSnapVendorPaths.IsVendor(moved, "UnityEngine.Rendering.Universal.Renderer2DData"));
            Assert.AreEqual(DocSnapVendorPaths.OwnerVendor,
                DocSnapVendorPaths.Classify(moved, "UnityEngine.Rendering.Universal.Renderer2DData"));
        }

        [Test]
        public void ASceneTemplate_IsUnitys()
        {
            Assert.IsTrue(DocSnapVendorPaths.IsVendor(
                "Assets/+_KsoSherPluss/Settings/Lit2DSceneTemplate.scenetemplate",
                "UnityEditor.SceneTemplate.SceneTemplateAsset"));
        }

        // The type rule is matched on the FULL name. Short names
        // collide, and a project's own class called Renderer2DData
        // having its broken references excused is the same failure
        // in the opposite direction.
        [Test]
        public void AProjectsOwnTypeIsNotExcusedByASharedShortName()
        {
            Assert.IsFalse(DocSnapVendorPaths.IsVendorType("MyGame.Rendering.Renderer2DData"));
            Assert.IsFalse(DocSnapVendorPaths.IsVendorType("Renderer2DData"));
            Assert.IsFalse(DocSnapVendorPaths.IsVendorType(""));
            Assert.IsFalse(DocSnapVendorPaths.IsVendorType(null));
        }

        // An ordinary asset of an ordinary engine type stays the
        // author's, whatever its type name looks like. A Material
        // with a missing texture is squarely their problem.
        [Test]
        public void AnOrdinaryEngineTypeStaysMine()
        {
            Assert.IsFalse(DocSnapVendorPaths.IsVendor("Assets/Art/Player.mat", "UnityEngine.Material"));
            Assert.IsFalse(DocSnapVendorPaths.IsVendor("Assets/Prefabs/Enemy.prefab", "UnityEngine.GameObject"));
        }

        // TMP Essentials tidied into a ThirdParty folder is still TMP
        // Essentials. Only the names nobody uses for a folder of
        // their own match this way - "Settings" and "Plugins"
        // deliberately do not.
        [Test]
        public void UnmistakableVendorFolderNamesMatchAnywhere()
        {
            Assert.IsTrue(DocSnapVendorPaths.IsVendor("Assets/_Game/ThirdParty/TextMesh Pro/Resources/x.asset"));
            Assert.IsFalse(DocSnapVendorPaths.IsVendor("Assets/_Game/Settings/GameConfig.asset"));
            Assert.IsFalse(DocSnapVendorPaths.IsVendor("Assets/_Game/Plugins/Mine.dll"));
        }

        // The file's own name is not a folder.
        [Test]
        public void AFileNamedAfterAVendorFolderIsStillMine()
        {
            Assert.IsFalse(DocSnapVendorPaths.IsVendor("Assets/Art/TextMesh Pro.png"));
        }

        // A settings entry with no slash in it reads as a folder
        // NAME. As a path it would have meant a top-level folder
        // beside Assets/, which is not a place Unity puts anything.
        [Test]
        public void ABareSettingsEntryMatchesThatFolderAnywhere()
        {
            DocSnapSettings.VendorFolders = "CoolKit";
            DocSnapVendorPaths.InvalidateCache();

            Assert.IsTrue(DocSnapVendorPaths.IsVendor("Assets/Vendors/CoolKit/thing.prefab"));
            Assert.AreEqual("CoolKit/", DocSnapVendorPaths.Describe("Assets/Vendors/CoolKit/thing.prefab"));
            Assert.IsFalse(DocSnapVendorPaths.IsVendor("Assets/Scripts/CoolKitBridge.cs"));
        }

        [Test]
        public void Describe_FallsBackToTheTypeWhenThePathIsUnremarkable()
        {
            Assert.AreEqual("Renderer2DData", DocSnapVendorPaths.Describe(
                "Assets/+_KsoSherPluss/Settings/Renderer2D.asset",
                "UnityEngine.Rendering.Universal.Renderer2DData"));

            // The folder still wins when there is one: it is the more
            // specific answer to "why is this not mine?".
            Assert.AreEqual("Assets/TextMesh Pro", DocSnapVendorPaths.Describe(
                "Assets/TextMesh Pro/x.asset", "UnityEngine.Rendering.Universal.Renderer2DData"));
        }
    }
}
