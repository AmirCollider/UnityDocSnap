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
        //   -docsnapLanguage <code>        language the site opens in
        //                                  (any code in DocSnapLanguages)
        //   -docsnapTheme light|dark       theme the site opens in
        //   -docsnapSkin auto|cozy|lite    skin the site opens in
        //   -docsnapNoThumbnails           metadata only, no pixel previews
        //   -docsnapNoFonts                skip the embedded web fonts
        //   -docsnapSaveSettings           also WRITE the settings above to
        //                                  ProjectSettings/UnityDocSnapSettings.json
        //
        // With none of the action arguments it runs a full
        // project export, which is the thing a first-time
        // user almost always means.
        //
        // Settings passed here apply to this run only and are
        // NOT written to the committed settings file unless
        // -docsnapSaveSettings says so. They used to be written
        // every time, which left the working tree dirty on every
        // CI run - failing the `git diff --exit-code` step a lot
        // of pipelines end with, and on a job that commits its
        // own output, quietly rewriting the project's
        // configuration to whatever one build happened to pass.
        // A build agent asking for a different output folder for
        // one run is not the same act as a person changing what
        // the project documents.
        // ==========================================
        public static void RunFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            bool badArguments = false;

            // Settings first: an export reads them as it runs, so
            // they have to be in place before anything starts.
            bool persist = Flag(args, "-docsnapSaveSettings");

            string output = Value(args, "-docsnapOutput", ref badArguments);
            if (output != null)
            {
                // Validated here rather than left to the export, so a
                // pipeline that points its documentation at Assets/ or
                // Library/ is told which argument is wrong and stops -
                // instead of opening every Scene first and only then
                // refusing, or (before this rule existed) succeeding and
                // leaving the project with thousands of imported files
                // in it.
                DocSnapOutputPathVerdict verdict = DocSnapSettings.ValidateOutputRoot(output);
                if (verdict != DocSnapOutputPathVerdict.Ok)
                {
                    Debug.LogError("[" + DocSnapConstants.ToolName + "] -docsnapOutput \"" + output + "\": "
                        + DocSnapOutputPathMessages.Describe(verdict, DocSnapLanguages.Fallback));
                    badArguments = true;
                }
                else
                {
                    ApplySetting("outputPath", output, persist, v => DocSnapSettings.OutputRootPath = v);
                }
            }

            string excludes = Value(args, "-docsnapExclude", ref badArguments);
            if (excludes != null) { ApplySetting("excludePatterns", excludes.Replace(";", "\n"), persist, v => DocSnapSettings.ExcludePatterns = v); }

            string language = Value(args, "-docsnapLanguage", ref badArguments);
            if (language != null) { ApplySetting("defaultSiteLanguage", language, persist, v => DocSnapSettings.DefaultSiteLanguage = v); }

            string theme = Value(args, "-docsnapTheme", ref badArguments);
            if (theme != null) { ApplySetting("defaultSiteTheme", theme, persist, v => DocSnapSettings.DefaultSiteTheme = v); }

            string skin = Value(args, "-docsnapSkin", ref badArguments);
            if (skin != null) { ApplySetting("siteSkin", skin, persist, v => DocSnapSettings.SiteSkin = v); }

            if (Flag(args, "-docsnapNoThumbnails")) { ApplySetting("generateThumbnails", "0", persist, v => DocSnapSettings.GenerateThumbnails = false); }
            if (Flag(args, "-docsnapNoFonts")) { ApplySetting("embedFonts", "0", persist, v => DocSnapSettings.EmbedFonts = false); }

            bool withFiles = Flag(args, "-docsnapWithFiles");
            List<string> scenes = Values(args, "-docsnapScene", ref badArguments);
            List<string> folders = Values(args, "-docsnapFolder", ref badArguments);

            // An argument that was meant to configure the run and did
            // not is a failure, not a detail. Silently falling back to
            // the default output folder is how a CI job writes its
            // documentation somewhere nobody looks and still reports
            // success.
            if (badArguments)
            {
                Debug.LogError("[" + DocSnapConstants.ToolName + "] Refusing to export: one or more arguments were malformed (see the warnings above).");
                DocSnapSettings.ClearSessionOverrides();
                if (Application.isBatchMode) { EditorApplication.Exit(1); }
                return;
            }

            var results = new List<DocSnapResult>();

            try
            {
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
            }
            finally
            {
                // Whatever happened, this session must not carry the
                // run's overrides into whatever runs next in the same
                // Editor.
                DocSnapSettings.ClearSessionOverrides();
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
        // ApplySetting
        // One command-line setting, applied either to this
        // session only (the default) or through the normal
        // setter, which writes the committed file.
        // ==========================================
        private static void ApplySetting(string key, string value, bool persist, Action<string> persistentSetter)
        {
            if (persist) { persistentSetter(value); }
            else { DocSnapSettings.SetSessionOverride(key, value); }
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
            // Automation is a Pro feature, and this is the one
            // gate in the tool that refuses rather than clamps.
            //
            // Everywhere else the rule is "run the export, leave
            // out the Pro parts" - because a person clicked a menu
            // item and wants documentation. Here nobody clicked
            // anything: this is a build agent, and a build agent
            // that half-succeeds is worse than one that fails. It
            // publishes a docs folder missing the exact outputs
            // the pipeline was built to produce, reports success,
            // and nobody looks again for six months.
            //
            // So a Free Editor gets a failure with the reason on
            // it, and -batchmode exits non-zero. The Editor's own
            // menu items are untouched and still export everything
            // Free includes.
            if (!Licensing.DocSnapLicense.Has(Licensing.DocSnapFeature.Automation))
            {
                return Failure(
                    "Scripted and command-line exports are a " + DocSnapConstants.ToolName
                    + " Pro feature. The Editor menu still exports everything the free edition includes.\n"
                    + "Unlock CI automation: " + DocSnapConstants.ProductUrl);
            }

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
        //
        // A value that starts with '-' is treated as the next
        // argument rather than as this one's value, so a typo'd
        // "-docsnapOutput -docsnapUpdate" cannot create a folder
        // literally called "-docsnapUpdate". This used to be
        // described as failing visibly, and it did not: the
        // helper returned null, the caller's `if (value != null)`
        // skipped the setting without a word, and the export ran
        // to the DEFAULT output folder reporting success. A
        // pipeline pointing its docs at Build/Docs would publish
        // nothing from there and go green.
        //
        // Now every such case says which argument was wrong, on
        // the console where a CI log will carry it, and sets the
        // caller's flag so the run is refused outright.
        // ==========================================
        // internal rather than private so the tests can reach them.
        // The rule they encode - that a missing value is refused
        // rather than reinterpreted - is one whose failure mode is
        // a green build that published nothing, so it needs holding
        // in place.
        internal static bool Flag(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) { return true; }
            }
            return false;
        }

        internal static string Value(string[] args, string name, ref bool malformed)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) { continue; }

                if (i + 1 >= args.Length)
                {
                    ReportMissingValue(name, "it is the last argument on the command line");
                    malformed = true;
                    return null;
                }

                string next = args[i + 1];
                if (string.IsNullOrEmpty(next))
                {
                    ReportMissingValue(name, "the value that follows it is empty");
                    malformed = true;
                    return null;
                }
                if (next.StartsWith("-", StringComparison.Ordinal))
                {
                    ReportMissingValue(name, "it is followed by \"" + next + "\", which is another argument rather than a value");
                    malformed = true;
                    return null;
                }
                return next;
            }
            return null;
        }

        internal static List<string> Values(string[] args, string name, ref bool malformed)
        {
            var found = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) { continue; }

                if (i + 1 >= args.Length)
                {
                    ReportMissingValue(name, "it is the last argument on the command line");
                    malformed = true;
                    continue;
                }

                string next = args[i + 1];
                if (string.IsNullOrEmpty(next) || next.StartsWith("-", StringComparison.Ordinal))
                {
                    ReportMissingValue(name, "it is not followed by a value");
                    malformed = true;
                    continue;
                }
                found.Add(next);
            }
            return found;
        }

        private static void ReportMissingValue(string name, string why)
        {
            Debug.LogWarning("[" + DocSnapConstants.ToolName + "] \"" + name
                + "\" needs a value, but " + why + ".");
        }
    }
}
