using System.Collections.Generic;
using Colossal;

namespace CS2AssetPerformanceAuditor.Localization
{
    public sealed class LocaleEN : IDictionarySource
    {
        private readonly AuditorSetting _setting;

        public LocaleEN(AuditorSetting setting)
        {
            _setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { _setting.GetSettingsLocaleID(), "CS2 Asset Performance Auditor" },
                { _setting.GetOptionTabLocaleID(AuditorSetting.MainTab), "General" },
                { _setting.GetOptionGroupLocaleID(AuditorSetting.ScanningGroup), "Scanning" },
                { _setting.GetOptionGroupLocaleID(AuditorSetting.AnalysisGroup), "Analysis" },
                { _setting.GetOptionGroupLocaleID(AuditorSetting.DisplayGroup), "Display" },
                { _setting.GetOptionGroupLocaleID(AuditorSetting.AdvancedGroup), "Advanced" },

                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.CollectSubordinateObjects)), "Collect subordinate objects" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.CollectSubordinateObjects)), "Include supported subordinate objects in the next census. Disabled collection is reported as not scanned, not zero." },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.CollectNetworkEdges)), "Collect network edges" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.CollectNetworkEdges)), "Include supported network-edge evidence in the next census." },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.FrameBudgetMsOption)), "Managed frame budget (ms)" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.FrameBudgetMsOption)), "Maximum managed work budget used by bounded audit slices each frame." },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.ProgressUpdateMs)), "Progress update interval (ms)" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.ProgressUpdateMs)), "Minimum interval between progress-only UI snapshot updates while a scan is active." },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.RefreshCatalogAtScanStart)), "Refresh catalog at scan start" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.RefreshCatalogAtScanStart)), "Refresh the prefab catalog before the next Asset Audit starts." },

                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.EnableHeuristicFindings)), "Heuristic findings" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.EnableHeuristicFindings)), "Enable versioned, evidence-based heuristic findings in Asset Audit results." },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.EnablePeerOutliers)), "Peer-outlier analysis" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.EnablePeerOutliers)), "Enable peer comparison when a sufficient comparable population is available." },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.ComparisonPopulationOption)), "Comparison population" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.ComparisonPopulationOption)), "Choose the peer population used for comparison and outlier analysis." },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.ShowNoticeFindings)), "Show Notice findings" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.ShowNoticeFindings)), "Include informational Notice findings in the Warnings view." },

                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.UiScalePercent)), "Auditor UI scale (%)" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.UiScalePercent)), "Scale the in-game Asset Performance Auditor panel from 75% to 150%." },

                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.PageSize)), "Asset page size" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.PageSize)), "Number of assets requested per bounded UI page." },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.MetadataCacheLimit)), "Metadata cache limit" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.MetadataCacheLimit)), "Maximum number of metadata entries retained by the bounded metadata cache." },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.DeepInspectionLimit)), "Deep Inspection limit" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.DeepInspectionLimit)), "Maximum bounded Deep Inspection selection limit." },

                { _setting.GetEnumValueLocaleID(AuditorSetting.ComparisonPopulationChoice.SameCategory), "Same category" },
                { _setting.GetEnumValueLocaleID(AuditorSetting.ComparisonPopulationChoice.BuiltinDlc), "Vanilla / DLC" },
                { _setting.GetEnumValueLocaleID(AuditorSetting.ComparisonPopulationChoice.Custom), "Custom assets" },
                { _setting.GetEnumValueLocaleID(AuditorSetting.ComparisonPopulationChoice.SameSourcePack), "Same source pack" }
            };
        }

        public void Unload()
        {
        }
    }
}
