using System;
using System.Linq;
using System.Text.Json;
using CS2AssetPerformanceAuditor.Core.Capabilities;
using CS2AssetPerformanceAuditor.Core.Census;
using CS2AssetPerformanceAuditor.Core.Observations;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using CS2AssetPerformanceAuditor.Core.Rendering;
using CS2AssetPerformanceAuditor.Export;
using NUnit.Framework;

namespace CS2AssetPerformanceAuditor.Tests
{
    [TestFixture]
    public sealed class AnalysisExportTests
    {
        private static readonly DateTimeOffset CapturedAt = new DateTimeOffset(2026, 9, 27, 9, 0, 0, TimeSpan.Zero);

        [Test]
        public void Full_report_preserves_prefab_render_geometry_surface_and_texture_hierarchy()
        {
            var prefab = new PrefabRecord(new PrefabKey("House.A", "Building"), "House A", PrefabTraits.Building, new AssetOriginEvidence(isBuiltin: true));
            var renderKey = new RenderAssetKey("Render.House.A", "Game.Prefabs.RenderPrefab");
            var render = new RenderAssetRecord(renderKey, "House A Render");
            var relation = new PrefabRenderRelation(prefab.Key, renderKey, RenderRelationKind.DirectMesh);
            var geometry = new GeometryObservation(
                "Geometry.House.A",
                Observation<int>.FromValue(1, ObservationOrigin.AssetDatabase, CapturedAt),
                Observation<long>.FromValue(1200, ObservationOrigin.Derived, CapturedAt),
                Observation<long>.FromValue(3600, ObservationOrigin.Derived, CapturedAt),
                Observation<int>.FromValue(2, ObservationOrigin.Derived, CapturedAt),
                Observation<long>.FromValue(4096, ObservationOrigin.AssetDatabase, CapturedAt),
                Array.Empty<MeshObservation>());
            var surface = new SurfaceObservation(
                "Surface.House.A",
                Observation<int>.FromValue(123, ObservationOrigin.AssetDatabase, CapturedAt),
                Observation<bool>.FromValue(false, ObservationOrigin.AssetDatabase, CapturedAt),
                Observation<bool>.FromValue(false, ObservationOrigin.AssetDatabase, CapturedAt),
                2, 1, 0, 1,
                new[] { "FOG_ON" },
                new[] { "Texture.House.A" });
            var texture = TextureObservation.Available(
                "Texture.House.A", 1024, 1024, 1, "DXT1", "Tex2D", 11, "Bilinear", "Repeat", 1, 699048, CapturedAt);
            var prefabAnalysis = new PrefabAnalysisEntry(
                prefab.Key,
                RenderCoverage.Supported,
                new[] { relation },
                Observation<long>.FromValue(1200, ObservationOrigin.Derived, CapturedAt),
                Observation<double>.Unavailable(Availability.NotApplicable, ObservationOrigin.Derived, CapturedAt),
                Observation<long>.FromValue(1, ObservationOrigin.Derived, CapturedAt),
                Observation<long>.FromValue(1, ObservationOrigin.Derived, CapturedAt),
                Observation<long>.FromValue(699048, ObservationOrigin.Estimated, CapturedAt));
            var analysis = new AssetAnalysisSnapshot(
                worldGeneration: 3,
                catalogGeneration: 7,
                analysisGeneration: 2,
                CapturedAt,
                new[] { prefabAnalysis },
                new[] { new RenderAssetAnalysisRecord(render, geometry, new[] { surface }, new[] { texture }) });
            var capabilities = new CapabilityReport("1.6.2f1", CompatibilityState.Untested, Array.Empty<CapabilityStatus>());

            var report = new AuditReportBuilder(new PrivacySanitizer())
                .Build(new[] { prefab }, 7, CapturedAt, null, capabilities, "0.1.0", CapturedAt, analysis: analysis);
            using var document = JsonDocument.Parse(new AuditReportSerializer().Serialize(report));
            var exportedAnalysis = document.RootElement.GetProperty("analysis");

            Assert.That(exportedAnalysis.GetProperty("assets")[0].GetProperty("renderCoverage").GetString(), Is.EqualTo("Supported"));
            Assert.That(exportedAnalysis.GetProperty("assets")[0].GetProperty("renderRelations")[0].GetProperty("renderAssetId").GetString(), Is.EqualTo("Render.House.A"));
            var exportedRender = exportedAnalysis.GetProperty("renderAssets")[0];
            Assert.That(exportedRender.GetProperty("geometry").GetProperty("totalVertexCount").GetProperty("value").GetInt64(), Is.EqualTo(1200));
            Assert.That(exportedRender.GetProperty("surfaces")[0].GetProperty("surfaceAssetId").GetString(), Is.EqualTo("Surface.House.A"));
            Assert.That(exportedRender.GetProperty("textures")[0].GetProperty("textureAssetId").GetString(), Is.EqualTo("Texture.House.A"));
            Assert.That(exportedRender.GetProperty("textures")[0].GetProperty("estimatedLogicalPayload").GetProperty("origin").GetString(), Is.EqualTo("Estimated"));
        }

