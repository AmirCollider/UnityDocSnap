// ==========================================
// DocSnapFontAssets
// The site's branded web fonts (Baloo 2, Quicksand and
// Space Mono for Latin; Vazirmatn for Arabic / Persian),
// embedded as base64 woff2 @font-face rules so the
// generated documentation site renders its full look with
// ZERO network requests - it is genuinely offline, not
// "offline unless you happen to also be online for the
// Google Fonts CDN". Only the font weights the stylesheet
// actually uses are included, and each face keeps its
// original unicode-range so the browser still falls
// through to the next family per glyph.
//
// Japanese (Kosugi Maru) is deliberately NOT embedded: a
// full CJK face is ~2 MB and would be baked into every
// version folder of every export. Japanese text instead
// uses a high-quality system font stack (see the
// :lang(ja) rule in Site~/style.css) that every desktop
// OS already ships with.
//
// The rules themselves live in Site~/fonts.css. Holding
// ~570 KB of base64 in a C# const made this one source
// file larger than the entire rest of the tool put
// together, for bytes no C# tool would ever read.
// ==========================================
namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapFontAssets
    {
        // ==========================================
        // FontFaceCss - the @font-face rules, prepended
        // to DocSnapSiteAssets.StyleCss so theme/style.css
        // carries its own fonts.
        // ==========================================
        public static string FontFaceCss
        {
            get { return DocSnapSiteFiles.Read(DocSnapSiteFiles.FontsCssFile); }
        }
    }
}
