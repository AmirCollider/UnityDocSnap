// ==========================================
// DocSnapAboutWindow
// A small, friendly About panel: version,
// edition, tagline, and links back to the repo
// and the product page.
// ==========================================
using AmirCollider.UnityDocSnap.Editor.Licensing;
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
            window.minSize = new Vector2(380, 290);
            window.maxSize = new Vector2(380, 290);
        }

        // ==========================================
        // OnGUI
        // ==========================================
        private void OnGUI()
        {
            GUILayout.Space(10);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
            GUILayout.Label("🧋 " + DocSnapConstants.ToolName, titleStyle);

            // The edition sits on the version line rather than in a
            // panel of its own. It is one of the two facts somebody
            // opens an About window to check, and a window that
            // shows the version but makes you go elsewhere to learn
            // which edition you are running is doing half the job.
            bool isPro = DocSnapLicense.IsPro;
            GUILayout.Label("v" + DocSnapConstants.Version + "  ·  "
                + (isPro ? "✨ Pro" : "🆓 Free"), EditorStyles.miniLabel);

            GUILayout.Space(8);
            GUILayout.Label(
                "Snap your whole Unity project into a cozy little\noffline website - full Hierarchy, full Inspector data,\nand asset import settings, for humans and AI alike.",
                EditorStyles.wordWrappedLabel);
            GUILayout.Space(8);
            GUILayout.Label("✨ Made with 🧋 by " + DocSnapConstants.Author, EditorStyles.label);
            GUILayout.Space(10);

            if (GUILayout.Button("Open GitHub Repository"))
            {
                Application.OpenURL(DocSnapConstants.GithubUrl);
            }

            GUILayout.Space(4);
            if (isPro)
            {
                // Nothing to sell somebody who already bought. The
                // useful button here is the one that manages what
                // they have.
                if (GUILayout.Button("Licence & devices"))
                {
                    DocSnapLicenseWindow.ShowWindow();
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("✨ Get Pro — " + DocSnapConstants.ProPriceDisplay))
                {
                    Application.OpenURL(DocSnapConstants.ProductUrl);
                }
                if (GUILayout.Button("I have a key", GUILayout.Width(110)))
                {
                    DocSnapLicenseWindow.ShowWindow();
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.Label("MIT License", EditorStyles.centeredGreyMiniLabel);
        }
    }
}
