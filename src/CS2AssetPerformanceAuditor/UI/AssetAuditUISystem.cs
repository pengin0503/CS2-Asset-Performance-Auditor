using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.UI.Binding;
using CS2AssetPerformanceAuditor.Core;
using CS2AssetPerformanceAuditor.Core.Census;
using CS2AssetPerformanceAuditor.Core.Diagnostics;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using CS2AssetPerformanceAuditor.Core.Query;
using CS2AssetPerformanceAuditor.Core.Rendering;
using CS2AssetPerformanceAuditor.Export;
using CS2AssetPerformanceAuditor.GameIntegration;
using Game.UI;
using Unity.Entities;

namespace CS2AssetPerformanceAuditor.UI
{
    public sealed partial class AssetAuditUISystem : UISystemBase
    {
        private readonly UiSnapshotBuilder _snapshotBuilder = new UiSnapshotBuilder();
        private readonly DiagnosticAggregator _diagnostics = new DiagnosticAggregator();
        private readonly AuditReportSerializer _reportSerializer = new AuditReportSerializer();
        private readonly AuditReportBuilder _reportBuilder = new AuditReportBuilder(new PrivacySanitizer());
        private readonly CsvSummaryExporter _csvExporter = new CsvSummaryExporter();

        private ValueBinding<string>? _snapshotBinding;
        private ValueBinding<string>? _exportBinding;
        private AssetAuditSystem? _lastAuditSystem;
        private CensusSnapshot? _lastCensus;
        private AssetAnalysisSnapshot? _lastAnalysis;
        private long _lastCatalogGeneration = -1;
        private AssetQuery _assetQuery = new AssetQuery();
        private AssetPage _assetPage = new AssetPage(Array.Empty<AssetPageItem>(), 0, 0, 100);
        private UiScanOptions _uiSettings = new UiScanOptions();
        private string? _lastSnapshotJson;
        private DateTimeOffset _lastPublishedAt;

        protected override void OnCreate()
        {
            base.OnCreate();
            _snapshotBinding = new ValueBinding<string>(UiBindingContract.Group, UiBindingContract.Snapshot, "{}");
            _exportBinding = new ValueBinding<string>(UiBindingContract.Group, UiBindingContract.ExportedReport, string.Empty);
            AddBinding(_snapshotBinding);
            AddBinding(_exportBinding);
            AddBinding(new TriggerBinding<string>(UiBindingContract.Group, UiBindingContract.RequestCensus, HandleRequestCensus, new Colossal.UI.Binding.StringReader()));
            AddBinding(new TriggerBinding<string>(UiBindingContract.Group, UiBindingContract.RequestAssetAudit, HandleRequestAssetAudit, new Colossal.UI.Binding.StringReader()));
            AddBinding(new TriggerBinding<string>(UiBindingContract.Group, UiBindingContract.RequestDeepInspection, HandleRequestDeepInspection, new Colossal.UI.Binding.StringReader()));
            AddBinding(new TriggerBinding(UiBindingContract.Group, UiBindingContract.CancelCensus, HandleCancelCurrentScan));
            AddBinding(new TriggerBinding<string>(UiBindingContract.Group, UiBindingContract.QueryAssets, HandleQueryAssets, new Colossal.UI.Binding.StringReader()));
            AddBinding(new TriggerBinding<string>(UiBindingContract.Group, UiBindingContract.RequestExport, HandleRequestExport, new Colossal.UI.Binding.StringReader()));
            AddBinding(new TriggerBinding<string>(UiBindingContract.Group, UiBindingContract.UpdateSettings, HandleUpdateSettings, new Colossal.UI.Binding.StringReader()));
            RefreshAssetPage(GetAuditSystem(), force: true);
            PublishSnapshot(force: true);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            var auditSystem = GetAuditSystem();
            var dataChanged = RefreshAssetPage(auditSystem, force: false);
            var now = DateTimeOffset.UtcNow;
            var publishInterval = TimeSpan.FromMilliseconds(_uiSettings.ProgressUpdateMs);
            if (dataChanged || now - _lastPublishedAt >= publishInterval)
                PublishSnapshot(force: dataChanged);
        }

        protected override void OnDestroy()
        {
            _snapshotBinding = null;
            _exportBinding = null;
            _lastAuditSystem = null;
            _lastCensus = null;
            _lastAnalysis = null;
            base.OnDestroy();
        }

