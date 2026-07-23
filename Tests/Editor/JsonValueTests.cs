// ==========================================
// JsonValueTests
// Unit tests for the dependency-free JSON model
// that every export is built from. JsonValue is
// the single most load-bearing type in the tool:
// if its writer or its (new) parser drifts, every
// data/*.json, every summary, and the incremental
// reuse path all silently break at once. These
// tests pin the writer's shape and prove the
// parser is an exact inverse of it (round-trip).
// ==========================================
using AmirCollider.UnityDocSnap.Editor.Json;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public class JsonValueTests
    {
        // ------------------------------------------
        // Writer shape
        // ------------------------------------------
        [Test]
        public void EmptyObject_WritesBraces()
        {
            Assert.AreEqual("{}", JsonValue.Obj().ToString());
        }

        [Test]
        public void EmptyArray_WritesBrackets()
        {
            Assert.AreEqual("[]", JsonValue.Arr().ToString());
        }

        [Test]
        public void Set_PreservesInsertionOrder()
        {
            JsonValue obj = JsonValue.Obj().Set("b", 1).Set("a", 2).Set("c", 3);
            string text = obj.ToString();
            int b = text.IndexOf("\"b\"", System.StringComparison.Ordinal);
            int a = text.IndexOf("\"a\"", System.StringComparison.Ordinal);
            int c = text.IndexOf("\"c\"", System.StringComparison.Ordinal);
            Assert.Less(b, a);
            Assert.Less(a, c);
        }

        [Test]
        public void Set_SameKeyTwice_OverwritesInPlace()
        {
            JsonValue obj = JsonValue.Obj().Set("k", "first").Set("k", "second");
            Assert.AreEqual("second", obj.Get("k").AsString());
            Assert.AreEqual(1, obj.Members.Count);
        }

        [Test]
        public void Number_WholeValue_HasNoDecimalPoint()
        {
            Assert.AreEqual("42", JsonValue.Num(42).ToString());
            Assert.AreEqual("-7", JsonValue.Num(-7).ToString());
        }

        [Test]
        public void Number_NaNOrInfinity_WritesZeroNotInvalidJson()
        {
            Assert.AreEqual("0", JsonValue.Num(double.NaN).ToString());
            Assert.AreEqual("0", JsonValue.Num(double.PositiveInfinity).ToString());
        }

        [Test]
        public void String_EscapesAngleBracketsForSafeScriptEmbedding()
        {
            string text = JsonValue.Str("<script>").ToString();
            StringAssert.Contains("\\u003c", text);
            StringAssert.Contains("\\u003e", text);
            StringAssert.DoesNotContain("<script>", text);
        }

        [Test]
        public void String_EscapesQuotesBackslashesAndControlChars()
        {
            string text = JsonValue.Str("a\"b\\c\nd\te").ToString();
            StringAssert.Contains("\\\"", text);
            StringAssert.Contains("\\\\", text);
            StringAssert.Contains("\\n", text);
            StringAssert.Contains("\\t", text);
        }

        [Test]
        public void NullString_BecomesJsonNull()
        {
            Assert.AreEqual("null", JsonValue.Str(null).ToString());
        }

        // ------------------------------------------
        // Accessors
        // ------------------------------------------
        [Test]
        public void Accessors_ReturnFallbackOnTypeMismatch()
        {
            JsonValue n = JsonValue.Num(5);
            Assert.AreEqual("fallback", n.AsString("fallback"));
            Assert.AreEqual(true, n.AsBool(true));

            JsonValue s = JsonValue.Str("hi");
            Assert.AreEqual(99, s.AsNumber(99));
        }

        [Test]
        public void Get_MissingKey_ReturnsNull()
        {
            Assert.IsTrue(JsonValue.Obj().Get("nope").IsNull);
            Assert.IsFalse(JsonValue.Obj().Set("x", 1).Has("y"));
        }

        // ------------------------------------------
        // Compact writer
        // ------------------------------------------
        [Test]
        public void Compact_AllScalarObject_IsInlinedOnOneLine()
        {
            string text = JsonValue.Obj().Set("a", 1).Set("b", "x").ToCompactString();
            Assert.IsFalse(text.Contains("\n"), "all-scalar object should inline");
            StringAssert.Contains("{ ", text);
        }

        [Test]
        public void Compact_ObjectWithNestedContainer_ExpandsBlock()
        {
            JsonValue obj = JsonValue.Obj().Set("child", JsonValue.Obj().Set("x", 1));
            Assert.IsTrue(obj.ToCompactString().Contains("\n"), "object holding a nested object should expand");
        }

        // ------------------------------------------
        // Parser round-trips (parser is the inverse of the writer)
        // ------------------------------------------
        [Test]
        public void Parse_RoundTripsNestedStructure()
        {
            JsonValue original = JsonValue.Obj()
                .Set("name", "Player")
                .Set("count", 3)
                .Set("ratio", 0.5)
                .Set("active", true)
                .Set("nothing", JsonValue.Null())
                .Set("tags", JsonValue.Arr().Add(JsonValue.Str("a")).Add(JsonValue.Str("b")))
                .Set("nested", JsonValue.Obj().Set("deep", JsonValue.Arr().Add(JsonValue.Num(1)).Add(JsonValue.Num(2))));

            string written = original.ToString();
            JsonValue reparsed = JsonValue.Parse(written);

            Assert.AreEqual(written, reparsed.ToString(), "parse(write(x)) must equal write(x)");
        }

        [Test]
        public void Parse_DecodesAngleBracketEscapesBackToLiteralText()
        {
            JsonValue reparsed = JsonValue.Parse(JsonValue.Str("<b>&</b>").ToString());
            Assert.AreEqual("<b>&</b>", reparsed.AsString());
        }

        [Test]
        public void Parse_ReadsTypesCorrectly()
        {
            JsonValue v = JsonValue.Parse("{ \"i\": 12, \"f\": 3.25, \"s\": \"hi\", \"b\": false, \"n\": null, \"a\": [1, 2] }");
            Assert.AreEqual(JsonKind.Object, v.Kind);
            Assert.AreEqual(12, v.Get("i").AsNumber());
            Assert.AreEqual(3.25, v.Get("f").AsNumber());
            Assert.AreEqual("hi", v.Get("s").AsString());
            Assert.AreEqual(false, v.Get("b").AsBool(true));
            Assert.IsTrue(v.Get("n").IsNull);
            Assert.AreEqual(2, v.Get("a").Items.Count);
        }

        [Test]
        public void Parse_PreservesObjectKeyOrder()
        {
            JsonValue v = JsonValue.Parse("{ \"z\": 1, \"m\": 2, \"a\": 3 }");
            Assert.AreEqual("z", v.Members[0].Key);
            Assert.AreEqual("m", v.Members[1].Key);
            Assert.AreEqual("a", v.Members[2].Key);
        }

        [Test]
        public void Parse_ToleratesArbitraryWhitespace()
        {
            JsonValue v = JsonValue.Parse("  {\n\t\"a\" :\r\n 1 ,\n \"b\":[ 1 , 2 ]\n}  ");
            Assert.AreEqual(1, v.Get("a").AsNumber());
            Assert.AreEqual(2, v.Get("b").Items.Count);
        }

        [Test]
        public void TryParse_ReturnsFalseOnMalformedInput()
        {
            JsonValue result;
            Assert.IsFalse(JsonValue.TryParse("{ \"a\": }", out result));
            Assert.IsFalse(JsonValue.TryParse("{ unquoted: 1 }", out result));
            Assert.IsFalse(JsonValue.TryParse(null, out result));
        }

        [Test]
        public void Parse_EmptyContainers()
        {
            Assert.AreEqual(0, JsonValue.Parse("{}").Members.Count);
            Assert.AreEqual(0, JsonValue.Parse("[]").Items.Count);
        }

        [Test]
        public void Parse_CompactWriterOutputRoundTrips()
        {
            JsonValue original = JsonValue.Obj()
                .Set("scalars", JsonValue.Obj().Set("a", 1).Set("b", 2))
                .Set("list", JsonValue.Arr().Add(JsonValue.Str("x")).Add(JsonValue.Str("y")));
            JsonValue reparsed = JsonValue.Parse(original.ToCompactString());
            Assert.AreEqual(original.ToString(), reparsed.ToString());
        }
    }
}
