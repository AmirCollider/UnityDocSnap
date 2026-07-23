// ==========================================
// DocSnapPackagesReader
// Gathers the packages the project depends on
// from Unity's Package Manager and splits them
// into the two groups a reader actually cares
// about:
//   • third-party — installed from a Git URL, a
//     local/embedded folder, a tarball, or a
//     non-Unity registry (this is where an
//     Asset Store / GitHub package shows up);
//   • Unity — Unity's own registry packages
//     (com.unity.*, e.g. "2D Animation") plus
//     the always-present built-in engine modules.
// For each package it records an access link and
// whether Unity reports a newer version available.
// Everything is best-effort: the Package Manager
// API surface shifts between Unity versions, so
// every optional read is guarded and a total
// failure simply yields an empty list rather than
// breaking an export.
// ==========================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AmirCollider.UnityDocSnap.Editor.Packages
{
    internal static class DocSnapPackagesReader
    {
        // ==========================================
        // ReadInstalledPackages
        // The public entry point: one ManifestPackageEntry
        // per package, sorted third-party → Unity →
        // built-in module, alphabetically within each.
        // ==========================================
        public static List<AmirCollider.UnityDocSnap.Editor.Manifest.ManifestPackageEntry> ReadInstalledPackages()
        {
            var result = new List<AmirCollider.UnityDocSnap.Editor.Manifest.ManifestPackageEntry>();

            PackageInfo[] packages = null;
            try { packages = PackageInfo.GetAllRegisteredPackages(); }
            catch (Exception ex) { Debug.LogWarning("[Unity DocSnap] Could not read packages via PackageManager: " + ex.Message); }

            if (packages != null && packages.Length > 0)
            {
                foreach (PackageInfo pkg in packages)
                {
                    var entry = BuildEntry(pkg);
                    if (entry != null) { result.Add(entry); }
                }
            }
            else
            {
                // Fallback: parse Packages/manifest.json directly. Less rich
                // (name + version only), but still lists what the project pulls in.
                result.AddRange(ReadFromManifestJson());
            }

            result.Sort(CompareForDisplay);
            return result;
        }

        // ==========================================
        // BuildEntry
        // Converts one PackageInfo into a manifest entry,
        // guarding every optional property access.
        // ==========================================
        private static AmirCollider.UnityDocSnap.Editor.Manifest.ManifestPackageEntry BuildEntry(PackageInfo pkg)
        {
            if (pkg == null || string.IsNullOrEmpty(pkg.name)) { return null; }

            var entry = new AmirCollider.UnityDocSnap.Editor.Manifest.ManifestPackageEntry
            {
                name = pkg.name,
                displayName = string.IsNullOrEmpty(pkg.displayName) ? pkg.name : pkg.displayName,
                version = pkg.version ?? "",
                source = pkg.source.ToString(),
                description = pkg.description ?? "",
                category = Categorize(pkg)
            };

            try { if (pkg.author != null) { entry.author = pkg.author.name ?? ""; } } catch { /* author optional */ }

            entry.url = ResolveAccessUrl(pkg);

            // Update availability: only meaningful for registry / built-in
            // packages, which have a known "latest" from Unity's registry.
            try
            {
                string latest = ResolveLatestVersion(pkg);
                if (!string.IsNullOrEmpty(latest))
                {
                    entry.latestVersion = latest;
                    entry.updateAvailable =
                        (pkg.source == PackageSource.Registry || pkg.source == PackageSource.BuiltIn)
                        && !string.IsNullOrEmpty(entry.version)
                        && IsNewer(latest, entry.version);
                }
            }
            catch { /* version info is best-effort */ }

            return entry;
        }

        // ==========================================
        // IsNewer
        // A best-effort "is 'candidate' a newer version
        // than 'current'?" using a dotted-numeric compare
        // (the pre-release suffix after '-'/'+' is ignored).
        // Deliberately conservative: anything it cannot
        // parse is treated as "not newer", so a package is
        // never falsely flagged as having an update.
        // ==========================================
        private static bool IsNewer(string candidate, string current)
        {
            int[] a = ParseVersion(candidate);
            int[] b = ParseVersion(current);
            if (a == null || b == null) { return false; }
            int len = Math.Max(a.Length, b.Length);
            for (int i = 0; i < len; i++)
            {
                int av = i < a.Length ? a[i] : 0;
                int bv = i < b.Length ? b[i] : 0;
                if (av != bv) { return av > bv; }
            }
            return false;
        }

        private static int[] ParseVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) { return null; }
            int cut = version.IndexOfAny(new[] { '-', '+', ' ' });
            string core = cut >= 0 ? version.Substring(0, cut) : version;
            string[] parts = core.Split('.');
            var nums = new List<int>();
            foreach (string part in parts)
            {
                int n;
                if (!int.TryParse(part, out n)) { return null; }
                nums.Add(n);
            }
            return nums.Count > 0 ? nums.ToArray() : null;
        }

        // ==========================================
        // Categorize
        // "builtin"  → Unity's engine modules (always present)
        // "unity"    → Unity's registry packages (com.unity.*)
        // "thirdparty" → everything else (Git, local, embedded,
        //               tarball, or a non-Unity registry — the
        //               Asset Store / GitHub bucket)
        // ==========================================
        private static string Categorize(PackageInfo pkg)
        {
            if (pkg.source == PackageSource.BuiltIn) { return "builtin"; }
            bool isUnityName = pkg.name.StartsWith("com.unity.", StringComparison.OrdinalIgnoreCase);
            if (pkg.source == PackageSource.Registry && isUnityName) { return "unity"; }
            return "thirdparty";
        }

        // ==========================================
        // ResolveAccessUrl
        // The best "open this" link for a package:
        // its documentation URL, else its Git/repository
        // URL, else nothing.
        // ==========================================
        private static string ResolveAccessUrl(PackageInfo pkg)
        {
            try { if (!string.IsNullOrEmpty(pkg.documentationUrl)) { return pkg.documentationUrl; } } catch { /* optional */ }
            try
            {
                if (pkg.repository != null && !string.IsNullOrEmpty(pkg.repository.url))
                {
                    return NormalizeRepoUrl(pkg.repository.url);
                }
            }
            catch { /* optional */ }
            return "";
        }

        // ==========================================
        // NormalizeRepoUrl
        // Turns a "git+https://…git" or "…git" clone URL
        // into a plain https link a browser can open.
        // ==========================================
        private static string NormalizeRepoUrl(string url)
        {
            string u = url;
            if (u.StartsWith("git+", StringComparison.OrdinalIgnoreCase)) { u = u.Substring(4); }
            if (u.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
            {
                u = "https://github.com/" + u.Substring("git@github.com:".Length);
            }
            if (u.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) { u = u.Substring(0, u.Length - 4); }
            return u;
        }

        // ==========================================
        // ResolveLatestVersion
        // The newest version Unity knows about for this
        // package. The exact property has drifted across
        // Unity versions (latestCompatible on older,
        // recommended on newer, and latestCompatible is
        // deprecated in Unity 6), so it is read via
        // reflection - whichever of these properties this
        // Unity version exposes - rather than taking a hard
        // compile-time dependency on any single one. That
        // keeps the package compiling across its whole
        // 2021.3 - Unity 6 support range.
        // ==========================================
        private static readonly string[] LatestVersionProps = { "recommended", "latestCompatible", "latest" };

        private static string ResolveLatestVersion(PackageInfo pkg)
        {
            object versions = pkg.versions;
            if (versions == null) { return ""; }

            Type versionsType = versions.GetType();
            foreach (string propName in LatestVersionProps)
            {
                try
                {
                    PropertyInfo pi = versionsType.GetProperty(propName);
                    if (pi == null) { continue; }
                    string value = pi.GetValue(versions, null) as string;
                    if (!string.IsNullOrEmpty(value)) { return value; }
                }
                catch { /* property shape differs on this Unity version - try the next */ }
            }
            return "";
        }

        // ==========================================
        // CompareForDisplay
        // Third-party first (what a reader most wants to
        // see), then Unity registry packages, then the
        // built-in modules; alphabetical within a group.
        // ==========================================
        private static int CompareForDisplay(
            AmirCollider.UnityDocSnap.Editor.Manifest.ManifestPackageEntry a,
            AmirCollider.UnityDocSnap.Editor.Manifest.ManifestPackageEntry b)
        {
            int ra = CategoryRank(a.category);
            int rb = CategoryRank(b.category);
            if (ra != rb) { return ra.CompareTo(rb); }
            return string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase);
        }

        private static int CategoryRank(string category)
        {
            switch (category)
            {
                case "thirdparty": return 0;
                case "unity": return 1;
                default: return 2; // builtin
            }
        }

        // ==========================================
        // ReadFromManifestJson
        // Minimal fallback used only when the Package
        // Manager API returns nothing: reads the top-level
        // "dependencies" object out of Packages/manifest.json
        // with a tiny, forgiving scan (no dependency on the
        // project's own JSON model, which lives in another
        // namespace). Name + version + category only.
        // ==========================================
        private static IEnumerable<AmirCollider.UnityDocSnap.Editor.Manifest.ManifestPackageEntry> ReadFromManifestJson()
        {
            var list = new List<AmirCollider.UnityDocSnap.Editor.Manifest.ManifestPackageEntry>();
            try
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");
                if (!File.Exists(manifestPath)) { return list; }

                var parsed = AmirCollider.UnityDocSnap.Editor.Json.JsonValue.Parse(File.ReadAllText(manifestPath));
                var deps = parsed.Get("dependencies");
                foreach (var member in deps.Members)
                {
                    string name = member.Key;
                    string version = member.Value.AsString("");
                    bool isUnity = name.StartsWith("com.unity.", StringComparison.OrdinalIgnoreCase);
                    list.Add(new AmirCollider.UnityDocSnap.Editor.Manifest.ManifestPackageEntry
                    {
                        name = name,
                        displayName = name,
                        version = version,
                        source = version.StartsWith("http", StringComparison.OrdinalIgnoreCase) || version.Contains("git")
                            ? "Git"
                            : (isUnity ? "Registry" : "Registry"),
                        category = isUnity ? "unity" : "thirdparty"
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Unity DocSnap] Could not read Packages/manifest.json fallback: " + ex.Message);
            }
            return list;
        }
    }
}
