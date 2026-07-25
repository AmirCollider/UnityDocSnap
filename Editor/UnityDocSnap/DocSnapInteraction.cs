// ==========================================
// DocSnapInteraction
// The one place that decides whether a message to
// the user is a modal dialog or a console line.
//
// EditorUtility.DisplayDialog is the right thing for
// someone who just clicked a menu item and the wrong
// thing everywhere else. In -batchmode there is no
// one to click "OK": depending on the Unity version
// the call either returns a default immediately or
// blocks a headless process that nobody can
// interact with, and either way the message - which
// is often the reason the export failed - is never
// seen, because the console is the only output a CI
// log captures.
//
// So every user-facing message in the tool goes
// through here. Interactive runs are unchanged, down
// to the button labels. Automated runs get the same
// text as an ordinary log line, and errors get
// logged as errors so a build annotation picks them
// up.
// ==========================================
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapInteraction
    {
        // ==========================================
        // Silent
        // True whenever nobody is watching: an explicit
        // -batchmode run, or an export driven by DocSnapAPI
        // which sets this for the duration of the call.
        //
        // Application.isBatchMode is 2018.3+; the package
        // floor is 2021.3, so it can be relied on.
        // ==========================================
        private static bool _forcedSilent;

        public static bool Silent
        {
            get { return _forcedSilent || Application.isBatchMode; }
        }

        // ==========================================
        // BeginSilent / EndSilent
        // Scoped by DocSnapAPI so a scripted export never
        // pops a dialog even in a normal, windowed Editor -
        // a script that runs ten exports in a row must not
        // stop ten times waiting for someone to click.
        // ==========================================
        public static void BeginSilent()
        {
            _forcedSilent = true;
        }

        public static void EndSilent()
        {
            _forcedSilent = false;
        }

        // ==========================================
        // Alert
        // A problem the user needs to know about. Modal
        // when someone is there; a console error when not,
        // because that is what a build log keeps.
        // ==========================================
        public static void Alert(string message)
        {
            if (Silent)
            {
                Debug.LogError("[" + DocSnapConstants.ToolName + "] " + message);
                return;
            }
            EditorUtility.DisplayDialog(DocSnapConstants.ToolName, message, "OK");
        }

        // ==========================================
        // Notice
        // Something worth saying that is not a problem -
        // a cancellation, a skipped step.
        // ==========================================
        public static void Notice(string message)
        {
            if (Silent)
            {
                Debug.Log("[" + DocSnapConstants.ToolName + "] " + message);
                return;
            }
            EditorUtility.DisplayDialog(DocSnapConstants.ToolName, message, "OK");
        }

        // ==========================================
        // Confirm
        // A two-button question. Silent callers always get
        // the "no" answer: the only such question in the
        // tool offers to open a folder in the OS file
        // browser, and doing that on a build agent is at
        // best pointless.
        // ==========================================
        public static bool Confirm(string title, string message, string ok, string cancel)
        {
            if (Silent)
            {
                Debug.Log("[" + DocSnapConstants.ToolName + "] " + message);
                return false;
            }
            return EditorUtility.DisplayDialog(title, message, ok, cancel);
        }
    }
}
