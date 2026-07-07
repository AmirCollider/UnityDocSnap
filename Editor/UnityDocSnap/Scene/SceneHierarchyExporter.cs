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
            if (!scene.IsValid())
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
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
                    EditorSceneManager.CloseScene(scene, true);
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
