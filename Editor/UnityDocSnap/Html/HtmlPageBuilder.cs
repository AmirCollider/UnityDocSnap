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
using System.Globalization;
using System.IO;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Export;
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Licensing;
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
        // JsString
        // A value being embedded inside an inline <script>
        // as a quoted JavaScript string.
        //
        // These used to go through Escape(), which is HTML
        // escaping and is simply the wrong tool inside a
        // <script>: a browser does not decode entities
        // there, so the value the script saw differed from
        // the same value read back out of the matching HTML
        // attribute - and the two are compared against each
        // other to decide whether a reader's stored
        // language/theme belongs to this export. It happened
        // to be harmless only because the one value passed
        // through it is hexadecimal. This escapes for the
        // context it is actually in, and closes the
        // </script> break-out that HTML escaping was
        // accidentally covering.
        // ==========================================
        public static string JsString(string value)
        {
            if (string.IsNullOrEmpty(value)) { return "\"\""; }
            var sb = new StringBuilder(value.Length + 8);
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    // Not a JS requirement - an HTML one. The parser
                    // ends an inline script at the first literal
                    // "</script", wherever it appears, including
                    // inside a string literal.
                    case '<': sb.Append("\\u003c"); break;
                    case '>': sb.Append("\\u003e"); break;
                    case '&': sb.Append("\\u0026"); break;
                    default:
                        if (c < 0x20) { sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture)); }
                        else { sb.Append(c); }
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        // ==========================================
        // JsStringArray
        // A JavaScript array literal of quoted strings,
        // each escaped by JsString - so a language code or
        // catalogue key can never break out of the inline
        // script it is emitted into.
        // ==========================================
        public static string JsStringArray(string[] values)
        {
            if (values == null || values.Length == 0) { return "[]"; }

            var sb = new StringBuilder(values.Length * 8);
            sb.Append('[');
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) { sb.Append(','); }
                sb.Append(JsString(values[i]));
            }
            sb.Append(']');
            return sb.ToString();
        }

        // ==========================================
        // TranslationCatalogueJson
        // DocSnapTranslations as a JavaScript object, so the
        // strings app.js builds at runtime can be localised
        // into a language added after the fact exactly like
        // the ones baked into the markup.
        //
        // Emits "{}" while the catalogue is empty, which is
        // the shipped state - the three built-in languages
        // are written inline in app.js, so there is nothing
        // to send.
        // ==========================================
        public static string TranslationCatalogueJson()
        {
            if (DocSnapTranslations.IsEmpty) { return "{}"; }

            var sb = new StringBuilder(256);
            sb.Append('{');
            bool firstKey = true;
            foreach (string english in DocSnapTranslations.EnglishKeys())
            {
                List<string> codes = DocSnapTranslations.CodesFor(english);
                if (codes.Count == 0) { continue; }

                if (!firstKey) { sb.Append(','); }
                firstKey = false;
                sb.Append(JsString(english)).Append(":{");
                for (int i = 0; i < codes.Count; i++)
                {
                    if (i > 0) { sb.Append(','); }
                    sb.Append(JsString(codes[i])).Append(':')
                      .Append(JsString(DocSnapTranslations.Find(english, codes[i])));
                }
                sb.Append('}');
            }
            sb.Append('}');
            return sb.ToString();
        }

        // ==========================================
        // Href
        // The one way a link to another page in this site
        // is built: percent-encode the path, then escape it
        // for the attribute it lands in.
        //
        // Half the site did this (via FieldRenderer.
        // EncodeUrlPath) and half concatenated the raw
        // manifest value straight into href="…". Those raw
        // ones broke on any Scene or folder whose name
        // contained '&' or a space - and a name containing
        // a double quote, which macOS and Linux both allow
        // in a file name, escaped the attribute entirely.
        // ==========================================
        public static string Href(string relativePath)
        {
            return FieldRenderer.EncodeUrlPath(relativePath);
        }

        public static string Href(string relativePath, string anchor)
        {
            if (string.IsNullOrEmpty(anchor)) { return Href(relativePath); }
            return Href(relativePath) + "#" + Escape(anchor);
        }

        // ==========================================
        // I18n
        // Emits one element carrying every registered
        // language's text as a data attribute. The
        // VISIBLE text is pre-rendered in the export's
        // default language (not always English), so the
        // very first paint of a ja/fa site is already in
        // the right language - no flash of English text
        // while app.js waits for DOMContentLoaded.
        //
        // The attribute list is built from
        // DocSnapLanguages rather than hard-coded as
        // data-en/data-ja/data-fa, so a language added to
        // that registry shows up in the markup here with
        // no change to this method or to any of its ~140
        // call sites. The call sites keep passing the three
        // built-in languages inline; anything beyond them
        // comes from DocSnapTranslations.
        // ==========================================
        public static string I18n(string tag, string cssClass, string en, string ja, string fa)
        {
            string cls = string.IsNullOrEmpty(cssClass) ? "" : " class=\"" + cssClass + "\"";
            string[] codes = DocSnapLanguages.Codes();
            string[] values = DocSnapText.ResolveAll(en, ja, fa);

            var sb = new StringBuilder(64 + (values.Length * 32));
            sb.Append('<').Append(tag).Append(cls);
            for (int i = 0; i < codes.Length; i++)
            {
                sb.Append(" data-").Append(codes[i]).Append("=\"").Append(Escape(values[i])).Append('"');
            }
            sb.Append('>')
              .Append(Escape(DocSnapText.Resolve(DocSnapRenderContext.DefaultLanguage, en, ja, fa)))
              .Append("</").Append(tag).Append('>');
            return sb.ToString();
        }

        // ==========================================
        // I18nAttributes
        // The same per-language expansion as I18n, but as a
        // run of attributes on an element the caller is
        // building itself - "data-ph-en", "data-ph-ja", …
        // for an <input> placeholder, which cannot be
        // localised by swapping element text.
        //
        // Returns a leading space so it drops straight into
        // a tag being concatenated.
        // ==========================================
        public static string I18nAttributes(string attributePrefix, string en, string ja, string fa)
        {
            string[] codes = DocSnapLanguages.Codes();
            string[] values = DocSnapText.ResolveAll(en, ja, fa);

            var sb = new StringBuilder(values.Length * 40);
            for (int i = 0; i < codes.Length; i++)
            {
                sb.Append(' ').Append(attributePrefix).Append(codes[i])
                  .Append("=\"").Append(Escape(values[i])).Append('"');
            }
            return sb.ToString();
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
            string themeDir = DocSnapConstants.SiteAssetsSubFolder + "/";

            // The default language + colour theme the exporter
            // chose. Set straight on <html> so the very first
            // paint already matches (no flash of the wrong theme);
            // app.js still lets a reader's own saved choice win.
            string defLang = DocSnapRenderContext.DefaultLanguage;
            string defTheme = DocSnapRenderContext.DefaultTheme == "dark" ? "dark" : "light";
            string dir = DocSnapLanguages.Direction(defLang);
            string stamp = DocSnapRenderContext.ExportStamp;

            // Which visual skin this export opens with, measured from the
            // exporting machine and the weight of this project. Baked onto
            // <html> like the theme so the very first paint is already
            // right; app.js re-checks it against the machine actually
            // reading the page and lets a reader override either way.
            DocSnapCapabilityReport caps = DocSnapRenderContext.ResolveCapability(manifest);
            string defSkin = DocSnapCapability.Normalize(caps.Skin);

            var sb = new StringBuilder(4096);
            sb.Append("<!doctype html>\n<html lang=\"").Append(defLang).Append("\" dir=\"").Append(dir)
              .Append("\" data-theme=\"").Append(defTheme).Append("\" data-skin=\"").Append(defSkin)
              .Append("\" data-export=\"").Append(Escape(stamp))
              .Append("\">\n<head>\n<meta charset=\"utf-8\">\n");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
            sb.Append("<title>").Append(Escape(titleEn)).Append(" - Unity DocSnap</title>\n");
            // Pre-paint boot script. It applies the reader's stored
            // language / theme (when their choice belongs to THIS
            // export run - same stamped marker app.js maintains) to
            // <html> BEFORE the body is parsed, so direction, fonts
            // and theme are right from the very first paint. Without
            // it, a reader viewing an RTL-default export in English
            // watched the whole page flip rtl→ltr a moment after
            // load. A choice stored while viewing any OTHER export
            // run (the stamp differs) is ignored here, so a fresh
            // export always paints its own defaults first; app.js
            // then resets the stored choice to match. When the
            // stored language differs from the baked one (a text
            // swap is coming at DOMContentLoaded), the body is
            // briefly hidden via html.ds-lang-pending so the swap is
            // invisible; a 1.5s timeout guarantees the page can
            // never stay hidden even if app.js fails to load.
            // The RTL list is baked in from DocSnapLanguages rather
            // than written as lang==='fa' here and again in app.js.
            // A right-to-left language added to the registry gets its
            // direction right on the very first paint, in the one
            // script that runs before the body exists.
            sb.Append("<script>(function(){var d=document.documentElement;var rtl=").Append(JsStringArray(DocSnapLanguages.RightToLeftCodes())).Append(";")
              .Append("var bakedLang=d.getAttribute('lang')||'en';var lang=bakedLang;var theme=d.getAttribute('data-theme')||'light';var stamp=d.getAttribute('data-export')||'';")
              .Append("var skin=d.getAttribute('data-skin')||'lite';")
              .Append("try{if(localStorage.getItem('unityDocSnapDefaults')===stamp+'|'+bakedLang+'|'+theme){var L=localStorage.getItem('unityDocSnapLang');var T=localStorage.getItem('unityDocSnapTheme');var S=localStorage.getItem('unityDocSnapSkin');if(L){lang=L;}if(T){theme=T;}if(S==='cozy'||S==='lite'){skin=S;}}}catch(e){}")
              .Append("if(lang!==bakedLang){d.classList.add('ds-lang-pending');setTimeout(function(){d.classList.remove('ds-lang-pending');},1500);}")
              .Append("d.setAttribute('lang',lang);d.setAttribute('dir',rtl.indexOf(lang)>=0?'rtl':'ltr');d.setAttribute('data-theme',theme);d.setAttribute('data-skin',skin);})();</script>\n");
            sb.Append("<link rel=\"stylesheet\" href=\"").Append(prefix).Append(themeDir).Append(DocSnapConstants.StyleFileName).Append("\">\n</head>\n");
            // Default to the calmer Simple view; app.js restores whichever
            // view the reader last chose. Advanced-only detail is present
            // in the markup either way, just hidden by CSS in Simple mode.
            sb.Append("<body class=\"ds-mode-simple\">\n");
            sb.Append("<div class=\"ds-shell\">\n");
            sb.Append(RenderSidebar(manifest, prefix, currentHtmlFile));
            sb.Append("<main class=\"ds-main\">\n");
            sb.Append(headerHtml);
            sb.Append(bodyHtml);
            sb.Append(RenderFooter());
            sb.Append("</main>\n</div>\n");
            sb.Append("<button class=\"ds-sidebar-reopen\" data-sidebar-toggle aria-label=\"Show sidebar\" title=\"Show sidebar  [\">\u00BB</button>\n");
            sb.Append("<button class=\"ds-back-top\" aria-label=\"Back to top\">\u2191</button>\n");
            // The page's own depth prefix ("" at root, "../" one level down)
            // so the search script can rewrite its root-relative record links
            // to work from wherever this page lives, plus the exporter's chosen
            // default language + theme and this run's stamp (app.js resets any
            // stored reader choice the first time it sees a new run's stamp,
            // so a fresh export always opens with its own defaults). Loaded
            // before app.js.
            sb.Append("<script>window.__DOCSNAP_PREFIX__=").Append(JsString(prefix)).Append(";")
              .Append("window.__DOCSNAP_LANG__=").Append(JsString(defLang)).Append(";")
              // The language registry, handed to the page: which
              // languages exist, which of them are right-to-left, and
              // any translations for strings app.js builds itself
              // rather than receives as markup (search results, the
              // "copied" toast). Without these, app.js needed its own
              // copy of the language list to stay correct.
              .Append("window.__DOCSNAP_LANGS__=").Append(JsStringArray(DocSnapLanguages.Codes())).Append(";")
              .Append("window.__DOCSNAP_RTL__=").Append(JsStringArray(DocSnapLanguages.RightToLeftCodes())).Append(";")
              .Append("window.__DOCSNAP_I18N__=").Append(TranslationCatalogueJson()).Append(";")
              .Append("window.__DOCSNAP_THEME__=").Append(JsString(defTheme)).Append(";")
              .Append("window.__DOCSNAP_SKIN__=").Append(JsString(defSkin)).Append(";")
              .Append("window.__DOCSNAP_CAPS__=").Append(CapabilityJson(caps)).Append(";")
              .Append("window.__DOCSNAP_EXPORT__=").Append(JsString(stamp)).Append(";</script>\n");
            // defer: the (potentially large) search index never blocks
            // HTML parsing; execution order is still guaranteed, so the
            // index global is always assigned before app.js runs.
            sb.Append("<script defer src=\"").Append(prefix).Append(themeDir).Append(DocSnapConstants.SearchIndexFileName).Append("\"></script>\n");
            sb.Append("<script defer src=\"").Append(prefix).Append(themeDir).Append(DocSnapConstants.ScriptFileName).Append("\"></script>\n</body>\n</html>\n");
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
            sb.Append("</div>");
            // The nav is worth its width when you are moving between
            // pages and costs you the width of a field table when you
            // are reading one. On a 13\" laptop that is the difference
            // between a three-column Field/Type/Value grid and a
            // stacked one, so it collapses (and remembers).
            sb.Append("<button class=\"ds-icon-btn\" data-sidebar-toggle aria-label=\"Collapse sidebar\" title=\"Collapse sidebar  [\" style=\"margin-inline-start:auto;\">\u00AB</button>");
            sb.Append("</div>\n");
            sb.Append("<p class=\"ds-tagline\">").Append(Escape(manifest.projectName)).Append("</p>\n");

            // The pre-rendered active state / icon match the export's
            // defaults so nothing visibly changes when app.js re-applies
            // them at DOMContentLoaded (no flash).
            string defLang = DocSnapRenderContext.DefaultLanguage;
            string defTheme = DocSnapRenderContext.DefaultTheme;
            // Resolved here rather than passed down from RenderPage: the
            // report is cached per export run, so this returns the very
            // same measurement without the sidebar depending on being
            // called after the page shell computed it.
            string defSkin = DocSnapCapability.Normalize(DocSnapRenderContext.ResolveCapability(manifest).Skin);
            sb.Append("<div class=\"ds-topbar\">");
            // One button per registered language, in registry order.
            // app.js validates a reader's stored language against the
            // buttons it finds here, so a language that is in the
            // registry is automatically offered, and one that has been
            // removed automatically stops being honoured.
            sb.Append("<div class=\"ds-langbar\" role=\"group\" aria-label=\"Language\">");
            foreach (DocSnapLanguage language in DocSnapLanguages.All)
            {
                sb.Append("<button class=\"ds-lang-btn")
                  .Append(defLang == language.Code ? " is-active" : "")
                  .Append("\" data-lang=\"").Append(Escape(language.Code)).Append("\">")
                  .Append(Escape(language.ButtonLabel)).Append("</button>");
            }
            sb.Append("</div>");
            // Light / dark theme toggle. app.js swaps the icon,
            // flips <html data-theme>, and remembers the choice.
            sb.Append("<button class=\"ds-theme-toggle\" data-theme-toggle aria-label=\"Toggle theme\" title=\"Light / Dark\">")
              .Append("<span class=\"ds-theme-icon\">").Append(defTheme == "dark" ? "☀" : "☾").Append("</span></button>");
            sb.Append("</div>\n");

            // Visual skin. The cozy look is the tool's signature and the
            // nicer thing to sit in front of; it is also more paint work
            // per row, which is why the default is measured rather than
            // assumed. Switching is always allowed - the machine reading
            // the page is often not the one that produced it.
            sb.Append("<div class=\"ds-skinbar\" role=\"group\" aria-label=\"Visual style\">");
            sb.Append("<button class=\"ds-skin-btn").Append(defSkin == DocSnapCapability.SkinCozy ? " is-active" : "").Append("\" data-skin=\"cozy\">")
              .Append(I18n("span", null, "\u2728 Cozy", "\u2728 \u30B3\u30FC\u30B8\u30FC", "\u2728 \u062F\u0646\u062C")).Append("</button>");
            sb.Append("<button class=\"ds-skin-btn").Append(defSkin == DocSnapCapability.SkinLite ? " is-active" : "").Append("\" data-skin=\"lite\">")
              .Append(I18n("span", null, "\u26A1 Lite", "\u26A1 \u30E9\u30A4\u30C8", "\u26A1 \u0633\u0628\u06A9")).Append("</button>");
            sb.Append("</div>\n");
            // Filled in by app.js, and only when the reader has turned the
            // cozy skin on against a measurement that said otherwise.
            sb.Append("<div class=\"ds-skin-warning\" data-skin-warning hidden></div>\n");

            // Detail-level switch. Simple hides the heavy, every-field
            // detail (engine-component fields, GUIDs, import settings)
            // for a quick read; Advanced shows everything. app.js flips
            // the body class and remembers the choice.
            sb.Append("<div class=\"ds-modebar\" role=\"group\" aria-label=\"Detail level\">");
            sb.Append("<button class=\"ds-mode-btn is-active\" data-mode=\"simple\">")
              .Append(I18n("span", null, "🌤 Simple", "🌤 シンプル", "🌤 ساده")).Append("</button>");
            sb.Append("<button class=\"ds-mode-btn\" data-mode=\"advanced\">")
              .Append(I18n("span", null, "🔬 Advanced", "🔬 詳細", "🔬 پیشرفته")).Append("</button>");
            sb.Append("</div>\n");

            // Site-wide search. The <input>'s placeholder is localised by
            // app.js via the data-ph-* attributes (applyLanguage sets it),
            // and the results panel is populated from the embedded search
            // index entirely client-side - no network, works under file://.
            const string phEn = "Search objects, assets…";
            const string phJa = "オブジェクト・アセットを検索…";
            const string phFa = "جستجوی آبجکت‌ها، فایل‌ها…";
            sb.Append("<div class=\"ds-search\">");
            sb.Append("<input type=\"search\" class=\"ds-search-input\" autocomplete=\"off\" spellcheck=\"false\" aria-label=\"Search\"")
              .Append(I18nAttributes("data-ph-", phEn, phJa, phFa))
              // Escaped like any other attribute value. It is a literal
              // today, but it is the only attribute on this element that
              // was being concatenated raw, and a localised string that
              // acquires an apostrophe or a quote should not be the thing
              // that discovers the omission.
              .Append(" placeholder=\"").Append(Escape(DocSnapText.Resolve(defLang, phEn, phJa, phFa))).Append("\">");
            sb.Append("<span class=\"ds-search-hint\" aria-hidden=\"true\">/</span>");
            sb.Append("<div class=\"ds-search-filters\" role=\"group\" aria-label=\"Search filter\">");
            sb.Append("<button class=\"ds-search-filter is-active\" data-search-filter=\"all\">").Append(I18n("span", null, "All", "すべて", "همه")).Append("</button>");
            sb.Append("<button class=\"ds-search-filter\" data-search-filter=\"scene\">").Append(I18n("span", null, "Scenes", "シーン", "سین‌ها")).Append("</button>");
            sb.Append("<button class=\"ds-search-filter\" data-search-filter=\"asset\">").Append(I18n("span", null, "Assets", "アセット", "فایل‌ها")).Append("</button>");
            sb.Append("</div>");
            sb.Append("<div class=\"ds-search-results\" hidden></div>");
            sb.Append("</div>\n");

            bool onIndex = currentHtmlFile == DocSnapConstants.IndexFileName;
            bool onPackages = currentHtmlFile == DocSnapConstants.PackagesFileName;
            bool onChanges = currentHtmlFile == DocSnapConstants.ChangesFileName;
            bool onIssues = currentHtmlFile == DocSnapConstants.IssuesFileName;
            sb.Append("<div class=\"ds-nav-section\"><ul class=\"ds-nav-list\">");
            sb.Append("<li><a class=\"ds-nav-link").Append(onIndex ? " is-current" : "").Append("\" href=\"").Append(prefix).Append(DocSnapConstants.IndexFileName).Append("\">");
            sb.Append(I18n("span", null, "\uD83C\uDFE0 Dashboard", "\uD83C\uDFE0 ダッシュボード", "\uD83C\uDFE0 داشبورد"));
            sb.Append("</a></li>");

            // Project health sits in the primary nav rather than only on
            // the dashboard: it is the one page a reader opens with an
            // actual question ("what is broken, and where?") instead of
            // to browse. The count is the answer at a glance.
            HealthTotals health = DocSnapHealthReport.Totals(manifest);
            sb.Append("<li><a class=\"ds-nav-link").Append(onIssues ? " is-current" : "").Append("\" href=\"").Append(prefix).Append(DocSnapConstants.IssuesFileName).Append("\">");
            sb.Append(I18n("span", null, "🩺 Health", "🩺 \u5065\u5EB7\u72B6\u614B", "🩺 \u0633\u0644\u0627\u0645\u062A"));
            sb.Append("<span class=\"ds-nav-count ").Append(health.TotalFindings > 0 ? "is-warn" : "is-ok").Append("\">")
              .Append(health.TotalFindings > 0 ? health.TotalFindings.ToString(CultureInfo.InvariantCulture) : "\u2713")
              .Append("</span></a></li>");
            if (manifest.packages != null && manifest.packages.Count > 0)
            {
                sb.Append("<li><a class=\"ds-nav-link").Append(onPackages ? " is-current" : "").Append("\" href=\"").Append(prefix).Append(DocSnapConstants.PackagesFileName).Append("\">");
                sb.Append(I18n("span", null, "📦 Packages", "📦 パッケージ", "📦 پکیج‌ها"));
                sb.Append("<span class=\"ds-nav-count\">").Append(manifest.packages.Count).Append("</span></a></li>");
            }
            if (DocSnapRenderContext.HasChangesPage)
            {
                sb.Append("<li><a class=\"ds-nav-link").Append(onChanges ? " is-current" : "").Append("\" href=\"").Append(prefix).Append(DocSnapConstants.ChangesFileName).Append("\">");
                sb.Append(I18n("span", null, "🔀 Changes", "🔀 変更点", "🔀 تغییرات"));
                if (!string.IsNullOrEmpty(DocSnapRenderContext.ChangesBaseVersion))
                {
                    sb.Append("<span class=\"ds-nav-count\">").Append(Escape(DocSnapRenderContext.ChangesBaseVersion)).Append("</span>");
                }
                sb.Append("</a></li>");
            }
            sb.Append("</ul></div>\n");

            sb.Append("<div class=\"ds-nav-section\"><p class=\"ds-nav-title\">");
            sb.Append(I18n("span", null, "Scenes", "シーン", "سین‌ها"));
            sb.Append("</p><div class=\"ds-nav-scroll\"><ul class=\"ds-nav-list\">\n");
            if (manifest.scenes.Count == 0)
            {
                sb.Append("<li>").Append(I18n("span", "ds-nav-empty", "No scenes exported yet", "エクスポート済みシーンはありません", "هنوز سینی اکسپورت نشده")).Append("</li>\n");
            }
            foreach (ManifestSceneEntry entry in manifest.scenes)
            {
                bool isCurrent = entry.htmlFile == currentHtmlFile;
                sb.Append("<li><a class=\"ds-nav-link").Append(isCurrent ? " is-current" : "").Append("\" href=\"")
                  .Append(Href(prefix + entry.htmlFile)).Append("\"><span>")
                  .Append(Escape(entry.sceneName)).Append("</span>")
                  .Append("<span class=\"ds-nav-count\">").Append(entry.gameObjectCount).Append("</span></a></li>\n");
            }
            sb.Append("</ul></div></div>\n");

            sb.Append("<div class=\"ds-nav-section\"><p class=\"ds-nav-title\">");
            sb.Append(I18n("span", null, "Assets", "アセット", "فایل‌ها"));
            sb.Append("</p><div class=\"ds-nav-scroll\"><ul class=\"ds-nav-list\">\n");
            if (manifest.assetFolders.Count == 0)
            {
                sb.Append("<li>").Append(I18n("span", "ds-nav-empty", "No asset folders exported yet", "エクスポート済みアセットはありません", "هنوز پوشه‌ای اکسپورت نشده")).Append("</li>\n");
            }
            foreach (ManifestFolderEntry entry in manifest.assetFolders)
            {
                bool isCurrent = entry.htmlFile == currentHtmlFile;
                sb.Append("<li><a class=\"ds-nav-link").Append(isCurrent ? " is-current" : "").Append("\" href=\"")
                  .Append(Href(prefix + entry.htmlFile)).Append("\"><span>")
                  .Append(Escape(entry.folderPath)).Append("</span>")
                  .Append("<span class=\"ds-nav-count\">").Append(entry.fileCount).Append("</span></a></li>\n");
            }
            sb.Append("</ul></div></div>\n");

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
        //
        // Every format takes the same route: a data: URI
        // inside an <img>. An SVG used to be different -
        // its markup was pasted into the page verbatim -
        // and SVG is not an image format in the way the
        // other two are. It is an XML document that can
        // carry <script>, event-handler attributes and
        // external references, so inlining one hands the
        // exported page arbitrary control over itself.
        // The file is the user's own, which makes the
        // practical risk small, but it is not always only
        // theirs: a logo can arrive from a brand kit, a
        // contractor, or an asset pack, and the page it
        // lands in is the page a whole team opens. Inside
        // an <img> the browser renders the same picture
        // with scripting and external loads disabled by
        // spec - so the fix costs the feature nothing.
        //
        // This is also the one place in the file that was
        // writing unescaped external content into the
        // page, which every other line here goes out of
        // its way not to do.
        // ==========================================
        private static string ResolveLogoHtml()
        {
            // Branding the exported site with somebody else's logo
            // is the white-label half of Pro, and it pairs with the
            // free-edition footer line: an export that carries your
            // mark and not ours is the one you hand a client.
            //
            // The setting itself stays visible and editable in the
            // Free edition rather than being hidden. A path field
            // that quietly does nothing would be the bug report;
            // Project Settings says which edition honours it, so a
            // configured logo is a thing somebody chose and can see
            // the effect of the moment they upgrade.
            if (!DocSnapLicense.Has(DocSnapFeature.Whitelabel))
            {
                return DocSnapSiteAssets.LogoMarkSvg;
            }

            string customPath = DocSnapSettings.CustomLogoAbsolutePath;
            if (string.IsNullOrEmpty(customPath) || !File.Exists(customPath))
            {
                return DocSnapSiteAssets.LogoMarkSvg;
            }

            try
            {
                string ext = Path.GetExtension(customPath).ToLowerInvariant();
                string mime = ext == ".svg" ? "image/svg+xml"
                    : ext == ".jpg" || ext == ".jpeg" ? "image/jpeg"
                    : ext == ".webp" ? "image/webp"
                    : "image/png";

                byte[] bytes = File.ReadAllBytes(customPath);
                return "<img alt=\"logo\" style=\"width:44px;height:44px;object-fit:contain;\" src=\"data:"
                    + mime + ";base64," + Convert.ToBase64String(bytes) + "\">";
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

            // A site exported by the Free edition carries one
            // quiet line saying so, linking to what Pro adds.
            //
            // Deliberately a footer and nothing else: no banner,
            // no watermark across the content, no interstitial. An
            // exported site is the customer's work product - it
            // goes to their team and sometimes to their client -
            // and a free tool that defaces that is a tool people
            // uninstall rather than upgrade. This is the credit
            // line every free tool has earned; buying Pro removes
            // it (see DocSnapFeature.Whitelabel).
            if (!DocSnapLicense.Has(DocSnapFeature.Whitelabel))
            {
                sb.Append("<a class=\"ds-free-badge\" href=\"").Append(DocSnapConstants.ProductUrl)
                  .Append("\" target=\"_blank\" rel=\"noopener\">");
                sb.Append(I18n("span", null,
                    "Free edition · see what Pro adds",
                    "無料版 · Pro の内容を見る",
                    "نسخه‌ی رایگان · ببین Pro چه دارد"));
                sb.Append("</a>");
            }

            sb.Append("</div>\n");
            return sb.ToString();
        }

        // ==========================================
        // RenderPageHeader
        // The title block used at the top of every
        // page's <main>: heading, subtitle, badges.
        //
        // titleIsHtml distinguishes the two kinds of title this
        // site has, which were previously handled the same way and
        // should not be. A DATA title - a project name, a Scene
        // name, a folder path - is one string, escaped, and must
        // never be translated. A UI-LABEL title ("Project health",
        // "Changes", "Packages") is a caption, and passing it as
        // plain text baked one language into the page permanently:
        // the Health heading stayed «سلامت پروژه» in every
        // language, because it was resolved once at export time
        // while every other label on the page carried all three
        // variants for app.js to swap.
        // ==========================================
        public static string RenderPageHeader(string emoji, string titleText, string subText, List<string> badgesHtml,
            bool subTextIsHtml = false, bool titleIsHtml = false)
        {
            var sb = new StringBuilder(512);
            sb.Append("<div class=\"ds-page-header\"><div><h1>").Append(emoji).Append(" ")
              .Append(titleIsHtml ? titleText : Escape(titleText)).Append("</h1>");
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
        // CapabilityJson
        // The skin measurement as a small JS object literal, so the
        // site can quote real numbers back at a reader who overrides
        // the verdict. A warning with no numbers attached is a shrug.
        // ==========================================
        private static string CapabilityJson(DocSnapCapabilityReport caps)
        {
            if (caps == null) { return "{}"; }

            var sb = new StringBuilder(512);
            sb.Append("{\"skin\":").Append(JsString(DocSnapCapability.Normalize(caps.Skin)));
            sb.Append(",\"ramMb\":").Append(caps.SystemMemoryMb.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"cores\":").Append(caps.ProcessorCount.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"cpu\":").Append(JsString(caps.ProcessorType));
            sb.Append(",\"gpu\":").Append(JsString(caps.GraphicsDeviceName));
            sb.Append(",\"gpuMb\":").Append(caps.GraphicsMemoryMb.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"gameObjects\":").Append(caps.GameObjectCount.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"assetFiles\":").Append(caps.AssetFileCount.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"reasons\":[");
            for (int i = 0; i < caps.Reasons.Count; i++)
            {
                if (i > 0) { sb.Append(','); }
                sb.Append(JsString(caps.Reasons[i]));
            }
            sb.Append("]}");
            return sb.ToString();
        }

        // ==========================================
        // Breadcrumb
        // "Dashboard › Scenes › MainMenu" above a page's
        // title.
        //
        // Every page below the root previously opened with no
        // indication of where it sat, which on a Scene called
        // "Main" inside a project with three of them is a real
        // question. It is also the one control that gets a
        // reader back out of a page they landed on from a
        // search result or a cross-link.
        // ==========================================
        public static string Breadcrumb(string prefix, string sectionEn, string sectionJa, string sectionFa, string currentLabel)
        {
            var sb = new StringBuilder(256);
            sb.Append("<nav class=\"ds-breadcrumb\" aria-label=\"Breadcrumb\">");
            sb.Append("<a href=\"").Append(prefix).Append(DocSnapConstants.IndexFileName).Append("\">")
              .Append(I18n("span", null, "Dashboard", "ダッシュボード", "داشبورد")).Append("</a>");
            if (!string.IsNullOrEmpty(sectionEn))
            {
                sb.Append("<span class=\"sep\">/</span>")
                  .Append(I18n("span", null, sectionEn, sectionJa, sectionFa));
            }
            sb.Append("<span class=\"sep\">/</span><span>").Append(Escape(currentLabel)).Append("</span>");
            sb.Append("</nav>\n");
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
