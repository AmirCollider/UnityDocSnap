// ==========================================
// DocSnapProPitch
// What Pro adds, written once, in three languages.
//
// This copy appears in five places - the export
// window's panel, the licence window, the About
// window, Project Settings, and the dialog after a
// free export - and it is the kind of text that gets
// edited in one of them and left stale in the other
// four. Worse, an upsell that describes a feature
// slightly differently in two places reads as
// careless, which is the last impression a paid
// product wants to make on somebody deciding whether
// to pay.
//
// So the list is data, in the same order everywhere,
// with each line tied to the DocSnapFeature it is
// selling. That last part matters: the pitch cannot
// drift from the gate, because they are the same
// enum. A feature moved into Free stops being
// advertised automatically.
//
// Tone: every line names something the reader can
// picture doing, not a capability. "Hand your whole
// project to an AI in one paste" is a Tuesday
// afternoon; "AI-oriented export pipeline" is a
// bullet point on a slide nobody read.
// ==========================================
using System.Collections.Generic;

namespace AmirCollider.UnityDocSnap.Editor.Licensing
{
    // ==========================================
    // DocSnapProPitchLine
    // One selling point: the feature it unlocks, an
    // emoji, a short title and one sentence, each in the
    // three languages the rest of the tool speaks.
    // ==========================================
    internal sealed class DocSnapProPitchLine
    {
        public readonly DocSnapFeature Feature;
        public readonly string Emoji;

        private readonly string _titleEn, _titleJa, _titleFa;
        private readonly string _bodyEn, _bodyJa, _bodyFa;

        public DocSnapProPitchLine(DocSnapFeature feature, string emoji,
            string titleEn, string titleJa, string titleFa,
            string bodyEn, string bodyJa, string bodyFa)
        {
            Feature = feature;
            Emoji = emoji;
            _titleEn = titleEn; _titleJa = titleJa; _titleFa = titleFa;
            _bodyEn = bodyEn; _bodyJa = bodyJa; _bodyFa = bodyFa;
        }

        public string Title(string lang) { return DocSnapText.Resolve(lang, _titleEn, _titleJa, _titleFa); }
        public string Body(string lang) { return DocSnapText.Resolve(lang, _bodyEn, _bodyJa, _bodyFa); }
    }

