// ==========================================
// DocSnapSiteFilesTests
// The generated site's stylesheet, script and
// fonts are read from real files shipped inside
// the package (Editor/UnityDocSnap/Site~/) rather
// than held in C# string constants.
//
// That swap moved one failure mode from "impossible"
// to "possible": a constant cannot go missing from a
// compiled assembly, but a file can be left out of a
// package, dropped by a hand-copy install, or looked
// for in the wrong place when the tool is installed
// as a UPM package instead of copied into Assets/.
// The symptom would be a completely unstyled export
// and a console error nobody reads, so it is worth a
// test that fails loudly instead.
// ==========================================
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class DocSnapSiteFilesTests
    {
        [Test]
        public void SiteFolder_IsFound()
        {
            Assert.IsFalse(string.IsNullOrEmpty(DocSnapSiteFiles.SiteFolderAbsolute()),
                "The Site~ folder holding the generated site's assets could not be located.");
            Assert.IsTrue(DocSnapSiteFiles.IsAvailable);
        }

        // Each file is checked through the property the exporter
        // actually calls, so a renamed file or a mistyped constant
        // fails here rather than in somebody's export.
        [Test]
        public void StyleCss_CarriesBothTheFontsAndTheStylesheet()
        {
            string css = DocSnapSiteAssets.StyleCss;

            Assert.IsNotEmpty(css);
            StringAssert.Contains("@font-face", css, "The embedded fonts are missing, so the site is not offline-complete.");
            StringAssert.Contains("ds-shell", css, "The stylesheet proper is missing.");
        }

        [Test]
        public void AppJs_IsPresent()
        {
            string js = DocSnapSiteAssets.AppJs;

            Assert.IsNotEmpty(js);
            StringAssert.Contains("DOMContentLoaded", js);
        }

        [Test]
        public void LogoMark_IsAnSvgElement()
        {
            string svg = DocSnapSiteAssets.LogoMarkSvg;

            Assert.IsNotEmpty(svg);
            StringAssert.StartsWith("<svg", svg);
            StringAssert.Contains("</svg>", svg);
        }

        // The fonts are the single largest thing in the package and
        // the whole reason an export renders identically offline. A
        // truncated or placeholder file would still "contain
        // @font-face" while carrying no actual glyphs.
        [Test]
        public void EmbeddedFonts_AreSubstantial()
        {
            Assert.Greater(DocSnapFontAssets.FontFaceCss.Length, 100000,
                "The embedded font CSS is far smaller than expected - it may have been truncated.");
        }

        // Reading is cached per domain reload; asking twice must not
        // produce two different answers.
        [Test]
        public void RepeatedReads_AreStable()
        {
            Assert.AreEqual(DocSnapSiteAssets.AppJs, DocSnapSiteAssets.AppJs);
            Assert.AreEqual(DocSnapSiteAssets.StyleCss.Length, DocSnapSiteAssets.StyleCss.Length);
        }
    }
}
