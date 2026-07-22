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
