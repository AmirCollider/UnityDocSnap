// ==========================================
// JsonValueStreamingTests
// data/*.json is now written straight to disk instead
// of being built as one string first. That is a change
// to how every export's largest artefact is produced,
// so the thing worth proving is not that streaming
// works - it is that it produces EXACTLY what the old
// path produced.
//
// A byte that moved would not announce itself: the
// incremental update parses these files back, so a
// writer that drifted from the parser would silently
// stop reusing unchanged Scenes, and a reader diffing
// two exports would see changes that are not there.
// ==========================================
using System.Globalization;
using System.IO;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Json;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class JsonValueStreamingTests
    {
        private static string Streamed(JsonValue value)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb, CultureInfo.InvariantCulture))
            {
                value.WriteTo(writer);
            }
            return sb.ToString();
        }

        private static string StreamedCompact(JsonValue value)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb, CultureInfo.InvariantCulture))
            {
                value.WriteCompactTo(writer);
            }
            return sb.ToString();
        }

        // ==========================================
        // A document with one of everything the exporter
        // actually emits: nesting, arrays, empty containers,
        // unicode, the characters that get escaped, integers,
        // fractions and negatives.
        // ==========================================
        private static JsonValue SampleDocument()
        {
            return JsonValue.Obj()
                .Set("sceneName", "Main")
                .Set("scenePath", "Assets/Scenes/Main.unity")
                .Set("exportedUtc", "2026-01-01T00:00:00Z")
                .Set("totalGameObjects", 3)
                .Set("depthTruncated", false)
                .Set("emptyObject", JsonValue.Obj())
                .Set("emptyArray", JsonValue.Arr())
                .Set("rootObjects", JsonValue.Arr()
                    .Add(JsonValue.Obj()
                        .Set("name", "Canvas")
                        .Set("tag", "Untagged")
                        .Set("scale", 1.5)
                        .Set("offset", -0.25)
                        .Set("components", JsonValue.Arr()
                            .Add(JsonValue.Obj()
                                .Set("type", "RectTransform")
                                .Set("missing", JsonValue.Null())
                                .Set("fields", JsonValue.Arr()
                                    .Add(JsonValue.Obj().Set("name", "m_Text").Set("value", "a\"b\\c\nd\te"))))))
                    .Add(JsonValue.Obj()
                        .Set("name", "メインカメラ")
                        .Set("note", "دوربین اصلی")
                        .Set("html", "<script>alert(1)</script>")));
        }

        [Test]
        public void StreamedOutputIsIdenticalToToString()
        {
            JsonValue document = SampleDocument();

            Assert.AreEqual(document.ToString(), Streamed(document));
        }

        [Test]
        public void StreamedCompactOutputIsIdenticalToToCompactString()
        {
            JsonValue document = SampleDocument();

            Assert.AreEqual(document.ToCompactString(), StreamedCompact(document));
        }

        [Test]
        public void StreamedOutputParsesBackToTheSameDocument()
        {
            // The incremental update reads these files back. A
            // writer the parser cannot read turns "reuse the
            // unchanged Scene" into "re-walk everything", quietly.
            JsonValue document = SampleDocument();

            JsonValue reparsed = JsonValue.Parse(Streamed(document));

            Assert.AreEqual(document.ToString(), reparsed.ToString());
        }

        [Test]
        public void EveryScalarKindSurvivesTheStream()
        {
            JsonValue scalars = JsonValue.Obj()
                .Set("string", "x")
                .Set("int", 42)
                .Set("long", 9007199254740992L)
                .Set("double", 0.125)
                .Set("negative", -7)
                .Set("true", true)
                .Set("false", false)
                .Set("null", JsonValue.Null());

            Assert.AreEqual(scalars.ToString(), Streamed(scalars));
        }

        [Test]
        public void AngleBracketsAreStillEscapedWhenStreamed()
        {
            // These files are also embedded in inline <script>
            // tags, which is why the writer escapes '<' and '>'
            // at all. Losing that in the streaming path would be
            // a script-injection hole that no test elsewhere
            // covers.
            string json = Streamed(JsonValue.Obj().Set("v", "</script>"));

            StringAssert.DoesNotContain("</script>", json);
            StringAssert.Contains("\\u003c", json);
        }

        [Test]
        public void ControlCharactersAreStillEscapedWhenStreamed()
        {
            string json = Streamed(JsonValue.Obj().Set("v", "a\u0001b"));

            StringAssert.Contains("\\u0001", json);
        }

        [Test]
        public void NumbersAreWrittenInvariantlyWhenStreamed()
        {
            // A StreamWriter carries a culture, and a culture
            // that writes "0,125" produces JSON nothing can
            // parse. The writer formats numbers itself for
            // exactly this reason; this pins that down.
            string json = Streamed(JsonValue.Obj().Set("v", 0.125));

            StringAssert.Contains("0.125", json);
            StringAssert.DoesNotContain("0,125", json);
        }

        [Test]
        public void ADeeplyNestedDocumentStreamsWithoutTruncation()
        {
            // The recursion is unchanged by streaming, but the
            // sink is not - and a sink that buffered per call
            // would show up here first.
            JsonValue leaf = JsonValue.Obj().Set("depth", "bottom");
            for (int i = 0; i < 200; i++)
            {
                leaf = JsonValue.Obj().Set("child", leaf);
            }

            string json = Streamed(leaf);

            Assert.AreEqual(leaf.ToString(), json);
            StringAssert.Contains("bottom", json);
        }

        [Test]
        public void ALargeDocumentStreamsIdentically()
        {
            // The whole point of the change is documents large
            // enough that holding them as one string matters.
            var items = JsonValue.Arr();
            for (int i = 0; i < 5000; i++)
            {
                items.Add(JsonValue.Obj()
                    .Set("name", "GameObject " + i)
                    .Set("index", i)
                    .Set("path", "Canvas/Panel/Item" + i));
            }
            JsonValue document = JsonValue.Obj().Set("rootObjects", items);

            Assert.AreEqual(document.ToString(), Streamed(document));
        }

        [Test]
        public void WriteToRejectsANullWriter()
        {
            Assert.Throws<System.ArgumentNullException>(() => JsonValue.Obj().WriteTo(null));
            Assert.Throws<System.ArgumentNullException>(() => JsonValue.Obj().WriteCompactTo(null));
        }
    }
}
