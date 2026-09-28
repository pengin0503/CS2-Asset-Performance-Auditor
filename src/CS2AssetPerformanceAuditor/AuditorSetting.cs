using System;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace CS2AssetPerformanceAuditor
{
    // Shared persisted backing store for both the vanilla Options page and the in-panel Settings tab.
    // Double/string values that the automatic Options generator cannot represent directly are exposed
    // through small UI proxy properties while the existing serialized contract remains unchanged.
    [FileLocation(nameof(CS2AssetPerformanceAuditor))]
    [SettingsUITabOrder(MainTab)]
    [SettingsUIGroupOrder(ScanningGroup, AnalysisGroup, DisplayGroup, AdvancedGroup)]
    [SettingsUIShowGroupName(ScanningGroup, AnalysisGroup, DisplayGroup, AdvancedGroup)]
    public sealed class AuditorSetting : ModSetting
    {
        public const string MainTab = "Main";
        public const string ScanningGroup = "Scanning";
        public const string AnalysisGroup = "Analysis";
        public const string DisplayGroup = "Display";
        public const string AdvancedGroup = "Advanced";

        public enum ComparisonPopulationChoice
        {
            SameCategory,
            BuiltinDlc,
            Custom,
            SameSourcePack
        }

        public AuditorSetting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        [SettingsUISection(MainTab, ScanningGroup)]
        public bool CollectSubordinateObjects { get; set; }

        [SettingsUISection(MainTab, ScanningGroup)]
        public bool CollectNetworkEdges { get; set; }

        [SettingsUIHidden]
        public double FrameBudgetMs { get; set; }

        [SettingsUISection(MainTab, ScanningGroup)]
        [SettingsUISlider(min = 0.25f, max = 8f, step = 0.25f, scalarMultiplier = 1)]
        public float FrameBudgetMsOption
        {
            get => (float)FrameBudgetMs;
            set => FrameBudgetMs = value;
        }

        [SettingsUISection(MainTab, ScanningGroup)]
        [SettingsUISlider(min = 50, max = 2000, step = 50, scalarMultiplier = 1)]
        public int ProgressUpdateMs { get; set; }

        [SettingsUISection(MainTab, ScanningGroup)]
        public bool RefreshCatalogAtScanStart { get; set; }

        [SettingsUISection(MainTab, AnalysisGroup)]
        public bool EnableHeuristicFindings { get; set; }

        [SettingsUISection(MainTab, AnalysisGroup)]
        public bool EnablePeerOutliers { get; set; }

        [SettingsUIHidden]
        public string ComparisonPopulation { get; set; } = "SameCategory";

        [SettingsUISection(MainTab, AnalysisGroup)]
        public ComparisonPopulationChoice ComparisonPopulationOption
        {
            get => ParseComparisonPopulation(ComparisonPopulation);
            set => ComparisonPopulation = value.ToString();
        }

        [SettingsUISection(MainTab, AnalysisGroup)]
        public bool ShowNoticeFindings { get; set; }

        [SettingsUISection(MainTab, AdvancedGroup)]
        [SettingsUIAdvanced]
        [SettingsUISlider(min = 25, max = 200, step = 25, scalarMultiplier = 1)]
        public int PageSize { get; set; }

        [SettingsUISection(MainTab, AdvancedGroup)]
        [SettingsUIAdvanced]
        [SettingsUISlider(min = 64, max = 4096, step = 64, scalarMultiplier = 1)]
        public int MetadataCacheLimit { get; set; }

        [SettingsUISection(MainTab, AdvancedGroup)]
        [SettingsUIAdvanced]
        [SettingsUISlider(min = 1, max = 16, step = 1, scalarMultiplier = 1)]
        public int DeepInspectionLimit { get; set; }

        [SettingsUIHidden]
        public double UiScale { get; set; }

        [SettingsUISection(MainTab, DisplayGroup)]
        [SettingsUISlider(min = 75, max = 150, step = 5, scalarMultiplier = 1)]
        public int UiScalePercent
        {
            get => (int)Math.Round(UiScale * 100.0);
            set => UiScale = value / 100.0;
        }

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

        private static ComparisonPopulationChoice ParseComparisonPopulation(string? value)
        {
            return value switch
            {
                "BuiltinDlc" => ComparisonPopulationChoice.BuiltinDlc,
                "Custom" => ComparisonPopulationChoice.Custom,
                "SameSourcePack" => ComparisonPopulationChoice.SameSourcePack,
                _ => ComparisonPopulationChoice.SameCategory
            };
        }
    }
}
