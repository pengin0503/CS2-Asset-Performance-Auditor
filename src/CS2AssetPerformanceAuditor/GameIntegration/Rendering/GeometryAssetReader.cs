using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using CS2AssetPerformanceAuditor.Core.Observations;
using CS2AssetPerformanceAuditor.Core.Rendering;

namespace CS2AssetPerformanceAuditor.GameIntegration.Rendering
{
    public sealed class GeometryAssetReader
    {
        private readonly GenerationCache<string, GeometryObservation> _cache = new GenerationCache<string, GeometryObservation>();

        public GeometryObservation Read(GeometryAsset? geometryAsset, long analysisGeneration, DateTimeOffset capturedAt)
        {
            if (geometryAsset == null)
                return GeometryObservation.Unavailable("geometry:missing", Availability.Failed, "APA-GEO-001", capturedAt);
            string id;
            try { id = StableId(geometryAsset); }
            catch { return GeometryObservation.Unavailable("geometry:unidentified", Availability.Failed, "APA-GEO-001", capturedAt); }
            return _cache.GetOrAdd(analysisGeneration, id, () => ReadCore(geometryAsset, id, capturedAt));
        }

        public void ClearCache() => _cache.Clear();

        private static GeometryObservation ReadCore(GeometryAsset geometryAsset, string id, DateTimeOffset capturedAt)
        {
            try
            {
                var meshCount = geometryAsset.meshCount;
                var meshes = new List<MeshObservation>(Math.Max(0, meshCount));
                long totalVertices = 0;
                long totalIndices = 0;
                var totalSubMeshes = 0;
                for (var meshIndex = 0; meshIndex < meshCount; meshIndex++)
                {
                    var vertexCount = geometryAsset.GetVertexCount(meshIndex);
                    var indexCount = geometryAsset.GetIndicesCount(meshIndex);
                    var subMeshCount = geometryAsset.GetSubMeshCount(meshIndex);
                    var subMeshes = new List<SubMeshObservation>(Math.Max(0, subMeshCount));
                    for (var subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
                    {
                        var descriptor = geometryAsset.GetSubMeshDesc(meshIndex, subMeshIndex);
                        var bounds = descriptor.bounds;
                        subMeshes.Add(SubMeshObservation.Create(meshIndex, subMeshIndex, descriptor.topology.ToString(), descriptor.indexCount, descriptor.vertexCount,
                            BoundsObservation.FromValues(bounds.center.x, bounds.center.y, bounds.center.z, bounds.extents.x, bounds.extents.y, bounds.extents.z), capturedAt));
                    }
                    totalVertices = checked(totalVertices + vertexCount);
                    totalIndices = checked(totalIndices + indexCount);
                    totalSubMeshes = checked(totalSubMeshes + subMeshCount);
                    meshes.Add(new MeshObservation(meshIndex,
                        Observation<int>.FromValue(vertexCount, ObservationOrigin.AssetDatabase, capturedAt),
                        Observation<int>.FromValue(indexCount, ObservationOrigin.AssetDatabase, capturedAt),
                        Observation<string>.FromValue(geometryAsset.GetIndexFormat(meshIndex).ToString(), ObservationOrigin.AssetDatabase, capturedAt),
                        subMeshes));
                }
                return new GeometryObservation(id,
                    Observation<int>.FromValue(meshCount, ObservationOrigin.AssetDatabase, capturedAt),
                    Observation<long>.FromValue(totalVertices, ObservationOrigin.Derived, capturedAt),
                    Observation<long>.FromValue(totalIndices, ObservationOrigin.Derived, capturedAt),
                    Observation<int>.FromValue(totalSubMeshes, ObservationOrigin.Derived, capturedAt),
                    Observation<long>.FromValue(geometryAsset.compressedDataSize, ObservationOrigin.AssetDatabase, capturedAt), meshes);
            }
            catch
            {
                return GeometryObservation.Unavailable(id, Availability.Failed, "APA-GEO-002", capturedAt);
            }
        }

        private static string StableId(GeometryAsset asset)
        {
            if (!string.IsNullOrWhiteSpace(asset.identifier)) return asset.identifier;
            if (!string.IsNullOrWhiteSpace(asset.uniqueName)) return asset.uniqueName;
            if (!string.IsNullOrWhiteSpace(asset.name)) return asset.name;
            throw new InvalidOperationException("A stable GeometryAsset identifier is unavailable.");
        }
    }
}
