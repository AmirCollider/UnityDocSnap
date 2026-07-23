// ==========================================
// PackagesPageRenderer
// Builds packages.html: the "Packages used in
// this project" page. Two groups, matching how a
// reader thinks about their dependencies:
//   • Installed / third-party — Asset Store, Git,
//     local or embedded packages;
//   • Unity packages — Unity's own registry
//     packages (e.g. 2D Animation), with the
//     always-present built-in engine modules in a
//     collapsed sub-section.
// Every card carries a version, its source, an
// access link, and - when Unity reports one - a
// clear "update available" badge.
// ==========================================
using System.Collections.Generic;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Html
{
    internal static class PackagesPageRenderer
    {
        // ==========================================
        // Render
        // ==========================================
        public static string Render(ManifestState manifest)
        {
            var thirdParty = new List<ManifestPackageEntry>();
            var unity = new List<ManifestPackageEntry>();
            var builtin = new List<ManifestPackageEntry>();
            int updates = 0;
            foreach (ManifestPackageEntry p in manifest.packages)
            {
                if (p.updateAvailable) { updates++; }
                switch (p.category)
                {
                    case "thirdparty": thirdParty.Add(p); break;
                    case "builtin": builtin.Add(p); break;
                    default: unity.Add(p); break;
                }
            }

            var badges = new List<string>
            {
                HtmlPageBuilder.Badge("lav", manifest.packages.Count + " packages")
            };
            if (updates > 0) { badges.Add(HtmlPageBuilder.Badge("warn", updates + " update" + (updates == 1 ? "" : "s"))); }

            string subHtml = HtmlPageBuilder.I18n("span", null, "Last scanned: ", "最終スキャン: ", "آخرین اسکن: ") + HtmlPageBuilder.Escape(manifest.packagesExportedUtc);
            string header = HtmlPageBuilder.RenderPageHeader("📦",
                "Packages", subHtml, badges, true);

            var sb = new StringBuilder(4096);

            sb.Append("<div class=\"ds-stat-grid\">");
            sb.Append(StatTile(thirdParty.Count, "Third-party", "サードパーティ", "شخص‌ثالث"));
            sb.Append(StatTile(unity.Count, "Unity packages", "Unityパッケージ", "پکیج‌های یونیتی"));
            sb.Append(StatTile(builtin.Count, "Built-in modules", "組み込みモジュール", "ماژول‌های داخلی"));
            sb.Append(StatTile(updates, "Updates available", "更新あり", "بروزرسانی موجود"));
            sb.Append("</div>\n");

            // Part 1 - third-party (Asset Store / GitHub / local).
            sb.Append("<div class=\"ds-card\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null,
                "Installed · Asset Store / Git / third-party",
                "インストール済み · Asset Store / Git / サードパーティ",
                "نصب‌شده · Asset Store / گیت‌هاب / شخص‌ثالث"));
            if (thirdParty.Count == 0)
            {
                sb.Append(EmptyNote(
                    "No third-party packages detected. Asset Store assets imported straight into Assets/ are not UPM packages and are listed on the Assets pages instead.",
                    "サードパーティ製パッケージは見つかりませんでした。Assets/ に直接インポートされたAsset Storeアセットは UPM パッケージではないため、アセットページに表示されます。",
                    "هیچ پکیج شخص‌ثالثی پیدا نشد. اسیت‌هایی که مستقیم داخل Assets/ ایمپورت شدن پکیج UPM نیستن و توی صفحات Assets نشون داده میشن."));
            }
            else
            {
                sb.Append("<div class=\"ds-pkg-grid\">");
                foreach (ManifestPackageEntry p in thirdParty) { sb.Append(PackageCard(p)); }
                sb.Append("</div>");
            }
            sb.Append("</div>\n");

            // Part 2 - Unity's own packages.
            sb.Append("<div class=\"ds-card\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null,
                "Unity packages", "Unityパッケージ", "پکیج‌های یونیتی"));
            if (unity.Count == 0)
            {
                sb.Append(EmptyNote("No Unity registry packages found.", "Unityレジストリのパッケージは見つかりませんでした。", "هیچ پکیج رجیستری یونیتی پیدا نشد."));
            }
            else
            {
                sb.Append("<div class=\"ds-pkg-grid\">");
                foreach (ManifestPackageEntry p in unity) { sb.Append(PackageCard(p)); }
                sb.Append("</div>");
            }

            // Built-in engine modules: always present, high count, low
            // interest - kept in a collapsed <details> so they never bury
            // the packages a reader actually installed.
            if (builtin.Count > 0)
            {
                sb.Append("<details class=\"ds-detail\" style=\"margin-top:14px;\"><summary>");
                sb.Append(HtmlPageBuilder.I18n("span", null,
                    "Built-in Unity modules", "組み込みUnityモジュール", "ماژول‌های داخلی یونیتی"));
                sb.Append(" (").Append(builtin.Count).Append(")</summary><div class=\"ds-detail-body\"><ul class=\"ds-module-list\">");
                foreach (ManifestPackageEntry p in builtin)
                {
                    sb.Append("<li><span class=\"ds-module-name\">").Append(HtmlPageBuilder.Escape(p.displayName))
                      .Append("</span> <span class=\"ds-module-ver mono\">").Append(HtmlPageBuilder.Escape(p.version)).Append("</span></li>");
                }
                sb.Append("</ul></div></details>");
            }
            sb.Append("</div>\n");

            return HtmlPageBuilder.RenderPage(manifest, DocSnapConstants.PackagesFileName, "Packages", header, sb.ToString());
        }

        // ==========================================
        // PackageCard
        // One package: name, id, version + source
        // badges, an update-available badge when Unity
        // reports a newer version, author, description,
        // and an access link.
        // ==========================================
        private static string PackageCard(ManifestPackageEntry p)
        {
            var sb = new StringBuilder(512);
            sb.Append("<div class=\"ds-pkg-card\">");
            sb.Append("<div class=\"ds-pkg-head\"><h4>").Append(Emoji(p)).Append(" ").Append(HtmlPageBuilder.Escape(p.displayName)).Append("</h4>");
            sb.Append("<div class=\"ds-badge-row\">");
            if (!string.IsNullOrEmpty(p.version)) { sb.Append(HtmlPageBuilder.Badge("lav", "v" + p.version)); }
            if (!string.IsNullOrEmpty(p.source)) { sb.Append(HtmlPageBuilder.Badge("ghost", p.source)); }
            if (p.updateAvailable)
            {
                string to = string.IsNullOrEmpty(p.latestVersion) ? "" : " → " + p.latestVersion;
                sb.Append(HtmlPageBuilder.BadgeRaw("warn", "⬆ " + HtmlPageBuilder.I18n("span", null, "Update", "更新", "بروزرسانی") + HtmlPageBuilder.Escape(to)));
            }
            sb.Append("</div></div>");

            sb.Append("<div class=\"ds-pkg-body\">");
            sb.Append("<div class=\"ds-pkg-id mono\">").Append(HtmlPageBuilder.Escape(p.name)).Append("</div>");
            if (!string.IsNullOrEmpty(p.author))
            {
                sb.Append("<div class=\"ds-pkg-author\">").Append(HtmlPageBuilder.I18n("span", null, "by ", "作者: ", "توسط ")).Append(HtmlPageBuilder.Escape(p.author)).Append("</div>");
            }
            if (!string.IsNullOrEmpty(p.description))
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

        private static string Emoji(ManifestPackageEntry p)
        {
            switch (p.category)
            {
                case "thirdparty": return "🧩"; // puzzle piece
                case "builtin": return "⚙️";      // gear
                default: return "🌐";              // globe (Unity registry)
            }
        }

        private static string StatTile(int num, string en, string ja, string fa)
        {
            return "<div class=\"ds-stat-tile\"><div class=\"ds-stat-num\">" + num + "</div><div class=\"ds-stat-label\">" + HtmlPageBuilder.I18n("span", null, en, ja, fa) + "</div></div>";
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
