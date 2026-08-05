// ==========================================
// DocSnapEditorText
// The one call an Editor window makes to get a
// localised string it can actually DRAW.
//
// DocSnapText.Resolve answers "which language", which
// is the whole question for the exported site: a
// browser joins Arabic letters and reorders
// right-to-left runs on its own, so the HTML wants the
// string exactly as it was written.
//
// An IMGUI window is the opposite. Nothing in Unity
// shapes or reorders anything, so the same string that
// renders perfectly in the exported site renders
// backwards and unjoined in the window that produced
// it. The two callers want different bytes for the
// same text, which is why this is a second entry point
// rather than a change to the first.
//
// The shaping itself comes from Unity DirectTMP when
// the project has it (see DocSnapDirectTmp); without
// it this is DocSnapText.Resolve and one comparison.
// ==========================================
namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapEditorText
    {
        // ==========================================
        // L
        // The localised string, ready to hand to a GUILayout
        // label, a GUIContent, a HelpBox or a dialog.
        //
        // Display only. It is not the string to compare against,
        // store in settings, or write into an export - shaping
        // replaces letters with presentation forms and reordering
        // permutes them, and a value that has been through either
        // is a picture of the text rather than the text.
        // ==========================================
        public static string L(string languageCode, string en, string ja, string fa)
        {
            return DocSnapDirectTmp.Prepare(languageCode, DocSnapText.Resolve(languageCode, en, ja, fa));
        }

        // ==========================================
        // Draw
        // One already-resolved string, prepared for drawing.
        // For the call sites that build their text rather than
        // choosing it - a count spliced into a sentence, a
        // headline assembled elsewhere.
        // ==========================================
        public static string Draw(string languageCode, string text)
        {
            return DocSnapDirectTmp.Prepare(languageCode, text);
        }

        // ==========================================
        // Native
        // The localised string for a surface the PLATFORM draws,
        // rather than IMGUI: an EditorUtility dialog, a
        // GenericMenu item, a window title.
        //
        // Those go through the operating system's own text stack,
        // which joins Arabic letters and reorders right-to-left
        // runs by itself and always has. Handing them text this
        // package already prepared does the work twice: the
        // reordering is undone, and the presentation forms that
        // replaced the letters stay behind. The result is the
        // mangled line that looks far more broken than the problem
        // being fixed - and it is the reason a dialog that read
        // perfectly before shaping was introduced stopped reading
        // at all afterwards.
        //
        // So it is not that these surfaces are skipped. They were
        // never ours to prepare.
        // ==========================================
        public static string Native(string languageCode, string en, string ja, string fa)
        {
            return DocSnapText.Resolve(languageCode, en, ja, fa);
        }

        // ==========================================
        // NeedsDirectTmp
        // True when the window is showing a right-to-left
        // language and nothing in the project can draw it
        // properly.
        //
        // Windows use this to say so once, with a link, instead
        // of leaving somebody to conclude that the Persian
        // translation is broken. It is a real difference between
        // "not translated" and "translated, and your Editor
        // cannot draw it" - and only one of them is something
        // the reader can fix.
        // ==========================================
        public static bool NeedsDirectTmp(string languageCode)
        {
            return DocSnapLanguages.IsRightToLeft(languageCode) && !DocSnapDirectTmp.IsAvailable;
        }
    }
}
