using System;
using System.Linq;
using System.Text.Json;
using CS2AssetPerformanceAuditor.Core.Capabilities;
using CS2AssetPerformanceAuditor.Core.Census;
using CS2AssetPerformanceAuditor.Core.Diagnostics;
using CS2AssetPerformanceAuditor.Core.Findings;
using CS2AssetPerformanceAuditor.Core.Observations;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using CS2AssetPerformanceAuditor.Core.Rendering;
using CS2AssetPerformanceAuditor.Export;
using NUnit.Framework;

namespace CS2AssetPerformanceAuditor.Tests
{
    [TestFixture]
    public sealed class BugRegressionTests
    {
        private static readonly DateTimeOffset CapturedAt = new DateTimeOffset(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);
        private static readonly CapabilityReport Capabilities = new CapabilityReport("1.6.2f1", CompatibilityState.Untested, Array.Empty<CapabilityStatus>());

        [TestCase("APA-AUD-004")]
        [TestCase("APA-DEEP-004")]
        public void Diagnostic_codes_raised_by_scan_and_ui_subsystems_are_accepted(string code)
        {
            var diagnostics = new DiagnosticAggregator();
            Assert.DoesNotThrow(() => diagnostics.Add(code, "rejected"));
            Assert.That(diagnostics.Snapshot().Single().Code.Value, Is.EqualTo(code));
        }

        [Test]
        public void Unknown_diagnostic_subsystems_are_still_rejected()
        {
            Assert.Throws<ArgumentException>(() => new DiagnosticCode("APA-XYZ-001"));
        }

        [Test]
        public void Weak_lod_finding_identifies_its_asset()
        {
            var finding = new FindingEngine()
                .EvaluateGeometryLod(new GeometryFindingInput("house", "Building", true, false, 1000, 950), CapturedAt)
                .Single(f => f.RuleId == "APA-LOD-002");
            Assert.That(finding.Evidence, Does.Contain("asset=house"));
        }

        [Test]
        public void Unresolved_render_structure_produces_no_lod_findings()
        {
            var findings = new FindingEngine().EvaluateGeometryLod(
                new GeometryFindingInput("zone", "Prefab", false, false, null, null, renderStructureResolved: false), CapturedAt);
            Assert.That(findings, Is.Empty);
        }

        [Test]
        public void Failed_render_structure_reports_integrity_without_claiming_a_missing_lod()
        {
            var findings = new FindingEngine().EvaluateGeometryLod(
                new GeometryFindingInput("broken", "Building", false, true, null, null, renderStructureResolved: false), CapturedAt);
            Assert.That(findings.Select(f => f.RuleId), Is.EqualTo(new[] { "APA-INT-001" }));
        }

        [TestCase(ExportScope.Filtered)]
        [TestCase(ExportScope.Selected)]
        public void Scoped_exports_keep_analysis_findings_that_have_no_asset_evidence(ExportScope scope)
        {
            var (prefab, other, analysis) = AnalysisWithOwnedFinding();
            var report = Builder().Build(new[] { prefab, other }, 5, CapturedAt, null, Capabilities, "0.1.0", CapturedAt,
                analysis: analysis, scope: scope, includedKeys: new[] { prefab.Key });

            var exported = report.Analysis.Findings.Single();
            Assert.That(exported.RuleId, Is.EqualTo("APA-LOD-002"));
            Assert.That(exported.PrefabId, Is.EqualTo("House.A"));
            Assert.That(exported.PrefabType, Is.EqualTo("Building"));
        }

        [Test]
        public void Scoped_exports_match_finding_owners_by_full_prefab_key()
        {
            var (prefab, other, analysis) = AnalysisWithOwnedFinding();
            var report = Builder().Build(new[] { prefab, other }, 5, CapturedAt, null, Capabilities, "0.1.0", CapturedAt,
                analysis: analysis, scope: ExportScope.Selected, includedKeys: new[] { other.Key });
            Assert.That(report.Analysis.Findings, Is.Empty);
        }

        [Test]
        public void Json_findings_carry_their_owning_prefab_key()
        {
            var (prefab, other, analysis) = AnalysisWithOwnedFinding();
            var report = Builder().Build(new[] { prefab, other }, 5, CapturedAt, null, Capabilities, "0.1.0", CapturedAt, analysis: analysis);
            using var document = JsonDocument.Parse(new AuditReportSerializer().Serialize(report));
            var finding = document.RootElement.GetProperty("analysis").GetProperty("findings")[0];
            Assert.That(finding.GetProperty("prefabId").GetString(), Is.EqualTo("House.A"));
            Assert.That(finding.GetProperty("prefabType").GetString(), Is.EqualTo("Building"));
        }

