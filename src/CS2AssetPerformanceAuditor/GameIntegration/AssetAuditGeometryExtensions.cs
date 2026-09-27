using System;
using System.Runtime.CompilerServices;
using CS2AssetPerformanceAuditor.Core.Rendering;
using CS2AssetPerformanceAuditor.GameIntegration.Rendering;
using Game.Prefabs;

namespace CS2AssetPerformanceAuditor.GameIntegration
{
    public static class AssetAuditGeometryExtensions
    {
        private static readonly ConditionalWeakTable<AssetAuditSystem, GeometryAssetReader> Readers = new ConditionalWeakTable<AssetAuditSystem, GeometryAssetReader>();
        public static GeometryObservation ReadGeometryMetadata(this AssetAuditSystem system, RenderPrefab renderPrefab, long analysisGeneration)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            if (analysisGeneration < 0) throw new ArgumentOutOfRangeException(nameof(analysisGeneration));
            return Readers.GetValue(system, _ => new GeometryAssetReader()).Read(renderPrefab, analysisGeneration, DateTimeOffset.UtcNow);
        }
    }
}
