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
            string refType = refNode.Get("refType").AsString("Object");
            bool isAsset = refNode.Get("isAsset").AsBool();

            if (isAsset)
            {
                string guid = refNode.Get("targetGuid").AsString("");
                ManifestAssetIndexEntry entry;
                if (!string.IsNullOrEmpty(guid) && GuidLookup != null && GuidLookup.TryGetValue(guid, out entry))
                {
                    string href = LinkPrefix + entry.htmlFile + "#" + entry.anchor;
                    return "<a class=\"ds-ref-chip\" href=\"" + href + "\">\uD83D\uDD17 " + targetNameHtml
                        + " <span class=\"type\">" + HtmlPageBuilder.Escape(refType) + "</span></a>";
                }
                return "<span class=\"ds-ref-chip is-unresolved\">" + targetNameHtml
                    + " <span class=\"type\">" + HtmlPageBuilder.Escape(refType) + " \u00B7 " + HtmlPageBuilder.I18n("span", null, "not exported yet", "未エクスポート", "هنوز اکسپورت نشده") + "</span></span>";
            }

            int instanceId = (int)refNode.Get("targetInstanceId").AsNumber(0);
            string anchor;
            if (LocalAnchors != null && LocalAnchors.TryGetValue(instanceId, out anchor))
            {
                return "<a class=\"ds-ref-chip\" href=\"#" + anchor + "\">\uD83D\uDD17 " + targetNameHtml
                    + " <span class=\"type\">" + HtmlPageBuilder.Escape(refType) + "</span></a>";
            }
            return "<span class=\"ds-ref-chip is-unresolved\">" + targetNameHtml
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
        // A Field / Type / Value table for one array
        // of field nodes (top-level or nested).
        // ==========================================
        public static string RenderFieldTable(JsonValue fieldsArray, RefLinkResolver resolver)
        {
            if (fieldsArray == null || fieldsArray.Items.Count == 0)
            {
                return "<p class=\"ds-empty-note\">" + HtmlPageBuilder.I18n("span", null, "No fields.", "フィールドはありません。", "فیلدی وجود نداره.") + "</p>";
            }
            var sb = new StringBuilder(1024);
            sb.Append("<table class=\"ds-field-table\"><tr>")
              .Append(HtmlPageBuilder.I18n("th", null, "Field", "フィールド", "فیلد"))
              .Append(HtmlPageBuilder.I18n("th", null, "Type", "型", "نوع"))
              .Append(HtmlPageBuilder.I18n("th", null, "Value", "値", "مقدار"))
              .Append("</tr>\n");
            foreach (JsonValue field in fieldsArray.Items)
            {
                string name = field.Has("label") ? field.Get("label").AsString("") : field.Get("name").AsString("");
                string kind = field.Get("kind").AsString("raw");
                sb.Append("<tr><td class=\"ds-field-name\">").Append(HtmlPageBuilder.Escape(name)).Append("</td>");
                sb.Append("<td class=\"ds-field-type\">").Append(HtmlPageBuilder.Escape(kind)).Append("</td>");
                sb.Append("<td class=\"ds-field-value\">").Append(RenderValue(field, resolver, 0)).Append("</td></tr>\n");
            }
            sb.Append("</table>");
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
                case "renderingLayerMask":
                    return HtmlPageBuilder.I18n("span", null, "Rendering Layer Mask", "レンダリングレイヤーマスク", "ماسک لایه رندر") + ": " + (uint)field.Get("value").AsNumber();
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

        private static string RenderGeneric(JsonValue field, RefLinkResolver resolver, int depth)
        {
            JsonValue fields = field.Get("fields");
            if (fields.Items.Count == 0) { return "<span class=\"ds-empty-note\">" + HtmlPageBuilder.I18n("span", null, "(empty)", "（空）", "(خالی)") + "</span>"; }
            return "<div class=\"ds-nested-table\">" + RenderFieldTable(fields, resolver) + "</div>";
        }

        private static string RenderManagedRef(JsonValue field, RefLinkResolver resolver, int depth)
        {
            string typeName = field.Get("typeName").AsString("");
            if (field.Get("isNull").AsBool())
            {
                return "<span class=\"ds-empty-note\">" + HtmlPageBuilder.I18n("span", null, "None", "なし", "هیچ‌کدام") + " (" + HtmlPageBuilder.Escape(typeName) + ")</span>";
            }
            var sb = new StringBuilder(256);
            sb.Append("<div class=\"ds-nested-table\"><div style=\"padding:6px 10px;font-weight:700;\">").Append(HtmlPageBuilder.Escape(typeName)).Append("</div>");
            sb.Append(RenderFieldTable(field.Get("fields"), resolver));
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string RenderArray(JsonValue field, RefLinkResolver resolver, int depth)
        {
            int count = (int)field.Get("count").AsNumber();
            if (count == 0) { return "<span class=\"ds-empty-note\">" + HtmlPageBuilder.I18n("span", null, "Empty array", "空の配列", "آرایه‌ی خالی") + "</span>"; }

            JsonValue items = field.Get("items");
            var sb = new StringBuilder(256);
            sb.Append("<div class=\"ds-array-wrap\">");
            for (int i = 0; i < items.Items.Count; i++)
            {
                sb.Append("<div><span class=\"ds-field-type\">[").Append(i).Append("]</span> ").Append(RenderValue(items.Items[i], resolver, depth + 1)).Append("</div>");
            }
            if (field.Get("truncated").AsBool())
            {
                sb.Append("<div class=\"ds-array-more\">\u2026").Append(count - items.Items.Count).Append(" ")
                  .Append(HtmlPageBuilder.I18n("span", null, "more (truncated)", "件省略", "مورد دیگر (کوتاه‌شده)")).Append("</div>");
            }
            sb.Append("</div>");
            return sb.ToString();
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
                foreach (JsonValue p in filePaths.Items)
                {
                    JsonValue entry;
                    if (filesByPath.TryGetValue(p.AsString(""), out entry)) { directFiles.Add(entry); }
                }
                sb.Append(RenderAssetGrid(directFiles, resolver));
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

            string thumb = file.Get("thumbnailBase64").AsString("");
            if (!string.IsNullOrEmpty(thumb))
            {
                sb.Append("<div class=\"ds-thumb\" style=\"background-image:url('").Append(thumb).Append("')\"></div>");
            }
            else
            {
                sb.Append("<div class=\"ds-thumb\">\uD83D\uDCC4</div>");
            }

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

            if (file.Has("materialInfo"))
            {
                JsonValue mat = file.Get("materialInfo");
                sb.Append("<div style=\"margin-top:10px;font-weight:700;\">").Append(HtmlPageBuilder.I18n("span", null, "Shader", "シェーダー", "Shader")).Append(": ").Append(HtmlPageBuilder.Escape(mat.Get("shaderName").AsString(""))).Append("</div>");
                sb.Append(RenderShaderProps(mat.Get("properties")));
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
                sb.Append("<div style=\"margin-top:10px;font-weight:700;\">").Append(HtmlPageBuilder.I18n("span", null, "Import Settings", "インポート設定", "تنظیمات ایمپورت")).Append("</div>");
                sb.Append(RenderFieldTable(file.Get("importerFields"), resolver));
            }
            if (file.Has("assetFields") && file.Get("assetFields").Items.Count > 0)
            {
                sb.Append("<div style=\"margin-top:10px;font-weight:700;\">").Append(HtmlPageBuilder.I18n("span", null, "Fields", "フィールド", "فیلدها")).Append("</div>");
                sb.Append(RenderFieldTable(file.Get("assetFields"), resolver));
            }
            if (file.Has("prefabRoot"))
            {
                Dictionary<int, string> prefabAnchors = BuildLocalAnchors(JsonValue.Arr().Add(file.Get("prefabRoot")));
                RefLinkResolver prefabResolver = resolver.WithLocalAnchors(prefabAnchors);
                sb.Append("<div style=\"margin-top:10px;font-weight:700;\">").Append(HtmlPageBuilder.I18n("span", null, "Prefab Contents", "Prefabの内容", "محتوای Prefab")).Append("</div>");
                sb.Append("<ul class=\"ds-tree\">").Append(RenderGoNode(file.Get("prefabRoot"), prefabResolver, true)).Append("</ul>");
            }

            sb.Append("</div></div>\n");
            return sb.ToString();
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

        private static string RenderShaderProps(JsonValue props)
        {
            if (props.Items.Count == 0) { return ""; }
            var sb = new StringBuilder(512);
            sb.Append("<table class=\"ds-field-table\"><tr>")
              .Append(HtmlPageBuilder.I18n("th", null, "Property", "プロパティ", "ویژگی"))
              .Append(HtmlPageBuilder.I18n("th", null, "Type", "型", "نوع"))
              .Append(HtmlPageBuilder.I18n("th", null, "Value", "値", "مقدار"))
              .Append("</tr>");
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
                sb.Append("<tr><td class=\"ds-field-name\">").Append(HtmlPageBuilder.Escape(name)).Append("</td><td class=\"ds-field-type\">")
                  .Append(HtmlPageBuilder.Escape(kind)).Append("</td><td class=\"ds-field-value\">").Append(valueHtml).Append("</td></tr>");
            }
            sb.Append("</table>");
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
