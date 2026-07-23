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
using AmirCollider.UnityDocSnap.Editor.Json;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor.Manifest
{
    [Serializable]
    internal sealed class ManifestSceneEntry
    {
        public string sceneName;
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
        public string packagesExportedUtc = "";
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

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, root.ToString());
        }
    }
}
