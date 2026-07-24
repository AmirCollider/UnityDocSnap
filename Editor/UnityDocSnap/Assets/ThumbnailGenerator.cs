// ==========================================
// ThumbnailGenerator
// Produces small base64 PNG previews so the
// generated site can show what a file looks
// like without ever copying the file itself.
// Images are read straight from disk (bypasses
// import/readable settings entirely); every
// other asset type falls back to its Editor
// type icon via a GPU-blit readable copy.
// ==========================================
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor.Assets
{
    internal static class ThumbnailGenerator
    {
        // ==========================================
        // The preview-wait budget.
        //
        // Unity renders asset previews asynchronously, so
        // TryWritePreviewPng polls for one. It used to poll
        // for up to AssetPreviewTimeoutMs PER ASSET on the
        // main thread - a project with a few thousand
        // non-image assets could therefore spend well over
        // half an hour inside Thread.Sleep with the Editor
        // frozen and no way to cancel, and the export looked
        // hung rather than slow.
        //
        // The per-asset timeout is still honoured, but every
        // millisecond spent waiting now also draws down one
        // shared per-export budget. Once it is gone, previews
        // are taken only if Unity already has them cached and
        // the export finishes at full speed with type icons
        // for the rest - a visibly worse thumbnail on some
        // cards, which is a far better trade than an Editor
        // that appears to have crashed.
        // ==========================================
        private static readonly System.Diagnostics.Stopwatch BudgetClock = new System.Diagnostics.Stopwatch();
        private static int _budgetMs;

        public static void BeginExport(int totalBudgetMs)
        {
            _budgetMs = totalBudgetMs;
            BudgetClock.Reset();
            BudgetClock.Start();
        }

        public static void EndExport()
        {
            BudgetClock.Stop();
        }

        public static bool BudgetExhausted
        {
            get { return _budgetMs > 0 && BudgetClock.IsRunning && BudgetClock.ElapsedMilliseconds >= _budgetMs; }
        }

        // ==========================================
        // TryGetImageThumbnailBase64
        // Reads a source image file's raw bytes,
        // downsizes it, and returns a data-URI PNG
        // plus the original pixel dimensions.
        // ==========================================
        public static string TryGetImageThumbnailBase64(string assetAbsolutePath, int maxDimension, out int sourceWidth, out int sourceHeight)
        {
            sourceWidth = 0;
            sourceHeight = 0;
            Texture2D source = null;
            Texture2D scaled = null;
            try
            {
                byte[] bytes = File.ReadAllBytes(assetAbsolutePath);
                source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!source.LoadImage(bytes))
                {
                    return null;
                }

                sourceWidth = source.width;
                sourceHeight = source.height;

                scaled = ScaleDown(source, maxDimension);
                byte[] png = scaled.EncodeToPNG();
                return "data:image/png;base64," + Convert.ToBase64String(png);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (source != null) { UnityEngine.Object.DestroyImmediate(source); }
                if (scaled != null && !ReferenceEquals(scaled, source)) { UnityEngine.Object.DestroyImmediate(scaled); }
            }
        }

        // ==========================================
        // TryGetIconBase64
        // Falls back to Unity's own Editor type icon
        // for assets that are not directly viewable
        // images (audio, models, materials, scripts…).
        // ==========================================
        public static string TryGetIconBase64(UnityEngine.Object asset)
        {
            Texture2D readable = null;
            try
            {
                Texture2D icon = AssetPreview.GetMiniThumbnail(asset);
                if (icon == null) { return null; }
                readable = MakeReadableCopy(icon);
                byte[] png = readable.EncodeToPNG();
                return "data:image/png;base64," + Convert.ToBase64String(png);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (readable != null) { UnityEngine.Object.DestroyImmediate(readable); }
            }
        }

        // ==========================================
        // TryWriteImageThumbnailPng
        // Same decode+downscale path as
        // TryGetImageThumbnailBase64, but writes a real
        // .png next to the site instead of inlining a
        // base64 data URI. Inlining meant every preview
        // was embedded twice (HTML + JSON) and could not
        // be lazy-loaded or cached, which is what made
        // large Asset pages unopenable.
        // Returns the site-relative path, or null.
        // ==========================================
        public static string TryWriteImageThumbnailPng(string assetAbsolutePath, string thumbsFolderAbsolute, string guid, int maxDimension, out int sourceWidth, out int sourceHeight)
        {
            sourceWidth = 0;
            sourceHeight = 0;
            Texture2D source = null;
            Texture2D scaled = null;
            try
            {
                byte[] bytes = File.ReadAllBytes(assetAbsolutePath);
                source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!source.LoadImage(bytes)) { return null; }

                sourceWidth = source.width;
                sourceHeight = source.height;

                scaled = ScaleDown(source, maxDimension);
                return WritePng(scaled, thumbsFolderAbsolute, guid);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (source != null) { UnityEngine.Object.DestroyImmediate(source); }
                if (scaled != null && !ReferenceEquals(scaled, source)) { UnityEngine.Object.DestroyImmediate(scaled); }
            }
        }

        // ==========================================
        // TryWritePreviewPng
        // Unity's own rendered asset preview (works for
        // .tga/.psd/.exr textures, Materials, Prefabs,
        // Models, Fonts - everything LoadImage cannot
        // decode). Previews generate asynchronously, so
        // this polls briefly rather than accepting the
        // null that a single call almost always returns
        // on a cold cache.
        // Returns the site-relative path, or null.
        // ==========================================
        public static string TryWritePreviewPng(UnityEngine.Object asset, string thumbsFolderAbsolute, string guid, int maxDimension, int timeoutMs)
        {
            if (asset == null) { return null; }
            Texture2D scaled = null;
            try
            {
                int instanceId = asset.GetInstanceID();
                Texture2D preview = AssetPreview.GetAssetPreview(asset);

                // Only wait while the shared export budget has room.
                // When it is spent this becomes a single cache probe:
                // assets Unity has already rendered still get their
                // real preview, everything else falls through to the
                // type icon instead of stalling the Editor again.
                if (preview == null && !BudgetExhausted)
                {
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    while (preview == null
                        && AssetPreview.IsLoadingAssetPreview(instanceId)
                        && clock.ElapsedMilliseconds < timeoutMs
                        && !BudgetExhausted)
                    {
                        System.Threading.Thread.Sleep(10);
                        preview = AssetPreview.GetAssetPreview(asset);
                    }
                }
                if (preview == null) { return null; }

                scaled = ScaleDown(preview, maxDimension);
                return WritePng(scaled, thumbsFolderAbsolute, guid);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (scaled != null) { UnityEngine.Object.DestroyImmediate(scaled); }
            }
        }

        // ==========================================
        // TryWriteIconPng
        // Last-resort generic Editor type icon, written
        // as a real file like every other preview.
        // Returns the site-relative path, or null.
        // ==========================================
        public static string TryWriteIconPng(UnityEngine.Object asset, string thumbsFolderAbsolute, string guid)
        {
            if (asset == null) { return null; }
            Texture2D readable = null;
            try
            {
                Texture2D icon = AssetPreview.GetMiniThumbnail(asset);
                if (icon == null) { return null; }
                readable = MakeReadableCopy(icon);
                return WritePng(readable, thumbsFolderAbsolute, guid);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (readable != null) { UnityEngine.Object.DestroyImmediate(readable); }
            }
        }

        // ==========================================
        // WritePng
        // Shared writer: encodes one texture to
        // <thumbsFolder>/<guid>.png and returns the
        // site-relative path used inside generated HTML
        // and JSON.
        // ==========================================
        private static string WritePng(Texture2D texture, string thumbsFolderAbsolute, string guid)
        {
            if (texture == null || string.IsNullOrEmpty(thumbsFolderAbsolute) || string.IsNullOrEmpty(guid)) { return null; }

            byte[] png = texture.EncodeToPNG();
            if (png == null || png.Length == 0) { return null; }

            Directory.CreateDirectory(thumbsFolderAbsolute);
            File.WriteAllBytes(Path.Combine(thumbsFolderAbsolute, guid + ".png"), png);

            return DocSnapConstants.SiteAssetsSubFolder + "/" + DocSnapConstants.ThumbsSubFolder + "/" + guid + ".png";
        }

        // ==========================================
        // ScaleDown
        // Downscales via a RenderTexture blit so it
        // works even on non-square / odd source sizes.
        // ==========================================
        private static Texture2D ScaleDown(Texture2D source, int maxDimension)
        {
            if (source.width <= maxDimension && source.height <= maxDimension)
            {
                return CloneReadable(source);
            }

            float scale = Mathf.Min((float)maxDimension / source.width, (float)maxDimension / source.height);
            int newWidth = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            int newHeight = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            return BlitResize(source, newWidth, newHeight);
        }

        // ==========================================
        // MakeReadableCopy
        // GPU-blit trick: works even when the source
        // texture itself is not marked Read/Write.
        // ==========================================
        private static Texture2D MakeReadableCopy(Texture2D source)
        {
            return BlitResize(source, source.width, source.height);
        }

        private static Texture2D CloneReadable(Texture2D source)
        {
            return BlitResize(source, source.width, source.height);
        }

        // ==========================================
        // BlitResize
        // Shared GPU blit path used for both resizing
        // and "make readable" duties.
        // ==========================================
       private static Texture2D BlitResize(Texture2D source, int width, int height)
        {
            // A default-readWrite RT blitted in Linear color space
            // produces a gamma-incorrect (washed out / near-black)
            // thumbnail. Force sRGB when the project is Linear.
            RenderTextureReadWrite readWrite = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? RenderTextureReadWrite.sRGB
                : RenderTextureReadWrite.Default;

            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, readWrite);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                result.Apply();
                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
