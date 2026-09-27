using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using CS2AssetPerformanceAuditor.Core.Capabilities;
using CS2AssetPerformanceAuditor.Core.Census;
using CS2AssetPerformanceAuditor.Core.Diagnostics;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using CS2AssetPerformanceAuditor.Core.Rendering;
using CS2AssetPerformanceAuditor.Core.Scanning;
using CS2AssetPerformanceAuditor.GameIntegration.Capabilities;
using CS2AssetPerformanceAuditor.GameIntegration.Census;
using CS2AssetPerformanceAuditor.GameIntegration.Prefabs;
using CS2AssetPerformanceAuditor.GameIntegration.Rendering;
using Game;
using Unity.Entities;

namespace CS2AssetPerformanceAuditor.GameIntegration
{
    public sealed class AssetAuditSystem : GameSystemBase
    {
        private const int CatalogSliceSize = 64;
        private const int CensusReductionSliceSize = 512;
        private static long _nextWorldGeneration;

        private IPrefabCatalogAccess? _catalog;
        private CensusAccess? _censusAccess;
        private readonly PublishedAuditState _publishedState = new PublishedAuditState();
        private CensusReducer? _censusReducer;
        private AssetAnalysisCollector? _analysisCollector;
        private RenderGraphSnapshot? _publishedRuntimeRenderGraph;
        private ScanTelemetry? _scanTelemetry;
        private bool _catalogScanRequested;
        private bool _catalogCaptureActive;
        private bool _censusScanRequested;
        private bool _censusCleanupRequested;
        private bool _networkCaptureStarted;
        private bool _assetAuditRequested;
        private bool _assetAuditWaitingForCatalog;
        private bool _assetAuditRefreshCatalog = true;
        private bool _assetAuditEnableHeuristics = true;
        private double _assetAuditFrameBudgetMs = 1.0;
        private long _catalogGenerationBeforeCapture;
        private long _catalogGenerationAtCensusStart;
        private long _nextAnalysisGeneration;
        private ScanOptions _requestedOptions = ScanOptions.Default;

        public CapabilityReport? Capabilities { get; private set; }
        public long WorldGeneration { get; private set; }
        public long CatalogGeneration => _catalog?.CatalogGeneration ?? 0;
        public DateTimeOffset CatalogCapturedAt => _catalog?.CatalogCapturedAt ?? DateTimeOffset.MinValue;
        public IReadOnlyList<PrefabRecord> CatalogRecords => _catalog?.PublishedRecords ?? Array.Empty<PrefabRecord>();
        public int CatalogCapturedEntityCount => _catalog?.CapturedEntityCount ?? 0;
        public int CatalogProcessedEntityCount => _catalog?.ProcessedEntityCount ?? 0;
        public int CatalogUnresolvedEntityCount => _catalog?.UnresolvedEntityCount ?? 0;
        public IReadOnlyDictionary<Entity, PrefabKey> RuntimeEntityKeys => _catalog?.RuntimeEntityKeys ?? new Dictionary<Entity, PrefabKey>();
        public ScanSession? CurrentScan { get; private set; }
        public bool IsScanActive => CurrentScan != null && (CurrentScan.State == ScanState.Running || CurrentScan.State == ScanState.CancellationRequested);
        public bool IsCensusScanActive => IsScanActive && CurrentScan?.Kind == ScanKind.Census;
        public CensusSnapshot? PublishedCensus => _publishedState.Census;
        public AssetAnalysisSnapshot? PublishedAnalysis => _publishedState.Analysis;
        public RenderGraphSnapshot? PublishedRuntimeRenderGraph => _publishedRuntimeRenderGraph;
        public string? LastDiagnosticCode { get; private set; }
        public int UnmatchedPrefabReferenceCount => _censusAccess?.UnmatchedPrefabReferenceCount ?? 0;
        public ScanTelemetrySnapshot? TelemetrySnapshot => _scanTelemetry?.Snapshot(DateTimeOffset.UtcNow);

        public void RequestCatalogScan()
        {
            if (!IsScanActive && !_catalogCaptureActive && !_assetAuditRequested)
                _catalogScanRequested = true;
        }

        public void RequestCensusScan(ScanOptions? scanOptions = null)
        {
            if (IsScanActive || _censusScanRequested || _censusCleanupRequested || _assetAuditRequested || _assetAuditWaitingForCatalog)
                return;
            _requestedOptions = scanOptions ?? ScanOptions.Default;
            _censusScanRequested = true;
        }

