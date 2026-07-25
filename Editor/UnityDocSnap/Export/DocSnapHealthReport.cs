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
// Two levels come out of one walk:
//
//   • COUNTS per exported Scene / folder
//     (ManifestHealthEntry), for the dashboard's
//     headline: "3 missing scripts in 2 scenes".
//   • Each individual FINDING (ManifestIssueEntry)
//     with the GameObject path or asset path it
//     sits on, the component and field that hold
//     it, and the anchor of the card that renders
//     it - so the issues page can link to the
//     actual problem rather than to the page it is
//     buried somewhere inside.
//
// Both are stored per scope so a single-Scene
// re-export updates only its own findings instead
// of discarding everyone else's.
// ==========================================
using System;
using System.Collections.Generic;
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Export
{
    internal static class DocSnapHealthReport
    {
        public const string KindMissingScript = "missingScript";
        public const string KindMissingReference = "missingReference";
        public const string KindUnresolvedAsset = "unresolvedAsset";

        // How deep into one component's serialized fields the
        // walk descends looking for broken references. Nested
        // structs and [SerializeReference] graphs are already
        // depth-capped by the reflector that wrote them; this is
        // the same guarantee on the reading side, for data an
        // older version may have written uncapped.
        private const int MaxFieldDepth = 24;

        // ==========================================
        // BuildSceneEntry
        // One health row plus the detailed findings for one
        // exported Scene. The scope key matches the one used
        // for the search records, so both are replaced together.
        // ==========================================
        public static ManifestHealthEntry BuildSceneEntry(
            JsonValue sceneData, string scope, string label, string htmlFile, List<ManifestIssueEntry> issues)
        {
            var entry = new ManifestHealthEntry
            {
                scope = scope,
                group = "scene",
                label = label,
                htmlFile = htmlFile,
                itemCount = (int)sceneData.Get("totalGameObjects").AsNumber()
            };

            var sink = new IssueSink(scope, "scene", label, htmlFile, issues);
            ScanGameObjects(sceneData.Get("rootObjects"), entry, sink);
            entry.issuesTruncated = sink.Truncated;
            return entry;
        }

        // ==========================================
        // BuildFolderEntry
        // The same for one exported asset folder. An asset
        // whose main type Unity could not resolve is usually a
        // file that failed to import - worth flagging
        // separately from a broken reference.
        // ==========================================
        public static ManifestHealthEntry BuildFolderEntry(
            JsonValue folderData, string scope, string label, string htmlFile, List<ManifestIssueEntry> issues)
        {
            var entry = new ManifestHealthEntry
            {
                scope = scope,
                group = "asset",
                label = label,
                htmlFile = htmlFile,
                itemCount = (int)folderData.Get("fileCount").AsNumber()
            };

            var sink = new IssueSink(scope, "asset", label, htmlFile, issues);

            foreach (JsonValue file in folderData.Get("files").Items)
            {
                string path = file.Get("path").AsString("");
                string fileName = file.Get("fileName").AsString(path);
                string guid = file.Get("guid").AsString("");
                string anchor = string.IsNullOrEmpty(guid) ? "" : "asset-" + guid;

                if (string.Equals(file.Get("mainType").AsString(""), "Unknown", StringComparison.Ordinal))
                {
                    entry.unresolvedAssets++;
                    sink.Add(KindUnresolvedAsset, path, ImporterLabel(file), anchor);
                }

                // Every field tree an asset can carry: its importer's
                // settings, the asset object's own serialized fields, and -
                // for a Prefab - the whole GameObject tree inside it, which
                // holds missing scripts and broken references of its own.
                ScanFields(file.Get("importerFields"), entry, sink, path, "Import Settings", anchor);
                ScanFields(file.Get("assetFields"), entry, sink, path, file.Get("mainType").AsString("Asset"), anchor);
                ScanPrefabRoot(file.Get("prefabRoot"), entry, sink, fileName, anchor);
            }

            entry.issuesTruncated = sink.Truncated;
            return entry;
        }

        // ==========================================
        // ScanGameObjects
        // One iterative pass over a whole GameObject tree,
        // carrying each object's hierarchy path down with it so
        // a finding can say "Canvas/HUD/HealthBar" instead of
        // just naming the Scene.
        //
        // Iterative on purpose: a Scene hierarchy or a deeply
        // nested Prefab is exactly the shape that makes a
        // recursive walk risk a StackOverflow, and a
        // StackOverflowException cannot be caught - it takes
        // the whole Editor down with it.
        // ==========================================
        private static void ScanGameObjects(JsonValue roots, ManifestHealthEntry entry, IssueSink sink)
        {
            if (roots == null) { return; }

            var pending = new Stack<GoLevel>();
            IReadOnlyList<JsonValue> rootItems = roots.Items;
            for (int i = rootItems.Count - 1; i >= 0; i--) { pending.Push(new GoLevel(rootItems[i], "")); }

            while (pending.Count > 0)
            {
                GoLevel level = pending.Pop();
                JsonValue go = level.Node;
                if (go == null) { continue; }

                string name = go.Get("name").AsString("GameObject");
                string path = string.IsNullOrEmpty(level.ParentPath) ? name : level.ParentPath + "/" + name;
                string anchor = "go-" + (int)go.Get("instanceId").AsNumber();

                foreach (JsonValue comp in go.Get("components").Items)
                {
                    if (comp == null) { continue; }

                    if (comp.Get("isMissing").AsBool())
                    {
                        entry.missingScripts++;
                        sink.Add(KindMissingScript, path, "Missing Script", anchor);
                        continue;
                    }

                    ScanFields(comp.Get("fields"), entry, sink, path, comp.Get("typeName").AsString("Component"), anchor);
                }

                IReadOnlyList<JsonValue> children = go.Get("children").Items;
                for (int i = children.Count - 1; i >= 0; i--) { pending.Push(new GoLevel(children[i], path)); }
            }
        }

        // ==========================================
        // ScanPrefabRoot
        // A Prefab asset's contents are a GameObject tree in
        // their own right. Its findings belong to the asset,
        // so the location is prefixed with the Prefab's own
        // file name rather than presented as a loose object.
        // ==========================================
        private static void ScanPrefabRoot(JsonValue prefabRoot, ManifestHealthEntry entry, IssueSink sink, string fileName, string anchor)
        {
            if (prefabRoot == null || prefabRoot.IsNull || prefabRoot.Kind != JsonKind.Object) { return; }

            var wrapper = JsonValue.Arr();
            wrapper.Add(prefabRoot);

            // A Prefab asset renders as a single card, so the link
            // lands on that card and the location names the object
            // inside it.
            ScanGameObjects(wrapper, entry, sink.ForPrefab(fileName, anchor));
        }

        // ==========================================
        // ScanFields
        // Every serialized field under one component / importer
        // / asset, including the ones nested inside structs,
        // arrays and [SerializeReference] graphs, looking for
        // object references whose target no longer exists.
        //
        // The field path is carried down so a finding reads
        // "PlayerController › waypoints[3].marker" rather than
        // "somewhere in PlayerController".
        // ==========================================
        private static void ScanFields(JsonValue fields, ManifestHealthEntry entry, IssueSink sink, string location, string owner, string anchor)
        {
            if (fields == null || fields.Kind != JsonKind.Array) { return; }

            var pending = new Stack<FieldLevel>();
            PushFieldList(pending, fields, "", 0);

            while (pending.Count > 0)
            {
                FieldLevel level = pending.Pop();
                JsonValue field = level.Node;
                if (field == null || field.Kind != JsonKind.Object) { continue; }

                string kind = field.Get("kind").AsString("");

                if (string.Equals(kind, "objectRef", StringComparison.Ordinal))
                {
                    if (field.Get("isMissing").AsBool())
                    {
                        entry.missingReferences++;
                        sink.Add(KindMissingReference, location, Describe(owner, level.Path), anchor);
                    }
                    continue;
                }

                if (level.Depth >= MaxFieldDepth) { continue; }

                // Arrays index their items; structs and managed
                // references name theirs.
                JsonValue items = field.Get("items");
                if (items != null && items.Kind == JsonKind.Array)
                {
                    IReadOnlyList<JsonValue> list = items.Items;
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        pending.Push(new FieldLevel(list[i], level.Path + "[" + i + "]", level.Depth + 1));
                    }
                }

                JsonValue nested = field.Get("fields");
                if (nested != null && nested.Kind == JsonKind.Array)
                {
                    PushFieldList(pending, nested, level.Path, level.Depth + 1);
                }
            }
        }

        private static void PushFieldList(Stack<FieldLevel> pending, JsonValue fields, string parentPath, int depth)
        {
            IReadOnlyList<JsonValue> list = fields.Items;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                JsonValue field = list[i];
                string name = field != null ? FieldName(field) : "";
                string path = string.IsNullOrEmpty(parentPath) ? name : parentPath + "." + name;
                pending.Push(new FieldLevel(field, path, depth));
            }
        }

        private static string FieldName(JsonValue field)
        {
            string name = field.Get("name").AsString("");
            return string.IsNullOrEmpty(name) ? field.Get("label").AsString("") : name;
        }

        private static string Describe(string owner, string fieldPath)
        {
            if (string.IsNullOrEmpty(fieldPath)) { return owner; }
            return string.IsNullOrEmpty(owner) ? fieldPath : owner + " › " + fieldPath;
        }

        private static string ImporterLabel(JsonValue file)
        {
            string importer = file.Get("importerType").AsString("");
            string ext = file.Get("extension").AsString("");
            if (!string.IsNullOrEmpty(importer)) { return importer; }
            return string.IsNullOrEmpty(ext) ? "Unknown type" : ext;
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

        // ==========================================
        // SortedIssues
        // Every recorded finding, hardest failure first, then
        // by scope and location, so the issues page reads the
        // same way on every export instead of in whatever
        // order the scopes happened to be walked.
        // ==========================================
        public static List<ManifestIssueEntry> SortedIssues(ManifestState state)
        {
            var list = new List<ManifestIssueEntry>();
            if (state == null || state.issues == null) { return list; }
            list.AddRange(state.issues);
            list.Sort((a, b) =>
            {
                int byKind = KindRank(a.kind).CompareTo(KindRank(b.kind));
                if (byKind != 0) { return byKind; }
                int byScope = string.Compare(a.scopeLabel, b.scopeLabel, StringComparison.OrdinalIgnoreCase);
                if (byScope != 0) { return byScope; }
                int byLocation = string.Compare(a.location, b.location, StringComparison.OrdinalIgnoreCase);
                if (byLocation != 0) { return byLocation; }
                return string.Compare(a.detail, b.detail, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        public static int KindRank(string kind)
        {
            if (string.Equals(kind, KindMissingScript, StringComparison.Ordinal)) { return 0; }
            if (string.Equals(kind, KindMissingReference, StringComparison.Ordinal)) { return 1; }
            return 2;
        }

        // A missing script is a harder failure than a broken
        // reference, which is harder than an asset Unity could
        // not type - weight them so the worst really sorts first.
        private static int Score(ManifestHealthEntry e)
        {
            return (e.missingScripts * 100) + (e.missingReferences * 10) + e.unresolvedAssets;
        }

        // ==========================================
        // IssueSink
        // Collects findings for one scope, stamping each with
        // the scope's identity and stopping at the per-scope
        // cap. A project broken enough to produce tens of
        // thousands of findings does not need every one of them
        // enumerated - it needs the first few hundred and an
        // honest note that there are more, which is what
        // Truncated drives.
        // ==========================================
        private sealed class IssueSink
        {
            private readonly ScopeState _state;

            // Set only on the view returned by ForPrefab: findings
            // found inside a Prefab asset are relabelled so their
            // location names the Prefab first ("Player.prefab ›
            // Body/Hand") and are pinned to the Prefab card's own
            // anchor, because a GameObject anchor only exists on a
            // Scene page.
            private readonly string _locationPrefix;
            private readonly string _forcedAnchor;

            public IssueSink(string scope, string group, string scopeLabel, string htmlFile, List<ManifestIssueEntry> target)
            {
                _state = new ScopeState
                {
                    Scope = scope,
                    Group = group,
                    ScopeLabel = scopeLabel,
                    HtmlFile = htmlFile,
                    Target = target
                };
            }

            private IssueSink(ScopeState state, string locationPrefix, string forcedAnchor)
            {
                _state = state;
                _locationPrefix = locationPrefix;
                _forcedAnchor = forcedAnchor;
            }

            // The prefab view shares this sink's state, so its
            // findings count against the same per-scope cap and the
            // same Truncated flag.
            public IssueSink ForPrefab(string fileName, string anchor)
            {
                return new IssueSink(_state, fileName, anchor);
            }

            public bool Truncated { get { return _state.Truncated; } }

            public void Add(string kind, string location, string detail, string anchor)
            {
                if (_state.Target == null) { return; }
                if (_state.Added >= DocSnapConstants.MaxIssuesPerScope) { _state.Truncated = true; return; }

                string where = location ?? "";
                if (!string.IsNullOrEmpty(_locationPrefix))
                {
                    where = string.IsNullOrEmpty(where) ? _locationPrefix : _locationPrefix + " › " + where;
                }

                _state.Target.Add(new ManifestIssueEntry
                {
                    scope = _state.Scope,
                    group = _state.Group,
                    kind = kind,
                    scopeLabel = _state.ScopeLabel,
                    location = where,
                    detail = detail ?? "",
                    htmlFile = _state.HtmlFile,
                    anchor = (_forcedAnchor ?? anchor) ?? ""
                });
                _state.Added++;
            }

            private sealed class ScopeState
            {
                public string Scope;
                public string Group;
                public string ScopeLabel;
                public string HtmlFile;
                public List<ManifestIssueEntry> Target;
                public int Added;
                public bool Truncated;
            }
        }

        private struct GoLevel
        {
            public readonly JsonValue Node;
            public readonly string ParentPath;

            public GoLevel(JsonValue node, string parentPath)
            {
                Node = node;
                ParentPath = parentPath;
            }
        }

        private struct FieldLevel
        {
            public readonly JsonValue Node;
            public readonly string Path;
            public readonly int Depth;

            public FieldLevel(JsonValue node, string path, int depth)
            {
                Node = node;
                Path = path;
                Depth = depth;
            }
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

        public int TotalFindings
        {
            get { return missingScripts + missingReferences + unresolvedAssets; }
        }

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
