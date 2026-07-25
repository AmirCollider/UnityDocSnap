// ==========================================
// DocSnapCapability
// Decides which visual skin a generated site
// should OPEN with, and records why.
//
// The site ships two: "cozy" (the original look -
// pastel gradients, soft shadows, generous radii, a
// bobbing mascot) and "lite" (flat surfaces, 1px
// borders, no animation). Cozy is the signature and
// the nicer thing to look at; it is also strictly
// more paint work per row, which stops being free
// somewhere between a demo scene and a project with
// forty thousand GameObjects on one page.
//
// So the choice is measured rather than guessed:
// the exporting machine's RAM / cores / GPU, plus
// how heavy this particular project is. Passing
// every check means the export opens cozy; failing
// any of them means it opens lite and says which
// check failed. Either way the reader can switch,
// because a machine the exporter has never seen may
// be the one actually reading the page.
// ==========================================
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    // ==========================================
    // DocSnapCapabilityReport
    // The measurement plus the verdict, ready to be
    // baked into a page as JSON.
    // ==========================================
    internal sealed class DocSnapCapabilityReport
    {
        public int SystemMemoryMb;
        public int ProcessorCount;
        public int ProcessorFrequencyMhz;
        public string ProcessorType = "";
        public string GraphicsDeviceName = "";
        public int GraphicsMemoryMb;

        public int GameObjectCount;
        public int AssetFileCount;

        // "cozy" | "lite"
        public string Skin = DocSnapCapability.SkinCozy;

        // Every check that failed, in plain language. Empty when the
        // verdict is cozy. Shown to the reader when they override it,
        // because "your machine might struggle" with no numbers
        // attached is not a warning, it is a shrug.
        public List<string> Reasons = new List<string>();

        public bool IsCozy { get { return Skin == DocSnapCapability.SkinCozy; } }
    }

    internal static class DocSnapCapability
    {
        public const string SkinCozy = "cozy";
        public const string SkinLite = "lite";

        // ==========================================
        // Thresholds.
        //
        // Chosen to be permissive rather than cautious: the cozy
        // skin is the one people want, and a machine that can run
        // the Unity Editor at all clears most of these. They exist
        // to catch the genuinely constrained cases - an 8 GB laptop
        // on integrated graphics opening a page with 40 000 animated
        // rows on it - not to gatekeep.
        // ==========================================
        public const int MinMemoryMb = 8192;
        public const int MinProcessorCount = 4;
        public const int MinGraphicsMemoryMb = 1024;
        public const int MaxCozyGameObjects = 40000;
        public const int MaxCozyAssetFiles = 20000;

        // Software / fallback renderers. Gradients and shadows on a
        // software rasteriser are not a style choice, they are a
        // slideshow.
        private static readonly string[] SoftwareRenderers =
        {
            "llvmpipe", "softpipe", "swiftshader", "microsoft basic render", "gdi generic"
        };

        // ==========================================
        // Measure
        // The machine as Unity reports it, plus the weight of
        // the project being exported, plus the verdict.
        // ==========================================
        public static DocSnapCapabilityReport Measure(int gameObjectCount, int assetFileCount)
        {
            var report = new DocSnapCapabilityReport
            {
                GameObjectCount = gameObjectCount,
                AssetFileCount = assetFileCount
            };

            // Every one of these can throw or return a sentinel on some
            // platform, and none of them is worth failing an export
            // over - an unmeasurable machine simply is not held against
            // the reader.
            try { report.SystemMemoryMb = SystemInfo.systemMemorySize; } catch { report.SystemMemoryMb = 0; }
            try { report.ProcessorCount = SystemInfo.processorCount; } catch { report.ProcessorCount = 0; }
            try { report.ProcessorFrequencyMhz = SystemInfo.processorFrequency; } catch { report.ProcessorFrequencyMhz = 0; }
            try { report.ProcessorType = SystemInfo.processorType ?? ""; } catch { report.ProcessorType = ""; }
            try { report.GraphicsDeviceName = SystemInfo.graphicsDeviceName ?? ""; } catch { report.GraphicsDeviceName = ""; }
            try { report.GraphicsMemoryMb = SystemInfo.graphicsMemorySize; } catch { report.GraphicsMemoryMb = 0; }

            // A zero from SystemInfo means "not reported on this
            // platform", not "this machine has no memory". Treating an
            // unknown as a failure would push most Linux and some macOS
            // exports to lite for no reason.
            if (report.SystemMemoryMb > 0 && report.SystemMemoryMb < MinMemoryMb)
            {
                report.Reasons.Add(Describe("RAM", report.SystemMemoryMb + " MB", MinMemoryMb + " MB"));
            }
            if (report.ProcessorCount > 0 && report.ProcessorCount < MinProcessorCount)
            {
                report.Reasons.Add(Describe("CPU cores", report.ProcessorCount.ToString(CultureInfo.InvariantCulture), MinProcessorCount.ToString(CultureInfo.InvariantCulture)));
            }
            if (report.GraphicsMemoryMb > 0 && report.GraphicsMemoryMb < MinGraphicsMemoryMb)
            {
                report.Reasons.Add(Describe("GPU memory", report.GraphicsMemoryMb + " MB", MinGraphicsMemoryMb + " MB"));
            }
            if (IsSoftwareRenderer(report.GraphicsDeviceName))
            {
                report.Reasons.Add("Software renderer (" + report.GraphicsDeviceName + ") — no GPU acceleration for gradients or shadows");
            }

            // Project weight matters at least as much as the machine:
            // the cozy skin's cost is per rendered row, so it is the
            // 40 000-object Scene that decides, not the CPU.
            if (gameObjectCount > MaxCozyGameObjects)
            {
                report.Reasons.Add("Large project: " + gameObjectCount.ToString("N0", CultureInfo.InvariantCulture)
                    + " GameObjects (animations stay smooth up to about " + MaxCozyGameObjects.ToString("N0", CultureInfo.InvariantCulture) + ")");
            }
            if (assetFileCount > MaxCozyAssetFiles)
            {
                report.Reasons.Add("Large project: " + assetFileCount.ToString("N0", CultureInfo.InvariantCulture)
                    + " asset files (about " + MaxCozyAssetFiles.ToString("N0", CultureInfo.InvariantCulture) + " is where the lighter skin starts paying off)");
            }

            report.Skin = report.Reasons.Count == 0 ? SkinCozy : SkinLite;
            return report;
        }

        private static string Describe(string what, string got, string wanted)
        {
            return what + ": " + got + " (" + wanted + " recommended for the cozy skin)";
        }

        private static bool IsSoftwareRenderer(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName)) { return false; }
            string lower = deviceName.ToLowerInvariant();
            foreach (string marker in SoftwareRenderers)
            {
                if (lower.Contains(marker)) { return true; }
            }
            return false;
        }

        // ==========================================
        // Normalize
        // Accepts a stored / user-supplied skin name and
        // returns one of the two real ones.
        // ==========================================
        public static string Normalize(string skin)
        {
            return skin == SkinCozy ? SkinCozy : SkinLite;
        }
    }
}
