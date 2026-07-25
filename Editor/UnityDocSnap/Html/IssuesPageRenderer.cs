// ==========================================
// IssuesPageRenderer
// Builds issues.html: every individual thing the
// export found wrong, each one a link that lands
// on the card holding it.
//
// The dashboard used to report "8 broken
// references" and link to the Assets page. On a
// real project that page is thousands of rows
// long, so the reader learned a number and then
// had to go hunting anyway - which is most of the
// work the tool was supposed to save. Each finding
// now carries the GameObject path or asset path it
// sits on, the component and field that hold it,
// and the anchor of the card that renders it.
// ==========================================
using System.Collections.Generic;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Export;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Html
{
    internal static class IssuesPageRenderer
    {
        // ==========================================
        // Render
        // ==========================================
        public static string Render(ManifestState manifest)
        {
            HealthTotals totals = DocSnapHealthReport.Totals(manifest);
            List<ManifestIssueEntry> issues = DocSnapHealthReport.SortedIssues(manifest);

            var badges = new List<string>();
            if (totals.IsClean)
            {
                badges.Add(HtmlPageBuilder.BadgeRaw("mint", HtmlPageBuilder.I18n("span", null, "All clear", "問題なし", "همه‌چیز سالم")));
            }
            else
            {
                badges.Add(HtmlPageBuilder.BadgeRaw("warn", totals.TotalFindings + " " + HtmlPageBuilder.I18n("span", null, "findings", "件の指摘", "مورد")));
            }

            string header = HtmlPageBuilder.RenderPageHeader("🩺",
                Localised("Project health", "プロジェクトの健康状態", "سلامت پروژه"),
                HtmlPageBuilder.I18n("span", null,
                    "Everything this export found wrong, linked to the exact object.",
                    "このエクスポートが見つけた問題を、該当オブジェクトへのリンク付きで。",
                    "هر چیزی که این خروجی پیدا کرده، با لینک مستقیم به همون آبجکت."),
                badges, true);

            var sb = new StringBuilder(4096);

            if (totals.IsClean && issues.Count == 0)
            {
                sb.Append("<div class=\"ds-card ds-allclear\">");
                sb.Append("<div class=\"ds-allclear-mark\">✓</div>");
                sb.Append(HtmlPageBuilder.I18n("h3", null,
                    "Nothing to fix",
                    "修正すべき点はありません",
                    "چیزی برای درست کردن نیست"));
                sb.Append("<p class=\"ds-empty-note\">").Append(HtmlPageBuilder.I18n("span", null,
                    "No missing scripts, no broken references, no unresolved assets, no duplicate Scene names.",
                    "欠落スクリプトも、切れた参照も、型不明アセットも、重複シーン名もありません。",
                    "نه اسکریپت گم‌شده‌ای هست، نه ارجاع شکسته‌ای، نه فایل بدون نوع، نه اسم سین تکراری.")).Append("</p>");
                sb.Append("</div>\n");
                sb.Append(RenderDuplicateScenes(totals));
                return HtmlPageBuilder.RenderPage(manifest, DocSnapConstants.IssuesFileName,
                    Localised("Project health", "プロジェクトの健康状態", "سلامت پروژه"), header, sb.ToString());
            }

            sb.Append(RenderSummaryTiles(totals));
            sb.Append(RenderTruncationNote(manifest));
            sb.Append(RenderIssueList(issues));
            sb.Append(RenderDuplicateScenes(totals));

            return HtmlPageBuilder.RenderPage(manifest, DocSnapConstants.IssuesFileName,
                Localised("Project health", "プロジェクトの健康状態", "سلامت پروژه"), header, sb.ToString());
        }

        // ==========================================
        // RenderSummaryTiles
        // The three counts, each one a filter button
        // rather than a dead number: clicking narrows the
        // list below instead of sending the reader off to
        // another page to start again.
        // ==========================================
        private static string RenderSummaryTiles(HealthTotals totals)
        {
            var sb = new StringBuilder(1024);
            sb.Append("<div class=\"ds-stat-grid ds-issue-tiles\">");
            sb.Append(Tile("all", totals.TotalFindings, "📋", "All findings", "すべて", "همه‌ی موارد", null, true));
            sb.Append(Tile(DocSnapHealthReport.KindMissingScript, totals.missingScripts, "⚠",
                "Missing scripts", "欠落スクリプト", "اسکریپت گم‌شده", "warn", false));
            sb.Append(Tile(DocSnapHealthReport.KindMissingReference, totals.missingReferences, "🔗",
                "Broken references", "切れた参照", "ارجاع شکسته", "warn", false));
            sb.Append(Tile(DocSnapHealthReport.KindUnresolvedAsset, totals.unresolvedAssets, "❓",
                "Unresolved assets", "型不明アセット", "فایل بدون نوع", "lav", false));
            sb.Append("</div>\n");
            return sb.ToString();
        }

        private static string Tile(string kind, int count, string icon, string en, string ja, string fa, string variant, bool active)
        {
            string cls = "ds-stat-tile ds-issue-tile" + (string.IsNullOrEmpty(variant) ? "" : " ds-tile-" + variant) + (active ? " is-active" : "");
            return "<button type=\"button\" class=\"" + cls + "\" data-issue-filter=\"" + kind + "\" aria-pressed=\"" + (active ? "true" : "false") + "\">"
                + "<div class=\"ds-stat-num\">" + count + "</div>"
                + "<div class=\"ds-stat-label\">" + icon + " " + HtmlPageBuilder.I18n("span", null, en, ja, fa) + "</div></button>";
        }

        // ==========================================
        // RenderTruncationNote
        // A scope that produced more findings than the cap
        // says so. A list that silently stops short while a
        // count above it says otherwise is exactly the kind
        // of quiet disagreement this whole page exists to
        // remove.
        // ==========================================
        private static string RenderTruncationNote(ManifestState manifest)
        {
            var capped = new List<string>();
            foreach (ManifestHealthEntry h in manifest.health)
            {
                if (h.issuesTruncated) { capped.Add(h.label); }
            }
            if (capped.Count == 0) { return ""; }

            var sb = new StringBuilder(256);
            sb.Append("<div class=\"ds-callout warn\">");
            sb.Append(HtmlPageBuilder.I18n("span", null,
                "Some scopes reported more findings than this page lists (capped at "
                    + DocSnapConstants.MaxIssuesPerScope + " each). The counts above are complete; the list below is not: ",
                "一部のスコープはこのページに載る上限(各 " + DocSnapConstants.MaxIssuesPerScope
                    + " 件)を超える問題を報告しました。上の件数は完全ですが、下の一覧は一部です: ",
                "بعضی بخش‌ها بیشتر از سقف این صفحه (هرکدام " + DocSnapConstants.MaxIssuesPerScope
                    + " مورد) ایراد داشتن. شمارش‌های بالا کاملن، ولی لیست پایین کامل نیست: "));
            sb.Append("<span class=\"mono\">").Append(HtmlPageBuilder.Escape(string.Join(", ", capped.ToArray()))).Append("</span>");
            sb.Append("</div>\n");
            return sb.ToString();
        }

        // ==========================================
        // RenderIssueList
        // One row per finding. Every row is an anchor
        // straight into the page and card that holds it -
        // the whole point of the page.
        // ==========================================
        private static string RenderIssueList(List<ManifestIssueEntry> issues)
        {
            var sb = new StringBuilder(8192);
            sb.Append("<div class=\"ds-card\">");
            sb.Append("<div class=\"ds-card-head\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null, "Findings", "指摘一覧", "موارد پیداشده"));
            sb.Append("<input type=\"search\" class=\"ds-inline-filter\" data-issue-search autocomplete=\"off\" spellcheck=\"false\" ")
              .Append("data-ph-en=\"Filter by object, field or scene…\" data-ph-ja=\"オブジェクト・フィールド・シーンで絞り込み…\" data-ph-fa=\"فیلتر بر اساس آبجکت، فیلد یا سین…\" ")
              .Append("placeholder=\"").Append(DefaultFilterPlaceholder()).Append("\" aria-label=\"Filter findings\">");
            sb.Append("</div>");

            sb.Append("<ol class=\"ds-issue-list\" data-issue-list>\n");

            int rendered = 0;
            foreach (ManifestIssueEntry issue in issues)
            {
                if (rendered >= DocSnapConstants.MaxIssuesRendered) { break; }
                sb.Append(RenderRow(issue));
                rendered++;
            }

            sb.Append("</ol>\n");

            if (issues.Count > rendered)
            {
                sb.Append("<p class=\"ds-empty-note\">")
                  .Append(HtmlPageBuilder.I18n("span", null,
                      "+" + (issues.Count - rendered) + " more findings are recorded in data/manifest.json.",
                      "さらに " + (issues.Count - rendered) + " 件は data/manifest.json に記録されています。",
                      "+" + (issues.Count - rendered) + " مورد دیگه توی data/manifest.json ثبت شده."))
                  .Append("</p>");
            }

            // Shown by app.js when the text filter matches nothing, so
            // an empty list never looks like a page that failed to load.
            sb.Append("<p class=\"ds-empty-note\" data-issue-empty hidden>")
              .Append(HtmlPageBuilder.I18n("span", null, "No findings match this filter.", "この絞り込みに一致する指摘はありません。", "هیچ موردی با این فیلتر مطابقت نداره."))
              .Append("</p>");

            sb.Append("</div>\n");
            return sb.ToString();
        }

        private static string RenderRow(ManifestIssueEntry issue)
        {
            string href = string.IsNullOrEmpty(issue.anchor)
                ? HtmlPageBuilder.Href(issue.htmlFile)
                : HtmlPageBuilder.Href(issue.htmlFile, issue.anchor);

            // One lowercase haystack per row so the client-side filter
            // does a single substring test instead of reading three
            // separate DOM nodes for every keystroke.
            string haystack = ((issue.location ?? "") + " " + (issue.detail ?? "") + " " + (issue.scopeLabel ?? "")).ToLowerInvariant();

            var sb = new StringBuilder(512);
            sb.Append("<li class=\"ds-issue-row ds-issue-").Append(issue.kind).Append("\" data-issue-kind=\"")
              .Append(issue.kind).Append("\" data-issue-text=\"").Append(HtmlPageBuilder.Escape(haystack)).Append("\">");
            sb.Append("<a class=\"ds-issue-link\" href=\"").Append(href).Append("\">");

            sb.Append("<span class=\"ds-issue-icon\" aria-hidden=\"true\">").Append(KindIcon(issue.kind)).Append("</span>");

            sb.Append("<span class=\"ds-issue-main\">");
            sb.Append("<span class=\"ds-issue-where\">").Append(HtmlPageBuilder.Escape(issue.location)).Append("</span>");
            if (!string.IsNullOrEmpty(issue.detail))
            {
                sb.Append("<span class=\"ds-issue-detail\">").Append(HtmlPageBuilder.Escape(issue.detail)).Append("</span>");
            }
            sb.Append("</span>");

            sb.Append("<span class=\"ds-issue-side\">");
            sb.Append("<span class=\"ds-issue-kind\">").Append(KindLabel(issue.kind)).Append("</span>");
            sb.Append("<span class=\"ds-issue-scope\">").Append(HtmlPageBuilder.Escape(issue.scopeLabel)).Append("</span>");
            sb.Append("</span>");

            sb.Append("</a></li>\n");
            return sb.ToString();
        }

        private static string KindIcon(string kind)
        {
            if (kind == DocSnapHealthReport.KindMissingScript) { return "⚠"; }
            if (kind == DocSnapHealthReport.KindMissingReference) { return "🔗"; }
            return "❓";
        }

        private static string KindLabel(string kind)
        {
            if (kind == DocSnapHealthReport.KindMissingScript)
            {
                return HtmlPageBuilder.I18n("span", null, "Missing script", "欠落スクリプト", "اسکریپت گم‌شده");
            }
            if (kind == DocSnapHealthReport.KindMissingReference)
            {
                return HtmlPageBuilder.I18n("span", null, "Broken reference", "切れた参照", "ارجاع شکسته");
            }
            return HtmlPageBuilder.I18n("span", null, "Unresolved asset", "型不明アセット", "فایل بدون نوع");
        }

        // ==========================================
        // RenderDuplicateScenes
        // Not a per-object finding, so it gets its own
        // short section rather than a row in the list.
        // ==========================================
        private static string RenderDuplicateScenes(HealthTotals totals)
        {
            if (totals.duplicateSceneNames == null || totals.duplicateSceneNames.Count == 0) { return ""; }

            var sb = new StringBuilder(512);
            sb.Append("<div class=\"ds-card\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null, "Duplicate Scene names", "重複したシーン名", "اسم‌های تکراری سین"));
            sb.Append("<p class=\"ds-empty-note\">").Append(HtmlPageBuilder.I18n("span", null,
                "These names are used by more than one Scene. Their pages no longer overwrite each other, but every \"open the X scene\" instruction is ambiguous until one is renamed.",
                "これらの名前は複数のシーンで使われています。ページが互いを上書きすることはなくなりましたが、「X シーンを開いて」という指示はどちらか改名するまで曖昧なままです。",
                "این اسم‌ها روی بیش از یک سین استفاده شدن. صفحه‌هاشون دیگه همدیگه رو بازنویسی نمی‌کنن، ولی تا وقتی یکی‌شون تغییر اسم نده، هر دستور «سین X رو باز کن» مبهمه."))
              .Append("</p>");
            sb.Append("<div class=\"ds-badge-row\">");
            foreach (string name in totals.duplicateSceneNames)
            {
                sb.Append(HtmlPageBuilder.Badge("warn", name));
            }
            sb.Append("</div></div>\n");
            return sb.ToString();
        }

        private static string DefaultFilterPlaceholder()
        {
            string lang = DocSnapRenderContext.DefaultLanguage;
            if (lang == "ja") { return "オブジェクト・フィールド・シーンで絞り込み…"; }
            if (lang == "fa") { return "فیلتر بر اساس آبجکت، فیلد یا سین…"; }
            return "Filter by object, field or scene…";
        }

        private static string Localised(string en, string ja, string fa)
        {
            string lang = DocSnapRenderContext.DefaultLanguage;
            if (lang == "ja") { return ja; }
            if (lang == "fa") { return fa; }
            return en;
        }
    }
}
