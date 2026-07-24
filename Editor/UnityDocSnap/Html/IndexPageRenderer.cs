// ==========================================
// IndexPageRenderer
// Builds index.html: the site's front door -
// quick stats plus a live list of every
// exported Scene and Asset folder.
// ==========================================
using System.Collections.Generic;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Export;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Html
{
    internal static class IndexPageRenderer
    {
        // ==========================================
        // Render
        // exportInfo (optional) surfaces this export's
        // exact timing + counts as an "Export Info" card
        // at the top of the dashboard.
        // ==========================================
        public static string Render(ManifestState manifest, VersionSnapshot exportInfo = null)
        {
            int totalGameObjects = 0;
            foreach (ManifestSceneEntry s in manifest.scenes) { totalGameObjects += s.gameObjectCount; }
            int totalFiles = 0;
            foreach (ManifestFolderEntry f in manifest.assetFolders) { totalFiles += f.fileCount; }

            var badges = new List<string>
            {
                HtmlPageBuilder.Badge("lav", "Unity " + manifest.unityVersion),
                HtmlPageBuilder.Badge("pink", "Unity DocSnap v" + DocSnapConstants.Version)
            };
            string lastExportHtml = HtmlPageBuilder.I18n("span", null, "Last export: ", "最終エクスポート: ", "آخرین اکسپورت: ") + HtmlPageBuilder.Escape(manifest.lastUpdatedUtc);
            string header = HtmlPageBuilder.RenderPageHeader("\uD83C\uDF70", manifest.projectName, lastExportHtml, badges, true);

            var sb = new StringBuilder(2048);

            if (exportInfo != null) { sb.Append(DocSnapExportInfo.RenderCard(exportInfo)); }

            // ONE stat grid for the whole dashboard. The Export Info
            // card above deliberately carries no counts of its own -
            // previously it showed "Scenes / Assets / Packages /
            // Updatable" and this grid repeated "Scenes exported /
            // Files tracked / Packages" right below it with the same
            // numbers under different labels.
            sb.Append("<div class=\"ds-stat-grid\">");
            sb.Append(StatTile(manifest.scenes.Count, "Scenes", "シーン", "سین‌ها", "pink"));
            sb.Append(StatTile(totalGameObjects, "GameObjects", "GameObject数", "GameObject ها", "lav"));
            sb.Append(StatTile(totalFiles, "Asset files", "アセットファイル", "فایل‌های Assets", "mint"));
            if (manifest.packages != null && manifest.packages.Count > 0)
            {
                sb.Append(StatTile(manifest.packages.Count, "Packages", "パッケージ", "پکیج‌ها", "pink"));
            }
            if (exportInfo != null && exportInfo.packagesUpdatable > 0)
            {
                sb.Append(StatTile(exportInfo.packagesUpdatable, "Updatable packages", "更新可能パッケージ", "پکیج‌های قابل‌آپدیت", "warn"));
            }
            sb.Append("</div>\n");

            sb.Append(RenderHealthCard(manifest));
            sb.Append(RenderExcludeNote(manifest));

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
                sb.Append("<li><a class=\"ds-folder-row\" href=\"").Append(HtmlPageBuilder.Href(s.htmlFile)).Append("\"><span class=\"ds-folder-path\">")
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
                sb.Append("<li><a class=\"ds-folder-row\" href=\"").Append(HtmlPageBuilder.Href(f.htmlFile)).Append("\"><span class=\"ds-folder-path\">")
                  .Append(HtmlPageBuilder.Escape(f.folderPath)).Append("</span><span class=\"ds-folder-meta\">")
                  .Append(f.fileCount).Append(" ").Append(HtmlPageBuilder.I18n("span", null, "files", "ファイル", "فایل")).Append("</span></a></li>\n");
            }
            sb.Append("</ul></div>\n");

            return HtmlPageBuilder.RenderPage(manifest, DocSnapConstants.IndexFileName, manifest.projectName, header, sb.ToString());
        }

        // ==========================================
        // RenderHealthCard
        // The dashboard's answer to "is anything actually
        // broken in here?".
        //
        // Every export already walked past each Missing
        // Script and each object reference whose target no
        // longer exists, wrote both faithfully into the JSON,
        // rendered them somewhere thousands of rows down the
        // Scene page - and then said nothing about them. You
        // had to already know to go looking. This leads with
        // the counts and links straight to the pages holding
        // them; a clean project gets a one-line all-clear
        // instead of a wall of zeroes.
        // ==========================================
        private static string RenderHealthCard(ManifestState manifest)
        {
            HealthTotals totals = DocSnapHealthReport.Totals(manifest);

            var sb = new StringBuilder(1024);
            sb.Append("<div class=\"ds-card\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null, "🩺 Project health", "🩺 プロジェクトの健康状態", "🩺 سلامت پروژه"));

            if (totals.IsClean)
            {
                sb.Append("<p class=\"ds-empty-note\">").Append(HtmlPageBuilder.I18n("span", null,
                    "No missing scripts, no broken references, no duplicate Scene names.",
                    "壊れたスクリプトも、切れた参照も、重複したシーン名もありません。",
                    "نه اسکریپت گم‌شده‌ای هست، نه ارجاع شکسته‌ای، نه اسم سین تکراری.")).Append("</p>");
                sb.Append("</div>\n");
                return sb.ToString();
            }

            sb.Append("<div class=\"ds-badge-row\">");
            if (totals.missingScripts > 0)
            {
                sb.Append(HtmlPageBuilder.BadgeRaw("warn", totals.missingScripts + " " + HtmlPageBuilder.I18n("span", null,
                    "missing scripts", "欠落スクリプト", "اسکریپت گم‌شده")));
            }
            if (totals.missingReferences > 0)
            {
                sb.Append(HtmlPageBuilder.BadgeRaw("warn", totals.missingReferences + " " + HtmlPageBuilder.I18n("span", null,
                    "broken references", "切れた参照", "ارجاع شکسته")));
            }
            if (totals.unresolvedAssets > 0)
            {
                sb.Append(HtmlPageBuilder.BadgeRaw(null, totals.unresolvedAssets + " " + HtmlPageBuilder.I18n("span", null,
                    "unresolved assets", "型不明アセット", "فایل بدون نوع مشخص")));
            }
            if (totals.duplicateSceneNames.Count > 0)
            {
                sb.Append(HtmlPageBuilder.BadgeRaw(null, totals.duplicateSceneNames.Count + " " + HtmlPageBuilder.I18n("span", null,
                    "duplicate scene names", "重複シーン名", "اسم سین تکراری")));
            }
            sb.Append("</div>");

            List<ManifestHealthEntry> worst = DocSnapHealthReport.Worst(manifest, 8);
            if (worst.Count > 0)
            {
                sb.Append("<ul class=\"ds-folder-list\">\n");
                foreach (ManifestHealthEntry e in worst)
                {
                    sb.Append("<li><a class=\"ds-folder-row\" href=\"").Append(HtmlPageBuilder.Href(e.htmlFile)).Append("\">")
                      .Append("<span class=\"ds-folder-path\">").Append(HtmlPageBuilder.Escape(e.label)).Append("</span>")
                      .Append("<span class=\"ds-folder-meta\">").Append(HtmlPageBuilder.Escape(DescribeFindings(e)))
                      .Append("</span></a></li>\n");
                }
                sb.Append("</ul>\n");
            }

            if (totals.duplicateSceneNames.Count > 0)
            {
                sb.Append("<p class=\"ds-empty-note\">")
                  .Append(HtmlPageBuilder.I18n("span", null,
                      "Scene names used more than once: ",
                      "複数回使われているシーン名: ",
                      "اسم‌های سین که بیش از یک‌بار استفاده شده‌اند: "))
                  .Append(HtmlPageBuilder.Escape(string.Join(", ", totals.duplicateSceneNames.ToArray())))
                  .Append("</p>");
            }

            sb.Append("</div>\n");
            return sb.ToString();
        }

        // Plain, language-neutral shorthand so one row stays
        // readable in every one of the three languages without
        // three separate spans inside a link.
        private static string DescribeFindings(ManifestHealthEntry e)
        {
            var parts = new List<string>();
            if (e.missingScripts > 0) { parts.Add(e.missingScripts + " ⚠ script"); }
            if (e.missingReferences > 0) { parts.Add(e.missingReferences + " 🔗 ref"); }
            if (e.unresolvedAssets > 0) { parts.Add(e.unresolvedAssets + " ? type"); }
            return string.Join(" · ", parts.ToArray());
        }

        // ==========================================
        // RenderExcludeNote
        // States what the export deliberately left out.
        // A file count that silently disagrees with the
        // Project window is a documentation tool quietly
        // lying; naming the rules that caused it is the
        // whole point of having them.
        // ==========================================
        private static string RenderExcludeNote(ManifestState manifest)
        {
            if (manifest.excludePatterns == null || manifest.excludePatterns.Count == 0) { return ""; }

            var sb = new StringBuilder(256);
            sb.Append("<div class=\"ds-card\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null, "🚫 Excluded from this export", "🚫 このエクスポートの除外対象", "🚫 موارد حذف‌شده از این خروجی"));
            sb.Append("<div class=\"ds-badge-row\">");
            foreach (string pattern in manifest.excludePatterns)
            {
                sb.Append(HtmlPageBuilder.Badge("ghost", pattern));
            }
            sb.Append("</div></div>\n");
            return sb.ToString();
        }

        private static string StatTile(int num, string labelEn, string labelJa, string labelFa, string variant = null)
        {
            string cls = string.IsNullOrEmpty(variant) ? "ds-stat-tile" : "ds-stat-tile ds-tile-" + variant;
            return "<div class=\"" + cls + "\"><div class=\"ds-stat-num\">" + num + "</div><div class=\"ds-stat-label\">" + HtmlPageBuilder.I18n("span", null, labelEn, labelJa, labelFa) + "</div></div>";
        }
    }
}
