// ==========================================
// IndexPageRenderer
// Builds index.html: the site's front door -
// quick stats plus a live list of every
// exported Scene and Asset folder.
// ==========================================
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Html
{
    internal static class IndexPageRenderer
    {
        // ==========================================
        // Render
        // ==========================================
        public static string Render(ManifestState manifest)
        {
            int totalGameObjects = 0;
            foreach (ManifestSceneEntry s in manifest.scenes) { totalGameObjects += s.gameObjectCount; }
            int totalFiles = 0;
            foreach (ManifestFolderEntry f in manifest.assetFolders) { totalFiles += f.fileCount; }

            var badges = new System.Collections.Generic.List<string>
            {
                HtmlPageBuilder.Badge("lav", "Unity " + manifest.unityVersion),
                HtmlPageBuilder.Badge("pink", "Unity DocSnap v" + DocSnapConstants.Version)
            };
            string lastExportHtml = HtmlPageBuilder.I18n("span", null, "Last export: ", "最終エクスポート: ", "آخرین اکسپورت: ") + HtmlPageBuilder.Escape(manifest.lastUpdatedUtc);
            string header = HtmlPageBuilder.RenderPageHeader("\uD83C\uDF70", manifest.projectName, lastExportHtml, badges, true);

            var sb = new StringBuilder(2048);

            sb.Append("<div class=\"ds-stat-grid\">");
            sb.Append(StatTile(manifest.scenes.Count, "Scenes exported", "エクスポート済みシーン", "سین اکسپورت‌شده"));
            sb.Append(StatTile(totalGameObjects, "GameObjects", "GameObject数", "GameObject ها"));
            sb.Append(StatTile(manifest.assetFolders.Count, "Asset folders", "アセットフォルダ", "پوشه‌ی فایل‌ها"));
            sb.Append(StatTile(totalFiles, "Files tracked", "追跡ファイル数", "فایل‌های ردیابی‌شده"));
            if (manifest.packages != null && manifest.packages.Count > 0)
            {
                sb.Append(StatTile(manifest.packages.Count, "Packages", "パッケージ", "پکیج‌ها"));
            }
            sb.Append("</div>\n");

            if (manifest.packages != null && manifest.packages.Count > 0)
            {
                sb.Append("<a class=\"ds-folder-row\" style=\"margin-bottom:18px;\" href=\"").Append(DocSnapConstants.PackagesFileName).Append("\">");
                sb.Append("<span class=\"ds-folder-path\">📦 ").Append(HtmlPageBuilder.I18n("span", null, "Packages used in this project", "このプロジェクトで使用中のパッケージ", "پکیج‌های استفاده‌شده در این پروژه")).Append("</span>");
                sb.Append("<span class=\"ds-folder-meta\">").Append(manifest.packages.Count).Append("</span></a>\n");
            }

            sb.Append("<div class=\"ds-card\">").Append(HtmlPageBuilder.I18n("h3", null, "Scenes", "シーン", "سین‌ها")).Append("<ul class=\"ds-folder-list\">\n");
            if (manifest.scenes.Count == 0)
            {
                sb.Append("<p class=\"ds-empty-note\">").Append(HtmlPageBuilder.I18n("span", null,
                    "No scenes exported yet - use Unity DocSnap > Export Scene in the Unity menu bar.",
                    "エクスポート済みシーンはまだありません。Unityメニューバーの Unity DocSnap > Export Scene から実行してください。",
                    "هنوز هیچ سینی اکسپورت نشده — از نوار منوی یونیتی، مسیر Unity DocSnap > Export Scene رو اجرا کن.")).Append("</p>");
            }
            foreach (ManifestSceneEntry s in manifest.scenes)
            {
                sb.Append("<li><a class=\"ds-folder-row\" href=\"").Append(s.htmlFile).Append("\"><span class=\"ds-folder-path\">")
                  .Append(HtmlPageBuilder.Escape(s.sceneName)).Append("</span><span class=\"ds-folder-meta\">")
                  .Append(s.gameObjectCount).Append(" ").Append(HtmlPageBuilder.I18n("span", null, "GameObjects", "GameObject", "GameObject")).Append("</span></a></li>\n");
            }
            sb.Append("</ul></div>\n");

            sb.Append("<div class=\"ds-card\">").Append(HtmlPageBuilder.I18n("h3", null, "Assets", "アセット", "فایل‌ها")).Append("<ul class=\"ds-folder-list\">\n");
            if (manifest.assetFolders.Count == 0)
            {
                sb.Append("<p class=\"ds-empty-note\">").Append(HtmlPageBuilder.I18n("span", null,
                    "No asset folders exported yet - use Unity DocSnap > Export Asset Info in the Unity menu bar.",
                    "エクスポート済みのアセットフォルダはまだありません。Unityメニューバーの Unity DocSnap > Export Asset Info から実行してください。",
                    "هنوز هیچ پوشه‌ی فایلی اکسپورت نشده — از نوار منوی یونیتی، مسیر Unity DocSnap > Export Asset Info رو اجرا کن.")).Append("</p>");
            }
            foreach (ManifestFolderEntry f in manifest.assetFolders)
            {
                sb.Append("<li><a class=\"ds-folder-row\" href=\"").Append(f.htmlFile).Append("\"><span class=\"ds-folder-path\">")
                  .Append(HtmlPageBuilder.Escape(f.folderPath)).Append("</span><span class=\"ds-folder-meta\">")
                  .Append(f.fileCount).Append(" ").Append(HtmlPageBuilder.I18n("span", null, "files", "ファイル", "فایل")).Append("</span></a></li>\n");
            }
            sb.Append("</ul></div>\n");

            return HtmlPageBuilder.RenderPage(manifest, DocSnapConstants.IndexFileName, manifest.projectName, header, sb.ToString());
        }

        private static string StatTile(int num, string labelEn, string labelJa, string labelFa)
        {
            return "<div class=\"ds-stat-tile\"><div class=\"ds-stat-num\">" + num + "</div><div class=\"ds-stat-label\">" + HtmlPageBuilder.I18n("span", null, labelEn, labelJa, labelFa) + "</div></div>";
        }
    }
}