        [Test]
        public void Csv_counts_findings_by_owner_and_not_by_prefab_id_alone()
        {
            var (prefab, other, analysis) = AnalysisWithOwnedFinding();
            var report = Builder().Build(new[] { prefab, other }, 5, CapturedAt, null, Capabilities, "0.1.0", CapturedAt, analysis: analysis);
            var lines = new CsvSummaryExporter().Export(report).Trim().Split(Environment.NewLine);

            Assert.That(lines, Does.Contain("House.A,Building,House A,Building,,,1"));
            Assert.That(lines, Does.Contain("House.A,Prop,House A prop,Prop,,,0"));
        }

        [Test]
        public void Csv_census_scope_lists_census_rows()
        {
            var prefab = new PrefabRecord(new PrefabKey("House.A", "Building"), "House A", PrefabTraits.Building, new AssetOriginEvidence(isBuiltin: true));
            var reducer = new CensusReducer(new[] { prefab }, 1, 5, CapturedAt, ScanOptions.Default);
            reducer.AddObject(prefab.Key, isSubordinate: false);
            reducer.AddObject(prefab.Key, isSubordinate: false);
            var report = Builder().Build(new[] { prefab }, 5, CapturedAt, reducer.BuildSnapshot(), Capabilities, "0.1.0", CapturedAt, scope: ExportScope.Census);
            var lines = new CsvSummaryExporter().Export(report).Trim().Split(Environment.NewLine);

            Assert.That(lines, Has.Length.EqualTo(2));
            Assert.That(lines[1], Is.EqualTo("House.A,Building,,,Present,2,0"));
        }

        [Test]
        public void Csv_findings_scope_lists_finding_owners()
        {
            var (prefab, other, analysis) = AnalysisWithOwnedFinding();
            var report = Builder().Build(new[] { prefab, other }, 5, CapturedAt, null, Capabilities, "0.1.0", CapturedAt, analysis: analysis, scope: ExportScope.Findings);
            var lines = new CsvSummaryExporter().Export(report).Trim().Split(Environment.NewLine);

            Assert.That(lines, Has.Length.EqualTo(2));
            Assert.That(lines[1], Is.EqualTo("House.A,Building,,,,,1"));
        }

        [TestCase("Traffic Light / Pole")]
        [TestCase("See https://example.com/docs for details")]
        [TestCase("Sign 50/60 and A/B variant")]
        public void Sanitizer_keeps_text_that_is_not_an_absolute_path(string text)
        {
            Assert.That(new PrivacySanitizer("alice", "HOST-7").SanitizeText(text), Is.EqualTo(text));
        }

        [Test]
        public void Sanitizer_still_redacts_absolute_unix_paths_with_spaces()
        {
            var sanitized = new PrivacySanitizer("alice", "HOST-7").SanitizeText("loaded /Users/John Doe/Library/Mods/city.mod");
            Assert.That(sanitized, Does.Not.Contain("/Users/"));
            Assert.That(sanitized, Does.Not.Contain("John Doe"));
            Assert.That(sanitized, Does.Contain("[redacted-path]"));
        }

        [Test]
        public void Catalog_content_comparison_ignores_capture_order_but_detects_changes()
        {
            var house = new PrefabRecord(new PrefabKey("House.A", "Building"), "House A", PrefabTraits.Building, new AssetOriginEvidence(isBuiltin: true));
            var tree = new PrefabRecord(new PrefabKey("Tree.A", "Tree"), "Tree A", PrefabTraits.Tree, new AssetOriginEvidence(isBuiltin: true, dlcPrerequisiteIds: new[] { "dlc" }));
            var sameTree = new PrefabRecord(new PrefabKey("Tree.A", "Tree"), "Tree A", PrefabTraits.Tree, new AssetOriginEvidence(isBuiltin: true, dlcPrerequisiteIds: new[] { "dlc" }));
            var renamedTree = new PrefabRecord(new PrefabKey("Tree.A", "Tree"), "Tree B", PrefabTraits.Tree, new AssetOriginEvidence(isBuiltin: true, dlcPrerequisiteIds: new[] { "dlc" }));
            var modTree = new PrefabRecord(new PrefabKey("Tree.A", "Tree"), "Tree A", PrefabTraits.Tree, new AssetOriginEvidence(isBuiltin: false, dlcPrerequisiteIds: new[] { "dlc" }));

            Assert.That(PrefabCatalogContent.HasSameRecords(new[] { house, tree }, new[] { sameTree, house }), Is.True);
            Assert.That(PrefabCatalogContent.HasSameRecords(new[] { house, tree }, new[] { house, renamedTree }), Is.False);
            Assert.That(PrefabCatalogContent.HasSameRecords(new[] { house, tree }, new[] { house, modTree }), Is.False);
            Assert.That(PrefabCatalogContent.HasSameRecords(new[] { house, tree }, new[] { house }), Is.False);
        }

