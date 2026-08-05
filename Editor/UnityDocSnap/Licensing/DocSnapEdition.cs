// ==========================================
// DocSnapEdition / DocSnapFeature
// The one place that says what each edition can do.
//
// Every gate in the tool asks this file and nothing
// else. That matters more than it looks: a feature
// split spread across the export service, the export
// window, the API and the settings UI is a split that
// drifts - the window disables a checkbox the service
// still honours, or the CLI quietly does something the
// menu refuses, and the difference between the editions
// becomes whatever the last edit happened to leave
// behind.
//
// So the matrix is stated once, as data, and read
// everywhere. Moving a feature between tiers is one line
// in MinimumEdition. Nothing else in the tool has an
// opinion, and the upgrade pitch is generated from the
// same table, so the marketing copy cannot promise
// something the gate does not deliver.
//
// A note on what this is and is not. This is a licence
// check, not a copy-protection scheme. The package ships
// as C# source inside the customer's own Editor -
// anybody determined enough can edit this file and
// rebuild. That is a deliberate trade: the alternative
// is an obfuscated DLL that a Unity developer cannot
// read, cannot debug and cannot trust inside their own
// project, which costs every honest customer something
// real to inconvenience a dishonest one briefly. The
// gate exists so the split is unambiguous and the
// bookkeeping is honest, and it is built to fail towards
// Free rather than towards a broken Editor.
// ==========================================
using System.Collections.Generic;

namespace AmirCollider.UnityDocSnap.Editor.Licensing
{
    // ==========================================
    // DocSnapEdition
    // Three tiers, and the numeric order is load-bearing:
    // Allows() compares them, so a higher tier
    // automatically has everything the tiers below it do.
    // Any new tier has to slot into that order rather than
    // being appended.
    //
    //   Free  - the shipped default. No key, no account, no
    //           network: install the package and the whole
    //           exporter works.
    //   Plus  - the two outputs people ask for by name: the
    //           AI-ready summaries and the Changes page.
    //           Aimed at somebody who wants exactly those
    //           and has no use for CI, backups or file
    //           copies - and who would otherwise have
    //           bought nothing.
    //   Pro   - everything, including automation, backups,
    //           unlimited history and white-labelling.
    // ==========================================
    internal enum DocSnapEdition
    {
        Free = 0,
        Plus = 1,
        Pro = 2
    }

    // ==========================================
    // DocSnapFeature
    // One entry per thing an edition may or may not be
    // allowed to do. Named after the user-visible
    // capability rather than the code path that implements
    // it, so a rename inside the exporter never silently
    // changes what somebody paid for.
    // ==========================================
    internal enum DocSnapFeature
    {
        // summary/*.md, summary/*.json and
        // summary/ai-bundle.md - the short, structured
        // documents written for an AI assistant rather than
        // for a browser. The single most requested output,
        // and half of what Plus exists to sell.
        AiSummaries,

        // changes.html - what moved between two exports,
        // with the old and new bytes of each changed file.
        // The other half of Plus.
        ChangesPage,

        // More than DocSnapEditionLimits.FreeVersionFolders
        // snapshots kept side by side on the versions shelf.
        UnlimitedVersions,

        // "Update Previous Export" reusing the Scenes and
        // assets whose source has not changed. Free and Plus
        // still have the menu item; it simply re-scans
        // everything, which is what every version before
        // 1.0.0 did.
        IncrementalUpdate,

        // source-files/ - verbatim copies of the real asset
        // bytes alongside their documentation.
        IncludeFiles,

        // project-backup.unitypackage - a whole-project
        // backup written into the version folder.
        ProjectBackup,

        // DocSnapAPI and -executeMethod: regenerating
        // documentation from CI instead of by remembering to
        // click a menu item.
        Automation,

