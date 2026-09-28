using Colossal.IO.AssetDatabase;
using CS2AssetPerformanceAuditor.GameIntegration;
using CS2AssetPerformanceAuditor.Localization;
using CS2AssetPerformanceAuditor.UI;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace CS2AssetPerformanceAuditor
{
    public sealed class Mod : IMod
    {
        // Loaded before the systems are created so the UI system can seed its settings from it.
        internal static AuditorSetting? Settings { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            var settings = new AuditorSetting(this);
            Settings = settings;

            var localizationManager = GameManager.instance?.localizationManager;
            if (localizationManager != null)
            {
                localizationManager.AddSource("en-US", new LocaleEN(settings));
                localizationManager.AddSource("ja-JP", new LocaleJA(settings));
            }

            AssetDatabase.global.LoadSettings(nameof(CS2AssetPerformanceAuditor), settings, new AuditorSetting(this));
            settings.RegisterInOptionsUI();

            updateSystem.UpdateAt<AssetAuditSystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAt<AssetAuditUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<AssetAuditSettingsSyncSystem>(SystemUpdatePhase.UIUpdate);
        }

        public void OnDispose()
        {
            Settings?.UnregisterInOptionsUI();
            Settings = null;
        }
    }
}
