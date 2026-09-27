using System;
using System.Collections.Generic;
using CS2AssetPerformanceAuditor.Core.Capabilities;
using CS2AssetPerformanceAuditor.Core.Census;
using CS2AssetPerformanceAuditor.Core.Diagnostics;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using CS2AssetPerformanceAuditor.Core.Rendering;

namespace CS2AssetPerformanceAuditor.Export
{
    public static class AuditReportBuilderCurrentExtensions
    {
        public static AuditReport BuildCurrent(
            this AuditReportBuilder builder,
            IEnumerable<PrefabRecord> catalog,
            long catalogGeneration,
            DateTimeOffset? catalogCapturedAt,
            CensusSnapshot? census,
            AssetAnalysisSnapshot? analysis,
            CapabilityReport capabilities,
            string modVersion,
            DateTimeOffset generatedAt,
            IEnumerable<DiagnosticAggregate>? diagnostics = null,
            ExportScope scope = ExportScope.Full,
            IEnumerable<PrefabKey>? includedKeys = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var currentCensus = census != null && census.CatalogGeneration == catalogGeneration
                ? census
                : null;
            var currentAnalysis = analysis != null && analysis.CatalogGeneration == catalogGeneration
                ? analysis
                : null;

            return builder.Build(
                catalog,
                catalogGeneration,
                catalogCapturedAt,
                currentCensus,
                capabilities,
                modVersion,
                generatedAt,
                diagnostics,
                analysis: currentAnalysis,
                scope: scope,
                includedKeys: includedKeys);
        }
    }
}