        [Test]
        public void Current_report_entrypoint_carries_matching_analysis_hierarchy()
        {
            var prefab = new PrefabRecord(new PrefabKey("House.Current", "Building"), "Current House", PrefabTraits.Building, new AssetOriginEvidence(isBuiltin: true));
            var analysisEntry = new PrefabAnalysisEntry(
                prefab.Key,
                RenderCoverage.Supported,
                Array.Empty<PrefabRenderRelation>(),
                Observation<long>.FromValue(100, ObservationOrigin.Derived, CapturedAt),
                Observation<double>.Unavailable(Availability.NotApplicable, ObservationOrigin.Derived, CapturedAt),
                Observation<long>.FromValue(0, ObservationOrigin.Derived, CapturedAt),
                Observation<long>.FromValue(0, ObservationOrigin.Derived, CapturedAt),
                Observation<long>.FromValue(0, ObservationOrigin.Estimated, CapturedAt));
            var analysis = new AssetAnalysisSnapshot(2, 11, 4, CapturedAt, new[] { analysisEntry }, Array.Empty<RenderAssetAnalysisRecord>());
            var capabilities = new CapabilityReport("1.6.2f1", CompatibilityState.Untested, Array.Empty<CapabilityStatus>());

            var report = new AuditReportBuilder(new PrivacySanitizer()).BuildCurrent(
                new[] { prefab }, 11, CapturedAt, null, analysis, capabilities, "0.1.0", CapturedAt);

            Assert.That(report.Analysis.Assets, Has.Length.EqualTo(1));
            Assert.That(report.Analysis.Assets[0].PrefabId, Is.EqualTo("House.Current"));
        }

        [Test]
        public void Current_report_entrypoint_omits_snapshots_from_previous_catalog_generation()
        {
            var prefab = new PrefabRecord(new PrefabKey("House.Old", "Building"), "Old House", PrefabTraits.Building, new AssetOriginEvidence(isBuiltin: true));
            var staleCensus = new CensusReducer(new[] { prefab }, 1, 10, CapturedAt, ScanOptions.Default).BuildSnapshot();
            var staleAnalysis = new AssetAnalysisSnapshot(1, 10, 1, CapturedAt, Array.Empty<PrefabAnalysisEntry>(), Array.Empty<RenderAssetAnalysisRecord>());
            var capabilities = new CapabilityReport("1.6.2f1", CompatibilityState.Untested, Array.Empty<CapabilityStatus>());

            var report = new AuditReportBuilder(new PrivacySanitizer()).BuildCurrent(
                new[] { prefab }, 11, CapturedAt, staleCensus, staleAnalysis, capabilities, "0.1.0", CapturedAt);

            Assert.That(report.Census, Is.Empty);
            Assert.That(report.CensusCatalogGeneration, Is.Null);
            Assert.That(report.Analysis.Assets, Is.Empty);
            Assert.That(report.Analysis.RenderAssets, Is.Empty);
        }

        [Test]
        public void Analysis_from_different_catalog_generation_is_rejected()
        {
            var analysis = new AssetAnalysisSnapshot(1, 4, 1, CapturedAt, Array.Empty<PrefabAnalysisEntry>(), Array.Empty<RenderAssetAnalysisRecord>());
            var capabilities = new CapabilityReport("1.6.2f1", CompatibilityState.Untested, Array.Empty<CapabilityStatus>());
            var builder = new AuditReportBuilder(new PrivacySanitizer());

            Assert.Throws<InvalidOperationException>(() => builder.Build(
                Array.Empty<PrefabRecord>(), 5, CapturedAt, null, capabilities, "0.1.0", CapturedAt, analysis: analysis));
        }
    }
}