    internal static class DocSnapProPitch
    {
        // ==========================================
        // Lines
        // Ordered by how much somebody evaluating the free
        // edition is likely to want the thing, not by how
        // hard it was to build. The AI outputs are first
        // because they are the reason most people are
        // reading this list at all.
        // ==========================================
        public static readonly DocSnapProPitchLine[] Lines =
        {
            new DocSnapProPitchLine(DocSnapFeature.AiSummaries, "🤖",
                "AI-ready summaries", "AI 向けサマリー", "خروجی آماده‌ی هوش مصنوعی",
                "summary/ai-bundle.md turns your whole project into one paste — short, structured Markdown and JSON an assistant can actually read, instead of forty screenshots.",
                "summary/ai-bundle.md がプロジェクト全体を 1 回の貼り付けにまとめます。スクリーンショットを何十枚も送る代わりに、AI がそのまま読める短い Markdown と JSON を渡せます。",
                "‏summary/ai-bundle.md کل پروژه را می‌کند یک پیست: مارک‌داون و جیسون کوتاه و ساختارمند که دستیار هوش مصنوعی واقعاً می‌خواندش — به‌جای چهل تا اسکرین‌شات."),

            new DocSnapProPitchLine(DocSnapFeature.ChangesPage, "🔁",
                "Changes page", "変更ページ", "صفحه‌ی تغییرات",
                "changes.html says exactly what moved between two exports — every changed file, with its old and new bytes side by side for review.",
                "changes.html は 2 つのエクスポート間で何が変わったかを示します。変更された各ファイルの変更前後のバイトを並べて確認できます。",
                "‏changes.html دقیقاً می‌گوید بین دو خروجی چه چیزی عوض شده — هر فایل تغییرکرده، با بایت‌های قبل و بعدش کنار هم برای بررسی."),

            new DocSnapProPitchLine(DocSnapFeature.UnlimitedVersions, "📚",
                "Unlimited version history", "無制限のバージョン履歴", "تاریخچه‌ی نامحدود نسخه‌ها",
                "Keep every snapshot you ever take, side by side on the versions shelf. Free keeps the three most recent.",
                "取得したスナップショットをすべて、バージョン一覧に並べて保持できます。無料版は最新 3 件までです。",
                "هر اسنپ‌شاتی که گرفتی روی قفسه‌ی نسخه‌ها می‌ماند. نسخه‌ی رایگان سه تای آخر را نگه می‌دارد."),

            new DocSnapProPitchLine(DocSnapFeature.IncrementalUpdate, "⚡",
                "Incremental updates", "差分更新", "بروزرسانی افزایشی",
                "\"Update Previous Export\" re-opens only the Scenes that actually changed. On a big project that is the difference between a coffee break and a click.",
                "「前回のエクスポートを更新」は実際に変更されたシーンだけを開き直します。大きなプロジェクトでは、休憩 1 回分がクリック 1 回分になります。",
                "«بروزرسانی خروجی قبلی» فقط سین‌هایی را دوباره باز می‌کند که واقعاً عوض شده‌اند. توی پروژه‌ی بزرگ یعنی فرق بین یک استراحت و یک کلیک."),

            new DocSnapProPitchLine(DocSnapFeature.Automation, "🤖",
                "CI & command line", "CI・コマンドライン", "‏CI و خط فرمان",
                "DocSnapAPI and -executeMethod regenerate the docs on every merge, so they are never the thing somebody forgot to update.",
                "DocSnapAPI と -executeMethod により、マージのたびにドキュメントを再生成できます。更新を忘れる余地がなくなります。",
                "‏DocSnapAPI و ‎-executeMethod مستندات را روی هر مرج دوباره می‌سازند، پس دیگر آن چیزی نیست که یادشان برود بروزرسانی کنند."),

            new DocSnapProPitchLine(DocSnapFeature.IncludeFiles, "📁",
                "File copies", "ファイル本体のコピー", "کپی خود فایل‌ها",
                "\"Export Full Project With Files\" puts the real asset bytes in source-files/, so the export is a complete record and not just a description of one.",
                "「ファイル付きでプロジェクト全体をエクスポート」はアセットの実バイトを source-files/ に格納します。説明だけでなく完全な記録になります。",
                "«خروجی کل پروژه با فایل‌ها» بایت واقعی اسست‌ها را می‌گذارد توی source-files/، پس خروجی یک سند کامل است نه فقط توضیح یک سند."),

            new DocSnapProPitchLine(DocSnapFeature.ProjectBackup, "📦",
                "Whole-project backup", "プロジェクト全体のバックアップ", "بک‌آپ کل پروژه",
                "A project-backup.unitypackage next to the documentation, so a snapshot restores the project itself and not only the memory of it.",
                "ドキュメントの隣に project-backup.unitypackage を作成します。スナップショットが記録だけでなくプロジェクトそのものを復元します。",
                "یک project-backup.unitypackage کنار مستندات، تا اسنپ‌شات خودِ پروژه را برگرداند نه فقط خاطره‌اش را."),

            new DocSnapProPitchLine(DocSnapFeature.Whitelabel, "✨",
                "Your logo, no badge", "自社ロゴ、バッジなし", "لوگوی خودت، بدون بَج",
                "Put your own logo in the sidebar and drop the \"made with the free edition\" line — the export is something you can hand a client.",
                "サイドバーに自社ロゴを表示し、「無料版で作成」の表記を外せます。クライアントにそのまま渡せる成果物になります。",
                "لوگوی خودت را بگذار توی سایدبار و خط «ساخته‌شده با نسخه‌ی رایگان» را بردار — خروجی چیزی می‌شود که به کارفرما تحویل بدهی.")
        };

        // ==========================================
        // Locked
        // The lines that are still locked for the current
        // edition, which is the only list an upsell should
        // ever show. Advertising something the reader
        // already has is how a panel gets ignored.
        // ==========================================
        public static List<DocSnapProPitchLine> Locked()
        {
            var locked = new List<DocSnapProPitchLine>(Lines.Length);
            foreach (DocSnapProPitchLine line in Lines)
            {
                if (!DocSnapLicense.Has(line.Feature)) { locked.Add(line); }
            }
            return locked;
        }

        // ==========================================
        // Headline / Subhead
        // The one-liner above the list, and the price line
        // under the button.
        // ==========================================
        public static string Headline(string lang)
        {
            return DocSnapText.Resolve(lang,
                "Unity DocSnap Pro",
                "Unity DocSnap Pro",
                "‏Unity DocSnap Pro");
        }

        public static string Subhead(string lang)
        {
            return DocSnapText.Resolve(lang,
                "One-off purchase, one machine, no subscription. " + DocSnapConstants.ProPriceDisplay + ".",
                "買い切り・1 台まで・サブスクリプションなし。" + DocSnapConstants.ProPriceDisplay + "。",
                "خرید یک‌باره، یک سیستم، بدون اشتراک ماهانه. " + DocSnapConstants.ProPriceDisplay + ".");
        }

        // ==========================================
        // WhatFreeKeeps
        // The reassurance half, and it is not filler.
        //
        // The reason to state it is that a locked feature
        // list, on its own, reads as "the thing you
        // installed is crippled". It is not: the free
        // edition exports the entire site - every Scene,
        // every component, every field, the health report,
        // the search, all three languages - and somebody who
        // believes otherwise stops evaluating before they
        // ever see what they would be buying.
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
