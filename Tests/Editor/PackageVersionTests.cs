// ==========================================
// PackageVersionTests
// The version string is written into every
// generated manifest.json, export-info.json,
// summary footer, dashboard badge and the About
// window - from DocSnapConstants.Version. The
// package's own version lives in package.json.
//
// Nothing kept the two in step, and they drifted:
// the constant sat at 0.6.2 while the package (and
// the changelog) were on 0.6.4, so two releases'
// worth of exports told their reader they had been
// made by a version of the tool that had not
// existed for weeks.
//
// This test is the mechanism that was missing. It
// is deliberately a test rather than "read
// package.json at runtime": the constant stays a
// compile-time constant with no file I/O on the
// export path, and the drift is caught when the
// suite runs instead of in someone's output.
// ==========================================
using System.IO;
using AmirCollider.UnityDocSnap.Editor;
using NUnit.Framework;
using UnityEditor;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class PackageVersionTests
    {
        [Test]
        public void Constant_MatchesPackageJson()
        {
            string packageJsonPath = FindPackageJson();
            if (packageJsonPath == null)
            {
                Assert.Ignore("package.json is not reachable from this test context (the package is not installed as a UPM package here).");
                return;
            }

            string declared = ReadVersion(File.ReadAllText(packageJsonPath));
            Assert.IsNotNull(declared, "package.json has no \"version\" field: " + packageJsonPath);
            Assert.AreEqual(declared, DocSnapConstants.Version,
                "DocSnapConstants.Version and package.json disagree. Every exported site reports DocSnapConstants.Version, so it has to be the package version.");
        }

        [Test]
        public void Version_IsNotEmpty()
        {
            Assert.IsNotEmpty(DocSnapConstants.Version);
        }

        // ==========================================
        // FindPackageJson
        // Walks up from this test script's own location
        // until a package.json turns up, so the test works
        // whether the package is embedded in Packages/, in
        // Assets/, or installed from a registry.
        // ==========================================
        private static string FindPackageJson()
        {
            string scriptPath = AssetDatabase.GUIDToAssetPath(
                AssetDatabase.FindAssets("PackageVersionTests t:MonoScript").Length > 0
                    ? AssetDatabase.FindAssets("PackageVersionTests t:MonoScript")[0]
                    : "");

            string directory = string.IsNullOrEmpty(scriptPath)
                ? Path.GetDirectoryName(Path.GetFullPath("Assets"))
                : Path.GetDirectoryName(Path.GetFullPath(scriptPath));

            for (int i = 0; i < 8 && !string.IsNullOrEmpty(directory); i++)
            {
                string candidate = Path.Combine(directory, "package.json");
                if (File.Exists(candidate)) { return candidate; }
                DirectoryInfo parent = Directory.GetParent(directory);
                directory = parent != null ? parent.FullName : null;
            }
            return null;
        }

        // ==========================================
        // ReadVersion
        // A three-line reader rather than a JSON parse:
        // this test must not depend on the very JsonValue
        // class the rest of the suite is busy testing.
        // ==========================================
        private static string ReadVersion(string json)
        {
            const string key = "\"version\"";
            int keyIndex = json.IndexOf(key, System.StringComparison.Ordinal);
            if (keyIndex < 0) { return null; }

            int colon = json.IndexOf(':', keyIndex + key.Length);
            if (colon < 0) { return null; }

            int open = json.IndexOf('"', colon + 1);
            if (open < 0) { return null; }

            int close = json.IndexOf('"', open + 1);
            if (close < 0) { return null; }

            return json.Substring(open + 1, close - open - 1);
        }
    }
}
