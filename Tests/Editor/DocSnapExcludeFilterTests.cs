// ==========================================
// DocSnapExcludeFilterTests
// The exclude rules decide what a whole export
// does and does not contain, so the matcher gets
// pinned down properly: a rule that quietly
// matches too much removes a project's real
// content from its own documentation, and one
// that matches too little leaves the user
// wondering why their setting did nothing.
// ==========================================
using AmirCollider.UnityDocSnap.Editor;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class DocSnapExcludeFilterTests
    {
        [Test]
        public void Empty_ExcludesNothing()
        {
            DocSnapExcludeFilter filter = DocSnapExcludeFilter.Parse("");
            Assert.IsTrue(filter.IsEmpty);
            Assert.IsFalse(filter.IsExcluded("Assets/Anything.cs"));
        }

        [Test]
        public void PrefixRule_MatchesFolderAndContents()
        {
            DocSnapExcludeFilter filter = DocSnapExcludeFilter.Parse("Assets/Plugins");

            Assert.IsTrue(filter.IsExcluded("Assets/Plugins"));
            Assert.IsTrue(filter.IsExcluded("Assets/Plugins/Vendor/Thing.cs"));
        }

        [Test]
        public void PrefixRule_DoesNotMatchASiblingWithTheSamePrefix()
        {
            // "Assets/Plugins" must not swallow "Assets/PluginsOfMine".
            // Matching on a bare StartsWith would, and would delete a
            // real folder from the documentation without a word.
            DocSnapExcludeFilter filter = DocSnapExcludeFilter.Parse("Assets/Plugins");

            Assert.IsFalse(filter.IsExcluded("Assets/PluginsOfMine/Thing.cs"));
            Assert.IsFalse(filter.IsExcluded("Assets/PluginsExtra"));
        }

        [Test]
        public void WildcardRule_MatchesByExtension()
        {
            DocSnapExcludeFilter filter = DocSnapExcludeFilter.Parse("*.psd");

            Assert.IsTrue(filter.IsExcluded("Assets/Art/Hero.psd"));
            Assert.IsFalse(filter.IsExcluded("Assets/Art/Hero.png"));
        }

        [Test]
        public void WildcardRule_MatchesMidPath()
        {
            DocSnapExcludeFilter filter = DocSnapExcludeFilter.Parse("Assets/Art/*/Raw");

            Assert.IsTrue(filter.IsExcluded("Assets/Art/Characters/Raw"));
            Assert.IsFalse(filter.IsExcluded("Assets/Art/Characters/Final"));
        }

        [Test]
        public void Matching_IsCaseInsensitiveAndSeparatorAgnostic()
        {
            DocSnapExcludeFilter filter = DocSnapExcludeFilter.Parse("assets\\plugins");
            Assert.IsTrue(filter.IsExcluded("Assets/Plugins/Thing.cs"));
        }

        [Test]
        public void Parse_IgnoresBlankLinesCommentsAndDuplicates()
        {
            DocSnapExcludeFilter filter = DocSnapExcludeFilter.Parse("Assets/A\n\n# a comment\nAssets/A\nAssets/B");

            Assert.AreEqual(2, filter.Patterns.Count);
            Assert.Contains("Assets/A", filter.Patterns);
            Assert.Contains("Assets/B", filter.Patterns);
        }

        [Test]
        public void Parse_AcceptsSemicolonAndCommaSeparators()
        {
            DocSnapExcludeFilter filter = DocSnapExcludeFilter.Parse("Assets/A; Assets/B, Assets/C");
            Assert.AreEqual(3, filter.Patterns.Count);
        }

        [Test]
        public void WildcardMatch_HandlesConsecutiveStarsWithoutBlowingUp()
        {
            Assert.IsTrue(DocSnapExcludeFilter.WildcardMatch("Assets/a/b/c.psd", "**c.psd"));
            Assert.IsTrue(DocSnapExcludeFilter.WildcardMatch("aaaaaaaaaaaaaaaaaaaab", "*a*a*a*a*b"));
            Assert.IsFalse(DocSnapExcludeFilter.WildcardMatch("aaaaaaaaaaaaaaaaaaaa", "*a*a*a*a*b"));
        }

        [Test]
        public void WildcardMatch_QuestionMarkMatchesExactlyOneCharacter()
        {
            Assert.IsTrue(DocSnapExcludeFilter.WildcardMatch("Level1", "Level?"));
            Assert.IsFalse(DocSnapExcludeFilter.WildcardMatch("Level12", "Level?"));
        }
    }
}
