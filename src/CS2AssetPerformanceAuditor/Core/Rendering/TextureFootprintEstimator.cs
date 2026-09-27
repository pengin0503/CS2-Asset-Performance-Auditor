using System;
using System.Collections.Generic;
using CS2AssetPerformanceAuditor.Core.Observations;

namespace CS2AssetPerformanceAuditor.Core.Rendering
{
    public static class TextureFootprintEstimator
    {
        private readonly struct FormatLayout
        {
            public FormatLayout(int blockWidth, int blockHeight, int bytesPerBlock) { BlockWidth = blockWidth; BlockHeight = blockHeight; BytesPerBlock = bytesPerBlock; }
            public int BlockWidth { get; }
            public int BlockHeight { get; }
            public int BytesPerBlock { get; }
        }

        private static readonly IReadOnlyDictionary<string, FormatLayout> Layouts = new Dictionary<string, FormatLayout>(StringComparer.Ordinal)
        {
            ["R8_UNorm"] = new FormatLayout(1, 1, 1),
            ["R8G8_UNorm"] = new FormatLayout(1, 1, 2),
            ["R8G8B8A8_UNorm"] = new FormatLayout(1, 1, 4),
            ["R8G8B8A8_SRGB"] = new FormatLayout(1, 1, 4),
            ["R16G16B16A16_SFloat"] = new FormatLayout(1, 1, 8),
            ["BC1_RGB_UNorm"] = new FormatLayout(4, 4, 8),
            ["BC1_RGB_SRGB"] = new FormatLayout(4, 4, 8),
            ["BC1_RGBA_UNorm"] = new FormatLayout(4, 4, 8),
            ["BC1_RGBA_SRGB"] = new FormatLayout(4, 4, 8),
            ["BC3_UNorm"] = new FormatLayout(4, 4, 16),
            ["BC3_SRGB"] = new FormatLayout(4, 4, 16),
            ["BC5_UNorm"] = new FormatLayout(4, 4, 16),
            ["BC7_UNorm"] = new FormatLayout(4, 4, 16),
            ["BC7_SRGB"] = new FormatLayout(4, 4, 16)
        };

        public const string MetricName = "Estimated logical full texture payload";

        public static Observation<long> Estimate(int width, int height, int depth, int mipsCount, string format, DateTimeOffset capturedAt)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (depth <= 0) throw new ArgumentOutOfRangeException(nameof(depth));
            if (mipsCount <= 0) throw new ArgumentOutOfRangeException(nameof(mipsCount));
            if (string.IsNullOrWhiteSpace(format)) throw new ArgumentException("Graphics format is required.", nameof(format));
            if (!Layouts.TryGetValue(format, out var layout))
                return Observation<long>.Unavailable(Availability.Unsupported, ObservationOrigin.Estimated, capturedAt);

            long total = 0;
            var w = width; var h = height; var d = depth;
            for (var mip = 0; mip < mipsCount; mip++)
            {
                var blocksX = (w + layout.BlockWidth - 1) / layout.BlockWidth;
                var blocksY = (h + layout.BlockHeight - 1) / layout.BlockHeight;
                total = checked(total + checked((long)blocksX * blocksY * Math.Max(1, d) * layout.BytesPerBlock));
                w = Math.Max(1, w / 2); h = Math.Max(1, h / 2); d = Math.Max(1, d / 2);
            }
            return Observation<long>.FromValue(total, ObservationOrigin.Estimated, capturedAt);
        }
    }
}
