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
using System.Security.Cryptography;
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
        public static VersionSnapshot BuildSnapshot(string version, ManifestState manifest, DocSnapExportOptions options, VersionSnapshot hashSource = null)
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
            snap.files = CollectAssetFiles(hashSource);
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
        public static List<VersionFileEntry> CollectAssetFiles(VersionSnapshot hashSource = null)
        {
            var list = new List<VersionFileEntry>();
            var knownHashes = BuildHashLookup(hashSource);
            int hashed = 0;
            int skippedTooLarge = 0;

            try
            {
                string projectRoot = DocSnapFileScan.ProjectRootAbsolute();

                // DocSnapFileScan honours the exclude rules and the
                // same "Unity does not import this" folder rules the
                // rest of the export uses. This method's own comment
                // already claimed to skip Unity's ~ folders; the
                // Directory.GetFiles call it used to make did not, so
                // the change diff listed files the Editor has never
                // heard of as additions and deletions.
                foreach (string relative in DocSnapFileScan.EnumerateProjectRelativeFiles("Assets", DocSnapExcludeFilter.FromSettings()))
                {
                    string absolute = Path.Combine(projectRoot, relative);
                    long size;
                    long ticks;
                    try
                    {
                        var fi = new FileInfo(absolute);
                        size = fi.Length;
                        ticks = fi.LastWriteTimeUtc.Ticks;
                    }
                    catch { size = 0; ticks = 0; }

                    string signature = size + ":" + ticks;

                    // The whole point of keeping the cheap signature: when
                    // it matches what was recorded last time, the bytes
                    // cannot have changed and the recorded hash is reused
                    // without touching the disk. Only a file the filesystem
                    // says moved is actually read - which on a normal
                    // re-export is a handful of files, not the project.
                    string hash;
                    if (!knownHashes.TryGetValue(SignatureKey(relative, signature), out hash) || string.IsNullOrEmpty(hash))
                    {
                        if (size > DocSnapConstants.MaxHashedFileBytes)
                        {
                            // Reading a multi-gigabyte file to answer "did it
                            // change?" costs more than the answer is worth, and
                            // a file that large changing size is the normal
                            // case anyway. Left empty so the diff falls back
                            // to the signature for this one file.
                            hash = "";
                            skippedTooLarge++;
                        }
                        else
                        {
                            hash = ComputeContentHash(absolute);
                            if (!string.IsNullOrEmpty(hash)) { hashed++; }
                        }
                    }

                    list.Add(new VersionFileEntry
                    {
                        path = relative,
                        size = size,
                        signature = signature,
                        contentHash = hash
                    });
                }
                list.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Unity DocSnap] Could not collect asset file list: " + ex.Message);
            }

            if (skippedTooLarge > 0)
            {
                Debug.Log("[Unity DocSnap] " + skippedTooLarge + " file(s) larger than "
                    + (DocSnapConstants.MaxHashedFileBytes / (1024 * 1024)) + " MB were fingerprinted by size and timestamp instead of content.");
            }
            _lastHashedCount = hashed;
            return list;
        }

        // How many files the last CollectAssetFiles call actually read
        // from disk. Exposed for diagnostics only.
        private static int _lastHashedCount;
        public static int LastHashedCount { get { return _lastHashedCount; } }

        // ==========================================
        // BuildHashLookup
        // path + signature -> recorded content hash, so an
        // unchanged file's hash comes back for free.
        //
        // Keyed on the signature as well as the path on purpose: if
        // the signature moved, the recorded hash may be stale and must
        // not be trusted - a miss here is what triggers the re-read.
        // ==========================================
        private static Dictionary<string, string> BuildHashLookup(VersionSnapshot source)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (source == null || source.files == null) { return map; }
            foreach (VersionFileEntry f in source.files)
            {
                if (string.IsNullOrEmpty(f.path) || string.IsNullOrEmpty(f.contentHash)) { continue; }
                map[SignatureKey(f.path, f.signature)] = f.contentHash;
            }
            return map;
        }

        private static string SignatureKey(string path, string signature)
        {
            return (path ?? "").Replace('\\', '/').ToLowerInvariant() + "|" + (signature ?? "");
        }

        // ==========================================
        // ComputeContentHash
        // A streaming MD5 of the file's bytes, hex-encoded.
        //
        // Not a security hash and not used as one - this only has to
        // answer "are these the same bytes as last time?", where MD5 is
        // both fast and far more than sufficient. Streamed in chunks so
        // a large asset never has to fit in memory at once.
        // ==========================================
        public static string ComputeContentHash(string absolutePath)
        {
            try
            {
                using (var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536))
                using (var md5 = MD5.Create())
                {
                    byte[] digest = md5.ComputeHash(stream);
                    var sb = new StringBuilder(digest.Length * 2);
                    foreach (byte b in digest) { sb.Append(b.ToString("x2", CultureInfo.InvariantCulture)); }
                    return sb.ToString();
                }
            }
            catch
            {
                // A locked or unreadable file falls back to the signature,
                // exactly as it did before hashing existed.
                return "";
            }
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
            // Which edition produced this folder. Recorded because
            // export-info is the file somebody opens six months
            // later to work out why a snapshot has no summary/
            // folder in it - and "made by the free edition" is a
            // complete answer, where silence is a bug hunt.
            root.Set("generatedBy", DocSnapConstants.ToolName + " v" + DocSnapConstants.Version);
            root.Set("edition", Licensing.DocSnapEditionMatrix.DisplayName(Licensing.DocSnapLicense.Edition));
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
            sb.Append("Edition        : ").Append(Licensing.DocSnapEditionMatrix.DisplayName(Licensing.DocSnapLicense.Edition)).Append('\n');
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
        // zone plus per-export facts (backup, changes base,
        // included file bytes). The headline COUNTS are
        // deliberately not repeated here - the dashboard
        // renders one single stat grid right below this
        // card, so the same numbers no longer appear twice
        // with slightly different labels.
        //
        // The export MOMENT is now stated once for the same
        // reason. It used to appear three times on one
        // screen: as a raw UTC ISO string in the page
        // header, as a local stamp here, and as the same ISO
        // string again on a line of its own labelled "UTC".
        // export-info.json and export-info.txt still carry
        // both forms - those are read by programs, and by
        // people six months later, and neither can hover.
        // ==========================================
        public static string RenderCard(VersionSnapshot snap)
        {
            if (snap == null) { return ""; }
            var sb = new StringBuilder(1024);
            sb.Append("<div class=\"ds-card ds-export-info\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null, "Export Info", "エクスポート情報", "اطلاعات خروجی"));

            sb.Append("<div class=\"ds-info-lines\">");

            // "Export version", not "Version". The page also carries a
            // "Unity DocSnap v1.0.1" badge, and on the export that
            // happened to land in a folder called V1.0.1 the card read
            // "Version V1.0.1" beside it - two unrelated numbers,
            // identical, neither one saying which it was.
            sb.Append(InfoLine("🏷", "Export version", "エクスポート版数", "نسخه‌ی این خروجی",
                HtmlPageBuilder.Escape(snap.version)));

            // ONE timestamp, in the reader's own words, with UTC on
            // hover and in export-info.json for anything that needs to
            // parse it. The card used to print the same instant twice -
            // once local, once as an ISO string on its own line - and a
            // reader who noticed had to work out that they matched.
            sb.Append(InfoLine("🕒", "Exported", "エクスポート日時", "زمان خروجی",
                "<span title=\"UTC " + HtmlPageBuilder.Escape(snap.exportedUtc) + "\">"
                + HtmlPageBuilder.Escape(snap.exportedLocal)
                + " <span class=\"ds-info-tz\">" + HtmlPageBuilder.Escape(snap.timeZone) + "</span></span>"));

            if (snap.withFiles)
            {
                sb.Append(InfoLine("🗃", "File copies", "ファイルコピー", "کپی فایل‌ها",
                    HtmlPageBuilder.I18n("span", null, "included (source-files/)", "含む(source-files/)", "شامل (source-files/)")));
            }
            if (snap.hasBackup)
            {
                sb.Append(InfoLine("🛟", "Backup", "バックアップ", "بک‌آپ", HtmlPageBuilder.Escape(DocSnapConstants.BackupFileName)));
            }
            if (!string.IsNullOrEmpty(snap.changesBaseVersion))
            {
                sb.Append(InfoLine("🔀", "Changes vs", "変更点の比較元", "تغییرات نسبت به", HtmlPageBuilder.Escape(snap.changesBaseVersion)));
            }
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
    }
}