        public void RequestAssetAudit(double frameBudgetMilliseconds = 1.0, bool refreshCatalogAtScanStart = true, bool enableHeuristicFindings = true)
        {
            if (IsScanActive || _assetAuditRequested || _assetAuditWaitingForCatalog || _censusScanRequested || _censusCleanupRequested || _catalogCaptureActive)
                return;
            _assetAuditFrameBudgetMs = NormalizeFrameBudget(frameBudgetMilliseconds);
            _assetAuditRefreshCatalog = refreshCatalogAtScanStart;
            _assetAuditEnableHeuristics = enableHeuristicFindings;
            _assetAuditRequested = true;
        }

        public void CancelCensusScan()
        {
            if (CurrentScan?.Kind == ScanKind.Census && CurrentScan.State == ScanState.Running)
                CurrentScan.RequestCancellation();
        }

        public void CancelCurrentScan()
        {
            if (CurrentScan?.State == ScanState.Running)
                CurrentScan.RequestCancellation();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            WorldGeneration = Interlocked.Increment(ref _nextWorldGeneration);
            _publishedState.ResetForWorld(WorldGeneration);
            Capabilities = CapabilityProbe.Probe(World);
            _catalog = new PrefabCatalogAccess(World);
            _censusAccess = new CensusAccess(World);
        }

        protected override void OnUpdate()
        {
            if (_censusCleanupRequested)
            {
                PollCensusCleanup();
                return;
            }

            if (CurrentScan?.State == ScanState.CancellationRequested)
            {
                if (CurrentScan.Kind == ScanKind.Census)
                    BeginCensusCancellation();
                else
                    CancelManagedScan();
                return;
            }

            if (_censusScanRequested && !IsScanActive && !_catalogCaptureActive)
            {
                StartCensusScan();
                return;
            }

            if (_assetAuditRequested && !IsScanActive && !_catalogCaptureActive)
            {
                StartAssetAudit();
                return;
            }

            if (_catalogScanRequested && !_catalogCaptureActive)
            {
                StartCatalogCapture();
                return;
            }

            if (_catalogCaptureActive)
            {
                ProcessCatalogCapture();
                return;
            }

            if (!IsScanActive)
                return;

            if (CurrentScan!.Kind == ScanKind.Census)
                AdvanceCensusScan();
            else if (CurrentScan.Kind == ScanKind.AssetAudit)
                AdvanceAssetAudit();
        }

        protected override void OnDestroy()
        {
            if (CurrentScan?.State == ScanState.Running)
                CurrentScan.RequestCancellation();
            _catalog?.CancelCapture();
            _censusAccess?.Dispose();
            _analysisCollector = null;
            _publishedRuntimeRenderGraph = null;
            if (CurrentScan?.State == ScanState.CancellationRequested)
                CurrentScan.MarkCancelled();
            _publishedState.ResetForWorld(WorldGeneration + 1);
            base.OnDestroy();
        }

        private void StartCensusScan()
        {
            _censusScanRequested = false;
            LastDiagnosticCode = null;
            _catalogGenerationAtCensusStart = CatalogGeneration;
            var startedAt = DateTimeOffset.UtcNow;
            _scanTelemetry = new ScanTelemetry(startedAt);
            CurrentScan = ScanSession.Start(ScanKind.Census, WorldGeneration, startedAt);
            CurrentScan.TransitionTo(ScanStage.CapturingCatalog);
            _catalogScanRequested = true;
        }

        private void StartAssetAudit()
        {
            _assetAuditRequested = false;
            LastDiagnosticCode = null;
            var startedAt = DateTimeOffset.UtcNow;
            _scanTelemetry = new ScanTelemetry(startedAt);
            CurrentScan = ScanSession.Start(ScanKind.AssetAudit, WorldGeneration, startedAt);

            if (_assetAuditRefreshCatalog || CatalogGeneration == 0)
            {
                _assetAuditWaitingForCatalog = true;
                _catalogScanRequested = true;
                return;
            }

            BeginAssetAnalysis();
        }