        private AssetAuditSystem? GetAuditSystem()
        {
            if (!World.IsCreated)
                return null;
            return World.GetExistingSystemManaged<AssetAuditSystem>();
        }

        private void HandleRequestCensus(string optionsJson)
        {
            try
            {
                if (UiSnapshotBuilder.TryDeserialize<UiScanOptions>(optionsJson, out var options))
                    _uiSettings = NormalizeSettings(options);
                var scanOptions = new ScanOptions(_uiSettings.CollectSubordinateObjects, _uiSettings.CollectNetworkEdges);
                InvalidateExport();
                GetAuditSystem()?.RequestCensusScan(scanOptions);
                PublishSnapshot(force: true);
            }
            catch
            {
                _diagnostics.Add("APA-CEN-004", "ui_census_request_rejected");
                PublishSnapshot(force: true);
            }
        }

        private void HandleRequestAssetAudit(string optionsJson)
        {
            try
            {
                if (UiSnapshotBuilder.TryDeserialize<UiScanOptions>(optionsJson, out var options))
                    _uiSettings = NormalizeSettings(options);
                InvalidateExport();
                GetAuditSystem()?.RequestAssetAudit(
                    _uiSettings.FrameBudgetMs,
                    _uiSettings.RefreshCatalogAtScanStart,
                    _uiSettings.EnableHeuristicFindings);
                PublishSnapshot(force: true);
            }
            catch
            {
                _diagnostics.Add("APA-AUD-004", "ui_asset_audit_request_rejected");
                PublishSnapshot(force: true);
            }
        }

        private void HandleRequestDeepInspection(string renderKeyText)
        {
            try
            {
                if (!RenderAssetKey.TryParse(renderKeyText, out var renderKey))
                    throw new ArgumentException("The render-asset key payload was invalid.", nameof(renderKeyText));
                var auditSystem = GetAuditSystem();
                if (auditSystem == null || !auditSystem.RequestDeepInspection(renderKey))
                    throw new InvalidOperationException("Deep Inspection could not be started for the selected render asset.");
                InvalidateExport();
                PublishSnapshot(force: true);
            }
            catch
            {
                _diagnostics.Add("APA-DEEP-004", "ui_deep_inspection_request_rejected");
                PublishSnapshot(force: true);
            }
        }

        private void HandleCancelCurrentScan()
        {
            GetAuditSystem()?.CancelCurrentScan();
            PublishSnapshot(force: true);
        }

        private void HandleQueryAssets(string queryJson)
        {
            try
            {
                if (!UiSnapshotBuilder.TryDeserialize<UiAssetQueryRequest>(queryJson, out var request))
                    throw new ArgumentException("The asset query payload was invalid.");
                _assetQuery = CreateAssetQuery(request);
                RefreshAssetPage(GetAuditSystem(), force: true);
                PublishSnapshot(force: true);
            }
            catch
            {
                _diagnostics.Add("APA-EXP-002", "ui_asset_query_rejected");
                PublishSnapshot(force: true);
            }
        }

        private void HandleUpdateSettings(string settingsJson)
        {
            try
            {
                if (!UiSnapshotBuilder.TryDeserialize<UiScanOptions>(settingsJson, out var options))
                    throw new ArgumentException("The settings payload was invalid.");
                _uiSettings = NormalizeSettings(options);
                PublishSnapshot(force: true);
            }
            catch
            {
                _diagnostics.Add("APA-EXP-003", "ui_scan_settings_rejected");
                PublishSnapshot(force: true);
            }
        }

        private void HandleRequestExport(string requestJson)
        {
            var auditSystem = GetAuditSystem();
            if (auditSystem?.Capabilities == null || _exportBinding == null)
                return;
            try
            {
                if (!UiSnapshotBuilder.TryDeserialize<UiExportRequest>(requestJson, out var request))
                    throw new ArgumentException("The export request payload was invalid.");
                var scope = ParseEnum(request.Scope, ExportScope.Full);
                var includedKeys = ResolveExportKeys(auditSystem, request, scope);
                var report = _reportBuilder.BuildCurrent(
                    auditSystem.CatalogRecords,
                    auditSystem.CatalogGeneration,
                    auditSystem.CatalogGeneration > 0 ? auditSystem.CatalogCapturedAt : (DateTimeOffset?)null,
                    auditSystem.PublishedCensus,
                    auditSystem.PublishedAnalysis,
                    auditSystem.Capabilities,
                    ProjectInfo.ModVersion,
                    DateTimeOffset.UtcNow,
                    _diagnostics.Snapshot(),
                    scope,
                    includedKeys);
                _exportBinding.Update(string.Equals(request.Format, "Csv", StringComparison.Ordinal)
                    ? _csvExporter.Export(report)
                    : _reportSerializer.Serialize(report));
            }
            catch
            {
                _diagnostics.Add("APA-EXP-001", "audit_report_export_failed");
                _exportBinding.Update("{\"errorCode\":\"APA-EXP-001\"}");
            }
        }