        [Test]
        public void Texture_whose_header_was_never_read_is_not_scanned_rather_than_zero_or_failed()
        {
            var texture = TextureObservation.NotResident("Texture.VT", CapturedAt);
            Assert.That(texture.EstimatedLogicalPayload.Availability, Is.EqualTo(Availability.NotScanned));
            Assert.That(texture.EstimatedLogicalPayload.HasValue, Is.False);
            Assert.That(texture.Width.Availability, Is.EqualTo(Availability.NotScanned));
            Assert.That(texture.Format.HasValue, Is.False);
        }

        [TestCase("=HYPERLINK(\"http://x\")", "'=HYPERLINK(\"\"http://x\"\")")]
        [TestCase("+cmd", "'+cmd")]
        [TestCase("-2+3", "'-2+3")]
        [TestCase("@SUM(A1)", "'@SUM(A1)")]
        public void Csv_text_cells_cannot_start_a_spreadsheet_formula(string displayName, string expectedCell)
        {
            var prefab = new PrefabRecord(new PrefabKey("Mod.Asset", "Prop"), displayName, PrefabTraits.Prop, new AssetOriginEvidence(isSubscribedMod: true));
            var report = Builder().Build(new[] { prefab }, 1, CapturedAt, null, Capabilities, "0.1.0", CapturedAt);
            var row = new CsvSummaryExporter().Export(report).Trim().Split(Environment.NewLine)[1];
            Assert.That(row, Does.Contain(expectedCell.Contains(",") ? "\"" + expectedCell + "\"" : expectedCell));
        }

        private static AuditReportBuilder Builder() => new AuditReportBuilder(new PrivacySanitizer("alice", "HOST-7"));

        private static (PrefabRecord Prefab, PrefabRecord Other, AssetAnalysisSnapshot Analysis) AnalysisWithOwnedFinding()
        {
            var prefab = new PrefabRecord(new PrefabKey("House.A", "Building"), "House A", PrefabTraits.Building, new AssetOriginEvidence(isBuiltin: true));
            // Same Prefab ID, different type: findings must not leak across Prefab keys.
            var other = new PrefabRecord(new PrefabKey("House.A", "Prop"), "House A prop", PrefabTraits.Prop, new AssetOriginEvidence(isBuiltin: true));
            var weakLod = new Finding("APA-LOD-002", FindingStatus.PotentialIssue, FindingCategory.Lod, "Weak LOD vertex reduction",
                "Heuristic evidence only.", new[] { "vertexRetentionPercent=95" }, FindingBasis.Heuristic, RuleSetInfo.Version);
            var analysis = new AssetAnalysisSnapshot(1, 5, 1, CapturedAt,
                new[] { Entry(prefab.Key, weakLod), Entry(other.Key) },
                Array.Empty<RenderAssetAnalysisRecord>());
            return (prefab, other, analysis);
        }

        private static PrefabAnalysisEntry Entry(PrefabKey key, params Finding[] findings) => new PrefabAnalysisEntry(
            key,
            RenderCoverage.Unknown,
            Array.Empty<PrefabRenderRelation>(),
            Observation<long>.Unavailable(Availability.Unsupported, ObservationOrigin.Derived, CapturedAt),
            Observation<double>.Unavailable(Availability.NotApplicable, ObservationOrigin.Derived, CapturedAt),
            Observation<long>.Unavailable(Availability.Unsupported, ObservationOrigin.Derived, CapturedAt),
            Observation<long>.Unavailable(Availability.Unsupported, ObservationOrigin.Derived, CapturedAt),
            Observation<long>.Unavailable(Availability.Unsupported, ObservationOrigin.Estimated, CapturedAt),
            findings);
    }
}
