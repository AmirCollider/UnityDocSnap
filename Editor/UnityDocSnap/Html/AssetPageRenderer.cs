// ==========================================
// AssetPageRenderer
// Builds assets/<FolderKey>.html: page header
// with file count, then a collapsible directory
// tree - each folder expandable on its own,
// showing only the files that live directly
// inside it (import settings, shader properties,
// or Prefab contents per file).
// ==========================================
using System;
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

            // OrdinalIgnoreCase: the folder tree normalises separators
            // while this map is keyed on the raw AssetDatabase path.
            // Any case drift between the two silently dropped files
            // out of the rendered tree.
            var filesByPath = new Dictionary<string, JsonValue>(StringComparer.OrdinalIgnoreCase);
            foreach (JsonValue file in folderData.Get("files").Items)
            {
                string path = file.Get("path").AsString("");
                if (!string.IsNullOrEmpty(path)) { filesByPath[path.Replace('\\', '/')] = file; }
            }
            string body = FieldRenderer.RenderFolderTree(folderData.Get("folderTree"), filesByPath, resolver, "ds-hier-assets");

            return HtmlPageBuilder.RenderPage(manifest, htmlFile, folderPath, header, body);
        }
    }
}
