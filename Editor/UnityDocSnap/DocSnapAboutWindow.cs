// ==========================================
// DocSnapAboutWindow
// A small, friendly About panel: version,
// tagline, and a link back to the repo.
// ==========================================
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal sealed class DocSnapAboutWindow : EditorWindow
    {
        // ==========================================
        // ShowWindow
        // ==========================================
        public static void ShowWindow()
        {
            var window = GetWindow<DocSnapAboutWindow>(true, "About " + DocSnapConstants.ToolName, true);
            window.minSize = new Vector2(360, 220);
            window.maxSize = new Vector2(360, 220);
        }

        // ==========================================
        // OnGUI
        // ==========================================
        private void OnGUI()
        {
            GUILayout.Space(10);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
            GUILayout.Label("\uD83E\uDDCB " + DocSnapConstants.ToolName, titleStyle);
            GUILayout.Label("v" + DocSnapConstants.Version, EditorStyles.miniLabel);
            GUILayout.Space(8);
            GUILayout.Label(
                "Snap your whole Unity project into a cozy little\noffline website - full Hierarchy, full Inspector data,\nand asset import settings, for humans and AI alike.",
                EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);
            GUILayout.Label("\u2728 Made with \uD83E\uDDCB by " + DocSnapConstants.Author, EditorStyles.label);
            GUILayout.Space(14);

            if (GUILayout.Button("Open GitHub Repository"))
            {
                Application.OpenURL(DocSnapConstants.GithubUrl);
            }
            GUILayout.Space(6);
            GUILayout.Label("MIT License", EditorStyles.centeredGreyMiniLabel);
        }
    }
}
