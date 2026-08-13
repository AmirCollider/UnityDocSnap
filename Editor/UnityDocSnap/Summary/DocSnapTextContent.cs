// ==========================================
// DocSnapTextContent
// The words that are actually on the screen.
//
// THE PROBLEM
// -----------
// The summary listed a UI object as
//
//     - PauseButtonTextTMP — TextMeshProUGUI
//
// and stopped there. The name of the object, the name
// of the component, and not one character of what it
// says. Every string in a project's UI - every button
// label, every menu title, every line of dialogue -
// was missing from the one output written to be handed
// to an assistant, because the summary expands the
// serialized fields of the project's OWN scripts only
// (see DocSnapSummaryWriter.IsOwnScript) and a text
// component belongs to Unity or to TextMesh Pro. The
// rule is right - it is what stops the summary drowning
// in UI boilerplate - and the text was collateral.
//
// It is also the most valuable single field in that
// whole component. "PauseButtonTextTMP" is a name
// somebody typed once; "ادامه" is what the player
// reads. An assistant asked "where does the pause menu
// say Resume?" could not answer from an export, and
// neither could a person searching the summary for a
// string they had seen in the game.
//
// WHAT COUNTS AS TEXT, AND WHY IT IS NOT A TYPE LIST
// --------------------------------------------------
// Matching on component type (Text, TextMeshProUGUI,
// TextMeshPro, TMP_InputField, TextMesh, …) means a
// list that is wrong the moment somebody uses a text
// component this file has not heard of - a package's
// own label widget, a future TMP class, a project's
// wrapper. Every one of them serializes the same
// field: a string called `m_Text` (Unity, TextMesh)
// or `m_text` (TextMesh Pro), which the Inspector
// draws as "Text".
//
// So the field is the rule. Anything carrying a
// serialized string field named `text` - with or
// without Unity's `m_` prefix, in either casing - is
// treated as text content, whoever wrote the
// component. A project's own script with a `text`
// field is caught by the same rule, which is correct:
// that is text too.
//
// Sizes are capped per string rather than per Scene.
// Text is short - a label is a handful of words - and
// its content is the point of including it, so it is
// the one thing in the summary that is worth its
// bytes. The cap only exists so a single serialized
// paragraph (a credits blob, a licence notice) cannot
// take over the file.
// ==========================================
using System;
using System.Collections.Generic;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Json;

namespace AmirCollider.UnityDocSnap.Editor.Summary
{
    internal static class DocSnapTextContent
    {
        // One string's ceiling in the dedicated Text section.
        // Long enough for a paragraph of dialogue, short enough
        // that a serialized credits screen cannot become the
        // summary.
        public const int MaxTextLength = 400;

        // The same string quoted inline on a hierarchy line,
        // where it shares the row with the object's name and
        // component list and must not push them off the screen.
        public const int MaxPreviewLength = 80;

        // How many distinct strings one GameObject contributes
        // to its own hierarchy line. More than one is already
        // unusual (a legacy Text and a TMP component on the
        // same object); four is the point at which the line is
        // no longer a line.
        public const int MaxTextsPerObject = 4;

        // How many rows the Scene's Text section lists before
        // saying how many more there are.
        public const int MaxTextsListed = 400;

        // How many strings one Prefab / asset entry contributes
        // to a folder summary, where each one is a sub-bullet
        // under a single file line.
        public const int MaxTextsPerAsset = 20;

        // ==========================================
        // Entry
        // One string, and enough context to find the thing
        // that draws it: the object's path inside its Scene
        // or Prefab, and the component that carries it.
        // ==========================================
        public struct Entry
        {
            public string Path;
            public string Component;
            public string Text;
        }

        // ==========================================
        // FromComponent
        // The text one component draws, or null when it
        // draws none. A missing script is skipped: it has no
        // fields to read and nothing to say.
        // ==========================================
        public static string FromComponent(JsonValue comp)
        {
            if (comp == null || comp.IsNull) { return null; }
            if (comp.Get("isMissing").AsBool()) { return null; }
            return FromFields(comp.Get("fields"));
        }

        // ==========================================
        // FromFields
        // The first non-empty text field in a serialized
        // field list. Shared by components and by asset
        // fields, so a ScriptableObject holding a line of
        // dialogue is read by exactly the rule that reads a
        // label in a Scene.
        // ==========================================
        public static string FromFields(JsonValue fields)
        {
            if (fields == null || fields.IsNull) { return null; }

            foreach (JsonValue field in fields.Items)
            {
                if (field.Get("kind").AsString("") != "string") { continue; }
                if (!IsTextField(field)) { continue; }

                string value = Flatten(field.Get("value").AsString(""));
                if (value.Length == 0) { continue; }
                return value;
            }
            return null;
        }

