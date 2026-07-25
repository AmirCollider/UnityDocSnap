// ==========================================
// DocSnapSettingsStoreTests
// The store behind ProjectSettings/
// UnityDocSnapSettings.json, which is a file meant
// to be committed, reviewed in a pull request and
// read by CI. Three properties make it useful and
// all three are easy to lose:
//
//   • It survives a round trip. A settings file that
//     cannot be read back is worse than none - the
//     project silently reverts to defaults and the
//     exclude list a team agreed on stops applying.
//   • It produces a clean diff. Keys have to keep
//     their order and an unchanged save has to be a
//     no-op, or every person who opens the settings
//     window commits a reordered file and the real
//     changes stop being visible in review.
//   • It never destroys what it cannot parse. A
//     hand-edited file with a typo must be reported,
//     not silently overwritten with defaults.
//
// The store takes its path as a constructor
// argument precisely so this can run against a temp
// folder with no Unity project involved.
// ==========================================
using System.IO;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class DocSnapSettingsStoreTests
    {
        private string _dir;
        private string _file;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "DocSnapSettingsTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_dir);
            _file = Path.Combine(_dir, "UnityDocSnapSettings.json");
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_dir)) { Directory.Delete(_dir, true); } }
            catch { /* a temp folder that outlives one test run is harmless */ }
        }

        private DocSnapSettingsStore NewStore()
        {
            return new DocSnapSettingsStore(_file);
        }

        // ------------------------------------------
        // Reading and writing
        // ------------------------------------------

        [Test]
        public void MissingFile_IsNotAnError()
        {
            DocSnapSettingsStore store = NewStore();
            Assert.IsNull(store.Get("excludePatterns"));
            Assert.IsNull(store.LastError, "a first run has no settings file and that is normal");
        }

        [Test]
        public void Set_WritesTheFile()
        {
            NewStore().Set("excludePatterns", "Assets/Plugins");
            Assert.IsTrue(File.Exists(_file));
        }

        [Test]
        public void Set_ThenReadBackFromANewStore_RoundTrips()
        {
            NewStore().Set("excludePatterns", "Assets/Plugins\nAssets/ThirdParty");
            Assert.AreEqual("Assets/Plugins\nAssets/ThirdParty", NewStore().Get("excludePatterns"));
        }

        [Test]
        public void RoundTrip_SurvivesQuotesBackslashesAndNonLatinText()
        {
            // Every one of these is a legal Unity folder name.
            DocSnapSettingsStore store = NewStore();
            store.Set("excludePatterns", "Assets/\"quoted\"\nAssets\\win\nAssets/تصاویر\nAssets/画像");
            Assert.AreEqual("Assets/\"quoted\"\nAssets\\win\nAssets/تصاویر\nAssets/画像", NewStore().Get("excludePatterns"));
        }

        [Test]
        public void WrittenFile_CarriesAFormatVersion()
        {
            NewStore().Set("siteSkin", "cozy");
            StringAssert.Contains("formatVersion", File.ReadAllText(_file));
        }

        // ------------------------------------------
        // Diff friendliness
        // ------------------------------------------

        [Test]
        public void Keys_KeepTheirInsertionOrderAcrossSaves()
        {
            DocSnapSettingsStore store = NewStore();
            store.Set("zebra", "1");
            store.Set("apple", "2");
            store.Set("mango", "3");

            string text = File.ReadAllText(_file);
            int zebra = text.IndexOf("zebra", System.StringComparison.Ordinal);
            int apple = text.IndexOf("apple", System.StringComparison.Ordinal);
            int mango = text.IndexOf("mango", System.StringComparison.Ordinal);

            Assert.Less(zebra, apple, "insertion order, not alphabetical");
            Assert.Less(apple, mango);
        }

        [Test]
        public void SettingTheSameValueAgain_DoesNotRewriteTheFile()
        {
            DocSnapSettingsStore store = NewStore();
            store.Set("siteSkin", "cozy");
            string first = File.ReadAllText(_file);
            File.WriteAllText(_file, first + "\n// touched\n");

            store.Set("siteSkin", "cozy");

            // Unity's settings providers call setters on every
            // repaint. If an identical value rewrote the file, the
            // file's contents and timestamp would churn constantly.
            StringAssert.Contains("// touched", File.ReadAllText(_file));
        }

        [Test]
        public void SettingADifferentValue_DoesRewriteTheFile()
        {
            DocSnapSettingsStore store = NewStore();
            store.Set("siteSkin", "cozy");
            store.Set("siteSkin", "lite");
            Assert.AreEqual("lite", NewStore().Get("siteSkin"));
        }

        // ------------------------------------------
        // Migration
        // ------------------------------------------

        [Test]
        public void ImportMissing_AdoptsAValueTheStoreDoesNotHave()
        {
            DocSnapSettingsStore store = NewStore();
            Assert.IsTrue(store.ImportMissing("excludePatterns", "Assets/Plugins"));
            Assert.AreEqual("Assets/Plugins", store.Get("excludePatterns"));
        }

        [Test]
        public void ImportMissing_NeverOverwritesACommittedValue()
        {
            NewStore().Set("excludePatterns", "Assets/Ours");

            DocSnapSettingsStore store = NewStore();
            Assert.IsFalse(store.ImportMissing("excludePatterns", "Assets/TheirLocalGuess"));
            Assert.AreEqual("Assets/Ours", store.Get("excludePatterns"),
                "the repository's committed answer has to win over one user's local leftovers");
        }

        [Test]
        public void ImportMissing_IgnoresEmptyValues()
        {
            DocSnapSettingsStore store = NewStore();
            Assert.IsFalse(store.ImportMissing("excludePatterns", ""));
            Assert.IsFalse(store.ImportMissing("excludePatterns", null));
            Assert.IsNull(store.Get("excludePatterns"));
        }

        // ------------------------------------------
        // Damaged files
        // ------------------------------------------

        [Test]
        public void CorruptFile_IsReportedAndNotSilentlyReplaced()
        {
            File.WriteAllText(_file, "{ this is not json");

            DocSnapSettingsStore store = NewStore();
            store.Load();

            Assert.IsNotNull(store.LastError, "a broken settings file has to be said out loud");
            Assert.IsNull(store.Get("excludePatterns"), "and must fall back to defaults");
            StringAssert.Contains("this is not json", File.ReadAllText(_file),
                "someone hand-edited and committed this file; overwriting it would destroy their work");
        }

        [Test]
        public void CorruptFile_IsForgottenOnceItIsFixed()
        {
            File.WriteAllText(_file, "{ broken");
            DocSnapSettingsStore store = NewStore();
            store.Load();
            Assert.IsNotNull(store.LastError);

            File.WriteAllText(_file, "{\"excludePatterns\": \"Assets/Plugins\"}");
            store.Load();

            Assert.IsNull(store.LastError, "a reload after the fix must not keep the old complaint");
            Assert.AreEqual("Assets/Plugins", store.Get("excludePatterns"));
        }

        [Test]
        public void UnknownKeys_AreLeftAlone()
        {
            // A newer version of the tool, or a hand-added note,
            // must not be dropped by an older one saving the file.
            File.WriteAllText(_file, "{\"formatVersion\": 1, \"siteSkin\": \"cozy\", \"somethingNewer\": \"keep me\"}");

            DocSnapSettingsStore store = NewStore();
            store.Set("siteSkin", "lite");

            Assert.AreEqual("keep me", NewStore().Get("somethingNewer"));
        }
    }
}
