using CS2AssetPerformanceAuditor.Export;
using NUnit.Framework;

namespace CS2AssetPerformanceAuditor.Tests
{
    [TestFixture]
    public sealed class ReportCompatibilityTests
    {
        [Test]
        public void Schema_query_profile_and_material_scan_option_mismatches_warn_before_comparison()
        {
            var baseline = Header("1", "1", true, true);
            Assert.That(ReportCompatibility.Compare(baseline, Header("2", "1", true, true)).IsCompatible, Is.False);
            Assert.That(ReportCompatibility.Compare(baseline, Header("1", "2", true, true)).Warnings, Does.Contain("query-profile-mismatch"));
            Assert.That(ReportCompatibility.Compare(baseline, Header("1", "1", false, true)).Warnings, Does.Contain("scan-options-mismatch"));
        }

        [Test]
        public void Additive_analysis_content_does_not_change_comparison_compatibility()
        {
            var left = Header("1", "1", true, true);
            var right = Header("1", "1", true, true);
            right.Analysis = new ReportAnalysis { Findings = new[] { new ReportFinding { RuleId = "APA-LOD-001" } } };
            Assert.That(ReportCompatibility.Compare(left, right).IsCompatible, Is.True);
        }

        private static AuditReport Header(string schema, string profile, bool subordinate, bool network)
        {
            return new AuditReport
            {
                SchemaVersion = schema,
                QueryProfileVersion = profile,
                ScanOptions = new ReportScanOptions { WasCensusScanned = true, CollectSubordinateObjects = subordinate, CollectNetworkEdges = network }
            };
        }
    }
}
