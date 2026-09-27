using System;
using System.Collections.Generic;
using System.Linq;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using CS2AssetPerformanceAuditor.Core.Rendering;

namespace CS2AssetPerformanceAuditor.GameIntegration.Rendering
{
    public sealed class RenderGraphInput
    {
        public RenderGraphInput(PrefabRecord prefab, object runtimePrefab)
        {
            Prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            RuntimePrefab = runtimePrefab ?? throw new ArgumentNullException(nameof(runtimePrefab));
        }
        public PrefabRecord Prefab { get; }
        public object RuntimePrefab { get; }
    }

    public sealed class RenderResolution
    {
        public RenderResolution(RenderCoverage coverage, IEnumerable<RenderAssetRecord>? renderAssets = null, IEnumerable<PrefabRenderRelation>? relations = null, string? diagnosticCode = null)
        {
            if (!Enum.IsDefined(typeof(RenderCoverage), coverage)) throw new ArgumentOutOfRangeException(nameof(coverage));
            Coverage = coverage;
            RenderAssets = Array.AsReadOnly((renderAssets ?? Array.Empty<RenderAssetRecord>()).ToArray());
            Relations = Array.AsReadOnly((relations ?? Array.Empty<PrefabRenderRelation>()).ToArray());
            DiagnosticCode = diagnosticCode;
        }
        public RenderCoverage Coverage { get; }
        public IReadOnlyList<RenderAssetRecord> RenderAssets { get; }
        public IReadOnlyList<PrefabRenderRelation> Relations { get; }
        public string? DiagnosticCode { get; }
    }

    public interface IRenderAssetResolver
    {
        bool CanResolve(RenderGraphInput input);
        RenderResolution Resolve(RenderGraphInput input);
    }
}
