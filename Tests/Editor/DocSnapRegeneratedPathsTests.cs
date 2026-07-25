// ==========================================
// DocSnapRegeneratedPathsTests
// The classifier that decides which files on the
// Changes page were rewritten by Unity rather than
// edited by a person.
//
// It is worth testing carefully in both directions,
// because both mistakes are bad in different ways:
//
//   a false NEGATIVE puts a file nobody touched back
//   on the diff, which is the bug this was written to
//   fix and which recurs on every single export;
//
//   a false POSITIVE moves a real, authored change out
//   of the counts and into a collapsed group nobody
//   opens, which is worse - the page would be quietly
//   wrong rather than noisily noisy.
//
// So the built-in list is asserted to catch exactly what
// it claims and, just as deliberately, to leave Bake
// output alone.
// ==========================================
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public class DocSnapRegeneratedPathsTests
    {
        [SetUp]
        [TearDown]
        public void ResetProjectPatterns()
        {
            // The pattern list is cached against the raw settings
            // string, and these tests change that string. Clearing it
            // both before and after keeps them independent of each
            // other AND of whatever the host project has configured.
            DocSnapSettings.ClearSessionOverrides();
            DocSnapRegeneratedPaths.InvalidateCache();
        }

        // ------------------------------------------
        // The reported case
        // ------------------------------------------

        // The exact path from the bug report. TMP rewrites this
        // whenever it renders a glyph the atlas does not have,
        // which includes the Editor drawing its own UI, so it
        // changes from nothing but opening and closing Unity.
        [Test]
        public void Classifies_TheReportedLiberationSansFallback()
        {
            Assert.IsTrue(DocSnapRegeneratedPaths.IsRegenerated(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"));
        }

        [Test]
        public void Classifies_TheNonFallbackFontAssetBesideIt()
        {
            Assert.IsTrue(DocSnapRegeneratedPaths.IsRegenerated(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"));
        }

        // TMP installs under "TextMesh Pro" or "TextMeshPro"
        // depending on how it arrived. A fix that only covered the
        // spelling in the bug report would leave half of all
        // projects still seeing it.
        [Test]
        public void Classifies_BothTextMeshProFolderSpellings()
        {
            Assert.IsTrue(DocSnapRegeneratedPaths.IsRegenerated(
                "Assets/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"));
        }

        [Test]
        public void Classifies_SpriteAssets()
        {
            Assert.IsTrue(DocSnapRegeneratedPaths.IsRegenerated(
                "Assets/TextMesh Pro/Sprite Assets/EmojiOne.asset"));
        }

        // ------------------------------------------
        // What it must NOT catch
        // ------------------------------------------

        // Lightmaps, NavMesh and occlusion data are regenerated
        // files too, and every one of them changes because somebody
        // pressed Bake. That is an authored change and precisely
        // what the Changes page exists to report.
        [Test]
        public void DoesNotClassify_BakedLightingData()
        {
            Assert.IsFalse(DocSnapRegeneratedPaths.IsRegenerated("Assets/Scenes/Main/Lightmap-0_comp_light.exr"));
            Assert.IsFalse(DocSnapRegeneratedPaths.IsRegenerated("Assets/Scenes/Main/LightingData.asset"));
            Assert.IsFalse(DocSnapRegeneratedPaths.IsRegenerated("Assets/Scenes/Main/NavMesh.asset"));
        }

        // TMP Settings lives one folder up from the font assets and
        // holds choices a person makes on purpose.
        [Test]
        public void DoesNotClassify_TmpSettings()
        {
            Assert.IsFalse(DocSnapRegeneratedPaths.IsRegenerated("Assets/TextMesh Pro/Resources/TMP Settings.asset"));
        }

        [Test]
        public void DoesNotClassify_OrdinaryProjectAssets()
        {
            Assert.IsFalse(DocSnapRegeneratedPaths.IsRegenerated("Assets/Scripts/PlayerController.cs"));
            Assert.IsFalse(DocSnapRegeneratedPaths.IsRegenerated("Assets/Art/Textures/hero.png"));
        }

        [Test]
        public void DoesNotClassify_NullOrEmpty()
        {
            Assert.IsFalse(DocSnapRegeneratedPaths.IsRegenerated(null));
            Assert.IsFalse(DocSnapRegeneratedPaths.IsRegenerated(""));
        }

        // A file whose name merely mentions the folder is not in it.
        [Test]
        public void DoesNotClassify_APathThatOnlyResemblesOne()
        {
            Assert.IsFalse(DocSnapRegeneratedPaths.IsRegenerated("Assets/Docs/TextMesh Pro notes.md"));
        }

        // ------------------------------------------
        // Path shape
        // ------------------------------------------

        // A pattern typed on Windows has to work on macOS, exactly
        // as the exclude filter promises for its own patterns.
        [Test]
        public void Matching_IsSeparatorAgnostic()
        {
            Assert.IsTrue(DocSnapRegeneratedPaths.IsRegenerated(
                @"Assets\TextMesh Pro\Resources\Fonts & Materials\LiberationSans SDF - Fallback.asset"));
        }

        [Test]
        public void Matching_IsCaseInsensitive()
        {
            Assert.IsTrue(DocSnapRegeneratedPaths.IsRegenerated(
                "assets/textmesh pro/resources/fonts & materials/liberationsans sdf - fallback.asset"));
        }

        // ------------------------------------------
        // Describe — the page shows this beside each file so a
        // reader can check the classification instead of taking
        // it on trust.
        // ------------------------------------------

        [Test]
        public void Describe_NamesThePatternThatMatched()
        {
            string pattern = DocSnapRegeneratedPaths.Describe(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset");
            Assert.AreEqual("Assets/TextMesh Pro/Resources/Fonts & Materials/*.asset", pattern);
        }

        [Test]
        public void Describe_IsEmptyForAnUnclassifiedPath()
        {
            Assert.AreEqual("", DocSnapRegeneratedPaths.Describe("Assets/Scripts/PlayerController.cs"));
            Assert.AreEqual("", DocSnapRegeneratedPaths.Describe(null));
        }

        // ------------------------------------------
        // Project-added patterns
        // ------------------------------------------

        [Test]
        public void ProjectPatterns_AreHonoured()
        {
            DocSnapSettings.SetSessionOverride("regeneratedPaths", "Assets/Generated/*.asset");

            Assert.IsTrue(DocSnapRegeneratedPaths.IsRegenerated("Assets/Generated/atlas.asset"));
            Assert.IsFalse(DocSnapRegeneratedPaths.IsRegenerated("Assets/Generated/atlas.png"));
        }

        [Test]
        public void ProjectPatterns_DoNotReplaceTheBuiltInOnes()
        {
            DocSnapSettings.SetSessionOverride("regeneratedPaths", "Assets/Generated/*.asset");

            Assert.IsTrue(DocSnapRegeneratedPaths.IsRegenerated(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"));
        }

        // The settings field carries comments and blank lines the
        // same way the exclude field does.
        [Test]
        public void ProjectPatterns_IgnoreBlankLinesAndComments()
        {
            DocSnapSettings.SetSessionOverride("regeneratedPaths", "# my own churn\n\nAssets/Generated/*.asset\n");

            var patterns = DocSnapRegeneratedPaths.Patterns();
            CollectionAssert.DoesNotContain(patterns, "# my own churn");
            CollectionAssert.DoesNotContain(patterns, "");
            CollectionAssert.Contains(patterns, "Assets/Generated/*.asset");
        }

        [Test]
        public void ProjectPatterns_AreDeduplicatedAgainstTheBuiltIns()
        {
            DocSnapSettings.SetSessionOverride("regeneratedPaths",
                "Assets/TextMesh Pro/Sprite Assets/*.asset");

            var patterns = DocSnapRegeneratedPaths.Patterns();
            int occurrences = 0;
            foreach (string p in patterns)
            {
                if (p == "Assets/TextMesh Pro/Sprite Assets/*.asset") { occurrences++; }
            }
            Assert.AreEqual(1, occurrences, "a pattern that duplicates a built-in must be listed once");
        }

        // The list is cached against the raw settings string. A
        // stale cache here means a project edits the field and the
        // next export ignores it.
        [Test]
        public void ChangingTheSetting_IsPickedUp()
        {
            Assert.IsFalse(DocSnapRegeneratedPaths.IsRegenerated("Assets/Generated/atlas.asset"));

            DocSnapSettings.SetSessionOverride("regeneratedPaths", "Assets/Generated/*.asset");
            Assert.IsTrue(DocSnapRegeneratedPaths.IsRegenerated("Assets/Generated/atlas.asset"));

            DocSnapSettings.ClearSessionOverrides();
            Assert.IsFalse(DocSnapRegeneratedPaths.IsRegenerated("Assets/Generated/atlas.asset"));
        }
    }
}
