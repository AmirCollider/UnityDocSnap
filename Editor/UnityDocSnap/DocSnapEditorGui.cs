// ==========================================
// DocSnapEditorGui
// Editor controls that draw Persian correctly on BOTH
// halves of themselves.
//
// A Unity popup is two different renderers wearing one
// control's name:
//
//   the CLOSED field   is drawn by IMGUI, which hands
//     every character to the font in the order it is
//     stored and joins nothing. Persian has to arrive
//     here already shaped and already reordered, or it
//     comes out as disconnected letters running
//     backwards.
//
//   the OPEN list      is drawn by the platform's own
//     menu, which has a real text stack behind it and
//     shapes and reorders on its own. Persian has to
//     arrive here EXACTLY AS TYPED. Text that was
//     prepared for the field gets prepared a second
//     time and comes out mangled - reversed back to
//     front, with presentation forms where letters
//     should be.
//
// EditorGUILayout.Popup takes one string array and uses
// it for both, so one array cannot be right in both
// places. Whichever form is passed, exactly one half of
// the control is wrong - which is why fixing the
// dropdown breaks the field and fixing the field breaks
// the dropdown.
//
// So the control is drawn as its two halves: a button
// wearing the popup style, with prepared text; and a
// GenericMenu, with raw text. The same rule decides
// every other surface in the tool - see
// DocSnapEditorText.Native for the list of things the
// platform draws for us.
// ==========================================
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapEditorGui
    {
        // A GenericMenu answers asynchronously - the callback runs
        // after OnGUI has returned - so a choice is parked here and
        // collected by the next pass of the control that made it.
        // Keyed by a caller-supplied name rather than by a control
        // ID, because a control ID is only stable while the layout
        // is, and this window's layout changes with its own options.
        private static readonly Dictionary<string, int> _pending = new Dictionary<string, int>();

        // ==========================================
        // Popup
        // The value to keep, which is `selected` until the reader
        // has picked something else.
        //
        // `options` are the RAW strings. Preparing them is this
        // method's business, and doing it at the call site is what
        // breaks the dropdown.
        // ==========================================
        public static int Popup(string key, string languageCode, int selected, string[] options,
            params GUILayoutOption[] layout)
        {
            if (options == null || options.Length == 0) { return selected; }

            int index = Mathf.Clamp(selected, 0, options.Length - 1);

            int picked;
            if (_pending.TryGetValue(key, out picked))
            {
                _pending.Remove(key);
                index = Mathf.Clamp(picked, 0, options.Length - 1);
            }

            var content = new GUIContent(DocSnapEditorText.Draw(languageCode, options[index]));
            Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.popup, layout);

            if (GUI.Button(rect, content, EditorStyles.popup))
            {
                ShowMenu(key, rect, index, options);
            }

            return index;
        }

        // ==========================================
        // LabeledPopup
        // The same control with a fixed-width label beside it,
        // which is the shape this tool's windows use.
        // ==========================================
        public static int LabeledPopup(string key, string languageCode, string label, string tooltip,
            int selected, string[] options, float labelWidth = 160f)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(label, tooltip), GUILayout.Width(labelWidth));
            int result = Popup(key, languageCode, selected, options);
            EditorGUILayout.EndHorizontal();
            return result;
        }

        private static void ShowMenu(string key, Rect rect, int index, string[] options)
        {
            var menu = new GenericMenu();
            EditorWindow window = EditorWindow.focusedWindow;

            for (int i = 0; i < options.Length; i++)
            {
                int choice = i;

                // RAW text, deliberately. This menu is drawn by the
                // platform, which shapes and reorders on its own.
                //
                // '/' would make a submenu out of anything after it, and
                // an option is a label rather than a path - a version
                // folder or a font family name is free to contain one.
                var item = new GUIContent((options[i] ?? "").Replace("/", "∕"));

                menu.AddItem(item, i == index, () =>
                {
                    _pending[key] = choice;

                    // The menu closed some frames after the window last
                    // drew, so nothing else is going to ask for a repaint.
                    if (window != null) { window.Repaint(); }
                });
            }

            menu.DropDown(rect);
        }

        // Dropped when a window closes, so a choice nobody collected
        // cannot be handed to a different window that later reuses
        // the same key.
        public static void Forget(string keyPrefix)
        {
            if (string.IsNullOrEmpty(keyPrefix)) { return; }

            var stale = new List<string>();
            foreach (KeyValuePair<string, int> entry in _pending)
            {
                if (entry.Key.StartsWith(keyPrefix, System.StringComparison.Ordinal)) { stale.Add(entry.Key); }
            }
            foreach (string k in stale) { _pending.Remove(k); }
        }
    }
}
