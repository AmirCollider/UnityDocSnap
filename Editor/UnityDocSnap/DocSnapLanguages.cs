// ==========================================
// DocSnapLanguages
// The single source of truth for which languages
// Unity DocSnap speaks.
//
// Every one of these facts used to be spelled out
// separately in about a dozen places: the language
// buttons in the sidebar, the pre-paint boot script's
// lang==='fa'?'rtl':'ltr', the export window's
// { "en", "ja", "fa" } popup array and its
// LangIndex switch, DocSnapExportOptions.NormalizeLang,
// the data-en/ja/fa attributes on every localised
// element, and app.js's own RTL map. Adding a fourth
// language therefore meant finding all of them, and
// missing one produced a site that rendered in German
// but laid itself out as though it were still English -
// or a language button that set a code nothing else
// recognised.
//
// Now it means adding one entry to All below, and
// (optionally) the translations themselves to
// DocSnapTranslations. Nothing else in the tool has to
// change: the emitted markup grows a data-<code>
// attribute per language on its own, the switcher grows
// a button, the export window grows a popup entry, and
// anything with no translation yet falls back to
// English rather than rendering blank.
// ==========================================
using System;
using System.Collections.Generic;

namespace AmirCollider.UnityDocSnap.Editor
{
    // ==========================================
    // DocSnapLanguage
    // One language, and everything the rest of the tool
    // needs to know about it.
    //
    //   Code        - the BCP-47 tag. Becomes the <html
    //                 lang>, the data-<code> attribute
    //                 suffix, and the stored setting
    //                 value, so it must stay stable once
    //                 shipped: changing it orphans every
    //                 reader's saved preference.
    //   ButtonLabel - what the site's own switcher shows.
    //                 Short: three buttons sit side by
    //                 side in a narrow sidebar.
    //   EditorName  - what the export window's popup
    //                 shows, where there is room for the
    //                 full name.
    //   RightToLeft - drives dir="rtl" on <html>. Not
    //                 derivable from the code, so it is
    //                 stated.
    // ==========================================
    internal sealed class DocSnapLanguage
    {
        public readonly string Code;
        public readonly string ButtonLabel;
        public readonly string EditorName;
        public readonly bool RightToLeft;

        public DocSnapLanguage(string code, string buttonLabel, string editorName, bool rightToLeft)
        {
            Code = code;
            ButtonLabel = buttonLabel;
            EditorName = editorName;
            RightToLeft = rightToLeft;
        }
    }

    internal static class DocSnapLanguages
    {
        // ==========================================
        // Fallback
        // The language every other one falls back to, and
        // the only one guaranteed to have a translation for
        // every string in the tool. It is also the attribute
        // the site's language sweep keys off ([data-en]), so
        // it cannot simply be renamed.
        // ==========================================
        public const string Fallback = "en";

        // ==========================================
        // All — the registry.
        //
        // TO ADD A LANGUAGE:
        //   1. Add an entry here, in the order the buttons
        //      should appear.
        //   2. Add its rows to DocSnapTranslations (optional -
        //      anything missing falls back to English, so a
        //      half-translated language still ships a working
        //      site).
        // That is the whole procedure. No call site, method
        // signature or renderer changes.
        //
        // English stays first: it is the fallback, and the
        // first entry is what an unrecognised stored code
        // resolves to.
        // ==========================================
        private static readonly DocSnapLanguage[] AllLanguages =
        {
            new DocSnapLanguage("en", "EN", "English", false),
            new DocSnapLanguage("ja", "日本語", "日本語 (Japanese)", false),
            new DocSnapLanguage("fa", "فارسی", "فارسی (Persian)", true)
        };

        public static IList<DocSnapLanguage> All
        {
            get { return Array.AsReadOnly(AllLanguages); }
        }

        public static int Count
        {
            get { return AllLanguages.Length; }
        }

        // ==========================================
        // Find — the language for a code, or null.
        // Comparison is ordinal-insensitive so a settings
        // file that says "EN" still resolves.
        // ==========================================
        public static DocSnapLanguage Find(string code)
        {
            if (string.IsNullOrEmpty(code)) { return null; }
            for (int i = 0; i < AllLanguages.Length; i++)
            {
                if (string.Equals(AllLanguages[i].Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    return AllLanguages[i];
                }
            }
            return null;
        }

        public static bool IsKnown(string code)
        {
            return Find(code) != null;
        }

        // ==========================================
        // Normalize
        // Any stored, typed or command-line value reduced to
        // a code the rest of the tool can rely on. An
        // unrecognised value is not an error worth stopping
        // an export for - it becomes English.
        // ==========================================
        public static string Normalize(string code)
        {
            DocSnapLanguage language = Find(code);
            return language == null ? Fallback : language.Code;
        }

        public static bool IsRightToLeft(string code)
        {
            DocSnapLanguage language = Find(code);
            return language != null && language.RightToLeft;
        }

        // ==========================================
        // Direction — the value for dir="…" on <html>.
        // ==========================================
        public static string Direction(string code)
        {
            return IsRightToLeft(code) ? "rtl" : "ltr";
        }

        // ==========================================
        // IndexOf / CodeAt
        // The bridge to IMGUI popups, which work in indices
        // rather than values. Out-of-range and unknown both
        // resolve to the fallback's index rather than
        // throwing: a settings file is user-editable, and a
        // typo there should not break the window that edits it.
        // ==========================================
        public static int IndexOf(string code)
        {
            for (int i = 0; i < AllLanguages.Length; i++)
            {
                if (string.Equals(AllLanguages[i].Code, code, StringComparison.OrdinalIgnoreCase)) { return i; }
            }
            return 0;
        }

        public static string CodeAt(int index)
        {
            if (index < 0 || index >= AllLanguages.Length) { return Fallback; }
            return AllLanguages[index].Code;
        }

        // ==========================================
        // Codes / EditorNames
        // Flat arrays for the callers that need one -
        // EditorGUILayout.Popup wants string[], and the
        // exported site wants the code list as JSON.
        // ==========================================
        public static string[] Codes()
        {
            var codes = new string[AllLanguages.Length];
            for (int i = 0; i < AllLanguages.Length; i++) { codes[i] = AllLanguages[i].Code; }
            return codes;
        }

        public static string[] EditorNames()
        {
            var names = new string[AllLanguages.Length];
            for (int i = 0; i < AllLanguages.Length; i++) { names[i] = AllLanguages[i].EditorName; }
            return names;
        }

        // ==========================================
        // RightToLeftCodes
        // Handed to the generated site so app.js and the
        // pre-paint boot script decide direction from the
        // same list this file holds, instead of each
        // carrying their own copy of "fa is the RTL one".
        // ==========================================
        public static string[] RightToLeftCodes()
        {
            var found = new List<string>();
            for (int i = 0; i < AllLanguages.Length; i++)
            {
                if (AllLanguages[i].RightToLeft) { found.Add(AllLanguages[i].Code); }
            }
            return found.ToArray();
        }
    }
}
