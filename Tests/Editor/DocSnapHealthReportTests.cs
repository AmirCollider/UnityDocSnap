// ==========================================
// DocSnapHealthReportTests
// The health pass is what turns data the export
// already had into the thing a person opens the
// documentation to find out. If it under-counts,
// a broken project is reported as clean - which is
// worse than not reporting at all.
// ==========================================
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
            ManifestHealthEntry entry = DocSnapHealthReport.BuildSceneEntry(SceneWith(0, 0), "Main", "Main", "scenes/Main.html");

            Assert.AreEqual(0, entry.missingScripts);
            Assert.AreEqual(0, entry.missingReferences);
        }

        [Test]
        public void MissingScriptsAndReferences_AreCountedSeparately()
        {
            ManifestHealthEntry entry = DocSnapHealthReport.BuildSceneEntry(SceneWith(2, 3), "Main", "Main", "scenes/Main.html");

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

            ManifestHealthEntry entry = DocSnapHealthReport.BuildSceneEntry(scene, "Main", "Main", "scenes/Main.html");
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

            ManifestHealthEntry entry = DocSnapHealthReport.BuildSceneEntry(root, "Deep", "Deep", "scenes/Deep.html");
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

            ManifestHealthEntry entry = DocSnapHealthReport.BuildFolderEntry(folder, "Assets", "Assets", "folders/Assets.html");
            Assert.AreEqual(2, entry.unresolvedAssets);
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
