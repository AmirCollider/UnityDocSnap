// ==========================================
// DocSnapTranslations
// Where a language added after the fact gets its
// words, without touching a single renderer.
//
// The tool's own three languages are written inline at
// the call site - HtmlPageBuilder.I18n("span", null,
// "Scenes", "シーン", "سین‌ها") - which is genuinely the
// nicest way to read a renderer: the English, the
// Japanese and the Persian sit together in the line
// that emits them, and none of them can drift out of
// sync with the markup around it.
//
// What that shape cannot do is grow. A fourth language
// has nowhere to live: every call site would need a
// fourth argument, which is a change to ~140 lines
// across nine files and every helper signature between
// them, and until all of it is done the package does
// not compile.
//
// So this file is the other half. It is a catalogue
// keyed by the English string - the same identity
// gettext has used for forty years - and it is
// consulted for any language that is not one of the
// three written inline. Adding German is:
//
//   1. DocSnapLanguages.All  += new DocSnapLanguage("de", "DE", "Deutsch", false)
//   2. Register("Scenes",     "de", "Szenen");
//      Register("Assets",     "de", "Assets");
//      …one line per string you want translated.
//
// Nothing else. Strings you have not translated yet
// fall back to English, so a half-finished language
// still produces a complete, working site - and you can
// ship the language on day one and translate the long
// tail later.
//
// Keying by the English text means two identical
// English strings share one translation. For a
// documentation tool that is the right trade: the
// alternative is inventing an ID for all ~140 of them,
// and an ID that has to be kept in sync with the string
// beside it is one more thing to get wrong.
// ==========================================
using System;
using System.Collections.Generic;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapTranslations
    {
        // english → (language code → translated text)
        private static readonly Dictionary<string, Dictionary<string, string>> Rows =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        static DocSnapTranslations()
        {
            LoadDefaults();
        }

        // ==========================================
        // LoadDefaults
        // The shipped catalogue. Empty today: the three
        // languages Unity DocSnap speaks are written inline
        // at their call sites, so there is nothing here to
        // duplicate. Add a language's rows below.
        //
        //   Register("Scenes", "de", "Szenen");
        //
        // Keep the English argument byte-identical to the
        // string in the renderer - it is the key. A row whose
        // English does not match any call site is dead
        // weight rather than an error, which is why
        // DocSnapTranslationsTests checks the shipped rows
        // against the strings the renderers actually emit.
        // ==========================================
        private static void LoadDefaults()
        {
        }

        // ==========================================
        // Register
        // Adds or replaces one translation. Public so a
        // studio that wants its own language can call this
        // from an [InitializeOnLoad] class in its own Editor
        // assembly without forking the package.
        //
        // A code that DocSnapLanguages does not know is
        // accepted and stored: registration order against
        // the registry is not something a caller should have
        // to reason about, and a row for a language nobody
        // asks for is simply never read.
        // ==========================================
        public static void Register(string english, string languageCode, string translated)
        {
            if (string.IsNullOrEmpty(english) || string.IsNullOrEmpty(languageCode)) { return; }
            if (translated == null) { return; }

            Dictionary<string, string> byCode;
            if (!Rows.TryGetValue(english, out byCode))
            {
                byCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Rows[english] = byCode;
            }
            byCode[languageCode] = translated;
        }

        // ==========================================
        // Find
        // The translation for one English string in one
        // language, or null when there is none - the caller
        // falls back to English rather than rendering blank.
        // ==========================================
        public static string Find(string english, string languageCode)
        {
            if (string.IsNullOrEmpty(english) || string.IsNullOrEmpty(languageCode)) { return null; }

            Dictionary<string, string> byCode;
            if (!Rows.TryGetValue(english, out byCode)) { return null; }

            string translated;
            return byCode.TryGetValue(languageCode, out translated) ? translated : null;
        }

        public static bool IsEmpty
        {
            get { return Rows.Count == 0; }
        }

        // ==========================================
        // EnglishKeys
        // Every English string the catalogue has a row for.
        // Used to hand the catalogue to the generated site,
        // where app.js needs the same lookups for the
        // strings it builds itself (search results, the
        // "copied" toast) rather than receives as markup.
        // ==========================================
        public static List<string> EnglishKeys()
        {
            return new List<string>(Rows.Keys);
        }

        // ==========================================
        // CodesFor — which languages a given row covers.
        // ==========================================
        public static List<string> CodesFor(string english)
        {
            var codes = new List<string>();
            if (string.IsNullOrEmpty(english)) { return codes; }

            Dictionary<string, string> byCode;
            if (Rows.TryGetValue(english, out byCode)) { codes.AddRange(byCode.Keys); }
            return codes;
        }

        // ==========================================
        // ResetToDefaults
        // Drops anything registered at runtime and reloads
        // the shipped rows. Exists for tests, which would
        // otherwise leak registrations into each other
        // through this static.
        // ==========================================
        internal static void ResetToDefaults()
        {
            Rows.Clear();
            LoadDefaults();
        }
    }
}
