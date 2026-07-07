// ==========================================
// HtmlPageBuilder
// Shared page shell (sidebar, footer, i18n
// plumbing) plus the generic renderers that
// turn a UniversalReflector field tree, a
// GameObject hierarchy, or an asset list into
// finished HTML. Every page in the generated
// site is assembled from these building blocks
// so the look stays consistent everywhere.
// ==========================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Html
{
    internal static class HtmlPageBuilder
    {
        // ==========================================
        // Escape
        // Minimal, dependency-free HTML escaping used
        // for every piece of untrusted project data
        // (names, paths, string field values, …).
        // ==========================================
        public static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) { return ""; }
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\'': sb.Append("&#39;"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        // ==========================================
        // I18n
        // Emits one element carrying all three
        // language variants as data attributes, with
        // English pre-rendered as the visible default
        // (see assets_ui/app.js for the switch logic).
        // ==========================================
        public static string I18n(string tag, string cssClass, string en, string ja, string fa)
        {
            string cls = string.IsNullOrEmpty(cssClass) ? "" : " class=\"" + cssClass + "\"";
            return "<" + tag + cls + " data-en=\"" + Escape(en) + "\" data-ja=\"" + Escape(ja) + "\" data-fa=\"" + Escape(fa) + "\">" + Escape(en) + "</" + tag + ">";
        }

        // ==========================================
        // RenderPage
        // Wraps a page's body content with the shared
        // sidebar, header slot, and footer, resolving
        // every relative asset/link path from the
        // page's own location in the output tree.
        // ==========================================
        public static string RenderPage(ManifestState manifest, string currentHtmlFile, string titleEn, string headerHtml, string bodyHtml)
        {
            string prefix = currentHtmlFile.IndexOf('/') >= 0 ? "../" : "";
            var sb = new StringBuilder(4096);
            sb.Append("<!doctype html>\n<html lang=\"en\" dir=\"ltr\">\n<head>\n<meta charset=\"utf-8\">\n");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
            sb.Append("<title>").Append(Escape(titleEn)).Append(" - Unity DocSnap</title>\n");
            sb.Append("<link rel=\"stylesheet\" href=\"").Append(prefix).Append("assets_ui/style.css\">\n</head>\n<body>\n");
            sb.Append("<div class=\"ds-shell\">\n");
            sb.Append(RenderSidebar(manifest, prefix, currentHtmlFile));
            sb.Append("<main class=\"ds-main\">\n");
            sb.Append(headerHtml);
            sb.Append(bodyHtml);
            sb.Append(RenderFooter());
            sb.Append("</main>\n</div>\n");
            sb.Append("<button class=\"ds-back-top\" aria-label=\"Back to top\">\u2B06</button>\n");
            sb.Append("<script src=\"").Append(prefix).Append("assets_ui/app.js\"></script>\n</body>\n</html>\n");
            return sb.ToString();
        }

        // ==========================================
        // RenderSidebar
        // Brand mark, language switcher, and the live
        // Scenes / Assets navigation built straight
        // from the manifest so it is always current.
        // ==========================================
        private static string RenderSidebar(ManifestState manifest, string prefix, string currentHtmlFile)
        {
            var sb = new StringBuilder(2048);
            sb.Append("<aside class=\"ds-sidebar\">\n<div class=\"ds-brand\">");
            sb.Append(ResolveLogoHtml());
            sb.Append("<div class=\"ds-brand-text\"><h1>Unity DocSnap</h1>");
            sb.Append(I18n("span", null, "Project documentation", "プロジェクトドキュメント", "مستندات پروژه"));
            sb.Append("</div></div>\n");
            sb.Append("<p class=\"ds-tagline\">").Append(Escape(manifest.projectName)).Append("</p>\n");

            sb.Append("<div class=\"ds-langbar\" role=\"group\" aria-label=\"Language\">");
            sb.Append("<button class=\"ds-lang-btn is-active\" data-lang=\"en\">EN</button>");
            sb.Append("<button class=\"ds-lang-btn\" data-lang=\"ja\">日本語</button>");
            sb.Append("<button class=\"ds-lang-btn\" data-lang=\"fa\">فارسی</button>");
            sb.Append("</div>\n");

            bool onIndex = currentHtmlFile == DocSnapConstants.IndexFileName;
            sb.Append("<div class=\"ds-nav-section\"><ul class=\"ds-nav-list\">");
            sb.Append("<li><a class=\"ds-nav-link").Append(onIndex ? " is-current" : "").Append("\" href=\"").Append(prefix).Append(DocSnapConstants.IndexFileName).Append("\">");
            sb.Append(I18n("span", null, "\uD83C\uDFE0 Dashboard", "\uD83C\uDFE0 ダッシュボード", "\uD83C\uDFE0 داشبورد"));
            sb.Append("</a></li></ul></div>\n");

            sb.Append("<div class=\"ds-nav-section\"><p class=\"ds-nav-title\">");
            sb.Append(I18n("span", null, "Scenes", "シーン", "سین‌ها"));
            sb.Append("</p><ul class=\"ds-nav-list\">\n");
            if (manifest.scenes.Count == 0)
            {
                sb.Append("<li>").Append(I18n("span", "ds-nav-empty", "No scenes exported yet", "エクスポート済みシーンはありません", "هنوز سینی اکسپورت نشده")).Append("</li>\n");
            }
            foreach (ManifestSceneEntry entry in manifest.scenes)
            {
                bool isCurrent = entry.htmlFile == currentHtmlFile;
                sb.Append("<li><a class=\"ds-nav-link").Append(isCurrent ? " is-current" : "").Append("\" href=\"")
                  .Append(prefix).Append(entry.htmlFile).Append("\">")
                  .Append(Escape(entry.sceneName))
                  .Append("<span class=\"ds-nav-count\">").Append(entry.gameObjectCount).Append("</span></a></li>\n");
            }
            sb.Append("</ul></div>\n");

            sb.Append("<div class=\"ds-nav-section\"><p class=\"ds-nav-title\">");
            sb.Append(I18n("span", null, "Assets", "アセット", "فایل‌ها"));
            sb.Append("</p><ul class=\"ds-nav-list\">\n");
            if (manifest.assetFolders.Count == 0)
            {
                sb.Append("<li>").Append(I18n("span", "ds-nav-empty", "No asset folders exported yet", "エクスポート済みアセットはありません", "هنوز پوشه‌ای اکسپورت نشده")).Append("</li>\n");
            }
            foreach (ManifestFolderEntry entry in manifest.assetFolders)
            {
                bool isCurrent = entry.htmlFile == currentHtmlFile;
                sb.Append("<li><a class=\"ds-nav-link").Append(isCurrent ? " is-current" : "").Append("\" href=\"")
                  .Append(prefix).Append(entry.htmlFile).Append("\">")
                  .Append(Escape(entry.folderPath))
                  .Append("<span class=\"ds-nav-count\">").Append(entry.fileCount).Append("</span></a></li>\n");
            }
            sb.Append("</ul></div>\n");

            sb.Append("<div class=\"ds-sidebar-footer\">");
            sb.Append(I18n("span", null, "Made with \uD83E\uDDCB by", "\uD83E\uDDCB を込めて", "با \uD83E\uDDCB ساخته‌شده توسط"));
            sb.Append(" <a href=\"").Append(DocSnapConstants.GithubUrl).Append("\">").Append(DocSnapConstants.Author).Append("</a></div>\n");
            sb.Append("</aside>\n");
            return sb.ToString();
        }

        // ==========================================
        // ResolveLogoHtml
        // Base64-embeds the user's configured logo
        // image when one is set and readable on disk;
        // otherwise falls back to the built-in mascot
        // mark so branding always looks finished.
        // ==========================================
        private static string ResolveLogoHtml()
        {
            string customPath = DocSnapSettings.CustomLogoAbsolutePath;
            if (string.IsNullOrEmpty(customPath) || !File.Exists(customPath))
            {
                return DocSnapSiteAssets.LogoMarkSvg;
            }

            try
            {
                string ext = Path.GetExtension(customPath).ToLowerInvariant();
                if (ext == ".svg")
                {
                    return File.ReadAllText(customPath);
                }
                string mime = ext == ".jpg" || ext == ".jpeg" ? "image/jpeg" : "image/png";
                byte[] bytes = File.ReadAllBytes(customPath);
                return "<img alt=\"logo\" style=\"width:44px;height:44px;object-fit:contain;\" src=\"data:" + mime + ";base64," + Convert.ToBase64String(bytes) + "\">";
            }
            catch
            {
                return DocSnapSiteAssets.LogoMarkSvg;
            }
        }

        // ==========================================
        // RenderFooter
        // Small credit line repeated at the bottom of
        // every page, matching README.md's own tone.
        // ==========================================
        private static string RenderFooter()
        {
            var sb = new StringBuilder(256);
            sb.Append("<div class=\"ds-footer\">");
            sb.Append(I18n("span", null,
                "Generated by Unity DocSnap \uD83C\uDF70",
                "Unity DocSnap \uD83C\uDF70 により生成",
                "تولید شده با Unity DocSnap \uD83C\uDF70"));
            sb.Append("<a href=\"").Append(DocSnapConstants.GithubUrl).Append("\">github.com/AmirCollider/UnityDocSnap</a>");
            sb.Append("</div>\n");
            return sb.ToString();
        }

        // ==========================================
        // RenderPageHeader
        // The title block used at the top of every
        // page's <main>: heading, subtitle, badges.
        // ==========================================
        public static string RenderPageHeader(string emoji, string titleText, string subText, List<string> badgesHtml, bool subTextIsHtml = false)
        {
            var sb = new StringBuilder(512);
            sb.Append("<div class=\"ds-page-header\"><div><h1>").Append(emoji).Append(" ").Append(Escape(titleText)).Append("</h1>");
            if (!string.IsNullOrEmpty(subText))
            {
                sb.Append("<p class=\"ds-page-sub\">").Append(subTextIsHtml ? subText : Escape(subText)).Append("</p>");
            }
            sb.Append("</div>");
            if (badgesHtml != null && badgesHtml.Count > 0)
            {
                sb.Append("<div class=\"ds-badge-row\">");
                foreach (string b in badgesHtml) { sb.Append(b); }
                sb.Append("</div>");
            }
            sb.Append("</div>\n");
            return sb.ToString();
        }

        // ==========================================
        // Badge
        // One small pill, e.g. "<span class='ds-badge pink'>...</span>".
        // ==========================================
        public static string Badge(string cssVariant, string text)
        {
            string cls = string.IsNullOrEmpty(cssVariant) ? "ds-badge" : "ds-badge " + cssVariant;
            return "<span class=\"" + cls + "\">" + Escape(text) + "</span>";
        }

        // ==========================================
        // BadgeRaw
        // Same pill chrome as Badge(), but accepts
        // pre-built inner HTML instead of a single
        // plain string - lets callers mix a raw number
        // with an I18n() span (e.g. "5 " + I18n(...))
        // without the markup being escaped away. Caller
        // must escape any raw dynamic text itself.
        // ==========================================
        public static string BadgeRaw(string cssVariant, string innerHtml)
        {
            string cls = string.IsNullOrEmpty(cssVariant) ? "ds-badge" : "ds-badge " + cssVariant;
            return "<span class=\"" + cls + "\">" + innerHtml + "</span>";
        }
    }
}
