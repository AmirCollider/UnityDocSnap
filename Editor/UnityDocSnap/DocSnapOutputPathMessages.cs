// ==========================================
// DocSnapOutputPathMessages
// The wording for every DocSnapOutputPathVerdict, kept
// away from the rule that produces it.
//
// DocSnapOutputPath is pure path logic and has no
// business knowing what language the person reading the
// result speaks; the settings UI, the export window and
// the command line all need to say the same thing in
// theirs. So the rule returns a verdict and this
// translates it - through DocSnapText, so a language
// added to the registry gets these messages the same
// way it gets everything else.
//
// Each message says three things, because a refusal
// that only says "no" is a support ticket: what is
// wrong, WHY it is wrong for this particular folder,
// and what to do instead.
// ==========================================
namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapOutputPathMessages
    {
        // ==========================================
        // Describe
        // One sentence explaining a rejection, in the given
        // language. Returns "" for Ok, so a caller can use
        // emptiness as the pass condition.
        // ==========================================
        public static string Describe(DocSnapOutputPathVerdict verdict, string languageCode)
        {
            switch (verdict)
            {
                case DocSnapOutputPathVerdict.Ok:
                    return "";

                case DocSnapOutputPathVerdict.InsideAssets:
                    return DocSnapText.Resolve(languageCode,
                        "The output folder cannot be inside Assets. Unity would import every exported file and generate a .meta for it, and the next export would then document the previous one. Pick a folder outside Assets - the default UnityDocSnap_Output, next to Assets, is a good one.",
                        "出力フォルダを Assets の中に置くことはできません。書き出したファイルがすべて Unity にインポートされて .meta が生成され、次回のエクスポートが前回の出力自身をドキュメント化してしまいます。Assets の外のフォルダを指定してください（既定の UnityDocSnap_Output が適切です）。",
                        "پوشه‌ی خروجی نمی‌تواند داخل Assets باشد. یونیتی همه‌ی فایل‌های خروجی را ایمپورت می‌کند و برایشان ‎.meta می‌سازد، و اکسپورت بعدی خروجیِ قبلی را مستند می‌کند. یک پوشه بیرون از Assets انتخاب کنید — پیش‌فرض یعنی UnityDocSnap_Output کنار Assets گزینه‌ی خوبی است.");

                case DocSnapOutputPathVerdict.InsideUnityManagedFolder:
                    return DocSnapText.Resolve(languageCode,
                        "The output folder cannot be inside Library, Temp, Logs or obj. Unity deletes and regenerates those, so an export written there is not a snapshot you can keep.",
                        "出力フォルダを Library・Temp・Logs・obj の中に置くことはできません。これらは Unity が削除して作り直すため、そこに書き出したエクスポートは残りません。",
                        "پوشه‌ی خروجی نمی‌تواند داخل Library، Temp، Logs یا obj باشد. یونیتی این‌ها را پاک و بازسازی می‌کند، پس اکسپورتی که آن‌جا نوشته شود ماندگار نیست.");

                case DocSnapOutputPathVerdict.InsideProjectSettings:
                    return DocSnapText.Resolve(languageCode,
                        "The output folder cannot be inside ProjectSettings. That folder is your project's configuration, and an export does not belong in it.",
                        "出力フォルダを ProjectSettings の中に置くことはできません。プロジェクトの設定用フォルダであり、エクスポート先には適しません。",
                        "پوشه‌ی خروجی نمی‌تواند داخل ProjectSettings باشد. آن پوشه پیکربندی پروژه است و جای اکسپورت نیست.");

                case DocSnapOutputPathVerdict.InsidePackages:
                    return DocSnapText.Resolve(languageCode,
                        "The output folder cannot be inside Packages. The package manager owns that folder and rewrites it.",
                        "出力フォルダを Packages の中に置くことはできません。Package Manager が管理し、書き換えるフォルダです。",
                        "پوشه‌ی خروجی نمی‌تواند داخل Packages باشد. مدیریت آن پوشه با Package Manager است و بازنویسی‌اش می‌کند.");

                case DocSnapOutputPathVerdict.IsProjectRoot:
                    return DocSnapText.Resolve(languageCode,
                        "The output folder cannot be the project root itself. Use a folder inside it, such as the default UnityDocSnap_Output.",
                        "出力フォルダにプロジェクトのルートそのものは指定できません。既定の UnityDocSnap_Output のように、その中のフォルダを指定してください。",
                        "پوشه‌ی خروجی نمی‌تواند خودِ ریشه‌ی پروژه باشد. یک پوشه داخل آن انتخاب کنید، مثل پیش‌فرض UnityDocSnap_Output.");

                case DocSnapOutputPathVerdict.ContainsProject:
                    return DocSnapText.Resolve(languageCode,
                        "The output folder cannot be a folder that contains your project. Unity DocSnap manages the contents of its output folder, and that would put your project inside something it manages.",
                        "出力フォルダに、プロジェクトを含んでいるフォルダは指定できません。Unity DocSnap は出力フォルダの中身を管理するため、プロジェクト自体が管理対象の中に入ってしまいます。",
                        "پوشه‌ی خروجی نمی‌تواند پوشه‌ای باشد که پروژه‌ی شما داخل آن است. Unity DocSnap محتوای پوشه‌ی خروجی‌اش را مدیریت می‌کند و این یعنی پروژه‌ی شما داخل چیزی قرار می‌گیرد که مدیریت می‌شود.");

                case DocSnapOutputPathVerdict.IsFilesystemRoot:
                    return DocSnapText.Resolve(languageCode,
                        "The output folder cannot be the root of a drive. Pick a folder inside it.",
                        "出力フォルダにドライブのルートは指定できません。その中のフォルダを指定してください。",
                        "پوشه‌ی خروجی نمی‌تواند ریشه‌ی درایو باشد. یک پوشه داخل آن انتخاب کنید.");

                case DocSnapOutputPathVerdict.Empty:
                case DocSnapOutputPathVerdict.Unusable:
                default:
                    return DocSnapText.Resolve(languageCode,
                        "That output folder is not a usable path. Leave the field empty to use the default UnityDocSnap_Output next to Assets.",
                        "その出力フォルダはパスとして使用できません。空欄にすると、Assets の隣の既定の UnityDocSnap_Output が使われます。",
                        "آن مسیر خروجی قابل استفاده نیست. اگر فیلد را خالی بگذارید، پیش‌فرض یعنی UnityDocSnap_Output کنار Assets استفاده می‌شود.");
            }
        }

        // ==========================================
        // DescribeForEditor
        // The same message in the language the person at the
        // Editor chose for Unity DocSnap's own windows -
        // which is not necessarily the language the exported
        // site is being rendered in.
        // ==========================================
        public static string DescribeForEditor(DocSnapOutputPathVerdict verdict)
        {
            return Describe(verdict, DocSnapSettings.WindowLanguage);
        }
    }
}
