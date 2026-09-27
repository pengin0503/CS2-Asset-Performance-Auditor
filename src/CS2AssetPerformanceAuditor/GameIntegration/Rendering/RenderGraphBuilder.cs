using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using CS2AssetPerformanceAuditor.Core.Rendering;

namespace CS2AssetPerformanceAuditor.GameIntegration.Rendering
{
    public sealed class RenderGraphSnapshot
    {
        private readonly IReadOnlyDictionary<PrefabKey, RenderCoverage> _coverage;
        private readonly IReadOnlyDictionary<RenderAssetKey, object> _runtimeAssets;

        internal RenderGraphSnapshot(
            IDictionary<PrefabKey, RenderCoverage> coverage,
            IEnumerable<RenderAssetRecord> renderAssets,
            IEnumerable<PrefabRenderRelation> relations,
            IDictionary<RenderAssetKey, object>? runtimeAssets = null)
        {
            _coverage = new ReadOnlyDictionary<PrefabKey, RenderCoverage>(new Dictionary<PrefabKey, RenderCoverage>(coverage));
            _runtimeAssets = new ReadOnlyDictionary<RenderAssetKey, object>(new Dictionary<RenderAssetKey, object>(runtimeAssets ?? new Dictionary<RenderAssetKey, object>()));
            RenderAssets = Array.AsReadOnly(renderAssets.ToArray());
            Relations = Array.AsReadOnly(relations.ToArray());
        }

        public IReadOnlyList<RenderAssetRecord> RenderAssets { get; }
        public IReadOnlyList<PrefabRenderRelation> Relations { get; }
        public int RuntimeAssetCount => _runtimeAssets.Count;
        public bool TryGetCoverage(PrefabKey prefabKey, out RenderCoverage coverage) => _coverage.TryGetValue(prefabKey, out coverage);
        public bool TryGetRuntimeAsset(RenderAssetKey key, out object runtimeAsset) => _runtimeAssets.TryGetValue(key, out runtimeAsset!);
    }

    public sealed class RenderGraphBuilder
    {
        private readonly IReadOnlyList<IRenderAssetResolver> _resolvers;
        public RenderGraphBuilder(IEnumerable<IRenderAssetResolver> resolvers)
        {
            if (resolvers == null) throw new ArgumentNullException(nameof(resolvers));
            _resolvers = Array.AsReadOnly(resolvers.ToArray());
        }

        public RenderGraphSnapshot Build(IEnumerable<RenderGraphInput> inputs)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            var coverage = new Dictionary<PrefabKey, RenderCoverage>();
            var assets = new Dictionary<RenderAssetKey, RenderAssetRecord>();
            var runtimeAssets = new Dictionary<RenderAssetKey, object>();
            var relations = new List<PrefabRenderRelation>();
            var relationKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var input in inputs)
            {
                if (input == null) throw new ArgumentException("Render graph inputs cannot contain null entries.", nameof(inputs));
                var resolver = _resolvers.FirstOrDefault(candidate => candidate.CanResolve(input));
                if (resolver == null)
                {
                    coverage[input.Prefab.Key] = RenderCoverage.Unknown;
                    continue;
                }

                RenderResolution resolution;
                try
                {
                    resolution = resolver.Resolve(input) ?? throw new InvalidOperationException("A render resolver returned no resolution.");
                }
                catch
                {
                    coverage[input.Prefab.Key] = RenderCoverage.Failed;
                    continue;
                }

                coverage[input.Prefab.Key] = resolution.Coverage;
                foreach (var asset in resolution.RenderAssets)
                    if (!assets.ContainsKey(asset.Key)) assets.Add(asset.Key, asset);
                foreach (var binding in resolution.RuntimeAssets)
                    if (!runtimeAssets.ContainsKey(binding.Key)) runtimeAssets.Add(binding.Key, binding.RuntimeAsset);

                foreach (var relation in resolution.Relations)
                {
                    if (relation.PrefabKey != input.Prefab.Key)
                    {
                        coverage[input.Prefab.Key] = RenderCoverage.Failed;
                        continue;
                    }
                    var relationKey = relation.PrefabKey + "|" + relation.RenderAssetKey + "|" + relation.RelationKind + "|" + (relation.LodLevel?.ToString() ?? string.Empty);
                    if (relationKeys.Add(relationKey)) relations.Add(relation);
                }
            }

            return new RenderGraphSnapshot(
                coverage,
                assets.Values.OrderBy(asset => asset.Key.RenderAssetType, StringComparer.Ordinal).ThenBy(asset => asset.Key.RenderAssetId, StringComparer.Ordinal),
                relations.OrderBy(relation => relation.PrefabKey.PrefabType, StringComparer.Ordinal)
                    .ThenBy(relation => relation.PrefabKey.PrefabId, StringComparer.Ordinal)
                    .ThenBy(relation => relation.LodLevel ?? -1)
                    .ThenBy(relation => relation.RelationKind)
                    .ThenBy(relation => relation.RenderAssetKey.RenderAssetId, StringComparer.Ordinal),
                runtimeAssets);
        }
    }
}
