// ==========================================
// ScenePageRenderer
// Builds scenes/<SceneName>.html: page header
// with quick stats, then the full Hierarchy
// tree with every GameObject's Inspector data.
// ==========================================
using System.Collections.Generic;
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Html
{
    internal static class ScenePageRenderer
    {
        // ==========================================
        // Render
        // ==========================================
        public static string Render(JsonValue sceneData, ManifestState manifest, string htmlFile)
        {
            string sceneName = sceneData.Get("sceneName").AsString("Scene");
            int count = (int)sceneData.Get("totalGameObjects").AsNumber();

            var resolver = new RefLinkResolver
            {
                GuidLookup = DocSnapManifest.BuildGuidLookup(manifest),
                LocalAnchors = FieldRenderer.BuildLocalAnchors(sceneData.Get("rootObjects")),
                LinkPrefix = "../"
            };

            var badges = new List<string>
            {
                HtmlPageBuilder.BadgeRaw(null, count + " " + HtmlPageBuilder.I18n("span", null, "GameObjects", "GameObject", "GameObject")),
                HtmlPageBuilder.BadgeRaw("ghost", HtmlPageBuilder.I18n("span", null, "Exported", "エクスポート日時", "اکسپورت‌شده") + " " + HtmlPageBuilder.Escape(sceneData.Get("exportedUtc").AsString("")))
            };
            string header = HtmlPageBuilder.RenderPageHeader("\uD83C\uDF33", sceneName, sceneData.Get("scenePath").AsString(""), badges);
            string body = FieldRenderer.RenderHierarchy(sceneData.Get("rootObjects"), resolver, "ds-hier-scene");

            return HtmlPageBuilder.RenderPage(manifest, htmlFile, sceneName, header, body);
        }
    }
}
