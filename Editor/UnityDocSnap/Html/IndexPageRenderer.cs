// ==========================================
// IndexPageRenderer
// Builds index.html: the site's front door -
// quick stats plus a live list of every
// exported Scene and Asset folder.
// ==========================================
using System;
using System.Collections.Generic;
using System.Globalization;
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
            // The human stamp, not the machine one. This line used to
            // print the raw UTC ISO string - the least readable of the
            // three renderings of one instant that shared this screen -
            // directly under the project's name.
            string when = exportInfo != null && !string.IsNullOrEmpty(exportInfo.exportedLocal)
                ? exportInfo.exportedLocal + " · " + exportInfo.timeZone
                : manifest.lastUpdatedUtc;
            string lastExportHtml = HtmlPageBuilder.I18n("span", null, "Last export: ", "最終エクスポート: ", "آخرین اکسپورت: ") + HtmlPageBuilder.Escape(when);
            string header = HtmlPageBuilder.RenderPageHeader("\uD83C\uDF70", manifest.projectName, lastExportHtml, badges, true);

            var sb = new StringBuilder(2048);

            if (exportInfo != null) { sb.Append(DocSnapExportInfo.RenderCard(exportInfo)); }

            // ONE stat grid for the whole dashboard. The Export Info
            // card above deliberately carries no counts of its own -
            // previously it showed "Scenes / Assets / Packages /
            // Updatable" and this grid repeated "Scenes exported /
            // Files tracked / Packages" right below it with the same
            // numbers under different labels.
            //
            // A tile with somewhere to go is a link. "Packages" and
            // "Updatable packages" sat here as dead numbers while a row
            // below said "Packages used in this project — 68" and was
            // the clickable one: the same fact twice, and the copy a
            // reader's eye lands on first was the one that did nothing.
            sb.Append("<div class=\"ds-stat-grid\">");
            sb.Append(StatTile(manifest.scenes.Count, "Scenes", "シーン", "سین‌ها", "pink"));
            sb.Append(StatTile(totalGameObjects, "GameObjects", "GameObject数", "GameObject ها", "lav"));
            sb.Append(StatTile(totalFiles, "Asset files", "アセットファイル", "فایل‌های Assets", "mint"));
            if (manifest.packages != null && manifest.packages.Count > 0)
            {
                sb.Append(StatLink(DocSnapConstants.PackagesFileName,
                    manifest.packages.Count, "Packages", "パッケージ", "پکیج‌ها", "pink"));
            }
            if (exportInfo != null && exportInfo.packagesUpdatable > 0)
            {
                sb.Append(StatLink(DocSnapConstants.PackagesFileName + "?group=updates",
                    exportInfo.packagesUpdatable, "Updatable packages", "更新可能パッケージ", "پکیج‌های قابل‌آپدیت", "warn"));
            }
            sb.Append("</div>\n");

            sb.Append(RenderHealthCard(manifest));

            // Everything on this site that is not a Scene page or a
            // folder page, in one card. These were three loose
            // full-width rows floating between cards, all built from
            // ds-folder-row - the control the Scenes and Assets lists
            // use for their CONTENTS - so the dashboard read as though
            // "Packages" and "Plan" were two asset folders that had
            // escaped their card.
            sb.Append(RenderExploreCard(manifest));
            sb.Append(RenderExcludeNote(manifest));

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

            sb.Append(RenderDirectTmpCard());

            return HtmlPageBuilder.RenderPage(manifest, DocSnapConstants.IndexFileName, manifest.projectName, header, sb.ToString());
        }

        // ==========================================
        // RenderExploreCard
        // Packages and Plan, in one card.
        //
        // Both used to be loose full-width <a class="ds-folder-row">
        // elements sitting between cards, which is the control the
        // Scenes and Assets lists use for the items INSIDE them. Two
        // rows in that shape, outside any card, read as two more
        // asset folders that had come untucked - and gave the
        // dashboard two competing column rhythms on one screen.
        //
        // The Plan row is always here, even on Pro. The first
        // question somebody asks of an export they did not make is
        // "is this all of it?", and the answer is a property of the
        // plan; the row states the edition without being clicked,
        // because the reader who most needs it is the one who does
        // not yet know there is a page to look for.
        // ==========================================
        private static string RenderExploreCard(ManifestState manifest)
        {
            var sb = new StringBuilder(768);
            sb.Append("<div class=\"ds-card\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null, "More in this export", "このエクスポートの他のページ", "بقیه‌ی این خروجی"));
            sb.Append("<ul class=\"ds-folder-list\">\n");

            // No health row: the card directly above this one is the
            // health summary and already carries its own link. A page
            // that lists a destination twice on one screen is the
            // habit this card was made to break.
            if (manifest.packages != null && manifest.packages.Count > 0)
            {
                sb.Append(ExploreRow(DocSnapConstants.PackagesFileName, "📦",
                    "Packages used in this project", "このプロジェクトで使用中のパッケージ", "پکیج‌های استفاده‌شده در این پروژه",
                    manifest.packages.Count.ToString(CultureInfo.InvariantCulture)));
            }

            sb.Append(ExploreRow(DocSnapConstants.PlanFileName, "🎟️",
                "Plan — what this export does and does not include",
                "プラン — このエクスポートに含まれるもの・含まれないもの",
                "پلن — چه چیزی توی این خروجی هست و چه چیزی نیست",
                HtmlPageBuilder.Escape(Licensing.DocSnapEditionMatrix.DisplayName(Licensing.DocSnapLicense.Edition))));

            sb.Append("</ul></div>\n");
            return sb.ToString();
        }

        private static string ExploreRow(string href, string emoji, string en, string ja, string fa, string metaHtml)
        {
            return "<li><a class=\"ds-folder-row\" href=\"" + HtmlPageBuilder.Href(href) + "\">"
                + "<span class=\"ds-folder-path\">" + emoji + " " + HtmlPageBuilder.I18n("span", null, en, ja, fa) + "</span>"
                + "<span class=\"ds-folder-meta\">" + metaHtml + "</span></a></li>\n";
        }

        // ==========================================
        // RenderDirectTmpCard
        // Unity DirectTMP, at the bottom of the dashboard.
        //
        // It earns its place here rather than being an
        // advertisement bolted on: an export of a Persian or Arabic
        // project is read BY people who work on that project, in
        // Unity, where TextMeshPro draws their language as a row of
        // disconnected letters running backwards. The tool that
        // fixes that is the one thing this page can usefully tell
        // them that is not in their project.
        //
        // What it says depends on the language, deliberately. A
        // Persian reader is being told about a problem they have -
        // so the problem is named. An English or Japanese reader is
        // being told a package exists - so it is one line and a
        // link, and nothing about a bug they have never hit. Both
        // texts ride the same data-<lang> attributes as every other
        // string on the site, so switching language switches this
        // too.
        // ==========================================
        private static string RenderDirectTmpCard()
        {
            var sb = new StringBuilder(768);
            sb.Append("<div class=\"ds-card ds-directtmp\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null,
                "✍️ Persian, Arabic and Japanese text in Unity",
                "✍️ Unity での日本語・ペルシャ語・アラビア語テキスト",
                "✍️ متن فارسی و عربی توی یونیتی"));

            sb.Append("<p class=\"ds-empty-note\">").Append(HtmlPageBuilder.I18n("span", null,
                "Unity DirectTMP hands TextMeshPro the font file itself, so every glyph a font contains is available with no atlas to rebuild — and it joins Arabic-script letters and puts right-to-left words in reading order, in your game and in the Unity Editor. Free and MIT licensed.",
                "Unity DirectTMP は TextMeshPro にフォントファイルそのものを渡す仕組みです。アトラスを作り直さずにフォント内の全グリフが使え、アラビア文字の連結と右から左への語順もゲーム内と Unity エディタの両方で処理します。無料・MIT ライセンス。",
                "‏Unity DirectTMP خودِ فایل فونت رو به TextMeshPro می‌ده، پس همه‌ی گلیف‌های فونت بدون ساختن اطلس در دسترسه — و حروف فارسی/عربی رو به هم می‌چسبونه و کلمه‌ها رو راست‌به‌چپ مرتب می‌کنه، هم توی بازی و هم توی خود ادیتور یونیتی. رایگان و با لایسنس MIT."))
              .Append("</p>");

            // Only the Persian copy names the problem, because only the
            // Persian reader has it. It is its own element with an empty
            // English and Japanese text rather than a longer fa string
            // in the paragraph above, so those two versions read as a
            // finished sentence instead of one with a hole in it. CSS
            // drops the element entirely when its text is empty, so no
            // blank gap is left behind.
            sb.Append(HtmlPageBuilder.I18n("p", "ds-empty-note ds-lang-only", "", "",
                "اگر متن‌های فارسیِ همین صفحه یا پنجره‌ی خروجی‌گیری Unity DocSnap توی ادیتور شکسته و جدا‌جدا دیده می‌شن، دلیلش همینه:"
                + " نه یونیتی و نه TextMeshPro حروف عربی رو به هم وصل نمی‌کنن. با نصب Unity DirectTMP توی پروژه این مشکل حل می‌شه"
                + " — پنجره‌های خود Unity DocSnap هم فارسی رو درست نشون می‌دن."));

            sb.Append("<div class=\"ds-file-actions\"><a class=\"ds-file-link\" href=\"")
              .Append(DocSnapConstants.DirectTmpUrl).Append("\" target=\"_blank\" rel=\"noopener\">⬇ ")
              .Append(HtmlPageBuilder.I18n("span", null,
                  "Get Unity DirectTMP", "Unity DirectTMP を入手", "دانلود Unity DirectTMP"))
              .Append("</a></div>");

            sb.Append("</div>\n");
            return sb.ToString();
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
            sb.Append("<div class=\"ds-card\"><div class=\"ds-card-head\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null, "🩺 Project health", "🩺 プロジェクトの健康状態", "🩺 سلامت پروژه"));
            sb.Append("<a class=\"ds-card-action\" href=\"").Append(DocSnapConstants.IssuesFileName).Append("\">")
              .Append(HtmlPageBuilder.I18n("span", null, "Open health report →", "健康レポートを開く →", "باز کردن گزارش سلامت →"))
              .Append("</a></div>");

            if (totals.IsClean)
            {
                sb.Append("<p class=\"ds-empty-note\">").Append(HtmlPageBuilder.I18n("span", null,
                    "No missing scripts, no broken references, no duplicate Scene names.",
                    "壊れたスクリプトも、切れた参照も、重複したシーン名もありません。",
                    "نه اسکریپت گم‌شده‌ای هست، نه ارجاع شکسته‌ای، نه اسم سین تکراری.")).Append("</p>");
                sb.Append("</div>\n");
                return sb.ToString();
            }

            // A project whose only findings are in Unity's own installed
            // folders reads as clean HERE, because that is the truth the
            // author cares about - with the rest named right below rather
            // than hidden. Leading with "8 broken references" when none of
            // the eight is theirs to fix is how the card lost its reader.
            if (totals.MineIsClean)
            {
                sb.Append("<p class=\"ds-empty-note\">").Append(HtmlPageBuilder.I18n("span", null,
                    "Nothing in your own files. ",
                    "自分のファイルには問題ありません。",
                    "توی فایل‌های خودت هیچ ایرادی نیست. "));
                sb.Append(HtmlPageBuilder.I18n("span", null,
                    totals.VendorFindings + " finding(s) belong to Unity or an installed package — a folder one of them wrote into Assets/, or a file Unity generates and maintains itself.",
                    totals.VendorFindings + " 件は Unity かインストール済みパッケージのもので、Assets/ 配下に書き込まれたフォルダか、Unity 自身が生成・管理するファイルにあります。",
                    "‏" + totals.VendorFindings + " مورد مال Unity یا پکیج‌های نصب‌شده‌ست — یا داخل پوشه‌ای که خودشون توی Assets/ ساختن، یا فایلی که Unity خودش می‌سازه و نگه می‌داره."));
                sb.Append("</p>");
                sb.Append(RenderDuplicateNote(totals));
                sb.Append("</div>\n");
                return sb.ToString();
            }

            // Every badge is a link into the health report, pre-filtered
            // to its own kind. A count that cannot be clicked is a
            // number the reader then has to go and find by hand, which
            // is most of the work this card was meant to remove.
            sb.Append("<div class=\"ds-badge-row\">");
            if (totals.missingScriptsMine > 0)
            {
                sb.Append(HealthBadge(DocSnapHealthReport.KindMissingScript, "warn", totals.missingScriptsMine,
                    "missing scripts", "欠落スクリプト", "اسکریپت گم‌شده"));
            }
            if (totals.missingReferencesMine > 0)
            {
                sb.Append(HealthBadge(DocSnapHealthReport.KindMissingReference, "warn", totals.missingReferencesMine,
                    "broken references", "切れた参照", "ارجاع شکسته"));
            }
            if (totals.unresolvedAssetsMine > 0)
            {
                sb.Append(HealthBadge(DocSnapHealthReport.KindUnresolvedAsset, "lav", totals.unresolvedAssetsMine,
                    "unresolved assets", "型不明アセット", "فایل بدون نوع مشخص"));
            }
            if (totals.VendorFindings > 0)
            {
                sb.Append("<a class=\"ds-badge ghost\" href=\"").Append(DocSnapConstants.IssuesFileName)
                  .Append("?owner=vendor#findings\">").Append(totals.VendorFindings).Append(" ")
                  .Append(HtmlPageBuilder.I18n("span", null,
                      "in Unity / packages", "Unity・パッケージ内", "در Unity و پکیج‌ها")).Append("</a>");
            }
            if (totals.duplicateSceneNames.Count > 0)
            {
                sb.Append(HealthBadge("duplicateScene", null, totals.duplicateSceneNames.Count,
                    "duplicate scene names", "重複シーン名", "اسم سین تکراری"));
            }
            sb.Append("</div>");

            List<ManifestHealthEntry> worst = DocSnapHealthReport.WorstMine(manifest, 6);
            if (worst.Count > 0)
            {
                sb.Append("<ul class=\"ds-folder-list\">\n");
                foreach (ManifestHealthEntry e in worst)
                {
                    // The scope row links into the health report scoped to
                    // that Scene / folder, not to the Scene page itself:
                    // "8 broken references in Assets" used to open the
                    // Assets page and leave the reader to find all eight.
                    string href = DocSnapConstants.IssuesFileName + "?owner=mine&scope=" + Uri.EscapeDataString(e.label ?? "") + "#findings";
                    sb.Append("<li><a class=\"ds-folder-row\" href=\"").Append(HtmlPageBuilder.Escape(href)).Append("\">")
                      .Append("<span class=\"ds-folder-path\">").Append(HtmlPageBuilder.Escape(e.label)).Append("</span>")
                      .Append("<span class=\"ds-folder-meta\">").Append(HtmlPageBuilder.Escape(DescribeMineFindings(e)))
                      .Append("</span></a></li>\n");
                }
                sb.Append("</ul>\n");
            }

            sb.Append(RenderDuplicateNote(totals));

            sb.Append("</div>\n");
            return sb.ToString();
        }

        private static string RenderDuplicateNote(HealthTotals totals)
        {
            if (totals.duplicateSceneNames == null || totals.duplicateSceneNames.Count == 0) { return ""; }
            return "<p class=\"ds-empty-note\">"
                + HtmlPageBuilder.I18n("span", null,
                    "Scene names used more than once: ",
                    "複数回使われているシーン名: ",
                    "اسم‌های سین که بیش از یک‌بار استفاده شده‌اند: ")
                + HtmlPageBuilder.Escape(string.Join(", ", totals.duplicateSceneNames.ToArray()))
                + "</p>";
        }

        private static string HealthBadge(string kind, string variant, int count, string en, string ja, string fa)
        {
            string cls = string.IsNullOrEmpty(variant) ? "ds-badge" : "ds-badge " + variant;
            return "<a class=\"" + cls + "\" href=\"" + DocSnapConstants.IssuesFileName + "?kind=" + kind + "#findings\">"
                + count + " " + HtmlPageBuilder.I18n("span", null, en, ja, fa) + "</a>";
        }

        // Plain, language-neutral shorthand so one row stays
        // readable in every one of the three languages without
        // three separate spans inside a link.
        private static string DescribeMineFindings(ManifestHealthEntry e)
        {
            var parts = new List<string>();
            if (e.missingScriptsMine > 0) { parts.Add(e.missingScriptsMine + " ⚠ script"); }
            if (e.missingReferencesMine > 0) { parts.Add(e.missingReferencesMine + " 🔗 ref"); }
            if (e.unresolvedAssetsMine > 0) { parts.Add(e.unresolvedAssetsMine + " ? type"); }
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
            return "<div class=\"" + TileClass(variant) + "\">" + TileBody(num, labelEn, labelJa, labelFa) + "</div>";
        }

        // The same tile with somewhere to go. Same chrome on purpose:
        // a reader should not have to learn which of two identical
        // boxes is clickable, so the ones that are get a hover
        // affordance from .ds-stat-link rather than a different shape.
        private static string StatLink(string href, int num, string labelEn, string labelJa, string labelFa, string variant = null)
        {
            return "<a class=\"" + TileClass(variant) + " ds-stat-link\" href=\"" + HtmlPageBuilder.Escape(href) + "\">"
                + TileBody(num, labelEn, labelJa, labelFa) + "</a>";
        }

        private static string TileClass(string variant)
        {
            return string.IsNullOrEmpty(variant) ? "ds-stat-tile" : "ds-stat-tile ds-tile-" + variant;
        }

        private static string TileBody(int num, string labelEn, string labelJa, string labelFa)
        {
            return "<div class=\"ds-stat-num\">" + num.ToString(CultureInfo.InvariantCulture) + "</div>"
                + "<div class=\"ds-stat-label\">" + HtmlPageBuilder.I18n("span", null, labelEn, labelJa, labelFa) + "</div>";
        }
    }
}
