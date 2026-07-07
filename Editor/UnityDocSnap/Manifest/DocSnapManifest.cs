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
        // BuildGuidLookup — indexes assetIndex by GUID
        // for fast cross-link resolution while
        // rendering HTML pages.
        // ==========================================
        public static Dictionary<string, ManifestAssetIndexEntry> BuildGuidLookup(ManifestState state)
        {
            var map = new Dictionary<string, ManifestAssetIndexEntry>();
            foreach (var entry in state.assetIndex)
            {
                if (!string.IsNullOrEmpty(entry.guid)) { map[entry.guid] = entry; }
            }
            return map;
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
