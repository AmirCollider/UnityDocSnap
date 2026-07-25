// ==========================================
// DocSnapEditionGate
// Where an export's requested options meet the
// edition that is actually licensed.
//
// The gate is one function rather than a check beside
// each feature, and that is the point. Scattered
// checks are how the export window ends up disabling
// something the service still writes, or the command
// line quietly producing an output the menu refuses -
// and with a paid tier, either of those is either
// giving away what somebody paid for or withholding
// what somebody paid for. Both are worse than a bug.
//
// Two rules the whole file follows:
//
//   • Clamp, never refuse. Asking for a Pro option in
//     the Free edition turns that option off and lets
//     the export run. Nobody's documentation fails to
//     exist because of a licence.
//
//   • Say what was turned off. Every clamp records a
//     line, and those lines are shown when the export
//     finishes. An export that silently produced less
//     than it was asked for is how somebody ships a
//     release believing they have a backup.
// ==========================================
using System.Collections.Generic;
using AmirCollider.UnityDocSnap.Editor.Export;

namespace AmirCollider.UnityDocSnap.Editor.Licensing
{
    // ==========================================
    // DocSnapGateReport
    // What the gate changed about this run, in the
    // customer's own words.
    // ==========================================
    internal sealed class DocSnapGateReport
    {
        public readonly List<string> Clamped = new List<string>();

        // True when the Free version cap redirected this export
        // onto an existing folder instead of adding a new one.
        // Carried separately because it changes where the output
        // lands, which is worth more than a footnote.
        public bool VersionCapped;

        public bool IsEmpty { get { return Clamped.Count == 0 && !VersionCapped; } }

        // ==========================================
        // Summary
        // The clamps as one block of text, ready to append
        // to the export-complete message. Empty when the
        // edition allowed everything that was asked for,
        // which is the normal case for both editions - Free
        // only trips this when somebody actively ticks a Pro
        // box.
        // ==========================================
        public string Summary(string lang)
        {
            if (Clamped.Count == 0) { return ""; }

            var sb = new System.Text.StringBuilder(256);
            sb.Append("\n\n");
            sb.Append(DocSnapText.Resolve(lang,
                "Skipped — not in your edition:",
                "スキップした項目(現在のエディションに含まれません):",
                "انجام نشد — توی نسخه‌ی فعلی‌ات نیست:"));
            foreach (string line in Clamped) { sb.Append("\n  • ").Append(line); }
            return sb.ToString();
        }
    }

    internal static class DocSnapEditionGate
    {
        // ==========================================
        // Tag
        // "The Changes page (changes.html)  — Plus $19.99"
        //
        // The tier and price go on the line rather than in a
        // sentence underneath, because the dialog lists several
        // of these at once and they do not all cost the same.
        // A single "upgrade for $49.99" footer under a list
        // where two items are $19.99 is the kind of thing a
        // customer discovers afterwards and resents.
        // ==========================================
        private static string Tag(DocSnapFeature feature, string text)
        {
            DocSnapEdition tier = DocSnapEditionMatrix.Required(feature);
            return text + "  — " + DocSnapEditionMatrix.DisplayName(tier)
                 + " " + DocSnapUpgradePitch.Price(tier);
        }

        // ==========================================
        // ClampOptions
        // Turns off every option this edition may not use
        // and reports each one.
        //
        // Mutates the options object it is handed, because
        // the alternative - returning a copy - leaves the
        // original in scope for a later line to read by
        // mistake, and "the copy is the real one" is a rule
        // somebody will eventually not know.
        // ==========================================
        public static DocSnapGateReport ClampOptions(DocSnapExportOptions options, string lang)
        {
            var report = new DocSnapGateReport();
            if (options == null) { return report; }

            if (options.includeFiles && !DocSnapLicense.Has(DocSnapFeature.IncludeFiles))
            {
                options.includeFiles = false;
                report.Clamped.Add(Tag(DocSnapFeature.IncludeFiles, DocSnapText.Resolve(lang,
                    "Copying the real asset bytes into source-files/",
                    "アセット本体を source-files/ にコピー",
                    "کپی بایت واقعی اسست‌ها توی source-files/")));
            }

            if (options.makeBackup && !DocSnapLicense.Has(DocSnapFeature.ProjectBackup))
            {
                options.makeBackup = false;
                report.Clamped.Add(Tag(DocSnapFeature.ProjectBackup, DocSnapText.Resolve(lang,
                    "The whole-project .unitypackage backup",
                    "プロジェクト全体の .unitypackage バックアップ",
                    "بک‌آپ ‎.unitypackage از کل پروژه")));
            }

            if (options.recordChanges && !DocSnapLicense.Has(DocSnapFeature.ChangesPage))
            {
                options.recordChanges = false;
                options.changesBaseVersion = "";
                report.Clamped.Add(Tag(DocSnapFeature.ChangesPage, DocSnapText.Resolve(lang,
                    "The Changes page (changes.html)",
                    "変更ページ(changes.html)",
                    "صفحه‌ی تغییرات (changes.html)")));
            }

            return report;
        }

