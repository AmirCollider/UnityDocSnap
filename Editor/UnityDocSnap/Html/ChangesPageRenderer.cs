// ==========================================
// ChangesPageRenderer
// Builds changes.html: everything that changed
// between this export and a chosen earlier
// version. Files added / removed / modified (with
// the file names themselves), package changes,
// scene changes, and the difference in export
// time. The diff is computed entirely in C# from
// the two stored VersionSnapshots, so the page is
// plain static HTML - no data left to fetch, works
// under file://.
// ==========================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Export;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Html
{
    internal static class ChangesPageRenderer
    {
        // ==========================================
        // Render
        // current = the export just produced; baseSnap =
        // the earlier version it is being compared against.
        // ==========================================
        public static string Render(ManifestState manifest, VersionSnapshot current, VersionSnapshot baseSnap)
        {
            var badges = new List<string>
            {
                HtmlPageBuilder.Badge("lav", current.version),
                HtmlPageBuilder.Badge("pink", "vs " + baseSnap.version)
            };
            string subHtml = HtmlPageBuilder.I18n("span", null,
                "What changed since ", "変更点(比較元: ", "تغییرات نسبت به ") + HtmlPageBuilder.Escape(baseSnap.version);
            string header = HtmlPageBuilder.RenderPageHeader("🔀", "Changes", subHtml, badges, true);

            var sb = new StringBuilder(4096);

            // ----- File diff -----
            var added = new List<string>();
            var removed = new List<string>();
            var modified = new List<string>();
            DiffFiles(baseSnap, current, added, removed, modified);

            // ----- Package diff -----
            var pkgAdded = new List<string>();
            var pkgRemoved = new List<string>();
            var pkgChanged = new List<string>();
            DiffPackages(baseSnap, current, pkgAdded, pkgRemoved, pkgChanged);

            // ----- Scene diff -----
            var sceneAdded = new List<string>();
            var sceneRemoved = new List<string>();
            var sceneChanged = new List<string>();
            DiffScenes(baseSnap, current, sceneAdded, sceneRemoved, sceneChanged);

            // ----- Headline tiles -----
            sb.Append("<div class=\"ds-stat-grid\">");
            sb.Append(StatTile(added.Count, "Files added", "追加ファイル", "فایل‌های اضافه‌شده", "mint"));
            sb.Append(StatTile(removed.Count, "Files removed", "削除ファイル", "فایل‌های حذف‌شده", "warn"));
            sb.Append(StatTile(modified.Count, "Files modified", "変更ファイル", "فایل‌های تغییرکرده", "lav"));
            sb.Append(StatTile(current.assetCount - baseSnap.assetCount, "Net file change", "ファイル増減", "تغییر خالص فایل‌ها", "pink", true));
            sb.Append("</div>\n");

            // ----- Timing -----
            sb.Append("<div class=\"ds-card\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null, "Export timing", "エクスポート時刻", "زمان خروجی"));
            sb.Append("<div class=\"ds-info-lines\">");
            sb.Append(TimingLine("🕒", baseSnap.version, baseSnap.exportedLocal, baseSnap.timeZone));
            sb.Append(TimingLine("🕒", current.version, current.exportedLocal, current.timeZone));
            string elapsed = DescribeElapsed(baseSnap.exportedUtc, current.exportedUtc);
            if (!string.IsNullOrEmpty(elapsed))
            {
                sb.Append("<div class=\"ds-info-line\"><span class=\"ds-info-key\">⏳ ")
                  .Append(HtmlPageBuilder.I18n("span", null, "Time between exports", "エクスポート間隔", "فاصله‌ی زمانی"))
                  .Append("</span><span class=\"ds-info-val\">").Append(HtmlPageBuilder.Escape(elapsed)).Append("</span></div>");
            }
            sb.Append("</div></div>\n");

            // ----- File lists -----
            sb.Append(DiffSection("📄", "Files", "ファイル", "فایل‌ها", added, removed, modified,
                "Added", "追加", "اضافه‌شده",
                "Removed", "削除", "حذف‌شده",
                "Modified", "変更", "تغییرکرده"));

            // ----- Packages -----
            sb.Append(DiffSection("📦", "Packages", "パッケージ", "پکیج‌ها", pkgAdded, pkgRemoved, pkgChanged,
                "Added", "追加", "اضافه‌شده",
                "Removed", "削除", "حذف‌شده",
                "Changed", "変更", "تغییرکرده"));

            // ----- Scenes -----
            sb.Append(DiffSection("🎬", "Scenes", "シーン", "سین‌ها", sceneAdded, sceneRemoved, sceneChanged,
                "Added", "追加", "اضافه‌شده",
                "Removed", "削除", "حذف‌شده",
                "Changed", "変更", "تغییرکرده"));

            bool nothing = added.Count == 0 && removed.Count == 0 && modified.Count == 0
                && pkgAdded.Count == 0 && pkgRemoved.Count == 0 && pkgChanged.Count == 0
                && sceneAdded.Count == 0 && sceneRemoved.Count == 0 && sceneChanged.Count == 0;
            if (nothing)
            {
                sb.Append("<div class=\"ds-callout\">").Append(HtmlPageBuilder.I18n("span", null,
                    "No differences detected between these two versions.",
                    "この2つのバージョンの間に違いは見つかりませんでした。",
                    "هیچ تفاوتی بین این دو نسخه پیدا نشد.")).Append("</div>\n");
            }

            return HtmlPageBuilder.RenderPage(manifest, DocSnapConstants.ChangesFileName, "Changes", header, sb.ToString());
        }

        // ==========================================
        // DiffFiles / DiffPackages / DiffScenes
        // ==========================================
        private static void DiffFiles(VersionSnapshot baseSnap, VersionSnapshot current, List<string> added, List<string> removed, List<string> modified)
        {
            var baseMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (VersionFileEntry f in baseSnap.files) { baseMap[f.path] = f.signature; }
            var curMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (VersionFileEntry f in current.files) { curMap[f.path] = f.signature; }

            foreach (VersionFileEntry f in current.files)
            {
                string baseSig;
                if (!baseMap.TryGetValue(f.path, out baseSig)) { added.Add(f.path); }
                else if (baseSig != f.signature) { modified.Add(f.path); }
            }
            foreach (VersionFileEntry f in baseSnap.files)
            {
                if (!curMap.ContainsKey(f.path)) { removed.Add(f.path); }
            }
            added.Sort(StringComparer.OrdinalIgnoreCase);
            removed.Sort(StringComparer.OrdinalIgnoreCase);
            modified.Sort(StringComparer.OrdinalIgnoreCase);
        }

        private static void DiffPackages(VersionSnapshot baseSnap, VersionSnapshot current, List<string> added, List<string> removed, List<string> changed)
        {
            var baseMap = new Dictionary<string, VersionPackageEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (VersionPackageEntry p in baseSnap.packages) { baseMap[p.name] = p; }
            var curMap = new Dictionary<string, VersionPackageEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (VersionPackageEntry p in current.packages) { curMap[p.name] = p; }

            foreach (VersionPackageEntry p in current.packages)
            {
                VersionPackageEntry old;
                if (!baseMap.TryGetValue(p.name, out old)) { added.Add(p.name + "  " + p.version); }
                else if (old.version != p.version) { changed.Add(p.name + "  " + old.version + " → " + p.version); }
            }
            foreach (VersionPackageEntry p in baseSnap.packages)
            {
                if (!curMap.ContainsKey(p.name)) { removed.Add(p.name + "  " + p.version); }
            }
            added.Sort(StringComparer.OrdinalIgnoreCase);
            removed.Sort(StringComparer.OrdinalIgnoreCase);
            changed.Sort(StringComparer.OrdinalIgnoreCase);
        }

        private static void DiffScenes(VersionSnapshot baseSnap, VersionSnapshot current, List<string> added, List<string> removed, List<string> changed)
        {
            var baseMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (VersionSceneEntry s in baseSnap.scenes) { baseMap[s.name] = s.gameObjectCount; }
            var curMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (VersionSceneEntry s in current.scenes) { curMap[s.name] = s.gameObjectCount; }

            foreach (VersionSceneEntry s in current.scenes)
            {
                int old;
                if (!baseMap.TryGetValue(s.name, out old)) { added.Add(s.name + "  (" + s.gameObjectCount + " GameObjects)"); }
                else if (old != s.gameObjectCount)
                {
                    int delta = s.gameObjectCount - old;
                    changed.Add(s.name + "  " + old + " → " + s.gameObjectCount + " GameObjects (" + (delta > 0 ? "+" : "") + delta + ")");
                }
            }
            foreach (VersionSceneEntry s in baseSnap.scenes)
            {
                if (!curMap.ContainsKey(s.name)) { removed.Add(s.name); }
            }
            added.Sort(StringComparer.OrdinalIgnoreCase);
            removed.Sort(StringComparer.OrdinalIgnoreCase);
            changed.Sort(StringComparer.OrdinalIgnoreCase);
        }

        // ==========================================
        // DiffSection — one card with three collapsible
        // lists (added / removed / changed), each hidden
        // when empty.
        // ==========================================
        private static string DiffSection(string emoji, string titleEn, string titleJa, string titleFa,
            List<string> added, List<string> removed, List<string> changed,
            string aEn, string aJa, string aFa, string rEn, string rJa, string rFa, string cEn, string cJa, string cFa)
        {
            if (added.Count == 0 && removed.Count == 0 && changed.Count == 0) { return ""; }

            var sb = new StringBuilder(1024);
            sb.Append("<div class=\"ds-card\">");
            sb.Append("<h3>").Append(emoji).Append(' ').Append(HtmlPageBuilder.I18n("span", null, titleEn, titleJa, titleFa)).Append("</h3>");
            sb.Append(DiffList("added", added, aEn, aJa, aFa));
            sb.Append(DiffList("removed", removed, rEn, rJa, rFa));
            sb.Append(DiffList("changed", changed, cEn, cJa, cFa));
            sb.Append("</div>\n");
            return sb.ToString();
        }

        private static string DiffList(string variant, List<string> items, string en, string ja, string fa)
        {
            if (items.Count == 0) { return ""; }
            var sb = new StringBuilder(512);
            sb.Append("<details class=\"ds-detail ds-diff-").Append(variant).Append("\" open><summary>");
            sb.Append("<span class=\"ds-diff-badge ds-diff-").Append(variant).Append("\">").Append(items.Count).Append("</span> ");
            sb.Append(HtmlPageBuilder.I18n("span", null, en, ja, fa));
            sb.Append("</summary><div class=\"ds-detail-body\"><ul class=\"ds-diff-list\">");
            foreach (string item in items)
            {
                sb.Append("<li class=\"ds-diff-item ds-diff-").Append(variant).Append("\">").Append(HtmlPageBuilder.Escape(item)).Append("</li>");
            }
            sb.Append("</ul></div></details>");
            return sb.ToString();
        }

        private static string TimingLine(string emoji, string version, string local, string tz)
        {
            return "<div class=\"ds-info-line\"><span class=\"ds-info-key\">" + emoji + " " + HtmlPageBuilder.Escape(version)
                + "</span><span class=\"ds-info-val\">" + HtmlPageBuilder.Escape(local)
                + " <span class=\"ds-info-tz\">" + HtmlPageBuilder.Escape(tz) + "</span></span></div>";
        }

        private static string StatTile(int num, string en, string ja, string fa, string variant, bool signed = false)
        {
            string text = signed && num > 0 ? "+" + num : num.ToString(CultureInfo.InvariantCulture);
            return "<div class=\"ds-stat-tile ds-tile-" + variant + "\"><div class=\"ds-stat-num\">" + text
                + "</div><div class=\"ds-stat-label\">" + HtmlPageBuilder.I18n("span", null, en, ja, fa) + "</div></div>";
        }

        // ==========================================
        // DescribeElapsed — friendly gap between two UTC
        // ISO timestamps, e.g. "2 days 3 hours".
        // ==========================================
        private static string DescribeElapsed(string fromUtc, string toUtc)
        {
            DateTime a, b;
            if (!TryParseUtc(fromUtc, out a) || !TryParseUtc(toUtc, out b)) { return ""; }
            TimeSpan span = b - a;
            if (span < TimeSpan.Zero) { span = span.Duration(); }

            if (span.TotalMinutes < 1) { return "less than a minute"; }
            var parts = new List<string>();
            if (span.Days > 0) { parts.Add(span.Days + (span.Days == 1 ? " day" : " days")); }
            if (span.Hours > 0) { parts.Add(span.Hours + (span.Hours == 1 ? " hour" : " hours")); }
            if (span.Days == 0 && span.Minutes > 0) { parts.Add(span.Minutes + (span.Minutes == 1 ? " minute" : " minutes")); }
            return string.Join(" ", parts.ToArray());
        }

        private static bool TryParseUtc(string s, out DateTime result)
        {
            return DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out result);
        }
    }
}
