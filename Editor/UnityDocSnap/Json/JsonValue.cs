// ==========================================
// JsonValue
// A tiny, dependency-free JSON document model
// with a pretty-printing writer. Used instead
// of a third-party library so Unity DocSnap
// has zero external dependencies (see README).
// Order of object keys is preserved, which
// keeps every export byte-for-byte readable.
// ==========================================
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AmirCollider.UnityDocSnap.Editor.Json
{
    internal enum JsonKind
    {
        Object,
        Array,
        String,
        Number,
        Bool,
        Null
    }

    internal sealed class JsonValue
    {
        public JsonKind Kind { get; private set; }

        private readonly List<KeyValuePair<string, JsonValue>> _members;
        private readonly List<JsonValue> _items;
        private readonly string _stringValue;
        private readonly double _numberValue;
        private readonly bool _boolValue;

        // ==========================================
        // Private constructor — use the static
        // factory methods below to build values.
        // ==========================================
        private JsonValue(JsonKind kind, List<KeyValuePair<string, JsonValue>> members = null,
            List<JsonValue> items = null, string strVal = null, double numVal = 0, bool boolVal = false)
        {
            Kind = kind;
            _members = members;
            _items = items;
            _stringValue = strVal;
            _numberValue = numVal;
            _boolValue = boolVal;
        }

        // ==========================================
        // Factory methods
        // ==========================================
        public static JsonValue Obj()
        {
            return new JsonValue(JsonKind.Object, members: new List<KeyValuePair<string, JsonValue>>());
        }

        public static JsonValue Arr()
        {
            return new JsonValue(JsonKind.Array, items: new List<JsonValue>());
        }

        public static JsonValue Str(string value)
        {
            return value == null ? Null() : new JsonValue(JsonKind.String, strVal: value);
        }

        public static JsonValue Num(double value)
        {
            return new JsonValue(JsonKind.Number, numVal: value);
        }

        public static JsonValue Num(int value)
        {
            return new JsonValue(JsonKind.Number, numVal: value);
        }

        public static JsonValue Num(long value)
        {
            return new JsonValue(JsonKind.Number, numVal: value);
        }

        public static JsonValue Bool(bool value)
        {
            return new JsonValue(JsonKind.Bool, boolVal: value);
        }

        public static JsonValue Null()
        {
            return new JsonValue(JsonKind.Null);
        }

        // ==========================================
        // Obj.Set(key, value) — fluent member add.
        // Overwrites if the key already exists so
        // callers can safely upsert.
        // ==========================================
        public JsonValue Set(string key, JsonValue value)
        {
            value = value ?? Null();
            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i].Key == key)
                {
                    _members[i] = new KeyValuePair<string, JsonValue>(key, value);
                    return this;
                }
            }
            _members.Add(new KeyValuePair<string, JsonValue>(key, value));
            return this;
        }

        public JsonValue Set(string key, string value) { return Set(key, Str(value)); }
        public JsonValue Set(string key, double value) { return Set(key, Num(value)); }
        public JsonValue Set(string key, int value) { return Set(key, Num(value)); }
        public JsonValue Set(string key, long value) { return Set(key, Num(value)); }
        public JsonValue Set(string key, bool value) { return Set(key, Bool(value)); }

        // ==========================================
        // Arr.Add(value) — fluent item append.
        // ==========================================
        public JsonValue Add(JsonValue value)
        {
            _items.Add(value ?? Null());
            return this;
        }

        // ==========================================
        // Accessors used by renderers walking the
        // tree back out (mirrors a read-only DOM).
        // ==========================================
        public bool Has(string key)
        {
            if (Kind != JsonKind.Object) { return false; }
            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i].Key == key) { return true; }
            }
            return false;
        }

        public JsonValue Get(string key)
        {
            if (Kind != JsonKind.Object) { return Null(); }
            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i].Key == key) { return _members[i].Value; }
            }
            return Null();
        }

        public IReadOnlyList<JsonValue> Items
        {
            get { return _items ?? (IReadOnlyList<JsonValue>)new List<JsonValue>(); }
        }

        public IReadOnlyList<KeyValuePair<string, JsonValue>> Members
        {
            get { return _members ?? (IReadOnlyList<KeyValuePair<string, JsonValue>>)new List<KeyValuePair<string, JsonValue>>(); }
        }

        public string AsString(string fallback = "")
        {
            return Kind == JsonKind.String ? _stringValue : fallback;
        }

        public double AsNumber(double fallback = 0)
        {
            return Kind == JsonKind.Number ? _numberValue : fallback;
        }

        public bool AsBool(bool fallback = false)
        {
            return Kind == JsonKind.Bool ? _boolValue : fallback;
        }

        public bool IsNull { get { return Kind == JsonKind.Null; } }

        // ==========================================
        // ToString() — entry point for pretty-printed
        // JSON text, two-space indentation.
        // ==========================================
        public override string ToString()
        {
            var sb = new StringBuilder(256);
            Write(sb, 0);
            return sb.ToString();
        }

        // ==========================================
        // Write(sb, indent) — recursive pretty-printer.
        // ==========================================
        private void Write(StringBuilder sb, int indent)
        {
            switch (Kind)
            {
                case JsonKind.Null:
                    sb.Append("null");
                    break;
                case JsonKind.Bool:
                    sb.Append(_boolValue ? "true" : "false");
                    break;
                case JsonKind.Number:
                    WriteNumber(sb, _numberValue);
                    break;
                case JsonKind.String:
                    WriteEscapedString(sb, _stringValue);
                    break;
                case JsonKind.Array:
                    WriteArrayItems(sb, indent);
                    break;
                case JsonKind.Object:
                    WriteObject(sb, indent);
                    break;
            }
        }

        // ==========================================
        // WriteNumber — avoids scientific notation
        // and trims trailing zeroes for readability.
        // ==========================================
        private static void WriteNumber(StringBuilder sb, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                sb.Append('0');
                return;
            }
            if (value == System.Math.Floor(value) && System.Math.Abs(value) < 1e15)
            {
                sb.Append(((long)value).ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append(value.ToString("R", CultureInfo.InvariantCulture));
            }
        }

        // ==========================================
        // WriteObject / WriteArrayItems
        // ==========================================
        private void WriteObject(StringBuilder sb, int indent)
        {
            if (_members.Count == 0)
            {
                sb.Append("{}");
                return;
            }
            sb.Append("{\n");
            string childPad = Pad(indent + 1);
            for (int i = 0; i < _members.Count; i++)
            {
                sb.Append(childPad);
                WriteEscapedString(sb, _members[i].Key);
                sb.Append(": ");
                _members[i].Value.Write(sb, indent + 1);
                if (i < _members.Count - 1) { sb.Append(','); }
                sb.Append('\n');
            }
            sb.Append(Pad(indent)).Append('}');
        }

        private void WriteArrayItems(StringBuilder sb, int indent)
        {
            if (_items.Count == 0)
            {
                sb.Append("[]");
                return;
            }
            sb.Append("[\n");
            string childPad = Pad(indent + 1);
            for (int i = 0; i < _items.Count; i++)
            {
                sb.Append(childPad);
                _items[i].Write(sb, indent + 1);
                if (i < _items.Count - 1) { sb.Append(','); }
                sb.Append('\n');
            }
            sb.Append(Pad(indent)).Append(']');
        }

        private static string Pad(int indent)
        {
            return new string(' ', indent * 2);
        }

        // ==========================================
        // WriteEscapedString — standard JSON escaping,
        // plus escaping '<' so this JSON can always be
        // safely embedded inside an inline <script> tag.
        // ==========================================
        private static void WriteEscapedString(StringBuilder sb, string value)
        {
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '<': sb.Append("\\u003c"); break;
                    case '>': sb.Append("\\u003e"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
