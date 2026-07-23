// ==========================================
// DocSnapExportInfo
// Builds the per-export snapshot (counts, exact
// timing with time zone, and the file/scene/
// package inventory the Changes page diffs),
// writes it into the version folder as both a
// machine-readable export-info.json and a plain,
// readable export-info.txt, and renders the
// "Export Info" card shown on the dashboard.
// ==========================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Html;
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Manifest;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor.Export
{
    internal static class DocSnapExportInfo
    {
        // ==========================================
        // BuildSnapshot
        // Gathers everything worth remembering about the
        // export that just ran: the moment it happened (UTC,
        // a human local stamp, and the machine's time zone),
        // the scene / asset / package / updatable-package
        // counts, and a flat inventory used for diffs later.
        // ==========================================
        public static VersionSnapshot BuildSnapshot(string version, ManifestState manifest, DocSnapExportOptions options)
        {
            var snap = new VersionSnapshot
            {
                version = version,
                defaultLanguage = DocSnapRenderContext.NormalizeLang(options.defaultLanguage),
                defaultTheme = options.defaultTheme == "dark" ? "dark" : "light",
                withFiles = options.includeFiles,
                changesBaseVersion = options.recordChanges ? (options.changesBaseVersion ?? "") : ""
            };

            DateTime nowUtc = DateTime.UtcNow;
            DateTime nowLocal = DateTime.Now;
            snap.exportedUtc = nowUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            snap.exportedLocal = nowLocal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            snap.timeZone = DescribeTimeZone(nowLocal);

            // Scenes (name + object count) from the manifest.
            snap.sceneCount = manifest.scenes.Count;
            foreach (ManifestSceneEntry s in manifest.scenes)
            {
                snap.scenes.Add(new VersionSceneEntry { name = s.sceneName, gameObjectCount = s.gameObjectCount });
            }

            // Packages (+ how many report an update available).
            if (manifest.packages != null)
            {
                snap.packageCount = manifest.packages.Count;
                foreach (ManifestPackageEntry p in manifest.packages)
                {
                    if (p.updateAvailable) { snap.packagesUpdatable++; }
                    snap.packages.Add(new VersionPackageEntry { name = p.name, version = p.version, updateAvailable = p.updateAvailable });
                }
            }

            // Flat file inventory of Assets/ (non-.meta, non-hidden),
            // each with a cheap size + last-write fingerprint.
            snap.files = CollectAssetFiles();
            snap.assetCount = snap.files.Count;

            return snap;
        }

        // ==========================================
        // CollectAssetFiles
        // Every real asset file under Assets/, project-
        // relative, with a size + last-write signature.
        // .meta files, hidden dotfiles, and Unity's own
        // ~ excluded folders are skipped so the count and
        // the diff both track the assets a person means.
        // ==========================================
        public static List<VersionFileEntry> CollectAssetFiles()
        {
            var list = new List<VersionFileEntry>();
            try
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string assets = Application.dataPath;
                if (!Directory.Exists(assets)) { return list; }

                foreach (string file in Directory.GetFiles(assets, "*", SearchOption.AllDirectories))
                {
                    string name = Path.GetFileName(file);
                    if (name.StartsWith(".", StringComparison.Ordinal)) { continue; }
                    if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) { continue; }

                    string relative = MakeRelative(projectRoot, file);
                    long size;
                    long ticks;
                    try
                    {
                        var fi = new FileInfo(file);
                        size = fi.Length;
                        ticks = fi.LastWriteTimeUtc.Ticks;
                    }
                    catch { size = 0; ticks = 0; }

                    list.Add(new VersionFileEntry
                    {
                        path = relative,
                        size = size,
                        signature = size + ":" + ticks
                    });
                }
                list.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Unity DocSnap] Could not collect asset file list: " + ex.Message);
            }
            return list;
        }

        private static string MakeRelative(string root, string full)
        {
            string r = full.Replace('\\', '/');
            string rootN = root.Replace('\\', '/');
            if (r.StartsWith(rootN + "/", StringComparison.OrdinalIgnoreCase)) { return r.Substring(rootN.Length + 1); }
            return r;
        }

        // ==========================================
        // DescribeTimeZone
        // "UTC+03:30 · Iran Standard Time" — the offset in
        // effect right now plus a friendly zone name.
        // ==========================================
        public static string DescribeTimeZone(DateTime localNow)
        {
            try
            {
                TimeZoneInfo tz = TimeZoneInfo.Local;
                TimeSpan off = tz.GetUtcOffset(localNow);
                string sign = off < TimeSpan.Zero ? "-" : "+";
                TimeSpan abs = off.Duration();
                string offset = "UTC" + sign + abs.Hours.ToString("00") + ":" + abs.Minutes.ToString("00");
                string name = tz.IsDaylightSavingTime(localNow) ? tz.DaylightName : tz.StandardName;
                return string.IsNullOrEmpty(name) ? offset : offset + " · " + name;
            }
            catch { return "UTC"; }
        }

        // ==========================================
        // WriteFiles
        // Emits export-info.json (structured) and
        // export-info.txt (plain, human) into the version
        // folder root.
        // ==========================================
        public static void WriteFiles(string versionFolder, VersionSnapshot snap, ManifestState manifest)
        {
            Directory.CreateDirectory(versionFolder);
            File.WriteAllText(Path.Combine(versionFolder, DocSnapConstants.ExportInfoFileName), BuildJson(snap, manifest).ToString());
            File.WriteAllText(Path.Combine(versionFolder, DocSnapConstants.ExportInfoReadableName), BuildReadable(snap, manifest));
        }

        private static JsonValue BuildJson(VersionSnapshot snap, ManifestState manifest)
        {
            var root = JsonValue.Obj();
            root.Set("generatedBy", DocSnapConstants.ToolName + " v" + DocSnapConstants.Version);
            root.Set("projectName", manifest.projectName);
            root.Set("unityVersion", manifest.unityVersion);
            root.Set("version", snap.version);
            root.Set("exportedUtc", snap.exportedUtc);
            root.Set("exportedLocal", snap.exportedLocal);
            root.Set("timeZone", snap.timeZone);
            root.Set("sceneCount", snap.sceneCount);
            root.Set("assetCount", snap.assetCount);
            root.Set("packageCount", snap.packageCount);
            root.Set("packagesUpdatable", snap.packagesUpdatable);
            root.Set("defaultLanguage", snap.defaultLanguage);
            root.Set("defaultTheme", snap.defaultTheme);
            root.Set("includedFiles", snap.withFiles);
            root.Set("hasBackup", snap.hasBackup);
            if (!string.IsNullOrEmpty(snap.changesBaseVersion)) { root.Set("changesBaseVersion", snap.changesBaseVersion); }
            return root;
        }

        private static string BuildReadable(VersionSnapshot snap, ManifestState manifest)
        {
            var sb = new StringBuilder(512);
            sb.Append("Unity DocSnap — Export Info\n");
            sb.Append("===========================\n\n");
            sb.Append("Project        : ").Append(manifest.projectName).Append('\n');
            sb.Append("Unity          : ").Append(manifest.unityVersion).Append('\n');
            sb.Append("Version        : ").Append(snap.version).Append('\n');
            sb.Append("Exported (UTC) : ").Append(snap.exportedUtc).Append('\n');
            sb.Append("Exported (local): ").Append(snap.exportedLocal).Append('\n');
            sb.Append("Time zone      : ").Append(snap.timeZone).Append('\n');
            sb.Append('\n');
            sb.Append("Scenes             : ").Append(snap.sceneCount).Append('\n');
            sb.Append("Assets (files)     : ").Append(snap.assetCount).Append('\n');
            sb.Append("Packages           : ").Append(snap.packageCount).Append('\n');
            sb.Append("Updatable packages : ").Append(snap.packagesUpdatable).Append('\n');
            sb.Append('\n');
            sb.Append("Site default language : ").Append(snap.defaultLanguage).Append('\n');
            sb.Append("Site default theme    : ").Append(snap.defaultTheme).Append('\n');
            sb.Append("Included file bytes    : ").Append(snap.withFiles ? "yes" : "no").Append('\n');
            sb.Append("Project backup (.unitypackage): ").Append(snap.hasBackup ? "yes" : "no").Append('\n');
            if (!string.IsNullOrEmpty(snap.changesBaseVersion))
            {
                sb.Append("Changes compared against: ").Append(snap.changesBaseVersion).Append('\n');
            }
            return sb.ToString();
        }

        // ==========================================
        // RenderCard
        // The "Export Info" card shown at the top of the
        // dashboard: the exact export moment with its time
        // zone, and the four headline counts.
        // ==========================================
        public static string RenderCard(VersionSnapshot snap)
        {
            if (snap == null) { return ""; }
            var sb = new StringBuilder(1024);
            sb.Append("<div class=\"ds-card ds-export-info\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null, "Export Info", "エクスポート情報", "اطلاعات خروجی"));

            sb.Append("<div class=\"ds-info-lines\">");
            sb.Append(InfoLine("🏷", "Version", "バージョン", "نسخه", HtmlPageBuilder.Escape(snap.version)));
            sb.Append(InfoLine("🕒", "Exported", "エクスポート日時", "زمان خروجی",
                HtmlPageBuilder.Escape(snap.exportedLocal) + " <span class=\"ds-info-tz\">" + HtmlPageBuilder.Escape(snap.timeZone) + "</span>"));
            sb.Append(InfoLine("🌐", "UTC", "UTC", "UTC", HtmlPageBuilder.Escape(snap.exportedUtc)));
            sb.Append("</div>");

            sb.Append("<div class=\"ds-stat-grid\" style=\"margin-top:14px;margin-bottom:0;\">");
            sb.Append(StatTile(snap.sceneCount, "Scenes", "シーン", "سین‌ها"));
            sb.Append(StatTile(snap.assetCount, "Assets", "アセット", "فایل‌ها (Assets)"));
            sb.Append(StatTile(snap.packageCount, "Packages", "パッケージ", "پکیج‌ها"));
            sb.Append(StatTile(snap.packagesUpdatable, "Updatable packages", "更新可能パッケージ", "پکیج‌های قابل‌آپدیت"));
            sb.Append("</div>");
            sb.Append("</div>\n");
            return sb.ToString();
        }

        private static string InfoLine(string emoji, string en, string ja, string fa, string valueHtml)
        {
            return "<div class=\"ds-info-line\"><span class=\"ds-info-key\">" + emoji + " "
                + HtmlPageBuilder.I18n("span", null, en, ja, fa)
                + "</span><span class=\"ds-info-val\">" + valueHtml + "</span></div>";
        }

        private static string StatTile(int num, string en, string ja, string fa)
        {
            return "<div class=\"ds-stat-tile\"><div class=\"ds-stat-num\">" + num + "</div><div class=\"ds-stat-label\">"
                + HtmlPageBuilder.I18n("span", null, en, ja, fa) + "</div></div>";
        }
    }
}
