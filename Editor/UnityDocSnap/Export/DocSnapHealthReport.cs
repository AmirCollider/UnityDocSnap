// ==========================================
// DocSnapHealthReport
// Turns data the export already collected into
// the answers a person actually opens the
// documentation to find.
//
// Every export walked past a "Missing Script"
// component and an object reference whose target
// no longer exists, faithfully wrote both into
// the JSON, rendered them on a page thousands of
// rows down - and then said nothing. A person
// with a broken project had to already know where
// to look.
//
// This gathers those two counts (plus assets Unity
// could not resolve a type for, and Scene names
// that collide) per exported Scene / folder and
// stores them in the manifest, so the dashboard can
// lead with "3 missing scripts in 2 scenes" and
// link straight to them - and so a single-Scene
// re-export updates only its own numbers instead of
// discarding everyone else's.
// ==========================================
using System;
using System.Collections.Generic;
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Export
{
    internal static class DocSnapHealthReport
    {
        // ==========================================
        // BuildSceneEntry / BuildFolderEntry
        // One health row for one exported scope. The
        // scope key matches the one used for the search
        // records, so both are replaced together.
        // ==========================================
        public static ManifestHealthEntry BuildSceneEntry(JsonValue sceneData, string scope, string label, string htmlFile)
        {
            var entry = new ManifestHealthEntry
            {
                scope = scope,
                group = "scene",
                label = label,
                htmlFile = htmlFile,
                itemCount = (int)sceneData.Get("totalGameObjects").AsNumber()
            };
            Scan(sceneData, entry);
            return entry;
        }

        public static ManifestHealthEntry BuildFolderEntry(JsonValue folderData, string scope, string label, string htmlFile)
        {
            var entry = new ManifestHealthEntry
            {
                scope = scope,
                group = "asset",
                label = label,
                htmlFile = htmlFile,
                itemCount = (int)folderData.Get("fileCount").AsNumber()
            };
            Scan(folderData, entry);

            // An asset whose main type Unity could not resolve is
            // usually a file that failed to import - worth flagging
            // separately from a broken reference.
            foreach (JsonValue file in folderData.Get("files").Items)
            {
                if (string.Equals(file.Get("mainType").AsString(""), "Unknown", StringComparison.Ordinal))
                {
                    entry.unresolvedAssets++;
                }
            }
            return entry;
        }

        // ==========================================
        // Scan
        // One iterative pass over the whole exported tree.
        // Iterative on purpose: a Scene hierarchy or a
        // deeply nested Prefab is exactly the shape that
        // makes a recursive walk risk a StackOverflow, and
        // a StackOverflowException cannot be caught - it
        // takes the whole Editor down with it.
        // ==========================================
        private static void Scan(JsonValue root, ManifestHealthEntry entry)
        {
            var pending = new Stack<JsonValue>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                JsonValue node = pending.Pop();
                if (node == null) { continue; }

                if (node.Kind == JsonKind.Object)
                {
                    Classify(node, entry);
                    foreach (KeyValuePair<string, JsonValue> member in node.Members)
                    {
                        if (member.Value != null && !member.Value.IsScalar) { pending.Push(member.Value); }
                    }
                }
                else if (node.Kind == JsonKind.Array)
                {
                    foreach (JsonValue item in node.Items)
                    {
                        if (item != null && !item.IsScalar) { pending.Push(item); }
                    }
                }
            }
        }

        // ==========================================
        // Classify
        // The two shapes worth counting, exactly as the
        // exporters write them:
        //   { "typeName": "Missing Script", "isMissing": true }
        //   { "kind": "objectRef", "isMissing": true, … }
        // ==========================================
        private static void Classify(JsonValue node, ManifestHealthEntry entry)
        {
            if (!node.Has("isMissing") || !node.Get("isMissing").AsBool()) { return; }

            if (string.Equals(node.Get("kind").AsString(""), "objectRef", StringComparison.Ordinal))
            {
                entry.missingReferences++;
            }
            else if (node.Has("typeName"))
            {
                entry.missingScripts++;
            }
        }

        // ==========================================
        // Totals
        // Project-wide roll-up across every scope the
        // manifest remembers, for the dashboard card.
        // ==========================================
        public static HealthTotals Totals(ManifestState state)
        {
            var totals = new HealthTotals();
            if (state == null || state.health == null) { return totals; }

            foreach (ManifestHealthEntry e in state.health)
            {
                totals.missingScripts += e.missingScripts;
                totals.missingReferences += e.missingReferences;
                totals.unresolvedAssets += e.unresolvedAssets;
                if (e.missingScripts > 0 || e.missingReferences > 0 || e.unresolvedAssets > 0)
                {
                    totals.affectedScopes++;
                }
            }
            totals.duplicateSceneNames = DuplicateSceneNames(state);
            return totals;
        }

        // ==========================================
        // DuplicateSceneNames
        // Two Scenes sharing a file name are no longer able
        // to overwrite each other's output (DocSnapNaming
        // disambiguates them), but it is still worth telling
        // someone: it usually means a copy was left behind,
        // and it makes every "open the Main scene"
        // instruction ambiguous for a reader.
        // ==========================================
        public static List<string> DuplicateSceneNames(ManifestState state)
        {
            var duplicates = new List<string>();
            if (state == null || state.scenes == null) { return duplicates; }

            var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (ManifestSceneEntry s in state.scenes)
            {
                string name = s.sceneName ?? "";
                int count;
                seen[name] = seen.TryGetValue(name, out count) ? count + 1 : 1;
            }
            foreach (KeyValuePair<string, int> pair in seen)
            {
                if (pair.Value > 1) { duplicates.Add(pair.Key); }
            }
            duplicates.Sort(StringComparer.OrdinalIgnoreCase);
            return duplicates;
        }

        // ==========================================
        // Worst
        // The scopes with something to fix, worst first,
        // so the dashboard can show the top few instead of
        // every scope in the project.
        // ==========================================
        public static List<ManifestHealthEntry> Worst(ManifestState state, int limit)
        {
            var list = new List<ManifestHealthEntry>();
            if (state == null || state.health == null) { return list; }

            foreach (ManifestHealthEntry e in state.health)
            {
                if (e.missingScripts > 0 || e.missingReferences > 0 || e.unresolvedAssets > 0) { list.Add(e); }
            }
            list.Sort((a, b) =>
            {
                int byScore = Score(b).CompareTo(Score(a));
                if (byScore != 0) { return byScore; }
                return string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase);
            });
            if (limit > 0 && list.Count > limit) { list.RemoveRange(limit, list.Count - limit); }
            return list;
        }

        // A missing script is a harder failure than a broken
        // reference, which is harder than an asset Unity could
        // not type - weight them so the worst really sorts first.
        private static int Score(ManifestHealthEntry e)
        {
            return (e.missingScripts * 100) + (e.missingReferences * 10) + e.unresolvedAssets;
        }
    }

    // ==========================================
    // HealthTotals
    // The project-wide roll-up handed to the renderers.
    // ==========================================
    internal sealed class HealthTotals
    {
        public int missingScripts;
        public int missingReferences;
        public int unresolvedAssets;
        public int affectedScopes;
        public List<string> duplicateSceneNames = new List<string>();

        public bool IsClean
        {
            get
            {
                return missingScripts == 0
                    && missingReferences == 0
                    && unresolvedAssets == 0
                    && (duplicateSceneNames == null || duplicateSceneNames.Count == 0);
            }
        }
    }
}
