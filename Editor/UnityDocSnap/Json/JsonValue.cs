// ==========================================
// JsonValue
// A tiny, dependency-free JSON document model
// with a pretty-printing writer. Used instead
// of a third-party library so Unity DocSnap
// has zero external dependencies (see README).
// Order of object keys is preserved, which
// keeps every export byte-for-byte readable.
// ==========================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        // Parse(text) — reads JSON text back into a
        // JsonValue tree. This is the exact inverse of
        // ToString()/ToCompactString(): object key order
        // is preserved, and the < / > escapes the
        // writer emits for '<' / '>' are decoded straight
        // back. It exists so an incremental export can reuse
        // a still-current data/*.json instead of re-walking
        // the Scene or re-reading every asset from disk, and
        // it makes JsonValue fully round-trippable (and so
        // unit-testable) on its own. Throws FormatException
        // on malformed input; callers that want a soft
        // failure use TryParse.
        // ==========================================
        public static JsonValue Parse(string text)
        {
            if (text == null) { throw new FormatException("JSON text is null."); }
            var parser = new Parser(text);
            parser.SkipWhitespace();
            JsonValue result = parser.ReadValue();
            parser.SkipWhitespace();
            if (!parser.AtEnd) { throw new FormatException("Unexpected trailing characters at position " + parser.Position + "."); }
            return result;
        }

        public static bool TryParse(string text, out JsonValue result)
        {
            try { result = Parse(text); return true; }
            catch { result = null; return false; }
        }

        // ==========================================
        // Parser — a small, allocation-light
        // recursive-descent JSON reader. Deliberately
        // strict about structure but forgiving about
        // whitespace, matching what this tool's own
        // writer produces.
        // ==========================================
        private sealed class Parser
        {
            private readonly string _s;
            private int _i;

            public Parser(string s) { _s = s; _i = 0; }
            public int Position { get { return _i; } }
            public bool AtEnd { get { return _i >= _s.Length; } }

            public void SkipWhitespace()
            {
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r') { _i++; }
                    else { break; }
                }
            }

            public JsonValue ReadValue()
            {
                if (_i >= _s.Length) { throw new FormatException("Unexpected end of JSON."); }
                char c = _s[_i];
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return Str(ReadString());
                    case 't': Expect("true"); return Bool(true);
                    case 'f': Expect("false"); return Bool(false);
                    case 'n': Expect("null"); return Null();
                    default: return ReadNumber();
                }
            }

            private JsonValue ReadObject()
            {
                var obj = Obj();
                _i++; // consume '{'
                SkipWhitespace();
                if (_i < _s.Length && _s[_i] == '}') { _i++; return obj; }
                while (true)
                {
                    SkipWhitespace();
                    if (_i >= _s.Length || _s[_i] != '"') { throw new FormatException("Expected object key at position " + _i + "."); }
                    string key = ReadString();
                    SkipWhitespace();
                    if (_i >= _s.Length || _s[_i] != ':') { throw new FormatException("Expected ':' at position " + _i + "."); }
                    _i++; // consume ':'
                    SkipWhitespace();
                    // AppendMember, not Set: Set scans every existing
                    // member to honour overwrite-on-duplicate, which
                    // makes parsing an object O(n²) in its member count.
                    // A duplicate key is invalid in the JSON this tool
                    // writes, and on the rare hand-edited input that has
                    // one, first-wins is a defensible reading.
                    obj.AppendMember(key, ReadValue());
                    SkipWhitespace();
                    if (_i >= _s.Length) { throw new FormatException("Unterminated object."); }
                    char sep = _s[_i];
                    if (sep == ',') { _i++; continue; }
                    if (sep == '}') { _i++; break; }
                    throw new FormatException("Expected ',' or '}' at position " + _i + ".");
                }
                return obj;
            }

            private JsonValue ReadArray()
            {
                var arr = Arr();
                _i++; // consume '['
                SkipWhitespace();
                if (_i < _s.Length && _s[_i] == ']') { _i++; return arr; }
                while (true)
                {
                    SkipWhitespace();
                    arr.Add(ReadValue());
                    SkipWhitespace();
                    if (_i >= _s.Length) { throw new FormatException("Unterminated array."); }
                    char sep = _s[_i];
                    if (sep == ',') { _i++; continue; }
                    if (sep == ']') { _i++; break; }
                    throw new FormatException("Expected ',' or ']' at position " + _i + ".");
                }
                return arr;
            }

            private string ReadString()
            {
                _i++; // consume opening quote
                var sb = new StringBuilder();
                while (_i < _s.Length)
                {
                    char c = _s[_i++];
                    if (c == '"') { return sb.ToString(); }
                    if (c == '\\')
                    {
                        if (_i >= _s.Length) { break; }
                        char e = _s[_i++];
                        switch (e)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (_i + 4 > _s.Length) { throw new FormatException("Truncated \\u escape."); }
                                string hex = _s.Substring(_i, 4);
                                _i += 4;
                                sb.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                                break;
                            default: throw new FormatException("Invalid escape '\\" + e + "' at position " + (_i - 1) + ".");
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                throw new FormatException("Unterminated string.");
            }

            private JsonValue ReadNumber()
            {
                int start = _i;
                if (_i < _s.Length && (_s[_i] == '-' || _s[_i] == '+')) { _i++; }
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') { _i++; }
                    else { break; }
                }
                if (_i == start) { throw new FormatException("Invalid JSON value at position " + start + "."); }
                string token = _s.Substring(start, _i - start);
                double value;
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    throw new FormatException("Invalid number '" + token + "'.");
                }
                return Num(value);
            }

            private void Expect(string literal)
            {
                if (_i + literal.Length > _s.Length || _s.Substring(_i, literal.Length) != literal)
                {
                    throw new FormatException("Expected '" + literal + "' at position " + _i + ".");
                }
                _i += literal.Length;
            }
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

        // ==========================================
        // AppendMember — unconditional append, used only
        // by the parser (see the note at its call site).
        // ==========================================
        private JsonValue AppendMember(string key, JsonValue value)
        {
            _members.Add(new KeyValuePair<string, JsonValue>(key, value ?? Null()));
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

        // Shared, never-mutated empty lists. Reading .Items on a
        // non-array (which every renderer does constantly, since
        // Get() on a missing key returns Null) used to allocate a
        // fresh List every single time.
        private static readonly List<JsonValue> EmptyItems = new List<JsonValue>();
        private static readonly List<KeyValuePair<string, JsonValue>> EmptyMembers = new List<KeyValuePair<string, JsonValue>>();

        public IReadOnlyList<JsonValue> Items
        {
            get { return _items ?? (IReadOnlyList<JsonValue>)EmptyItems; }
        }

        public IReadOnlyList<KeyValuePair<string, JsonValue>> Members
        {
            get { return _members ?? (IReadOnlyList<KeyValuePair<string, JsonValue>>)EmptyMembers; }
        }

        // ==========================================
        // IsScalar — anything that is not a container.
        // Public so a tree walker can decide whether a
        // node is worth descending into without having
        // to test both container kinds itself.
        // ==========================================
        public bool IsScalar
        {
            get { return Kind != JsonKind.Object && Kind != JsonKind.Array; }
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
            using (var writer = new StringWriter(sb, CultureInfo.InvariantCulture))
            {
                Write(writer, 0);
            }
            return sb.ToString();
        }

        // ==========================================
        // WriteTo(writer) — the same output as ToString(),
        // sent straight to a destination instead of built
        // in memory first.
        //
        // ToString() on a full Scene export is a single
        // string holding the entire document: a Scene with
        // tens of thousands of GameObjects produces tens of
        // megabytes of JSON, and writing it to disk meant
        // holding all of it, then handing File.WriteAllText a
        // copy to encode - two large-object-heap allocations
        // for a file that is written once, forwards, and
        // never re-read. Streaming it costs nothing extra
        // (the recursion is unchanged; only the sink
        // differs) and the peak no longer scales with the
        // size of the biggest Scene in the project.
        //
        // ToString() is kept, unchanged byte-for-byte, for
        // the callers that genuinely want a string - the
        // search index embedded in a page, and the tests.
        // ==========================================
        public void WriteTo(TextWriter writer)
        {
            if (writer == null) { throw new ArgumentNullException("writer"); }
            Write(writer, 0);
        }

        // ==========================================
        // ToCompactString() — a leaf-inlining variant of
        // ToString(). An object or array whose values are
        // ALL scalars is printed inline on a single line
        // ("{ "a": 1, "b": "x" }" / "[1, 2, 3]"); anything
        // holding a nested object/array still expands one
        // member per line. Used by the concise "simple"
        // JSON summaries so a small Scene is a few hundred
        // lines instead of one line per scalar. Does not
        // affect ToString(), so the full export is byte-
        // for-byte unchanged.
        // ==========================================
        public string ToCompactString()
        {
            var sb = new StringBuilder(256);
            using (var writer = new StringWriter(sb, CultureInfo.InvariantCulture))
            {
                WriteCompact(writer, 0);
            }
            return sb.ToString();
        }

        // ==========================================
        // WriteCompactTo(writer) — ToCompactString()
        // streamed, for the same reason as WriteTo.
        // ==========================================
        public void WriteCompactTo(TextWriter writer)
        {
            if (writer == null) { throw new ArgumentNullException("writer"); }
            WriteCompact(writer, 0);
        }

        private bool IsInlineable()
        {
            if (Kind == JsonKind.Object)
            {
                foreach (var m in _members) { if (!m.Value.IsScalar) { return false; } }
                return true;
            }
            if (Kind == JsonKind.Array)
            {
                foreach (var it in _items) { if (!it.IsScalar) { return false; } }
                return true;
            }
            return true;
        }

        private void WriteCompact(TextWriter writer, int indent)
        {
            switch (Kind)
            {
                case JsonKind.Object:
                    if (_members.Count == 0) { writer.Write("{}"); return; }
                    if (IsInlineable()) { WriteInlineObject(writer); return; }
                    WriteBlockObjectCompact(writer, indent);
                    return;
                case JsonKind.Array:
                    if (_items.Count == 0) { writer.Write("[]"); return; }
                    if (IsInlineable()) { WriteInlineArray(writer); return; }
                    WriteBlockArrayCompact(writer, indent);
                    return;
                default:
                    Write(writer, indent);
                    return;
            }
        }

        private void WriteInlineObject(TextWriter writer)
        {
            writer.Write("{ ");
            for (int i = 0; i < _members.Count; i++)
            {
                WriteEscapedString(writer, _members[i].Key);
                writer.Write(": ");
                _members[i].Value.Write(writer, 0);
                if (i < _members.Count - 1) { writer.Write(", "); }
            }
            writer.Write(" }");
        }

        private void WriteInlineArray(TextWriter writer)
        {
            writer.Write('[');
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].Write(writer, 0);
                if (i < _items.Count - 1) { writer.Write(", "); }
            }
            writer.Write(']');
        }

        private void WriteBlockObjectCompact(TextWriter writer, int indent)
        {
            writer.Write("{\n");
            string childPad = Pad(indent + 1);
            for (int i = 0; i < _members.Count; i++)
            {
                writer.Write(childPad);
                WriteEscapedString(writer, _members[i].Key);
                writer.Write(": ");
                _members[i].Value.WriteCompact(writer, indent + 1);
                if (i < _members.Count - 1) { writer.Write(','); }
                writer.Write('\n');
            }
            writer.Write(Pad(indent));
            writer.Write('}');
        }

        private void WriteBlockArrayCompact(TextWriter writer, int indent)
        {
            writer.Write("[\n");
            string childPad = Pad(indent + 1);
            for (int i = 0; i < _items.Count; i++)
            {
                writer.Write(childPad);
                _items[i].WriteCompact(writer, indent + 1);
                if (i < _items.Count - 1) { writer.Write(','); }
                writer.Write('\n');
            }
            writer.Write(Pad(indent));
            writer.Write(']');
        }

        // ==========================================
        // Write(writer, indent) — recursive pretty-printer.
        // ==========================================
        private void Write(TextWriter writer, int indent)
        {
            switch (Kind)
            {
                case JsonKind.Null:
                    writer.Write("null");
                    break;
                case JsonKind.Bool:
                    writer.Write(_boolValue ? "true" : "false");
                    break;
                case JsonKind.Number:
                    WriteNumber(writer, _numberValue);
                    break;
                case JsonKind.String:
                    WriteEscapedString(writer, _stringValue);
                    break;
                case JsonKind.Array:
                    WriteArrayItems(writer, indent);
                    break;
                case JsonKind.Object:
                    WriteObject(writer, indent);
                    break;
            }
        }

        // ==========================================
        // WriteNumber — avoids scientific notation
        // and trims trailing zeroes for readability.
        //
        // NaN and ±Infinity have no JSON representation.
        // They are written as null rather than 0: a Unity
        // float field genuinely can hold Infinity (an
        // uninitialised distance, a divide-by-zero left in
        // a serialized value), and reporting that as the
        // number zero is a documentation tool inventing a
        // value the project does not contain. null reads
        // back through AsNumber(fallback) exactly like the
        // old 0 for every renderer, but no longer lies to
        // anyone reading the JSON directly.
        // ==========================================
        private static void WriteNumber(TextWriter writer, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                writer.Write("null");
                return;
            }
            if (value == System.Math.Floor(value) && System.Math.Abs(value) < 1e15)
            {
                writer.Write(((long)value).ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                writer.Write(value.ToString("R", CultureInfo.InvariantCulture));
            }
        }

        // ==========================================
        // WriteObject / WriteArrayItems
        // ==========================================
        private void WriteObject(TextWriter writer, int indent)
        {
            if (_members.Count == 0)
            {
                writer.Write("{}");
                return;
            }
            writer.Write("{\n");
            string childPad = Pad(indent + 1);
            for (int i = 0; i < _members.Count; i++)
            {
                writer.Write(childPad);
                WriteEscapedString(writer, _members[i].Key);
                writer.Write(": ");
                _members[i].Value.Write(writer, indent + 1);
                if (i < _members.Count - 1) { writer.Write(','); }
                writer.Write('\n');
            }
            writer.Write(Pad(indent));
            writer.Write('}');
        }

        private void WriteArrayItems(TextWriter writer, int indent)
        {
            if (_items.Count == 0)
            {
                writer.Write("[]");
                return;
            }
            writer.Write("[\n");
            string childPad = Pad(indent + 1);
            for (int i = 0; i < _items.Count; i++)
            {
                writer.Write(childPad);
                _items[i].Write(writer, indent + 1);
                if (i < _items.Count - 1) { writer.Write(','); }
                writer.Write('\n');
            }
            writer.Write(Pad(indent));
            writer.Write(']');
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
        private static void WriteEscapedString(TextWriter writer, string value)
        {
            writer.Write('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': writer.Write("\\\""); break;
                    case '\\': writer.Write("\\\\"); break;
                    case '\n': writer.Write("\\n"); break;
                    case '\r': writer.Write("\\r"); break;
                    case '\t': writer.Write("\\t"); break;
                    case '<': writer.Write("\\u003c"); break;
                    case '>': writer.Write("\\u003e"); break;
                    default:
                        if (c < 0x20)
                        {
                            writer.Write("\\u");
                            writer.Write(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            writer.Write(c);
                        }
                        break;
                }
            }
            writer.Write('"');
        }
    }
}
