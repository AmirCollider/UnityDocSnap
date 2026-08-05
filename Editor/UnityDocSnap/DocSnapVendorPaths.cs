// ==========================================
// DocSnapVendorPaths
// Splits "things I made" from "things Unity and my
// packages dropped into Assets/".
//
// The health report was honest but useless without
// this: a project would report eight findings, seven
// of them in Assets/Settings (the render-pipeline
// assets a Unity template creates) and one in
// Assets/TextMesh Pro (TMP Essentials). None of them
// is the author's to fix, none can be deleted, and
// they sat at the top of the list on every export
// burying anything that actually was the author's
// problem. A count you are expected to ignore trains
// you to ignore the count.
//
// Two questions decide it, and one of them is not a
// path:
//
//   WHERE is the file?  A fixed install location a
//     package or template writes into is not the
//     author's folder. The built-in list covers what
//     Unity itself ships; the project can add its own
//     (an Asset Store vendor folder, a shared
//     submodule) in Project Settings.
//
//   WHAT is the file?   A URP Renderer2D.asset is a
//     Unity artefact wherever it is filed. Path alone
//     got this wrong for every project that keeps its
//     render-pipeline assets somewhere other than
//     Assets/Settings - which is most projects that
//     have any organisation at all. The report then
//     said "7 broken references in your files" and
//     listed seven fields of a URP data asset
//     (probeVolumeResources.probeVolumeDebugShader and
//     friends) that the author has never seen, cannot
//     open, and could not have broken: they are
//     Unity's own upgrade leftovers. That is the exact
//     failure this file was written to prevent, one
//     step further out.
// ==========================================
using System;
using System.Collections.Generic;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapVendorPaths
    {
        public const string OwnerMine = "mine";
        public const string OwnerVendor = "vendor";

        // ==========================================
        // Folders Unity's own templates, packages and
        // "import Essentials" buttons install into, anchored
        // at the root of Assets/. Every one of these is
        // content a project receives rather than writes, and
        // none of it is safe to hand-edit.
        //
        //   TextMesh Pro       TMP Essentials / Examples
        //   Settings           URP / HDRP pipeline assets from
        //                      a Unity template
        //   TutorialInfo       the "Learn" template scaffolding
        //   Samples            package sample imports
        //   XR, XRI            XR plug-in management + the
        //                      Interaction Toolkit's own assets
        //   Standard Assets    Unity's legacy bundle
        //   Plugins            the conventional third-party drop
        //   Editor Default
        //   Resources, Gizmos  Unity's magic Editor folders
        //
        // Root-anchored on purpose: "Settings", "Samples",
        // "Plugins" and "XR" are all names an author uses for
        // their own folders further down the tree, and
        // matching those anywhere would hide real findings.
        // ==========================================
        private static readonly string[] BuiltInVendorFolders =
        {
            "Assets/TextMesh Pro",
            "Assets/TextMeshPro",
            "Assets/Settings",
            "Assets/TutorialInfo",
            "Assets/Samples",
            "Assets/XR",
            "Assets/XRI",
            "Assets/Standard Assets",
            "Assets/Plugins",
            "Assets/Editor Default Resources",
            "Assets/Gizmos"
        };

        // ==========================================
        // Folder NAMES that mean the same thing wherever
        // they appear, because nobody names a folder of
        // their own after them.
        //
        // "Assets/_Game/ThirdParty/TextMesh Pro" is TMP
        // Essentials exactly as much as "Assets/TextMesh Pro"
        // is, and a project that tidied its imports into one
        // parent folder should not lose the classification
        // for having done so.
        // ==========================================
        private static readonly string[] BuiltInVendorFolderNames =
        {
            "TextMesh Pro",
            "TextMeshPro",
            "TutorialInfo",
            "Standard Assets",
            "Editor Default Resources"
        };

        // ==========================================
        // Asset TYPES that are Unity's own, wherever the
        // file happens to be filed.
        //
        // Matched on the full type name so a project's own
        // class called Renderer2DData is not swept up with
        // Unity's. A namespace prefix entry covers a whole
        // family: everything under UnityEngine.Rendering.
        // Universal is render-pipeline machinery, and none of
        // its serialized fields is authored by hand - they
        // are written by the template that created the
        // project and rewritten by Unity's own upgraders,
        // which is where the broken ones come from.
        // ==========================================
        private static readonly string[] VendorTypeNamespaces =
        {
            "UnityEngine.Rendering.Universal",
            "UnityEngine.Rendering.HighDefinition",
            "UnityEngine.Rendering.PostProcessing",
            "UnityEditor.Rendering",
            "UnityEditor.SceneTemplate",
            "UnityEditor.SceneManagement.SceneTemplate"
        };

        private static readonly string[] VendorTypeNames =
        {
            "UnityEngine.Rendering.RenderPipelineAsset",
            "UnityEngine.Rendering.RenderPipelineGlobalSettings",
            "UnityEngine.Rendering.VolumeProfile",
            "UnityEngine.LightingSettings",
            "UnityEditor.LightingSettings",
            "UnityEngine.LightingDataAsset",
            "UnityEngine.AI.NavMeshData",
            "UnityEngine.OcclusionCullingData",
            "UnityEditor.OcclusionCullingData",
            "TMPro.TMP_Settings",
            "UnityEngine.InputSystem.InputSettings"
        };

        // ==========================================
        // Classify
        // OwnerVendor when the project-relative path sits
        // under a vendor folder, OwnerMine otherwise.
        //
        // Anything outside Assets/ (a Packages/… path, or a
        // label that is not a path at all) counts as vendor:
        // an installed package is the definition of code
        // somebody else maintains.
        // ==========================================
        public static string Classify(string projectRelativePath)
        {
            return IsVendor(projectRelativePath) ? OwnerVendor : OwnerMine;
        }

        // ==========================================
        // Classify (path + type)
        // The same answer, with the asset's own main type
        // taken into account - so a file Unity generates is
        // Unity's wherever the project keeps it.
        //
        // assetTypeFullName is the namespace-qualified name
        // recorded by the exporter ("mainTypeFullName"). An
        // empty one simply falls back to the path rule, which
        // is what an export written by an older version of
        // this tool carries.
        // ==========================================
        public static string Classify(string projectRelativePath, string assetTypeFullName)
        {
            return IsVendor(projectRelativePath, assetTypeFullName) ? OwnerVendor : OwnerMine;
        }

        public static bool IsVendor(string projectRelativePath)
        {
            if (string.IsNullOrEmpty(projectRelativePath)) { return false; }
            string path = Normalize(projectRelativePath);

            if (path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase)) { return true; }

            foreach (string folder in VendorFolders())
            {
                if (IsUnderOrEqual(path, folder)) { return true; }
            }
            return HasVendorFolderName(path);
        }

        public static bool IsVendor(string projectRelativePath, string assetTypeFullName)
        {
            return IsVendor(projectRelativePath) || IsVendorType(assetTypeFullName);
        }

        // ==========================================
        // IsVendorType
        // True for the configuration assets Unity's own
        // templates, importers and bakers write - the ones
        // whose serialized fields no Inspector the author
        // opens is responsible for.
        // ==========================================
        public static bool IsVendorType(string assetTypeFullName)
        {
            if (string.IsNullOrEmpty(assetTypeFullName)) { return false; }
            string type = assetTypeFullName.Trim();

            foreach (string name in VendorTypeNames)
            {
                if (string.Equals(type, name, StringComparison.Ordinal)) { return true; }
            }

            foreach (string ns in VendorTypeNamespaces)
            {
                if (type.Length > ns.Length
                    && type.StartsWith(ns, StringComparison.Ordinal)
                    && type[ns.Length] == '.')
                {
                    return true;
                }
            }
            return false;
        }

        // ==========================================
        // VendorFolders
        // The built-in list plus whatever the project added,
        // normalised and de-duplicated. Cached per raw
        // settings string: this is called once per finding
        // and the settings value only changes when someone
        // edits it.
        // ==========================================
        private static string _cachedRaw;
        private static List<string> _cachedFolders;
        private static List<string> _cachedFolderNames;

        public static List<string> VendorFolders()
        {
            Rebuild();
            return _cachedFolders;
        }

        // ==========================================
        // VendorFolderNames
        // Entries that match on NAME rather than on full
        // path: the built-in unmistakable ones, plus any the
        // project typed without a leading "Assets/".
        //
        // Reading a bare "MyVendor" as a name rather than as
        // a path is the useful interpretation of it: as a
        // path it would mean a top-level folder called
        // MyVendor sitting beside Assets/, which is not a
        // place Unity has ever put anything.
        // ==========================================
        public static List<string> VendorFolderNames()
        {
            Rebuild();
            return _cachedFolderNames;
        }

        private static void Rebuild()
        {
            string raw = DocSnapSettings.VendorFolders ?? "";
            if (_cachedFolders != null && _cachedRaw == raw) { return; }

            var folders = new List<string>();
            var names = new List<string>();
            var seenFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string builtIn in BuiltInVendorFolders)
            {
                if (seenFolders.Add(builtIn)) { folders.Add(builtIn); }
            }
            foreach (string builtIn in BuiltInVendorFolderNames)
            {
                if (seenNames.Add(builtIn)) { names.Add(builtIn); }
            }

            foreach (string piece in raw.Split(new[] { '\n', '\r', ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string entry = Normalize(piece);
                if (entry.Length == 0 || entry.StartsWith("#", StringComparison.Ordinal)) { continue; }

                if (entry.IndexOf('/') < 0)
                {
                    if (seenNames.Add(entry)) { names.Add(entry); }
                    continue;
                }

                if (seenFolders.Add(entry)) { folders.Add(entry); }
            }

            _cachedRaw = raw;
            _cachedFolders = folders;
            _cachedFolderNames = names;
        }

        private static bool HasVendorFolderName(string path)
        {
            List<string> names = VendorFolderNames();
            if (names.Count == 0) { return false; }

            // The file's own name is not a folder, so the last segment
            // never counts - "Assets/Art/TextMesh Pro.png" is a picture.
            int lastSlash = path.LastIndexOf('/');
            if (lastSlash <= 0) { return false; }

            string folders = path.Substring(0, lastSlash);
            foreach (string segment in folders.Split('/'))
            {
                for (int i = 0; i < names.Count; i++)
                {
                    if (string.Equals(segment, names[i], StringComparison.OrdinalIgnoreCase)) { return true; }
                }
            }
            return false;
        }

        // ==========================================
        // Describe
        // Which vendor folder a path fell under, for the
        // "why is this not my file?" question a reader will
        // reasonably ask. "" when the path is the project's
        // own.
        // ==========================================
        public static string Describe(string projectRelativePath)
        {
            if (string.IsNullOrEmpty(projectRelativePath)) { return ""; }
            string path = Normalize(projectRelativePath);
            if (path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase)) { return "Packages/"; }
            foreach (string folder in VendorFolders())
            {
                if (IsUnderOrEqual(path, folder)) { return folder; }
            }

            int lastSlash = path.LastIndexOf('/');
            if (lastSlash > 0)
            {
                List<string> names = VendorFolderNames();
                foreach (string segment in path.Substring(0, lastSlash).Split('/'))
                {
                    for (int i = 0; i < names.Count; i++)
                    {
                        if (string.Equals(segment, names[i], StringComparison.OrdinalIgnoreCase)) { return segment + "/"; }
                    }
                }
            }
            return "";
        }

        // ==========================================
        // Describe (path + type)
        // The same note, falling back to the type when the
        // path itself is unremarkable: "Renderer2DData" says
        // considerably more about why a finding is not the
        // author's than a folder name would have.
        // ==========================================
        public static string Describe(string projectRelativePath, string assetTypeFullName)
        {
            string byPath = Describe(projectRelativePath);
            if (!string.IsNullOrEmpty(byPath)) { return byPath; }
            return IsVendorType(assetTypeFullName) ? ShortTypeName(assetTypeFullName) : "";
        }

        private static string ShortTypeName(string typeFullName)
        {
            if (string.IsNullOrEmpty(typeFullName)) { return ""; }
            int dot = typeFullName.LastIndexOf('.');
            return dot >= 0 && dot + 1 < typeFullName.Length ? typeFullName.Substring(dot + 1) : typeFullName;
        }

        private static bool IsUnderOrEqual(string path, string folder)
        {
            if (path.Equals(folder, StringComparison.OrdinalIgnoreCase)) { return true; }
            return path.Length > folder.Length
                && path.StartsWith(folder, StringComparison.OrdinalIgnoreCase)
                && path[folder.Length] == '/';
        }

        private static string Normalize(string value)
        {
            return value == null ? "" : value.Replace('\\', '/').Trim().TrimEnd('/');
        }

        // Test seam: the folder list is cached against the raw
        // settings string, and a test that changes the setting
        // needs the cache to notice.
        public static void InvalidateCache()
        {
            _cachedRaw = null;
            _cachedFolders = null;
            _cachedFolderNames = null;
        }
    }
}