        private IEnumerable<PrefabKey>? ResolveExportKeys(AssetAuditSystem auditSystem, UiExportRequest request, ExportScope scope)
        {
            if (scope == ExportScope.Filtered)
            {
                return new AssetQueryService(auditSystem.CatalogRecords, auditSystem.PublishedCensus, auditSystem.CatalogGeneration)
                    .QueryMatchingKeys(_assetQuery);
            }
            if (scope != ExportScope.Selected)
                return null;
            if (request.SelectedKeys == null || request.SelectedKeys.Length == 0)
                throw new ArgumentException("Selected export requires at least one stable Prefab key.");
            return request.SelectedKeys
                .Where(key => key != null && !string.IsNullOrWhiteSpace(key.PrefabId) && !string.IsNullOrWhiteSpace(key.PrefabType))
                .Select(key => new PrefabKey(key.PrefabId, key.PrefabType))
                .Distinct()
                .ToArray();
        }

        private bool RefreshAssetPage(AssetAuditSystem? auditSystem, bool force)
        {
            var catalogGeneration = auditSystem?.CatalogGeneration ?? 0;
            var census = auditSystem?.PublishedCensus;
            var analysis = auditSystem?.PublishedAnalysis;
            var underlyingDataChanged = !ReferenceEquals(auditSystem, _lastAuditSystem)
                || catalogGeneration != _lastCatalogGeneration
                || !ReferenceEquals(census, _lastCensus)
                || !ReferenceEquals(analysis, _lastAnalysis);
            var changed = force || underlyingDataChanged;
            if (!changed)
                return false;
            if (underlyingDataChanged && _lastAuditSystem != null)
                InvalidateExport();
            var records = auditSystem?.CatalogRecords ?? Array.Empty<PrefabRecord>();
            var service = new AssetQueryService(records, census, catalogGeneration);
            _assetPage = service.Query(_assetQuery);
            _lastAuditSystem = auditSystem;
            _lastCatalogGeneration = catalogGeneration;
            _lastCensus = census;
            _lastAnalysis = analysis;
            return true;
        }

        private void InvalidateExport() => _exportBinding?.Update(string.Empty);

        private void PublishSnapshot(bool force)
        {
            if (_snapshotBinding == null)
                return;
            var auditSystem = GetAuditSystem();
            var snapshot = _snapshotBuilder.Build(auditSystem, _assetPage, _uiSettings, ProjectInfo.ModVersion);
            UiAnalysisProjection.ApplyDeepInspections(snapshot, GetCurrentAnalysis(auditSystem));
            snapshot.Diagnostics = CreateDiagnostics(auditSystem);
            var json = UiSnapshotBuilder.Serialize(snapshot);
            var publishInterval = TimeSpan.FromMilliseconds(_uiSettings.ProgressUpdateMs);
            if (!force && StringComparer.Ordinal.Equals(json, _lastSnapshotJson))
                return;
            if (!force && DateTimeOffset.UtcNow - _lastPublishedAt < publishInterval)
                return;
            _snapshotBinding.Update(json);
            _lastSnapshotJson = json;
            _lastPublishedAt = DateTimeOffset.UtcNow;
        }

        private static AssetAnalysisSnapshot? GetCurrentAnalysis(AssetAuditSystem? auditSystem)
        {
            var analysis = auditSystem?.PublishedAnalysis;
            if (auditSystem == null || analysis == null)
                return null;
            return analysis.WorldGeneration == auditSystem.WorldGeneration
                && analysis.CatalogGeneration == auditSystem.CatalogGeneration
                ? analysis
                : null;
        }

