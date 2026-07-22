// ==========================================
// FieldRenderer
// Turns the generic JsonValue field trees
// produced by UniversalReflector into readable
// HTML: value tables, component cards, a
// combined tree+detail GameObject view, and
// asset cards - with clickable cross-links
// wherever a reference can be resolved.
// ==========================================
using System;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Json;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Html
{
    // ==========================================
    // RefLinkResolver
    // Carries everything needed to turn an
    // "objectRef" field into either a working
    // cross-page/cross-anchor link or an honest
    // "not exported yet" note.
    // ==========================================
    internal sealed class RefLinkResolver
    {
        public Dictionary<string, ManifestAssetIndexEntry> GuidLookup;
        public Dictionary<int, string> LocalAnchors;
        public string LinkPrefix;

        public RefLinkResolver WithLocalAnchors(Dictionary<int, string> anchors)
        {
            return new RefLinkResolver { GuidLookup = GuidLookup, LocalAnchors = anchors, LinkPrefix = LinkPrefix };
        }

        // ==========================================
        // ResolveObjectRef
        // Renders one reference chip: missing, null,
        // linked-to-asset, linked-to-same-page, or
        // unresolved (target exists but was not part
        // of any export yet).
        // ==========================================
        public string ResolveObjectRef(JsonValue refNode)
        {
            if (refNode.Get("isMissing").AsBool())
            {
                return "<span class=\"ds-ref-chip is-missing\">\u26A0 " + HtmlPageBuilder.I18n("span", null, "missing reference", "参照が見つかりません", "رفرنس گم‌شده") + "</span>";
            }
            if (refNode.Get("isNull").AsBool(true))
            {
                return "<span class=\"ds-empty-note\">" + HtmlPageBuilder.I18n("span", null, "None", "なし", "هیچ‌کدام") + "</span>";
            }

            string targetName = refNode.Get("targetName").AsString("");
            string targetNameHtml = string.IsNullOrEmpty(targetName)
                ? HtmlPageBuilder.I18n("span", null, "(unnamed)", "（名称なし）", "(بدون‌نام)")
                : HtmlPageBuilder.Escape(targetName);
            string titleAttr = string.IsNullOrEmpty(targetName) ? "" : " title=\"" + HtmlPageBuilder.Escape(targetName) + "\"";
            string refType = refNode.Get("refType").AsString("Object");
            bool isAsset = refNode.Get("isAsset").AsBool();

            if (isAsset)
            {
                string guid = refNode.Get("targetGuid").AsString("");
                ManifestAssetIndexEntry entry;
                if (!string.IsNullOrEmpty(guid) && GuidLookup != null && GuidLookup.TryGetValue(guid, out entry))
                {
                    string href = LinkPrefix + entry.htmlFile + "#" + entry.anchor;
                    return "<a class=\"ds-ref-chip\"" + titleAttr + " href=\"" + href + "\">\uD83D\uDD17 " + targetNameHtml
                        + " <span class=\"type\">" + HtmlPageBuilder.Escape(refType) + "</span></a>";
                }
                return "<span class=\"ds-ref-chip is-unresolved\"" + titleAttr + ">" + targetNameHtml
                    + " <span class=\"type\">" + HtmlPageBuilder.Escape(refType) + " \u00B7 " + HtmlPageBuilder.I18n("span", null, "not exported yet", "未エクスポート", "هنوز اکسپورت نشده") + "</span></span>";
            }

            int instanceId = (int)refNode.Get("targetInstanceId").AsNumber(0);
            string anchor;
            if (LocalAnchors != null && LocalAnchors.TryGetValue(instanceId, out anchor))
            {
                return "<a class=\"ds-ref-chip\"" + titleAttr + " href=\"#" + anchor + "\">\uD83D\uDD17 " + targetNameHtml
                    + " <span class=\"type\">" + HtmlPageBuilder.Escape(refType) + "</span></a>";
            }
            return "<span class=\"ds-ref-chip is-unresolved\"" + titleAttr + ">" + targetNameHtml
                + " <span class=\"type\">" + HtmlPageBuilder.Escape(refType) + "</span></span>";
        }
    }

    internal static class FieldRenderer
    {
        // ==========================================
        // BuildLocalAnchors
        // Maps every GameObject's and Component's
        // instance id to that GameObject's anchor id,
        // so same-Scene (or same-Prefab) references
        // resolve to a working in-page link.
        // ==========================================
        public static Dictionary<int, string> BuildLocalAnchors(JsonValue rootObjects)
        {
            var map = new Dictionary<int, string>();
            foreach (JsonValue go in rootObjects.Items) { CollectAnchors(go, map); }
            return map;
        }

        private static void CollectAnchors(JsonValue go, Dictionary<int, string> map)
        {
            int goId = (int)go.Get("instanceId").AsNumber();
            string anchor = "go-" + goId;
            map[goId] = anchor;
            foreach (JsonValue comp in go.Get("components").Items)
            {
                if (comp.Has("instanceId"))
                {
                    map[(int)comp.Get("instanceId").AsNumber()] = anchor;
                }
            }
            foreach (JsonValue child in go.Get("children").Items) { CollectAnchors(child, map); }
        }

        // ==========================================
        // RenderHierarchy
        // Top-level entry for a Scene/Prefab tree:
        // expand/collapse controls plus every root
        // GameObject rendered as a merged tree+detail node.
        // ==========================================
        public static string RenderHierarchy(JsonValue rootObjects, RefLinkResolver resolver, string treeId)
        {
            var sb = new StringBuilder(1024);
            sb.Append("<div class=\"ds-card\"><div style=\"display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:8px;\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null, "Hierarchy", "\u30D2\u30A8\u30E9\u30EB\u30AD\u30FC", "Hierarchy"));
            sb.Append("<span><button type=\"button\" data-tree-expand=\"").Append(treeId).Append("\" data-mode=\"expand\" class=\"ds-badge lav\" style=\"cursor:pointer;border:none;\">").Append(HtmlPageBuilder.I18n("span", null, "Expand all", "すべて展開", "باز کردن همه")).Append("</button> ");
            sb.Append("<button type=\"button\" data-tree-expand=\"").Append(treeId).Append("\" data-mode=\"collapse\" class=\"ds-badge ghost\" style=\"cursor:pointer;border:1px solid var(--line);\">").Append(HtmlPageBuilder.I18n("span", null, "Collapse all", "すべて折りたたむ", "بستن همه")).Append("</button></span>");
            sb.Append("</div><ul class=\"ds-tree\" id=\"").Append(treeId).Append("\">\n");
            foreach (JsonValue go in rootObjects.Items) { sb.Append(RenderGoNode(go, resolver, true)); }
            sb.Append("</ul></div>\n");
            return sb.ToString();
        }

        // ==========================================
        // RenderGoNode
        // One GameObject as a collapsible node that
        // is simultaneously its own Inspector-style
        // detail card, with its children nested inside.
        // ==========================================
        public static string RenderGoNode(JsonValue go, RefLinkResolver resolver, bool openByDefault)
        {
            int id = (int)go.Get("instanceId").AsNumber();
            string name = go.Get("name").AsString("GameObject");
            bool activeSelf = go.Get("activeSelf").AsBool(true);
            string tag = go.Get("tag").AsString("Untagged");
            JsonValue children = go.Get("children");
            bool hasChildren = children.Items.Count > 0;

            var sb = new StringBuilder(512);
            sb.Append("<li id=\"go-").Append(id).Append("\">");
            sb.Append("<details class=\"ds-go").Append(activeSelf ? "" : " ds-go-inactive").Append("\"").Append(openByDefault ? " open" : "").Append(">");
            sb.Append("<summary>").Append(HtmlPageBuilder.Escape(name));
            if (!string.Equals(tag, "Untagged", StringComparison.Ordinal))
            {
                sb.Append(" <span class=\"ds-go-tag\">").Append(HtmlPageBuilder.Escape(tag)).Append("</span>");
            }
            sb.Append("</summary>\n");
            sb.Append(RenderGoDetailBody(go, resolver));
            if (hasChildren)
            {
                sb.Append("<ul>\n");
                foreach (JsonValue child in children.Items) { sb.Append(RenderGoNode(child, resolver, false)); }
                sb.Append("</ul>\n");
            }
            sb.Append("</details></li>\n");
            return sb.ToString();
        }

        // ==========================================
        // RenderGoDetailBody
        // Transform tiles, state badges, and every
        // Component's field table for one GameObject.
        // ==========================================
        private static string RenderGoDetailBody(JsonValue go, RefLinkResolver resolver)
        {
            var sb = new StringBuilder(512);
            sb.Append("<div class=\"ds-go-card-body\">");

            bool activeSelf = go.Get("activeSelf").AsBool(true);
            bool isStatic = go.Get("isStatic").AsBool(false);
            string layer = go.Get("layerName").AsString("Default");

            sb.Append("<div class=\"ds-badge-row\">");
            sb.Append(HtmlPageBuilder.BadgeRaw(activeSelf ? "mint" : "warn", activeSelf
                ? HtmlPageBuilder.I18n("span", null, "Active", "アクティブ", "فعال")
                : HtmlPageBuilder.I18n("span", null, "Inactive", "非アクティブ", "غیرفعال")));
            if (isStatic) { sb.Append(HtmlPageBuilder.BadgeRaw("lav", HtmlPageBuilder.I18n("span", null, "Static", "スタティック", "Static"))); }
            sb.Append(HtmlPageBuilder.BadgeRaw(null, HtmlPageBuilder.I18n("span", null, "Layer", "レイヤー", "لایه") + ": " + HtmlPageBuilder.Escape(layer)));
            sb.Append("</div>");

            JsonValue t = go.Get("transform");
            sb.Append("<div class=\"ds-transform-grid\">");
            sb.Append(TransformTile("Position", t.Get("localPosition")));
            sb.Append(TransformTile("Rotation", t.Get("localEulerAngles")));
            sb.Append(TransformTile("Scale", t.Get("localScale")));
            sb.Append("</div>");

            foreach (JsonValue comp in go.Get("components").Items)
            {
                sb.Append(RenderComponent(comp, resolver));
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        private static string TransformTile(string label, JsonValue v)
        {
            return "<div class=\"ds-transform-tile\"><span class=\"lbl\">" + label + "</span>" + VecXYZ(v) + "</div>";
        }

        // ==========================================
        // RenderComponent
        // One Component card: header (name, on/off
        // state) plus its full field table, or a
        // warning card for a missing script.
        // ==========================================
        public static string RenderComponent(JsonValue comp, RefLinkResolver resolver)
        {
            if (comp.Get("isMissing").AsBool())
            {
                return "<div class=\"ds-component is-missing\"><div class=\"ds-component-head\">\u26A0 " + HtmlPageBuilder.I18n("span", null, "Missing Script", "スクリプトが見つかりません", "اسکریپت گم‌شده") + "</div></div>\n";
            }

            bool isUserScript = comp.Get("isUserScript").AsBool();
            string typeName = comp.Get("typeName").AsString("Component");

            var sb = new StringBuilder(512);
            sb.Append("<div class=\"ds-component").Append(isUserScript ? " is-user-script" : "").Append("\">");
            sb.Append("<div class=\"ds-component-head\">").Append(HtmlPageBuilder.Escape(typeName));
            if (comp.Get("isBehaviour").AsBool())
            {
                bool enabled = comp.Get("enabled").AsBool(true);
                sb.Append("<span class=\"ds-component-toggle ").Append(enabled ? "on" : "off").Append("\">").Append(enabled ? "ON" : "OFF").Append("</span>");
            }
            sb.Append("</div><div style=\"padding:8px 16px 14px;\">");
            sb.Append(RenderFieldTable(comp.Get("fields"), resolver));
            sb.Append("</div></div>\n");
            return sb.ToString();
        }

       // ==========================================
        // RenderFieldTable
        // A Field / Type / Value grid for one array of
        // field nodes (top-level or nested). Built on
        // CSS Grid (.ds-field-grid, rows using
        // display:contents) instead of a <table>, so
        // this grid's own column tracks are sized from
        // its own available width at every nesting
        // level - nesting one of these inside another
        // (a struct inside a struct, a field list inside
        // a card, ...) can no longer compound into an
        // ever-narrower fixed percentage the way nested
        // <table>s used to.
        // ==========================================
        public static string RenderFieldTable(JsonValue fieldsArray, RefLinkResolver resolver)
        {
            if (fieldsArray == null || fieldsArray.Items.Count == 0)
            {
                return "<p class=\"ds-empty-note\">" + HtmlPageBuilder.I18n("span", null, "No fields.", "フィールドはありません。", "فیلدی وجود نداره.") + "</p>";
            }
            var sb = new StringBuilder(1024);
            sb.Append("<div class=\"ds-field-grid\">");
            sb.Append("<div class=\"ds-field-grid-head\">")
              .Append(HtmlPageBuilder.I18n("span", null, "Field", "フィールド", "فیلد"))
              .Append(HtmlPageBuilder.I18n("span", null, "Type", "型", "نوع"))
              .Append(HtmlPageBuilder.I18n("span", null, "Value", "値", "مقدار"))
              .Append("</div>\n");
            foreach (JsonValue field in fieldsArray.Items)
            {
                string name = field.Has("label") ? field.Get("label").AsString("") : field.Get("name").AsString("");
                string kind = field.Get("kind").AsString("raw");
                sb.Append("<div class=\"ds-field-grid-row\">");
                sb.Append("<div class=\"ds-field-name\">").Append(HtmlPageBuilder.Escape(name)).Append("</div>");
                sb.Append("<div class=\"ds-field-type\">").Append(HtmlPageBuilder.Escape(kind)).Append("</div>");
                sb.Append("<div class=\"ds-field-value\">").Append(RenderValue(field, resolver, 0)).Append("</div>");
                sb.Append("</div>\n");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        // ==========================================
        // RenderValue
        // Dispatches a single field node to the right
        // presentation by its "kind" tag.
        // ==========================================
        public static string RenderValue(JsonValue field, RefLinkResolver resolver, int depth)
        {
            string kind = field.Get("kind").AsString("raw");
            switch (kind)
            {
                case "int":
                case "float":
                    return FormatNumber(field.Get("value").AsNumber());
                case "bool":
                    bool b = field.Get("value").AsBool();
                    return "<span class=\"ds-pill " + (b ? "bool-true" : "bool-false") + "\">" + (b ? "\u2713 true" : "\u2717 false") + "</span>";
                case "string":
                    return "<span>" + HtmlPageBuilder.Escape(field.Get("value").AsString("")) + "</span>";
                case "enum":
                    return "<span class=\"ds-pill enum\">" + HtmlPageBuilder.Escape(field.Get("value").AsString("")) + "</span>";
                case "color":
                    string hex = field.Get("value").AsString("#ffffff");
                    return "<span class=\"ds-swatch\" style=\"background:" + hex + "\"></span>" + HtmlPageBuilder.Escape(hex);
                case "layerMask":
                    return HtmlPageBuilder.I18n("span", null, "Layer Mask", "レイヤーマスク", "لایه‌ماسک") + ": " + (int)field.Get("value").AsNumber();
                case "objectRef":
                    return resolver.ResolveObjectRef(field);
                case "vector2":
                    return VecXY(field.Get("value"));
                case "vector2int":
                    return VecXY(field.Get("value"));
                case "vector3":
                case "quaternion":
                    return VecXYZ(field.Get("value"));
                case "vector3int":
                    return VecXYZ(field.Get("value"));
                case "vector4":
                    return VecXYZW(field.Get("value"));
                case "rect":
                case "rectint":
                    {
                        JsonValue v = field.Get("value");
                        return "x:" + FormatNumber(v.Get("x").AsNumber()) + " y:" + FormatNumber(v.Get("y").AsNumber())
                            + " w:" + FormatNumber(v.Get("width").AsNumber()) + " h:" + FormatNumber(v.Get("height").AsNumber());
                    }
                case "bounds":
                    {
                        JsonValue v = field.Get("value");
                        return "center " + VecInline(v.Get("center")) + " \u00B7 size " + VecInline(v.Get("size"));
                    }
                case "boundsint":
                    {
                        JsonValue v = field.Get("value");
                        return "pos " + VecInline(v.Get("position")) + " \u00B7 size " + VecInline(v.Get("size"));
                    }
                case "curve":
                    return HtmlPageBuilder.I18n("span", null, "Animation Curve", "アニメーションカーブ", "منحنی انیمیشن") + " (" + (int)field.Get("value").Get("keyCount").AsNumber() + " " + HtmlPageBuilder.I18n("span", null, "keys", "キー", "کلید") + ")";
                case "gradient":
                    return HtmlPageBuilder.I18n("span", null, "Gradient", "グラデーション", "گرادیان") + " (" + field.Get("value").Get("colorKeys").Items.Count + " " + HtmlPageBuilder.I18n("span", null, "color keys", "カラーキー", "کلید رنگ") + ")";
                case "hash128":
                    return "<span class=\"mono\">" + HtmlPageBuilder.Escape(field.Get("value").AsString("")) + "</span>";
                case "exposedRef":
                    return field.Get("isNull").AsBool(true)
                        ? "<span class=\"ds-empty-note\">" + HtmlPageBuilder.I18n("span", null, "unresolved", "未解決", "حل‌نشده") + "</span>"
                        : HtmlPageBuilder.Escape(field.Get("targetName").AsString(""));
                case "managedRef":
                    return RenderManagedRef(field, resolver, depth);
                case "generic":
                    return RenderGeneric(field, resolver, depth);
                case "array":
                    return RenderArray(field, resolver, depth);
                case "error":
                    return "<span class=\"ds-ref-chip is-missing\">\u26A0 " + HtmlPageBuilder.Escape(field.Get("value").AsString("")) + "</span>";
                default:
                    return "<span class=\"mono\">" + HtmlPageBuilder.Escape(field.Get("value").AsString("")) + "</span>";
            }
        }

        // ==========================================
        // RenderGeneric
        // Renders a nested struct/class field as its
        // own boxed field grid (see .ds-nested-block).
        // ==========================================
        private static string RenderGeneric(JsonValue field, RefLinkResolver resolver, int depth)
        {
            JsonValue fields = field.Get("fields");
            if (fields.Items.Count == 0) { return "<span class=\"ds-empty-note\">" + HtmlPageBuilder.I18n("span", null, "(empty)", "（空）", "(خالی)") + "</span>"; }
            return "<div class=\"ds-nested-block\">" + RenderFieldTable(fields, resolver) + "</div>";
        }
        // ==========================================
        // RenderManagedRef
        // Renders a [SerializeReference] polymorphic
        // field as its concrete type name plus its own
        // boxed field grid (see .ds-nested-block).
        // ==========================================
        private static string RenderManagedRef(JsonValue field, RefLinkResolver resolver, int depth)
        {
            string typeName = field.Get("typeName").AsString("");
            if (field.Get("isNull").AsBool())
            {
                return "<span class=\"ds-empty-note\">" + HtmlPageBuilder.I18n("span", null, "None", "なし", "هیچ‌کدام") + " (" + HtmlPageBuilder.Escape(typeName) + ")</span>";
            }
            var sb = new StringBuilder(256);
            sb.Append("<div class=\"ds-nested-block\"><div class=\"ds-nested-block-title\">").Append(HtmlPageBuilder.Escape(typeName)).Append("</div>");
            sb.Append(RenderFieldTable(field.Get("fields"), resolver));
            sb.Append("</div>");
            return sb.ToString();
        }

        // ==========================================
        // CompactArrayItemKinds
        // Field "kind" values short/simple enough to
        // render as a fixed-width grid cell inside
        // RenderArray's scrollable box (see
        // .ds-array-grid / .ds-array-cell). Anything
        // not in this set (generic structs, managedRef,
        // nested arrays not already handled by
        // IsScalarMatrix) keeps its own full-width
        // block line instead, since it can be
        // arbitrarily large/complex.
        // ==========================================
        private static readonly HashSet<string> CompactArrayItemKinds = new HashSet<string>
        {
            "int", "float", "bool", "string", "enum", "color", "layerMask",
            "vector2", "vector2int", "vector3", "vector3int", "vector4",
            "quaternion", "rect", "rectint", "hash128", "objectRef", "error",
            "curve", "gradient", "bounds", "boundsint", "exposedRef"
        };

        // ==========================================
        // IsScalarMatrix
        // True when every element of this array is
        // itself an array whose own elements are all
        // "compact" kinds - a genuine jagged/2D numeric
        // matrix (vertex lists, transform matrices,
        // per-bone weight tables, ...). These render as
        // one real row/column grid (see RenderMatrix)
        // instead of nesting an array-grid inside an
        // array-block for every row, which is what let
        // ordinary numeric data keep recreating the
        // character-splitting bug no matter how many
        // times the flat-array case alone got patched.
        // ==========================================
        private static bool IsScalarMatrix(JsonValue items)
        {
            if (items.Items.Count == 0) { return false; }
            foreach (JsonValue item in items.Items)
            {
                if (item.Get("kind").AsString("") != "array") { return false; }
                JsonValue innerItems = item.Get("items");
                if (innerItems.Items.Count == 0) { return false; }
                foreach (JsonValue inner in innerItems.Items)
                {
                    if (!CompactArrayItemKinds.Contains(inner.Get("kind").AsString(""))) { return false; }
                }
            }
            return true;
        }

        // ==========================================
        // RenderArray
        // Dispatches to the dedicated matrix renderer
        // for a genuine jagged/2D numeric array, or to
        // the flat/mixed renderer otherwise. Either way,
        // every leaf scalar value flows through the same
        // nowrap+ellipsis cell (see RenderFlatOrMixedArray
        // / .ds-array-cell), so a value can never be
        // broken mid-character no matter how deep the
        // nesting goes.
        // ==========================================
        private static string RenderArray(JsonValue field, RefLinkResolver resolver, int depth)
        {
            int count = (int)field.Get("count").AsNumber();
            if (count == 0) { return "<span class=\"ds-empty-note\">" + HtmlPageBuilder.I18n("span", null, "Empty array", "空の配列", "آرایه‌ی خالی") + "</span>"; }

            JsonValue items = field.Get("items");
            string body = IsScalarMatrix(items)
                ? RenderMatrix(items, resolver)
                : RenderFlatOrMixedArray(items, resolver, depth);

            if (!field.Get("truncated").AsBool()) { return body; }

            return body + "<div class=\"ds-array-more\">\u2026" + (count - items.Items.Count) + " "
                + HtmlPageBuilder.I18n("span", null, "more (truncated)", "件省略", "مورد دیگر (کوتاه‌شده)") + "</div>";
        }

        // ==========================================
        // RenderMatrix
        // Renders a jagged/2D scalar array as one real,
        // sticky-header, horizontally+vertically
        // scrollable table (see .ds-matrix-scroll /
        // .ds-matrix-table) - rows and columns exactly
        // like a spreadsheet, every cell's own width
        // driven by table layout, never by an ancestor's
        // shrinking fixed percentage. Rows shorter than
        // the widest row are padded with a placeholder
        // cell instead of throwing.
        // ==========================================
        private static string RenderMatrix(JsonValue rows, RefLinkResolver resolver)
        {
            int colCount = 0;
            bool anyRowTruncated = false;
            foreach (JsonValue row in rows.Items)
            {
                colCount = Math.Max(colCount, row.Get("items").Items.Count);
                if (row.Get("truncated").AsBool()) { anyRowTruncated = true; }
            }

            var sb = new StringBuilder(1024);
            sb.Append("<div class=\"ds-matrix-scroll\"><table class=\"ds-matrix-table\"><thead><tr><th></th>");
            for (int c = 0; c < colCount; c++) { sb.Append("<th>").Append(c).Append("</th>"); }
            sb.Append("</tr></thead><tbody>\n");

            for (int r = 0; r < rows.Items.Count; r++)
            {
                JsonValue rowItems = rows.Items[r].Get("items");
                sb.Append("<tr><th class=\"ds-matrix-row-head\">").Append(r).Append("</th>");
                for (int c = 0; c < colCount; c++)
                {
                    if (c < rowItems.Items.Count)
                    {
                        sb.Append("<td>").Append(RenderValue(rowItems.Items[c], resolver, 0)).Append("</td>");
                    }
                    else
                    {
                        sb.Append("<td>\u00B7</td>");
                    }
                }
                sb.Append("</tr>\n");
            }
            sb.Append("</tbody></table></div>");
            if (anyRowTruncated)
            {
                sb.Append("<div class=\"ds-array-more\">").Append(HtmlPageBuilder.I18n("span", null,
                    "Some rows have additional columns not shown (truncated).",
                    "一部の行には表示されていない追加の列があります(省略)。",
                    "بعضی از ردیف‌ها ستون‌های بیشتری دارن که نشون داده نشدن (کوتاه‌شده).")).Append("</div>");
            }
            return sb.ToString();
        }

        // ==========================================
        // RenderFlatOrMixedArray
        // Simple scalar elements become fixed-width
        // grid cells inside a scrollable box (see
        // .ds-array-grid / .ds-array-cell) - a value
        // either fits or truncates with an ellipsis and
        // a title tooltip, never breaks mid-character.
        // Complex elements (nested structs, managedRef,
        // further nested arrays IsScalarMatrix did not
        // already handle) still render as their own
        // full-width, independently-scrollable block line.
        // ==========================================
        private static string RenderFlatOrMixedArray(JsonValue items, RefLinkResolver resolver, int depth)
        {
            var gridSb = new StringBuilder(256);
            var blockSb = new StringBuilder();
            for (int i = 0; i < items.Items.Count; i++)
            {
                JsonValue item = items.Items[i];
                string itemKind = item.Get("kind").AsString("raw");
                string valueHtml = RenderValue(item, resolver, depth + 1);
                if (CompactArrayItemKinds.Contains(itemKind))
                {
                    string title = PlainTextForTitle(item, itemKind);
                    gridSb.Append("<div class=\"ds-array-cell\"").Append(string.IsNullOrEmpty(title) ? "" : " title=\"" + HtmlPageBuilder.Escape(title) + "\"")
                          .Append("><span class=\"idx\">[").Append(i).Append("]</span><span class=\"val\">").Append(valueHtml).Append("</span></div>");
                }
                else
                {
                    blockSb.Append("<div class=\"ds-array-block-item\"><span class=\"ds-field-type\">[").Append(i).Append("]</span> ").Append(valueHtml).Append("</div>");
                }
            }

            var sb = new StringBuilder(512);
            if (gridSb.Length > 0) { sb.Append("<div class=\"ds-array-grid\">").Append(gridSb.ToString()).Append("</div>"); }
            if (blockSb.Length > 0) { sb.Append(blockSb.ToString()); }
            return sb.ToString();
        }

        // ==========================================
        // PlainTextForTitle
        // Best-effort plain-text rendering of a compact
        // array item, used only for the array cell's
        // title tooltip, so a truncated/ellipsized value
        // is always still readable in full on hover.
        // ==========================================
        private static string PlainTextForTitle(JsonValue item, string kind)
        {
            switch (kind)
            {
                case "int":
                case "float":
                    return FormatNumber(item.Get("value").AsNumber());
                case "bool":
                    return item.Get("value").AsBool() ? "true" : "false";
                case "string":
                case "enum":
                case "color":
                case "hash128":
                    return item.Get("value").AsString("");
                case "vector2":
                case "vector2int":
                    {
                        JsonValue v = item.Get("value");
                        return "X " + FormatNumber(v.Get("x").AsNumber()) + " Y " + FormatNumber(v.Get("y").AsNumber());
                    }
                case "vector3":
                case "vector3int":
                case "quaternion":
                    {
                        JsonValue v = item.Get("value");
                        return "X " + FormatNumber(v.Get("x").AsNumber()) + " Y " + FormatNumber(v.Get("y").AsNumber()) + " Z " + FormatNumber(v.Get("z").AsNumber());
                    }
                case "vector4":
                    {
                        JsonValue v = item.Get("value");
                        return "X " + FormatNumber(v.Get("x").AsNumber()) + " Y " + FormatNumber(v.Get("y").AsNumber()) + " Z " + FormatNumber(v.Get("z").AsNumber()) + " W " + FormatNumber(v.Get("w").AsNumber());
                    }
                default:
                    return "";
            }
        }

        // ==========================================
        // RenderFolderTree
        // Top-level entry for an asset folder's
        // directory tree: expand/collapse controls
        // plus the root folder rendered as a
        // collapsible node, with every subfolder
        // nested inside exactly like the Scene
        // Hierarchy tree above.
        // ==========================================
        public static string RenderFolderTree(JsonValue treeRoot, Dictionary<string, JsonValue> filesByPath, RefLinkResolver resolver, string treeId)
        {
            var sb = new StringBuilder(1024);
            sb.Append("<div class=\"ds-card\"><div style=\"display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:8px;\">");
            sb.Append(HtmlPageBuilder.I18n("h3", null, "Folders", "フォルダ", "پوشه‌ها"));
            sb.Append("<span><button type=\"button\" data-tree-expand=\"").Append(treeId).Append("\" data-mode=\"expand\" class=\"ds-badge lav\" style=\"cursor:pointer;border:none;\">").Append(HtmlPageBuilder.I18n("span", null, "Expand all", "すべて展開", "باز کردن همه")).Append("</button> ");
            sb.Append("<button type=\"button\" data-tree-expand=\"").Append(treeId).Append("\" data-mode=\"collapse\" class=\"ds-badge ghost\" style=\"cursor:pointer;border:1px solid var(--line);\">").Append(HtmlPageBuilder.I18n("span", null, "Collapse all", "すべて折りたたむ", "بستن همه")).Append("</button></span>");
            sb.Append("</div><ul class=\"ds-tree\" id=\"").Append(treeId).Append("\">\n");
            sb.Append(RenderFolderNode(treeRoot, filesByPath, resolver, true));
            sb.Append("</ul></div>\n");
            return sb.ToString();
        }

        // ==========================================
        // RenderFolderNode
        // One folder as a collapsible node: its own
        // directly-contained files as an asset grid,
        // then its subfolders nested inside, exactly
        // like a GameObject node nests its children.
        // ==========================================
        private static string RenderFolderNode(JsonValue folder, Dictionary<string, JsonValue> filesByPath, RefLinkResolver resolver, bool openByDefault)
        {
            string folderName = folder.Get("folderName").AsString("Assets");
            string folderPath = folder.Get("folderPath").AsString("");
            int directCount = (int)folder.Get("directFileCount").AsNumber();
            int totalCount = (int)folder.Get("totalFileCount").AsNumber();
            JsonValue subfolders = folder.Get("subfolders");
            JsonValue filePaths = folder.Get("filePaths");
            bool hasSubfolders = subfolders.Items.Count > 0;

            var sb = new StringBuilder(512);
            sb.Append("<li id=\"folder-").Append(SanitizeAnchor(folderPath)).Append("\">");
            sb.Append("<details class=\"ds-go\"").Append(openByDefault ? " open" : "").Append(">");
            sb.Append("<summary>\uD83D\uDCC1 ").Append(HtmlPageBuilder.Escape(folderName));
            sb.Append(" <span class=\"ds-go-tag\">").Append(totalCount).Append(" ").Append(HtmlPageBuilder.I18n("span", null, "files", "ファイル", "فایل")).Append("</span>");
            sb.Append("</summary>\n");

            sb.Append("<div class=\"ds-go-card-body\">");
            if (directCount == 0 && !hasSubfolders)
            {
                sb.Append("<p class=\"ds-empty-note\">").Append(HtmlPageBuilder.I18n("span", null, "Empty folder.", "空のフォルダです。", "پوشه‌ی خالی.")).Append("</p>");
            }
            else if (directCount > 0)
            {
                var directFiles = JsonValue.Arr();
                int skipped = 0;
                foreach (JsonValue p in filePaths.Items)
                {
                    JsonValue entry;
                    if (!filesByPath.TryGetValue(p.AsString(""), out entry)) { continue; }

                    // Hard cap per folder node. Without it a single
                    // folder holding thousands of assets produces a
                    // page no browser can lay out; the complete,
                    // uncapped list stays in data/assets_*.json.
                    if (directFiles.Items.Count >= DocSnapConstants.MaxAssetsRenderedPerFolderNode)
                    {
                        skipped++;
                        continue;
                    }
                    directFiles.Add(entry);
                }
                sb.Append(RenderAssetGrid(directFiles, resolver));

                if (skipped > 0)
                {
                    sb.Append("<p class=\"ds-array-more\">+ ").Append(skipped).Append(" ")
                      .Append(HtmlPageBuilder.I18n("span", null,
                          "more file(s) in this folder - see data/assets_*.json for the complete list",
                          "件のファイルは省略されました - 完全な一覧は data/assets_*.json を参照",
                          "فایل دیگر در این پوشه - لیست کامل در data/assets_*.json"))
                      .Append("</p>");
                }
            }
            sb.Append("</div>");

            if (hasSubfolders)
            {
                sb.Append("<ul>\n");
                foreach (JsonValue child in subfolders.Items) { sb.Append(RenderFolderNode(child, filesByPath, resolver, false)); }
                sb.Append("</ul>\n");
            }

            sb.Append("</details></li>\n");
            return sb.ToString();
        }

        // ==========================================
        // SanitizeAnchor
        // Turns a folder path into a safe HTML id
        // fragment (letters/digits kept, everything
        // else collapsed to a dash).
        // ==========================================
        private static string SanitizeAnchor(string path)
        {
            var sb = new StringBuilder(path.Length);
            foreach (char c in path)
            {
                sb.Append(char.IsLetterOrDigit(c) ? c : '-');
            }
            return sb.ToString();
        }

        // ==========================================
        // RenderAssetGrid / RenderAssetCard
        // Cards for one exported asset folder's files.
        // ==========================================
        public static string RenderAssetGrid(JsonValue files, RefLinkResolver resolver)
        {
            var sb = new StringBuilder(1024);
            sb.Append("<div class=\"ds-asset-grid\">\n");
            foreach (JsonValue file in files.Items) { sb.Append(RenderAssetCard(file, resolver)); }
            sb.Append("</div>\n");
            return sb.ToString();
        }

        private static string RenderAssetCard(JsonValue file, RefLinkResolver resolver)
        {
            string guid = file.Get("guid").AsString("");
            string fileName = file.Get("fileName").AsString("");
            string path = file.Get("path").AsString("");
            string mainType = file.Get("mainType").AsString("Asset");

            var sb = new StringBuilder(1024);
            sb.Append("<div class=\"ds-asset-card\" id=\"asset-").Append(guid).Append("\">");
            sb.Append("<div class=\"ds-asset-card-head\"><h3>").Append(HtmlPageBuilder.Escape(fileName)).Append("</h3></div>");
            sb.Append("<div class=\"ds-asset-card-body\">");

            sb.Append(RenderAssetMedia(file, fileName, resolver));

            sb.Append(KvLine("Path", "パス", "مسیر", path));
            sb.Append(KvLine("Type", "タイプ", "نوع", mainType));
            if (file.Has("imageWidth"))
            {
                sb.Append(KvLine("Dimensions", "解像度", "ابعاد", (int)file.Get("imageWidth").AsNumber() + " \u00D7 " + (int)file.Get("imageHeight").AsNumber()));
            }
            if (file.Has("fileSizeBytes"))
            {
                sb.Append(KvLine("Size", "ファイルサイズ", "حجم فایل", FormatBytes(file.Get("fileSizeBytes").AsNumber())));
            }
            if (!string.IsNullOrEmpty(guid)) { sb.Append(KvLine("GUID", "GUID", "GUID", guid)); }

           if (file.Has("audioInfo"))
            {
                JsonValue audio = file.Get("audioInfo");
                if (audio.Has("lengthSeconds")) { sb.Append(KvLine("Length", "長さ", "مدت", FormatDuration(audio.Get("lengthSeconds").AsNumber()))); }
                if (audio.Has("channels")) { sb.Append(KvLine("Channels", "チャンネル", "کانال", ((int)audio.Get("channels").AsNumber()).ToString(CultureInfo.InvariantCulture))); }
                if (audio.Has("frequency")) { sb.Append(KvLine("Sample Rate", "サンプルレート", "نرخ نمونه‌برداری", ((int)audio.Get("frequency").AsNumber()).ToString(CultureInfo.InvariantCulture) + " Hz")); }
                if (audio.Has("compressionFormat")) { sb.Append(KvLine("Compression", "圧縮", "فشرده‌سازی", audio.Get("compressionFormat").AsString(""))); }
                if (audio.Has("loadType")) { sb.Append(KvLine("Load Type", "ロードタイプ", "نوع بارگذاری", audio.Get("loadType").AsString(""))); }
            }
            if (file.Has("materialInfo"))
            {
                JsonValue mat = file.Get("materialInfo");
                sb.Append("<div style=\"margin-top:10px;font-weight:700;\">").Append(HtmlPageBuilder.I18n("span", null, "Shader", "シェーダー", "Shader")).Append(": ").Append(HtmlPageBuilder.Escape(mat.Get("shaderName").AsString(""))).Append("</div>");
                sb.Append(OpenDetail("Shader Properties", "シェーダープロパティ", "ویژگی‌های Shader"));
                sb.Append(RenderShaderProps(mat.Get("properties")));
                sb.Append(CloseDetail());
            }
            if (file.Has("scriptInfo"))
            {
                JsonValue si = file.Get("scriptInfo");
                if (si.Has("className")) { sb.Append(KvLine("Class", "クラス", "کلاس", si.Get("className").AsString(""))); }
                string baseTypes = si.Get("baseTypes").AsString("");
                if (!string.IsNullOrEmpty(baseTypes)) { sb.Append(KvLine("Base", "基底クラス", "کلاس پایه", baseTypes)); }
            }
            if (file.Has("importerFields") && file.Get("importerFields").Items.Count > 0)
            {
                sb.Append(OpenDetail("Import Settings", "インポート設定", "تنظیمات ایمپورت"));
                sb.Append(RenderFieldTable(file.Get("importerFields"), resolver));
                sb.Append(CloseDetail());
            }
            if (file.Has("assetFields") && file.Get("assetFields").Items.Count > 0)
            {
                sb.Append(OpenDetail("Fields", "フィールド", "فیلدها"));
                sb.Append(RenderFieldTable(file.Get("assetFields"), resolver));
                sb.Append(CloseDetail());
            }
            if (file.Has("prefabRoot"))
            {
                Dictionary<int, string> prefabAnchors = BuildLocalAnchors(JsonValue.Arr().Add(file.Get("prefabRoot")));
                RefLinkResolver prefabResolver = resolver.WithLocalAnchors(prefabAnchors);
                sb.Append(OpenDetail("Prefab Contents", "Prefabの内容", "محتوای Prefab"));
                sb.Append("<ul class=\"ds-tree\">").Append(RenderGoNode(file.Get("prefabRoot"), prefabResolver, true)).Append("</ul>");
                sb.Append(CloseDetail());
            }

            sb.Append("</div></div>\n");
            return sb.ToString();
        }

        // ==========================================
        // OpenDetail / CloseDetail
        // Heavy per-asset tables (a TextureImporter
        // alone is ~120 rows) live inside a collapsed
        // <details>. A closed <details> is parsed but
        // never laid out or painted, which is the
        // difference between a folder page opening and
        // a folder page hanging the browser.
        // ==========================================
        private static string OpenDetail(string en, string ja, string fa)
        {
            return "<details class=\"ds-detail\"><summary>" + HtmlPageBuilder.I18n("span", null, en, ja, fa) + "</summary><div class=\"ds-detail-body\">";
        }

        private static string CloseDetail()
        {
            return "</div></details>";
        }

        // ==========================================
        // FormatDuration
        // Seconds as m:ss, or s.sss below one minute.
        // ==========================================
        private static string FormatDuration(double seconds)
        {
            if (seconds < 60)
            {
                return seconds.ToString("0.###", CultureInfo.InvariantCulture) + " s";
            }
            int minutes = (int)(seconds / 60);
            double remainder = seconds - (minutes * 60);
            return minutes.ToString(CultureInfo.InvariantCulture) + ":" + remainder.ToString("00.#", CultureInfo.InvariantCulture);
        }

        // ==========================================
        // Media extension sets
        // Decides which visual an asset card leads
        // with. Kept here (not in the exporter) so the
        // exporter stays a pure metadata producer.
        // ==========================================
        private static readonly HashSet<string> PreviewableImageExtensions =
            new HashSet<string> { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".svg" };

        private static readonly HashSet<string> PlayableAudioExtensions =
            new HashSet<string> { ".wav", ".mp3", ".ogg", ".m4a", ".aac", ".flac" };

        private static readonly HashSet<string> PlayableVideoExtensions =
            new HashSet<string> { ".mp4", ".webm", ".ogv", ".m4v" };

        // ==========================================
        // RenderAssetMedia
        // The visual block at the top of one asset
        // card. Preference order:
        //   1. the real copied file (files/…), when
        //      "Export Full Project With Files" put it
        //      there - a live <img>/<audio>/<video>;
        //   2. the embedded base64 thumbnail;
        //   3. a placeholder glyph.
        // Before this existed, copied files/ bytes were
        // never referenced by any page, which is why
        // images and audio never showed up even after a
        // with-files export.
        // ==========================================
        private static string RenderAssetMedia(JsonValue file, string fileName, RefLinkResolver resolver)
        {
            string physical = file.Get("physicalFile").AsString("");
            string prefix = resolver == null ? "" : resolver.LinkPrefix;

            // A written-out .png is preferred over an inlined base64
            // data URI: it lazy-loads, caches, and keeps the HTML
            // small enough for a browser to actually open.
            string thumbFile = file.Get("thumbnailFile").AsString("");
            string thumb = !string.IsNullOrEmpty(thumbFile)
                ? EncodeUrlPath(prefix + thumbFile)
                : file.Get("thumbnailBase64").AsString("");

            bool thumbIsIcon = file.Get("thumbnailIsIcon").AsBool(false);
            string extension = GetExtensionLower(fileName);
            string alt = HtmlPageBuilder.Escape(fileName);

            var sb = new StringBuilder(512);

            if (!string.IsNullOrEmpty(physical))
            {
                string href = EncodeUrlPath((resolver == null ? "" : resolver.LinkPrefix) + physical);

                if (PreviewableImageExtensions.Contains(extension))
                {
                    sb.Append("<div class=\"ds-thumb\"><img loading=\"lazy\" decoding=\"async\" src=\"")
                      .Append(href).Append("\" alt=\"").Append(alt).Append("\"></div>");
                }
                else if (PlayableAudioExtensions.Contains(extension))
                {
                    sb.Append("<audio class=\"ds-media\" controls preload=\"none\" src=\"").Append(href).Append("\"></audio>");
                }
                else if (PlayableVideoExtensions.Contains(extension))
                {
                    sb.Append("<video class=\"ds-media\" controls preload=\"metadata\" src=\"").Append(href).Append("\"></video>");
                }
                else
                {
                    sb.Append(RenderThumbFallback(thumb, alt, thumbIsIcon));
                }

                sb.Append("<div class=\"ds-file-actions\">");
                sb.Append("<a class=\"ds-file-link\" href=\"").Append(href).Append("\" target=\"_blank\" rel=\"noopener\">\uD83D\uDD17 ")
                  .Append(HtmlPageBuilder.I18n("span", null, "Open file", "\u30D5\u30A1\u30A4\u30EB\u3092\u958B\u304F", "\u0628\u0627\u0632 \u06A9\u0631\u062F\u0646 \u0641\u0627\u06CC\u0644"))
                  .Append("</a>");
                sb.Append("<a class=\"ds-file-link\" href=\"").Append(href).Append("\" download>\u2B07 ")
                  .Append(HtmlPageBuilder.I18n("span", null, "Download", "\u30C0\u30A6\u30F3\u30ED\u30FC\u30C9", "\u062F\u0627\u0646\u0644\u0648\u062F"))
                  .Append("</a>");
                sb.Append("</div>");

                return sb.ToString();
            }

           sb.Append(RenderThumbFallback(thumb, alt, thumbIsIcon));
            return sb.ToString();
        }
        
        // ==========================================
        // RenderThumbFallback
        // The embedded base64 thumbnail as a real
        // <img> (lazy-loadable, saveable, printable,
        // accessible) instead of a background-image on
        // an empty <div>.
        // ==========================================
        private static string RenderThumbFallback(string thumbnailSrc, string escapedAlt, bool isIcon)
        {
            if (string.IsNullOrEmpty(thumbnailSrc))
            {
                return "<div class=\"ds-thumb\">\uD83D\uDCC4</div>";
            }
            // A 16x16 type icon stretched across a 4:3 preview box
            // reads as an empty card. Icons get their own sizing.
            string cls = isIcon ? "ds-thumb is-icon" : "ds-thumb";
            return "<div class=\"" + cls + "\"><img loading=\"lazy\" decoding=\"async\" src=\"" + thumbnailSrc + "\" alt=\"" + escapedAlt + "\"></div>";
        }

        // ==========================================
        // GetExtensionLower
        // Lower-cased extension including the dot, or
        // an empty string when there is none.
        // ==========================================
        private static string GetExtensionLower(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) { return ""; }
            int dot = fileName.LastIndexOf('.');
            return dot < 0 ? "" : fileName.Substring(dot).ToLowerInvariant();
        }

        // ==========================================
        // EncodeUrlPath
        // Percent-encodes each path segment while
        // leaving the separators intact. Unity asset
        // paths routinely contain spaces, '#' and '?',
        // every one of which silently breaks an href.
        // ==========================================
        private static string EncodeUrlPath(string path)
        {
            if (string.IsNullOrEmpty(path)) { return ""; }
            string[] segments = path.Replace('\\', '/').Split('/');
            var sb = new StringBuilder(path.Length + 16);
            for (int i = 0; i < segments.Length; i++)
            {
                if (i > 0) { sb.Append('/'); }
                if (segments[i] == "." || segments[i] == "..")
                {
                    sb.Append(segments[i]);
                    continue;
                }
                sb.Append(Uri.EscapeDataString(segments[i]));
            }
            return HtmlPageBuilder.Escape(sb.ToString());
        }

        // ==========================================
        // Small formatting helpers
        // ==========================================
        private static string KvLine(string kEn, string kJa, string kFa, string v)
        {
            return "<div class=\"ds-kv-line\"><span class=\"k\">" + HtmlPageBuilder.I18n("span", null, kEn, kJa, kFa) + "</span><span class=\"v\">" + HtmlPageBuilder.Escape(v) + "</span></div>";
        }
        
        private static string FormatBytes(double bytes)
        {
            string[] units = { "B", "KB", "MB", "GB" };
            double size = bytes;
            int u = 0;
            while (size >= 1024 && u < units.Length - 1) { size /= 1024; u++; }
            return size.ToString("0.#", CultureInfo.InvariantCulture) + " " + units[u];
        }

       // ==========================================
        // RenderShaderProps
        // A Property / Type / Value field grid for one
        // Material's shader properties - shares the same
        // .ds-field-grid markup as every other field
        // table instead of its own separate <table>.
        // ==========================================
        private static string RenderShaderProps(JsonValue props)
        {
            if (props.Items.Count == 0) { return ""; }
            var sb = new StringBuilder(512);
            sb.Append("<div class=\"ds-field-grid\">");
            sb.Append("<div class=\"ds-field-grid-head\">")
              .Append(HtmlPageBuilder.I18n("span", null, "Property", "プロパティ", "ویژگی"))
              .Append(HtmlPageBuilder.I18n("span", null, "Type", "型", "نوع"))
              .Append(HtmlPageBuilder.I18n("span", null, "Value", "値", "مقدار"))
              .Append("</div>");
            foreach (JsonValue p in props.Items)
            {
                string kind = p.Get("kind").AsString("raw");
                string name = p.Get("name").AsString("");
                string valueHtml;
                switch (kind)
                {
                    case "color":
                        string hex = p.Get("value").AsString("#ffffff");
                        valueHtml = "<span class=\"ds-swatch\" style=\"background:" + hex + "\"></span>" + hex;
                        break;
                    case "float":
                        valueHtml = FormatNumber(p.Get("value").AsNumber());
                        break;
                    case "vector4":
                        valueHtml = VecXYZW(p.Get("value"));
                        break;
                    case "texture":
                        valueHtml = p.Get("hasTexture").AsBool()
                            ? HtmlPageBuilder.Escape(p.Get("textureName").AsString(""))
                            : "<span class=\"ds-empty-note\">None</span>";
                        break;
                    default:
                        valueHtml = "-";
                        break;
                }
                sb.Append("<div class=\"ds-field-grid-row\"><div class=\"ds-field-name\">").Append(HtmlPageBuilder.Escape(name))
                  .Append("</div><div class=\"ds-field-type\">").Append(HtmlPageBuilder.Escape(kind))
                  .Append("</div><div class=\"ds-field-value\">").Append(valueHtml).Append("</div></div>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string FormatNumber(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) { return "0"; }
            if (d == Math.Floor(d) && Math.Abs(d) < 1e15) { return ((long)d).ToString(CultureInfo.InvariantCulture); }
            return d.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string VecXY(JsonValue v)
        {
            return "<span class=\"ds-vec3\"><b>X</b> " + FormatNumber(v.Get("x").AsNumber()) + " <b>Y</b> " + FormatNumber(v.Get("y").AsNumber()) + "</span>";
        }

        private static string VecXYZ(JsonValue v)
        {
            return "<span class=\"ds-vec3\"><b>X</b> " + FormatNumber(v.Get("x").AsNumber()) + " <b>Y</b> " + FormatNumber(v.Get("y").AsNumber())
                + " <b>Z</b> " + FormatNumber(v.Get("z").AsNumber()) + "</span>";
        }

        private static string VecXYZW(JsonValue v)
        {
            return "<span class=\"ds-vec3\"><b>X</b> " + FormatNumber(v.Get("x").AsNumber()) + " <b>Y</b> " + FormatNumber(v.Get("y").AsNumber())
                + " <b>Z</b> " + FormatNumber(v.Get("z").AsNumber()) + " <b>W</b> " + FormatNumber(v.Get("w").AsNumber()) + "</span>";
        }

        private static string VecInline(JsonValue v)
        {
            return "(" + FormatNumber(v.Get("x").AsNumber()) + ", " + FormatNumber(v.Get("y").AsNumber()) + ", " + FormatNumber(v.Get("z").AsNumber()) + ")";
        }
    }
}
