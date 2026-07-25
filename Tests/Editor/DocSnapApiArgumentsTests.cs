// ==========================================
// DocSnapApiArgumentsTests
// The -executeMethod command-line parser.
//
// The bug these hold shut: a value that begins with '-'
// is correctly refused - `-docsnapOutput -docsnapUpdate`
// must not create a folder literally called
// "-docsnapUpdate" - but refusing it used to mean
// returning null, which the caller skipped in silence.
// The export then ran to the DEFAULT output folder and
// reported success, so a pipeline pointing its docs at
// Build/Docs published nothing from there and still went
// green.
//
// A parser that silently reinterprets its input is the
// worst kind to leave untested, because every symptom
// appears somewhere other than the parser.
// ==========================================
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public class DocSnapApiArgumentsTests
    {
        [SetUp]
        [TearDown]
        public void ResetOverrides()
        {
            DocSnapSettings.ClearSessionOverrides();
        }

        // The parser reports each malformed argument as a console
        // warning. NUnit fails a test on an unexpected log, so the
        // cases below that expect one say so.
        private static void ExpectWarning()
        {
            LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("needs a value"));
        }

        // ------------------------------------------
        // The happy path
        // ------------------------------------------

        [Test]
        public void Value_ReadsTheArgumentThatFollows()
        {
            bool malformed = false;
            string[] args = { "Unity", "-batchmode", "-docsnapOutput", "Build/Docs" };

            Assert.AreEqual("Build/Docs", DocSnapAPI.Value(args, "-docsnapOutput", ref malformed));
            Assert.IsFalse(malformed);
        }

        // Unity hands back the whole process command line, so an
        // argument this tool does not know must simply not match.
        [Test]
        public void Value_IsNullWhenTheArgumentIsAbsent()
        {
            bool malformed = false;
            string[] args = { "Unity", "-batchmode", "-projectPath", "." };

            Assert.IsNull(DocSnapAPI.Value(args, "-docsnapOutput", ref malformed));
            Assert.IsFalse(malformed, "an argument that was never passed is not an error");
        }

        [Test]
        public void Value_IsCaseInsensitive()
        {
            bool malformed = false;
            string[] args = { "-DOCSNAPOUTPUT", "Build/Docs" };

            Assert.AreEqual("Build/Docs", DocSnapAPI.Value(args, "-docsnapOutput", ref malformed));
            Assert.IsFalse(malformed);
        }

        // ------------------------------------------
        // The bug: malformed input must be loud
        // ------------------------------------------

        [Test]
        public void Value_FollowedByAnotherFlag_IsRefusedAndFlagged()
        {
            ExpectWarning();
            bool malformed = false;
            string[] args = { "-docsnapOutput", "-docsnapUpdate" };

            Assert.IsNull(DocSnapAPI.Value(args, "-docsnapOutput", ref malformed),
                "a folder called \"-docsnapUpdate\" must never be created");
            Assert.IsTrue(malformed, "the run has to be refused, not quietly defaulted");
        }

        // The old loop ran to args.Length - 1, so a flag in the very
        // last position was not examined at all: no value, no
        // warning, no flag, nothing.
        [Test]
        public void Value_AsTheLastArgument_IsRefusedAndFlagged()
        {
            ExpectWarning();
            bool malformed = false;
            string[] args = { "Unity", "-batchmode", "-docsnapOutput" };

            Assert.IsNull(DocSnapAPI.Value(args, "-docsnapOutput", ref malformed));
            Assert.IsTrue(malformed);
        }

        [Test]
        public void Value_FollowedByAnEmptyString_IsRefusedAndFlagged()
        {
            ExpectWarning();
            bool malformed = false;
            string[] args = { "-docsnapOutput", "" };

            Assert.IsNull(DocSnapAPI.Value(args, "-docsnapOutput", ref malformed));
            Assert.IsTrue(malformed);
        }

        // ------------------------------------------
        // Values — the repeatable arguments
        // ------------------------------------------

        [Test]
        public void Values_CollectsEveryOccurrence()
        {
            bool malformed = false;
            string[] args = { "-docsnapScene", "Assets/A.unity", "-docsnapScene", "Assets/B.unity" };

            List<string> found = DocSnapAPI.Values(args, "-docsnapScene", ref malformed);

            CollectionAssert.AreEqual(new[] { "Assets/A.unity", "Assets/B.unity" }, found);
            Assert.IsFalse(malformed);
        }

        [Test]
        public void Values_IsEmptyWhenTheArgumentIsAbsent()
        {
            bool malformed = false;
            string[] args = { "Unity", "-batchmode" };

            CollectionAssert.IsEmpty(DocSnapAPI.Values(args, "-docsnapScene", ref malformed));
            Assert.IsFalse(malformed);
        }

        // One bad occurrence must not be shrugged off just because
        // another one parsed: exporting three of the four scenes
        // somebody asked for, and reporting success, is the same
        // class of silence.
        [Test]
        public void Values_FlagsAMalformedOccurrenceEvenWhenAnotherIsFine()
        {
            ExpectWarning();
            bool malformed = false;
            string[] args = { "-docsnapScene", "Assets/A.unity", "-docsnapScene", "-docsnapUpdate" };

            List<string> found = DocSnapAPI.Values(args, "-docsnapScene", ref malformed);

            CollectionAssert.AreEqual(new[] { "Assets/A.unity" }, found);
            Assert.IsTrue(malformed);
        }

        // ------------------------------------------
        // Flag
        // ------------------------------------------

        [Test]
        public void Flag_FindsAPresentFlagAndMissesAnAbsentOne()
        {
            string[] args = { "Unity", "-batchmode", "-docsnapUpdate" };

            Assert.IsTrue(DocSnapAPI.Flag(args, "-docsnapUpdate"));
            Assert.IsTrue(DocSnapAPI.Flag(args, "-DOCSNAPUPDATE"));
            Assert.IsFalse(DocSnapAPI.Flag(args, "-docsnapWithFiles"));
        }

        // ------------------------------------------
        // Session overrides — the CI-dirties-the-repo fix
        // ------------------------------------------

        // The whole point: a command-line setting changes what this
        // run does without touching the file the project commits.
        [Test]
        public void SessionOverride_WinsOverTheStoredValue()
        {
            DocSnapSettings.SetSessionOverride("outputPath", "Build/Docs");

            Assert.AreEqual("Build/Docs", DocSnapSettings.OutputRootPath);
            Assert.IsTrue(DocSnapSettings.HasSessionOverrides);
        }

        [Test]
        public void ClearingOverrides_RestoresTheStoredValue()
        {
            string original = DocSnapSettings.OutputRootPath;

            DocSnapSettings.SetSessionOverride("outputPath", "Build/Docs");
            Assert.AreEqual("Build/Docs", DocSnapSettings.OutputRootPath);

            DocSnapSettings.ClearSessionOverrides();

            Assert.AreEqual(original, DocSnapSettings.OutputRootPath);
            Assert.IsFalse(DocSnapSettings.HasSessionOverrides);
        }

        // An override must not reach the settings file. This is the
        // assertion that actually encodes "a CI run leaves the
        // working tree clean".
        [Test]
        public void SessionOverride_IsNotWrittenToTheSettingsStore()
        {
            DocSnapSettings.SetSessionOverride("excludePatterns", "Assets/OnlyForThisRun");

            Assert.AreEqual("Assets/OnlyForThisRun", DocSnapSettings.ExcludePatterns);
            Assert.AreNotEqual("Assets/OnlyForThisRun", DocSnapSettings.Store.Get("excludePatterns"),
                "the committed settings file must not have been touched");
        }

        [Test]
        public void SessionOverride_AppliesToBooleanSettingsToo()
        {
            DocSnapSettings.SetSessionOverride("generateThumbnails", "0");
            Assert.IsFalse(DocSnapSettings.GenerateThumbnails);

            DocSnapSettings.SetSessionOverride("embedFonts", "0");
            Assert.IsFalse(DocSnapSettings.EmbedFonts);
        }
    }
}
