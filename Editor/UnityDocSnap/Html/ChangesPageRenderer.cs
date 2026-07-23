// ==========================================
// ChangesPageRenderer
// Builds changes.html: everything that changed
// between this export and a chosen earlier
// version. Files added / removed / modified (with
// the file names AND their sizes / size deltas),
// package changes, scene changes (with GameObject
// deltas), the total size change, and the
// difference in export time. The diff is computed
// entirely in C# from the two stored
// VersionSnapshots, so the page is plain static
// HTML - no data left to fetch, works under file://.
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
        // FileDiff
        // One file's before/after entry, so the page can
        // show sizes and size deltas, not just the path.
        // ==========================================
        private sealed class FileDiff
        {
            public string path;
            public long oldSize;
            public long newSize;
        }

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

            // ----- File diff (with sizes) -----
            var added = new List<FileDiff>();
            var removed = new List<FileDiff>();
            var modified = new List<FileDiff>();
            DiffFiles(baseSnap, current, added, removed, modified);

            long baseBytes = 0;
            foreach (VersionFileEntry f in baseSnap.files) { baseBytes += f.size; }
            long currentBytes = 0;
            foreach (VersionFileEntry f in current.files) { currentBytes += f.size; }
            long bytesDelta = currentBytes - baseBytes;

            // ----- Package diff -----
            var pkgItems = new List<string>[3] { new List<string>(), new List<string>(), new List<string>() };
            DiffPackages(baseSnap, current, pkgItems[0], pkgItems[1], pkgItems[2]);

            // ----- Scene diff -----
            var sceneItems = new List<string>[3] { new List<string>(), new List<string>(), new List<string>() };
            int goDelta = DiffScenes(baseSnap, current, sceneItems[0], sceneItems[1], sceneItems[2]);

            int pkgChangeCount = pkgItems[0].Count + pkgItems[1].Count + pkgItems[2].Count;
            int sceneChangeCount = sceneItems[0].Count + sceneItems[1].Count + sceneItems[2].Count;

            // ----- Headline tiles -----
            sb.Append("<div class=\"ds-stat-grid\">");
            sb.Append(StatTile(added.Count.ToString(CultureInfo.InvariantCulture), "Files added", "追加ファイル", "فایل‌های اضافه‌شده", "mint"));
            sb.Append(StatTile(removed.Count.ToString(CultureInfo.InvariantCulture), "Files removed", "削除ファイル", "فایل‌های حذف‌شده", "warn"));
            sb.Append(StatTile(modified.Count.ToString(CultureInfo.InvariantCulture), "Files modified", "変更ファイル", "فایل‌های تغییرکرده", "lav"));
            sb.Append(StatTile(Signed(current.assetCount - baseSnap.assetCount), "Net file change", "ファイル増減", "تغییر خالص فایل‌ها", "pink"));
            sb.Append(StatTile(FormatSignedBytes(bytesDelta), "Size change", "サイズ増減", "تغییر حجم", bytesDelta >= 0 ? "mint" : "warn"));
            sb.Append(StatTile(Signed(goDelta), "GameObjects", "GameObject増減", "تغییر GameObject ها", "lav"));
            sb.Append(StatTile(pkgChangeCount.ToString(CultureInfo.InvariantCulture), "Package changes", "パッケージ変更", "تغییرات پکیج", "pink"));
            sb.Append(StatTile(sceneChangeCount.ToString(CultureInfo.InvariantCulture), "Scene changes", "シーン変更", "تغییرات سین", "lav"));
            sb.Append("</div>\n");

            // ----- Timing + totals -----
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
            sb.Append(InfoLine("🗂", "Total files", "総ファイル数", "کل فایل‌ها",
                baseSnap.assetCount + " → " + current.assetCount));
            sb.Append(InfoLine("💾", "Total size", "総サイズ", "حجم کل",
                FormatBytes(baseBytes) + " → " + FormatBytes(currentBytes)));
            sb.Append("</div></div>\n");

            // ----- File lists -----
            var addedHtml = new List<string>(added.Count);
            foreach (FileDiff d in added) { addedHtml.Add(FileItem("added", d)); }
            var removedHtml = new List<string>(removed.Count);
            foreach (FileDiff d in removed) { removedHtml.Add(FileItem("removed", d)); }
            var modifiedHtml = new List<string>(modified.Count);
            foreach (FileDiff d in modified) { modifiedHtml.Add(FileItem("changed", d)); }

            sb.Append(DiffSection("📄", "Files", "ファイル", "فایル‌ها", addedHtml, removedHtml, modifiedHtml,
                "Added", "追加", "اضافه‌شده",
                "Removed", "削除", "حذف‌شده",
                "Modified", "変更", "تغییرکرده"));

            // ----- Packages -----
            sb.Append(DiffSection("📦", "Packages", "パッケージ", "پکیج‌ها", pkgItems[0], pkgItems[1], pkgItems[2],
                "Added", "追加", "اضافه‌شده",
                "Removed", "削除", "حذف‌شده",
                "Changed", "変更", "تغییرکرده"));

            // ----- Scenes -----
            sb.Append(DiffSection("🎬", "Scenes", "シーン", "سین‌ها", sceneItems[0], sceneItems[1], sceneItems[2],
                "Added", "追加", "اضافه‌شده",
                "Removed", "削除", "حذف‌شده",
                "Changed", "変更", "تغییرکرده"));

            bool nothing = added.Count == 0 && removed.Count == 0 && modified.Count == 0
                && pkgChangeCount == 0 && sceneChangeCount == 0;
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
        private static void DiffFiles(VersionSnapshot baseSnap, VersionSnapshot current, List<FileDiff> added, List<FileDiff> removed, List<FileDiff> modified)
        {
            var baseMap = new Dictionary<string, VersionFileEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (VersionFileEntry f in baseSnap.files) { baseMap[f.path] = f; }
            var curMap = new Dictionary<string, VersionFileEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (VersionFileEntry f in current.files) { curMap[f.path] = f; }

            foreach (VersionFileEntry f in current.files)
            {
                VersionFileEntry old;
                if (!baseMap.TryGetValue(f.path, out old)) { added.Add(new FileDiff { path = f.path, newSize = f.size }); }
                else if (old.signature != f.signature) { modified.Add(new FileDiff { path = f.path, oldSize = old.size, newSize = f.size }); }
            }
            foreach (VersionFileEntry f in baseSnap.files)
            {
                if (!curMap.ContainsKey(f.path)) { removed.Add(new FileDiff { path = f.path, oldSize = f.size }); }
            }
            Comparison<FileDiff> byPath = (a, b) => string.Compare(a.path, b.path, StringComparison.OrdinalIgnoreCase);
            added.Sort(byPath);
            removed.Sort(byPath);
            modified.Sort(byPath);
        }

        private static void DiffPackages(VersionSnapshot baseSnap, VersionSnapshot current, List<string> added, List<string> removed, List<string> changed)
        {
            var baseMap = new Dictionary<string, VersionPackageEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (VersionPackageEntry p in baseSnap.packages) { baseMap[p.name] = p; }
            var curMap = new Dictionary<string, VersionPackageEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (VersionPackageEntry p in current.packages) { curMap[p.name] = p; }

            var addedNames = new List<VersionPackageEntry>();
            var changedPairs = new List<KeyValuePair<VersionPackageEntry, VersionPackageEntry>>();
            var removedNames = new List<VersionPackageEntry>();

            foreach (VersionPackageEntry p in current.packages)
            {
                VersionPackageEntry old;
                if (!baseMap.TryGetValue(p.name, out old)) { addedNames.Add(p); }
                else if (old.version != p.version) { changedPairs.Add(new KeyValuePair<VersionPackageEntry, VersionPackageEntry>(old, p)); }
            }
            foreach (VersionPackageEntry p in baseSnap.packages)
            {
                if (!curMap.ContainsKey(p.name)) { removedNames.Add(p); }
            }

            addedNames.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            removedNames.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            changedPairs.Sort((a, b) => string.Compare(a.Value.name, b.Value.name, StringComparison.OrdinalIgnoreCase));

            foreach (VersionPackageEntry p in addedNames) { added.Add(NamedItem("added", p.name, p.version)); }
            foreach (VersionPackageEntry p in removedNames) { removed.Add(NamedItem("removed", p.name, p.version)); }
            foreach (KeyValuePair<VersionPackageEntry, VersionPackageEntry> pair in changedPairs)
            {
                changed.Add(NamedItem("changed", pair.Value.name, pair.Key.version + " → " + pair.Value.version));
            }
        }

        // Returns the net GameObject-count delta across all scenes.
        private static int DiffScenes(VersionSnapshot baseSnap, VersionSnapshot current, List<string> added, List<string> removed, List<string> changed)
        {
            var baseMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int baseTotal = 0;
            foreach (VersionSceneEntry s in baseSnap.scenes) { baseMap[s.name] = s.gameObjectCount; baseTotal += s.gameObjectCount; }
            var curMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int curTotal = 0;
            foreach (VersionSceneEntry s in current.scenes) { curMap[s.name] = s.gameObjectCount; curTotal += s.gameObjectCount; }

            var curSorted = new List<VersionSceneEntry>(current.scenes);
            curSorted.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            foreach (VersionSceneEntry s in curSorted)
            {
                int old;
                if (!baseMap.TryGetValue(s.name, out old))
                {
                    added.Add(NamedItem("added", s.name, s.gameObjectCount + " GameObjects"));
                }
                else if (old != s.gameObjectCount)
                {
                    int delta = s.gameObjectCount - old;
                    changed.Add(NamedItem("changed", s.name, old + " → " + s.gameObjectCount + " GameObjects (" + Signed(delta) + ")"));
                }
            }

            var baseSorted = new List<VersionSceneEntry>(baseSnap.scenes);
            baseSorted.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            foreach (VersionSceneEntry s in baseSorted)
            {
                if (!curMap.ContainsKey(s.name)) { removed.Add(NamedItem("removed", s.name, s.gameObjectCount + " GameObjects")); }
            }

            return curTotal - baseTotal;
        }

        // ==========================================
        // FileItem / NamedItem
        // One diff line: the path split into a light
        // directory part and a bold file name, plus a
        // right-aligned size / detail column.
        // ==========================================
        private static string FileItem(string variant, FileDiff d)
        {
            string normalized = (d.path ?? "").Replace('\\', '/');
            int slash = normalized.LastIndexOf('/');
            string dir = slash >= 0 ? normalized.Substring(0, slash + 1) : "";
            string name = slash >= 0 ? normalized.Substring(slash + 1) : normalized;

            string sizeHtml;
            if (variant == "added")
            {
                sizeHtml = "<span class=\"plus\">+</span> " + HtmlPageBuilder.Escape(FormatBytes(d.newSize));
            }
            else if (variant == "removed")
            {
                sizeHtml = "<span class=\"minus\">−</span> " + HtmlPageBuilder.Escape(FormatBytes(d.oldSize));
            }
            else
            {
                long delta = d.newSize - d.oldSize;
                sizeHtml = HtmlPageBuilder.Escape(FormatBytes(d.oldSize) + " → " + FormatBytes(d.newSize));
                if (delta != 0)
                {
                    sizeHtml += " <span class=\"" + (delta > 0 ? "plus" : "minus") + "\">" + HtmlPageBuilder.Escape(FormatSignedBytes(delta)) + "</span>";
                }
            }

            return "<li class=\"ds-diff-item ds-diff-" + variant + "\">"
                + "<span class=\"ds-diff-pathwrap\"><span class=\"ds-diff-dir\">" + HtmlPageBuilder.Escape(dir)
                + "</span><span class=\"ds-diff-file\">" + HtmlPageBuilder.Escape(name) + "</span></span>"
                + "<span class=\"ds-diff-size\">" + sizeHtml + "</span></li>";
        }

        private static string NamedItem(string variant, string name, string detail)
        {
            return "<li class=\"ds-diff-item ds-diff-" + variant + "\">"
                + "<span class=\"ds-diff-pathwrap\"><span class=\"ds-diff-file\">" + HtmlPageBuilder.Escape(name) + "</span></span>"
                + "<span class=\"ds-diff-size\">" + HtmlPageBuilder.Escape(detail) + "</span></li>";
        }

        // ==========================================
        // DiffSection — one card with three collapsible
        // lists (added / removed / changed), each hidden
        // when empty. Long lists start collapsed so the
        // page opens light even after huge changes.
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

        private static string DiffList(string variant, List<string> itemsHtml, string en, string ja, string fa)
        {
            if (itemsHtml.Count == 0) { return ""; }
            bool open = itemsHtml.Count <= 50;
            var sb = new StringBuilder(512);
            sb.Append("<details class=\"ds-detail ds-diff-").Append(variant).Append("\"").Append(open ? " open" : "").Append("><summary>");
            sb.Append("<span class=\"ds-diff-badge ds-diff-").Append(variant).Append("\">").Append(itemsHtml.Count).Append("</span> ");
            sb.Append(HtmlPageBuilder.I18n("span", null, en, ja, fa));
            sb.Append("</summary><div class=\"ds-detail-body\"><ul class=\"ds-diff-list\">");
            foreach (string item in itemsHtml) { sb.Append(item); }
            sb.Append("</ul></div></details>");
            return sb.ToString();
        }

        private static string TimingLine(string emoji, string version, string local, string tz)
        {
            return "<div class=\"ds-info-line\"><span class=\"ds-info-key\">" + emoji + " " + HtmlPageBuilder.Escape(version)
                + "</span><span class=\"ds-info-val\">" + HtmlPageBuilder.Escape(local)
                + " <span class=\"ds-info-tz\">" + HtmlPageBuilder.Escape(tz) + "</span></span></div>";
        }

        private static string InfoLine(string emoji, string en, string ja, string fa, string value)
        {
            return "<div class=\"ds-info-line\"><span class=\"ds-info-key\">" + emoji + " "
                + HtmlPageBuilder.I18n("span", null, en, ja, fa)
                + "</span><span class=\"ds-info-val\">" + HtmlPageBuilder.Escape(value) + "</span></div>";
        }

        private static string StatTile(string text, string en, string ja, string fa, string variant)
        {
            return "<div class=\"ds-stat-tile ds-tile-" + variant + "\"><div class=\"ds-stat-num\">" + HtmlPageBuilder.Escape(text)
                + "</div><div class=\"ds-stat-label\">" + HtmlPageBuilder.I18n("span", null, en, ja, fa) + "</div></div>";
        }

        private static string Signed(int n)
        {
            return n > 0 ? "+" + n.ToString(CultureInfo.InvariantCulture) : n.ToString(CultureInfo.InvariantCulture);
        }

        // ==========================================
        // FormatBytes / FormatSignedBytes
        // ==========================================
        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = Math.Abs((double)bytes);
            int u = 0;
            while (size >= 1024 && u < units.Length - 1) { size /= 1024; u++; }
            string body = size.ToString("0.#", CultureInfo.InvariantCulture) + " " + units[u];
            return bytes < 0 ? "-" + body : body;
        }

        private static string FormatSignedBytes(long bytes)
        {
            return bytes > 0 ? "+" + FormatBytes(bytes) : FormatBytes(bytes);
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
