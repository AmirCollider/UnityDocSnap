// ==========================================
// DocSnapLogGuard
// Suppresses the specific console noise an export
// is known to produce - URP's "more than one global
// light" warning while a Scene is opened additively,
// and the native "type is not a supported int value"
// error a SerializedProperty of an unfamiliar type
// emits while being read.
//
// It used to do that by setting
// Debug.unityLogger.logEnabled = false for the
// duration, which silences EVERY log in the Editor
// while it is engaged - including a genuine
// NullReferenceException thrown from a user's
// OnValidate as the Scene loads, or an error from an
// unrelated background import that happened to land
// in the same window. A documentation tool losing
// the user's own errors is a bad trade for a tidier
// console, and the failure is invisible: the message
// simply never appears, so nobody can even tell it
// happened.
//
// This filters instead. The guard installs an
// ILogHandler that forwards everything to Unity's own
// handler except the handful of messages it can
// positively identify, so anything the tool did not
// come here to hide still reaches the console. What
// it did hide is counted, and the count is available
// for reporting rather than being lost.
//
// Two properties carried over from the old
// implementation because both were load-bearing:
//
//   • Nesting. Two guards used to fight, the inner
//     one restoring while the outer one still wanted
//     the filter on. A depth counter means only the
//     outermost guard restores.
//   • ForceRestore. A native exception can unwind
//     without running managed finally blocks on some
//     Unity versions, so every export ends with an
//     unconditional restore.
// ==========================================
using System;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal struct DocSnapLogGuard : IDisposable
    {
        private static int _depth;
        private static ILogHandler _previousHandler;
        private static FilteringLogHandler _filter;
        private static int _suppressedTotal;

        private bool _engaged;

        // ==========================================
        // SuppressedCount
        // How many messages this export's guards have
        // swallowed. Zero is the normal answer; a large
        // number means the noise list needs revisiting
        // rather than that something is wrong.
        // ==========================================
        public static int SuppressedCount
        {
            get { return _suppressedTotal; }
        }

        // ==========================================
        // ResetCount — called once at the start of an
        // export so the number describes that run.
        // ==========================================
        public static void ResetCount()
        {
            _suppressedTotal = 0;
        }

        // ==========================================
        // Mute — use as: using (DocSnapLogGuard.Mute()) { … }
        // ==========================================
        public static DocSnapLogGuard Mute()
        {
            var guard = new DocSnapLogGuard { _engaged = true };
            if (_depth == 0)
            {
                _previousHandler = Debug.unityLogger.logHandler;
                _filter = new FilteringLogHandler(_previousHandler);
                Debug.unityLogger.logHandler = _filter;
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
                Restore();
            }
        }

        // ==========================================
        // ForceRestore
        // The safety valve for an export aborted by an
        // exception thrown from native code inside a
        // filtered call, which on some Unity versions can
        // unwind without running managed finally blocks.
        // Called at the end of every export so the handler
        // can never be left installed.
        // ==========================================
        public static void ForceRestore()
        {
            if (_depth == 0 && _filter == null) { return; }
            _depth = 0;
            Restore();
        }

        private static void Restore()
        {
            if (_filter != null)
            {
                _suppressedTotal += _filter.Suppressed;
                // Only hand the logger back if nothing else has
                // replaced the handler in the meantime; clobbering
                // someone else's handler would be the same class of
                // bug this type exists to stop causing.
                if (ReferenceEquals(Debug.unityLogger.logHandler, _filter))
                {
                    Debug.unityLogger.logHandler = _previousHandler;
                }
                _filter = null;
            }
            _previousHandler = null;
        }

        // ==========================================
        // FilteringLogHandler
        // Forwards everything to the handler it replaced
        // except the messages this tool provably causes.
        // ==========================================
        private sealed class FilteringLogHandler : ILogHandler
        {
            private readonly ILogHandler _inner;
            public int Suppressed;

            public FilteringLogHandler(ILogHandler inner)
            {
                _inner = inner;
            }

            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                if (IsKnownNoise(logType, format, args))
                {
                    Suppressed++;
                    return;
                }
                _inner.LogFormat(logType, context, format, args);
            }

            // An exception carries a stack trace pointing at real
            // code, and this tool has no way to tell one it caused
            // from one a user's OnValidate threw while a Scene was
            // opening. Never suppressed.
            public void LogException(Exception exception, UnityEngine.Object context)
            {
                _inner.LogException(exception, context);
            }

            // ==========================================
            // IsKnownNoise
            // Deliberately narrow: two specific messages,
            // matched on substrings that identify them
            // without depending on the exact wording of any
            // one Unity version. Anything else - including
            // any Error or Assert - goes straight through.
            // ==========================================
            private static bool IsKnownNoise(LogType logType, string format, object[] args)
            {
                if (logType == LogType.Exception) { return false; }
                if (string.IsNullOrEmpty(format)) { return false; }

                string message = Compose(format, args);
                if (string.IsNullOrEmpty(message)) { return false; }

                // URP/HDRP, while a Scene is briefly opened additively
                // beside one that already has a directional light.
                if (message.IndexOf("more than one", StringComparison.OrdinalIgnoreCase) >= 0
                    && message.IndexOf("light", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                // Native, while a SerializedProperty of a type the
                // reflector has not met is read for its int value.
                if (message.IndexOf("is not a supported", StringComparison.OrdinalIgnoreCase) >= 0
                    && message.IndexOf("value", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                return false;
            }

            // string.Format throws on a format/args mismatch, and a
            // guard that throws while deciding whether to hide a
            // message would turn console noise into a broken export.
            // On any doubt the raw format string is matched instead,
            // which at worst means one message is not recognised.
            private static string Compose(string format, object[] args)
            {
                if (args == null || args.Length == 0) { return format; }
                try { return string.Format(format, args); }
                catch (FormatException) { return format; }
                catch (ArgumentException) { return format; }
            }
        }
    }
}