        private void StartCatalogCapture()
        {
            _catalogScanRequested = false;
            if (_catalog == null)
                return;
            _catalogGenerationBeforeCapture = _catalog.CatalogGeneration;
            var deferPublication = CurrentScan?.Kind == ScanKind.Census
                && CurrentScan.State == ScanState.Running
                && CurrentScan.Stage == ScanStage.CapturingCatalog;
            try
            {
                _catalog.BeginCapture(deferPublication);
                _catalogCaptureActive = true;
            }
            catch
            {
                LastDiagnosticCode = "APA-CAT-001";
                DegradeCapability(CapabilityId.PrefabCatalog, "catalog_capture_failed");
                if (CurrentScan?.State == ScanState.Running)
                {
                    if (CurrentScan.Kind == ScanKind.Census)
                        FailCensusScan("APA-CAT-001", CapabilityId.PrefabCatalog, "catalog_capture_failed");
                    else if (CurrentScan.Kind == ScanKind.AssetAudit)
                        FailAssetAudit("APA-CAT-001", CapabilityId.PrefabCatalog, "catalog_capture_failed");
                }
            }
        }

        private void ProcessCatalogCapture()
        {
            if (_catalog == null)
            {
                _catalogCaptureActive = false;
                return;
            }

            if (_catalog.IsWorking)
            {
                var before = _catalog.ProcessedEntityCount;
                var stopwatch = Stopwatch.StartNew();
                _catalog.ProcessNextSlice(CatalogSliceSize);
                stopwatch.Stop();
                RecordManagedSlice(stopwatch.Elapsed, Math.Max(0, _catalog.ProcessedEntityCount - before));
                if (CurrentScan?.Kind == ScanKind.Census && CurrentScan.Stage == ScanStage.ProcessingCatalog && CurrentScan.State == ScanState.Running)
                    ReportExactProgress(CurrentScan, _catalog.ProcessedEntityCount, _catalog.CapturedEntityCount);
                return;
            }

            _catalogCaptureActive = false;
            if (CurrentScan?.State == ScanState.Running && CurrentScan.Kind == ScanKind.Census && CurrentScan.Stage == ScanStage.CapturingCatalog)
            {
                if (!_catalog.HasPendingPublication || _catalog.PendingCatalogGeneration <= _catalogGenerationBeforeCapture)
                {
                    LastDiagnosticCode = "APA-CAT-002";
                    DegradeCapability(CapabilityId.PrefabCatalog, "catalog_capture_not_staged");
                    FailCensusScan("APA-CAT-002", CapabilityId.PrefabCatalog, "catalog_capture_not_staged");
                    return;
                }
                CurrentScan.TransitionTo(ScanStage.ProcessingCatalog);
                return;
            }

            if (CurrentScan?.State == ScanState.Running && CurrentScan.Kind == ScanKind.AssetAudit && _assetAuditWaitingForCatalog)
            {
                _assetAuditWaitingForCatalog = false;
                if (_catalog.CatalogGeneration <= _catalogGenerationBeforeCapture)
                {
                    FailAssetAudit("APA-CAT-004", CapabilityId.PrefabCatalog, "catalog_refresh_failed_before_asset_audit");
                    return;
                }
                BeginAssetAnalysis();
                return;
            }

            if (_catalog.CatalogGeneration <= _catalogGenerationBeforeCapture)
            {
                LastDiagnosticCode = "APA-CAT-002";
                DegradeCapability(CapabilityId.PrefabCatalog, "catalog_capture_not_published");
            }
        }

        private void BeginAssetAnalysis()
        {
            var session = CurrentScan;
            if (session == null || session.Kind != ScanKind.AssetAudit || session.State != ScanState.Running || session.Stage != ScanStage.Preparing)
                return;
            if (CatalogGeneration <= 0)
            {
                FailAssetAudit("APA-AUD-001", CapabilityId.PrefabCatalog, "asset_audit_requires_published_catalog");
                return;
            }

            var previousGeneration = PublishedAnalysis?.AnalysisGeneration ?? 0;
            _nextAnalysisGeneration = Math.Max(_nextAnalysisGeneration + 1, previousGeneration + 1);
            _analysisCollector = new AssetAnalysisCollector(
                World,
                CatalogRecords,
                RuntimeEntityKeys,
                PublishedCensus,
                WorldGeneration,
                CatalogGeneration,
                _nextAnalysisGeneration,
                DateTimeOffset.UtcNow,
                _assetAuditEnableHeuristics);
            session.TransitionTo(ScanStage.ResolvingRenderGraph);
        }

