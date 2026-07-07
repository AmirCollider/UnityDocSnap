// ==========================================
// IndexPageRenderer
// Builds index.html: the site's front door -
// quick stats plus a live list of every
// exported Scene and Asset folder.
// ==========================================
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Manifest;

namespace AmirCollider.UnityDocSnap.Editor.Html
{
    internal static class IndexPageRenderer
    {
        // ==========================================
        // Render
        // ==========================================
        public static string Render(ManifestState manifest)
        {
            int totalGameObjects = 0;
            foreach (ManifestSceneEntry s in manifest.scenes) { totalGameObjects += s.gameObjectCount; }
            int totalFiles = 0;
            foreach (ManifestFolderEntry f in manifest.assetFolders) { totalFiles += f.fileCount; }

            var badges = new System.Collections.Generic.List<string>
            {
                HtmlPageBuilder.Badge("lav", "Unity " + manifest.unityVersion),
                HtmlPageBuilder.Badge("pink", "Unity DocSnap v" + DocSnapConstants.Version)
            };
            string header = HtmlPageBuilder.RenderPageHeader("\uD83C\uDF70", manifest.projectName, "Last export: " + manifest.lastUpdatedUtc, badges);

            var sb = new StringBuilder(2048);

            sb.Append("<div class=\"ds-stat-grid\">");
            sb.Append(StatTile(manifest.scenes.Count, "Scenes exported"));
            sb.Append(StatTile(totalGameObjects, "GameObjects"));
            sb.Append(StatTile(manifest.assetFolders.Count, "Asset folders"));
            sb.Append(StatTile(totalFiles, "Files tracked"));
            sb.Append("</div>\n");

            sb.Append("<div class=\"ds-card\"><h3>Scenes</h3><ul class=\"ds-folder-list\">\n");
            if (manifest.scenes.Count == 0)
            {
                sb.Append("<p class=\"ds-empty-note\">No scenes exported yet - use Unity DocSnap &gt; Export Scene in the Unity menu bar.</p>");
            }
            foreach (ManifestSceneEntry s in manifest.scenes)
            {
                sb.Append("<li><a class=\"ds-folder-row\" href=\"").Append(s.htmlFile).Append("\"><span class=\"ds-folder-path\">")
                  .Append(HtmlPageBuilder.Escape(s.sceneName)).Append("</span><span class=\"ds-folder-meta\">")
                  .Append(s.gameObjectCount).Append(" GameObjects</span></a></li>\n");
            }
            sb.Append("</ul></div>\n");

            sb.Append("<div class=\"ds-card\"><h3>Assets</h3><ul class=\"ds-folder-list\">\n");
            if (manifest.assetFolders.Count == 0)
            {
                sb.Append("<p class=\"ds-empty-note\">No asset folders exported yet - use Unity DocSnap &gt; Export Asset Info in the Unity menu bar.</p>");
            }
            foreach (ManifestFolderEntry f in manifest.assetFolders)
            {
                sb.Append("<li><a class=\"ds-folder-row\" href=\"").Append(f.htmlFile).Append("\"><span class=\"ds-folder-path\">")
                  .Append(HtmlPageBuilder.Escape(f.folderPath)).Append("</span><span class=\"ds-folder-meta\">")
                  .Append(f.fileCount).Append(" files</span></a></li>\n");
            }
            sb.Append("</ul></div>\n");

            return HtmlPageBuilder.RenderPage(manifest, DocSnapConstants.IndexFileName, manifest.projectName, header, sb.ToString());
        }

        private static string StatTile(int num, string label)
        {
            return "<div class=\"ds-stat-tile\"><div class=\"ds-stat-num\">" + num + "</div><div class=\"ds-stat-label\">" + HtmlPageBuilder.Escape(label) + "</div></div>";
        }
    }
}
