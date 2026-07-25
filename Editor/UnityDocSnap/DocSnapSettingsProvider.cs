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
            EditorGUILayout.LabelField("Scope", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                new GUIContent("Exclude paths", "One path or pattern per line. Prefixes match a whole folder (Assets/Plugins); * and ? are wildcards (*.psd)."),
                EditorStyles.label);
            EditorGUI.BeginChangeCheck();
            string excludes = EditorGUILayout.TextArea(DocSnapSettings.ExcludePatterns, GUILayout.MinHeight(64));
            if (EditorGUI.EndChangeCheck()) { DocSnapSettings.ExcludePatterns = excludes; }

            DocSnapExcludeFilter filter = DocSnapExcludeFilter.Parse(excludes);
            EditorGUILayout.LabelField(
                filter.IsEmpty ? "Nothing excluded — the whole project is documented." : "Active rules: " + string.Join(" · ", filter.Patterns.ToArray()),
                EditorStyles.miniLabel);

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Health report", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                new GUIContent("Not-my-code folders",
                    "One folder per line. These are still fully documented — they are only separated out in the health report, so \"8 broken references\" can say how many are actually yours to fix."),
                EditorStyles.label);
            EditorGUI.BeginChangeCheck();
            string vendors = EditorGUILayout.TextArea(DocSnapSettings.VendorFolders, GUILayout.MinHeight(48));
            if (EditorGUI.EndChangeCheck()) { DocSnapSettings.VendorFolders = vendors; }
            EditorGUILayout.LabelField(
                "Always included: " + string.Join(" · ", DocSnapVendorPaths.VendorFolders().ToArray()),
                EditorStyles.miniLabel);

            // The Changes page's own noise filter. Kept next to the
            // health report's because it answers the same shape of
            // question - "which of these is actually mine?" - just for
            // a different page.
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Changes page", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                new GUIContent("Rewritten-by-Unity paths",
                    "One path or pattern per line, same syntax as Exclude paths. These files are still documented in full — they are only listed separately on the Changes page, because Unity rewrites them on its own and they would otherwise appear as a change nobody made."),
                EditorStyles.label);
            EditorGUI.BeginChangeCheck();
            string regenerated = EditorGUILayout.TextArea(DocSnapSettings.RegeneratedPaths, GUILayout.MinHeight(48));
            if (EditorGUI.EndChangeCheck()) { DocSnapSettings.RegeneratedPaths = regenerated; }
            EditorGUILayout.LabelField(
                "Always included: " + string.Join(" · ", DocSnapRegeneratedPaths.Patterns().ToArray()),
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                "TextMesh Pro's font assets re-serialise their glyph atlas whenever a new character is rendered, so they change just from opening and closing the Editor.",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Output extras", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            bool aiBundle = EditorGUILayout.Toggle(
                new GUIContent("Write summary/ai-bundle.md", "Concatenates every summary this export produces into one Markdown file, so a whole project can be pasted into an AI assistant in a single go instead of a folder at a time."),
                DocSnapSettings.WriteAiBundle);
            if (EditorGUI.EndChangeCheck()) { DocSnapSettings.WriteAiBundle = aiBundle; }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);

            // Which skin the site OPENS with. "Auto" measures the
            // machine (RAM / cores / GPU) and how heavy the project is:
            // the cozy skin is the nicer thing to look at and strictly
            // more paint work per row, so on a big project or a tight
            // machine it opens light instead. A reader can always
            // switch inside the site; this is only the starting point.
            string[] skinLabels = { "Auto (measure this machine + project)", "Cozy — gradients, shadows, animation", "Lite — flat and fast" };
            string[] skinValues = { "auto", DocSnapCapability.SkinCozy, DocSnapCapability.SkinLite };
            int skinIndex = System.Array.IndexOf(skinValues, DocSnapSettings.SiteSkin);
            if (skinIndex < 0) { skinIndex = 0; }
            EditorGUI.BeginChangeCheck();
            int pickedSkin = EditorGUILayout.Popup(
                new GUIContent("Site skin", "Auto picks the cozy skin when there is room for it and the lite skin when there is not. Readers can switch either way inside the site."),
                skinIndex, skinLabels);
            if (EditorGUI.EndChangeCheck()) { DocSnapSettings.SiteSkin = skinValues[pickedSkin]; }

            if (skinValues[skinIndex] == "auto")
            {
                DocSnapCapabilityReport probe = DocSnapCapability.Measure(0, 0);
                EditorGUILayout.LabelField(" ",
                    "This machine: " + probe.SystemMemoryMb + " MB RAM · " + probe.ProcessorCount + " cores · "
                        + (string.IsNullOrEmpty(probe.GraphicsDeviceName) ? "unknown GPU" : probe.GraphicsDeviceName),
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(6);
            EditorGUI.BeginChangeCheck();
            bool embedFonts = EditorGUILayout.Toggle(
                new GUIContent("Embed web fonts",
                    "On by default: each version folder carries its own ~570 KB of branded fonts, so an export is a self-contained thing you can zip and send. Turn this off to save that per export; the site falls back to the system font stack and the layout is unchanged."),
                DocSnapSettings.EmbedFonts);
            if (EditorGUI.EndChangeCheck()) { DocSnapSettings.EmbedFonts = embedFonts; }

            EditorGUILayout.Space(6);
            EditorGUI.BeginChangeCheck();
            bool thumbs = EditorGUILayout.Toggle(
                new GUIContent("Generate Image Thumbnails", "On by default so image assets get real preview thumbnails. Turn this off if you need DocSnap's stricter mode where pixels never leave your project."),
                DocSnapSettings.GenerateThumbnails);
            if (EditorGUI.EndChangeCheck()) { DocSnapSettings.GenerateThumbnails = thumbs; }

            // Thumbnails write real pixel data, and the difference
            // matters enough to say out loud rather than leave in a
            // tooltip: a reader who believes the export contains no
            // imagery may hand it to someone outside the team.
            if (thumbs)
            {
                EditorGUILayout.HelpBox(
                    "Thumbnails are ON: downscaled PNG previews of your image assets are written into theme/thumbs/, "
                    + "so the export contains actual pixels. Turn this off if the export must carry metadata only.",
                    MessageType.Info);
            }

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

            // Where these live, said plainly. Everything above
            // except the logo path describes the PROJECT, so it is
            // written to a file meant to be committed - and a team
            // that does not know the file exists cannot commit it.
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Where these are stored", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                DocSnapSettings.ProjectSettingsRelativePath + "  —  commit this file to share it with your team and your CI.",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                "The Custom Logo Path is an absolute path on this machine, so it stays per-user and is not written there.",
                EditorStyles.miniLabel);

            string storeError = DocSnapSettings.Store.LastError;
            if (!string.IsNullOrEmpty(storeError))
            {
                EditorGUILayout.HelpBox(
                    DocSnapSettings.ProjectSettingsRelativePath + " " + storeError,
                    MessageType.Warning);
            }
        }
    }
}
