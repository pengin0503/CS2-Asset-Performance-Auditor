using System.Collections.Generic;
using System.Threading;
using CS2AssetPerformanceAuditor.Core.Capabilities;
using CS2AssetPerformanceAuditor.Core.Census;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using CS2AssetPerformanceAuditor.Core.Scanning;
using CS2AssetPerformanceAuditor.GameIntegration.Capabilities;
using CS2AssetPerformanceAuditor.GameIntegration.Census;
using CS2AssetPerformanceAuditor.GameIntegration.Prefabs;
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
        private bool _catalogScanRequested;
        private bool _catalogCaptureActive;
        private bool _censusScanRequested;
        private bool _censusCleanupRequested;
        private bool _networkCaptureStarted;
        private long _catalogGenerationBeforeCapture;
        private long _catalogGenerationAtCensusStart;
        private ScanOptions _requestedOptions = ScanOptions.Default;

        public CapabilityReport? Capabilities { get; private set; }

        public long WorldGeneration { get; private set; }

        public long CatalogGeneration => _catalog?.CatalogGeneration ?? 0;

        public int CatalogCapturedEntityCount => _catalog?.CapturedEntityCount ?? 0;

        public int CatalogProcessedEntityCount => _catalog?.ProcessedEntityCount ?? 0;

        public int CatalogUnresolvedEntityCount => _catalog?.UnresolvedEntityCount ?? 0;

        public IReadOnlyList<PrefabRecord> CatalogRecords => _catalog?.PublishedRecords ?? System.Array.Empty<PrefabRecord>();

        public IReadOnlyDictionary<Entity, PrefabKey> RuntimeEntityKeys => _catalog?.RuntimeEntityKeys
            ?? new Dictionary<Entity, PrefabKey>();

        public ScanSession? CurrentScan { get; private set; }

        public bool IsCensusScanActive => CurrentScan != null
            && (CurrentScan.State == ScanState.Running || CurrentScan.State == ScanState.CancellationRequested);

        public CensusSnapshot? PublishedCensus => _publishedState.Census;

        public string? LastDiagnosticCode { get; private set; }

        public int UnmatchedPrefabReferenceCount => _censusAccess?.UnmatchedPrefabReferenceCount ?? 0;

        public void RequestCatalogScan()
        {
            if (!IsCensusScanActive && !_catalogCaptureActive)
                _catalogScanRequested = true;
        }

        public void RequestCensusScan(ScanOptions? scanOptions = null)
        {
            if (IsCensusScanActive || _censusScanRequested || _censusCleanupRequested)
                return;
            _requestedOptions = scanOptions ?? ScanOptions.Default;
            _censusScanRequested = true;
        }

        public void CancelCensusScan()
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
                BeginCensusCancellation();
                return;
            }

            if (_censusScanRequested && !IsCensusScanActive && !_catalogCaptureActive)
            {
                StartCensusScan();
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

            if (IsCensusScanActive)
                AdvanceCensusScan();
        }

        protected override void OnDestroy()
        {
            if (CurrentScan?.State == ScanState.Running)
                CurrentScan.RequestCancellation();
            _catalog?.CancelCapture();
            _censusAccess?.Dispose();
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
            CurrentScan = ScanSession.Start(ScanKind.Census, WorldGeneration, System.DateTimeOffset.UtcNow);
            CurrentScan.TransitionTo(ScanStage.CapturingCatalog);
            _catalogScanRequested = true;
        }

        private void StartCatalogCapture()
        {
            _catalogScanRequested = false;
            if (_catalog == null)
                return;

            _catalogGenerationBeforeCapture = _catalog.CatalogGeneration;
            try
            {
                _catalog.BeginCapture();
                _catalogCaptureActive = true;
            }
            catch
            {
                LastDiagnosticCode = "APA-CAT-001";
                DegradeCapability(CapabilityId.PrefabCatalog, "catalog_capture_failed");
                if (CurrentScan?.State == ScanState.Running)
                    FailCensusScan("APA-CAT-001", CapabilityId.PrefabCatalog, "catalog_capture_failed");
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
                _catalog.ProcessNextSlice(CatalogSliceSize);
                if (CurrentScan?.Stage == ScanStage.ProcessingCatalog && CurrentScan.State == ScanState.Running)
                    ReportExactProgress(CurrentScan, _catalog.ProcessedEntityCount, _catalog.CapturedEntityCount);
                return;
            }

            _catalogCaptureActive = false;
            if (_catalog.CatalogGeneration <= _catalogGenerationBeforeCapture)
            {
                LastDiagnosticCode = "APA-CAT-002";
                DegradeCapability(CapabilityId.PrefabCatalog, "catalog_capture_not_published");
                if (CurrentScan?.State == ScanState.Running)
                    FailCensusScan("APA-CAT-002", CapabilityId.PrefabCatalog, "catalog_capture_not_published");
                return;
            }

            if (CurrentScan?.State == ScanState.Running && CurrentScan.Stage == ScanStage.CapturingCatalog)
                CurrentScan.TransitionTo(ScanStage.ProcessingCatalog);
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
                        if (catalog.CatalogGeneration <= _catalogGenerationAtCensusStart)
                        {
                            FailCensusScan("APA-CAT-003", CapabilityId.PrefabCatalog, "catalog_generation_did_not_advance");
                            return;
                        }
                        _censusReducer = new CensusReducer(
                            catalog.PublishedRecords,
                            WorldGeneration,
                            catalog.CatalogGeneration,
                            System.DateTimeOffset.UtcNow,
                            _requestedOptions);
                        session.TransitionTo(ScanStage.CapturingObjectCensus);
                        census.BeginObjectCapture(catalog.RuntimeEntityKeys, _censusReducer, _requestedOptions);
                        return;

                    case ScanStage.CapturingObjectCensus:
                        if (!census.ObjectCaptureJobsCompleted)
                            return;
                        census.CompleteObjectCapture();
                        session.TransitionTo(ScanStage.ReducingObjectCensus);
                        ReportExactProgress(session, 0, census.CapturedObjectReferenceCount);
                        return;

                    case ScanStage.ReducingObjectCensus:
                        census.ReduceObjectSlice(CensusReductionSliceSize);
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
                        census.ReduceNetworkSlice(CensusReductionSliceSize);
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
                var failedCapability = session.Stage == ScanStage.CapturingNetworkCensus
                    || session.Stage == ScanStage.ReducingNetworkCensus
                    ? CapabilityId.NetworkEdgeCensus
                    : CapabilityId.ObjectCensus;
                var diagnostic = failedCapability == CapabilityId.NetworkEdgeCensus ? "APA-CEN-002" : "APA-CEN-001";
                FailCensusScan(diagnostic, failedCapability, "census_capture_or_reduction_failed");
            }
        }

        private void PublishCensus(ScanSession session, CensusAccess census)
        {
            if (WorldGeneration != session.WorldGeneration || _publishedState.WorldGeneration != session.WorldGeneration)
            {
                FailCensusScan("APA-CEN-003", CapabilityId.ObjectCensus, "world_generation_changed_before_publish");
                return;
            }

            var snapshot = census.BuildSnapshot();
            session.TransitionTo(ScanStage.Finalizing);
            session.ReportProgress(1, 1);
            session.Complete();
            if (!_publishedState.TryPublishCensus(snapshot, scanSucceeded: true))
            {
                LastDiagnosticCode = "APA-CEN-003";
                DegradeCapability(CapabilityId.ObjectCensus, "census_publish_world_mismatch");
            }
            else
            {
                LastDiagnosticCode = null;
            }

            census.FinishScan();
            _censusReducer = null;
            _networkCaptureStarted = false;
        }

        private void BeginCensusCancellation()
        {
            if (_catalogCaptureActive)
            {
                _catalog?.CancelCapture();
                _catalogCaptureActive = false;
            }
            _catalogScanRequested = false;
            _censusAccess?.RequestCancellation();
            _censusCleanupRequested = true;
            PollCensusCleanup();
        }

        private void FailCensusScan(string diagnosticCode, CapabilityId capability, string capabilityDetail)
        {
            LastDiagnosticCode = diagnosticCode;
            DegradeCapability(capability, capabilityDetail);
            _catalogScanRequested = false;
            if (_catalogCaptureActive)
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

        private static void ReportExactProgress(ScanSession session, long completed, long total)
        {
            if (total > 0)
                session.ReportProgress(completed, total);
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
    }
}
