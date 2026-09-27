using System;
using System.Collections.Generic;
using System.Linq;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using CS2AssetPerformanceAuditor.Core.Rendering;
using CS2AssetPerformanceAuditor.GameIntegration.Rendering;
using NUnit.Framework;

namespace CS2AssetPerformanceAuditor.Tests
{
    [TestFixture]
    public sealed class RenderGraphTests
    {
        [Test]
        public void Unsupported_family_is_unknown_without_synthetic_zero_geometry_resource()
        {
            var network = Record("Road.Small", PrefabTraits.Network);
            var graph = new RenderGraphBuilder(Array.Empty<IRenderAssetResolver>())
                .Build(new[] { new RenderGraphInput(network, new object()) });

            Assert.That(graph.TryGetCoverage(network.Key, out var coverage), Is.True);
            Assert.That(coverage, Is.EqualTo(RenderCoverage.Unknown));
            Assert.That(graph.RenderAssets, Is.Empty);
            Assert.That(graph.Relations, Is.Empty);
        }

        [Test]
        public void Shared_render_asset_is_deduplicated_across_prefabs()
        {
            var houseA = Record("House.A", PrefabTraits.Building);
            var houseB = Record("House.B", PrefabTraits.Building);
            var sharedKey = new RenderAssetKey("Render.SharedHouse", "RenderPrefab");
            var resolver = new FakeResolver(input => Supported(
                new RenderAssetRecord(sharedKey, "Shared House Mesh"),
                new PrefabRenderRelation(input.Prefab.Key, sharedKey, RenderRelationKind.DirectMesh)));

            var graph = new RenderGraphBuilder(new[] { resolver }).Build(new[]
            {
                new RenderGraphInput(houseA, new object()),
                new RenderGraphInput(houseB, new object())
            });

            Assert.That(graph.RenderAssets.Select(asset => asset.Key), Is.EqualTo(new[] { sharedKey }));
            Assert.That(graph.Relations.Count, Is.EqualTo(2));
            Assert.That(graph.Relations.Select(relation => relation.PrefabKey), Is.EquivalentTo(new[] { houseA.Key, houseB.Key }));
        }

        [Test]
        public void Runtime_render_asset_binding_is_deduplicated_by_stable_render_key()
        {
            var houseA = Record("House.A", PrefabTraits.Building);
            var houseB = Record("House.B", PrefabTraits.Building);
            var sharedKey = new RenderAssetKey("Render.SharedHouse", "RenderPrefab");
            var runtimeRender = new object();
            var resolver = new FakeResolver(input => new RenderResolution(
                RenderCoverage.Supported,
                new[] { new RenderAssetRecord(sharedKey, "Shared House Mesh") },
                new[] { new PrefabRenderRelation(input.Prefab.Key, sharedKey, RenderRelationKind.DirectMesh) },
                runtimeAssets: new[] { new RuntimeRenderAssetBinding(sharedKey, runtimeRender) }));

            var graph = new RenderGraphBuilder(new[] { resolver }).Build(new[]
            {
                new RenderGraphInput(houseA, new object()),
                new RenderGraphInput(houseB, new object())
            });

            Assert.That(graph.RuntimeAssetCount, Is.EqualTo(1));
            Assert.That(graph.TryGetRuntimeAsset(sharedKey, out var resolved), Is.True);
            Assert.That(resolved, Is.SameAs(runtimeRender));
        }

        [Test]
        public void Lod_relation_kind_remains_attributable()
        {
            var tree = Record("Tree.Oak", PrefabTraits.Tree);
            var lodKey = new RenderAssetKey("Render.Oak.LOD1", "RenderPrefab");
            var resolver = new FakeResolver(input => Supported(
                new RenderAssetRecord(lodKey, "Oak LOD1"),
                new PrefabRenderRelation(input.Prefab.Key, lodKey, RenderRelationKind.Lod, lodLevel: 1)));

            var graph = new RenderGraphBuilder(new[] { resolver })
                .Build(new[] { new RenderGraphInput(tree, new object()) });

            var relation = graph.Relations.Single();
            Assert.That(relation.RelationKind, Is.EqualTo(RenderRelationKind.Lod));
            Assert.That(relation.LodLevel, Is.EqualTo(1));
        }

        private static RenderResolution Supported(RenderAssetRecord asset, PrefabRenderRelation relation)
        {
            return new RenderResolution(RenderCoverage.Supported, new[] { asset }, new[] { relation });
        }

        private static PrefabRecord Record(string id, PrefabTraits traits)
        {
            return new PrefabRecord(new PrefabKey(id, traits.ToString()), id, traits, new AssetOriginEvidence());
        }

        private sealed class FakeResolver : IRenderAssetResolver
        {
            private readonly Func<RenderGraphInput, RenderResolution> _resolve;

            public FakeResolver(Func<RenderGraphInput, RenderResolution> resolve)
            {
                _resolve = resolve;
            }

            public bool CanResolve(RenderGraphInput input) => true;

            public RenderResolution Resolve(RenderGraphInput input) => _resolve(input);
        }
    }
}
