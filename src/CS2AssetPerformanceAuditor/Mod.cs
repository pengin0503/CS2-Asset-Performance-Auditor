using Game;
using Game.Modding;
using CS2AssetPerformanceAuditor.GameIntegration;
using CS2AssetPerformanceAuditor.UI;

namespace CS2AssetPerformanceAuditor
{
    public sealed class Mod : IMod
    {
        private Setting? _setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            _setting = new Setting(this);
            _setting.RegisterInOptionsUI();

            updateSystem.UpdateAt<AssetAuditSystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAt<AssetAuditUISystem>(SystemUpdatePhase.UIUpdate);
        }

        public void OnDispose()
        {
            _setting?.UnregisterInOptionsUI();
            _setting = null;
        }
    }
}