        // ==========================================
        // FromGameObject
        // Every distinct string one GameObject draws, for the
        // hierarchy line that names it. Distinct because a
        // Prefab instance carrying both a legacy Text and a
        // TMP component with the same content should say it
        // once.
        // ==========================================
        public static List<string> FromGameObject(JsonValue go)
        {
            var texts = new List<string>();
            if (go == null || go.IsNull) { return texts; }

            foreach (JsonValue comp in go.Get("components").Items)
            {
                string text = FromComponent(comp);
                if (text == null) { continue; }
                if (texts.Contains(text)) { continue; }
                texts.Add(text);
                if (texts.Count >= MaxTextsPerObject) { break; }
            }
            return texts;
        }

        // ==========================================
        // Collect
        // Every string in a hierarchy, in walk order, each
        // with the path of the object that draws it.
        //
        // Uncapped on purpose: the caller caps its own
        // rendering, and it can only report "…+N more"
        // honestly if the collection knows the real N.
        // ==========================================
        public static List<Entry> Collect(JsonValue objects, string parentPath)
        {
            var entries = new List<Entry>();
            Walk(objects, parentPath, entries);
            return entries;
        }

        // ==========================================
        // CollectFromRoot
        // The same, for a single root node rather than an
        // array of them - the shape a Prefab asset's
        // `prefabRoot` comes in.
        // ==========================================
        public static List<Entry> CollectFromRoot(JsonValue root)
        {
            var entries = new List<Entry>();
            if (root == null || root.IsNull) { return entries; }
            Walk(JsonValue.Arr().Add(root), "", entries);
            return entries;
        }

        private static void Walk(JsonValue objects, string parentPath, List<Entry> entries)
        {
            foreach (JsonValue go in objects.Items)
            {
                string name = Flatten(go.Get("name").AsString("GameObject"));
                string path = string.IsNullOrEmpty(parentPath) ? name : parentPath + "/" + name;

                foreach (JsonValue comp in go.Get("components").Items)
                {
                    string text = FromComponent(comp);
                    if (text == null) { continue; }
                    entries.Add(new Entry
                    {
                        Path = path,
                        Component = comp.Get("typeName").AsString("Component"),
                        Text = text
                    });
                }

                Walk(go.Get("children"), path, entries);
            }
        }

        // ==========================================
        // CountIn
        // How many strings a hierarchy holds, for the
        // header line that counts what the summary contains.
        // ==========================================
        public static int CountIn(JsonValue objects)
        {
            return Collect(objects, "").Count;
        }

        // ==========================================
        // Preview / Full
        // The two lengths one string is shown at: quoted
        // inline beside its object, and in full in the
        // section that exists to carry it.
        // ==========================================
        public static string Preview(string text)
        {
            return Truncate(text, MaxPreviewLength);
        }

        public static string Full(string text)
        {
            return Truncate(text, MaxTextLength);
        }

        // ==========================================
        // IsTextField
        // Whether a serialized field is the one the
        // Inspector labels "Text". Both the serialized name
        // and the display label are asked, because a project
        // wrapper may name its field `caption` and label it
        // "Text", and TextMesh Pro does the reverse.
        // ==========================================
        private static bool IsTextField(JsonValue field)
        {
            return IsTextName(field.Get("name").AsString(""))
                || IsTextName(field.Get("label").AsString(""));
        }

        private static bool IsTextName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) { return false; }
            string name = raw.StartsWith("m_", StringComparison.Ordinal) ? raw.Substring(2) : raw;
            return string.Equals(name, "text", StringComparison.OrdinalIgnoreCase);
        }

        // ==========================================
        // Flatten
        // A multi-line string on one line. Text components
        // routinely hold newlines - that is what makes them
        // text - and every line of both summary forms is a
        // single Markdown bullet or a single JSON value.
        // ==========================================
        private static string Flatten(string s)
        {
            if (string.IsNullOrEmpty(s)) { return ""; }

            var sb = new StringBuilder(s.Length);
            bool lastWasSpace = false;
            foreach (char c in s)
            {
                bool isBreak = c == '\n' || c == '\r' || c == '\t';
                char next = isBreak ? ' ' : c;
                if (next == ' ')
                {
                    // Text is authored with line breaks and indentation
                    // for shape, not for content; collapsing the runs
                    // keeps "Game\n\n   Over" from arriving as a row of
                    // spaces.
                    if (lastWasSpace) { continue; }
                    lastWasSpace = true;
                }
                else
                {
                    lastWasSpace = false;
                }
                sb.Append(next);
            }
            return sb.ToString().Trim();
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) { return s ?? ""; }
            return s.Substring(0, max) + "…";
        }
    }
}
