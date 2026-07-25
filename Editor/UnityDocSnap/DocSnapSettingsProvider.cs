// ==========================================
// DocSnapSettingsProvider
// Exposes DocSnapSettings under Edit > Project
// Settings > Unity DocSnap, using Unity's own
// SettingsProvider API rather than adding an
// extra item to the fixed "Unity DocSnap" menu
// tree documented in README.md.
// ==========================================
using System.Collections.Generic;
using AmirCollider.UnityDocSnap.Editor.Licensing;
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
        // DrawEditionBanner
        // Which edition this machine is running, at the top of
        // the settings that edition governs.
        //
        // It belongs here rather than only in the About window
        // because this page is where somebody configures things
        // whose behaviour depends on the answer - the AI bundle,
        // the custom logo - and "why did my setting do nothing?"
        // should be answerable without leaving the screen the
        // setting is on.
        // ==========================================
        private static void DrawEditionBanner()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();

            bool isPro = DocSnapLicense.IsPro;
            EditorGUILayout.LabelField(
                isPro ? "✨  Unity DocSnap Pro" : "🆓  Unity DocSnap Free",
                EditorStyles.boldLabel);

            if (GUILayout.Button(isPro ? "Licence" : "What's in Pro?", EditorStyles.miniButton, GUILayout.Width(110)))
            {
                DocSnapLicenseWindow.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();

            // A key that is stored but not working is the one state
            // worth interrupting for: the customer already paid, and
            // every other screen will silently behave as Free until
            // somebody notices.
            DocSnapLicenseStatus status = DocSnapLicense.Status();
            if (status.NeedsAttention)
            {
                string why = DocSnapLicense.DescribeVerdict(status.Verdict);
                if (!string.IsNullOrEmpty(why)) { EditorGUILayout.HelpBox(why, MessageType.Warning); }
            }
        }

        // ==========================================
        // DrawSettingsGui
        // ==========================================
        private static void DrawSettingsGui(string searchContext)
        {
            DrawEditionBanner();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string outputPath = EditorGUILayout.TextField(
                new GUIContent("Output Path", "Empty = default UnityDocSnap_Output next to Assets"),
                DocSnapSettings.OutputRootPath);
            if (EditorGUI.EndChangeCheck()) { DocSnapSettings.OutputRootPath = outputPath; }

            // The field still accepts anything - a text field that
            // fights the person typing into it is worse than one that
            // explains itself, and half of "Assets/Docs" is a prefix of
            // a perfectly good path. What it does not do is let the
            // value sit there looking accepted: the resolved
            // destination is shown, and a destination an export will
            // refuse says so here, with the reason, at the moment it is
            // typed rather than ten minutes into a full export.
            DocSnapOutputPathVerdict verdict = DocSnapSettings.ValidateOutputRoot(outputPath);
            string resolvedOutput = DocSnapSettings.ResolveOutputRootAbsoluteWithoutCreating();
            if (verdict == DocSnapOutputPathVerdict.Ok)
            {
                EditorGUILayout.LabelField("Exports to: " + resolvedOutput, EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    DocSnapOutputPathMessages.DescribeForEditor(verdict) + "\n\n" + resolvedOutput,
                    MessageType.Error);
                if (GUILayout.Button("Use the default (UnityDocSnap_Output)"))
                {
                    DocSnapSettings.OutputRootPath = "";
                    GUI.FocusControl(null);
                }
            }

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

            // The setting stays editable in the Free edition rather
            // than being greyed out, because it describes the
            // PROJECT and lives in the committed settings file: a
            // team where one person has Pro and another does not
            // must still be able to agree on it in a pull request,
            // and a field that disables itself per-machine would
            // make that file mean different things on different
            // desks. What changes is that the free Editor says
            // plainly that it will not act on it.
            if (!DocSnapLicense.Has(DocSnapFeature.AiSummaries))
            {
                EditorGUILayout.HelpBox(
                    "The summary/ folder and ai-bundle.md are Pro features, so exports from this machine skip them. "
                    + "The setting is still written to the shared settings file for teammates who have Pro.",
                    MessageType.None);
            }

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

            if (!string.IsNullOrEmpty(DocSnapSettings.CustomLogoAbsolutePath)
                && !DocSnapLicense.Has(DocSnapFeature.Whitelabel))
            {
                EditorGUILayout.HelpBox(
                    "Exports from this machine use the built-in mark: a custom logo is part of Pro, along with removing "
                    + "the \"free edition\" line from the exported site's footer.",
                    MessageType.None);
            }

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
