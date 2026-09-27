using Colossal.IO.AssetDatabase;
using Game.Modding;

namespace CS2AssetPerformanceAuditor
{
    // Persisted backing store for the in-panel Settings tab. The type name is what
    // AssetDatabase.SaveSpecificSetting matches when saving, so it is deliberately distinct from the common "Setting".
    // It is not registered in the game's Options UI: the Options generator only renders bool/int/float/string
    // properties with specific attributes, and the panel is the single place these values are edited.
    [FileLocation(nameof(CS2AssetPerformanceAuditor))]
    public sealed class AuditorSetting : ModSetting
    {
        public AuditorSetting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public bool CollectSubordinateObjects { get; set; }
        public bool CollectNetworkEdges { get; set; }
        public double FrameBudgetMs { get; set; }
        public int ProgressUpdateMs { get; set; }
        public bool RefreshCatalogAtScanStart { get; set; }
        public bool EnableHeuristicFindings { get; set; }
        public bool EnablePeerOutliers { get; set; }
        public string ComparisonPopulation { get; set; } = "SameCategory";
        public bool ShowNoticeFindings { get; set; }
        public int PageSize { get; set; }
        public int MetadataCacheLimit { get; set; }
        public int DeepInspectionLimit { get; set; }
        public double UiScale { get; set; }

        public override void SetDefaults()
        {
            CollectSubordinateObjects = true;
            CollectNetworkEdges = true;
            FrameBudgetMs = 1.0;
            ProgressUpdateMs = 200;
            RefreshCatalogAtScanStart = true;
            EnableHeuristicFindings = true;
            EnablePeerOutliers = true;
            ComparisonPopulation = "SameCategory";
            ShowNoticeFindings = true;
            PageSize = 100;
            MetadataCacheLimit = 512;
            DeepInspectionLimit = 1;
            UiScale = 1.0;
        }
    }
}
