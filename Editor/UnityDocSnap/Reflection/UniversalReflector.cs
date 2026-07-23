// ==========================================
// UniversalReflector
// Walks a UnityEngine.Object's SerializedObject
// exactly the way the Inspector would, turning
// every visible property into a JsonValue node.
// This single reflector backs Components, Asset
// Importers, Materials, ScriptableObjects, and
// Prefab contents alike, so "every option" stays
// accurate even for types this tool never
// explicitly special-cased.
// ==========================================
using System;
using AmirCollider.UnityDocSnap.Editor.Json;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor.Reflection
{
    internal static class UniversalReflector
    {
        // ==========================================
        // ReadTopLevelFields
        // Entry point: returns a JsonValue array of
        // every visible top-level serialized field
        // on the given object.
        // ==========================================
        public static JsonValue ReadTopLevelFields(UnityEngine.Object target)
        {
            var results = JsonValue.Arr();
            if (target == null) { return results; }

            SerializedObject serializedObject;
            try
            {
                serializedObject = new SerializedObject(target);
            }
            catch (Exception ex)
            {
                results.Add(JsonValue.Obj().Set("name", "(reflection error)").Set("kind", "error").Set("value", ex.Message));
                return results;
            }

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                results.Add(ReadField(iterator, 0));
            }
            return results;
        }

        // ==========================================
        // ReadField
        // Converts one SerializedProperty (and, when
        // it is a container, everything nested inside
        // it) into a single JsonValue node.
        // ==========================================
        public static JsonValue ReadField(SerializedProperty prop, int depth, int arrayDepth = 0)
        {
            var node = JsonValue.Obj();
            node.Set("name", prop.name);
            if (!string.IsNullOrEmpty(prop.displayName) && prop.displayName != prop.name)
            {
                node.Set("label", prop.displayName);
            }

            // Prefab override marker. SerializedProperty.prefabOverride is
            // true when this exact value was changed on a Prefab instance
            // relative to its Prefab asset - the single most useful thing a
            // documentation tool can surface about a Prefab instance ("this
            // was tweaked here"). It is always false for a plain object, so
            // it is only recorded when true to keep the export lean. Reading
            // it is cheap and cannot throw.
            if (prop.prefabOverride) { node.Set("prefabOverride", true); }

            if (depth > DocSnapConstants.MaxGenericRecursionDepth)
            {
                node.Set("kind", "unsupported");
                node.Set("value", "(max nesting depth reached)");
                return node;
            }

            try
            {
               if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
                {
                    return ReadArray(prop, node, depth, arrayDepth);
                }

                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        node.Set("kind", "int");
                        node.Set("value", prop.longValue);
                        break;
                    case SerializedPropertyType.Boolean:
                        node.Set("kind", "bool");
                        node.Set("value", prop.boolValue);
                        break;
                    case SerializedPropertyType.Float:
                        node.Set("kind", "float");
                        node.Set("value", prop.doubleValue);
                        break;
                    case SerializedPropertyType.String:
                        node.Set("kind", "string");
                        node.Set("value", prop.stringValue ?? "");
                        break;
                    case SerializedPropertyType.Color:
                        node.Set("kind", "color");
                        node.Set("value", ColorToHex(prop.colorValue));
                        node.Set("alpha", prop.colorValue.a);
                        break;
                    case SerializedPropertyType.ObjectReference:
                        ReadObjectReference(prop, node);
                        break;
                    case SerializedPropertyType.LayerMask:
                        node.Set("kind", "layerMask");
                        node.Set("value", prop.intValue);
                        break;
                    case SerializedPropertyType.Enum:
                        ReadEnum(prop, node);
                        break;
                    case SerializedPropertyType.Vector2:
                        node.Set("kind", "vector2");
                        node.Set("value", Vec2(prop.vector2Value));
                        break;
                    case SerializedPropertyType.Vector3:
                        node.Set("kind", "vector3");
                        node.Set("value", Vec3(prop.vector3Value));
                        break;
                    case SerializedPropertyType.Vector4:
                        node.Set("kind", "vector4");
                        node.Set("value", Vec4(prop.vector4Value));
                        break;
                    case SerializedPropertyType.Rect:
                        node.Set("kind", "rect");
                        node.Set("value", RectObj(prop.rectValue));
                        break;
                    case SerializedPropertyType.ArraySize:
                        node.Set("kind", "int");
                        node.Set("value", prop.intValue);
                        break;
                    case SerializedPropertyType.Character:
                        node.Set("kind", "int");
                        node.Set("value", prop.intValue);
                        break;
                    case SerializedPropertyType.AnimationCurve:
                        node.Set("kind", "curve");
                        node.Set("value", CurveSummary(prop.animationCurveValue));
                        break;
                    case SerializedPropertyType.Bounds:
                        node.Set("kind", "bounds");
                        node.Set("value", BoundsObj(prop.boundsValue));
                        break;
                    case SerializedPropertyType.Gradient:
                        node.Set("kind", "gradient");
                        node.Set("value", GradientSummary(prop));
                        break;
                    case SerializedPropertyType.Quaternion:
                        node.Set("kind", "quaternion");
                        node.Set("value", Vec3(prop.quaternionValue.eulerAngles));
                        break;
                    case SerializedPropertyType.ExposedReference:
                        ReadExposedReference(prop, node);
                        break;
                    case SerializedPropertyType.FixedBufferSize:
                        node.Set("kind", "int");
                        node.Set("value", prop.intValue);
                        break;
                    case SerializedPropertyType.Vector2Int:
                        node.Set("kind", "vector2int");
                        node.Set("value", Vec2Int(prop.vector2IntValue));
                        break;
                    case SerializedPropertyType.Vector3Int:
                        node.Set("kind", "vector3int");
                        node.Set("value", Vec3Int(prop.vector3IntValue));
                        break;
                    case SerializedPropertyType.RectInt:
                        node.Set("kind", "rectint");
                        node.Set("value", RectIntObj(prop.rectIntValue));
                        break;
                    case SerializedPropertyType.BoundsInt:
                        node.Set("kind", "boundsint");
                        node.Set("value", BoundsIntObj(prop.boundsIntValue));
                        break;
                    case SerializedPropertyType.ManagedReference:
                        ReadManagedReference(prop, node, depth, arrayDepth);
                        break;
                    case SerializedPropertyType.Hash128:
                        node.Set("kind", "hash128");
                        node.Set("value", prop.hash128Value.ToString());
                        break;
                    case SerializedPropertyType.Generic:
                        ReadGeneric(prop, node, depth, arrayDepth);
                        break;
                    default:
                        // Covers property types added by newer Unity
                        // versions (e.g. RenderingLayerMask) without
                        // taking a hard compile-time dependency on them.
                        ReadUnknownPropertyType(prop, node);
                        break;
                }
            }
            catch (Exception ex)
            {
                node.Set("kind", "error");
                node.Set("value", ex.Message);
            }

            return node;
        }

        // ==========================================
        // ReadArray
        // Walks a SerializedProperty array/list,
        // capping how many elements get expanded so
        // a huge array cannot blow up the export. An
        // array reached from inside another array
        // (jagged/nested arrays) uses the much smaller
        // MaxNestedArrayElementsRendered cap instead -
        // this is what used to multiply into tens of
        // thousands of exported rows for a single field.
        // ==========================================
        private static JsonValue ReadArray(SerializedProperty prop, JsonValue node, int depth, int arrayDepth)
        {
            node.Set("kind", "array");
            int count = prop.arraySize;
            node.Set("count", count);

            int cap = arrayDepth > 0 ? DocSnapConstants.MaxNestedArrayElementsRendered : DocSnapConstants.MaxArrayElementsRendered;
            var itemsArr = JsonValue.Arr();
            int limit = Math.Min(count, cap);
            for (int i = 0; i < limit; i++)
            {
                SerializedProperty element = prop.GetArrayElementAtIndex(i);
                itemsArr.Add(ReadField(element, depth + 1, arrayDepth + 1));
            }
            node.Set("items", itemsArr);
            node.Set("truncated", count > limit);
            return node;
        }

        // ==========================================
        // ReadGeneric
        // Walks the children of a nested struct/class
        // (a non-array Generic property) one level at
        // a time using the standard SerializedProperty
        // sibling-walk-until-end-marker idiom. arrayDepth
        // passes through unchanged here - descending into
        // a struct's own fields does not enter a new
        // array nesting level.
        // ==========================================
        private static JsonValue ReadGeneric(SerializedProperty prop, JsonValue node, int depth, int arrayDepth)
        {
            node.Set("kind", "generic");
            node.Set("typeName", string.IsNullOrEmpty(prop.type) ? "Generic" : prop.type);

            var fieldsArr = JsonValue.Arr();
            SerializedProperty endProperty = prop.GetEndProperty();
            SerializedProperty current = prop.Copy();
            bool enterChildren = true;
            int guard = 0;
            while (current.NextVisible(enterChildren) && !SerializedProperty.EqualContents(current, endProperty))
            {
                enterChildren = false;
                fieldsArr.Add(ReadField(current, depth + 1, arrayDepth));
                guard++;
                if (guard > 1000) { break; }
            }
            node.Set("fields", fieldsArr);
            return node;
        }

        // ==========================================
        // ReadManagedReference
        // Handles [SerializeReference] polymorphic
        // fields: records the concrete runtime type,
        // then walks its children the same way as a
        // Generic struct. arrayDepth passes through
        // unchanged for the same reason as ReadGeneric.
        // ==========================================
        private static void ReadManagedReference(SerializedProperty prop, JsonValue node, int depth, int arrayDepth)
        {
            node.Set("kind", "managedRef");
            string fullTypeName = prop.managedReferenceFullTypename;
            bool isNull = string.IsNullOrEmpty(fullTypeName);
            node.Set("isNull", isNull);
            node.Set("typeName", isNull ? "(null)" : SimplifyManagedTypeName(fullTypeName));
            if (isNull) { return; }

            var fieldsArr = JsonValue.Arr();
            SerializedProperty endProperty = prop.GetEndProperty();
            SerializedProperty current = prop.Copy();
            bool enterChildren = true;
            int guard = 0;
            while (current.NextVisible(enterChildren) && !SerializedProperty.EqualContents(current, endProperty))
            {
                enterChildren = false;
                fieldsArr.Add(ReadField(current, depth + 1, arrayDepth));
                guard++;
                if (guard > 1000) { break; }
            }
            node.Set("fields", fieldsArr);
        }

        // ==========================================
        // ReadObjectReference
        // Captures a reference field as a connection:
        // who it points to, whether that target is a
        // project asset (with a resolvable GUID) or a
        // Scene object, and whether the link is broken.
        // ==========================================
        private static void ReadObjectReference(SerializedProperty prop, JsonValue node)
        {
            node.Set("kind", "objectRef");
            node.Set("refType", ExtractPPtrTypeName(prop.type));

            UnityEngine.Object target = prop.objectReferenceValue;
            if (target == null)
            {
                bool isMissing = prop.objectReferenceInstanceIDValue != 0;
                node.Set("isNull", !isMissing);
                node.Set("isMissing", isMissing);
                return;
            }

            node.Set("isNull", false);
            node.Set("isMissing", false);
            node.Set("targetName", target.name);
            node.Set("targetTypeName", target.GetType().Name);
            node.Set("targetInstanceId", target.GetInstanceID());

            string assetPath = AssetDatabase.GetAssetPath(target);
            bool isAsset = !string.IsNullOrEmpty(assetPath);
            node.Set("isAsset", isAsset);
            if (isAsset)
            {
                node.Set("targetPath", assetPath);
                node.Set("targetGuid", AssetDatabase.AssetPathToGUID(assetPath));
            }
        }

        // ==========================================
        // ReadExposedReference
        // Best-effort read of Timeline-style exposed
        // references (resolution depends on a binding
        // context that a plain Editor pass may lack).
        // ==========================================
        private static void ReadExposedReference(SerializedProperty prop, JsonValue node)
        {
            node.Set("kind", "exposedRef");
            UnityEngine.Object resolved = null;
            try { resolved = prop.exposedReferenceValue; }
            catch { /* no resolver bound in this context; leave unresolved */ }
            node.Set("targetName", resolved != null ? resolved.name : null);
            node.Set("isNull", resolved == null);
        }

        // ==========================================
        // ReadEnum
        // Resolves the human-readable enum label
        // instead of the raw underlying integer.
        // ==========================================
        private static void ReadEnum(SerializedProperty prop, JsonValue node)
        {
            node.Set("kind", "enum");
            string[] names = prop.enumDisplayNames;
            int index = prop.enumValueIndex;
            if (names != null && index >= 0 && index < names.Length)
            {
                node.Set("value", names[index]);
            }
            else
            {
                node.Set("value", prop.intValue.ToString());
            }
        }

        // ==========================================
        // ReadUnknownPropertyType
        // Dispatches a property whose SerializedPropertyType
        // this reflector does not explicitly recognise (i.e.
        // added by a newer Unity release than this file was
        // written against). The previous approach here tried
        // longValue/stringValue/boolValue in sequence, which
        // looked safe (each wrapped in try/catch) but is not:
        // for property types like RenderingLayerMask, a
        // mismatched accessor makes Unity's native layer log
        // "type is not a supported int value" directly via
        // Debug.LogError, rather than throwing a catchable
        // managed exception - so no try/catch around that call
        // can ever stop the spam, no matter how it is ordered.
        // boxedValue is the one API built specifically to read
        // a property's value without the caller needing to
        // know its concrete type in advance, so it is used
        // exclusively here instead of guessing with typed
        // accessors, with console logging additionally muted
        // for the duration of the call as a second safety net.
        // On Unity versions old enough to predate boxedValue,
        // the exotic new property types that trigger this in
        // the first place do not exist either, so the plain
        // string/bool fallback stays safe there.
        // ==========================================
        private static void ReadUnknownPropertyType(SerializedProperty prop, JsonValue node)
        {
            string typeName = prop.propertyType.ToString();
            node.Set("kind", string.IsNullOrEmpty(typeName) ? "raw" : "raw:" + typeName);

#if UNITY_2022_2_OR_NEWER
            bool previousLogEnabled = Debug.unityLogger.logEnabled;
            Debug.unityLogger.logEnabled = false;
            try
            {
                object boxed = prop.boxedValue;
                node.Set("value", boxed != null ? boxed.ToString() : "(null)");
            }
            catch (Exception ex)
            {
                node.Set("value", "(unsupported property type: " + prop.propertyType + " - " + ex.Message + ")");
            }
            finally
            {
                Debug.unityLogger.logEnabled = previousLogEnabled;
            }
#else
            try { node.Set("value", prop.stringValue); return; } catch { /* not string-like */ }
            try { node.Set("value", prop.boolValue.ToString()); return; } catch { /* not bool-like */ }
            node.Set("value", "(unsupported property type: " + prop.propertyType + ")");
#endif
        }

        // ==========================================
        // ExtractPPtrTypeName
        // SerializedProperty.type for object reference
        // fields comes back as "PPtr<$TypeName>"; this
        // pulls out the readable TypeName.
        // ==========================================
        private static string ExtractPPtrTypeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) { return "Object"; }
            if (raw.StartsWith("PPtr<$", StringComparison.Ordinal) && raw.EndsWith(">", StringComparison.Ordinal))
            {
                return raw.Substring(6, raw.Length - 7);
            }
            return raw;
        }

        // ==========================================
        // SimplifyManagedTypeName
        // managedReferenceFullTypename is formatted as
        // "AssemblyName Namespace.ClassName"; this
        // keeps just the ClassName for display.
        // ==========================================
        private static string SimplifyManagedTypeName(string fullTypeName)
        {
            int spaceIndex = fullTypeName.IndexOf(' ');
            string typePart = spaceIndex >= 0 ? fullTypeName.Substring(spaceIndex + 1) : fullTypeName;
            int dotIndex = typePart.LastIndexOf('.');
            return dotIndex >= 0 ? typePart.Substring(dotIndex + 1) : typePart;
        }

        // ==========================================
        // Value-shape helpers (Vector/Rect/Bounds/etc.)
        // ==========================================
        private static JsonValue Vec2(Vector2 v)
        {
            return JsonValue.Obj().Set("x", v.x).Set("y", v.y);
        }

        private static JsonValue Vec3(Vector3 v)
        {
            return JsonValue.Obj().Set("x", v.x).Set("y", v.y).Set("z", v.z);
        }

        private static JsonValue Vec4(Vector4 v)
        {
            return JsonValue.Obj().Set("x", v.x).Set("y", v.y).Set("z", v.z).Set("w", v.w);
        }

        private static JsonValue Vec2Int(Vector2Int v)
        {
            return JsonValue.Obj().Set("x", v.x).Set("y", v.y);
        }

        private static JsonValue Vec3Int(Vector3Int v)
        {
            return JsonValue.Obj().Set("x", v.x).Set("y", v.y).Set("z", v.z);
        }

        private static JsonValue RectObj(Rect r)
        {
            return JsonValue.Obj().Set("x", r.x).Set("y", r.y).Set("width", r.width).Set("height", r.height);
        }

        private static JsonValue RectIntObj(RectInt r)
        {
            return JsonValue.Obj().Set("x", r.x).Set("y", r.y).Set("width", r.width).Set("height", r.height);
        }

        private static JsonValue BoundsObj(Bounds b)
        {
            return JsonValue.Obj().Set("center", Vec3(b.center)).Set("size", Vec3(b.size));
        }

        private static JsonValue BoundsIntObj(BoundsInt b)
        {
            return JsonValue.Obj().Set("position", Vec3Int(b.position)).Set("size", Vec3Int(b.size));
        }

        private static string ColorToHex(Color c)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(c);
        }

        private static JsonValue CurveSummary(AnimationCurve curve)
        {
            var obj = JsonValue.Obj();
            int keyCount = curve != null ? curve.length : 0;
            obj.Set("keyCount", keyCount);
            var keys = JsonValue.Arr();
            if (curve != null)
            {
                int limit = Math.Min(keyCount, 20);
                for (int i = 0; i < limit; i++)
                {
                    Keyframe k = curve[i];
                    keys.Add(JsonValue.Obj().Set("time", k.time).Set("value", k.value));
                }
            }
            obj.Set("keys", keys);
            return obj;
        }

        private static JsonValue GradientSummary(SerializedProperty prop)
        {
            var obj = JsonValue.Obj();
            var colorKeys = JsonValue.Arr();
            var alphaKeys = JsonValue.Arr();

            Gradient gradient = null;
            try { gradient = prop.gradientValue; } catch { /* unavailable in this Unity version */ }

            if (gradient != null)
            {
                foreach (GradientColorKey ck in gradient.colorKeys)
                {
                    colorKeys.Add(JsonValue.Obj().Set("time", ck.time).Set("color", ColorToHex(ck.color)));
                }
                foreach (GradientAlphaKey ak in gradient.alphaKeys)
                {
                    alphaKeys.Add(JsonValue.Obj().Set("time", ak.time).Set("alpha", ak.alpha));
                }
            }

            obj.Set("colorKeys", colorKeys);
            obj.Set("alphaKeys", alphaKeys);
            return obj;
        }
    }
}