        private void AdvanceAssetAudit()
        {
            var session = CurrentScan;
            var collector = _analysisCollector;
            if (session == null || collector == null || session.Kind != ScanKind.AssetAudit || session.State != ScanState.Running)
                return;

            try
            {
                switch (session.Stage)
                {
                    case ScanStage.ResolvingRenderGraph:
                    {
                        var before = collector.RenderProcessedCount;
                        var stopwatch = Stopwatch.StartNew();
                        var completed = collector.ProcessRenderGraphSlice(_assetAuditFrameBudgetMs);
                        stopwatch.Stop();
                        RecordManagedSlice(stopwatch.Elapsed, Math.Max(0, collector.RenderProcessedCount - before));
                        ReportExactProgress(session, collector.RenderProcessedCount, collector.RenderTargetCount);
                        if (completed)
                            session.TransitionTo(ScanStage.CollectingGeometry);
                        return;
                    }
                    case ScanStage.CollectingGeometry:
                    {
                        var before = collector.GeometryProcessedCount;
                        var stopwatch = Stopwatch.StartNew();
                        var completed = collector.ProcessGeometrySlice(_assetAuditFrameBudgetMs);
                        stopwatch.Stop();
                        RecordManagedSlice(stopwatch.Elapsed, Math.Max(0, collector.GeometryProcessedCount - before));
                        ReportExactProgress(session, collector.GeometryProcessedCount, collector.GeometryTargetCount);
                        if (completed)
                            session.TransitionTo(ScanStage.CollectingSurfaceTexture);
                        return;
                    }
                    case ScanStage.CollectingSurfaceTexture:
                    {
                        var before = collector.SurfaceTextureProcessedCount;
                        var stopwatch = Stopwatch.StartNew();
                        var completed = collector.ProcessSurfaceTextureSlice(_assetAuditFrameBudgetMs);
                        stopwatch.Stop();
                        RecordManagedSlice(stopwatch.Elapsed, Math.Max(0, collector.SurfaceTextureProcessedCount - before));
                        ReportExactProgress(session, collector.SurfaceTextureProcessedCount, collector.SurfaceTextureTargetCount);
                        if (completed)
                            session.TransitionTo(ScanStage.EvaluatingFindings);
                        return;
                    }
                    case ScanStage.EvaluatingFindings:
                    {
                        var before = collector.FindingsProcessedCount;
                        var stopwatch = Stopwatch.StartNew();
                        var completed = collector.ProcessFindingSlice(_assetAuditFrameBudgetMs);
                        stopwatch.Stop();
                        RecordManagedSlice(stopwatch.Elapsed, Math.Max(0, collector.FindingsProcessedCount - before));
                        ReportExactProgress(session, collector.FindingsProcessedCount, collector.FindingsTargetCount);
                        if (completed)
                            PublishAssetAnalysis(session, collector);
                        return;
                    }
                }
            }
            catch
            {
                var capability = session.Stage == ScanStage.CollectingSurfaceTexture
                    ? CapabilityId.SurfaceMetadata
                    : CapabilityId.GeometryMetadata;
                FailAssetAudit("APA-AUD-002", capability, "asset_analysis_stage_failed");
            }
        }

        private void PublishAssetAnalysis(ScanSession session, AssetAnalysisCollector collector)
        {
            if (WorldGeneration != session.WorldGeneration || CatalogGeneration <= 0)
            {
                FailAssetAudit("APA-AUD-003", CapabilityId.GeometryMetadata, "world_or_catalog_changed_before_analysis_publish");
                return;
            }

            var snapshot = collector.BuildSnapshot();
            if (snapshot.WorldGeneration != WorldGeneration || snapshot.CatalogGeneration != CatalogGeneration)
            {
                FailAssetAudit("APA-AUD-003", CapabilityId.GeometryMetadata, "analysis_generation_context_changed_before_publish");
                return;
            }

            session.TransitionTo(ScanStage.Finalizing);
            session.ReportProgress(1, 1);
            if (!_publishedState.TryPublishAnalysis(snapshot, scanSucceeded: session.CanPublish))
            {
                FailAssetAudit("APA-AUD-003", CapabilityId.GeometryMetadata, "analysis_publish_world_mismatch");
                return;
            }

            _publishedRuntimeRenderGraph = collector.RenderGraph;
            session.Complete();
            LastDiagnosticCode = null;
            _analysisCollector = null;
        }

