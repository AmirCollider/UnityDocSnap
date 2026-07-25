// ==========================================
// DocSnapSettingsStore
// The file behind the project-scoped half of
// DocSnapSettings.
//
// Every setting used to live in EditorUserSettings,
// which writes ProjectSettings/EditorUserSettings.asset
// - a file Unity documents as per-user data, which
// carries a team member's VCS credentials, and which
// most studios therefore keep out of version control
// on purpose. That is the right home for "which
// language do I want the export window drawn in".
// It is the wrong home for "which folders does THIS
// PROJECT not want documented", because that is a
// fact about the project, and putting it there had
// three consequences:
//
//   • Nobody could commit it. Every person on a team
//     configured the exclude list by hand, or did not,
//     and their exports silently disagreed about what
//     the project even contains.
//   • CI could not have it at all. A build agent gets
//     a fresh checkout with no EditorUserSettings.asset,
//     so an automated export documented the Asset Store
//     content the team had spent a year excluding.
//   • Nothing was reviewable. A change to what the
//     project documents left no diff for anyone to see.
//
// So the project-scoped settings now live in
// ProjectSettings/UnityDocSnapSettings.json: plain,
// ordered, one key per line, meant to be committed
// and meant to be read in a pull request. The
// genuinely personal settings stay exactly where
// they were.
//
// The format is the tool's own JsonValue rather than
// Unity's JsonUtility so that key order is stable -
// a settings file that reorders itself on every save
// produces a diff every time anyone opens the
// window, which is how a file stops being reviewed.
// ==========================================
using System;
using System.Collections.Generic;
using System.IO;
using AmirCollider.UnityDocSnap.Editor.Json;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal sealed class DocSnapSettingsStore
    {
        // Bumped only when the file's shape changes in a way a
        // previous version could not read. Present from the
        // first write so there is never a version-less file to
        // guess about later.
        public const int CurrentFormatVersion = 1;

        private readonly string _filePath;
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<string> _order = new List<string>();
        private bool _loaded;

        public DocSnapSettingsStore(string filePath)
        {
            _filePath = filePath;
        }

        // ==========================================
        // Get / Set
        // A missing key returns null, which is what every
        // caller already treats as "use the default".
        // ==========================================
        public string Get(string key)
        {
            EnsureLoaded();
            string value;
            return _values.TryGetValue(key, out value) ? value : null;
        }

        public void Set(string key, string value)
        {
            EnsureLoaded();
            string existing;
            bool had = _values.TryGetValue(key, out existing);

            // Writing an identical value must not touch the file.
            // Unity's settings providers call setters freely on
            // every repaint, and a save per repaint would rewrite
            // the file - and its modification time, and any watcher
            // downstream - dozens of times a second.
            if (had && string.Equals(existing, value, StringComparison.Ordinal)) { return; }

            if (!had) { _order.Add(key); }
            _values[key] = value ?? "";
            Save();
        }

        // ==========================================
        // ImportMissing
        // One-time migration hook: adopts a value only when
        // this store has no opinion of its own, so a project
        // that has already committed a settings file is never
        // overwritten by whatever happened to be in the
        // migrating user's local EditorUserSettings.
        //
        // Returns true when anything was actually adopted, so
        // the caller can save once rather than per key.
        // ==========================================
        public bool ImportMissing(string key, string value)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(value)) { return false; }
            if (_values.ContainsKey(key)) { return false; }

            _order.Add(key);
            _values[key] = value;
            return true;
        }

        // ==========================================
        // Load
        // A missing file is the normal first-run state and
        // means "no project settings yet", not an error. A
        // corrupt one is reported rather than silently
        // replaced: overwriting a file somebody hand-edited
        // and committed would destroy their work, and the
        // defaults are perfectly usable in the meantime.
        // ==========================================
        public void Load()
        {
            _values.Clear();
            _order.Clear();
            _loaded = true;
            // Cleared first: a reload after the user fixed a broken
            // file must not keep reporting the old complaint.
            LastError = null;

            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath)) { return; }

            string text;
            try { text = File.ReadAllText(_filePath); }
            catch (Exception ex)
            {
                LastError = "could not be read (" + ex.Message + ")";
                return;
            }

            JsonValue root;
            if (!JsonValue.TryParse(text, out root) || root == null || root.Kind != JsonKind.Object)
            {
                LastError = "is not valid JSON, so the built-in defaults are being used instead";
                return;
            }

            foreach (KeyValuePair<string, JsonValue> member in root.Members)
            {
                if (member.Key == "formatVersion") { continue; }
                if (member.Value == null) { continue; }
                _order.Add(member.Key);
                _values[member.Key] = member.Value.Kind == JsonKind.String
                    ? member.Value.AsString("")
                    : member.Value.ToCompactString();
            }
        }

        // ==========================================
        // LastError
        // Non-null when the file exists but could not be
        // used. The settings UI surfaces it; nothing else
        // needs to care.
        // ==========================================
        public string LastError { get; private set; }

        // ==========================================
        // Save
        // Writes the whole file. Keys keep the order they
        // were first written in, so a diff shows the line
        // that changed and nothing else.
        // ==========================================
        public void Save()
        {
            if (string.IsNullOrEmpty(_filePath)) { return; }

            var root = JsonValue.Obj();
            root.Set("formatVersion", CurrentFormatVersion);
            foreach (string key in _order)
            {
                string value;
                if (!_values.TryGetValue(key, out value)) { continue; }
                root.Set(key, value);
            }

            try
            {
                string directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory)) { Directory.CreateDirectory(directory); }
                File.WriteAllText(_filePath, root.ToString() + "\n");
                LastError = null;
            }
            catch (Exception ex)
            {
                LastError = "could not be written (" + ex.Message + ")";
            }
        }

        private void EnsureLoaded()
        {
            if (!_loaded) { Load(); }
        }
    }
}
