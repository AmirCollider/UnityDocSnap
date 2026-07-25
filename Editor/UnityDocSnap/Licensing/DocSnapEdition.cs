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
// menu refuses, and the difference between the two
// editions becomes whatever the last edit happened to
// leave behind.
//
// So the matrix is stated once, as data, and read
// everywhere. Adding a feature to Pro is one line in
// ProFeatures; moving one back to Free is deleting
// that line. Nothing else in the tool has an opinion.
//
// A note on what this is and is not. This is a
// licence check, not a copy-protection scheme. The
// package ships as C# source inside the customer's own
// Editor - anybody determined enough can edit this
// file and rebuild. That is a deliberate trade: the
// alternative is an obfuscated DLL that a Unity
// developer cannot read, cannot debug and cannot trust
// inside their own project, which costs every honest
// customer something real to inconvenience a dishonest
// one briefly. The gate exists so the split is
// unambiguous and the bookkeeping is honest, and it is
// built to fail towards Free rather than towards a
// broken Editor.
// ==========================================
namespace AmirCollider.UnityDocSnap.Editor.Licensing
{
    // ==========================================
    // DocSnapEdition
    // Free is the shipped default and needs no key at
    // all: install the package and every core export
    // works. Pro is unlocked by a licence key bound to
    // one machine (see DocSnapLicense).
    // ==========================================
    internal enum DocSnapEdition
    {
        Free,
        Pro
    }

    // ==========================================
    // DocSnapFeature
    // One entry per thing an edition may or may not be
    // allowed to do. Named after the user-visible
    // capability rather than the code path that
    // implements it, so a rename inside the exporter
    // never silently changes what somebody paid for.
    // ==========================================
    internal enum DocSnapFeature
    {
        // summary/*.md, summary/*.json and
        // summary/ai-bundle.md - the short, structured
        // documents written for an AI assistant rather
        // than for a browser. The single most valuable
        // thing the tool produces, and the reason a team
        // that already has documentation still wants it.
        AiSummaries,

        // changes.html - what moved between two exports,
        // with the old and new bytes of each changed file.
        ChangesPage,

        // More than DocSnapEditionLimits.FreeVersionFolders
        // snapshots kept side by side on the versions shelf.
        UnlimitedVersions,

        // "Update Previous Export" reusing the Scenes and
        // assets whose source has not changed. Free still
        // has the menu item; it simply re-scans everything,
        // which is what every version before 1.0.0 did.
        IncrementalUpdate,

        // source-files/ - verbatim copies of the real asset
        // bytes alongside their documentation.
        IncludeFiles,

        // project-backup.unitypackage - a whole-project
        // backup written into the version folder.
        ProjectBackup,

        // DocSnapAPI and -executeMethod: regenerating
        // documentation from CI instead of by remembering
        // to click a menu item.
        Automation,

        // The exported site carries your logo and no
        // "made with the free edition" badge.
        Whitelabel
    }

    // ==========================================
    // DocSnapEditionLimits
    // The numbers the Free edition is capped at. Kept
    // beside the matrix because they are the same kind
    // of fact, and because a cap with no name in the
    // code is a cap nobody can find later.
    // ==========================================
    internal static class DocSnapEditionLimits
    {
        // How many version folders the Free edition keeps on
        // the shelf at once.
        //
        // Three rather than one on purpose. One would hide the
        // feature completely: somebody who never sees a second
        // snapshot never learns that a version history is a
        // thing this tool does, and cannot miss what they have
        // not seen. Three is enough to use it, feel it working,
        // and then hit the wall on the fourth export - which is
        // the moment the upgrade means something.
        //
        // Hitting the cap is not an error. The export re-uses
        // the newest folder instead of adding a fourth, so the
        // work always completes; only the history stops growing.
        public const int FreeVersionFolders = 3;
    }

    internal static class DocSnapEditionMatrix
    {
        // ==========================================
        // ProFeatures
        // Everything Pro adds. Anything not on this list is
        // in both editions, which is the safer direction for
        // the default to point: a feature added later and
        // never classified stays available to everyone
        // instead of silently disappearing for the people
        // who were already using it.
        // ==========================================
        private static readonly DocSnapFeature[] ProFeatures =
        {
            DocSnapFeature.AiSummaries,
            DocSnapFeature.ChangesPage,
            DocSnapFeature.UnlimitedVersions,
            DocSnapFeature.IncrementalUpdate,
            DocSnapFeature.IncludeFiles,
            DocSnapFeature.ProjectBackup,
            DocSnapFeature.Automation,
            DocSnapFeature.Whitelabel
        };

        // ==========================================
        // Allows
        // Whether `edition` may use `feature`.
        // ==========================================
        public static bool Allows(DocSnapEdition edition, DocSnapFeature feature)
        {
            if (edition == DocSnapEdition.Pro) { return true; }

            foreach (DocSnapFeature pro in ProFeatures)
            {
                if (pro == feature) { return false; }
            }
            return true;
        }

        // ==========================================
        // DisplayName
        // What the edition is called in the UI, in the
        // exported site's badge, and in export-info.txt.
        // ==========================================
        public static string DisplayName(DocSnapEdition edition)
        {
            return edition == DocSnapEdition.Pro ? "Pro" : "Free";
        }
    }
}
