// ==========================================
// DocSnapDirectTmp
// Persian and Arabic, drawn correctly in this tool's
// own Editor windows - when the project has Unity
// DirectTMP in it.
//
// THE PROBLEM
// -----------
// Every window in this package is IMGUI, and IMGUI
// hands each character to the font in the order it is
// stored. For English that is the order it is read in
// and nobody has to think about it. For Persian it is
// the opposite order, and the letters additionally
// have to be JOINED - one codepoint per letter goes in
// and one connected word has to come out. Unity does
// neither. TextMeshPro does neither. So a window whose
// language is set to فارسی draws a row of disconnected
// letters running backwards, and DocSnap's Persian
// translation - which is complete, and correct - is
// unreadable in the very window it was written for.
//
// THE FIX, AND WHY IT IS SHAPED LIKE THIS
// ---------------------------------------
// Unity DirectTMP already solves exactly this, and its
// shaping half (DirectDisplayText / DirectArabicShaper
// / DirectBidi) is pure C# with no Unity types in it -
// it was written to be callable from IMGUI. So when
// that package is in the project, this file finds it
// and uses it.
//
// By reflection, and not by an assembly reference,
// because DirectTMP is a SEPARATE package that most
// DocSnap users will not have. A hard reference would
// make this one fail to compile without it, and a
// documentation tool that refuses to build because a
// text package is missing is a worse bug than the one
// being fixed. An asmdef versionDefine would work too
// and costs the user a file to edit; this costs them
// nothing.
//
// Everything is resolved once and cached, including
// the failure: a project without DirectTMP pays one
// assembly scan for the lifetime of the domain and a
// null check per string thereafter.
// ==========================================
using System;
using System.Reflection;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapDirectTmp
    {
        private const string TypeName = "UnityDirectTMP.DirectDisplayText";
        private const string MethodName = "Prepare";

        // DirectBidi.RightToLeft. Passed rather than left to
        // auto-detection because a Persian UI string routinely opens
        // with a Latin word - "Unity DocSnap ▸ …", a version number, a
        // menu path - and auto-detection would read the whole line as
        // an English paragraph and lay it out left to right.
        private const int ForceRightToLeft = 1;

        private static bool _resolved;
        private static Func<string, int, string> _prepare;

        // ==========================================
        // IsAvailable
        // Whether this project has Unity DirectTMP. Used by the
        // windows to tell the reader why their Persian looks the
        // way it does, and what to install if they want it fixed -
        // silence there is how somebody concludes the translation
        // is broken rather than missing a dependency.
        // ==========================================
        public static bool IsAvailable
        {
            get
            {
                Resolve();
                return _prepare != null;
            }
        }

        // ==========================================
        // Prepare
        // The text as it should be DRAWN: letters joined,
        // right-to-left words in reading order.
        //
        // A no-op for every left-to-right language, and a no-op
        // when DirectTMP is not installed - so a call site can wrap
        // every string it draws without asking either question
        // first. The returned string is for display only; it must
        // never be compared, saved, or fed back in.
        // ==========================================
        public static string Prepare(string languageCode, string text)
        {
            if (string.IsNullOrEmpty(text)) { return text; }
            if (!DocSnapLanguages.IsRightToLeft(languageCode)) { return text; }

            Resolve();
            if (_prepare == null) { return text; }

            try
            {
                return _prepare(text, ForceRightToLeft) ?? text;
            }
            catch
            {
                // Display-only text is never worth an exception. A window
                // that draws its Persian unshaped is the state this file
                // exists to improve on; a window that throws while
                // drawing is a window nobody can use at all.
                return text;
            }
        }

        // ==========================================
        // Resolve
        // Finds UnityDirectTMP.DirectDisplayText.Prepare(string,
        // int) in whatever assembly it happens to live in.
        //
        // Type.GetType() is not enough: it only searches the
        // calling assembly and mscorlib unless it is handed an
        // assembly-qualified name, and the name of the assembly
        // DirectTMP compiles into is that package's business, not
        // this one's.
        // ==========================================
        private static void Resolve()
        {
            if (_resolved) { return; }
            _resolved = true;

            try
            {
                Type type = FindType();
                if (type == null) { return; }

                MethodInfo method = type.GetMethod(
                    MethodName,
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string), typeof(int) },
                    null);

                if (method == null || method.ReturnType != typeof(string)) { return; }

                _prepare = (Func<string, int, string>)Delegate.CreateDelegate(
                    typeof(Func<string, int, string>), method, false);
            }
            catch
            {
                _prepare = null;
            }
        }

        private static Type FindType()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type;
                try { type = assembly.GetType(TypeName, false); }
                catch { continue; }

                if (type != null) { return type; }
            }
            return null;
        }

        // Test seam. The resolution is cached for the life of the
        // domain, which is right in the Editor and wrong for a test
        // that wants to assert both branches.
        internal static void InvalidateCache()
        {
            _resolved = false;
            _prepare = null;
        }
    }
}
