// ==========================================
// DocSnapText
// The one place a localised string is chosen.
//
// Every localised string in the tool arrives the same
// way: three inline arguments (English, Japanese,
// Persian) and the language the caller wants. That
// choice used to be made by an inline conditional
// wherever it happened to be needed -
//
//     lang == "ja" ? ja : lang == "fa" ? fa : en
//
// - written out in HtmlPageBuilder.I18n, in
// FieldRenderer's placeholder resolver, in
// IssuesPageRenderer.Localised, in the export window's
// L(), and in the export-complete dialog. Five copies
// of one rule, none of which knew about a fourth
// language and all of which would have needed finding
// to teach it one.
//
// Resolution order, once, here:
//   1. DocSnapTranslations - which covers any language
//      added after the fact, and lets a studio override
//      a built-in string without forking the package.
//   2. The inline argument for one of the three
//      built-in languages.
//   3. English.
//
// Step 1 is skipped entirely while the catalogue is
// empty, which is the shipped state, so the common path
// is the same comparison it always was.
// ==========================================
namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapText
    {
        // ==========================================
        // Resolve
        // The text for `languageCode`, given the three
        // built-in variants written at the call site.
        //
        // A null ja/fa is treated as "not translated" and
        // falls through to English rather than rendering an
        // empty element - a blank label is a bug a reader
        // cannot diagnose, an English one is merely
        // untranslated.
        // ==========================================
        public static string Resolve(string languageCode, string en, string ja, string fa)
        {
            string code = DocSnapLanguages.Normalize(languageCode);

            if (!DocSnapTranslations.IsEmpty)
            {
                string catalogued = DocSnapTranslations.Find(en, code);
                if (!string.IsNullOrEmpty(catalogued)) { return catalogued; }
            }

            if (code == "ja" && !string.IsNullOrEmpty(ja)) { return ja; }
            if (code == "fa" && !string.IsNullOrEmpty(fa)) { return fa; }
            return en ?? "";
        }

        // ==========================================
        // ResolveAll
        // The same string in every registered language, in
        // registry order - what an element carrying one
        // data-<code> attribute per language needs. Built
        // here rather than by the renderer so the renderer
        // never has to know how many languages there are.
        // ==========================================
        public static string[] ResolveAll(string en, string ja, string fa)
        {
            string[] codes = DocSnapLanguages.Codes();
            var values = new string[codes.Length];
            for (int i = 0; i < codes.Length; i++)
            {
                values[i] = Resolve(codes[i], en, ja, fa);
            }
            return values;
        }
    }
}
