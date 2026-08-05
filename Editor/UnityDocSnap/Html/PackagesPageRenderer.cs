// ==========================================
// PackagesPageRenderer
// Builds packages.html: the "Packages used in
// this project" page.
//
// Every package the project depends on, in one
// list you can narrow:
//   • Third-party — Asset Store, Git, local or
//     embedded packages;
//   • Unity — Unity's own registry packages;
//   • Built-in — the engine modules, always
//     present and rendered compactly.
// Every card carries a version, its source, an
// access link, and - when Unity reports one - a
// clear "update available" badge.
//
// The counts at the top are CONTROLS, not
// decoration. The page used to state "2 third-party
// / 19 Unity / 47 built-in / 4 updates available"
// and then print all sixty-eight in fixed sections
// in alphabetical order, so the reader who came to
// this page because of the number 4 - which is the
// only one of the four anybody comes here about -
// had to scan sixty-eight cards looking for a badge.
// Clicking the tile now narrows the list to exactly
// those four, and the dashboard's "Updatable
// packages" tile links straight into that state.
// ==========================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Export;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Html
{
    internal static class PackagesPageRenderer
    {
        private const string GroupThirdParty = "thirdparty";
        private const string GroupBuiltIn = "builtin";
        private const string GroupUnity = "unity";

        // ==========================================
        // Render
        // ==========================================
        public static string Render(ManifestState manifest)
        {
            var ordered = new List<ManifestPackageEntry>(manifest.packages);
            int thirdParty = 0, unity = 0, builtin = 0, updates = 0;
            foreach (ManifestPackageEntry p in ordered)
            {
                if (p.updateAvailable) { updates++; }
                switch (Group(p))
                {
                    case GroupThirdParty: thirdParty++; break;
                    case GroupBuiltIn: builtin++; break;
                    default: unity++; break;
                }
            }

            // Third-party first, then Unity's, then the engine modules,
            // and alphabetical inside each. That is the order of
            // decreasing "did I choose this?", which is the order a
            // reader scans in - and the client-side sort control can
            // re-cut it by update state or by name without a reload.
            ordered.Sort(CompareDefault);

            var badges = new List<string>
            {
                HtmlPageBuilder.Badge("lav", manifest.packages.Count + " packages")
            };
            if (updates > 0) { badges.Add(HtmlPageBuilder.Badge("warn", updates + " update" + (updates == 1 ? "" : "s"))); }

            string subHtml = HtmlPageBuilder.I18n("span", null, "Last scanned: ", "最終スキャン: ", "آخرین اسکن: ") + HtmlPageBuilder.Escape(manifest.packagesExportedUtc);
            string header = HtmlPageBuilder.RenderPageHeader("📦",
                HtmlPageBuilder.I18n("span", null, "Packages", "パッケージ", "پکیج‌ها"),
                subHtml, badges, true, true);

            var sb = new StringBuilder(8192);

            // The tiles ARE the group filter. Same chrome as every other
            // stat grid in the site, so nothing new has to be learned;
            // pressing one narrows the list below instead of sending the
            // reader off to find the rows by hand.
            sb.Append("<div class=\"ds-stat-grid ds-issue-tiles\">");
            sb.Append(Tile("all", manifest.packages.Count, "📦", "All packages", "すべて", "همه‌ی پکیج‌ها", null, true));
            sb.Append(Tile(GroupThirdParty, thirdParty, "🧩", "Third-party", "サードパーティ", "شخص‌ثالث", null, false));
            sb.Append(Tile(GroupUnity, unity, "🌐", "Unity packages", "Unityパッケージ", "پکیج‌های یونیتی", null, false));
            sb.Append(Tile(GroupBuiltIn, builtin, "⚙️", "Built-in modules", "組み込みモジュール", "ماژول‌های داخلی", null, false));
            sb.Append(Tile("updates", updates, "⬆", "Updates available", "更新あり", "بروزرسانی موجود", "warn", false));
            sb.Append("</div>\n");

            sb.Append("<div class=\"ds-card\" id=\"packages\">");
            sb.Append("<div class=\"ds-card-head\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null, "Installed", "インストール済み", "نصب‌شده"));
            sb.Append("<div class=\"ds-toolbar\">");
            sb.Append(RenderSort());
            sb.Append("<input type=\"search\" class=\"ds-inline-filter\" data-pkg-search autocomplete=\"off\" spellcheck=\"false\"")
              .Append(HtmlPageBuilder.I18nAttributes("data-ph-",
                  "Filter by name, id or author…", "名前・ID・作者で絞り込み…", "فیلتر بر اساس نام، شناسه یا سازنده…"))
              .Append(" placeholder=\"").Append(HtmlPageBuilder.Escape(DocSnapText.Resolve(DocSnapRenderContext.DefaultLanguage,
                  "Filter by name, id or author…", "名前・ID・作者で絞り込み…", "فیلتر بر اساس نام، شناسه یا سازنده…")))
              .Append("\" aria-label=\"Filter packages\">");
            sb.Append("</div></div>");

            sb.Append("<div class=\"ds-pkg-grid\" data-pkg-list>");
            foreach (ManifestPackageEntry p in ordered) { sb.Append(PackageCard(p)); }
            sb.Append("</div>");

            if (manifest.packages.Count == 0)
            {
                sb.Append(EmptyNote(
                    "No packages found. Asset Store assets imported straight into Assets/ are not UPM packages and are listed on the Assets pages instead.",
                    "パッケージは見つかりませんでした。Assets/ に直接インポートされたAsset StoreアセットはUPMパッケージではないため、アセットページに表示されます。",
                    "هیچ پکیجی پیدا نشد. اسیت‌هایی که مستقیم داخل Assets/ ایمپورت شدن پکیج UPM نیستن و توی صفحات Assets نشون داده میشن."));
            }

            // Shown by app.js when the filters match nothing, so an empty
            // grid never looks like a page that failed to load.
            sb.Append("<p class=\"ds-empty-note\" data-pkg-empty hidden>")
              .Append(HtmlPageBuilder.I18n("span", null,
                  "No packages match this filter.", "この絞り込みに一致するパッケージはありません。", "هیچ پکیجی با این فیلتر مطابقت نداره."))
              .Append("</p>");

            sb.Append("</div>\n");

            return HtmlPageBuilder.RenderPage(manifest, DocSnapConstants.PackagesFileName, "Packages", header, sb.ToString());
        }

        // ==========================================
        // The default order: third-party, then Unity's own,
        // then the engine modules, alphabetical inside each.
        // ==========================================
        private static int CompareDefault(ManifestPackageEntry a, ManifestPackageEntry b)
        {
            int byGroup = GroupRank(a).CompareTo(GroupRank(b));
            if (byGroup != 0) { return byGroup; }
            return string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase);
        }

        private static int GroupRank(ManifestPackageEntry p)
        {
            switch (Group(p))
            {
                case GroupThirdParty: return 0;
                case GroupBuiltIn: return 2;
                default: return 1;
            }
        }

        private static string Group(ManifestPackageEntry p)
        {
            if (string.Equals(p.category, GroupThirdParty, StringComparison.Ordinal)) { return GroupThirdParty; }
            if (string.Equals(p.category, GroupBuiltIn, StringComparison.Ordinal)) { return GroupBuiltIn; }
            return GroupUnity;
        }

        // ==========================================
        // RenderSort
        // Three cuts of the same list. "Updates first" is the
        // one that matters and is the reason this control
        // exists: a reader who wants to know what is out of
        // date should not have to read what is not.
        // ==========================================
        private static string RenderSort()
        {
            var sb = new StringBuilder(512);
            sb.Append("<select class=\"ds-inline-select\" data-pkg-sort aria-label=\"Sort packages\">");
            sb.Append(SortOption("group", "Grouped", "グループ順", "گروه‌بندی‌شده"));
            sb.Append(SortOption("updates", "Updates first", "更新が先", "اول قابل‌بروزرسانی‌ها"));
            sb.Append(SortOption("name", "Name A→Z", "名前順", "بر اساس نام"));
            sb.Append("</select>");
            return sb.ToString();
        }

        private static string SortOption(string value, string en, string ja, string fa)
        {
            // <option> carries the same data-<lang> attributes every other
            // label does, so app.js's language pass swaps it with the rest
            // of the page rather than leaving one control in English.
            return "<option value=\"" + value + "\""
                + HtmlPageBuilder.I18nAttributes("data-", en, ja, fa) + ">"
                + HtmlPageBuilder.Escape(DocSnapText.Resolve(DocSnapRenderContext.DefaultLanguage, en, ja, fa))
                + "</option>";
        }

        private static string Tile(string filter, int count, string icon, string en, string ja, string fa, string variant, bool active)
        {
            string cls = "ds-stat-tile ds-issue-tile" + (string.IsNullOrEmpty(variant) ? "" : " ds-tile-" + variant) + (active ? " is-active" : "");
            return "<button type=\"button\" class=\"" + cls + "\" data-pkg-filter=\"" + filter + "\" aria-pressed=\"" + (active ? "true" : "false") + "\">"
                + "<div class=\"ds-stat-num\">" + count.ToString(CultureInfo.InvariantCulture) + "</div>"
                + "<div class=\"ds-stat-label\">" + icon + " " + HtmlPageBuilder.I18n("span", null, en, ja, fa) + "</div>"
                + "</button>";
        }

        // ==========================================
        // PackageCard
        // One package: name, id, version + source
        // badges, an update-available badge when Unity
        // reports a newer version, author, description,
        // and an access link.
        //
        // A built-in engine module gets the compact
        // variant. There are typically fifty of them, none
        // of them was installed by a decision anybody made,
        // and a full card each buries the two that were.
        // ==========================================
        private static string PackageCard(ManifestPackageEntry p)
        {
            string group = Group(p);
            bool compact = group == GroupBuiltIn;

            // One lowercase haystack per card so the client-side filter
            // does a single substring test instead of reading four
            // separate DOM nodes on every keystroke.
            string haystack = ((p.displayName ?? "") + " " + (p.name ?? "") + " " + (p.author ?? "") + " " + (p.version ?? "")).ToLowerInvariant();

            var sb = new StringBuilder(512);
            sb.Append("<div class=\"ds-pkg-card").Append(compact ? " ds-pkg-compact" : "")
              .Append("\" data-pkg-group=\"").Append(group)
              .Append("\" data-pkg-update=\"").Append(p.updateAvailable ? "1" : "0")
              .Append("\" data-pkg-name=\"").Append(HtmlPageBuilder.Escape((p.displayName ?? "").ToLowerInvariant()))
              .Append("\" data-pkg-text=\"").Append(HtmlPageBuilder.Escape(haystack)).Append("\">");

            sb.Append("<div class=\"ds-pkg-head\"><h4>").Append(Emoji(group)).Append(" ").Append(HtmlPageBuilder.Escape(p.displayName)).Append("</h4>");
            sb.Append("<div class=\"ds-badge-row\">");
            if (!string.IsNullOrEmpty(p.version)) { sb.Append(HtmlPageBuilder.Badge("lav", "v" + p.version)); }
            if (!compact && !string.IsNullOrEmpty(p.source)) { sb.Append(HtmlPageBuilder.Badge("ghost", p.source)); }
            if (p.updateAvailable)
            {
                string to = string.IsNullOrEmpty(p.latestVersion) ? "" : " → " + p.latestVersion;
                sb.Append(HtmlPageBuilder.BadgeRaw("warn", "⬆ " + HtmlPageBuilder.I18n("span", null, "Update", "更新", "بروزرسانی") + HtmlPageBuilder.Escape(to)));
            }
            sb.Append("</div></div>");

            sb.Append("<div class=\"ds-pkg-body\">");
            sb.Append("<div class=\"ds-pkg-id mono\">").Append(HtmlPageBuilder.Escape(p.name)).Append("</div>");
            if (!compact && !string.IsNullOrEmpty(p.author))
            {
                sb.Append("<div class=\"ds-pkg-author\">").Append(HtmlPageBuilder.I18n("span", null, "by ", "作者: ", "توسط ")).Append(HtmlPageBuilder.Escape(p.author)).Append("</div>");
            }
            if (!compact && !string.IsNullOrEmpty(p.description))
            {
                sb.Append("<p class=\"ds-pkg-desc\">").Append(HtmlPageBuilder.Escape(Shorten(p.description, 240))).Append("</p>");
            }
            if (!string.IsNullOrEmpty(p.url))
            {
                sb.Append("<div class=\"ds-file-actions\"><a class=\"ds-file-link\" href=\"").Append(HtmlPageBuilder.Escape(p.url))
                  .Append("\" target=\"_blank\" rel=\"noopener\">🔗 ")
                  .Append(HtmlPageBuilder.I18n("span", null, "Open", "開く", "باز کردن")).Append("</a></div>");
            }
            sb.Append("</div></div>");
            return sb.ToString();
        }

        private static string Emoji(string group)
        {
            switch (group)
            {
                case GroupThirdParty: return "🧩"; // puzzle piece
                case GroupBuiltIn: return "⚙️";     // gear
                default: return "🌐";               // globe (Unity registry)
            }
        }

        private static string EmptyNote(string en, string ja, string fa)
        {
            return "<p class=\"ds-empty-note\">" + HtmlPageBuilder.I18n("span", null, en, ja, fa) + "</p>";
        }

        private static string Shorten(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) { return ""; }
            string oneLine = s.Replace('\n', ' ').Replace('\r', ' ');
            return oneLine.Length <= max ? oneLine : oneLine.Substring(0, max) + "…";
        }
    }
}
