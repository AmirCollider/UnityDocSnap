// ==========================================
// DocSnapSiteFiles
// Reads the generated site's own static assets
// (stylesheet, script, embedded fonts, logo mark)
// from real files shipped beside this code, in
// Editor/UnityDocSnap/Site~/.
//
// They used to live inside C# verbatim strings.
// That worked, and cost more than it looked like:
//   • ~680 KB of the assembly's source - about half
//     of the whole codebase - was CSS, JS and base64
//     font bytes that no C# tool could see into. No
//     linting, no syntax highlighting, no formatter,
//     and every literal double quote written twice.
//   • DocSnapSiteAssets carried a comment reading
//     "the readable sources they were authored from
//     are plain style.css / app.js; keep the two in
//     step" - a manual synchronisation obligation
//     against files that were NOT in the repository.
//     There was no way to check it had been honoured,
//     and no way for a contributor to even find the
//     other half.
// The files are now the source of truth and are read
// at export time, so what ships is what was authored.
//
// The folder is named with a trailing '~' on purpose.
// Unity ignores such folders completely: no import, no
// .meta files, no guessing at what importer a .css or
// a .js should get - while UPM still ships them inside
// the package exactly like the standard Documentation~
// and Samples~ folders. So they are on disk to be read
// and invisible to the Editor otherwise.
// ==========================================
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapSiteFiles
    {
        public const string FontsCssFile = "fonts.css";
        public const string StyleCssFile = "style.css";
        public const string AppJsFile = "app.js";
        public const string LogoSvgFile = "logo.svg";

        private const string SiteFolderName = "Site~";

        // Read once per domain reload. Unity reloads the domain on
        // any script change, so an edited stylesheet is picked up
        // on the next compile without a restart, and a full-project
        // export does not re-read the same 570 KB of font CSS for
        // every version folder it writes.
        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>(StringComparer.Ordinal);
        private static string _siteFolder;
        private static bool _siteFolderResolved;
        private static bool _reportedMissingFolder;

        // ==========================================
        // Read
        // One site file's text, or "" when it cannot be
        // found. A miss is reported ONCE per file: the
        // export still finishes (an unstyled site is
        // readable, and losing the whole export over a
        // missing stylesheet would be worse), but it can
        // never fail silently - a site that quietly lost
        // its stylesheet looks like the tool is broken,
        // with nothing on screen to say why.
        // ==========================================
        public static string Read(string fileName)
        {
            string cached;
            if (Cache.TryGetValue(fileName, out cached)) { return cached; }

            string text = "";
            string folder = SiteFolderAbsolute();
            if (!string.IsNullOrEmpty(folder))
            {
                string full = Path.Combine(folder, fileName);
                try
                {
                    if (File.Exists(full)) { text = File.ReadAllText(full); }
                    else { ReportMissingFile(full); }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[Unity DocSnap] Could not read site asset \"" + fileName + "\": " + ex.Message);
                }
            }

            Cache[fileName] = text;
            return text;
        }

        // ==========================================
        // IsAvailable
        // Whether the site assets were found at all. The
        // export uses this to warn the user up front rather
        // than after they have waited out a full export.
        // ==========================================
        public static bool IsAvailable
        {
            get
            {
                string folder = SiteFolderAbsolute();
                return !string.IsNullOrEmpty(folder) && File.Exists(Path.Combine(folder, StyleCssFile));
            }
        }

        // ==========================================
        // SiteFolderAbsolute
        // Locates Editor/UnityDocSnap/Site~ for both
        // supported install shapes, because they resolve
        // to genuinely different places on disk:
        //
        //   1. Installed as a UPM package (the git URL in
        //      the README, a registry, or a local file:
        //      reference). Application.dataPath is useless
        //      here - the package lives outside Assets/,
        //      often in the global package cache - so the
        //      Package Manager is asked where it put this
        //      assembly.
        //   2. Copied into Assets/ by hand (README option
        //      B). No PackageInfo exists, so this code's
        //      own .cs file is located through the
        //      AssetDatabase and the folder is taken from
        //      beside it - which stays correct no matter
        //      where inside Assets/ the folder was dropped
        //      or what it was renamed to.
        // ==========================================
        public static string SiteFolderAbsolute()
        {
            if (_siteFolderResolved) { return _siteFolder; }
            _siteFolderResolved = true;
            _siteFolder = ResolveFromPackage() ?? ResolveFromAssetDatabase();

            if (_siteFolder == null && !_reportedMissingFolder)
            {
                _reportedMissingFolder = true;
                Debug.LogError("[Unity DocSnap] Could not locate the \"" + SiteFolderName
                    + "\" folder that holds the generated site's stylesheet, script and fonts. "
                    + "It ships beside the tool's scripts at Editor/UnityDocSnap/" + SiteFolderName
                    + " - if the package was copied in by hand, copy that folder too. "
                    + "Exports will still be written, but without styling.");
            }
            return _siteFolder;
        }

        private static string ResolveFromPackage()
        {
            try
            {
                // Fully qualified: UnityEngine also defines a PackageInfo,
                // so the short name is ambiguous in a file that uses both
                // namespaces.
                UnityEditor.PackageManager.PackageInfo package =
                    UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DocSnapSiteFiles).Assembly);
                if (package == null || string.IsNullOrEmpty(package.resolvedPath)) { return null; }

                string candidate = Path.Combine(package.resolvedPath,
                    Path.Combine("Editor", Path.Combine("UnityDocSnap", SiteFolderName)));
                return Directory.Exists(candidate) ? candidate : null;
            }
            catch
            {
                // Older Unity, or an assembly the Package Manager does
                // not know about: fall through to the AssetDatabase.
                return null;
            }
        }

        private static string ResolveFromAssetDatabase()
        {
            try
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                foreach (string guid in AssetDatabase.FindAssets("DocSnapSiteFiles t:MonoScript"))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(assetPath)) { continue; }
                    if (!assetPath.EndsWith("/DocSnapSiteFiles.cs", StringComparison.Ordinal)) { continue; }

                    string scriptAbsolute = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
                    string candidate = Path.Combine(Path.GetDirectoryName(scriptAbsolute), SiteFolderName);
                    if (Directory.Exists(candidate)) { return candidate; }
                }
            }
            catch { /* best-effort lookup only */ }
            return null;
        }

        private static void ReportMissingFile(string fullPath)
        {
            Debug.LogError("[Unity DocSnap] Site asset missing: \"" + fullPath
                + "\". The export will be written without it.");
        }
    }
}
