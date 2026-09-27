using System;
using System.Runtime.CompilerServices;
using Colossal.IO.AssetDatabase;
using CS2AssetPerformanceAuditor.Core.Rendering;
using CS2AssetPerformanceAuditor.GameIntegration.Rendering;

namespace CS2AssetPerformanceAuditor.GameIntegration
{
    public static class AssetAuditGeometryExtensions
    {
        private static readonly ConditionalWeakTable<AssetAuditSystem, GeometryAssetReader> Readers = new ConditionalWeakTable<AssetAuditSystem, GeometryAssetReader>();
        public static GeometryObservation ReadGeometryMetadata(this AssetAuditSystem system, GeometryAsset? geometryAsset, long analysisGeneration)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            if (analysisGeneration < 0) throw new ArgumentOutOfRangeException(nameof(analysisGeneration));
            return Readers.GetValue(system, _ => new GeometryAssetReader()).Read(geometryAsset, analysisGeneration, DateTimeOffset.UtcNow);
        }
    }
}
