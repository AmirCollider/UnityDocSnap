// ==========================================
// DocSnapManifest
// Tracks what has been exported so far so
// repeated exports are incremental and so
// cross-links between Scenes and Assets can
// be resolved even when they were exported
// on different runs.
//
// Internal roundtrip state uses UnityEngine's
// built-in JsonUtility (simple, fixed-shape,
// zero third-party dependency). The public,
// human/AI-facing data/manifest.json is written
// separately with JsonValue for full control
// over its shape.
// ==========================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Json;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor.Manifest
{
    [Serializable]
    internal sealed class ManifestSceneEntry
    {
        public string sceneName;

        // The base name every output file for this Scene is
        // written under (see DocSnapNaming.SceneKey). Split
        // from sceneName because they are no longer always the
        // same string: two Scenes both called "Main" in
        // different folders used to produce the same
        // scenes/Main.html and silently overwrite each other,
        // so a name that collides now gets a short stable hash
        // suffix while sceneName stays the friendly label shown
        // in the sidebar.
        public string sceneKey;

        public string scenePath;
        public string htmlFile;
        public string jsonFile;
        public string exportedUtc;
        public int gameObjectCount;

        // A cheap fingerprint of the Scene's source file (see
        // DocSnapExportService.SceneSignature). An incremental
        // "Update Previous Export" reuses this Scene's existing
        // output when the fingerprint is unchanged, instead of
        // re-opening and re-walking the whole Scene.
        public string sourceSignature;
    }

    [Serializable]
    internal sealed class ManifestFolderEntry
    {
        public string folderPath;
        public string folderKey;
        public string htmlFile;
        public string jsonFile;
        public string exportedUtc;
        public int fileCount;

        // Fingerprint of every file under the folder (count + newest
        // write time). Lets an incremental update skip the expensive
        // per-asset pass when nothing in the folder changed.
        public string sourceSignature;
    }

    // ==========================================
    // ManifestPackageEntry
    // One row of the "Packages used" page: a UPM
    // package the project depends on, tagged as
    // Unity's own or third-party (Asset Store / Git),
    // with an access link and whether Unity reports a
    // newer version available.
    // ==========================================
    [Serializable]
    internal sealed class ManifestPackageEntry
    {
        public string name;
        public string displayName;
        public string version;
        public string latestVersion;
        public bool updateAvailable;
        public string source;
        public string category; // "unity" | "thirdparty"
        public string author;
        public string description;
        public string url;
    }

    // ==========================================
    // ManifestSearchEntry
    // One lightweight, searchable record baked into
    // the site's embedded search index. Kept tiny on
    // purpose (name + one line of context + a link)
    // so even a huge project's index stays small and
    // fast to filter in the browser.
    // ==========================================
    [Serializable]
    internal sealed class ManifestSearchEntry
    {
        public string scope;    // sceneName / folderKey the record belongs to (for re-export replacement)
        public string group;    // "scene" | "asset"
        public string category; // GameObject / Component / Asset / Folder / Scene
        public string name;
        public string sub;      // secondary text (component list / path)
        public string url;      // htmlFile#anchor, relative to the output root
    }

    // ==========================================
    // ManifestHealthEntry
    // What one exported Scene / folder reported about
    // its own condition: missing scripts, references
    // whose target is gone, assets Unity could not
    // resolve a type for. Stored per scope so a
    // single-Scene re-export refreshes only its own row
    // and leaves every other scope's findings intact -
    // the same rule the search records follow.
    // ==========================================
    [Serializable]
    internal sealed class ManifestHealthEntry
    {
        public string scope;      // sceneKey / folderKey
        public string group;      // "scene" | "asset"
        public string label;      // friendly name for the dashboard
        public string htmlFile;   // page to link the finding to
        public int itemCount;     // GameObjects / files in this scope
        public int missingScripts;
        public int missingReferences;
        public int unresolvedAssets;

        // The same three counts restricted to the project's OWN
        // content. Kept as separate totals rather than derived from
        // the issue list, because that list is capped per scope
        // while these are exact - and "how many of these are
        // actually mine?" is the first thing a reader asks.
        public int missingScriptsMine;
        public int missingReferencesMine;
        public int unresolvedAssetsMine;

        // True when this scope produced more findings than
        // MaxIssuesPerScope, so the issues page can say the list
        // is partial instead of quietly disagreeing with the
        // count shown right above it.
        public bool issuesTruncated;
    }

    // ==========================================
    // ManifestIssueEntry
    // ONE finding, with enough context to walk straight
    // to it.
    //
    // The counts in ManifestHealthEntry answer "how many?"
    // and nothing else: the dashboard said "8 broken
    // references" and linked to the Assets page, which on a
    // real project is thousands of rows long. Being told a
    // number and then handed a haystack is not much better
    // than not being told. Each finding now carries the
    // object it lives on, the component and field that hold
    // it, and the anchor of the card that renders it - so
    // the link lands on the actual problem.
    // ==========================================
    [Serializable]
    internal sealed class ManifestIssueEntry
    {
        public string scope;       // sceneKey / folderKey — replaced with the health row
        public string group;       // "scene" | "asset"
        public string kind;        // "missingScript" | "missingReference" | "unresolvedAsset"
        public string scopeLabel;  // "MainMenu" / "Assets/UI"
        public string location;    // "Canvas/Panel/StartButton" or "Assets/UI/icon.png"
        public string detail;      // "PlayerController › targetTransform"
        public string htmlFile;    // "scenes/MainMenu.html"
        public string anchor;      // "go-12345" / "asset-<guid>"

        // "mine" | "vendor" — whether this is the author's own
        // content or something Unity / a package installed into
        // Assets/ (see DocSnapVendorPaths).
        //
        // Without this the report was honest but unusable: a
        // project would say eight findings, seven in
        // Assets/Settings and one in Assets/TextMesh Pro, none of
        // them the author's to fix and none of them removable -
        // and they sat at the top of the list on every export,
        // burying anything that actually was. A count you are
        // expected to ignore teaches you to ignore the count.
        public string owner;

        // The vendor folder this finding fell under, so the page
        // can answer "why is this not mine?" without the reader
        // having to guess. "" for the project's own files.
        public string ownerNote;
    }

    [Serializable]
    internal sealed class ManifestAssetIndexEntry
    {
        public string guid;
        public string folderKey;
        public string htmlFile;
        public string anchor;
        public string name;
    }

    [Serializable]
    internal sealed class ManifestState
    {
        public string projectName = "";
        public string unityVersion = "";
        public string lastUpdatedUtc = "";
        public List<ManifestSceneEntry> scenes = new List<ManifestSceneEntry>();
        public List<ManifestFolderEntry> assetFolders = new List<ManifestFolderEntry>();
        public List<ManifestAssetIndexEntry> assetIndex = new List<ManifestAssetIndexEntry>();
        public List<ManifestPackageEntry> packages = new List<ManifestPackageEntry>();
        public List<ManifestSearchEntry> searchRecords = new List<ManifestSearchEntry>();
        public List<ManifestHealthEntry> health = new List<ManifestHealthEntry>();
        public List<ManifestIssueEntry> issues = new List<ManifestIssueEntry>();
        public string packagesExportedUtc = "";

        // The exclude patterns the last export ran with, so the
        // dashboard and export-info can state plainly what was
        // deliberately left out. An omission nobody is told
        // about is worse than no omission.
        public List<string> excludePatterns = new List<string>();

        // ==========================================
        // loadedFromDisk
        // Whether this state was actually read back from a
        // prior run, as opposed to being a blank one handed
        // out because there was nothing to read.
        //
        // It exists for exactly one consumer, and that
        // consumer deletes files. PruneStaleOutput treats
        // the manifest as the complete record of everything
        // this project has ever exported, and removes any
        // file in a managed output folder that the record
        // does not mention. That premise is sound only while
        // the record survives - and it lives in Library/,
        // which is machine-local, git-ignored, and routinely
        // deleted outright to make Unity behave.
        //
        // Load() answers a missing or unreadable state file
        // by returning a blank one and carrying on, which is
        // right for every other caller. For the pruner it
        // meant a manifest describing one Scene could arrive
        // at a version folder holding twenty, and every page,
        // JSON and summary belonging to the other nineteen
        // was "unrecognised" - and deleted. The version
        // folder is the artefact the user keeps; nothing else
        // in the tool can put it back.
        //
        // [NonSerialized] so it never round-trips through the
        // state file: it is a fact about THIS load, and a
        // serialized `true` read back out of a corrupt file
        // would defeat the whole point.
        [NonSerialized] public bool loadedFromDisk;
    }

    internal static class DocSnapManifest
    {
        // ==========================================
        // InternalStateAbsolutePath — resolves the
        // Library-local roundtrip file for the
        // current project (never part of the
        // published output).
        // ==========================================
        public static string InternalStateAbsolutePath()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, DocSnapConstants.InternalStateRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        // ==========================================
        // Load — reads prior state, or returns a
        // fresh, empty state on first run.
        // ==========================================
        public static ManifestState Load()
        {
            string path = InternalStateAbsolutePath();
            if (!File.Exists(path))
            {
                return new ManifestState { projectName = ResolveProjectName(), unityVersion = Application.unityVersion };
            }

            try
            {
                string text = File.ReadAllText(path);
                ManifestState state = JsonUtility.FromJson<ManifestState>(text);
                if (state == null) { state = new ManifestState(); }
                state.scenes = state.scenes ?? new List<ManifestSceneEntry>();
                state.assetFolders = state.assetFolders ?? new List<ManifestFolderEntry>();
                state.assetIndex = state.assetIndex ?? new List<ManifestAssetIndexEntry>();
                state.packages = state.packages ?? new List<ManifestPackageEntry>();
                state.searchRecords = state.searchRecords ?? new List<ManifestSearchEntry>();
                state.health = state.health ?? new List<ManifestHealthEntry>();
                state.issues = state.issues ?? new List<ManifestIssueEntry>();
                state.excludePatterns = state.excludePatterns ?? new List<string>();
                BackfillSceneKeys(state);
                // Set only here: this is the one path where a real
                // prior record was successfully read back. See the
                // field's own comment for why the pruner cares.
                state.loadedFromDisk = true;
                return state;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Unity DocSnap] Could not read prior manifest state, starting fresh. " + ex.Message);
                return new ManifestState { projectName = ResolveProjectName(), unityVersion = Application.unityVersion };
            }
        }

        // ==========================================
        // Save — writes the roundtrip state back to
        // the Library folder for the next export run.
        // ==========================================
        public static void Save(ManifestState state)
        {
            state.projectName = ResolveProjectName();
            state.unityVersion = Application.unityVersion;
            state.lastUpdatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            string path = InternalStateAbsolutePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(state, true));
        }

        // ==========================================
        // BackfillSceneKeys
        // State written by a DocSnap older than 0.7.0 has no
        // sceneKey. Its output files were named after the
        // Scene name, so that is exactly what the key was -
        // filling it in keeps every previously exported page,
        // summary and cross-link resolving instead of the
        // upgrade orphaning them.
        // ==========================================
        private static void BackfillSceneKeys(ManifestState state)
        {
            foreach (ManifestSceneEntry scene in state.scenes)
            {
                if (string.IsNullOrEmpty(scene.sceneKey)) { scene.sceneKey = scene.sceneName; }
            }
        }

        // ==========================================
        // ReplaceHealthForScope — swaps in one scope's
        // fresh findings, mirroring the search records so
        // the two never disagree about what still exists.
        // ==========================================
        public static void ReplaceHealthForScope(ManifestState state, string scope, ManifestHealthEntry entry)
        {
            state.health.RemoveAll(h => h.scope == scope);
            if (entry != null) { state.health.Add(entry); }
        }

        // ==========================================
        // ReplaceIssuesForScope — the per-finding detail
        // behind one scope's health counts, swapped in the
        // same breath so a row saying "3 broken references"
        // can never list two.
        // ==========================================
        public static void ReplaceIssuesForScope(ManifestState state, string scope, List<ManifestIssueEntry> entries)
        {
            state.issues.RemoveAll(i => i.scope == scope);
            if (entries != null) { state.issues.AddRange(entries); }
        }

        // ==========================================
        // SetExcludePatterns — records what this export
        // deliberately skipped.
        // ==========================================
        public static void SetExcludePatterns(ManifestState state, List<string> patterns)
        {
            state.excludePatterns = patterns ?? new List<string>();
        }

        // ==========================================
        // ResolveProjectName — the folder name that
        // contains Assets/, used purely as a label.
        // ==========================================
        private static string ResolveProjectName()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return new DirectoryInfo(projectRoot).Name;
        }

        // ==========================================
        // UpsertScene — records/updates one Scene's
        // export location, replacing any prior entry
        // for the same Scene path.
        // ==========================================
        public static void UpsertScene(ManifestState state, ManifestSceneEntry entry)
        {
            state.scenes.RemoveAll(s => s.scenePath == entry.scenePath);
            state.scenes.Add(entry);
            state.scenes.Sort((a, b) => string.Compare(a.sceneName, b.sceneName, StringComparison.OrdinalIgnoreCase));
        }

        // ==========================================
        // UpsertFolder — records/updates one asset
        // folder's export location.
        // ==========================================
        public static void UpsertFolder(ManifestState state, ManifestFolderEntry entry)
        {
            state.assetFolders.RemoveAll(f => f.folderKey == entry.folderKey);
            state.assetFolders.Add(entry);
            state.assetFolders.Sort((a, b) => string.Compare(a.folderPath, b.folderPath, StringComparison.OrdinalIgnoreCase));
        }

        // ==========================================
        // ReplaceAssetIndexForFolder — swaps in the
        // freshly exported asset->page lookups for a
        // folder, dropping stale entries first (so
        // deleted files stop appearing as false links).
        // ==========================================
        public static void ReplaceAssetIndexForFolder(ManifestState state, string folderKey, List<ManifestAssetIndexEntry> freshEntries)
        {
            state.assetIndex.RemoveAll(a => a.folderKey == folderKey);
            state.assetIndex.AddRange(freshEntries);
        }

        // ==========================================
        // ReplaceSearchRecordsForScope — swaps in the
        // freshly built search records for one Scene or
        // folder, dropping that scope's previous records
        // first so a re-export never leaves stale entries
        // pointing at objects that no longer exist.
        // ==========================================
        public static void ReplaceSearchRecordsForScope(ManifestState state, string scope, List<ManifestSearchEntry> freshEntries)
        {
            state.searchRecords.RemoveAll(r => r.scope == scope);
            if (freshEntries != null) { state.searchRecords.AddRange(freshEntries); }
        }

        // ==========================================
        // FindScene / FindFolder — locate a prior entry
        // by its source path/key, used by the incremental
        // update to decide whether an item can be reused.
        // ==========================================
        public static ManifestSceneEntry FindScene(ManifestState state, string scenePath)
        {
            return state.scenes.Find(s => s.scenePath == scenePath);
        }

        public static ManifestFolderEntry FindFolder(ManifestState state, string folderKey)
        {
            return state.assetFolders.Find(f => f.folderKey == folderKey);
        }

        // ==========================================
        // SetPackages — replaces the recorded package
        // list wholesale (packages are project-global,
        // not per Scene/folder) and stamps the time.
        // ==========================================
        public static void SetPackages(ManifestState state, List<ManifestPackageEntry> packages)
        {
            state.packages = packages ?? new List<ManifestPackageEntry>();
            state.packagesExportedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        // ==========================================
        // BuildGuidLookup — indexes assetIndex by GUID
        // for fast cross-link resolution while
        // rendering HTML pages.
        // ==========================================
       public static Dictionary<string, ManifestAssetIndexEntry> BuildGuidLookup(ManifestState state)
        {
            var map = new Dictionary<string, ManifestAssetIndexEntry>();
            foreach (var entry in state.assetIndex)
            {
                if (string.IsNullOrEmpty(entry.guid)) { continue; }

                ManifestAssetIndexEntry existing;
                if (!map.TryGetValue(entry.guid, out existing))
                {
                    map[entry.guid] = entry;
                    continue;
                }

                // The same asset is indexed once per exported folder
                // that contains it (the root "Assets" page plus any
                // sub-folder page). Plain last-writer-wins made every
                // cross-link resolve to whichever entry happened to
                // be appended last, which changed between runs.
                // Resolve deterministically to the most specific
                // (deepest) page, which is also the smallest one to
                // open.
                if (IsMoreSpecific(entry, existing)) { map[entry.guid] = entry; }
            }
            return map;
        }

        // ==========================================
        // IsMoreSpecific
        // A longer folderKey means a deeper, narrower
        // page. Ordinal comparison breaks exact ties so
        // the result never depends on list order.
        // ==========================================
        private static bool IsMoreSpecific(ManifestAssetIndexEntry candidate, ManifestAssetIndexEntry current)
        {
            string candidateKey = candidate.folderKey ?? "";
            string currentKey = current.folderKey ?? "";

            if (candidateKey.Length != currentKey.Length)
            {
                return candidateKey.Length > currentKey.Length;
            }
            return string.CompareOrdinal(candidateKey, currentKey) < 0;
        }

        // ==========================================
        // WritePublicJson — emits the human/AI-facing
        // data/manifest.json summary using JsonValue.
        // ==========================================
        public static void WritePublicJson(ManifestState state, string filePath)
        {
            var root = JsonValue.Obj();
            root.Set("projectName", state.projectName);
            root.Set("unityVersion", state.unityVersion);
            root.Set("lastUpdatedUtc", state.lastUpdatedUtc);
            root.Set("generatedBy", DocSnapConstants.ToolName + " v" + DocSnapConstants.Version);

            var scenesArr = JsonValue.Arr();
            foreach (var s in state.scenes)
            {
                scenesArr.Add(JsonValue.Obj()
                    .Set("sceneName", s.sceneName)
                    .Set("sceneKey", s.sceneKey)
                    .Set("scenePath", s.scenePath)
                    .Set("htmlFile", s.htmlFile)
                    .Set("jsonFile", s.jsonFile)
                    .Set("exportedUtc", s.exportedUtc)
                    .Set("gameObjectCount", s.gameObjectCount));
            }
            root.Set("scenes", scenesArr);

            var foldersArr = JsonValue.Arr();
            foreach (var f in state.assetFolders)
            {
                foldersArr.Add(JsonValue.Obj()
                    .Set("folderPath", f.folderPath)
                    .Set("folderKey", f.folderKey)
                    .Set("htmlFile", f.htmlFile)
                    .Set("jsonFile", f.jsonFile)
                    .Set("exportedUtc", f.exportedUtc)
                    .Set("fileCount", f.fileCount));
            }
            root.Set("assetFolders", foldersArr);

            // What this export left out, stated rather than
            // implied - a reader (or an AI) comparing the file
            // count here against the Project window needs to
            // know an exclude rule is why they differ.
            var excludesArr = JsonValue.Arr();
            foreach (string pattern in state.excludePatterns) { excludesArr.Add(JsonValue.Str(pattern)); }
            root.Set("excludedPatterns", excludesArr);

            // The findings the dashboard leads with, so anything
            // consuming the JSON gets them without re-deriving
            // them from the full per-Scene data.
            var healthArr = JsonValue.Arr();
            foreach (ManifestHealthEntry h in state.health)
            {
                if (h.missingScripts == 0 && h.missingReferences == 0 && h.unresolvedAssets == 0) { continue; }
                healthArr.Add(JsonValue.Obj()
                    .Set("scope", h.scope)
                    .Set("group", h.group)
                    .Set("label", h.label)
                    .Set("htmlFile", h.htmlFile)
                    .Set("missingScripts", h.missingScripts)
                    .Set("missingReferences", h.missingReferences)
                    .Set("unresolvedAssets", h.unresolvedAssets)
                    .Set("issuesTruncated", h.issuesTruncated));
            }
            root.Set("health", healthArr);

            // The individual findings behind those counts. An AI
            // assistant handed this file can now name the object and
            // field that are broken instead of only how many are.
            var issuesArr = JsonValue.Arr();
            foreach (ManifestIssueEntry i in state.issues)
            {
                issuesArr.Add(JsonValue.Obj()
                    .Set("kind", i.kind)
                    .Set("group", i.group)
                    .Set("scope", i.scopeLabel)
                    .Set("location", i.location)
                    .Set("detail", i.detail)
                    .Set("owner", string.IsNullOrEmpty(i.owner) ? DocSnapVendorPaths.OwnerMine : i.owner)
                    .Set("page", string.IsNullOrEmpty(i.anchor) ? i.htmlFile : i.htmlFile + "#" + i.anchor));
            }
            root.Set("issues", issuesArr);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            // Streamed rather than materialised: this document carries
            // every issue in the project, and on a project with
            // thousands of them the string was large enough to be worth
            // not building. Same bytes, same writer - see
            // JsonValue.WriteTo.
            using (var writer = new StreamWriter(filePath, false, new UTF8Encoding(false)))
            {
                root.WriteTo(writer);
            }
        }
    }
}
