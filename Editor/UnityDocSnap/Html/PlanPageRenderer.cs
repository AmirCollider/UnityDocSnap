// ==========================================
// PlanPageRenderer
// Builds plan.html: which edition made this export,
// what that edition includes, what it does not, and
// where to check the licence.
//
// The page exists because an export outlives the moment
// it was made. It gets copied to a share, zipped to a
// client, opened by somebody who was not at the keyboard
// - and the question "why is there no summary/ folder in
// here?" then has no way to be answered from inside the
// artefact. Without this page, the honest answer ("that
// is a paid feature and this export was made on Free")
// looks exactly like the export having failed halfway,
// which is the reading somebody will act on.
//
// Three rules it follows:
//
//   • Both halves, always. The included list is rendered
//     for a Free reader and the missing list for a Pro
//     one - it is a statement of what this plan IS, not
//     an upsell that happens to appear when something is
//     locked. A page that only ever appears to say "you
//     are missing things" is an advert; a page that
//     always says what you have is documentation.
//
//   • Read from the matrix, never restated. Every line
//     comes from DocSnapEditionMatrix and
//     DocSnapUpgradePitch, so the page cannot promise a
//     feature the gate withholds, or omit one it grants.
//     A hand-written copy of the feature list here would
//     be a fourth place to forget to edit.
//
//   • Say where to check. The licence link goes to the
//     Worker's /license page, which is the one place that
//     can answer "is my key really active, and on which
//     machines?" - the question somebody reading this
//     page after an unexpected Free export actually has.
// ==========================================
using System.Collections.Generic;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Licensing;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Html
{
    internal static class PlanPageRenderer
    {
        // ==========================================
        // Render
        // ==========================================
        public static string Render(ManifestState manifest)
        {
            DocSnapEdition edition = DocSnapLicense.Edition;
            string name = DocSnapEditionMatrix.DisplayName(edition);

            var badges = new List<string>
            {
                HtmlPageBuilder.BadgeRaw(edition == DocSnapEdition.Free ? "ghost" : "mint",
                    HtmlPageBuilder.Escape(name))
            };
            if (edition != DocSnapEdition.Free)
            {
                badges.Add(HtmlPageBuilder.BadgeRaw("lav", HtmlPageBuilder.I18n("span", null,
                    "Licensed", "ライセンス済み", "لایسنس‌دار")));
            }

            string header = HtmlPageBuilder.RenderPageHeader("🎟️",
                HtmlPageBuilder.I18n("span", null, "Plan", "プラン", "پلن"),
                HtmlPageBuilder.I18n("span", null,
                    "Which edition produced this export, and what that means for what is in the folder.",
                    "このエクスポートを作成したエディションと、それがフォルダの内容に何を意味するか。",
                    "این خروجی با کدام نسخه ساخته شده و این یعنی چه چیزی توی فولدر هست و چه چیزی نیست."),
                badges, true, true);

            var sb = new StringBuilder(6144);
            sb.Append(RenderPlanCard(edition, name));
            sb.Append(RenderIncludedCard(edition));
            sb.Append(RenderMissingCard(edition));
            sb.Append(RenderVerifyCard(edition));

            return HtmlPageBuilder.RenderPage(manifest, DocSnapConstants.PlanFileName, "Plan", header, sb.ToString());
        }

        // ==========================================
        // RenderPlanCard
        // The one-line answer, plus the two facts that are
        // properties of the plan rather than of a feature:
        // how many snapshots the shelf keeps, and whether the
        // exported site carries the free-edition line.
        // ==========================================
        private static string RenderPlanCard(DocSnapEdition edition, string name)
        {
            var sb = new StringBuilder(1024);
            sb.Append("<div class=\"ds-card\"><div class=\"ds-card-head\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null,
                "This export", "このエクスポート", "این خروجی"));
            sb.Append("</div>");

            sb.Append("<p class=\"ds-empty-note\">");
            if (edition == DocSnapEdition.Free)
            {
                sb.Append(HtmlPageBuilder.I18n("span", null,
                    "Made with the Free edition. Every Scene, every component, every serialized field, the health report, the packages page, search, both skins and all three languages are here — the free edition exports the whole site, and nothing on this page is missing because the export went wrong.",
                    "無料版で作成されました。すべてのシーン、コンポーネント、シリアライズフィールド、ヘルスレポート、パッケージページ、検索、2 つのスキン、3 言語がすべて含まれています。無料版はサイト全体をエクスポートします。このページに挙がっている項目は、エクスポートの失敗によって欠けているのではありません。",
                    "با نسخه‌ی رایگان ساخته شده. همه‌ی سین‌ها، همه‌ی کامپوننت‌ها، همه‌ی فیلدهای سریالایزشده، گزارش سلامت، صفحه‌ی پکیج‌ها، جست‌وجو، هر دو ظاهر و هر سه زبان اینجا هستند — نسخه‌ی رایگان کل سایت را خروجی می‌گیرد و چیزی که در این صفحه نیست، به‌خاطر خطای خروجی‌گیری نیست."));
            }
            else
            {
                sb.Append(HtmlPageBuilder.I18n("span", null,
                    "Made with the " + name + " edition, on a licensed machine.",
                    name + " 版(ライセンス認証済みのマシン)で作成されました。",
                    "با نسخه‌ی " + name + " و روی یک سیستم لایسنس‌دار ساخته شده."));
            }
            sb.Append("</p>");

            sb.Append("<div class=\"ds-info-lines\">");

            sb.Append(InfoLine("🎟️", "Plan", "プラン", "پلن",
                HtmlPageBuilder.Escape(name)));

            int shelf = DocSnapEditionLimits.VersionFolders(edition);
            sb.Append(InfoLine("📚", "Snapshots kept", "保持されるスナップショット", "تعداد اسنپ‌شات نگه‌داشته‌شده",
                shelf == DocSnapEditionLimits.Unlimited
                    ? HtmlPageBuilder.I18n("span", null, "every one", "すべて", "همه")
                    : HtmlPageBuilder.Escape(shelf.ToString(System.Globalization.CultureInfo.InvariantCulture))));

            sb.Append(InfoLine("🏷️", "Free-edition line in the footer", "フッターの無料版表記", "خط «نسخه‌ی رایگان» توی فوتر",
                Yes(DocSnapEditionMatrix.ShowsFreeEditionBadge(edition))));

            sb.Append("</div></div>\n");
            return sb.ToString();
        }

        // ==========================================
        // RenderIncludedCard
        // What this plan does have, from the same table the
        // gate reads.
        //
        // Rendered even for Free, where the list is empty of
        // paid lines: the empty case still gets a sentence,
        // because a card that vanishes teaches the reader
        // nothing while a card that says "the whole site, and
        // none of the paid extras" answers the question.
        // ==========================================
        private static string RenderIncludedCard(DocSnapEdition edition)
        {
            var sb = new StringBuilder(2048);
            sb.Append("<div class=\"ds-card\"><div class=\"ds-card-head\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null,
                "✅ In this plan", "✅ このプランに含まれるもの", "✅ توی این پلن هست"));
            sb.Append("</div>");

            sb.Append("<p class=\"ds-empty-note\">");
            sb.Append(HtmlPageBuilder.I18n("span", null,
                "Every edition exports the complete site: all Scenes, all components, every serialized field, the project health report, the packages page, search, and all three languages.",
                "すべてのエディションでサイト全体をエクスポートします。すべてのシーン、コンポーネント、シリアライズフィールド、プロジェクトのヘルスレポート、パッケージページ、検索、3 言語すべてが含まれます。",
                "همه‌ی نسخه‌ها کل سایت را خروجی می‌گیرند: همه‌ی سین‌ها، همه‌ی کامپوننت‌ها، هر فیلد سریالایزشده، گزارش سلامت پروژه، صفحه‌ی پکیج‌ها، جست‌وجو و هر سه زبان."));
            sb.Append("</p>");

            var unlocked = new List<DocSnapPitchLine>();
            foreach (DocSnapPitchLine line in DocSnapUpgradePitch.Lines)
            {
                if (DocSnapEditionMatrix.Allows(edition, line.Feature)) { unlocked.Add(line); }
            }

            if (unlocked.Count == 0)
            {
                sb.Append("<p class=\"ds-empty-note\">").Append(HtmlPageBuilder.I18n("span", null,
                    "None of the paid extras below are part of this export.",
                    "以下の有料機能は、このエクスポートには含まれていません。",
                    "هیچ‌کدام از قابلیت‌های پولیِ پایین توی این خروجی نیستند.")).Append("</p>");
            }
            else
            {
                sb.Append(RenderFeatureList(unlocked, true));
            }

            sb.Append("</div>\n");
            return sb.ToString();
        }

        // ==========================================
        // RenderMissingCard
        // What this plan does not have - named, priced with
        // the tier that actually unlocks it, and stated as a
        // fact about the folder rather than as a banner.
        //
        // Each line quotes the CHEAPEST tier that unlocks it,
        // for the same reason the export window's locked
        // toggles do: sending somebody to the $49.99 page for
        // a $19.99 feature loses the sale outright, and reads
        // as a bait and switch once they find out.
        // ==========================================
        private static string RenderMissingCard(DocSnapEdition edition)
        {
            List<DocSnapPitchLine> locked = DocSnapUpgradePitch.LockedFor(edition);

            var sb = new StringBuilder(2048);
            sb.Append("<div class=\"ds-card\"><div class=\"ds-card-head\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null,
                "🔒 Not in this plan", "🔒 このプランに含まれないもの", "🔒 توی این پلن نیست"));
            sb.Append("</div>");

            if (locked.Count == 0)
            {
                sb.Append("<div class=\"ds-callout ok\">").Append(HtmlPageBuilder.I18n("span", null,
                    "Nothing. This is the top plan — every feature Unity DocSnap has is in this export.",
                    "ありません。最上位プランのため、Unity DocSnap のすべての機能がこのエクスポートに含まれています。",
                    "هیچی. این بالاترین پلن است — هر قابلیتی که Unity DocSnap دارد توی این خروجی هست."))
                  .Append("</div></div>\n");
                return sb.ToString();
            }

            sb.Append("<p class=\"ds-empty-note\">").Append(HtmlPageBuilder.I18n("span", null,
                "These are absent from this export folder on purpose. They are not errors, and re-exporting will not produce them until the plan covers them.",
                "これらは意図的にこのエクスポートフォルダに含まれていません。エラーではなく、プランが対象にするまでは再エクスポートしても生成されません。",
                "این‌ها عمداً توی این فولدر خروجی نیستند. خطا نیستند و تا وقتی پلن شاملشان نشود، خروجی گرفتن دوباره هم نمی‌سازدشان.")).Append("</p>");

            sb.Append(RenderFeatureList(locked, false));

            // The middle tier has to be named where the locked list is
            // in front of the reader, not only on the website. Somebody
            // who wants the AI summaries and the Changes page and
            // nothing else, shown a single $49.99 price, buys nothing.
            if (DocSnapUpgradePitch.NextTierFor(edition) == DocSnapEdition.Plus)
            {
                sb.Append("<div class=\"ds-callout\">").Append(HtmlPageBuilder.I18n("span", null,
                    DocSnapUpgradePitch.PlusIsEnough("en"),
                    DocSnapUpgradePitch.PlusIsEnough("ja"),
                    DocSnapUpgradePitch.PlusIsEnough("fa"))).Append("</div>");
            }

            sb.Append("</div>\n");
            return sb.ToString();
        }

        // ==========================================
        // RenderFeatureList
        // One shared list for both halves, so the included
        // and missing sides can never be formatted - or
        // worded - differently.
        // ==========================================
        private static string RenderFeatureList(List<DocSnapPitchLine> lines, bool included)
        {
            var sb = new StringBuilder(2048);
            sb.Append("<ul class=\"ds-issue-list\">");
            foreach (DocSnapPitchLine line in lines)
            {
                DocSnapEdition tier = DocSnapEditionMatrix.Required(line.Feature);
                sb.Append("<li class=\"ds-issue-row\"><div class=\"ds-issue-link\">");
                sb.Append("<span class=\"ds-issue-icon\">").Append(line.Emoji).Append("</span>");
                sb.Append("<span class=\"ds-issue-main\">");
                sb.Append("<span class=\"ds-issue-kind\">")
                  .Append(HtmlPageBuilder.I18n("span", null, line.Title("en"), line.Title("ja"), line.Title("fa")))
                  .Append("</span>");
                sb.Append("<span class=\"ds-issue-where\">")
                  .Append(HtmlPageBuilder.I18n("span", null, line.Body("en"), line.Body("ja"), line.Body("fa")))
                  .Append("</span>");
                sb.Append("</span>");

                // The tier tag on an INCLUDED line would read as an
                // advert for something the reader already paid for, so
                // only the missing half carries a price.
                sb.Append("<span class=\"ds-issue-side\">");
                sb.Append(included
                    ? HtmlPageBuilder.BadgeRaw("mint", HtmlPageBuilder.I18n("span", null, "included", "含まれる", "هست"))
                    : HtmlPageBuilder.BadgeRaw("ghost", HtmlPageBuilder.Escape(
                        DocSnapEditionMatrix.DisplayName(tier) + " " + DocSnapUpgradePitch.Price(tier))));
                sb.Append("</span>");
                sb.Append("</div></li>");
            }
            sb.Append("</ul>");
            return sb.ToString();
        }

        // ==========================================
        // RenderVerifyCard
        // Where to check that the plan above is really the
        // plan that was paid for.
        //
        // This is the card somebody opens angry: they bought
        // Pro, the page says Free, and they want to know
        // whether the key is wrong, the machine is wrong, or
        // the export simply predates the activation. The
        // /license page answers all three - it lists the
        // machines a key is active on - so it is linked
        // first, before any buy link. Leading with "buy"
        // in front of somebody who has already paid is the
        // worst possible order.
        // ==========================================
        private static string RenderVerifyCard(DocSnapEdition edition)
        {
            var sb = new StringBuilder(1024);
            sb.Append("<div class=\"ds-card\"><div class=\"ds-card-head\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null,
                "🔎 Check this licence", "🔎 ライセンスを確認", "🔎 بررسی درستی لایسنس"));
            sb.Append("<a class=\"ds-card-action\" href=\"").Append(DocSnapConstants.LicenseManageUrl)
              .Append("\" target=\"_blank\" rel=\"noopener\">")
              .Append(HtmlPageBuilder.I18n("span", null,
                  "Open the licence page →", "ライセンスページを開く →", "باز کردن صفحه‌ی لایسنس →"))
              .Append("</a></div>");

            sb.Append("<p class=\"ds-empty-note\">").Append(HtmlPageBuilder.I18n("span", null,
                "Paste your key at the link above to see which machines it is activated on, and to release one if you have moved computer. If you hold a paid key and this export still says Free, the machine that produced it was not activated at the time — activate it in Unity under \"Unity DocSnap ▸ Licence & Pro Features\" and export again.",
                "上のリンクにキーを貼り付けると、そのキーが認証されているマシンを確認でき、別のマシンに移る場合は解放もできます。有料キーをお持ちなのにこのエクスポートが Free と表示される場合、作成時にそのマシンが認証されていません。Unity の「Unity DocSnap ▸ Licence & Pro Features」で認証してから、再度エクスポートしてください。",
                "کلیدت را توی لینک بالا بزن تا ببینی روی چه سیستم‌هایی فعال است و اگر سیستمت عوض شده، یکی را آزاد کنی. اگر کلید پولی داری و این خروجی باز هم Free نشان می‌دهد، یعنی سیستمی که این خروجی را ساخته در آن لحظه فعال نبوده — از منوی «Unity DocSnap ▸ Licence & Pro Features» توی یونیتی فعالش کن و دوباره خروجی بگیر."))
              .Append("</p>");

            sb.Append("<div class=\"ds-info-lines\">");
            sb.Append(InfoLine("🔗", "Licence page", "ライセンスページ", "صفحه‌ی لایسنس", Link(DocSnapConstants.LicenseManageUrl)));
            sb.Append(InfoLine("📖", "All three editions", "3 つのエディション", "هر سه نسخه", Link(DocSnapConstants.ProductUrl)));
            if (edition != DocSnapEdition.Pro)
            {
                DocSnapEdition next = DocSnapUpgradePitch.NextTierFor(edition);
                if (next != DocSnapEdition.Free)
                {
                    sb.Append(InfoLine("🛒", "Upgrade", "アップグレード", "ارتقا",
                        Link(DocSnapUpgradePitch.BuyUrl(next),
                            DocSnapEditionMatrix.DisplayName(next) + " · " + DocSnapUpgradePitch.Price(next))));
                }
            }
            sb.Append("</div></div>\n");
            return sb.ToString();
        }

        // ==========================================
        // Small helpers, mirroring DocSnapExportInfo's own
        // info-line markup so the Plan page and the Export
        // Info card read as the same kind of thing.
        // ==========================================
        private static string InfoLine(string emoji, string en, string ja, string fa, string valueHtml)
        {
            return "<div class=\"ds-info-line\"><span class=\"ds-info-key\">" + emoji + " "
                + HtmlPageBuilder.I18n("span", null, en, ja, fa)
                + "</span><span class=\"ds-info-val\">" + valueHtml + "</span></div>";
        }

        private static string Link(string url)
        {
            return Link(url, url);
        }

        private static string Link(string url, string label)
        {
            return "<a href=\"" + HtmlPageBuilder.Escape(url) + "\" target=\"_blank\" rel=\"noopener\">"
                + HtmlPageBuilder.Escape(label) + "</a>";
        }

        private static string Yes(bool value)
        {
            return "<span class=\"ds-pill " + (value ? "bool-true" : "bool-false") + "\">"
                + HtmlPageBuilder.I18n("span", null,
                    value ? "yes" : "no",
                    value ? "あり" : "なし",
                    value ? "بله" : "خیر")
                + "</span>";
        }
    }
}
