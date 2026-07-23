// ==========================================
// DocSnapSearchIndex
// Builds the site's client-side search index.
//
// Two responsibilities:
//   1. Turn one exported Scene / asset-folder tree
//      into a flat list of tiny search records
//      (name + one line of context + a deep link),
//      which the export service stores in the
//      manifest so a single-item export never wipes
//      another item's records.
//   2. Emit theme/search-index.js from those stored
//      records - a plain `window.__DOCSNAP_SEARCH__ = [...]`
//      assignment rather than a .json, because a page
//      opened from the file:// origin cannot fetch()
//      an external JSON, but it can always load a
//      <script>. The list is hard-capped so an
//      enormous project can never emit a search file
//      large enough to hang the browser.
// ==========================================
using System;
using System.Collections.Generic;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Html;
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Search
{
    internal static class DocSnapSearchIndex
    {
        // A per-scope ceiling so one gigantic Scene or
        // folder cannot dominate (or bloat) the index.
        private const int MaxRecordsPerScope = 6000;
        private const int MaxSubLength = 120;

        // ==========================================
        // BuildSceneRecords
        // One record for the Scene itself plus one per
        // GameObject (name + its component types + a
        // deep link to that object's card).
        // ==========================================
        public static List<ManifestSearchEntry> BuildSceneRecords(JsonValue sceneData, string sceneName, string htmlFile)
        {
            var records = new List<ManifestSearchEntry>();
            records.Add(new ManifestSearchEntry
            {
                scope = sceneName,
                group = "scene",
                category = "Scene",
                name = sceneName,
                sub = sceneData.Get("scenePath").AsString(""),
                url = htmlFile
            });

            WalkGameObjects(sceneData.Get("rootObjects"), go =>
            {
                if (records.Count >= MaxRecordsPerScope) { return; }
                int id = (int)go.Get("instanceId").AsNumber();
                records.Add(new ManifestSearchEntry
                {
                    scope = sceneName,
                    group = "scene",
                    category = "GameObject",
                    name = go.Get("name").AsString("GameObject"),
                    sub = Trim(BuildSub(sceneName, ComponentTypes(go), go)),
                    url = htmlFile + "#go-" + id
                });
            });
            return records;
        }

        // ==========================================
        // BuildFolderRecords
        // One record per exported asset (name + type +
        // path, deep-linked to its card) plus one per
        // folder node (deep-linked to that folder in the
        // directory tree).
        // ==========================================
        public static List<ManifestSearchEntry> BuildFolderRecords(JsonValue folderData, string folderKey, string htmlFile)
        {
            var records = new List<ManifestSearchEntry>();

            foreach (JsonValue file in folderData.Get("files").Items)
            {
                if (records.Count >= MaxRecordsPerScope) { break; }
                string guid = file.Get("guid").AsString("");
                string type = file.Get("mainType").AsString("Asset");
                string path = file.Get("path").AsString("");
                records.Add(new ManifestSearchEntry
                {
                    scope = folderKey,
                    group = "asset",
                    category = type,
                    name = file.Get("fileName").AsString(""),
                    sub = Trim(type + " · " + path),
                    url = string.IsNullOrEmpty(guid) ? htmlFile : htmlFile + "#asset-" + guid
                });
            }

            AddFolderNodeRecords(folderData.Get("folderTree"), folderKey, htmlFile, records);
            return records;
        }

        // ==========================================
        // WriteSearchIndexJs
        // Serialises every stored record into a single
        // JS global assignment, capped at MaxSearchRecords.
        // Uses the JsonValue writer for the array so all
        // string escaping (quotes, '<') is handled exactly
        // as everywhere else in the tool.
        // ==========================================
        public static string WriteSearchIndexJs(ManifestState manifest)
        {
            var arr = JsonValue.Arr();
            int count = 0;
            foreach (ManifestSearchEntry r in manifest.searchRecords)
            {
                if (count >= DocSnapConstants.MaxSearchRecords) { break; }
                arr.Add(JsonValue.Obj()
                    .Set("c", r.category ?? "")
                    .Set("n", r.name ?? "")
                    .Set("s", r.sub ?? "")
                    .Set("u", r.url ?? "")
                    .Set("g", r.group ?? ""));
                count++;
            }

            var sb = new StringBuilder(4096);
            sb.Append("// Unity DocSnap search index - generated, do not edit.\n");
            sb.Append("window.__DOCSNAP_SEARCH__ = ");
            sb.Append(arr.ToString());
            sb.Append(";\n");
            sb.Append("window.__DOCSNAP_SEARCH_TRUNCATED__ = ");
            sb.Append(manifest.searchRecords.Count > count ? "true" : "false");
            sb.Append(";\n");
            return sb.ToString();
        }

        // ==========================================
        // Helpers
        // ==========================================
        private static void AddFolderNodeRecords(JsonValue folder, string folderKey, string htmlFile, List<ManifestSearchEntry> records)
        {
            if (folder == null || folder.IsNull) { return; }
            if (records.Count >= MaxRecordsPerScope) { return; }

            string folderPath = folder.Get("folderPath").AsString("");
            int total = (int)folder.Get("totalFileCount").AsNumber();
            records.Add(new ManifestSearchEntry
            {
                scope = folderKey,
                group = "asset",
                category = "Folder",
                name = folder.Get("folderName").AsString(""),
                sub = Trim(folderPath + " · " + total + " files"),
                url = htmlFile + "#" + FieldRenderer.FolderAnchor(folderPath)
            });

            foreach (JsonValue child in folder.Get("subfolders").Items)
            {
                AddFolderNodeRecords(child, folderKey, htmlFile, records);
            }
        }

        private static void WalkGameObjects(JsonValue objects, Action<JsonValue> visit)
        {
            foreach (JsonValue go in objects.Items)
            {
                visit(go);
                WalkGameObjects(go.Get("children"), visit);
            }
        }

        private static string ComponentTypes(JsonValue go)
        {
            var parts = new List<string>();
            foreach (JsonValue comp in go.Get("components").Items)
            {
                if (comp.Get("isMissing").AsBool()) { parts.Add("Missing Script"); continue; }
                parts.Add(comp.Get("typeName").AsString("Component"));
            }
            return string.Join(", ", parts.ToArray());
        }

        private static string BuildSub(string sceneName, string components, JsonValue go)
        {
            string prefix = sceneName;
            if (go.Has("prefab"))
            {
                string prefabName = go.Get("prefab").Get("assetName").AsString("");
                prefix += go.Get("prefab").Get("isVariant").AsBool() ? " · Prefab Variant" : " · Prefab";
                if (!string.IsNullOrEmpty(prefabName)) { prefix += " " + prefabName; }
            }
            return string.IsNullOrEmpty(components) ? prefix : prefix + " — " + components;
        }

        private static string Trim(string s)
        {
            if (string.IsNullOrEmpty(s)) { return ""; }
            string cleaned = s.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
            return cleaned.Length <= MaxSubLength ? cleaned : cleaned.Substring(0, MaxSubLength) + "…";
        }
    }
}