        private void AdvanceCensusScan()
        {
            var session = CurrentScan;
            var census = _censusAccess;
            var catalog = _catalog;
            if (session == null || census == null || catalog == null || session.State != ScanState.Running)
                return;

            try
            {
                switch (session.Stage)
                {
                    case ScanStage.ProcessingCatalog:
                        if (!catalog.HasPendingPublication || catalog.PendingCatalogGeneration <= _catalogGenerationAtCensusStart)
                        {
                            FailCensusScan("APA-CAT-003", CapabilityId.PrefabCatalog, "catalog_generation_did_not_advance");
                            return;
                        }
                        _censusReducer = new CensusReducer(catalog.PendingRecords, WorldGeneration, catalog.PendingCatalogGeneration, DateTimeOffset.UtcNow, _requestedOptions);
                        session.TransitionTo(ScanStage.CapturingObjectCensus);
                        census.BeginObjectCapture(catalog.PendingRuntimeEntityKeys, _censusReducer, _requestedOptions);
                        return;

                    case ScanStage.CapturingObjectCensus:
                        if (!census.ObjectCaptureJobsCompleted)
                            return;
                        census.CompleteObjectCapture();
                        session.TransitionTo(ScanStage.ReducingObjectCensus);
                        ReportExactProgress(session, 0, census.CapturedObjectReferenceCount);
                        return;

                    case ScanStage.ReducingObjectCensus:
                        var objectBefore = census.ProcessedObjectReferenceCount;
                        var objectStopwatch = Stopwatch.StartNew();
                        census.ReduceObjectSlice(CensusReductionSliceSize);
                        objectStopwatch.Stop();
                        RecordManagedSlice(objectStopwatch.Elapsed, Math.Max(0, census.ProcessedObjectReferenceCount - objectBefore));
                        ReportExactProgress(session, census.ProcessedObjectReferenceCount, census.CapturedObjectReferenceCount);
                        if (!census.ObjectReductionCompleted)
                            return;
                        census.ReleaseObjectCapture();
                        session.TransitionTo(ScanStage.CapturingNetworkCensus);
                        return;

                    case ScanStage.CapturingNetworkCensus:
                        if (!_networkCaptureStarted)
                        {
                            census.BeginNetworkCapture();
                            _networkCaptureStarted = true;
                            return;
                        }
                        if (!census.NetworkCaptureJobsCompleted)
                            return;
                        if (_requestedOptions.CollectNetworkEdges)
                            census.CompleteNetworkCapture();
                        session.TransitionTo(ScanStage.ReducingNetworkCensus);
                        ReportExactProgress(session, 0, census.CapturedNetworkEdgeCount);
                        return;

                    case ScanStage.ReducingNetworkCensus:
                        var networkBefore = census.ProcessedNetworkEdgeCount;
                        var networkStopwatch = Stopwatch.StartNew();
                        census.ReduceNetworkSlice(CensusReductionSliceSize);
                        networkStopwatch.Stop();
                        RecordManagedSlice(networkStopwatch.Elapsed, Math.Max(0, census.ProcessedNetworkEdgeCount - networkBefore));
                        ReportExactProgress(session, census.ProcessedNetworkEdgeCount, census.CapturedNetworkEdgeCount);
                        if (!census.NetworkReductionCompleted)
                            return;
                        census.ReleaseNetworkCapture();
                        PublishCensus(session, census);
                        return;
                }
            }
            catch
            {
                var failedCapability = session.Stage == ScanStage.CapturingNetworkCensus || session.Stage == ScanStage.ReducingNetworkCensus
                    ? CapabilityId.NetworkEdgeCensus
                    : CapabilityId.ObjectCensus;
                var diagnostic = failedCapability == CapabilityId.NetworkEdgeCensus ? "APA-CEN-002" : "APA-CEN-001";
                FailCensusScan(diagnostic, failedCapability, "census_capture_or_reduction_failed");
            }
        }

        private void PublishCensus(ScanSession session, CensusAccess census)
        {
            var catalog = _catalog;
            if (catalog == null || !catalog.HasPendingPublication || WorldGeneration != session.WorldGeneration || _publishedState.WorldGeneration != session.WorldGeneration)
            {
                FailCensusScan("APA-CEN-003", CapabilityId.ObjectCensus, "world_generation_changed_before_publish");
                return;
            }

            var snapshot = census.BuildSnapshot();
            if (snapshot.CatalogGeneration != catalog.PendingCatalogGeneration)
            {
                FailCensusScan("APA-CEN-003", CapabilityId.ObjectCensus, "catalog_generation_changed_before_publish");
                return;
            }

            session.TransitionTo(ScanStage.Finalizing);
            session.ReportProgress(1, 1);
            catalog.CommitPendingCapture();
            if (!_publishedState.TryPublishCensus(snapshot, scanSucceeded: session.CanPublish))
            {
                LastDiagnosticCode = "APA-CEN-003";
                DegradeCapability(CapabilityId.ObjectCensus, "census_publish_world_mismatch");
                return;
            }

            session.Complete();
            LastDiagnosticCode = null;
            census.FinishScan();
            _censusReducer = null;
            _networkCaptureStarted = false;
        }

