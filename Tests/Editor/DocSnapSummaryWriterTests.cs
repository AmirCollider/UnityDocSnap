// ==========================================
// DocSnapSummaryWriterTests
// Tests for the "simple" summary output — the
// short Markdown + JSON that gets pasted into an
// AI assistant. These build the same JsonValue
// tree shape the Scene / asset exporters produce
// and assert the summariser's contract: it lists
// the hierarchy, expands ONLY the project's own
// scripts (never Unity's built-in components), and
// keeps container values collapsed. The JSON form
// is checked by parsing it straight back with the
// JsonValue parser.
// ==========================================
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Summary;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public class DocSnapSummaryWriterTests
    {
        // ------------------------------------------
        // Tree builders mirroring the exporter's shape
        // ------------------------------------------
        private static JsonValue Vec3(float x, float y, float z)
        {
            return JsonValue.Obj().Set("x", x).Set("y", y).Set("z", z);
        }

        private static JsonValue Transform()
        {
            return JsonValue.Obj()
                .Set("localPosition", Vec3(0, 0, 0))
                .Set("localEulerAngles", Vec3(0, 0, 0))
                .Set("localScale", Vec3(1, 1, 1));
        }

        private static JsonValue Field(string name, string kind, JsonValue value)
        {
            return JsonValue.Obj().Set("name", name).Set("kind", kind).Set("value", value);
        }

        private static JsonValue UserScript(string typeName, string scriptPath, JsonValue fields)
        {
            return JsonValue.Obj()
                .Set("typeName", typeName)
                .Set("isMissing", false)
                .Set("isUserScript", true)
                .Set("isBehaviour", true)
                .Set("enabled", true)
                .Set("scriptPath", scriptPath)
                .Set("fields", fields);
        }

        private static JsonValue BuiltinComponent(string typeName)
        {
            return JsonValue.Obj()
                .Set("typeName", typeName)
                .Set("isMissing", false)
                .Set("isUserScript", false)
                .Set("isBehaviour", true)
                .Set("enabled", true)
                .Set("fields", JsonValue.Arr());
        }

        private static JsonValue GameObject(string name, JsonValue components, JsonValue children)
        {
            return JsonValue.Obj()
                .Set("name", name)
                .Set("tag", "Untagged")
                .Set("activeSelf", true)
                .Set("layerName", "Default")
                .Set("transform", Transform())
                .Set("components", components ?? JsonValue.Arr())
                .Set("children", children ?? JsonValue.Arr());
        }

        private static JsonValue BuildScene()
        {
            JsonValue heroFields = JsonValue.Arr()
                .Add(Field("health", "int", JsonValue.Num(100)))
                .Add(Field("speed", "float", JsonValue.Num(3.5)))
                .Add(JsonValue.Obj().Set("name", "waypoints").Set("kind", "array").Set("count", 4).Set("truncated", false).Set("items", JsonValue.Arr()));

            JsonValue hero = GameObject("Hero",
                JsonValue.Arr()
                    .Add(BuiltinComponent("SpriteRenderer"))
                    .Add(UserScript("HeroController", "Assets/Scripts/HeroController.cs", heroFields)),
                JsonValue.Arr().Add(GameObject("Weapon", JsonValue.Arr().Add(BuiltinComponent("BoxCollider2D")), null)));

            return JsonValue.Obj()
                .Set("sceneName", "TestScene")
                .Set("scenePath", "Assets/Scenes/TestScene.unity")
                .Set("unityVersion", "2022.3.10f1")
                .Set("exportedUtc", "2026-07-23T00:00:00Z")
                .Set("totalGameObjects", 2)
                .Set("rootObjects", JsonValue.Arr().Add(hero));
        }

        private static JsonValue BuildFolder()
        {
            JsonValue hero = JsonValue.Obj()
                .Set("path", "Assets/Art/hero.png").Set("fileName", "hero.png")
                .Set("mainType", "Texture2D").Set("imageWidth", 256).Set("imageHeight", 256)
                .Set("fileSizeBytes", 2048);
            JsonValue config = JsonValue.Obj()
                .Set("path", "Assets/Art/config.asset").Set("fileName", "config.asset")
                .Set("mainType", "GameConfig").Set("fileSizeBytes", 512);

            JsonValue tree = JsonValue.Obj()
                .Set("folderName", "Art")
                .Set("folderPath", "Assets/Art")
                .Set("directFileCount", 2)
                .Set("totalFileCount", 2)
                .Set("filePaths", JsonValue.Arr().Add(JsonValue.Str("Assets/Art/hero.png")).Add(JsonValue.Str("Assets/Art/config.asset")))
                .Set("subfolders", JsonValue.Arr());

            return JsonValue.Obj()
                .Set("folderPath", "Assets/Art")
                .Set("folderKey", "Art")
                .Set("exportedUtc", "2026-07-23T00:00:00Z")
                .Set("fileCount", 2)
                .Set("files", JsonValue.Arr().Add(hero).Add(config))
                .Set("folderTree", tree);
        }

        // ------------------------------------------
        // Scene Markdown
        // ------------------------------------------
        [Test]
        public void Scene_Markdown_HasTitleAndHierarchy()
        {
            string md = DocSnapSummaryWriter.RenderScene(BuildScene());
            StringAssert.Contains("# TestScene", md);
            StringAssert.Contains("## Hierarchy", md);
            StringAssert.Contains("Hero", md);
            StringAssert.Contains("Weapon", md);
        }

        [Test]
        public void Scene_Markdown_ExpandsOwnScriptFieldsOnly()
        {
            string md = DocSnapSummaryWriter.RenderScene(BuildScene());
            // The user script is expanded with its values...
            StringAssert.Contains("HeroController", md);
            StringAssert.Contains("health", md);
            StringAssert.Contains("100", md);
            // ...but a container value stays collapsed, never exploded.
            StringAssert.Contains("[4", md); // "[4 items]" style summary
        }

        [Test]
        public void Scene_Markdown_DoesNotTreatBuiltinComponentAsCustomScript()
        {
            string md = DocSnapSummaryWriter.RenderScene(BuildScene());
            // Built-ins appear as plain type names in the hierarchy line...
            StringAssert.Contains("SpriteRenderer", md);
            // ...and the reported custom-script count is exactly one (HeroController).
            StringAssert.Contains("1 custom scripts", md);
        }

        // ------------------------------------------
        // Scene JSON
        // ------------------------------------------
        [Test]
        public void Scene_Json_IsValidAndReportsTotals()
        {
            string json = DocSnapSummaryWriter.RenderSceneJson(BuildScene());
            JsonValue parsed = JsonValue.Parse(json);
            Assert.AreEqual("scene-summary", parsed.Get("kind").AsString());
            Assert.AreEqual("TestScene", parsed.Get("scene").AsString());
            Assert.AreEqual(2, parsed.Get("totals").Get("gameObjects").AsNumber());
            Assert.AreEqual(1, parsed.Get("totals").Get("customScripts").AsNumber());
            Assert.Greater(parsed.Get("hierarchy").Items.Count, 0);
        }

        // ------------------------------------------
        // Folder Markdown + JSON
        // ------------------------------------------
        [Test]
        public void Folder_Markdown_ListsFilesWithFacts()
        {
            string md = DocSnapSummaryWriter.RenderFolder(BuildFolder());
            StringAssert.Contains("# Assets/Art", md);
            StringAssert.Contains("hero.png", md);
            StringAssert.Contains("Texture2D", md);
            StringAssert.Contains("256", md);
            StringAssert.Contains("config.asset", md);
        }

        [Test]
        public void Folder_Json_IsValidAndCountsFiles()
        {
            string json = DocSnapSummaryWriter.RenderFolderJson(BuildFolder());
            JsonValue parsed = JsonValue.Parse(json);
            Assert.AreEqual("folder-summary", parsed.Get("kind").AsString());
            Assert.AreEqual(2, parsed.Get("files").AsNumber());
            Assert.AreEqual("Assets/Art", parsed.Get("folder").AsString());
        }

        // ------------------------------------------
        // Output path helpers
        // ------------------------------------------
        [Test]
        public void SummaryPaths_LiveUnderSummaryFolderWithExpectedPrefixes()
        {
            StringAssert.StartsWith("summary/scene-", DocSnapSummaryWriter.SceneSummaryMarkdown("Main"));
            StringAssert.EndsWith(".md", DocSnapSummaryWriter.SceneSummaryMarkdown("Main"));
            StringAssert.StartsWith("summary/folder-", DocSnapSummaryWriter.FolderSummaryJson("Art"));
            StringAssert.EndsWith(".json", DocSnapSummaryWriter.FolderSummaryJson("Art"));
        }
    }
}
