// ==========================================
// DocSnapRunResult
// What the last export actually did.
//
// Every export entry point returns void and reports
// its outcome by putting a dialog on screen, which
// is exactly right for someone who clicked a menu
// item and completely useless to anything else. A
// build agent running an export needs to know
// whether it worked so it can fail the build if it
// did not, and "a dialog appeared" is not an answer
// it can read - in batch mode nobody is there to see
// the dialog at all.
//
// So each entry point records its outcome here as
// well as showing it, and DocSnapAPI reads it. The
// state is deliberately dumb and single-slot: an
// export is a modal, one-at-a-time operation driven
// by a menu click or a command line, never two at
// once, so there is nothing to key it by.
// ==========================================
namespace AmirCollider.UnityDocSnap.Editor.Export
{
    internal static class DocSnapRunResult
    {
        public static bool Succeeded { get; private set; }
        public static bool Cancelled { get; private set; }
        public static string Message { get; private set; }
        public static string OutputRoot { get; private set; }

        // ==========================================
        // Begin
        // Called first thing by every entry point, so a
        // run that throws before reaching any outcome is
        // reported as a failure rather than as the
        // previous run's success.
        // ==========================================
        public static void Begin()
        {
            Succeeded = false;
            Cancelled = false;
            Message = "";
            OutputRoot = "";
        }

        public static void Fail(string message)
        {
            Succeeded = false;
            Cancelled = false;
            Message = message ?? "";
        }

        // A cancelled export is not a failure - the user asked
        // for it to stop and everything already written is
        // intact - but it is not a success either, and an
        // automated caller has to be able to tell the
        // difference.
        public static void Cancel(string message)
        {
            Succeeded = false;
            Cancelled = true;
            Message = message ?? "";
        }

        public static void Succeed(string message, string outputRoot)
        {
            Succeeded = true;
            Cancelled = false;
            Message = message ?? "";
            OutputRoot = outputRoot ?? "";
        }
    }
}
