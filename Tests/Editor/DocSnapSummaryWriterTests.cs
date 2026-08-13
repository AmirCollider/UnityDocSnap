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
            return BuiltinComponent(typeName, JsonValue.Arr());
        }

        private static JsonValue BuiltinComponent(string typeName, JsonValue fields)
        {
            return JsonValue.Obj()
                .Set("typeName", typeName)
                .Set("isMissing", false)
                .Set("isUserScript", false)
                .Set("isBehaviour", true)
                .Set("enabled", true)
                .Set("fields", fields);
        }

        // A text component as the reflector reports one: TextMesh
        // Pro serializes `m_text` and labels it "Text", Unity's own
        // UI.Text serializes `m_Text`. Neither is a user script, so
        // neither is expanded by the summary - which is exactly why
        // the text needs its own path out.
        private static JsonValue TextComponent(string typeName, string fieldName, string text)
        {
            return BuiltinComponent(typeName, JsonValue.Arr()
                .Add(JsonValue.Obj().Set("name", fieldName).Set("label", "Text").Set("kind", "string").Set("value", text))
                .Add(Field("m_fontSize", "float", JsonValue.Num(36))));
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

            // The UI half: a TMP label with real text, a legacy
            // UI.Text beside it, and one whose text is only
            // whitespace - which is not text and must not be listed.
            JsonValue label = GameObject("PauseButtonTextTMP",
                JsonValue.Arr().Add(TextComponent("TextMeshProUGUI", "m_text", "Resume\nGame")), null);
            JsonValue score = GameObject("ScoreLabel",
                JsonValue.Arr().Add(TextComponent("Text", "m_Text", "Score: 0")), null);
            JsonValue blank = GameObject("EmptyLabel",
                JsonValue.Arr().Add(TextComponent("TextMeshProUGUI", "m_text", "   ")), null);

            JsonValue canvas = GameObject("Canvas",
                JsonValue.Arr().Add(BuiltinComponent("Canvas")),
                JsonValue.Arr().Add(label).Add(score).Add(blank));

            return JsonValue.Obj()
                .Set("sceneName", "TestScene")
                .Set("scenePath", "Assets/Scenes/TestScene.unity")
                .Set("unityVersion", "2022.3.10f1")
                .Set("exportedUtc", "2026-07-23T00:00:00Z")
                .Set("totalGameObjects", 6)
                .Set("rootObjects", JsonValue.Arr().Add(hero).Add(canvas));
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

            // A UI Prefab: the other place a project's on-screen text
            // lives, and one the summary used to report only as an
            // object count.
            JsonValue prefabRoot = GameObject("Hud",
                JsonValue.Arr().Add(BuiltinComponent("Canvas")),
                JsonValue.Arr().Add(GameObject("GameOverLabel",
                    JsonValue.Arr().Add(TextComponent("TextMeshProUGUI", "m_text", "Game Over")), null)));

            JsonValue prefab = JsonValue.Obj()
                .Set("path", "Assets/Art/Hud.prefab").Set("fileName", "Hud.prefab")
                .Set("mainType", "GameObject").Set("prefabGameObjectCount", 2)
                .Set("prefabRoot", prefabRoot).Set("fileSizeBytes", 4096);

            // A ScriptableObject holding a line of dialogue. Its
            // m_Script field is a string too, and must not be read as
            // text.
            JsonValue line = JsonValue.Obj()
                .Set("path", "Assets/Art/Line01.asset").Set("fileName", "Line01.asset")
                .Set("mainType", "DialogueLine").Set("fileSizeBytes", 256)
                .Set("assetFields", JsonValue.Arr()
                    .Add(Field("m_Script", "string", JsonValue.Str("not the text")))
                    .Add(Field("text", "string", JsonValue.Str("Who goes there?"))));

            JsonValue tree = JsonValue.Obj()
                .Set("folderName", "Art")
                .Set("folderPath", "Assets/Art")
                .Set("directFileCount", 4)
                .Set("totalFileCount", 4)
                .Set("filePaths", JsonValue.Arr()
                    .Add(JsonValue.Str("Assets/Art/hero.png"))
                    .Add(JsonValue.Str("Assets/Art/config.asset"))
                    .Add(JsonValue.Str("Assets/Art/Hud.prefab"))
                    .Add(JsonValue.Str("Assets/Art/Line01.asset")))
                .Set("subfolders", JsonValue.Arr());

            return JsonValue.Obj()
                .Set("folderPath", "Assets/Art")
                .Set("folderKey", "Art")
                .Set("exportedUtc", "2026-07-23T00:00:00Z")
                .Set("fileCount", 4)
                .Set("files", JsonValue.Arr().Add(hero).Add(config).Add(prefab).Add(line))
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
            Assert.AreEqual(6, parsed.Get("totals").Get("gameObjects").AsNumber());
            Assert.AreEqual(1, parsed.Get("totals").Get("customScripts").AsNumber());
            Assert.Greater(parsed.Get("hierarchy").Items.Count, 0);
        }

        // ------------------------------------------
        // On-screen text
        //
        // The summary expands the project's own scripts only, and
        // every text component in Unity belongs to Unity or to
        // TextMesh Pro - so every string a project puts on the
        // screen used to be dropped, leaving the reader with the
        // NAME of a label ("PauseButtonTextTMP") and none of its
        // words. These are the tests for the way back out.
        // ------------------------------------------
        [Test]
        public void Scene_Markdown_QuotesTextOfBuiltInTextComponents()
        {
            string md = DocSnapSummaryWriter.RenderScene(BuildScene());

            // TextMesh Pro's m_text and Unity's own m_Text alike.
            StringAssert.Contains("Resume Game", md);
            StringAssert.Contains("Score: 0", md);

            // On the hierarchy line, beside the object that draws it.
            StringAssert.Contains("PauseButtonTextTMP — TextMeshProUGUI · text \"Resume Game\"", md);
        }

        [Test]
        public void Scene_Markdown_ListsEveryTextWithTheObjectPath()
        {
            string md = DocSnapSummaryWriter.RenderScene(BuildScene());

            StringAssert.Contains("## Text", md);
            StringAssert.Contains("**Canvas/PauseButtonTextTMP**", md);
            StringAssert.Contains("2 on-screen texts", md);
        }

        [Test]
        public void Scene_Markdown_DoesNotListWhitespaceOnlyTextAsText()
        {
            string md = DocSnapSummaryWriter.RenderScene(BuildScene());

            // The object is still in the hierarchy - it simply has
            // nothing to say, so it is not quoted and not counted.
            StringAssert.Contains("EmptyLabel", md);
            StringAssert.DoesNotContain("EmptyLabel — TextMeshProUGUI · text", md);
        }

        [Test]
        public void Scene_Json_CarriesTextOnTheNodeAndInTheFlatList()
        {
            JsonValue parsed = JsonValue.Parse(DocSnapSummaryWriter.RenderSceneJson(BuildScene()));

            Assert.AreEqual(2, parsed.Get("totals").Get("texts").AsNumber());
            Assert.AreEqual(2, parsed.Get("texts").Items.Count);

            JsonValue first = parsed.Get("texts").Items[0];
            Assert.AreEqual("Canvas/PauseButtonTextTMP", first.Get("path").AsString());
            Assert.AreEqual("TextMeshProUGUI", first.Get("component").AsString());
            Assert.AreEqual("Resume Game", first.Get("text").AsString());

            // …and on the hierarchy node itself, so the tree answers
            // "what does this object say?" without a cross-reference.
            JsonValue canvas = parsed.Get("hierarchy").Items[1];
            Assert.AreEqual("Resume Game", canvas.Get("children").Items[0].Get("text").AsString());
        }

        [Test]
        public void Folder_Markdown_ListsTextInsidePrefabsAndAssets()
        {
            string md = DocSnapSummaryWriter.RenderFolder(BuildFolder());

            StringAssert.Contains("Hud/GameOverLabel", md);
            StringAssert.Contains("\"Game Over\"", md);
            StringAssert.Contains("\"Who goes there?\"", md);

            // m_Script is a string field on every serialized asset and
            // is never the text.
            StringAssert.DoesNotContain("not the text", md);
        }

        [Test]
        public void Folder_Json_CarriesTextPerFile()
        {
            JsonValue parsed = JsonValue.Parse(DocSnapSummaryWriter.RenderFolderJson(BuildFolder()));
            JsonValue files = parsed.Get("byFolder").Items[0].Get("files");

            JsonValue prefab = files.Items[2];
            Assert.AreEqual("Hud.prefab", prefab.Get("name").AsString());
            Assert.AreEqual(1, prefab.Get("texts").Items.Count);
            Assert.AreEqual("Game Over", prefab.Get("texts").Items[0].Get("text").AsString());

            JsonValue asset = files.Items[3];
            Assert.AreEqual("Who goes there?", asset.Get("texts").Items[0].Get("text").AsString());

            // A texture has no text and gains no empty array for it.
            Assert.IsFalse(files.Items[0].Has("texts"));
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
            Assert.AreEqual(4, parsed.Get("files").AsNumber());
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
