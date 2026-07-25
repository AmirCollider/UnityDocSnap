// ==========================================
// DocSnapSearchIndexTests
// The search index is the one export artefact that
// is EXECUTED rather than displayed: it ships as
// theme/search-index.js and the page loads it with
// a <script> tag, because a file:// page cannot
// fetch() a .json. That makes every GameObject name
// in the project a line of JavaScript, and a name
// nobody thought about - a quote, a backslash, a
// literal `</script>` - is the difference between a
// working search box and a page whose scripts stop
// parsing halfway down.
//
// It is also the artefact with the hardest cap to
// verify by eye: MaxSearchRecords exists so a
// 200,000-object project cannot emit a file large
// enough to hang the browser, and "it did not hang"
// is not something anyone checks by hand.
// ==========================================
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Manifest;
using AmirCollider.UnityDocSnap.Editor.Search;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public class DocSnapSearchIndexTests
    {
        // ------------------------------------------
        // Helpers
        // ------------------------------------------

        private static JsonValue Scene(params JsonValue[] roots)
        {
            var arr = JsonValue.Arr();
            foreach (JsonValue root in roots) { arr.Add(root); }
            return JsonValue.Obj()
                .Set("scenePath", "Assets/Scenes/Main.unity")
                .Set("rootObjects", arr);
        }

        private static JsonValue GameObject(string name, int instanceId)
        {
            return JsonValue.Obj()
                .Set("name", name)
                .Set("instanceId", instanceId)
                .Set("components", JsonValue.Arr())
                .Set("children", JsonValue.Arr());
        }

        private static ManifestState ManifestWith(params ManifestSearchEntry[] entries)
        {
            var manifest = new ManifestState();
            foreach (ManifestSearchEntry entry in entries) { manifest.searchRecords.Add(entry); }
            return manifest;
        }

        private static ManifestSearchEntry Entry(string name)
        {
            return new ManifestSearchEntry
            {
                scope = "Main",
                group = "scene",
                category = "GameObject",
                name = name,
                sub = "Main",
                url = "scenes/Main.html"
            };
        }

        // ------------------------------------------
        // Record building
        // ------------------------------------------

        [Test]
        public void BuildSceneRecords_AlwaysIncludesTheSceneItself()
        {
            var records = DocSnapSearchIndex.BuildSceneRecords(Scene(), "Main", "Main", "scenes/Main.html");
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Scene", records[0].category);
            Assert.AreEqual("Main", records[0].name);
        }

        [Test]
        public void BuildSceneRecords_AddsOneRecordPerGameObject()
        {
            var records = DocSnapSearchIndex.BuildSceneRecords(
                Scene(GameObject("Canvas", 11), GameObject("Player", 12)),
                "Main", "Main", "scenes/Main.html");

            Assert.AreEqual(3, records.Count, "the Scene plus its two GameObjects");
            Assert.AreEqual("Canvas", records[1].name);
            Assert.AreEqual("Player", records[2].name);
        }

        // The deep link is what makes a search result useful
        // rather than merely correct: it opens the page AND
        // scrolls to the object.
        [Test]
        public void BuildSceneRecords_DeepLinksToTheObjectAnchor()
        {
            var records = DocSnapSearchIndex.BuildSceneRecords(
                Scene(GameObject("Player", 4242)), "Main", "Main", "scenes/Main.html");

            Assert.AreEqual("scenes/Main.html#go-4242", records[1].url);
        }

        [Test]
        public void BuildSceneRecords_TagsEveryRecordWithItsScope()
        {
            var records = DocSnapSearchIndex.BuildSceneRecords(
                Scene(GameObject("Player", 1)), "Main", "Main", "scenes/Main.html");

            // The scope is how a single-Scene re-export replaces its
            // own records without touching any other Scene's.
            foreach (ManifestSearchEntry record in records)
            {
                Assert.AreEqual("Main", record.scope);
            }
        }

        // ------------------------------------------
        // Emitted JavaScript
        // ------------------------------------------

        [Test]
        public void WriteSearchIndexJs_AssignsTheGlobalTheSiteReads()
        {
            string js = DocSnapSearchIndex.WriteSearchIndexJs(ManifestWith(Entry("Player")));
            StringAssert.Contains("window.__DOCSNAP_SEARCH__ = ", js);
            StringAssert.Contains("window.__DOCSNAP_SEARCH_TRUNCATED__ = false", js);
        }

        [Test]
        public void WriteSearchIndexJs_EmptyManifest_StillEmitsAnArray()
        {
            // A page whose index file failed to define the global at
            // all throws on load and takes the rest of app.js with it.
            string js = DocSnapSearchIndex.WriteSearchIndexJs(ManifestWith());
            StringAssert.Contains("window.__DOCSNAP_SEARCH__ = [", js);
        }

        [Test]
        public void WriteSearchIndexJs_EscapesQuotesInNames()
        {
            string js = DocSnapSearchIndex.WriteSearchIndexJs(ManifestWith(Entry("say \"hi\"")));
            StringAssert.Contains("\\\"hi\\\"", js);
        }

        [Test]
        public void WriteSearchIndexJs_EscapesBackslashes()
        {
            string js = DocSnapSearchIndex.WriteSearchIndexJs(ManifestWith(Entry("a\\b")));
            StringAssert.Contains("a\\\\b", js);
        }

        // The one that actually breaks a page: the HTML parser ends
        // an inline script at the first literal "</script", and a
        // GameObject may legally be called that.
        [Test]
        public void WriteSearchIndexJs_NeverEmitsARawAngleBracket()
        {
            string js = DocSnapSearchIndex.WriteSearchIndexJs(
                ManifestWith(Entry("</script><script>alert(1)</script>")));

            int payloadStart = js.IndexOf("__DOCSNAP_SEARCH__", System.StringComparison.Ordinal);
            string payload = js.Substring(payloadStart);
            StringAssert.DoesNotContain("<", payload);
            StringAssert.DoesNotContain(">", payload);
        }

        [Test]
        public void WriteSearchIndexJs_CapsTheRecordCountAndSaysSo()
        {
            var manifest = new ManifestState();
            for (int i = 0; i < DocSnapConstants.MaxSearchRecords + 25; i++)
            {
                manifest.searchRecords.Add(Entry("Object" + i));
            }

            string js = DocSnapSearchIndex.WriteSearchIndexJs(manifest);

            // Truncation has to be announced, or the site reports
            // "no matches" for something that is really in the
            // project and simply did not fit in the index.
            StringAssert.Contains("window.__DOCSNAP_SEARCH_TRUNCATED__ = true", js);

            // The last record past the cap must not be in the file.
            StringAssert.DoesNotContain("Object" + (DocSnapConstants.MaxSearchRecords + 24), js);
        }
    }
}
