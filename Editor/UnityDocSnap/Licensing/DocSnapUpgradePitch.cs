// ==========================================
// DocSnapUpgradePitch
// What each paid tier adds, written once, in three
// languages.
//
// This copy appears in five places - the export window's
// panel, the licence window, the About window, Project
// Settings, and the dialog after an export - and it is
// the kind of text that gets edited in one of them and
// left stale in the other four. Worse, an upsell that
// describes a feature slightly differently in two places
// reads as careless, which is the last impression a paid
// product wants to make on somebody deciding whether to
// pay.
//
// So the list is data, in the same order everywhere, and
// each line is tied to the DocSnapFeature it sells. That
// last part matters twice over now that there are two
// paid tiers: the pitch cannot drift from the gate,
// because they are the same enum, and each line's PRICE
// is looked up from the matrix rather than written down
// here. A feature moved between tiers re-prices itself
// everywhere it is mentioned.
//
// Tone: every line names something the reader can picture
// doing, not a capability. "Hand your whole project to an
// AI in one paste" is a Tuesday afternoon; "AI-oriented
// export pipeline" is a bullet point on a slide nobody
// read.
// ==========================================
using System.Collections.Generic;

namespace AmirCollider.UnityDocSnap.Editor.Licensing
{
    // ==========================================
    // DocSnapPitchLine
    // One selling point: the feature it unlocks, an emoji,
    // a short title and one sentence, each in the three
    // languages the rest of the tool speaks.
    //
    // Which tier it belongs to is deliberately NOT a field.
    // It is asked of DocSnapEditionMatrix, so the pitch and
    // the gate cannot disagree about what something costs.
    // ==========================================
    internal sealed class DocSnapPitchLine
    {
        public readonly DocSnapFeature Feature;
        public readonly string Emoji;

        private readonly string _titleEn, _titleJa, _titleFa;
        private readonly string _bodyEn, _bodyJa, _bodyFa;

        public DocSnapPitchLine(DocSnapFeature feature, string emoji,
            string titleEn, string titleJa, string titleFa,
            string bodyEn, string bodyJa, string bodyFa)
        {
            Feature = feature;
            Emoji = emoji;
            _titleEn = titleEn; _titleJa = titleJa; _titleFa = titleFa;
            _bodyEn = bodyEn; _bodyJa = bodyJa; _bodyFa = bodyFa;
        }

        public DocSnapEdition Tier { get { return DocSnapEditionMatrix.Required(Feature); } }

        public string Title(string lang) { return DocSnapText.Resolve(lang, _titleEn, _titleJa, _titleFa); }
        public string Body(string lang) { return DocSnapText.Resolve(lang, _bodyEn, _bodyJa, _bodyFa); }
    }

