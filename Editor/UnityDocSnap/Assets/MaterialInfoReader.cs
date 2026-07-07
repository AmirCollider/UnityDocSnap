// ==========================================
// MaterialInfoReader
// Reads a Material's shader name and every
// exposed shader property (colors, floats,
// vectors, textures) using Shader's modern
// property-reflection API.
// ==========================================
using AmirCollider.UnityDocSnap.Editor.Json;
using UnityEngine;
using UnityEngine.Rendering;

namespace AmirCollider.UnityDocSnap.Editor.Assets
{
    internal static class MaterialInfoReader
    {
        // ==========================================
        // ReadMaterial
        // Builds a JsonValue describing the shader in
        // use and the live value of every property it
        // exposes on this specific Material instance.
        // ==========================================
        public static JsonValue ReadMaterial(Material material)
        {
            var node = JsonValue.Obj();
            Shader shader = material.shader;
            node.Set("shaderName", shader != null ? shader.name : "(none)");
            node.Set("renderQueue", material.renderQueue);

            var propertiesArr = JsonValue.Arr();
            if (shader != null)
            {
                int count = shader.GetPropertyCount();
                for (int i = 0; i < count; i++)
                {
                    propertiesArr.Add(ReadProperty(material, shader, i));
                }
            }
            node.Set("properties", propertiesArr);
            return node;
        }

        // ==========================================
        // ReadProperty
        // Converts one shader property slot into a
        // JsonValue node, dispatching by property type.
        // ==========================================
        private static JsonValue ReadProperty(Material material, Shader shader, int index)
        {
            string name = shader.GetPropertyName(index);
            var node = JsonValue.Obj().Set("name", name);

            ShaderPropertyType type;
            try { type = shader.GetPropertyType(index); }
            catch
            {
                node.Set("kind", "unsupported");
                return node;
            }

            switch (type)
            {
                case ShaderPropertyType.Color:
                    node.Set("kind", "color");
                    node.Set("value", "#" + ColorUtility.ToHtmlStringRGBA(material.GetColor(name)));
                    break;
                case ShaderPropertyType.Vector:
                    Vector4 v = material.GetVector(name);
                    node.Set("kind", "vector4");
                    node.Set("value", JsonValue.Obj().Set("x", v.x).Set("y", v.y).Set("z", v.z).Set("w", v.w));
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    node.Set("kind", "float");
                    node.Set("value", material.GetFloat(name));
                    break;
                case ShaderPropertyType.Texture:
                    ReadTextureProperty(material, name, node);
                    break;
                default:
                    // Covers newer property kinds (e.g. Int) added by
                    // later Unity versions without a hard dependency.
                    node.Set("kind", "raw");
                    TryReadFloatFallback(material, name, node);
                    break;
            }
            return node;
        }

        // ==========================================
        // ReadTextureProperty
        // Captures which texture asset is bound plus
        // its tiling/offset, without embedding pixels.
        // ==========================================
        private static void ReadTextureProperty(Material material, string name, JsonValue node)
        {
            node.Set("kind", "texture");
            Texture tex = material.GetTexture(name);
            node.Set("hasTexture", tex != null);
            if (tex == null) { return; }

            node.Set("textureName", tex.name);
            string path = UnityEditor.AssetDatabase.GetAssetPath(tex);
            if (!string.IsNullOrEmpty(path))
            {
                node.Set("texturePath", path);
                node.Set("textureGuid", UnityEditor.AssetDatabase.AssetPathToGUID(path));
            }

            Vector2 scale = material.GetTextureScale(name);
            Vector2 offset = material.GetTextureOffset(name);
            node.Set("tiling", JsonValue.Obj().Set("x", scale.x).Set("y", scale.y));
            node.Set("offset", JsonValue.Obj().Set("x", offset.x).Set("y", offset.y));
        }

        private static void TryReadFloatFallback(Material material, string name, JsonValue node)
        {
            try { node.Set("value", material.GetFloat(name)); }
            catch { /* not float-shaped; leave as raw with no value */ }
        }
    }
}
