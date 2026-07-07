// ==========================================
// DocSnapSettingsProvider
// Exposes DocSnapSettings under Edit > Project
// Settings > Unity DocSnap, using Unity's own
// SettingsProvider API rather than adding an
// extra item to the fixed "Unity DocSnap" menu
// tree documented in README.md.
// ==========================================
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapSettingsProvider
    {
        // ==========================================
        // CreateSettingsProvider
        // ==========================================
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/Unity DocSnap", SettingsScope.Project)
            {
                label = "Unity DocSnap",
                guiHandler = DrawSettingsGui,
                keywords = new HashSet<string>(new[] { "DocSnap", "Unity DocSnap", "documentation", "export", "hierarchy" })
            };
            return provider;
        }

        // ==========================================
        // DrawSettingsGui
        // ==========================================
        private static void DrawSettingsGui(string searchContext)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string outputPath = EditorGUILayout.TextField(
                new GUIContent("Output Path", "Empty = default UnityDocSnap_Output next to Assets"),
                DocSnapSettings.OutputRootPath);
            if (EditorGUI.EndChangeCheck()) { DocSnapSettings.OutputRootPath = outputPath; }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            bool thumbs = EditorGUILayout.Toggle(
                new GUIContent("Generate Image Thumbnails", "On by default so image assets get real preview thumbnails. Turn this off if you need DocSnap's stricter mode where pixels never leave your project."),
                DocSnapSettings.GenerateThumbnails);
            if (EditorGUI.EndChangeCheck()) { DocSnapSettings.GenerateThumbnails = thumbs; }

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            string logoPath = EditorGUILayout.TextField(
                new GUIContent("Custom Logo Path", "Empty = built-in mascot mark. Accepts .png, .jpg, or .svg."),
                DocSnapSettings.CustomLogoAbsolutePath);
            if (EditorGUI.EndChangeCheck()) { DocSnapSettings.CustomLogoAbsolutePath = logoPath; }
            if (GUILayout.Button("Browse...", GUILayout.Width(90)))
            {
                string picked = EditorUtility.OpenFilePanel("Choose a logo image", Application.dataPath, "png,jpg,jpeg,svg");
                if (!string.IsNullOrEmpty(picked)) { DocSnapSettings.CustomLogoAbsolutePath = picked; }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(14);
            EditorGUILayout.HelpBox(
                "These settings only change how the next export looks. Run any Unity DocSnap export again afterwards to see them take effect.",
                MessageType.None);
        }
    }
}
