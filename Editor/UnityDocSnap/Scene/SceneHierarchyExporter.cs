// ==========================================
// SceneHierarchyExporter
// Reads a Scene's full GameObject hierarchy,
// including every Component's live Inspector
// data, without disturbing whatever the user
// currently has open in the Editor.
// Namespace is "SceneExport" (not "Scene") to
// avoid colliding with UnityEngine.SceneManagement.Scene.
// ==========================================
using System;
using System.IO;
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmirCollider.UnityDocSnap.Editor.SceneExport
{
    internal static class SceneHierarchyExporter
    {
        // ==========================================
        // ExportScene
        // Opens the target Scene additively only if
        // it is not already loaded, walks every root
        // GameObject, then restores the original
        // Scene load state exactly as it was found.
        // Console logging is briefly disabled around
        // the additive open/close, since URP's 2D
        // Renderer logs a "more than one global light"
        // warning the instant a second Scene's Global
        // Light 2Ds coexist with the caller's already-
        // open Scene - a transient condition this
        // method itself creates and immediately
        // unwinds, not a real project issue.
        // ==========================================
        public static JsonValue ExportScene(string scenePath, out int gameObjectCount)
        {
            gameObjectCount = 0;

            string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", scenePath));
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException("Scene not found on disk: " + scenePath);
            }

            Scene scene = default;
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                Scene candidate = EditorSceneManager.GetSceneAt(i);
                if (candidate.path == scenePath && candidate.isLoaded)
                {
                    scene = candidate;
                    break;
                }
            }

            bool weOpenedIt = false;
            bool previousLogEnabled = Debug.unityLogger.logEnabled;
            if (!scene.IsValid())
            {
                Debug.unityLogger.logEnabled = false;
                try
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }
                finally
                {
                    Debug.unityLogger.logEnabled = previousLogEnabled;
                }
                weOpenedIt = true;
            }

            try
            {
                var root = JsonValue.Obj();
                root.Set("sceneName", Path.GetFileNameWithoutExtension(scenePath));
                root.Set("scenePath", scenePath);
                root.Set("exportedUtc", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                root.Set("unityVersion", Application.unityVersion);

                var rootObjectsArr = JsonValue.Arr();
                int counter = 0;
                foreach (GameObject go in scene.GetRootGameObjects())
                {
                    rootObjectsArr.Add(BuildGameObjectNode(go, ref counter));
                }
                root.Set("rootObjects", rootObjectsArr);
                root.Set("totalGameObjects", counter);
                gameObjectCount = counter;
                return root;
            }
            finally
            {
                if (weOpenedIt)
                {
                    Debug.unityLogger.logEnabled = false;
                    try
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                    finally
                    {
                        Debug.unityLogger.logEnabled = previousLogEnabled;
                    }
                }
            }
        }

        // ==========================================
        // BuildStandaloneGameObjectTree
        // Public entry point for walking a GameObject
        // that is not part of a loaded Scene (namely a
        // Prefab asset's root, loaded read-only via
        // AssetDatabase.LoadAssetAtPath<GameObject>).
        // ==========================================
        public static JsonValue BuildStandaloneGameObjectTree(GameObject root, out int gameObjectCount)
        {
            int counter = 0;
            JsonValue node = BuildGameObjectNode(root, ref counter);
            gameObjectCount = counter;
            return node;
        }

        // ==========================================
        // BuildGameObjectNode
        // Recursively captures one GameObject: its
        // flags, Transform, every Component (via the
        // universal reflector), and its children.
        // ==========================================
        private static JsonValue BuildGameObjectNode(GameObject go, ref int counter)
        {
            counter++;

            var node = JsonValue.Obj();
            node.Set("name", go.name);
            node.Set("instanceId", go.GetInstanceID());
            node.Set("activeSelf", go.activeSelf);
            node.Set("activeInHierarchy", go.activeInHierarchy);
            node.Set("isStatic", go.isStatic);

            string tag = "Untagged";
            try { tag = go.tag; } catch { /* tag no longer exists in TagManager */ }
            node.Set("tag", tag);
            node.Set("layerIndex", go.layer);
            node.Set("layerName", LayerMask.LayerToName(go.layer));

            AddPrefabInfo(go, node);

            node.Set("transform", TransformNode(go.transform));
            node.Set("components", BuildComponentsArray(go));

            var childrenArr = JsonValue.Arr();
            Transform t = go.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                childrenArr.Add(BuildGameObjectNode(t.GetChild(i).gameObject, ref counter));
            }
            node.Set("children", childrenArr);

            return node;
        }

        // ==========================================
        // AddPrefabInfo
        // Records whether a GameObject is a Prefab
        // instance, which Prefab asset it comes from,
        // and what kind (Regular / Variant / Model). For
        // the outermost instance root it also rolls up
        // how many components / child objects were added
        // or removed relative to the Prefab. This is the
        // Prefab-awareness a documentation tool needs:
        // "this object is an instance of X (a Variant),
        // with 2 added components". Every Prefab API call
        // is best-effort - a missing/older API or an
        // unexpected context never fails an export.
        // ==========================================
        private static void AddPrefabInfo(GameObject go, JsonValue node)
        {
            try
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(go)) { return; }

                var prefab = JsonValue.Obj();
                prefab.Set("isInstance", true);

                bool isRoot = PrefabUtility.IsOutermostPrefabInstanceRoot(go);
                prefab.Set("isInstanceRoot", isRoot);

                PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType(go);
                prefab.Set("assetType", assetType.ToString());
                prefab.Set("isVariant", assetType == PrefabAssetType.Variant);

                string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    prefab.Set("assetPath", assetPath);
                    prefab.Set("assetName", Path.GetFileNameWithoutExtension(assetPath));
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);
                    if (!string.IsNullOrEmpty(guid)) { prefab.Set("assetGuid", guid); }
                }

                if (isRoot)
                {
                    try { prefab.Set("addedComponents", PrefabUtility.GetAddedComponents(go).Count); } catch { /* best-effort */ }
                    try { prefab.Set("removedComponents", PrefabUtility.GetRemovedComponents(go).Count); } catch { /* best-effort */ }
                    try { prefab.Set("addedGameObjects", PrefabUtility.GetAddedGameObjects(go).Count); } catch { /* best-effort */ }
                }

                node.Set("prefab", prefab);
            }
            catch
            {
                // Prefab reflection is a nice-to-have; never let it break an export.
            }
        }

        // ==========================================
        // BuildComponentsArray
        // Lists every Component except Transform,
        // whose data already lives in the dedicated
        // "transform" field above. Null entries (a
        // missing script) are reported explicitly.
        // ==========================================
        private static JsonValue BuildComponentsArray(GameObject go)
        {
            var componentsArr = JsonValue.Arr();
            Component[] components = go.GetComponents<Component>();
            foreach (Component c in components)
            {
                if (c == null)
                {
                    componentsArr.Add(JsonValue.Obj()
                        .Set("typeName", "Missing Script")
                        .Set("isMissing", true));
                    continue;
                }

                if (c is Transform) { continue; }

                componentsArr.Add(BuildComponentNode(c));
            }
            return componentsArr;
        }

        // ==========================================
        // BuildComponentNode
        // Captures one Component's identity (type,
        // enabled state, backing script path) plus
        // every serialized field via the reflector.
        // ==========================================
        private static JsonValue BuildComponentNode(Component c)
        {
            var node = JsonValue.Obj();
            node.Set("typeName", c.GetType().Name);
            node.Set("instanceId", c.GetInstanceID());
            node.Set("isMissing", false);

            Behaviour behaviour = c as Behaviour;
            if (behaviour != null)
            {
                node.Set("isBehaviour", true);
                node.Set("enabled", behaviour.enabled);
            }
            else
            {
                node.Set("isBehaviour", false);
            }

            MonoBehaviour mb = c as MonoBehaviour;
            node.Set("isUserScript", mb != null);
            if (mb != null)
            {
                MonoScript script = MonoScript.FromMonoBehaviour(mb);
                if (script != null)
                {
                    node.Set("scriptPath", AssetDatabase.GetAssetPath(script));
                }
            }

            // A component that exists on this Prefab instance but not on the
            // Prefab asset is an "added component" override - worth calling
            // out, since it is configuration that lives only on the instance.
            try
            {
                if (PrefabUtility.IsPartOfPrefabInstance(c) && PrefabUtility.IsAddedComponentOverride(c))
                {
                    node.Set("isAddedComponent", true);
                }
            }
            catch
            {
                // best-effort prefab awareness only
            }

            node.Set("fields", UniversalReflector.ReadTopLevelFields(c));
            return node;
        }

        // ==========================================
        // TransformNode
        // Records both world and local Transform data
        // so the site can show either at a glance.
        // ==========================================
        private static JsonValue TransformNode(Transform t)
        {
            var node = JsonValue.Obj();
            node.Set("position", Vec3(t.position));
            node.Set("localPosition", Vec3(t.localPosition));
            node.Set("eulerAngles", Vec3(t.eulerAngles));
            node.Set("localEulerAngles", Vec3(t.localEulerAngles));
            node.Set("localScale", Vec3(t.localScale));
            return node;
        }

        private static JsonValue Vec3(Vector3 v)
        {
            return JsonValue.Obj().Set("x", v.x).Set("y", v.y).Set("z", v.z);
        }
    }
}
