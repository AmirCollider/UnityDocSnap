// ==========================================
// DocSnapSiteAssets
// The generated site's static assets (CSS + JS +
// the mascot mark), so the output has zero
// external file dependencies at export time and
// makes zero network requests when opened.
//
// The bytes themselves live in real, editable
// files beside this code - Editor/UnityDocSnap/
// Site~/style.css, app.js, fonts.css, logo.svg -
// and are read through DocSnapSiteFiles. They used
// to be C# verbatim strings in this file, which is
// why it was 98 KB of un-lintable, un-highlightable
// text with every quote written twice, and why it
// carried a "keep these in step with the real
// style.css / app.js" note about files that were
// not in the repository at all. See DocSnapSiteFiles
// for the whole reasoning.
// ==========================================
namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapSiteAssets
    {
        // ==========================================
        // StyleCss - theme/style.css contents.
        //
        // The embedded @font-face rules are prepended to
        // the stylesheet proper, exactly as before, so the
        // single file written into every version folder
        // carries its own fonts and the site is genuinely
        // offline rather than "offline unless you also
        // happen to be online for a font CDN".
        // ==========================================
        public static string StyleCss
        {
            get
            {
                string sheet = DocSnapSiteFiles.Read(DocSnapSiteFiles.StyleCssFile);
                if (!DocSnapSettings.EmbedFonts)
                {
                    // The stylesheet already names the system stack
                    // after every embedded family, so dropping the
                    // @font-face block changes which face renders and
                    // nothing else - no layout shift, no missing
                    // glyphs. See DocSnapSettings.EmbedFonts for why
                    // anyone would want the ~570 KB back.
                    return "/* Embedded web fonts were disabled for this export"
                        + " (Project Settings > Unity DocSnap). The site uses the"
                        + " system font stack instead. */\n" + sheet;
                }
                return DocSnapFontAssets.FontFaceCss + sheet;
            }
        }

        // ==========================================
        // AppJs - theme/app.js contents.
        // ==========================================
        public static string AppJs
        {
            get { return DocSnapSiteFiles.Read(DocSnapSiteFiles.AppJsFile); }
        }

        // ==========================================
        // LogoMarkSvg - the built-in mascot mark, inlined
        // into every page's sidebar when the user has not
        // configured a logo of their own.
        // ==========================================
        public static string LogoMarkSvg
        {
            get { return DocSnapSiteFiles.Read(DocSnapSiteFiles.LogoSvgFile); }
        }
    }
}
