using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.UI.Binding;
using CS2AssetPerformanceAuditor.Core;
using CS2AssetPerformanceAuditor.Core.Census;
using CS2AssetPerformanceAuditor.Core.Diagnostics;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using CS2AssetPerformanceAuditor.Core.Query;
using CS2AssetPerformanceAuditor.Export;
using CS2AssetPerformanceAuditor.GameIntegration;
using Game.UI;
using Unity.Entities;

namespace CS2AssetPerformanceAuditor.UI
{
    public sealed class AssetAuditUISystem : UISystemBase
    {
        private static readonly TimeSpan PublishInterval = TimeSpan.FromMilliseconds(200);

        private readonly UiSnapshotBuilder _snapshotBuilder = new UiSnapshotBuilder();
        private readonly DiagnosticAggregator _diagnostics = new DiagnosticAggregator();
        private readonly AuditReportSerializer _reportSerializer = new AuditReportSerializer();
        private readonly AuditReportBuilder _reportBuilder = new AuditReportBuilder(new PrivacySanitizer());

        private ValueBinding<string>? _snapshotBinding;
        private ValueBinding<string>? _exportBinding;
        private AssetAuditSystem? _lastAuditSystem;
        private CensusSnapshot? _lastCensus;
        private long _lastCatalogGeneration = -1;
        private AssetQuery _assetQuery = new AssetQuery();
        private AssetPage _assetPage = new AssetPage(Array.AsReadOnly(new AssetPageItem[0]), 0, 0, AssetQuery.DefaultPageSize);
        private ScanOptions _scanOptions = ScanOptions.Default;
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
            AddBinding(new TriggerBinding(UiBindingContract.Group, UiBindingContract.CancelCensus, HandleCancelCensus));
            AddBinding(new TriggerBinding<string>(UiBindingContract.Group, UiBindingContract.QueryAssets, HandleQueryAssets, new Colossal.UI.Binding.StringReader()));
            AddBinding(new TriggerBinding(UiBindingContract.Group, UiBindingContract.RequestExport, HandleRequestExport));
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
            if (dataChanged || now - _lastPublishedAt >= PublishInterval)
                PublishSnapshot(force: dataChanged);
        }

        protected override void OnDestroy()
        {
            _snapshotBinding = null;
            _exportBinding = null;
            _lastAuditSystem = null;
            _lastCensus = null;
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
                    _scanOptions = new ScanOptions(options.CollectSubordinateObjects, options.CollectNetworkEdges);

                GetAuditSystem()?.RequestCensusScan(_scanOptions);
                PublishSnapshot(force: true);
            }
            catch
            {
                _diagnostics.Add("APA-CEN-004", "ui_census_request_rejected");
                PublishSnapshot(force: true);
            }
        }

        private void HandleCancelCensus()
        {
            GetAuditSystem()?.CancelCensusScan();
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
                    throw new ArgumentException("The scan settings payload was invalid.");
                _scanOptions = new ScanOptions(options.CollectSubordinateObjects, options.CollectNetworkEdges);
                PublishSnapshot(force: true);
            }
            catch
            {
                _diagnostics.Add("APA-EXP-003", "ui_scan_settings_rejected");
                PublishSnapshot(force: true);
            }
        }

        private void HandleRequestExport()
        {
            var auditSystem = GetAuditSystem();
            if (auditSystem?.Capabilities == null || _exportBinding == null)
                return;

            try
            {
                var report = _reportBuilder.Build(
                    auditSystem.CatalogRecords,
                    auditSystem.CatalogGeneration,
                    auditSystem.CatalogGeneration > 0 ? auditSystem.CatalogCapturedAt : (DateTimeOffset?)null,
                    auditSystem.PublishedCensus,
                    auditSystem.Capabilities,
                    ProjectInfo.ModVersion,
                    DateTimeOffset.UtcNow,
                    _diagnostics.Snapshot());
                _exportBinding.Update(_reportSerializer.Serialize(report));
            }
            catch
            {
                _diagnostics.Add("APA-EXP-001", "phase_one_report_export_failed");
                _exportBinding.Update("{\"errorCode\":\"APA-EXP-001\"}");
            }
        }

        private bool RefreshAssetPage(AssetAuditSystem? auditSystem, bool force)
        {
            var catalogGeneration = auditSystem?.CatalogGeneration ?? 0;
            var census = auditSystem?.PublishedCensus;
            var changed = force || !ReferenceEquals(auditSystem, _lastAuditSystem)
                || catalogGeneration != _lastCatalogGeneration
                || !ReferenceEquals(census, _lastCensus);
            if (!changed)
                return false;

            var records = auditSystem?.CatalogRecords ?? Array.Empty<PrefabRecord>();
            var service = new AssetQueryService(records, census, catalogGeneration);
            _assetPage = service.Query(_assetQuery);
            _lastAuditSystem = auditSystem;
            _lastCatalogGeneration = catalogGeneration;
            _lastCensus = census;
            return true;
        }

        private void PublishSnapshot(bool force)
        {
            if (_snapshotBinding == null)
                return;

            var auditSystem = GetAuditSystem();
            var snapshot = _snapshotBuilder.Build(auditSystem, _assetPage, _scanOptions, ProjectInfo.ModVersion);
            var json = UiSnapshotBuilder.Serialize(snapshot);
            if (!force && StringComparer.Ordinal.Equals(json, _lastSnapshotJson))
                return;
            if (!force && DateTimeOffset.UtcNow - _lastPublishedAt < PublishInterval)
                return;

            _snapshotBinding.Update(json);
            _lastSnapshotJson = json;
            _lastPublishedAt = DateTimeOffset.UtcNow;
        }

        private static AssetQuery CreateAssetQuery(UiAssetQueryRequest request)
        {
            var traitFilter = ParseOptionalEnum<PrefabTraits>(request.TraitFilter);
            var sourceFilter = ParseEnum(request.SourceFilter, AssetSourceFilter.Any);
            var presenceFilter = ParseOptionalEnum<CensusPresence>(request.PresenceFilter);
            var sort = ParseEnum(request.Sort, AssetSort.DisplayNameAscending);
            return new AssetQuery(
                request.SearchText,
                traitFilter,
                sourceFilter,
                presenceFilter,
                sort,
                request.Offset,
                Math.Max(1, request.Limit));
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
            if (string.IsNullOrWhiteSpace(value) || !Enum.TryParse(value, ignoreCase: false, out T parsed))
                return null;
            return parsed;
        }
    }
}
