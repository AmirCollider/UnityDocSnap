// ==========================================
// DocSnapProjectBackup
// Exports the whole project's Assets/ as a single
// .unitypackage placed inside the version folder.
// This is the "even if the project is deleted, you
// can bring it back" safety net: importing the
// package into an empty Unity project restores every
// asset, with dependencies and folder structure
// intact. Best-effort - a failure is logged and
// surfaced but never aborts the documentation export.
// ==========================================
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor.Export
{
    internal static class DocSnapProjectBackup
    {
        // ==========================================
        // ExportProjectPackage
        // Writes <versionFolder>/project-backup.unitypackage
        // covering all of Assets/ with dependencies and full
        // recursion. Returns true on success.
        // ==========================================
        public static bool ExportProjectPackage(string versionFolder, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                Directory.CreateDirectory(versionFolder);
                string packagePath = Path.Combine(versionFolder, DocSnapConstants.BackupFileName);

                EditorUtility.DisplayProgressBar(
                    DocSnapConstants.ToolName,
                    "Building project backup (.unitypackage)…",
                    0.5f);

                // Recurse walks every sub-folder of Assets; IncludeDependencies
                // pulls in anything referenced from outside the selection. The
                // whole Assets/ tree is the selection, so together they capture
                // the entire project.
                AssetDatabase.ExportPackage(
                    new[] { "Assets" },
                    packagePath,
                    ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

                return File.Exists(packagePath);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Debug.LogWarning("[Unity DocSnap] Project backup failed: " + ex.Message);
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