        private UiDiagnostics CreateDiagnostics(AssetAuditSystem? auditSystem)
        {
            var telemetry = auditSystem?.TelemetrySnapshot;
            return new UiDiagnostics
            {
                Harmony = "not used",
                LastScanState = auditSystem?.CurrentScan?.State.ToString() ?? "Idle",
                LastDiagnosticCode = auditSystem?.LastDiagnosticCode,
                CatalogUnresolvedCount = auditSystem?.CatalogUnresolvedEntityCount ?? 0,
                UnmatchedPrefabReferenceCount = auditSystem?.UnmatchedPrefabReferenceCount ?? 0,
                DiagnosticDistinctCount = _diagnostics.DistinctCount,
                DiagnosticOccurrenceCount = _diagnostics.OccurrenceCount,
                Telemetry = telemetry == null ? null : new UiScanTelemetry
                {
                    ElapsedMilliseconds = telemetry.ElapsedMilliseconds,
                    ProcessedItems = telemetry.ProcessedItems,
                    SliceCount = telemetry.SliceCount,
                    SampleCount = telemetry.SampleCount,
                    MaxSliceMilliseconds = telemetry.MaxSliceMilliseconds,
                    P95SliceMilliseconds = telemetry.P95SliceMilliseconds
                },
                AggregatedDiagnostics = _diagnostics.Snapshot().Select(item => new UiDiagnosticEntry
                {
                    Code = item.Code.Value,
                    Message = item.Message,
                    Count = item.Count,
                    FirstSeenAt = FormatTime(item.FirstSeenAt),
                    LastSeenAt = FormatTime(item.LastSeenAt)
                }).ToArray()
            };
        }

        private static AssetQuery CreateAssetQuery(UiAssetQueryRequest request)
        {
            var traitFilter = ParseOptionalEnum<PrefabTraits>(request.TraitFilter);
            var sourceFilter = ParseEnum(request.SourceFilter, AssetSourceFilter.Any);
            var presenceFilter = ParseOptionalEnum<CensusPresence>(request.PresenceFilter);
            var sort = ParseEnum(request.Sort, AssetSort.DisplayNameAscending);
            return new AssetQuery(request.SearchText, traitFilter, sourceFilter, presenceFilter, sort, request.Offset, Math.Min(AssetQueryService.MaximumPageSize, Math.Max(1, request.Limit)));
        }

        private static UiScanOptions NormalizeSettings(UiScanOptions options)
        {
            return new UiScanOptions
            {
                CollectSubordinateObjects = options.CollectSubordinateObjects,
                CollectNetworkEdges = options.CollectNetworkEdges,
                FrameBudgetMs = Clamp(options.FrameBudgetMs, 0.25, 8.0, 1.0),
                ProgressUpdateMs = (int)Clamp(options.ProgressUpdateMs, 50, 2000, 200),
                RefreshCatalogAtScanStart = options.RefreshCatalogAtScanStart,
                EnableHeuristicFindings = options.EnableHeuristicFindings,
                EnablePeerOutliers = options.EnablePeerOutliers,
                ComparisonPopulation = NormalizePopulation(options.ComparisonPopulation),
                ShowNoticeFindings = options.ShowNoticeFindings,
                PageSize = (int)Clamp(options.PageSize, 25, AssetQueryService.MaximumPageSize, 100),
                MetadataCacheLimit = (int)Clamp(options.MetadataCacheLimit, 64, 4096, 512),
                DeepInspectionLimit = (int)Clamp(options.DeepInspectionLimit, 1, 16, 1),
                UiScale = Clamp(options.UiScale, 0.75, 1.5, 1.0)
            };
        }

        private static double Clamp(double value, double minimum, double maximum, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return fallback;
            return Math.Min(maximum, Math.Max(minimum, value));
        }

        private static string NormalizePopulation(string? value)
        {
            switch (value)
            {
                case "BuiltinDlc":
                case "Custom":
                case "SameSourcePack":
                case "SameCategory":
                    return value;
                default:
                    return "SameCategory";
            }
        }

        private static T ParseEnum<T>(string? value, T fallback) where T : struct
        {
            return !string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, ignoreCase: false, out T parsed)
                && Enum.IsDefined(typeof(T), parsed)
                ? parsed
                : fallback;
        }

        private static T? ParseOptionalEnum<T>(string? value) where T : struct
        {
            if (string.IsNullOrWhiteSpace(value) || !Enum.TryParse(value, ignoreCase: false, out T parsed) || !Enum.IsDefined(typeof(T), parsed))
                return null;
            return parsed;
        }

        private static string FormatTime(DateTimeOffset value) => value.ToUniversalTime().ToString("O");
    }
}
