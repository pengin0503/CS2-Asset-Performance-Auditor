using System;
using System.Collections.Generic;
using CS2AssetPerformanceAuditor.Core.Rendering;

namespace CS2AssetPerformanceAuditor.Core.Findings
{
    public sealed class GeometryFindingInput
    {
        public GeometryFindingInput(string assetId, string peerCategory, bool lowerLodPresent, bool requiredRenderReferenceBroken, long? lod0VertexCount, long? lod1VertexCount)
        {
            if (string.IsNullOrWhiteSpace(assetId)) throw new ArgumentException("Asset ID is required.", nameof(assetId));
            if (string.IsNullOrWhiteSpace(peerCategory)) throw new ArgumentException("Peer category is required.", nameof(peerCategory));
            AssetId = assetId; PeerCategory = peerCategory; LowerLodPresent = lowerLodPresent; RequiredRenderReferenceBroken = requiredRenderReferenceBroken;
            Lod0VertexCount = lod0VertexCount; Lod1VertexCount = lod1VertexCount;
        }
        public string AssetId { get; }
        public string PeerCategory { get; }
        public bool LowerLodPresent { get; }
        public bool RequiredRenderReferenceBroken { get; }
        public long? Lod0VertexCount { get; }
        public long? Lod1VertexCount { get; }
    }

    public sealed class FindingEngine
    {
        public IReadOnlyList<Finding> EvaluateGeometryLod(GeometryFindingInput input, DateTimeOffset capturedAt)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var findings = new List<Finding>();
            if (input.RequiredRenderReferenceBroken)
            {
                findings.Add(new Finding("APA-INT-001", FindingStatus.Warning, FindingCategory.Integrity,
                    "Required render reference is unresolved",
                    "A required render reference could not be resolved; this is structural evidence rather than a performance heuristic.",
                    new[] { "asset=" + input.AssetId }, FindingBasis.Deterministic, RuleSetInfo.Version));
            }

            if (!input.LowerLodPresent)
            {
                findings.Add(new Finding("APA-LOD-001", FindingStatus.Notice, FindingCategory.Lod,
                    "No lower LOD observed",
                    "No lower LOD was observed. This is an observation and is not automatically a performance defect.",
                    new[] { "asset=" + input.AssetId }, FindingBasis.Observation, RuleSetInfo.Version));
                return findings.AsReadOnly();
            }

            if (input.Lod0VertexCount.HasValue && input.Lod1VertexCount.HasValue)
            {
                var retention = LodMetrics.RetentionPercent(input.Lod0VertexCount.Value, input.Lod1VertexCount.Value, capturedAt);
                if (retention.HasValue && retention.Value >= RuleSetInfo.WeakLodRetentionPercent)
                {
                    findings.Add(new Finding("APA-LOD-002", FindingStatus.PotentialIssue, FindingCategory.Lod,
                        "Weak LOD vertex reduction",
                        "The lower LOD retains an unusually large share of LOD0 vertices under the versioned project heuristic; this does not prove a runtime bottleneck.",
                        new[] { "vertexRetentionPercent=" + retention.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture), "heuristicThresholdPercent=" + RuleSetInfo.WeakLodRetentionPercent.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) },
                        FindingBasis.Heuristic, RuleSetInfo.Version));
                }
            }

            return findings.AsReadOnly();
        }
    }
}
