using System;
using System.Linq;
using CS2AssetPerformanceAuditor.Core.Findings;
using CS2AssetPerformanceAuditor.Core.Observations;
using CS2AssetPerformanceAuditor.Core.Rendering;
using NUnit.Framework;

namespace CS2AssetPerformanceAuditor.Tests
{
    [TestFixture]
    public sealed class FindingEngineTests
    {
        private static readonly DateTimeOffset CapturedAt = new DateTimeOffset(2026, 9, 27, 7, 5, 0, TimeSpan.Zero);

        [Test]
        public void Lod_retention_math_is_exact_and_deterministic()
        {
            var retention = LodMetrics.RetentionPercent(5000, 4199, CapturedAt);
            var reduction = LodMetrics.ReductionPercent(5000, 4199, CapturedAt);

            Assert.That(retention.Availability, Is.EqualTo(Availability.Available));
            Assert.That(retention.Value, Is.EqualTo(83.98d).Within(0.0000001));
            Assert.That(reduction.Value, Is.EqualTo(16.02d).Within(0.0000001));
        }

        [Test]
        public void Missing_lower_lod_is_notice_not_warning()
        {
            var findings = new FindingEngine().EvaluateGeometryLod(new GeometryFindingInput(
                assetId: "house",
                peerCategory: "Building",
                lowerLodPresent: false,
                requiredRenderReferenceBroken: false,
                lod0VertexCount: null,
                lod1VertexCount: null), CapturedAt);

            Assert.That(findings.Any(f => f.RuleId == "APA-LOD-001" && f.Status == FindingStatus.Notice), Is.True);
            Assert.That(findings.Any(f => f.Status == FindingStatus.Warning), Is.False);
        }

        [Test]
        public void Weak_lod_reduction_is_heuristic_potential_issue()
        {
            var findings = new FindingEngine().EvaluateGeometryLod(new GeometryFindingInput(
                "house", "Building", true, false, 1000, 920), CapturedAt);
            var finding = findings.Single(f => f.RuleId == "APA-LOD-002");

            Assert.That(finding.Status, Is.EqualTo(FindingStatus.PotentialIssue));
            Assert.That(finding.Basis, Is.EqualTo(FindingBasis.Heuristic));
            Assert.That(finding.RuleVersion, Is.EqualTo(RuleSetInfo.Version));
            Assert.That(finding.Evidence.Any(e => e.Contains("92")), Is.True);
        }

        [Test]
        public void Broken_required_render_reference_may_be_warning()
        {
            var findings = new FindingEngine().EvaluateGeometryLod(new GeometryFindingInput(
                "broken", "Building", false, true, null, null), CapturedAt);

            var finding = findings.Single(f => f.RuleId == "APA-INT-001");
            Assert.That(finding.Status, Is.EqualTo(FindingStatus.Warning));
            Assert.That(finding.Basis, Is.EqualTo(FindingBasis.Deterministic));
        }

        [Test]
        public void Missing_metric_evidence_does_not_fabricate_heuristic_finding()
        {
            var findings = new FindingEngine().EvaluateGeometryLod(new GeometryFindingInput(
                "unknown", "Building", true, false, lod0VertexCount: null, lod1VertexCount: null), CapturedAt);

            Assert.That(findings.Any(f => f.RuleId == "APA-LOD-002"), Is.False);
        }
    }
}