    internal static class DocSnapUpgradePitch
    {
        // ==========================================
        // Lines
        // Ordered cheapest tier first, and within a tier by
        // how much somebody evaluating is likely to want the
        // thing - not by how hard it was to build. The two
        // Plus lines come first because they are the reason
        // most people are reading this list at all, and
        // because seeing them priced at $19.99 is the whole
        // point of having a middle tier.
        // ==========================================
        public static readonly DocSnapPitchLine[] Lines =
        {
            // ---- Plus ----
            new DocSnapPitchLine(DocSnapFeature.AiSummaries, "🤖",
                "AI-ready summaries", "AI 向けサマリー", "خروجی آماده‌ی هوش مصنوعی",
                "summary/ai-bundle.md turns your whole project into one paste — short, structured Markdown and JSON an assistant can actually read, instead of forty screenshots.",
                "summary/ai-bundle.md がプロジェクト全体を 1 回の貼り付けにまとめます。スクリーンショットを何十枚も送る代わりに、AI がそのまま読める短い Markdown と JSON を渡せます。",
                "‏summary/ai-bundle.md کل پروژه را می‌کند یک پیست: مارک‌داون و جیسون کوتاه و ساختارمند که دستیار هوش مصنوعی واقعاً می‌خواندش — به‌جای چهل تا اسکرین‌شات."),

            new DocSnapPitchLine(DocSnapFeature.ChangesPage, "🔁",
                "Changes page", "変更ページ", "صفحه‌ی تغییرات",
                "changes.html says exactly what moved between two exports — every changed file, with its old and new bytes side by side for review.",
                "changes.html は 2 つのエクスポート間で何が変わったかを示します。変更された各ファイルの変更前後のバイトを並べて確認できます。",
                "‏changes.html دقیقاً می‌گوید بین دو خروجی چه چیزی عوض شده — هر فایل تغییرکرده، با بایت‌های قبل و بعدش کنار هم برای بررسی."),

            // ---- Pro ----
            new DocSnapPitchLine(DocSnapFeature.UnlimitedVersions, "📚",
                "Unlimited version history", "無制限のバージョン履歴", "تاریخچه‌ی نامحدود نسخه‌ها",
                "Keep every snapshot you ever take, side by side on the versions shelf. Free and Plus keep the three most recent.",
                "取得したスナップショットをすべて、バージョン一覧に並べて保持できます。無料版と Plus は最新 3 件までです。",
                "هر اسنپ‌شاتی که گرفتی روی قفسه‌ی نسخه‌ها می‌ماند. نسخه‌ی رایگان و Plus سه تای آخر را نگه می‌دارند."),

            new DocSnapPitchLine(DocSnapFeature.IncrementalUpdate, "⚡",
                "Incremental updates", "差分更新", "بروزرسانی افزایشی",
                "\"Update Previous Export\" re-opens only the Scenes that actually changed. On a big project that is the difference between a coffee break and a click.",
                "「前回のエクスポートを更新」は実際に変更されたシーンだけを開き直します。大きなプロジェクトでは、休憩 1 回分がクリック 1 回分になります。",
                "«بروزرسانی خروجی قبلی» فقط سین‌هایی را دوباره باز می‌کند که واقعاً عوض شده‌اند. توی پروژه‌ی بزرگ یعنی فرق بین یک استراحت و یک کلیک."),

            new DocSnapPitchLine(DocSnapFeature.Automation, "🤖",
                "CI & command line", "CI・コマンドライン", "‏CI و خط فرمان",
                "DocSnapAPI and -executeMethod regenerate the docs on every merge, so they are never the thing somebody forgot to update.",
                "DocSnapAPI と -executeMethod により、マージのたびにドキュメントを再生成できます。更新を忘れる余地がなくなります。",
                "‏DocSnapAPI و ‎-executeMethod مستندات را روی هر مرج دوباره می‌سازند، پس دیگر آن چیزی نیست که یادشان برود بروزرسانی کنند."),

            new DocSnapPitchLine(DocSnapFeature.IncludeFiles, "📁",
                "File copies", "ファイル本体のコピー", "کپی خود فایل‌ها",
                "\"Export Full Project With Files\" puts the real asset bytes in source-files/, so the export is a complete record and not just a description of one.",
                "「ファイル付きでプロジェクト全体をエクスポート」はアセットの実バイトを source-files/ に格納します。説明だけでなく完全な記録になります。",
                "«خروجی کل پروژه با فایل‌ها» بایت واقعی اسست‌ها را می‌گذارد توی source-files/، پس خروجی یک سند کامل است نه فقط توضیح یک سند."),

            new DocSnapPitchLine(DocSnapFeature.ProjectBackup, "📦",
                "Whole-project backup", "プロジェクト全体のバックアップ", "بک‌آپ کل پروژه",
                "A project-backup.unitypackage next to the documentation, so a snapshot restores the project itself and not only the memory of it.",
                "ドキュメントの隣に project-backup.unitypackage を作成します。スナップショットが記録だけでなくプロジェクトそのものを復元します。",
                "یک project-backup.unitypackage کنار مستندات، تا اسنپ‌شات خودِ پروژه را برگرداند نه فقط خاطره‌اش را."),

            new DocSnapPitchLine(DocSnapFeature.CustomLogo, "✨",
                "Your own logo", "自社ロゴ", "لوگوی خودت",
                "Put your studio's logo in the exported site's sidebar — the export becomes something you can hand a client. (Any paid tier already removes the free-edition line from the footer.)",
                "エクスポートしたサイトのサイドバーに自社ロゴを表示できます。クライアントにそのまま渡せる成果物になります(有料版ではフッターの無料版表記はいずれも外れます)。",
                "لوگوی استودیوی خودت را بگذار توی سایدبار سایت خروجی — خروجی چیزی می‌شود که به کارفرما تحویل بدهی. (هر نسخه‌ی پولی، خط «نسخه‌ی رایگان» را از فوتر برمی‌دارد.)")
        };

        // ==========================================
        // Locked
        // The lines the current edition does not have, which
        // is the only list an upsell should ever show.
        // Advertising something the reader already has is how
        // a panel gets ignored.
        // ==========================================
        public static List<DocSnapPitchLine> Locked()
        {
            return LockedFor(DocSnapLicense.Edition);
        }

        public static List<DocSnapPitchLine> LockedFor(DocSnapEdition edition)
        {
            var locked = new List<DocSnapPitchLine>(Lines.Length);
            foreach (DocSnapPitchLine line in Lines)
            {
                if (!DocSnapEditionMatrix.Allows(edition, line.Feature)) { locked.Add(line); }
            }
            return locked;
        }

        // ==========================================
        // NextTier
        // The cheapest tier that would unlock something the
        // current edition does not have.
        //
        // The single most important number on any upsell
        // surface, and the easiest to get wrong. A Free user
        // looking at a locked Changes checkbox must be quoted
        // $19.99, not $49.99 - quoting the top price for a
        // feature that costs less is how a middle tier fails
        // to earn its existence, and it reads as a bait and
        // switch when they find out.
        //
        // Returns Free when nothing is locked, i.e. Pro.
        // ==========================================
        public static DocSnapEdition NextTier()
        {
            return NextTierFor(DocSnapLicense.Edition);
        }

