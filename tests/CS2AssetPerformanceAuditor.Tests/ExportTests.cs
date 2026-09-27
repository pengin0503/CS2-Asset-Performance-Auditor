using System;
using System.Linq;
using System.Text.Json;
using CS2AssetPerformanceAuditor.Core.Capabilities;
using CS2AssetPerformanceAuditor.Core.Census;
using CS2AssetPerformanceAuditor.Core.Diagnostics;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using CS2AssetPerformanceAuditor.Export;
using NUnit.Framework;

namespace CS2AssetPerformanceAuditor.Tests
{
    public sealed class ExportTests
    {
        private static readonly DateTimeOffset CapturedAt = new DateTimeOffset(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

        [Test]
        public void JsonContainsRequiredVersionScanCapabilityAndCaptureMetadata()
        {
            var prefab = new PrefabRecord(
                new PrefabKey("asset-house", "Building"), "House", PrefabTraits.Building,
                new AssetOriginEvidence(isBuiltin: true));
            var reducer = new CensusReducer(new[] { prefab }, 4, 9, CapturedAt, ScanOptions.Default);
            reducer.AddObject(prefab.Key, isSubordinate: false);
            var capabilities = new CapabilityReport("1.6.2f1", CompatibilityState.Untested,
                new[] { new CapabilityStatus(CapabilityId.ObjectCensus, CapabilityState.Supported, "probe_ok") });
            var report = new AuditReportBuilder(new PrivacySanitizer("test-user", "test-host"))
                .Build(new[] { prefab }, 9, CapturedAt, reducer.BuildSnapshot(), capabilities, "0.1.0", CapturedAt);
            var json = new AuditReportSerializer().Serialize(report);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            Assert.That(root.GetProperty("schemaVersion").GetString(), Is.EqualTo(ReportSchema.SchemaVersion));
            Assert.That(root.GetProperty("ruleSetVersion").GetString(), Is.EqualTo(ReportSchema.RuleSetVersion));
            Assert.That(root.GetProperty("queryProfileVersion").GetString(), Is.EqualTo(CensusQueryProfile.V1));
            Assert.That(root.GetProperty("scanOptions").GetProperty("collectNetworkEdges").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("modVersion").GetString(), Is.EqualTo("0.1.0"));
            Assert.That(root.GetProperty("gameVersion").GetString(), Is.EqualTo("1.6.2f1"));
            Assert.That(root.GetProperty("generatedAt").GetString(), Is.EqualTo(CapturedAt.ToString("O")));
            Assert.That(root.GetProperty("catalogCapturedAt").GetString(), Is.EqualTo(CapturedAt.ToString("O")));
            Assert.That(root.GetProperty("censusCapturedAt").GetString(), Is.EqualTo(CapturedAt.ToString("O")));
            Assert.That(root.GetProperty("capabilityReport").GetProperty("compatibility").GetString(), Is.EqualTo("Untested"));
            Assert.That(root.GetProperty("catalog").GetArrayLength(), Is.EqualTo(1));
            Assert.That(root.GetProperty("census").GetArrayLength(), Is.EqualTo(1));
        }

        [Test]
        public void ReportRejectsCensusFromDifferentCatalogGeneration()
        {
            var prefab = new PrefabRecord(
                new PrefabKey("asset-house", "Building"), "House", PrefabTraits.Building,
                new AssetOriginEvidence(isBuiltin: true));
            var census = new CensusReducer(new[] { prefab }, 4, 9, CapturedAt, ScanOptions.Default).BuildSnapshot();
            var capabilities = new CapabilityReport("1.6.2f1", CompatibilityState.Untested,
                Array.Empty<CapabilityStatus>());
            var builder = new AuditReportBuilder(new PrivacySanitizer("test-user", "test-host"));

            Assert.Throws<InvalidOperationException>(() => builder.Build(
                new[] { prefab },
                catalogGeneration: 10,
                catalogCapturedAt: CapturedAt,
                census,
                capabilities,
                "0.1.0",
                CapturedAt));
        }

        [Test]
        public void IdenticalDiagnosticsAreAggregatedInReport()
        {
            var diagnostics = new DiagnosticAggregator();
            diagnostics.Add("APA-CAT-004", "prefab_resolution_failed");
            diagnostics.Add("APA-CAT-004", "prefab_resolution_failed");

            var aggregate = diagnostics.Snapshot().Single();
            Assert.That(aggregate.Count, Is.EqualTo(2));
            Assert.That(aggregate.Code.Value, Is.EqualTo("APA-CAT-004"));
        }

        [Test]
        public void ReportDoesNotContainLocalUserHostAbsolutePathsOrRuntimeEntityIndexes()
        {
            var privatePath = @"C:\Users\alice\Documents\CS2\Mods\Audit\asset.prefab";
            var prefab = new PrefabRecord(
                new PrefabKey(privatePath, "Building"), "Alice's HOST-7 House", PrefabTraits.Building,
                new AssetOriginEvidence(isBuiltin: false, assetDatabaseSource: privatePath,
                    assetPackMembership: new[] { @"/home/alice/.local/share/Paradox/assets/private-pack" }));
            var capabilities = new CapabilityReport("1.6.2f1", CompatibilityState.Untested,
                Array.Empty<CapabilityStatus>());
            var report = new AuditReportBuilder(new PrivacySanitizer("alice", "HOST-7"))
                .Build(new[] { prefab }, 12, CapturedAt, census: null, capabilities, "0.1.0", CapturedAt);
            var json = new AuditReportSerializer().Serialize(report);

            Assert.That(json, Does.Not.Contain("alice"));
            Assert.That(json, Does.Not.Contain("HOST-7"));
            Assert.That(json, Does.Not.Contain(@"C:\Users\"));
            Assert.That(json, Does.Not.Contain("/home/"));
            Assert.That(json, Does.Not.Contain("Entity.Index"));
            Assert.That(json, Does.Contain("redacted"));
        }
    }
}
