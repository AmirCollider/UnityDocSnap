// ==========================================
// DocSnapNamingTests
// Covers the rule that used to be missing
// entirely: two different project paths must
// never produce the same output file name.
//
// The bugs these pin down were both silent data
// loss - the second export simply overwrote the
// first and the sidebar showed two entries
// pointing at one page.
// ==========================================
using System.Collections.Generic;
using AmirCollider.UnityDocSnap.Editor;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class DocSnapNamingTests
    {
        // ==========================================
        // Scene keys
        // ==========================================
        [Test]
        public void SceneKey_UniqueName_StaysPlainAndReadable()
        {
            var all = new List<string> { "Assets/Scenes/Main.unity", "Assets/Scenes/Menu.unity" };
            Assert.AreEqual("Main", DocSnapNaming.SceneKey("Assets/Scenes/Main.unity", all));
            Assert.AreEqual("Menu", DocSnapNaming.SceneKey("Assets/Scenes/Menu.unity", all));
        }

        [Test]
        public void SceneKey_DuplicateNames_ResolveToDifferentKeys()
        {
            var all = new List<string> { "Assets/Levels/Main.unity", "Assets/Test/Main.unity" };

            string a = DocSnapNaming.SceneKey("Assets/Levels/Main.unity", all);
            string b = DocSnapNaming.SceneKey("Assets/Test/Main.unity", all);

            Assert.AreNotEqual(a, b, "Two Scenes named Main.unity must not write to the same output file.");
            StringAssert.StartsWith("Main-", a);
            StringAssert.StartsWith("Main-", b);
        }

        [Test]
        public void SceneKey_IsStableAcrossCalls()
        {
            var all = new List<string> { "Assets/Levels/Main.unity", "Assets/Test/Main.unity" };
            Assert.AreEqual(
                DocSnapNaming.SceneKey("Assets/Levels/Main.unity", all),
                DocSnapNaming.SceneKey("Assets/Levels/Main.unity", all),
                "An unstable key would rot every cross-link in a previously exported site.");
        }

        [Test]
        public void SceneKey_UnsafeCharacters_AreReplaced()
        {
            var all = new List<string> { "Assets/Scenes/Level: 1?.unity" };
            string key = DocSnapNaming.SceneKey("Assets/Scenes/Level: 1?.unity", all);

            Assert.IsFalse(key.Contains(":"));
            Assert.IsFalse(key.Contains("?"));
            Assert.IsFalse(key.Contains("/"));
        }

        [Test]
        public void SceneKey_SpacesAndDashes_AreKept()
        {
            var all = new List<string> { "Assets/Scenes/Main Menu - v2.unity" };
            Assert.AreEqual("Main Menu - v2", DocSnapNaming.SceneKey("Assets/Scenes/Main Menu - v2.unity", all));
        }

        // ==========================================
        // Folder keys
        // ==========================================
        [Test]
        public void FolderKey_AssetsRoot_IsAssets()
        {
            Assert.AreEqual("Assets", DocSnapNaming.FolderKey("Assets"));
            Assert.AreEqual("Assets", DocSnapNaming.FolderKey("Assets/"));
        }

        [Test]
        public void FolderKey_OrdinaryPath_KeepsFriendlyFlattenedName()
        {
            Assert.AreEqual("Images_Backgrounds", DocSnapNaming.FolderKey("Assets/Images/Backgrounds"));
        }

        [Test]
        public void FolderKey_SlashAndUnderscore_DoNotCollide()
        {
            // "UI/Menu" and "UI_Menu" both flattened to "UI_Menu",
            // so whichever exported second replaced the first.
            string fromSlash = DocSnapNaming.FolderKey("Assets/UI/Menu");
            string fromUnderscore = DocSnapNaming.FolderKey("Assets/UI_Menu");

            Assert.AreNotEqual(fromSlash, fromUnderscore);
            Assert.AreEqual("UI_Menu", fromSlash, "The unambiguous path should keep the friendly name.");
        }

        [Test]
        public void FolderKey_BackslashesAreNormalised()
        {
            Assert.AreEqual(
                DocSnapNaming.FolderKey("Assets/Art/Props"),
                DocSnapNaming.FolderKey("Assets\\Art\\Props"));
        }

        // ==========================================
        // Sanitising
        // ==========================================
        [Test]
        public void SanitizeFileName_EmptyOrWhitespace_BecomesUnnamed()
        {
            Assert.AreEqual("unnamed", DocSnapNaming.SanitizeFileName(""));
            Assert.AreEqual("unnamed", DocSnapNaming.SanitizeFileName("   "));
            Assert.AreEqual("unnamed", DocSnapNaming.SanitizeFileName(null));
        }

        [Test]
        public void SanitizeFileName_TrailingDot_IsRemoved()
        {
            // Windows silently drops a trailing dot when creating the
            // file, so the name on disk would stop matching the name
            // recorded in the manifest - and every link to it breaks.
            Assert.AreEqual("Scene", DocSnapNaming.SanitizeFileName("Scene."));
        }

        [Test]
        public void SanitizeFileName_ReservedDeviceName_IsEscaped()
        {
            // Matched case-insensitively (Windows does), but the
            // author's own capitalisation is preserved.
            Assert.AreEqual("_CON", DocSnapNaming.SanitizeFileName("CON"));
            Assert.AreEqual("_nul", DocSnapNaming.SanitizeFileName("nul"));
        }

        [Test]
        public void StableHash_IsDeterministic_AndDiffersByInput()
        {
            Assert.AreEqual(DocSnapNaming.StableHashHex("Assets/A"), DocSnapNaming.StableHashHex("Assets/A"));
            Assert.AreNotEqual(DocSnapNaming.StableHashHex("Assets/A"), DocSnapNaming.StableHashHex("Assets/B"));
        }
    }
}
