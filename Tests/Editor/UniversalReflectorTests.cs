// ==========================================
// UniversalReflectorTests
// EditMode tests for the reflector that backs
// every Component, importer, Material and
// ScriptableObject export. The reflector is a
// wide switch over SerializedPropertyType, and
// that is exactly the kind of code that can
// regress silently between Unity versions (a
// renamed accessor, a reordered enum, a new
// property type) without any obvious error. These
// tests reflect a real ScriptableObject built for
// the purpose and pin the "kind" + value each
// property type maps to.
// ==========================================
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public class UniversalReflectorTests
    {
        private enum Mood { Calm, Happy, Grumpy }

        // A serializable nested struct so the "generic" branch
        // gets exercised too.
        [System.Serializable]
        private struct Slot
        {
            public int amount;
            public string label;
        }

        private sealed class ReflectorProbe : ScriptableObject
        {
            public int health = 7;
            public float speed = 2.5f;
            public bool alive = true;
            public string title = "Boss";
            public Mood mood = Mood.Happy;
            public Vector2 aim = new Vector2(1f, 2f);
            public Vector3 spawn = new Vector3(1f, 2f, 3f);
            public Color tint = Color.red;
            public int[] scores = { 10, 20, 30 };
            public Slot slot = new Slot { amount = 5, label = "gold" };
            public Object linkedAsset; // left null on purpose
        }

        private ReflectorProbe _probe;

        [SetUp]
        public void SetUp()
        {
            _probe = ScriptableObject.CreateInstance<ReflectorProbe>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_probe != null) { Object.DestroyImmediate(_probe); }
        }

        private JsonValue Field(string name)
        {
            JsonValue fields = UniversalReflector.ReadTopLevelFields(_probe);
            foreach (JsonValue f in fields.Items)
            {
                if (f.Get("name").AsString() == name) { return f; }
            }
            return null;
        }

        [Test]
        public void NullTarget_ReturnsEmptyArray_NeverThrows()
        {
            JsonValue result = UniversalReflector.ReadTopLevelFields(null);
            Assert.AreEqual(JsonKind.Array, result.Kind);
            Assert.AreEqual(0, result.Items.Count);
        }

        [Test]
        public void ReadsEveryTopLevelField()
        {
            // Every declared field plus Unity's hidden m_Script.
            Assert.IsNotNull(Field("health"));
            Assert.IsNotNull(Field("speed"));
            Assert.IsNotNull(Field("alive"));
            Assert.IsNotNull(Field("title"));
        }

        [Test]
        public void Integer_Field()
        {
            JsonValue f = Field("health");
            Assert.AreEqual("int", f.Get("kind").AsString());
            Assert.AreEqual(7, f.Get("value").AsNumber());
        }

        [Test]
        public void Float_Field()
        {
            JsonValue f = Field("speed");
            Assert.AreEqual("float", f.Get("kind").AsString());
            Assert.AreEqual(2.5, f.Get("value").AsNumber(), 0.0001);
        }

        [Test]
        public void Boolean_Field()
        {
            JsonValue f = Field("alive");
            Assert.AreEqual("bool", f.Get("kind").AsString());
            Assert.IsTrue(f.Get("value").AsBool());
        }

        [Test]
        public void String_Field()
        {
            JsonValue f = Field("title");
            Assert.AreEqual("string", f.Get("kind").AsString());
            Assert.AreEqual("Boss", f.Get("value").AsString());
        }

        [Test]
        public void Enum_ResolvesToDisplayNameNotRawInt()
        {
            JsonValue f = Field("mood");
            Assert.AreEqual("enum", f.Get("kind").AsString());
            Assert.AreEqual("Happy", f.Get("value").AsString());
        }

        [Test]
        public void Vector2_Field()
        {
            JsonValue f = Field("aim");
            Assert.AreEqual("vector2", f.Get("kind").AsString());
            Assert.AreEqual(1, f.Get("value").Get("x").AsNumber(), 0.0001);
            Assert.AreEqual(2, f.Get("value").Get("y").AsNumber(), 0.0001);
        }

        [Test]
        public void Vector3_Field()
        {
            JsonValue f = Field("spawn");
            Assert.AreEqual("vector3", f.Get("kind").AsString());
            JsonValue v = f.Get("value");
            Assert.AreEqual(1, v.Get("x").AsNumber(), 0.0001);
            Assert.AreEqual(2, v.Get("y").AsNumber(), 0.0001);
            Assert.AreEqual(3, v.Get("z").AsNumber(), 0.0001);
        }

        [Test]
        public void Color_SerializesAsHex()
        {
            JsonValue f = Field("tint");
            Assert.AreEqual("color", f.Get("kind").AsString());
            // Color.red -> #FF0000 (RGB, case-insensitive compare for safety).
            Assert.AreEqual("#ff0000", f.Get("value").AsString().ToLowerInvariant());
        }

        [Test]
        public void Array_ReportsCountAndItems()
        {
            JsonValue f = Field("scores");
            Assert.AreEqual("array", f.Get("kind").AsString());
            Assert.AreEqual(3, f.Get("count").AsNumber());
            Assert.AreEqual(3, f.Get("items").Items.Count);
            Assert.AreEqual(10, f.Get("items").Items[0].Get("value").AsNumber());
            Assert.IsFalse(f.Get("truncated").AsBool());
        }

        [Test]
        public void NestedStruct_BecomesGenericWithFields()
        {
            JsonValue f = Field("slot");
            Assert.AreEqual("generic", f.Get("kind").AsString());
            JsonValue inner = f.Get("fields");
            bool sawAmount = false, sawLabel = false;
            foreach (JsonValue child in inner.Items)
            {
                if (child.Get("name").AsString() == "amount") { sawAmount = true; Assert.AreEqual(5, child.Get("value").AsNumber()); }
                if (child.Get("name").AsString() == "label") { sawLabel = true; Assert.AreEqual("gold", child.Get("value").AsString()); }
            }
            Assert.IsTrue(sawAmount, "expected the struct's int field");
            Assert.IsTrue(sawLabel, "expected the struct's string field");
        }

        [Test]
        public void NullObjectReference_IsMarkedNullNotMissing()
        {
            JsonValue f = Field("linkedAsset");
            Assert.AreEqual("objectRef", f.Get("kind").AsString());
            Assert.IsTrue(f.Get("isNull").AsBool());
            Assert.IsFalse(f.Get("isMissing").AsBool());
        }
    }
}
