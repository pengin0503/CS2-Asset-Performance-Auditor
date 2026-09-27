using System;
using Colossal.IO.AssetDatabase;
using CS2AssetPerformanceAuditor.Core.Rendering;

namespace CS2AssetPerformanceAuditor.GameIntegration.Rendering
{
    public sealed class TextureAssetReader
    {
        private readonly GenerationCache<string, TextureObservation> _cache = new GenerationCache<string, TextureObservation>();

        public TextureObservation Read(TextureAsset texture, long analysisGeneration, DateTimeOffset capturedAt)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            var id = StableId(texture);
            return _cache.GetOrAdd(analysisGeneration, id, () => ReadCore(texture, id, capturedAt));
        }

        public void ClearCache() => _cache.Clear();

        private static TextureObservation ReadCore(TextureAsset texture, string id, DateTimeOffset capturedAt)
        {
            var width = texture.width;
            var height = texture.height;
            // TextureAsset stores Tex2D (depth 1) and Tex2DArray (depth = slice count) data only.
            var depth = Math.Max(1, texture.depth);
            var mips = Math.Max(1, texture.mipsCount);
            var format = texture.format.ToString();
            var estimated = TextureFootprintEstimator.Estimate(width, height, depth, mips, format, capturedAt);
            return TextureObservation.FromMetadata(id, width, height, depth, format, texture.dimension.ToString(), mips,
                texture.filterMode.ToString(), texture.wrapMode.ToString(), texture.anisoLevel, estimated, capturedAt);
        }

        private static string StableId(TextureAsset asset)
        {
            if (!string.IsNullOrWhiteSpace(asset.identifier)) return asset.identifier;
            if (!string.IsNullOrWhiteSpace(asset.uniqueName)) return asset.uniqueName;
            if (!string.IsNullOrWhiteSpace(asset.name)) return asset.name;
            throw new InvalidOperationException("A stable TextureAsset identifier is unavailable.");
        }
    }
}
