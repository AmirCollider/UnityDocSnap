// ==========================================
// DocSnapAPI
// The tool's only public surface: a scripted or
// automated way to run an export.
//
// Everything else in Unity DocSnap is `internal` on
// purpose - it is an editor tool, not a library, and
// a public type is a promise about a shape that
// cannot then change freely. But "no public API"
// also meant no way to run an export except by
// clicking a menu item, and the menu handlers are
// private, so `-executeMethod` had nothing to call.
// A team that wants its documentation regenerated on
// every merge could not have it; the one job a
// documentation tool most obviously belongs in is
// the one place it could not go.
//
// So this file, and only this file, is public: four
// entry points, a result to check, and a command
// line to drive them. It is deliberately small -
// small enough to keep working - and it holds no
// logic of its own beyond argument parsing. The
// exports it runs are the same ones the menu runs.
//
// From C#:
//
//     var result = DocSnapAPI.ExportFullProject();
//     if (!result.Succeeded) { Debug.LogError(result.Message); }
//
// From a command line / CI:
//
//     Unity -batchmode -quit -projectPath . \
//           -executeMethod AmirCollider.UnityDocSnap.Editor.DocSnapAPI.RunFromCommandLine \
//           -docsnapOutput Build/Docs -docsnapExclude "Assets/Plugins;Assets/ThirdParty"
//
// In -batchmode the process exits non-zero when the
// export fails, so a red build means a real problem
// rather than a line nobody read in a log.
// ==========================================
using System;
using System.Collections.Generic;
using AmirCollider.UnityDocSnap.Editor.Export;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    // ==========================================
    // DocSnapResult
    // What an export did, in a form a script can act
    // on. Cancelled is separate from failed: the user
    // stopped it, everything already written is intact,
    // and a build should usually treat that differently
    // from a crash.
    // ==========================================
    public struct DocSnapResult
    {
        public bool Succeeded;
        public bool Cancelled;
        public string Message;
        public string OutputPath;

        public override string ToString()
        {
            string state = Succeeded ? "OK" : (Cancelled ? "CANCELLED" : "FAILED");
            return state + ": " + Message;
        }
    }

    public static class DocSnapAPI
    {
        // ==========================================
        // Version — the installed package version, so a
        // build log can record which one produced its docs.
        // ==========================================
        public static string Version
        {
            get { return DocSnapConstants.Version; }
        }

        // ==========================================
        // OutputRoot — the absolute folder exports land in,
        // honouring the configured output path.
        // ==========================================
        public static string OutputRoot
        {
            get { return DocSnapSettings.ResolveOutputRootAbsolute(); }
        }

        // ==========================================
        // ExportFullProject
        // Every Scene plus the whole Assets tree, into a
        // new version folder.
        //
        // includeFiles copies the real asset bytes into
        // source-files/ as well. It is off by default here
        // for the same reason it is an explicit menu item:
        // it turns a metadata export into one that carries
        // the project's actual content.
        // ==========================================
        public static DocSnapResult ExportFullProject(bool includeFiles = false)
        {
            return Run(() =>
            {
                if (includeFiles) { DocSnapExportService.ExportFullProjectWithFiles(); }
                else { DocSnapExportService.ExportFullProject(); }
            });
        }

        // ==========================================
        // ExportScene
        // One Scene, by its project-relative path
        // ("Assets/Scenes/Main.unity"), into the current
        // version folder.
        // ==========================================
        public static DocSnapResult ExportScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                return Failure("No scene path was given.");
            }
            return Run(() => DocSnapExportService.ExportScene(scenePath));
        }

        // ==========================================
        // ExportAssetFolder
        // One folder under Assets/, recursively, by its
        // project-relative path ("Assets/Art/Textures").
        // ==========================================
        public static DocSnapResult ExportAssetFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return Failure("No folder path was given.");
            }
            return Run(() => DocSnapExportService.ExportFolder(folderPath));
        }

        // ==========================================
        // UpdatePreviousExport
        // Refreshes the newest existing version folder in
        // place, reusing whatever has not changed. This is
        // the one to run on a schedule: it is incremental,
        // so it costs a fraction of a full export and does
        // not add a version folder every time.
        // ==========================================
        public static DocSnapResult UpdatePreviousExport()
        {
            return Run(DocSnapExportService.UpdatePreviousExport);
        }

        // ==========================================
        // RunFromCommandLine
        // The -executeMethod target. Reads its
        // configuration from the command line, runs one
        // export, and in batch mode exits with a code the
        // shell can branch on.
        //
        // Recognised arguments:
        //
        //   -docsnapUpdate                 refresh the newest version in place
        //   -docsnapScene <path>           export one Scene (repeatable)
        //   -docsnapFolder <path>          export one Assets folder (repeatable)
        //   -docsnapWithFiles              also copy asset bytes
        //   -docsnapOutput <path>          output root (absolute or project-relative)
        //   -docsnapExclude "a;b"          exclude patterns, ';' separated
        //   -docsnapLanguage en|ja|fa      language the site opens in
        //   -docsnapTheme light|dark       theme the site opens in
        //   -docsnapSkin auto|cozy|lite    skin the site opens in
        //   -docsnapNoThumbnails           metadata only, no pixel previews
        //   -docsnapNoFonts                skip the embedded web fonts
        //
        // With none of the action arguments it runs a full
        // project export, which is the thing a first-time
        // user almost always means.
        // ==========================================
        public static void RunFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();

            // Settings first: an export reads them as it runs, so
            // they have to be in place before anything starts.
            // These write to the committed project settings file
            // exactly as the Project Settings window does - a CI
            // job that passes -docsnapExclude is changing the
            // project's configuration, and it should be visible
            // that it did.
            string output = Value(args, "-docsnapOutput");
            if (output != null) { DocSnapSettings.OutputRootPath = output; }

            string excludes = Value(args, "-docsnapExclude");
            if (excludes != null) { DocSnapSettings.ExcludePatterns = excludes.Replace(";", "\n"); }

            string language = Value(args, "-docsnapLanguage");
            if (language != null) { DocSnapSettings.DefaultSiteLanguage = language; }

            string theme = Value(args, "-docsnapTheme");
            if (theme != null) { DocSnapSettings.DefaultSiteTheme = theme; }

            string skin = Value(args, "-docsnapSkin");
            if (skin != null) { DocSnapSettings.SiteSkin = skin; }

            if (Flag(args, "-docsnapNoThumbnails")) { DocSnapSettings.GenerateThumbnails = false; }
            if (Flag(args, "-docsnapNoFonts")) { DocSnapSettings.EmbedFonts = false; }

            bool withFiles = Flag(args, "-docsnapWithFiles");
            List<string> scenes = Values(args, "-docsnapScene");
            List<string> folders = Values(args, "-docsnapFolder");

            var results = new List<DocSnapResult>();

            if (Flag(args, "-docsnapUpdate"))
            {
                results.Add(UpdatePreviousExport());
            }
            else if (scenes.Count > 0 || folders.Count > 0)
            {
                foreach (string scene in scenes) { results.Add(ExportScene(scene)); }
                foreach (string folder in folders) { results.Add(ExportAssetFolder(folder)); }
            }
            else
            {
                results.Add(ExportFullProject(withFiles));
            }

            bool allSucceeded = true;
            foreach (DocSnapResult result in results)
            {
                if (result.Succeeded)
                {
                    Debug.Log("[" + DocSnapConstants.ToolName + "] " + result.Message
                        + (string.IsNullOrEmpty(result.OutputPath) ? "" : "  ->  " + result.OutputPath));
                }
                else
                {
                    allSucceeded = false;
                    Debug.LogError("[" + DocSnapConstants.ToolName + "] " + result.ToString());
                }
            }

            // Only in batch mode. Calling Exit in a windowed Editor
            // would close the user's Editor out from under them,
            // which is a spectacular thing for a documentation tool
            // to do to somebody experimenting in the console.
            if (Application.isBatchMode && !allSucceeded)
            {
                EditorApplication.Exit(1);
            }
        }

        // ==========================================
        // Run
        // Shared shell: silence the dialogs, run the
        // export, read back what it did.
        //
        // The try/finally matters more than it looks. An
        // export that throws must still restore
        // interactivity, or every later dialog in the
        // session - including ones from unrelated code -
        // would be silently swallowed for the rest of the
        // Editor's life.
        // ==========================================
        private static DocSnapResult Run(Action action)
        {
            DocSnapInteraction.BeginSilent();
            try
            {
                action();
            }
            catch (Exception ex)
            {
                // An exception escaping the service is a bug, not a
                // user error, so the stack trace is worth keeping in
                // the log even though the message is returned too.
                Debug.LogException(ex);
                return Failure(ex.Message);
            }
            finally
            {
                DocSnapInteraction.EndSilent();
                DocSnapLogGuard.ForceRestore();
            }

            return new DocSnapResult
            {
                Succeeded = DocSnapRunResult.Succeeded,
                Cancelled = DocSnapRunResult.Cancelled,
                Message = DocSnapRunResult.Message,
                OutputPath = DocSnapRunResult.OutputRoot
            };
        }

        private static DocSnapResult Failure(string message)
        {
            return new DocSnapResult { Succeeded = false, Cancelled = false, Message = message, OutputPath = "" };
        }

        // ==========================================
        // Argument helpers
        // Unity hands the whole process command line back,
        // including its own arguments, so these look only
        // for the names above and ignore everything else.
        // A value that starts with '-' is treated as the
        // next argument rather than as this one's value,
        // so a typo'd "-docsnapOutput -docsnapUpdate"
        // fails visibly instead of creating a folder
        // literally called "-docsnapUpdate".
        // ==========================================
        private static bool Flag(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) { return true; }
            }
            return false;
        }

        private static string Value(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) { continue; }
                string next = args[i + 1];
                if (string.IsNullOrEmpty(next) || next.StartsWith("-", StringComparison.Ordinal)) { return null; }
                return next;
            }
            return null;
        }

        private static List<string> Values(string[] args, string name)
        {
            var found = new List<string>();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) { continue; }
                string next = args[i + 1];
                if (string.IsNullOrEmpty(next) || next.StartsWith("-", StringComparison.Ordinal)) { continue; }
                found.Add(next);
            }
            return found;
        }
    }
}
