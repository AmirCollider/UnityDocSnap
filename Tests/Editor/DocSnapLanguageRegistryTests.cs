// ==========================================
// DocSnapLanguageRegistryTests
// The registry exists so that adding a language is a
// change to one file rather than to a dozen. A test
// that only checked the three shipped languages would
// not notice the day that stopped being true, so the
// interesting tests here REGISTER A FOURTH LANGUAGE
// and assert that the rest of the tool follows it -
// direction, attributes, popup entries and all.
//
// That is the actual promise being made, and it is
// the one that quietly breaks the moment somebody
// writes lang == "fa" somewhere new.
// ==========================================
using System.Collections.Generic;
using AmirCollider.UnityDocSnap.Editor;
using AmirCollider.UnityDocSnap.Editor.Html;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class DocSnapLanguageRegistryTests
    {
        // The catalogue is a static, so a test that writes to it
        // has to put it back or it leaks into whatever runs next.
        [TearDown]
        public void ResetCatalogue()
        {
            DocSnapTranslations.ResetToDefaults();
        }

        // ==========================================
        // The registry itself.
        // ==========================================
        [Test]
        public void EnglishIsTheFallbackAndComesFirst()
        {
            Assert.AreEqual("en", DocSnapLanguages.Fallback);
            Assert.AreEqual("en", DocSnapLanguages.CodeAt(0));
        }

        [Test]
        public void TheThreeShippedLanguagesAreRegistered()
        {
            Assert.IsTrue(DocSnapLanguages.IsKnown("en"));
            Assert.IsTrue(DocSnapLanguages.IsKnown("ja"));
            Assert.IsTrue(DocSnapLanguages.IsKnown("fa"));
        }

        [Test]
        public void AnUnknownCodeNormalisesToEnglishRatherThanThrowing()
        {
            // A settings file is hand-editable and a command line
            // is typed. Neither should be able to break an export.
            Assert.AreEqual("en", DocSnapLanguages.Normalize("de"));
            Assert.AreEqual("en", DocSnapLanguages.Normalize(""));
            Assert.AreEqual("en", DocSnapLanguages.Normalize(null));
        }

        [Test]
        public void ACodeIsRecognisedRegardlessOfCase()
        {
            Assert.AreEqual("ja", DocSnapLanguages.Normalize("JA"));
        }

        [Test]
        public void PersianIsRightToLeftAndTheOthersAreNot()
        {
            Assert.AreEqual("rtl", DocSnapLanguages.Direction("fa"));
            Assert.AreEqual("ltr", DocSnapLanguages.Direction("en"));
            Assert.AreEqual("ltr", DocSnapLanguages.Direction("ja"));
        }

        [Test]
        public void AnUnknownCodeIsLeftToRightRatherThanUndefined()
        {
            Assert.AreEqual("ltr", DocSnapLanguages.Direction("zz"));
        }

        [Test]
        public void CodeAtClampsInsteadOfThrowing()
        {
            // The export window holds a popup index across domain
            // reloads. A registry that changed in between must not
            // turn that into an IndexOutOfRangeException.
            Assert.AreEqual("en", DocSnapLanguages.CodeAt(-1));
            Assert.AreEqual("en", DocSnapLanguages.CodeAt(9999));
        }

        [Test]
        public void IndexOfAndCodeAtAreInverses()
        {
            foreach (DocSnapLanguage language in DocSnapLanguages.All)
            {
                Assert.AreEqual(language.Code, DocSnapLanguages.CodeAt(DocSnapLanguages.IndexOf(language.Code)));
            }
        }

        [Test]
        public void EveryRegisteredLanguageHasTheLabelsTheUiNeeds()
        {
            // A language with no button label renders an empty
            // button; one with no editor name renders an empty
            // popup row. Both are the kind of thing that is only
            // noticed by the person who cannot read the site.
            foreach (DocSnapLanguage language in DocSnapLanguages.All)
            {
                Assert.IsNotEmpty(language.Code, "language code");
                Assert.IsNotEmpty(language.ButtonLabel, "button label for " + language.Code);
                Assert.IsNotEmpty(language.EditorName, "editor name for " + language.Code);
            }
        }

        [Test]
        public void CodesAndEditorNamesLineUpWithTheRegistry()
        {
            // The export window pairs these two arrays by index.
            string[] codes = DocSnapLanguages.Codes();
            string[] names = DocSnapLanguages.EditorNames();

            Assert.AreEqual(DocSnapLanguages.Count, codes.Length);
            Assert.AreEqual(codes.Length, names.Length);
        }

        [Test]
        public void CodesAreUnique()
        {
            var seen = new HashSet<string>();
            foreach (DocSnapLanguage language in DocSnapLanguages.All)
            {
                Assert.IsTrue(seen.Add(language.Code.ToLowerInvariant()), "duplicate language code: " + language.Code);
            }
        }

        // ==========================================
        // Resolution.
        // ==========================================
        [Test]
        public void ResolvePicksTheRequestedLanguage()
        {
            Assert.AreEqual("Scenes", DocSnapText.Resolve("en", "Scenes", "シーン", "سین‌ها"));
            Assert.AreEqual("シーン", DocSnapText.Resolve("ja", "Scenes", "シーン", "سین‌ها"));
            Assert.AreEqual("سین‌ها", DocSnapText.Resolve("fa", "Scenes", "シーン", "سین‌ها"));
        }

        [Test]
        public void ResolveFallsBackToEnglishForAnUntranslatedString()
        {
            // A blank label is a bug a reader cannot diagnose; an
            // English one is merely untranslated.
            Assert.AreEqual("Scenes", DocSnapText.Resolve("ja", "Scenes", null, null));
            Assert.AreEqual("Scenes", DocSnapText.Resolve("fa", "Scenes", "シーン", ""));
        }

        [Test]
        public void ResolveFallsBackToEnglishForAnUnknownLanguage()
        {
            Assert.AreEqual("Scenes", DocSnapText.Resolve("de", "Scenes", "シーン", "سین‌ها"));
        }

        // ==========================================
        // The catalogue - the extension point.
        // ==========================================
        [Test]
        public void TheShippedCatalogueIsEmpty()
        {
            // The three built-in languages live at their call
            // sites. If this ever fails, a string is defined in
            // two places and the two can disagree.
            Assert.IsTrue(DocSnapTranslations.IsEmpty);
        }

        [Test]
        public void ACataloguedTranslationIsUsed()
        {
            DocSnapTranslations.Register("Scenes", "ja", "シーン一覧");

            Assert.AreEqual("シーン一覧", DocSnapText.Resolve("ja", "Scenes", "シーン", "سین‌ها"));
        }

        [Test]
        public void ACatalogueMissAtRuntimeStillFallsBackToTheInlineText()
        {
            DocSnapTranslations.Register("Assets", "ja", "アセット一覧");

            Assert.AreEqual("シーン", DocSnapText.Resolve("ja", "Scenes", "シーン", "سین‌ها"));
        }

        [Test]
        public void RegisteringIsIdempotentAndLastWriteWins()
        {
            DocSnapTranslations.Register("Scenes", "ja", "first");
            DocSnapTranslations.Register("Scenes", "ja", "second");

            Assert.AreEqual("second", DocSnapTranslations.Find("Scenes", "ja"));
        }

        [Test]
        public void ResetToDefaultsClearsRuntimeRegistrations()
        {
            DocSnapTranslations.Register("Scenes", "ja", "temporary");
            DocSnapTranslations.ResetToDefaults();

            Assert.IsNull(DocSnapTranslations.Find("Scenes", "ja"));
            Assert.IsTrue(DocSnapTranslations.IsEmpty);
        }

        // ==========================================
        // What the markup does with all of the above.
        // ==========================================
        [Test]
        public void EveryRegisteredLanguageGetsItsOwnDataAttribute()
        {
            string html = HtmlPageBuilder.I18n("span", null, "Scenes", "シーン", "سین‌ها");

            foreach (DocSnapLanguage language in DocSnapLanguages.All)
            {
                StringAssert.Contains("data-" + language.Code + "=\"", html);
            }
        }

        [Test]
        public void ALanguageAddedToTheCatalogueReachesTheMarkup()
        {
            // The point of the whole exercise: a translation
            // supplied without touching the renderer shows up in
            // what the renderer emits.
            DocSnapTranslations.Register("Scenes", "ja", "シーン一覧");

            string html = HtmlPageBuilder.I18n("span", null, "Scenes", "シーン", "سین‌ها");

            StringAssert.Contains("data-ja=\"シーン一覧\"", html);
        }

        [Test]
        public void LocalisedTextIsEscapedInEveryLanguage()
        {
            // The values become attribute values. A quote or an
            // angle bracket in any one of them must not be able
            // to end the attribute it sits in.
            string html = HtmlPageBuilder.I18n("span", null, "a\"b", "<c>", "d&e");

            StringAssert.Contains("&quot;", html);
            StringAssert.Contains("&lt;c&gt;", html);
            StringAssert.Contains("&amp;", html);
            StringAssert.DoesNotContain("<c>", html);
        }

        [Test]
        public void PlaceholderAttributesCoverEveryRegisteredLanguage()
        {
            string attributes = HtmlPageBuilder.I18nAttributes("data-ph-", "Search", "検索", "جستجو");

            foreach (DocSnapLanguage language in DocSnapLanguages.All)
            {
                StringAssert.Contains("data-ph-" + language.Code + "=\"", attributes);
            }
            // Emitted mid-tag, so it has to bring its own
            // separating space.
            StringAssert.StartsWith(" ", attributes);
        }

        // ==========================================
        // What the page hands to app.js.
        // ==========================================
        [Test]
        public void TheRightToLeftListIsEmittedAsAJavaScriptArray()
        {
            string js = HtmlPageBuilder.JsStringArray(DocSnapLanguages.RightToLeftCodes());

            StringAssert.Contains("\"fa\"", js);
            StringAssert.DoesNotContain("\"en\"", js);
        }

        [Test]
        public void AnEmptyArrayIsStillValidJavaScript()
        {
            Assert.AreEqual("[]", HtmlPageBuilder.JsStringArray(new string[0]));
            Assert.AreEqual("[]", HtmlPageBuilder.JsStringArray(null));
        }

        [Test]
        public void ArrayValuesAreEscapedForTheScriptTheyLandIn()
        {
            string js = HtmlPageBuilder.JsStringArray(new[] { "</script>" });

            StringAssert.DoesNotContain("</script>", js);
            StringAssert.Contains("\\u003c", js);
        }

        [Test]
        public void AnEmptyCatalogueIsEmittedAsAnEmptyObject()
        {
            Assert.AreEqual("{}", HtmlPageBuilder.TranslationCatalogueJson());
        }

        [Test]
        public void ThePopulatedCatalogueIsEmittedForTheSiteScript()
        {
            DocSnapTranslations.Register("No matches", "ja", "ヒットなし");

            string js = HtmlPageBuilder.TranslationCatalogueJson();

            StringAssert.Contains("\"No matches\"", js);
            StringAssert.Contains("\"ja\"", js);
            StringAssert.Contains("\"ヒットなし\"", js);
        }

        [Test]
        public void CatalogueKeysAreEscapedForTheScriptTheyLandIn()
        {
            DocSnapTranslations.Register("</script>", "ja", "x");

            string js = HtmlPageBuilder.TranslationCatalogueJson();

            StringAssert.DoesNotContain("</script>", js);
        }
    }
}
