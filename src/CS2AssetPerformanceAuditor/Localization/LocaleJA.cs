using System.Collections.Generic;
using Colossal;

namespace CS2AssetPerformanceAuditor.Localization
{
    public sealed class LocaleJA : IDictionarySource
    {
        private readonly AuditorSetting _setting;

        public LocaleJA(AuditorSetting setting)
        {
            _setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { _setting.GetSettingsLocaleID(), "CS2 アセットパフォーマンス監査" },
                { _setting.GetOptionTabLocaleID(AuditorSetting.MainTab), "全般" },
                { _setting.GetOptionGroupLocaleID(AuditorSetting.ScanningGroup), "スキャン" },
                { _setting.GetOptionGroupLocaleID(AuditorSetting.AnalysisGroup), "分析" },
                { _setting.GetOptionGroupLocaleID(AuditorSetting.DisplayGroup), "表示" },
                { _setting.GetOptionGroupLocaleID(AuditorSetting.AdvancedGroup), "高度な設定" },

                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.CollectSubordinateObjects)), "従属オブジェクトを収集" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.CollectSubordinateObjects)), "対応する従属オブジェクトを次回の Census に含めます。無効時は 0 ではなく未スキャンとして扱います。" },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.CollectNetworkEdges)), "ネットワークエッジを収集" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.CollectNetworkEdges)), "対応するネットワークエッジの証拠を次回の Census に含めます。" },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.FrameBudgetMsOption)), "フレーム予算（ms）" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.FrameBudgetMsOption)), "監査処理を分割実行するときに 1 フレームで使用する管理処理時間の上限です。" },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.ProgressUpdateMs)), "進捗更新間隔（ms）" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.ProgressUpdateMs)), "スキャン中に進捗だけが変化した場合の UI 更新間隔です。" },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.RefreshCatalogAtScanStart)), "スキャン開始時にカタログを更新" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.RefreshCatalogAtScanStart)), "次回の Asset Audit 開始前に Prefab カタログを更新します。" },

                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.EnableHeuristicFindings)), "ヒューリスティック検出" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.EnableHeuristicFindings)), "バージョン管理された証拠ベースのヒューリスティック検出を有効にします。" },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.EnablePeerOutliers)), "同種アセット外れ値分析" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.EnablePeerOutliers)), "比較可能な母集団が十分にある場合、同種アセットとの比較を有効にします。" },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.ComparisonPopulationOption)), "比較対象" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.ComparisonPopulationOption)), "比較・外れ値分析に使用するアセット母集団を選択します。" },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.ShowNoticeFindings)), "Notice を表示" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.ShowNoticeFindings)), "Warnings 画面に情報レベルの Notice を表示します。" },

                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.UiScalePercent)), "監査 UI の倍率（%）" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.UiScalePercent)), "ゲーム内 Asset Performance Auditor パネルを 75～150% で拡大・縮小します。" },

                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.PageSize)), "アセットページサイズ" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.PageSize)), "UI が 1 回に要求するアセット件数です。" },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.MetadataCacheLimit)), "メタデータキャッシュ上限" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.MetadataCacheLimit)), "保持するメタデータキャッシュ件数の上限です。" },
                { _setting.GetOptionLabelLocaleID(nameof(AuditorSetting.DeepInspectionLimit)), "Deep Inspection 上限" },
                { _setting.GetOptionDescLocaleID(nameof(AuditorSetting.DeepInspectionLimit)), "Deep Inspection で扱う選択数の上限です。" },

                { _setting.GetEnumValueLocaleID(AuditorSetting.ComparisonPopulationChoice.SameCategory), "同一カテゴリ" },
                { _setting.GetEnumValueLocaleID(AuditorSetting.ComparisonPopulationChoice.BuiltinDlc), "バニラ / DLC" },
                { _setting.GetEnumValueLocaleID(AuditorSetting.ComparisonPopulationChoice.Custom), "カスタムアセット" },
                { _setting.GetEnumValueLocaleID(AuditorSetting.ComparisonPopulationChoice.SameSourcePack), "同一ソースパック" }
            };
        }

        public void Unload()
        {
        }
    }
}
