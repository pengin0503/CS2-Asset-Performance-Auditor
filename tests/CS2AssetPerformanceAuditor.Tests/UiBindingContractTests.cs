using CS2AssetPerformanceAuditor.Core.Rendering;
using CS2AssetPerformanceAuditor.UI;
using NUnit.Framework;

namespace CS2AssetPerformanceAuditor.Tests
{
    [TestFixture]
    public sealed class UiBindingContractTests
    {
        [Test]
        public void Analysis_and_deep_inspection_triggers_have_stable_contract_names()
        {
            Assert.That(UiBindingContract.RequestAssetAudit, Is.EqualTo("requestAssetAudit"));
            Assert.That(UiBindingContract.RequestDeepInspection, Is.EqualTo("requestDeepInspection"));
        }

        [Test]
        public void Render_asset_key_round_trips_ui_binding_text()
        {
            var key = new RenderAssetKey("Render:House.A", "Game.Prefabs.RenderPrefab");

            Assert.That(RenderAssetKey.TryParse(key.ToString(), out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(key));
            Assert.That(RenderAssetKey.TryParse("invalid", out _), Is.False);
        }
    }
}
