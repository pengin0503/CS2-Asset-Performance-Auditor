using NUnit.Framework;
using CS2AssetPerformanceAuditor.Core.Prefabs;

namespace CS2AssetPerformanceAuditor.Tests
{
    [TestFixture]
    public sealed class PrefabProjectionTests
    {
        [Test]
        public void Prefab_key_uses_only_stable_prefab_identity()
        {
            var first = new PrefabKey("Road.Small", "NetworkPrefab");
            var samePrefabFromAnotherRuntimeWorld = new PrefabKey("Road.Small", "NetworkPrefab");

            Assert.That(first, Is.EqualTo(samePrefabFromAnotherRuntimeWorld));
            Assert.That(first.PrefabId, Is.EqualTo("Road.Small"));
            Assert.That(first.PrefabType, Is.EqualTo("NetworkPrefab"));
        }

        [Test]
        public void Source_evidence_preserves_overlapping_origin_facts()
        {
            var evidence = new AssetOriginEvidence(
                isBuiltin: true,
                isSubscribedMod: true,
                isPackaged: true,
                dlcPrerequisiteIds: new[] { "dlc-a" },
                assetPackMembership: new[] { "regional-pack" },
                assetDatabaseSource: "asset-database",
                paradoxModsPlatformId: "12345");

            Assert.That(evidence.IsBuiltin, Is.True);
            Assert.That(evidence.IsSubscribedMod, Is.True);
            Assert.That(evidence.IsPackaged, Is.True);
            Assert.That(evidence.DlcPrerequisiteIds, Is.EquivalentTo(new[] { "dlc-a" }));
            Assert.That(evidence.AssetPackMembership, Is.EquivalentTo(new[] { "regional-pack" }));
            Assert.That(evidence.AssetDatabaseSource, Is.EqualTo("asset-database"));
            Assert.That(evidence.ParadoxModsPlatformId, Is.EqualTo("12345"));
        }

        [Test]
        public void Prefab_traits_can_represent_multiple_classifications()
        {
            var traits = PrefabTraits.Building | PrefabTraits.ServiceBuilding | PrefabTraits.RenderOnly;

            Assert.That(traits.HasFlag(PrefabTraits.Building), Is.True);
            Assert.That(traits.HasFlag(PrefabTraits.ServiceBuilding), Is.True);
            Assert.That(traits.HasFlag(PrefabTraits.RenderOnly), Is.True);
            Assert.That(traits.HasFlag(PrefabTraits.Vehicle), Is.False);
        }

        [Test]
        public void Prefab_record_keeps_stable_identity_and_source_evidence()
        {
            var evidence = new AssetOriginEvidence(isBuiltin: true);
            var key = new PrefabKey("Building.FireStation", "BuildingPrefab");
            var record = new PrefabRecord(key, "Fire Station", PrefabTraits.Building | PrefabTraits.ServiceBuilding, evidence);

            Assert.That(record.Key, Is.EqualTo(key));
            Assert.That(record.DisplayName, Is.EqualTo("Fire Station"));
            Assert.That(record.Traits.HasFlag(PrefabTraits.Building), Is.True);
            Assert.That(record.OriginEvidence, Is.SameAs(evidence));
        }
    }
}
