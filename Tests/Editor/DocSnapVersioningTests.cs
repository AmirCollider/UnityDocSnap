// ==========================================
// DocSnapVersioningTests
// Version numbering decides which folder an
// export lands in. It had no tests at all, which
// is uncomfortable for logic with a hand-rolled
// 0-9 roll-over and a custom-name escape hatch:
// getting it wrong means either overwriting a
// previous export or spraying folders.
// ==========================================
using System.Collections.Generic;
using AmirCollider.UnityDocSnap.Editor.Export;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class DocSnapVersioningTests
    {
        private static VersionsState StateWith(params string[] versions)
        {
            var state = new VersionsState();
            foreach (string v in versions) { state.versions.Add(new VersionSnapshot { version = v }); }
            return state;
        }

        [Test]
        public void ParseVersion_ReadsTheStandardShape()
        {
            int[] parsed = DocSnapVersioning.ParseVersion("V1.2.3");
            Assert.IsNotNull(parsed);
            Assert.AreEqual(new[] { 1, 2, 3 }, parsed);
        }

        [Test]
        public void ParseVersion_RejectsCustomNames()
        {
            Assert.IsNull(DocSnapVersioning.ParseVersion("release-candidate"));
            Assert.IsNull(DocSnapVersioning.ParseVersion("V1.2"));
            Assert.IsNull(DocSnapVersioning.ParseVersion("V1.2.x"));
            Assert.IsNull(DocSnapVersioning.ParseVersion(""));
        }

        [Test]
        public void NextVersion_EmptyShelf_StartsAtV100()
        {
            Assert.AreEqual("V1.0.0", DocSnapVersioning.NextVersion(null, StateWith()));
        }

        [Test]
        public void NextVersion_IncrementsPatch()
        {
            Assert.AreEqual("V1.0.4", DocSnapVersioning.NextVersion(null, StateWith("V1.0.0", "V1.0.3")));
        }

        [Test]
        public void NextVersion_RollsPatchIntoMinorAndMinorIntoMajor()
        {
            Assert.AreEqual("V1.1.0", DocSnapVersioning.NextVersion(null, StateWith("V1.0.9")));
            Assert.AreEqual("V2.0.0", DocSnapVersioning.NextVersion(null, StateWith("V1.9.9")));
        }

        [Test]
        public void NextVersion_NeverReturnsAnExistingName()
        {
            VersionsState state = StateWith("V1.0.0", "V1.0.1", "V1.0.2", "milestone");
            string next = DocSnapVersioning.NextVersion(null, state);

            Assert.AreEqual("V1.0.3", next);
            foreach (VersionSnapshot v in state.versions)
            {
                Assert.AreNotEqual(v.version, next, "The sequence must never land on a folder that already exists.");
            }
        }

        [Test]
        public void NextVersion_IgnoresCustomNamesWhenChoosingTheHighest()
        {
            // A custom name is not part of the numeric sequence, so it
            // must not be able to drag the next number anywhere.
            Assert.AreEqual("V1.0.1", DocSnapVersioning.NextVersion(null, StateWith("V1.0.0", "zzz-final")));
        }

        [Test]
        public void CompareVersions_OrdersNumericallyNotAlphabetically()
        {
            Assert.Less(DocSnapVersioning.CompareVersions("V1.0.9", "V1.1.0"), 0);
            Assert.Greater(DocSnapVersioning.CompareVersions("V2.0.0", "V1.9.9"), 0);
            Assert.AreEqual(0, DocSnapVersioning.CompareVersions("V1.2.3", "V1.2.3"));
        }

        [Test]
        public void CompareVersions_CustomNamesSortAfterNumberedOnes()
        {
            Assert.Less(DocSnapVersioning.CompareVersions("V1.0.0", "milestone"), 0);
            Assert.Greater(DocSnapVersioning.CompareVersions("milestone", "V9.9.9"), 0);
        }

        [Test]
        public void NewestVersion_PicksTheHighest_NotTheLastAdded()
        {
            Assert.AreEqual("V1.1.0", DocSnapVersioning.NewestVersion(StateWith("V1.1.0", "V1.0.2")));
        }

        [Test]
        public void NewestVersion_EmptyShelf_IsEmptyString()
        {
            Assert.AreEqual("", DocSnapVersioning.NewestVersion(StateWith()));
        }

        [Test]
        public void UpsertSnapshot_ReplacesRatherThanDuplicating()
        {
            VersionsState state = StateWith("V1.0.0");
            DocSnapVersioning.UpsertSnapshot(state, new VersionSnapshot { version = "V1.0.0", sceneCount = 7 });

            Assert.AreEqual(1, state.versions.Count);
            Assert.AreEqual(7, state.versions[0].sceneCount);
        }

        [Test]
        public void UpsertSnapshot_KeepsTheListOrderedOldestToNewest()
        {
            var state = new VersionsState();
            DocSnapVersioning.UpsertSnapshot(state, new VersionSnapshot { version = "V1.1.0" });
            DocSnapVersioning.UpsertSnapshot(state, new VersionSnapshot { version = "V1.0.0" });

            Assert.AreEqual("V1.0.0", state.versions[0].version);
            Assert.AreEqual("V1.1.0", state.versions[1].version);
        }

        [Test]
        public void IsValidCustomName_RejectsPathSeparatorsAndDots()
        {
            Assert.IsFalse(DocSnapVersioning.IsValidCustomName(""));
            Assert.IsFalse(DocSnapVersioning.IsValidCustomName("a/b"));
            Assert.IsFalse(DocSnapVersioning.IsValidCustomName("a\\b"));
            Assert.IsFalse(DocSnapVersioning.IsValidCustomName("."));
            Assert.IsFalse(DocSnapVersioning.IsValidCustomName(".."));
        }

        [Test]
        public void IsValidCustomName_AcceptsOrdinaryNames()
        {
            Assert.IsTrue(DocSnapVersioning.IsValidCustomName("milestone-1"));
            Assert.IsTrue(DocSnapVersioning.IsValidCustomName("Before the refactor"));
        }

        // ==========================================
        // Windows trims a trailing '.' or ' ' when it creates a
        // directory, so "V1." would silently become a folder called
        // "V1" while the registry, versions.html and every link kept
        // saying "V1." - and the next real V1 would then collide with
        // it. Path.GetInvalidFileNameChars() does not cover this.
        // ==========================================
        [Test]
        public void IsValidCustomName_RejectsNamesWindowsWouldSilentlyRename()
        {
            Assert.IsFalse(DocSnapVersioning.IsValidCustomName("V1."));
            Assert.IsFalse(DocSnapVersioning.IsValidCustomName("milestone "));
            Assert.IsTrue(DocSnapVersioning.IsValidCustomName("v1.2"));
        }

        [Test]
        public void IsValidCustomName_RejectsReservedDeviceNames()
        {
            Assert.IsFalse(DocSnapVersioning.IsValidCustomName("CON"));
            Assert.IsFalse(DocSnapVersioning.IsValidCustomName("nul"));
            Assert.IsFalse(DocSnapVersioning.IsValidCustomName("COM1.backup"));
            Assert.IsTrue(DocSnapVersioning.IsValidCustomName("console"));
            Assert.IsTrue(DocSnapVersioning.IsValidCustomName("COM10"));
        }

        // ==========================================
        // Unity rewrites assets it manages on its own schedule -
        // LiberationSans SDF - Fallback.asset every time the dynamic
        // font atlas renders a glyph - so a size+timestamp signature
        // reported files nobody had opened as modified on nearly every
        // Changes page.
        // ==========================================
        [Test]
        public void HasFileChanged_SameBytes_NewTimestamp_IsNotAChange()
        {
            var before = new VersionFileEntry { path = "Assets/TextMesh Pro/x.asset", size = 100, signature = "100:111", contentHash = "abc" };
            var after = new VersionFileEntry { path = "Assets/TextMesh Pro/x.asset", size = 100, signature = "100:999", contentHash = "abc" };

            Assert.IsFalse(DocSnapVersioning.HasFileChanged(before, after));
        }

        [Test]
        public void HasFileChanged_DifferentBytes_IsAChange()
        {
            var before = new VersionFileEntry { path = "Assets/a.png", size = 100, signature = "100:111", contentHash = "abc" };
            var after = new VersionFileEntry { path = "Assets/a.png", size = 140, signature = "140:999", contentHash = "def" };

            Assert.IsTrue(DocSnapVersioning.HasFileChanged(before, after));
        }

        [Test]
        public void HasFileChanged_WithoutHashes_FallsBackToTheSignature()
        {
            // A snapshot recorded before hashes existed. Falling back is
            // better than declaring every file in the project modified
            // once, during the upgrade.
            var before = new VersionFileEntry { path = "Assets/a.png", size = 100, signature = "100:111" };
            var after = new VersionFileEntry { path = "Assets/a.png", size = 100, signature = "100:111" };
            Assert.IsFalse(DocSnapVersioning.HasFileChanged(before, after));

            after.signature = "100:222";
            Assert.IsTrue(DocSnapVersioning.HasFileChanged(before, after));
        }

        [Test]
        public void HasFileChanged_OneSideMissingAHash_StillUsesTheSignature()
        {
            var before = new VersionFileEntry { path = "Assets/a.png", size = 100, signature = "100:111" };
            var after = new VersionFileEntry { path = "Assets/a.png", size = 100, signature = "100:111", contentHash = "abc" };

            Assert.IsFalse(DocSnapVersioning.HasFileChanged(before, after));
        }

        [Test]
        public void FindSnapshot_MissingVersion_IsNullNotAnException()
        {
            Assert.IsNull(DocSnapVersioning.FindSnapshot(StateWith("V1.0.0"), "V9.9.9"));
            Assert.IsNull(DocSnapVersioning.FindSnapshot(null, "V1.0.0"));
            Assert.IsNull(DocSnapVersioning.FindSnapshot(StateWith("V1.0.0"), ""));
        }
    }
}