        private void BeginCensusCancellation()
        {
            if (_catalogCaptureActive || _catalog?.HasPendingPublication == true)
            {
                _catalog?.CancelCapture();
                _catalogCaptureActive = false;
            }
            _catalogScanRequested = false;
            _censusAccess?.RequestCancellation();
            _censusCleanupRequested = true;
            PollCensusCleanup();
        }

        private void CancelManagedScan()
        {
            if (CurrentScan == null || CurrentScan.State != ScanState.CancellationRequested)
                return;
            if (_catalogCaptureActive)
            {
                _catalog?.CancelCapture();
                _catalogCaptureActive = false;
            }
            _catalogScanRequested = false;
            _assetAuditWaitingForCatalog = false;
            _analysisCollector = null;
            CurrentScan.MarkCancelled();
        }

        private void FailCensusScan(string diagnosticCode, CapabilityId capability, string capabilityDetail)
        {
            LastDiagnosticCode = diagnosticCode;
            DegradeCapability(capability, capabilityDetail);
            _catalogScanRequested = false;
            if (_catalogCaptureActive || _catalog?.HasPendingPublication == true)
            {
                _catalog?.CancelCapture();
                _catalogCaptureActive = false;
            }
            if (CurrentScan?.State == ScanState.Running || CurrentScan?.State == ScanState.CancellationRequested)
                CurrentScan.Fail(diagnosticCode);
            _censusAccess?.RequestCancellation();
            _censusCleanupRequested = true;
            PollCensusCleanup();
        }

        private void FailAssetAudit(string diagnosticCode, CapabilityId capability, string capabilityDetail)
        {
            LastDiagnosticCode = diagnosticCode;
            DegradeCapability(capability, capabilityDetail);
            _catalogScanRequested = false;
            _assetAuditWaitingForCatalog = false;
            if (_catalogCaptureActive)
            {
                _catalog?.CancelCapture();
                _catalogCaptureActive = false;
            }
            if (CurrentScan?.State == ScanState.Running || CurrentScan?.State == ScanState.CancellationRequested)
                CurrentScan.Fail(diagnosticCode);
            _analysisCollector = null;
        }

        private void PollCensusCleanup()
        {
            if (_censusAccess == null)
            {
                _censusCleanupRequested = false;
                return;
            }
            if (_censusAccess.CleanupPending)
                return;
            _censusAccess.CompleteCleanup();
            _censusCleanupRequested = false;
            _censusReducer = null;
            _networkCaptureStarted = false;
            if (CurrentScan?.State == ScanState.CancellationRequested)
                CurrentScan.MarkCancelled();
        }

        private void RecordManagedSlice(TimeSpan elapsed, long processedItems)
        {
            _scanTelemetry?.RecordManagedSlice(elapsed, processedItems);
        }

        private static void ReportExactProgress(ScanSession session, long completed, long total)
        {
            if (total > 0)
                session.ReportProgress(Math.Min(completed, total), total);
            else
                session.ReportProgress(null, null);
        }

        private void DegradeCapability(CapabilityId id, string detail)
        {
            if (Capabilities == null)
                return;
            var statuses = new List<CapabilityStatus>();
            var found = false;
            foreach (var status in Capabilities.Capabilities)
            {
                if (status.Id == id)
                {
                    statuses.Add(new CapabilityStatus(id, CapabilityState.Degraded, detail));
                    found = true;
                }
                else
                {
                    statuses.Add(status);
                }
            }
            if (!found)
                statuses.Add(new CapabilityStatus(id, CapabilityState.Degraded, detail));
            Capabilities = new CapabilityReport(Capabilities.GameVersion, Capabilities.Compatibility, statuses);
        }

        private static double NormalizeFrameBudget(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 1.0;
            return Math.Min(8.0, Math.Max(0.25, value));
        }
    }
}
