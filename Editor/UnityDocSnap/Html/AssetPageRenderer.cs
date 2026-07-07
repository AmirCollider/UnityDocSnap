// ==========================================
// AssetPageRenderer
// Builds assets/<FolderKey>.html: page header
// with file count, then a card grid with every
// file's import settings, shader properties,
// or Prefab contents.
// ==========================================
using System.Collections.Generic;
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Html
{
    internal static class AssetPageRenderer
    {
        // ==========================================
        // Render
        // ==========================================
        public static string Render(JsonValue folderData, ManifestState manifest, string htmlFile)
        {
            string folderPath = folderData.Get("folderPath").AsString("Assets");
            int fileCount = (int)folderData.Get("fileCount").AsNumber();

            var resolver = new RefLinkResolver
            {
                GuidLookup = DocSnapManifest.BuildGuidLookup(manifest),
                LocalAnchors = null,
                LinkPrefix = "../"
            };

            var badges = new List<string>
            {
                HtmlPageBuilder.BadgeRaw(null, fileCount + " " + HtmlPageBuilder.I18n("span", null, "files", "ファイル", "فایل")),
                HtmlPageBuilder.BadgeRaw("ghost", HtmlPageBuilder.I18n("span", null, "Exported", "エクスポート日時", "اکسپورت‌شده") + " " + HtmlPageBuilder.Escape(folderData.Get("exportedUtc").AsString("")))
            };
            string header = HtmlPageBuilder.RenderPageHeader("\uD83D\uDCC1", folderPath, "", badges);
            string body = FieldRenderer.RenderAssetGrid(folderData.Get("files"), resolver);

            return HtmlPageBuilder.RenderPage(manifest, htmlFile, folderPath, header, body);
        }
    }
}
