// ==========================================
// UnityDocSnapMenu
// Every menu entry the tool exposes: the
// "Unity DocSnap" top-level menu (matching
// README.md exactly) and the equivalent
// right-click actions in the Project window.
// ==========================================
using System;
using System.Collections.Generic;
using System.IO;
using AmirCollider.UnityDocSnap.Editor.Export;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class UnityDocSnapMenu
    {
        // ==========================================
        // Export Scene - dynamic dropdown of every
        // Scene currently in the project.
        // ==========================================
        [MenuItem(DocSnapConstants.MenuExportScene, false, 1)]
        private static void ShowExportSceneMenu()
        {
            var menu = new GenericMenu();
            List<string> scenePaths = DocSnapExportService.FindAllScenePaths();

            if (scenePaths.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No scenes found in this project"));
            }
            else
            {
                foreach (string scenePath in scenePaths)
                {
                    string captured = scenePath;
                    string label = Path.GetFileNameWithoutExtension(scenePath);
                    menu.AddItem(new GUIContent(label), false, () => DocSnapExportService.ExportScene(captured));
                }
            }
            menu.ShowAsContext();
        }

        // ==========================================
        // Export Asset Info - Entire Assets Folder
        // ==========================================
        [MenuItem(DocSnapConstants.MenuExportAssetInfoEntire, false, 12)]
        private static void ExportEntireAssetsFolder()
        {
            DocSnapExportService.ExportFolder("Assets");
        }

        // ==========================================
        // Export Asset Info - Selected Folder...
        // ==========================================
        [MenuItem(DocSnapConstants.MenuExportAssetInfoSelected, false, 13)]
        private static void ExportSelectedFolder()
        {
            string picked = EditorUtility.OpenFolderPanel("Select a folder inside Assets", Application.dataPath, "");
            if (string.IsNullOrEmpty(picked)) { return; }

            string relative = AbsoluteToAssetsRelative(picked);
            if (relative == null)
            {
                EditorUtility.DisplayDialog(DocSnapConstants.ToolName, "Please choose a folder inside this project's Assets folder.", "OK");
                return;
            }
            DocSnapExportService.ExportFolder(relative);
        }

        // ==========================================
        // Export Full Project
        // ==========================================
        [MenuItem(DocSnapConstants.MenuExportFullProject, false, 23)]
        private static void ExportFullProjectMenuItem()
        {
            DocSnapExportService.ExportFullProject();
        }

        // ==========================================
        // Export Full Project With Files
        // ==========================================
        [MenuItem(DocSnapConstants.MenuExportFullProjectWithFiles, false, 24)]
        private static void ExportFullProjectWithFilesMenuItem()
        {
            DocSnapExportService.ExportFullProjectWithFiles();
        }

        // ==========================================
        // Update Previous Export
        // Re-runs a full export but reuses any Scene or
        // the Assets folder whose source has not changed
        // since last time - a fast refresh for a large
        // project instead of re-scanning everything. A
        // confirmation dialog warns that it overwrites the
        // previous export in place and recommends a backup
        // first (the export folder is only ever rewritten,
        // never versioned).
        // ==========================================
        [MenuItem(DocSnapConstants.MenuUpdatePreviousExport, false, 25)]
        private static void UpdatePreviousExportMenuItem()
        {
            if (!DocSnapExportService.HasPreviousExport())
            {
                bool runFull = EditorUtility.DisplayDialog(
                    DocSnapConstants.ToolName,
                    "There's no previous export to update yet.\n\nRun a full export first?\n\nمنتظر بروزرسانی چیزی نیست — هنوز خروجی قبلی وجود نداره. اول یک خروجی کامل بگیر؟",
                    "Run Full Export",
                    "Cancel");
                if (runFull) { DocSnapExportService.ExportFullProject(); }
                return;
            }

            bool proceed = EditorUtility.DisplayDialog(
                DocSnapConstants.ToolName + "  ↻",
                "This updates your PREVIOUS export in place.\n" +
                "Only Scenes / Assets that changed since the last export are re-scanned; unchanged ones are reused, so it's much faster.\n\n" +
                "⚠ It overwrites the existing output folder. It's recommended to back up your previous export first.\n\n" +
                "─────\n" +
                "この操作は前回のエクスポートを上書きします。先にバックアップを推奨します。\n\n" +
                "─────\n" +
                "این کار خروجی قبلی رو همونجا بروزرسانی (و بازنویسی) می‌کنه. فقط موارد تغییر‌کرده دوباره اسکن می‌شن. بهتره قبلش بک‌آپ بگیری.",
                "Update & overwrite",
                "Cancel");

            if (proceed) { DocSnapExportService.UpdatePreviousExport(); }
        }

        // ==========================================
        // Open Output Folder
        // ==========================================
        [MenuItem(DocSnapConstants.MenuOpenOutputFolder, false, 34)]
        private static void OpenOutputFolderMenuItem()
        {
            string root = DocSnapSettings.ResolveOutputRootAbsolute();
            string indexPath = Path.Combine(root, DocSnapConstants.IndexFileName);
            EditorUtility.RevealInFinder(File.Exists(indexPath) ? indexPath : root);
        }

        // ==========================================
        // About Unity DocSnap
        // ==========================================
        [MenuItem(DocSnapConstants.MenuAbout, false, 45)]
        private static void ShowAboutMenuItem()
        {
            DocSnapAboutWindow.ShowWindow();
        }

        // ==========================================
        // Project window context menu - Export This Scene
        // ==========================================
        [MenuItem(DocSnapConstants.AssetsContextRoot + "Export This Scene", false, 1)]
        private static void ContextExportScene()
        {
            DocSnapExportService.ExportScene(AssetDatabase.GetAssetPath(Selection.activeObject));
        }

        [MenuItem(DocSnapConstants.AssetsContextRoot + "Export This Scene", true)]
        private static bool ValidateContextExportScene()
        {
            if (!(Selection.activeObject is SceneAsset)) { return false; }
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
        }

        // ==========================================
        // Project window context menu - Export This Folder
        // ==========================================
        [MenuItem(DocSnapConstants.AssetsContextRoot + "Export This Folder", false, 2)]
        private static void ContextExportFolder()
        {
            DocSnapExportService.ExportFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
        }

        [MenuItem(DocSnapConstants.AssetsContextRoot + "Export This Folder", true)]
        private static bool ValidateContextExportFolder()
        {
            if (Selection.activeObject == null) { return false; }
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path);
        }

        // ==========================================
        // Project window context menu - Export Containing
        // Folder Info (for a single selected file, not a
        // Scene or a folder).
        // ==========================================
        [MenuItem(DocSnapConstants.AssetsContextRoot + "Export Containing Folder Info", false, 3)]
        private static void ContextExportContainingFolder()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            string folder = Path.GetDirectoryName(path).Replace('\\', '/');
            DocSnapExportService.ExportFolder(folder);
        }

        [MenuItem(DocSnapConstants.AssetsContextRoot + "Export Containing Folder Info", true)]
        private static bool ValidateContextExportContainingFolder()
        {
            if (Selection.activeObject == null) { return false; }
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) { return false; }
            return !(Selection.activeObject is SceneAsset);
        }

        // ==========================================
        // Project window context menu - Export Full Project
        // ==========================================
        [MenuItem(DocSnapConstants.AssetsContextRoot + "Export Full Project", false, 14)]
        private static void ContextExportFullProject()
        {
            DocSnapExportService.ExportFullProject();
        }

        // ==========================================
        // Project window context menu - Open Output Folder
        // ==========================================
        [MenuItem(DocSnapConstants.AssetsContextRoot + "Open Output Folder", false, 15)]
        private static void ContextOpenOutputFolder()
        {
            OpenOutputFolderMenuItem();
        }

        // ==========================================
        // AbsoluteToAssetsRelative
        // Converts an absolute folder path (as chosen
        // via EditorUtility.OpenFolderPanel) back into
        // a project-relative "Assets/..." path, or null
        // if it falls outside this project's Assets.
        // ==========================================
        private static string AbsoluteToAssetsRelative(string absolutePath)
        {
            string dataPath = Application.dataPath.Replace('\\', '/');
            string normalized = absolutePath.Replace('\\', '/');

            if (normalized == dataPath) { return "Assets"; }
            if (normalized.StartsWith(dataPath + "/")) { return "Assets" + normalized.Substring(dataPath.Length); }
            return null;
        }
    }
}
