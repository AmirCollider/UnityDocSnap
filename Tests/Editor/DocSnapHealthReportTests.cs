// ==========================================
// DocSnapHealthReportTests
// The health pass is what turns data the export
// already had into the thing a person opens the
// documentation to find out. If it under-counts,
// a broken project is reported as clean - which is
// worse than not reporting at all.
// ==========================================
using System.Collections.Generic;
using AmirCollider.UnityDocSnap.Editor.Export;
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Manifest;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class DocSnapHealthReportTests
    {
        // A minimal Scene tree in exactly the shape
        // SceneHierarchyExporter writes.
        private static JsonValue SceneWith(int missingScripts, int missingRefs)
        {
            var components = JsonValue.Arr();
            for (int i = 0; i < missingScripts; i++)
            {
                components.Add(JsonValue.Obj().Set("typeName", "Missing Script").Set("isMissing", true));
            }

            var fields = JsonValue.Arr();
            for (int i = 0; i < missingRefs; i++)
            {
                fields.Add(JsonValue.Obj().Set("kind", "objectRef").Set("isNull", false).Set("isMissing", true));
            }
            components.Add(JsonValue.Obj().Set("typeName", "Rigidbody").Set("isMissing", false).Set("fields", fields));

            var root = JsonValue.Obj();
            root.Set("totalGameObjects", 1);
            root.Set("rootObjects", JsonValue.Arr().Add(JsonValue.Obj()
                .Set("name", "Player")
                .Set("instanceId", 1)
                .Set("components", components)
                .Set("children", JsonValue.Arr())));
            return root;
        }

        [Test]
        public void CleanScene_ReportsNothing()
        {
            ManifestHealthEntry entry = DocSnapHealthReport.BuildSceneEntry(SceneWith(0, 0), "Main", "Main", "scenes/Main.html", new List<ManifestIssueEntry>());

            Assert.AreEqual(0, entry.missingScripts);
            Assert.AreEqual(0, entry.missingReferences);
        }

        [Test]
        public void MissingScriptsAndReferences_AreCountedSeparately()
        {
            ManifestHealthEntry entry = DocSnapHealthReport.BuildSceneEntry(SceneWith(2, 3), "Main", "Main", "scenes/Main.html", new List<ManifestIssueEntry>());

            Assert.AreEqual(2, entry.missingScripts);
            Assert.AreEqual(3, entry.missingReferences);
        }

        [Test]
        public void NestedChildren_AreAlsoScanned()
        {
            JsonValue scene = SceneWith(0, 0);
            JsonValue rootObject = scene.Get("rootObjects").Items[0];
            rootObject.Get("children").Add(JsonValue.Obj()
                .Set("name", "Child")
                .Set("instanceId", 2)
                .Set("components", JsonValue.Arr().Add(
                    JsonValue.Obj().Set("typeName", "Missing Script").Set("isMissing", true)))
                .Set("children", JsonValue.Arr()));

            ManifestHealthEntry entry = DocSnapHealthReport.BuildSceneEntry(scene, "Main", "Main", "scenes/Main.html", new List<ManifestIssueEntry>());
            Assert.AreEqual(1, entry.missingScripts);
        }

        [Test]
        public void DeepHierarchy_DoesNotOverflowTheStack()
        {
            // The whole reason the scan is iterative. A recursive
            // version would die here, and StackOverflowException
            // cannot be caught - it takes the Editor with it.
            var root = JsonValue.Obj();
            root.Set("totalGameObjects", 5000);

            var deepest = JsonValue.Obj()
                .Set("name", "Leaf")
                .Set("components", JsonValue.Arr().Add(JsonValue.Obj().Set("typeName", "Missing Script").Set("isMissing", true)))
                .Set("children", JsonValue.Arr());

            JsonValue current = deepest;
            for (int i = 0; i < 5000; i++)
            {
                current = JsonValue.Obj()
                    .Set("name", "Node")
                    .Set("components", JsonValue.Arr())
                    .Set("children", JsonValue.Arr().Add(current));
            }
            root.Set("rootObjects", JsonValue.Arr().Add(current));

            ManifestHealthEntry entry = DocSnapHealthReport.BuildSceneEntry(root, "Deep", "Deep", "scenes/Deep.html", new List<ManifestIssueEntry>());
            Assert.AreEqual(1, entry.missingScripts);
        }

        [Test]
        public void UnresolvedAssets_AreCountedFromMainType()
        {
            var folder = JsonValue.Obj();
            folder.Set("fileCount", 3);
            folder.Set("files", JsonValue.Arr()
                .Add(JsonValue.Obj().Set("mainType", "Texture2D"))
                .Add(JsonValue.Obj().Set("mainType", "Unknown"))
                .Add(JsonValue.Obj().Set("mainType", "Unknown")));

            ManifestHealthEntry entry = DocSnapHealthReport.BuildFolderEntry(folder, "Assets", "Assets", "folders/Assets.html", new List<ManifestIssueEntry>());
            Assert.AreEqual(2, entry.unresolvedAssets);
        }

        // ==========================================
        // The point of the whole feature: a count alone
        // ("8 broken references") sends the reader to a page
        // with thousands of rows on it. These assert that
        // each finding knows the object, the field and the
        // anchor it belongs to.
        // ==========================================
        [Test]
        public void EachFinding_CarriesItsObjectPath_ComponentAndField()
        {
            var fields = JsonValue.Arr().Add(JsonValue.Obj()
                .Set("name", "target")
                .Set("kind", "objectRef")
                .Set("isNull", false)
                .Set("isMissing", true));

            var scene = JsonValue.Obj();
            scene.Set("totalGameObjects", 2);
            scene.Set("rootObjects", JsonValue.Arr().Add(JsonValue.Obj()
                .Set("name", "Canvas")
                .Set("instanceId", 10)
                .Set("components", JsonValue.Arr())
                .Set("children", JsonValue.Arr().Add(JsonValue.Obj()
                    .Set("name", "StartButton")
                    .Set("instanceId", 42)
                    .Set("components", JsonValue.Arr().Add(JsonValue.Obj()
                        .Set("typeName", "MenuController")
                        .Set("isMissing", false)
                        .Set("fields", fields)))
                    .Set("children", JsonValue.Arr())))));

            var issues = new List<ManifestIssueEntry>();
            DocSnapHealthReport.BuildSceneEntry(scene, "Main", "Main", "scenes/Main.html", issues);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(DocSnapHealthReport.KindMissingReference, issues[0].kind);
            Assert.AreEqual("Canvas/StartButton", issues[0].location);
            Assert.AreEqual("MenuController › target", issues[0].detail);
            Assert.AreEqual("scenes/Main.html", issues[0].htmlFile);
            Assert.AreEqual("go-42", issues[0].anchor);
        }

        [Test]
        public void ReferencesNestedInArraysAndStructs_AreFoundAndPathed()
        {
            var broken = JsonValue.Obj().Set("kind", "objectRef").Set("isNull", false).Set("isMissing", true);
            var element = JsonValue.Obj()
                .Set("name", "entry")
                .Set("kind", "generic")
                .Set("fields", JsonValue.Arr().Add(JsonValue.Obj()
                    .Set("name", "marker")
                    .Set("kind", "objectRef")
                    .Set("isNull", false)
                    .Set("isMissing", true)));

            var fields = JsonValue.Arr()
                .Add(JsonValue.Obj().Set("name", "waypoints").Set("kind", "array")
                    .Set("items", JsonValue.Arr().Add(JsonValue.Obj().Set("kind", "generic").Set("fields", JsonValue.Arr())).Add(element)))
                .Add(broken.Set("name", "direct"));

            var scene = JsonValue.Obj();
            scene.Set("totalGameObjects", 1);
            scene.Set("rootObjects", JsonValue.Arr().Add(JsonValue.Obj()
                .Set("name", "Player")
                .Set("instanceId", 7)
                .Set("components", JsonValue.Arr().Add(JsonValue.Obj()
                    .Set("typeName", "PathFollower").Set("isMissing", false).Set("fields", fields)))
                .Set("children", JsonValue.Arr())));

            var issues = new List<ManifestIssueEntry>();
            ManifestHealthEntry entry = DocSnapHealthReport.BuildSceneEntry(scene, "Main", "Main", "scenes/Main.html", issues);

            Assert.AreEqual(2, entry.missingReferences);
            var details = new List<string>();
            foreach (ManifestIssueEntry i in issues) { details.Add(i.detail); }
            details.Sort();
            Assert.AreEqual(new[] { "PathFollower › direct", "PathFollower › waypoints[1].marker" }, details.ToArray());
        }

        [Test]
        public void UnresolvedAsset_LinksToItsOwnCard()
        {
            var folder = JsonValue.Obj();
            folder.Set("fileCount", 1);
            folder.Set("files", JsonValue.Arr().Add(JsonValue.Obj()
                .Set("mainType", "Unknown")
                .Set("path", "Assets/Weird/thing.xyz")
                .Set("fileName", "thing.xyz")
                .Set("extension", ".xyz")
                .Set("guid", "abc123")));

            var issues = new List<ManifestIssueEntry>();
            DocSnapHealthReport.BuildFolderEntry(folder, "Assets", "Assets", "folders/Assets.html", issues);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(DocSnapHealthReport.KindUnresolvedAsset, issues[0].kind);
            Assert.AreEqual("Assets/Weird/thing.xyz", issues[0].location);
            Assert.AreEqual("asset-abc123", issues[0].anchor);
        }

        [Test]
        public void FindingsInsideAPrefabAsset_AreLabelledWithThePrefab()
        {
            var folder = JsonValue.Obj();
            folder.Set("fileCount", 1);
            folder.Set("files", JsonValue.Arr().Add(JsonValue.Obj()
                .Set("mainType", "GameObject")
                .Set("path", "Assets/Prefabs/Player.prefab")
                .Set("fileName", "Player.prefab")
                .Set("guid", "pref01")
                .Set("prefabRoot", JsonValue.Obj()
                    .Set("name", "Player")
                    .Set("instanceId", 99)
                    .Set("components", JsonValue.Arr().Add(
                        JsonValue.Obj().Set("typeName", "Missing Script").Set("isMissing", true)))
                    .Set("children", JsonValue.Arr()))));

            var issues = new List<ManifestIssueEntry>();
            ManifestHealthEntry entry = DocSnapHealthReport.BuildFolderEntry(folder, "Assets", "Assets", "folders/Assets.html", issues);

            Assert.AreEqual(1, entry.missingScripts);
            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual("Player.prefab › Player", issues[0].location);
            // Pinned to the Prefab's asset card, not to a GameObject
            // anchor that only exists on a Scene page.
            Assert.AreEqual("asset-pref01", issues[0].anchor);
        }

        [Test]
        public void PerScopeIssueCap_IsReportedRatherThanSilent()
        {
            var components = JsonValue.Arr();
            for (int i = 0; i < DocSnapConstants.MaxIssuesPerScope + 5; i++)
            {
                components.Add(JsonValue.Obj().Set("typeName", "Missing Script").Set("isMissing", true));
            }

            var scene = JsonValue.Obj();
            scene.Set("totalGameObjects", 1);
            scene.Set("rootObjects", JsonValue.Arr().Add(JsonValue.Obj()
                .Set("name", "Broken")
                .Set("instanceId", 1)
                .Set("components", components)
                .Set("children", JsonValue.Arr())));

            var issues = new List<ManifestIssueEntry>();
            ManifestHealthEntry entry = DocSnapHealthReport.BuildSceneEntry(scene, "Main", "Main", "scenes/Main.html", issues);

            // The COUNT stays complete even though the list is capped -
            // a truncated list beside a truncated count would hide the
            // problem twice over.
            Assert.AreEqual(DocSnapConstants.MaxIssuesPerScope + 5, entry.missingScripts);
            Assert.AreEqual(DocSnapConstants.MaxIssuesPerScope, issues.Count);
            Assert.IsTrue(entry.issuesTruncated);
        }

        [Test]
        public void SortedIssues_PutTheHardestFailuresFirst()
        {
            var state = new ManifestState();
            state.issues.Add(new ManifestIssueEntry { kind = DocSnapHealthReport.KindUnresolvedAsset, scopeLabel = "Assets", location = "a" });
            state.issues.Add(new ManifestIssueEntry { kind = DocSnapHealthReport.KindMissingScript, scopeLabel = "Main", location = "b" });
            state.issues.Add(new ManifestIssueEntry { kind = DocSnapHealthReport.KindMissingReference, scopeLabel = "Main", location = "c" });

            List<ManifestIssueEntry> sorted = DocSnapHealthReport.SortedIssues(state);
            Assert.AreEqual(DocSnapHealthReport.KindMissingScript, sorted[0].kind);
            Assert.AreEqual(DocSnapHealthReport.KindMissingReference, sorted[1].kind);
            Assert.AreEqual(DocSnapHealthReport.KindUnresolvedAsset, sorted[2].kind);
        }

        [Test]
        public void DuplicateSceneNames_AreDetected()
        {
            var state = new ManifestState();
            state.scenes.Add(new ManifestSceneEntry { sceneName = "Main", sceneKey = "Main-aaa" });
            state.scenes.Add(new ManifestSceneEntry { sceneName = "Main", sceneKey = "Main-bbb" });
            state.scenes.Add(new ManifestSceneEntry { sceneName = "Menu", sceneKey = "Menu" });

            Assert.AreEqual(new[] { "Main" }, DocSnapHealthReport.DuplicateSceneNames(state).ToArray());
        }

        [Test]
        public void Totals_RollUpEveryScope_AndReportCleanWhenEmpty()
        {
            var state = new ManifestState();
            Assert.IsTrue(DocSnapHealthReport.Totals(state).IsClean);

            state.health.Add(new ManifestHealthEntry { scope = "A", missingScripts = 2 });
            state.health.Add(new ManifestHealthEntry { scope = "B", missingReferences = 5 });

            HealthTotals totals = DocSnapHealthReport.Totals(state);
            Assert.IsFalse(totals.IsClean);
            Assert.AreEqual(2, totals.missingScripts);
            Assert.AreEqual(5, totals.missingReferences);
            Assert.AreEqual(2, totals.affectedScopes);
        }

        [Test]
        public void Worst_SortsMostSevereFirst_AndHonoursTheLimit()
        {
            var state = new ManifestState();
            state.health.Add(new ManifestHealthEntry { scope = "few", label = "few", missingReferences = 1 });
            state.health.Add(new ManifestHealthEntry { scope = "many", label = "many", missingScripts = 4 });
            state.health.Add(new ManifestHealthEntry { scope = "clean", label = "clean" });

            var worst = DocSnapHealthReport.Worst(state, 1);
            Assert.AreEqual(1, worst.Count);
            Assert.AreEqual("many", worst[0].label);
        }
    }
}