        // The exported site's sidebar carries your logo
        // instead of the built-in mark.
        //
        // Note that this is NOT what removes the free-edition
        // line from the footer. Any paid tier removes that,
        // because telling somebody who paid that they are
        // running the free edition is simply false - see
        // DocSnapEditionMatrix.ShowsFreeEditionBadge.
        CustomLogo,

        // Deleting a snapshot from the versions shelf, and -
        // once the shelf is empty - the output folder itself.
        //
        // Pro only, and the reason is the shelf cap rather than
        // the deleting. Free keeps three snapshots and Plus five;
        // if either could delete one, the cap would be a speed
        // bump rather than a limit, and the tier would be worth
        // what the effort of clicking twice is worth. Pro has no
        // cap, so there is nothing there for the button to
        // undermine - it is house-keeping for somebody with an
        // unlimited shelf, which is precisely who accumulates
        // enough snapshots to want one gone.
        //
        // Deleting the output folder BY HAND has always worked
        // and still does. It leaves the registry in Library/
        // untouched, so the shelf count does not move - which is
        // deliberate, and is what stops the same manoeuvre being
        // a way around the cap.
        ManageVersions
    }

    // ==========================================
    // DocSnapEditionLimits
    // The numbers the lower tiers are capped at. Kept
    // beside the matrix because they are the same kind of
    // fact, and because a cap with no name in the code is a
    // cap nobody can find later.
    // ==========================================
    internal static class DocSnapEditionLimits
    {
        // How many version folders each edition keeps on the
        // shelf at once.
        //
        // Free gets three rather than one on purpose. One would
        // hide the feature completely: somebody who never sees a
        // second snapshot never learns that a version history is
        // a thing this tool does, and cannot miss what they have
        // not seen. Three is enough to use it, feel it working,
        // and then hit the wall on the fourth export - which is
        // the moment an upgrade means something.
        //
        // Plus gets five rather than three, and the difference
        // between those two numbers is the entire reason this is
        // a per-edition value instead of one constant. A paid
        // tier that behaves identically to the free one in some
        // visible respect teaches the customer that paying
        // changed less than they thought - which is a bad thing
        // to teach somebody you would like to sell the NEXT tier
        // to. Five is a real, noticeable step up and still
        // leaves unlimited history as a reason to buy Pro.
        //
        // Hitting the cap is never an error. The export re-uses
        // the newest folder instead of adding another, so the
        // work always completes; only the history stops growing,
        // and nothing already on the shelf is deleted.
        public const int FreeVersionFolders = 3;
        public const int PlusVersionFolders = 5;

        // Pro. int.MaxValue rather than a sentinel like 0 or -1,
        // so the comparison in ResolveVersionCap reads the same
        // way for every edition and there is no special case to
        // forget.
        public const int Unlimited = int.MaxValue;

        // ==========================================
        // VersionFolders
        // The shelf limit for one edition.
        //
        // Every caller asks this rather than picking a constant,
        // so a limit can never be quoted by the export window and
        // enforced differently by the gate - which is exactly the
        // sort of disagreement a customer reports as "it said
        // five and then did three".
        // ==========================================
        public static int VersionFolders(DocSnapEdition edition)
        {
            switch (edition)
            {
                case DocSnapEdition.Pro: return Unlimited;
                case DocSnapEdition.Plus: return PlusVersionFolders;
                default: return FreeVersionFolders;
            }
        }
    }