        // ==========================================
        // WritesAiSummaries
        // Whether this export produces the summary/ folder
        // at all.
        //
        // Read at each write site rather than clamped into
        // the options, because unlike the switches above it
        // is not something the user asked for on this run -
        // it is a thing every export did until 1.0.0, and
        // the export path should keep asking the same
        // question in the same place.
        // ==========================================
        public static bool WritesAiSummaries
        {
            get { return DocSnapLicense.Has(DocSnapFeature.AiSummaries); }
        }

        public static bool CanRunIncrementally
        {
            get { return DocSnapLicense.Has(DocSnapFeature.IncrementalUpdate); }
        }

        // ==========================================
        // ResolveVersionCap
        // The Free edition's version-shelf limit, applied at
        // the moment a new folder would be allocated.
        //
        // At the cap the export is redirected onto the
        // NEWEST existing version and refreshes it in place,
        // rather than being refused or quietly deleting the
        // oldest snapshot. Every one of those alternatives
        // is worse:
        //
        //   refusing        - the customer's documentation
        //                     does not get made, over a
        //                     limit they hit by succeeding
        //                     at using the tool
        //   deleting oldest - the tool destroys a snapshot
        //                     the customer chose to keep, to
        //                     enforce a commercial rule.
        //                     Nothing licensing-related
        //                     should ever delete somebody's
        //                     data.
        //
        // Refreshing the newest one loses nothing that was
        // not about to be superseded anyway, keeps all three
        // kept snapshots intact, and puts the limit in front
        // of the person at the moment it costs them
        // something - which is the only moment an upgrade
        // means anything.
        //
        // Returns the version to write into, or null to
        // proceed with normal allocation.
        // ==========================================
        public static string ResolveVersionCap(string baseRoot, VersionsState registry, DocSnapGateReport report, string lang)
        {
            // The limit is per edition (Free 3, Plus 5, Pro
            // unlimited) rather than one number with a bool in
            // front of it, so this reads the same way for all three
            // and there is no tier with its own branch to forget.
            int limit = DocSnapEditionLimits.VersionFolders(DocSnapLicense.Edition);
            if (limit == DocSnapEditionLimits.Unlimited) { return null; }

            HashSet<string> existing = DocSnapVersioning.ExistingVersionNames(baseRoot, registry);
            if (existing.Count < limit) { return null; }

            string newest = DocSnapVersioning.NewestVersion(registry);

            // No newest version to fall back onto means the folders
            // on disk are not in the registry - a hand-copied output
            // tree, or one restored from a backup. Allocating
            // normally is the honest answer there: the cap exists to
            // limit the shelf this tool built, not to police a
            // directory somebody assembled themselves.
            if (string.IsNullOrEmpty(newest)) { return null; }

            // This one line does NOT go through Tag(), which would
            // quote Pro's price because that is where "unlimited"
            // lives. For a Free user standing at three snapshots
            // the honest next step is Plus, which keeps five for
            // $19.99 - and sending them to the $49.99 page for a
            // limit the cheaper tier already raises is both worse
            // selling and, once they notice, a small dishonesty.
            report.VersionCapped = true;
            report.Clamped.Add(DocSnapText.Resolve(lang,
                "Keeping more than " + limit + " snapshots — this export refreshed " + newest
                    + " instead of adding a new folder.  " + NextShelf(lang),
                "スナップショットを " + limit + " 件より多く保持すること — 今回は新しいフォルダを作らず "
                    + newest + " を更新しました。  " + NextShelf(lang),
                "نگه‌داشتن بیش از " + limit + " اسنپ‌شات — این خروجی به‌جای ساختن فولدر جدید، "
                    + newest + " را بروزرسانی کرد.  " + NextShelf(lang)));

            return newest;
        }

        // ==========================================
        // NextShelf
        // What the tiers above this one keep, named with their
        // prices, so the sentence answers "and what do I do about
        // it?" rather than only "this happened".
        // ==========================================
        private static string NextShelf(string lang)
        {
            if (DocSnapLicense.Edition == DocSnapEdition.Free)
            {
                return DocSnapText.Resolve(lang,
                    "Plus (" + DocSnapUpgradePitch.Price(DocSnapEdition.Plus) + ") keeps "
                        + DocSnapEditionLimits.PlusVersionFolders + "; Pro ("
                        + DocSnapUpgradePitch.Price(DocSnapEdition.Pro) + ") keeps every one.",
                    "Plus(" + DocSnapUpgradePitch.Price(DocSnapEdition.Plus) + ")は "
                        + DocSnapEditionLimits.PlusVersionFolders + " 件、Pro("
                        + DocSnapUpgradePitch.Price(DocSnapEdition.Pro) + ")はすべて保持します。",
                    "نسخه‌ی Plus (" + DocSnapUpgradePitch.Price(DocSnapEdition.Plus) + ") "
                        + DocSnapEditionLimits.PlusVersionFolders + " تا نگه می‌دارد و Pro ("
                        + DocSnapUpgradePitch.Price(DocSnapEdition.Pro) + ") همه را.");
            }

            return DocSnapText.Resolve(lang,
                "Pro (" + DocSnapUpgradePitch.Price(DocSnapEdition.Pro) + ") keeps every one.",
                "Pro(" + DocSnapUpgradePitch.Price(DocSnapEdition.Pro) + ")はすべて保持します。",
                "نسخه‌ی Pro (" + DocSnapUpgradePitch.Price(DocSnapEdition.Pro) + ") همه را نگه می‌دارد.");
        }
    }
}
