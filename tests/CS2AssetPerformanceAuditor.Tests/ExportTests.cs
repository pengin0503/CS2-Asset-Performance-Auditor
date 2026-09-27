using System;
using System.Linq;
using System.Text.Json;
using CS2AssetPerformanceAuditor.Core.Capabilities;
using CS2AssetPerformanceAuditor.Core.Census;
using CS2AssetPerformanceAuditor.Core.Diagnostics;
using CS2AssetPerformanceAuditor.Core.Findings;
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
            var prefab = new PrefabRecord(new PrefabKey("asset-house", "Building"), "House", PrefabTraits.Building, new AssetOriginEvidence(isBuiltin: true));
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
        public void Full_json_preserves_finding_evidence_basis_and_rule_version()
        {
            var prefab = new PrefabRecord(new PrefabKey("asset-house", "Building"), "House", PrefabTraits.Building, new AssetOriginEvidence(isBuiltin: true));
            var finding = new Finding("APA-LOD-002", FindingStatus.PotentialIssue, FindingCategory.Lod,
                "Weak LOD vertex reduction", "Heuristic evidence only.", new[] { "vertexRetentionPercent=92" }, FindingBasis.Heuristic, RuleSetInfo.Version);
            var capabilities = new CapabilityReport("1.6.2f1", CompatibilityState.Untested, Array.Empty<CapabilityStatus>());
            var report = new AuditReportBuilder(new PrivacySanitizer("test-user", "test-host"))
                .Build(new[] { prefab }, 1, CapturedAt, null, capabilities, "0.1.0", CapturedAt, findings: new[] { finding }, scope: ExportScope.Full);
            using var document = JsonDocument.Parse(new AuditReportSerializer().Serialize(report));
            var exported = document.RootElement.GetProperty("analysis").GetProperty("findings")[0];

            Assert.That(exported.GetProperty("ruleId").GetString(), Is.EqualTo("APA-LOD-002"));
            Assert.That(exported.GetProperty("basis").GetString(), Is.EqualTo("Heuristic"));
            Assert.That(exported.GetProperty("ruleVersion").GetString(), Is.EqualTo(RuleSetInfo.Version));
            Assert.That(exported.GetProperty("evidence")[0].GetString(), Is.EqualTo("vertexRetentionPercent=92"));
        }

        [Test]
        public void Csv_summary_is_flat_and_escapes_fields_without_nested_json()
        {
            var report = new AuditReport
            {
                Catalog = new[] { new ReportPrefab { PrefabId = "asset,1", PrefabType = "Building", DisplayName = "House \"A\"", Traits = "Building" } },
                Census = new[] { new ReportCensusEntry { PrefabId = "asset,1", PrefabType = "Building", Presence = "Present", Counters = new ReportCensusCounters { TopLevelObjects = new ReportObservation { Availability = "Available", Value = 2 } } } },
                Analysis = new ReportAnalysis { Findings = new[] { new ReportFinding { RuleId = "APA-LOD-001", Status = "Notice" } } }
            };
            var csv = new CsvSummaryExporter().Export(report);

            Assert.That(csv, Does.Contain("prefabId,prefabType,displayName,traits,presence,topLevelObjects,findingCount"));
            Assert.That(csv, Does.Contain("\"asset,1\""));
            Assert.That(csv, Does.Contain("\"House \"\"A\"\"\""));
            Assert.That(csv, Does.Not.Contain("{\""));
        }

        [Test]
        public void ReportRejectsCensusFromDifferentCatalogGeneration()
        {
            var prefab = new PrefabRecord(new PrefabKey("asset-house", "Building"), "House", PrefabTraits.Building, new AssetOriginEvidence(isBuiltin: true));
            var census = new CensusReducer(new[] { prefab }, 4, 9, CapturedAt, ScanOptions.Default).BuildSnapshot();
            var capabilities = new CapabilityReport("1.6.2f1", CompatibilityState.Untested, Array.Empty<CapabilityStatus>());
            var builder = new AuditReportBuilder(new PrivacySanitizer("test-user", "test-host"));
            Assert.Throws<InvalidOperationException>(() => builder.Build(new[] { prefab }, 10, CapturedAt, census, capabilities, "0.1.0", CapturedAt));
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
            var prefab = new PrefabRecord(new PrefabKey(privatePath, "Building"), "Alice's HOST-7 House", PrefabTraits.Building,
                new AssetOriginEvidence(isBuiltin: false, assetDatabaseSource: privatePath, assetPackMembership: new[] { @"/home/alice/.local/share/Paradox/assets/private-pack" }));
            var capabilities = new CapabilityReport("1.6.2f1", CompatibilityState.Untested, Array.Empty<CapabilityStatus>());
            var finding = new Finding("APA-TEX-001", FindingStatus.Unknown, FindingCategory.Texture, "Read failed", privatePath, new[] { privatePath }, FindingBasis.Observation, RuleSetInfo.Version);
            var report = new AuditReportBuilder(new PrivacySanitizer("alice", "HOST-7"))
                .Build(new[] { prefab }, 12, CapturedAt, null, capabilities, "0.1.0", CapturedAt, findings: new[] { finding });
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
