#if CS2_GAME_API_AVAILABLE
using Game;
using Game.Modding;
using CS2AssetPerformanceAuditor.GameIntegration;
using CS2AssetPerformanceAuditor.UI;
#endif

namespace CS2AssetPerformanceAuditor
{
#if CS2_GAME_API_AVAILABLE
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
#else
    // This marker keeps the project buildable without local game assemblies. A distributable
    // mod build defines CS2_GAME_API_AVAILABLE by supplying CSII_MANAGEDPATH.
    public static class Mod
    {
        public const bool RequiresGameReferences = true;
    }
#endif
}
