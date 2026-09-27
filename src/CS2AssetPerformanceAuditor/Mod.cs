using Colossal.IO.AssetDatabase;
using Game;
using Game.Modding;
using CS2AssetPerformanceAuditor.GameIntegration;
using CS2AssetPerformanceAuditor.UI;

namespace CS2AssetPerformanceAuditor
{
    public sealed class Mod : IMod
    {
        // Loaded before the systems are created so the UI system can seed its settings from it.
        internal static AuditorSetting? Settings { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            var settings = new AuditorSetting(this);
            AssetDatabase.global.LoadSettings(nameof(CS2AssetPerformanceAuditor), settings, new AuditorSetting(this));
            Settings = settings;

            updateSystem.UpdateAt<AssetAuditSystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAt<AssetAuditUISystem>(SystemUpdatePhase.UIUpdate);
        }

        public void OnDispose()
        {
            Settings = null;
        }
    }
}
