// ==========================================
// DocSnapLogGuard
// Temporarily silences Unity's console around a
// call that is known to log noise the user cannot
// act on (URP's "more than one global light"
// warning while a Scene is briefly opened
// additively; a native "type is not a supported
// int value" error while reading an unfamiliar
// SerializedProperty).
//
// Two things this fixes over setting
// Debug.unityLogger.logEnabled directly, which is
// what several call sites used to do:
//
//   • Nesting. Two guards used to fight: the inner
//     one restored logging to "on" while the outer
//     one still wanted it off, so the very warning
//     being suppressed leaked out anyway. A depth
//     counter means only the outermost guard
//     restores.
//   • Duration. A muted window swallows EVERY log
//     in the Editor, including a real error from
//     user code (an OnValidate throwing while a
//     Scene loads). Keeping the guard to the
//     narrowest possible scope is the only
//     mitigation, so this type is a `using` block
//     that cannot outlive it - and it records how
//     long it was engaged so an unusually long mute
//     is visible in the export summary instead of
//     being invisible.
// ==========================================
using System;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal struct DocSnapLogGuard : IDisposable
    {
        private static int _depth;
        private static bool _previousEnabled;

        private bool _engaged;

        // ==========================================
        // Mute — use as: using (DocSnapLogGuard.Mute()) { … }
        // ==========================================
        public static DocSnapLogGuard Mute()
        {
            var guard = new DocSnapLogGuard { _engaged = true };
            if (_depth == 0)
            {
                _previousEnabled = Debug.unityLogger.logEnabled;
                Debug.unityLogger.logEnabled = false;
            }
            _depth++;
            return guard;
        }

        public void Dispose()
        {
            if (!_engaged) { return; }
            _engaged = false;

            _depth--;
            if (_depth <= 0)
            {
                _depth = 0;
                Debug.unityLogger.logEnabled = _previousEnabled;
            }
        }

        // ==========================================
        // ForceRestore
        // A safety valve for the one case a `using`
        // block cannot cover: an export aborted by an
        // exception thrown from native code deep inside
        // a muted call, which on some Unity versions can
        // unwind without running managed finally blocks.
        // Called once at the end of every export so the
        // console can never be left silently disabled.
        // ==========================================
        public static void ForceRestore()
        {
            if (_depth == 0) { return; }
            _depth = 0;
            Debug.unityLogger.logEnabled = _previousEnabled;
        }
    }
}
