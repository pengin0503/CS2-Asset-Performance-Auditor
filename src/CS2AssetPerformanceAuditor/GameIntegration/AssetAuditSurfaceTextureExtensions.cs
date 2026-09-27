using System;
using System.Runtime.CompilerServices;
using Colossal.IO.AssetDatabase;
using CS2AssetPerformanceAuditor.Core.Rendering;
using CS2AssetPerformanceAuditor.GameIntegration.Rendering;

namespace CS2AssetPerformanceAuditor.GameIntegration
{
    public static class AssetAuditSurfaceTextureExtensions
    {
        private sealed class Readers
        {
            public readonly SurfaceAssetReader Surface = new SurfaceAssetReader();
            public readonly TextureAssetReader Texture = new TextureAssetReader();
        }
        private static readonly ConditionalWeakTable<AssetAuditSystem, Readers> Cache = new ConditionalWeakTable<AssetAuditSystem, Readers>();

        public static SurfaceObservation ReadSurfaceMetadata(this AssetAuditSystem system, SurfaceAsset surface, long analysisGeneration)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            return Cache.GetValue(system, _ => new Readers()).Surface.Read(surface, analysisGeneration, DateTimeOffset.UtcNow);
        }

        public static TextureObservation ReadTextureMetadata(this AssetAuditSystem system, TextureAsset texture, long analysisGeneration)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            return Cache.GetValue(system, _ => new Readers()).Texture.Read(texture, analysisGeneration, DateTimeOffset.UtcNow);
        }
    }
}