        public static DocSnapEdition NextTierFor(DocSnapEdition edition)
        {
            var cheapest = DocSnapEdition.Free;
            foreach (DocSnapPitchLine line in Lines)
            {
                if (DocSnapEditionMatrix.Allows(edition, line.Feature)) { continue; }
                if (cheapest == DocSnapEdition.Free || line.Tier < cheapest) { cheapest = line.Tier; }
            }
            return cheapest;
        }

        // ==========================================
        // Price / BuyUrl
        // Per tier, from DocSnapConstants, so a price change
        // is one edit rather than a hunt through five windows.
        // ==========================================
        public static string Price(DocSnapEdition edition)
        {
            return edition == DocSnapEdition.Pro ? DocSnapConstants.ProPriceDisplay
                 : edition == DocSnapEdition.Plus ? DocSnapConstants.PlusPriceDisplay
                 : "";
        }

        public static string BuyUrl(DocSnapEdition edition)
        {
            return edition == DocSnapEdition.Pro ? DocSnapConstants.BuyProUrl
                 : edition == DocSnapEdition.Plus ? DocSnapConstants.BuyPlusUrl
                 : DocSnapConstants.ProductUrl;
        }

        // ==========================================
        // BuyLabel
        // "Get Plus — $19.99". One string rather than three
        // concatenations at each call site, because the ones
        // at each call site drifted.
        // ==========================================
        public static string BuyLabel(DocSnapEdition edition, string lang)
        {
            string name = DocSnapEditionMatrix.DisplayName(edition);
            string price = Price(edition);
            return DocSnapText.Resolve(lang,
                "Get " + name + " — " + price,
                name + " を購入 — " + price,
                "خرید " + name + " — " + price);
        }

        // ==========================================
        // Headline / Subhead
        // The one-liner above the list, and the terms under
        // the button.
        // ==========================================
        public static string Headline(string lang)
        {
            return DocSnapText.Resolve(lang,
                "Unity DocSnap — Plus & Pro",
                "Unity DocSnap — Plus と Pro",
                "‏Unity DocSnap — Plus و Pro");
        }

        public static string Subhead(string lang)
        {
            return DocSnapText.Resolve(lang,
                "Plus " + DocSnapConstants.PlusPriceDisplay + " · Pro " + DocSnapConstants.ProPriceDisplay
                    + ". One-off, one machine, no subscription.",
                "Plus " + DocSnapConstants.PlusPriceDisplay + " · Pro " + DocSnapConstants.ProPriceDisplay
                    + "。買い切り・1 台まで・サブスクリプションなし。",
                "‏Plus " + DocSnapConstants.PlusPriceDisplay + " · Pro " + DocSnapConstants.ProPriceDisplay
                    + ". خرید یک‌باره، یک سیستم، بدون اشتراک ماهانه.");
        }

        // ==========================================
        // PlusIsEnough
        // The line that makes the middle tier work.
        //
        // Somebody who wants the AI outputs and the Changes
        // page and nothing else would look at a single $49.99
        // price and buy nothing at all. Saying out loud that
        // those two have their own cheaper tier is the whole
        // reason it exists, and it has to be said on the
        // surfaces where the locked feature is in front of
        // them - not only on the website.
        // ==========================================
        public static string PlusIsEnough(string lang)
        {
            return DocSnapText.Resolve(lang,
                "Only want the AI summaries and the Changes page? Plus is " + DocSnapConstants.PlusPriceDisplay
                    + " and covers exactly those two.",
                "AI サマリーと変更ページだけが必要ですか?Plus は " + DocSnapConstants.PlusPriceDisplay
                    + " で、その 2 つを対象としています。",
                "فقط خروجی AI و صفحه‌ی تغییرات را می‌خواهی؟ نسخه‌ی Plus با " + DocSnapConstants.PlusPriceDisplay
                    + " دقیقاً همین دو تا را دارد.");
        }

        // ==========================================
        // WhatFreeKeeps
        // The reassurance half, and it is not filler.
        //
        // The reason to state it is that a locked feature
        // list, on its own, reads as "the thing you installed
        // is crippled". It is not: the free edition exports
        // the entire site - every Scene, every component,
        // every field, the health report, the search, all
        // three languages - and somebody who believes
        // otherwise stops evaluating before they ever see
        // what they would be buying.
        // ==========================================
        public static string WhatFreeKeeps(string lang)
        {
            return DocSnapText.Resolve(lang,
                "Free stays free and complete: every Scene, every component, every serialized field, the health report, the packages page, search, both skins and all three languages.",
                "無料版はこれまで通り完全に使えます。すべてのシーン、コンポーネント、シリアライズフィールド、ヘルスレポート、パッケージページ、検索、2 つのスキン、3 言語に対応します。",
                "نسخه‌ی رایگان کامل و رایگان می‌ماند: همه‌ی سین‌ها، همه‌ی کامپوننت‌ها، همه‌ی فیلدهای سریالایزشده، گزارش سلامت، صفحه‌ی پکیج‌ها، جست‌وجو، هر دو ظاهر و هر سه زبان.");
        }
    }
}