    internal static class DocSnapEditionMatrix
    {
        // ==========================================
        // MinimumEdition
        // The cheapest tier that unlocks each feature.
        //
        // This is the entire commercial design of the tool, in
        // one table. Reading it top to bottom is the fastest
        // way to answer "what am I actually buying?", which is
        // exactly the question a scattered set of checks makes
        // unanswerable.
        //
        // A feature with no entry here is available to
        // everyone. That default points the safer way: a
        // capability added later and never classified stays
        // available to the people already using it, instead of
        // silently disappearing.
        // ==========================================
        private static readonly Dictionary<DocSnapFeature, DocSnapEdition> MinimumEdition =
            new Dictionary<DocSnapFeature, DocSnapEdition>
            {
                // --- Plus (and therefore Pro) ---
                { DocSnapFeature.AiSummaries,       DocSnapEdition.Plus },
                { DocSnapFeature.ChangesPage,       DocSnapEdition.Plus },

                // --- Pro only ---
                { DocSnapFeature.UnlimitedVersions, DocSnapEdition.Pro },
                { DocSnapFeature.IncrementalUpdate, DocSnapEdition.Pro },
                { DocSnapFeature.IncludeFiles,      DocSnapEdition.Pro },
                { DocSnapFeature.ProjectBackup,     DocSnapEdition.Pro },
                { DocSnapFeature.Automation,        DocSnapEdition.Pro },
                { DocSnapFeature.CustomLogo,        DocSnapEdition.Pro },
                { DocSnapFeature.ManageVersions,    DocSnapEdition.Pro }
            };

        // ==========================================
        // Required
        // The cheapest edition that unlocks `feature`, which
        // is what the UI needs in order to say "PLUS" rather
        // than "PRO" on a locked control. Getting that wrong
        // sends somebody to the wrong checkout page.
        // ==========================================
        public static DocSnapEdition Required(DocSnapFeature feature)
        {
            DocSnapEdition minimum;
            return MinimumEdition.TryGetValue(feature, out minimum) ? minimum : DocSnapEdition.Free;
        }

        // ==========================================
        // Allows
        // Whether `edition` may use `feature`.
        //
        // A plain >= comparison, which is why the enum's
        // numeric order is not decoration: Pro gets everything
        // Plus gets without either tier having to be listed
        // twice, and a tier inserted in the middle later
        // inherits the same rule for free.
        // ==========================================
        public static bool Allows(DocSnapEdition edition, DocSnapFeature feature)
        {
            return edition >= Required(feature);
        }

        // ==========================================
        // ShowsFreeEditionBadge
        // Whether the generated site carries the small
        // "free edition" line in its footer.
        //
        // Driven by "did this person pay anything?" rather
        // than by a feature flag, because it is not a feature -
        // it is a statement about the export, and on a Plus
        // export it would be a false one. Somebody who paid
        // $19.99 is not running the free edition, and a paid
        // product that says otherwise on every page it
        // generates is picking a fight with its own customer.
        //
        // Swapping the built-in mark for your own logo is a
        // separate thing and stays on Pro
        // (DocSnapFeature.CustomLogo).
        // ==========================================
        public static bool ShowsFreeEditionBadge(DocSnapEdition edition)
        {
            return edition == DocSnapEdition.Free;
        }

        // ==========================================
        // DisplayName
        // What the edition is called in the UI, in the
        // exported site's badge, and in export-info.txt.
        // ==========================================
        public static string DisplayName(DocSnapEdition edition)
        {
            switch (edition)
            {
                case DocSnapEdition.Pro: return "Pro";
                case DocSnapEdition.Plus: return "Plus";
                default: return "Free";
            }
        }

        // ==========================================
        // Parse
        // The tier name carried in a licence token, turned
        // into an edition.
        //
        // Anything unrecognised is Free. A token from a future
        // version naming a tier this build has never heard of
        // must not be guessed at generously - the safe reading
        // of "I do not know what this is" is the one that
        // grants nothing.
        // ==========================================
        public static DocSnapEdition Parse(string tier)
        {
            if (tier == "pro") { return DocSnapEdition.Pro; }
            if (tier == "plus") { return DocSnapEdition.Plus; }
            return DocSnapEdition.Free;
        }

        // ==========================================
        // TokenTier
        // The inverse: the string the Worker puts in a token.
        // Kept beside Parse so the two can never disagree.
        // ==========================================
        public static string TokenTier(DocSnapEdition edition)
        {
            switch (edition)
            {
                case DocSnapEdition.Pro: return "pro";
                case DocSnapEdition.Plus: return "plus";
                default: return "free";
            }
        }
    }
}
