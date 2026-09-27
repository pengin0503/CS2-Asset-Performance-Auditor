using System;

namespace CS2AssetPerformanceAuditor.Core.Scanning
{
    public sealed class ScanSession
    {
        private const int TotalStages = 8;

        private ScanSession(ScanKind kind, long worldGeneration, DateTimeOffset startedAt)
        {
            Kind = kind;
            WorldGeneration = worldGeneration;
            StartedAt = startedAt;
            State = ScanState.Running;
            Stage = ScanStage.Preparing;
            Progress = CreateIndeterminateProgress(Stage);
        }

        public ScanKind Kind { get; }

        public long WorldGeneration { get; }

        public DateTimeOffset StartedAt { get; }

        public ScanState State { get; private set; }

        public ScanStage Stage { get; private set; }

        public ScanProgress Progress { get; private set; }

        public string? DiagnosticCode { get; private set; }

        public bool CanPublish => State == ScanState.Running && Stage == ScanStage.Finalizing;

        public static ScanSession Start(ScanKind kind, long worldGeneration, DateTimeOffset startedAt)
        {
            if (!Enum.IsDefined(typeof(ScanKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            return new ScanSession(kind, worldGeneration, startedAt);
        }

        public void TransitionTo(ScanStage nextStage)
        {
            EnsureRunning();
            var expected = GetNextStage(Stage);
            if (nextStage != expected || nextStage == ScanStage.Completed)
                throw new InvalidOperationException("Scan stages must advance in the Phase 1 order; use Complete after Finalizing.");

            Stage = nextStage;
            Progress = CreateIndeterminateProgress(Stage);
        }

        public void ReportProgress(long? completedItems, long? totalItems)
        {
            EnsureRunning();
            Progress = new ScanProgress(Stage, GetStageNumber(Stage), TotalStages, completedItems, totalItems);
        }

        public void RequestCancellation()
        {
            if (State == ScanState.Running)
                State = ScanState.CancellationRequested;
        }

        public void MarkCancelled()
        {
            if (State != ScanState.CancellationRequested)
                throw new InvalidOperationException("Cancellation can complete only after a cancellation request.");

            State = ScanState.Cancelled;
        }

        public void Fail(string diagnosticCode)
        {
            if (State != ScanState.Running && State != ScanState.CancellationRequested)
                throw new InvalidOperationException("Only an active scan can fail.");
            if (string.IsNullOrWhiteSpace(diagnosticCode))
                throw new ArgumentException("A stable diagnostic code is required.", nameof(diagnosticCode));

            DiagnosticCode = diagnosticCode;
            State = ScanState.Failed;
        }

        public void Complete()
        {
            if (!CanPublish)
                throw new InvalidOperationException("A scan can complete only after successful Finalizing.");

            State = ScanState.Completed;
            Stage = ScanStage.Completed;
            Progress = new ScanProgress(ScanStage.Completed, TotalStages, TotalStages, 1, 1);
        }

        private void EnsureRunning()
        {
            if (State != ScanState.Running)
                throw new InvalidOperationException("The scan is not accepting work or progress updates.");
        }

        private static ScanStage GetNextStage(ScanStage stage)
        {
            switch (stage)
            {
                case ScanStage.Preparing: return ScanStage.CapturingCatalog;
                case ScanStage.CapturingCatalog: return ScanStage.ProcessingCatalog;
                case ScanStage.ProcessingCatalog: return ScanStage.CapturingObjectCensus;
                case ScanStage.CapturingObjectCensus: return ScanStage.ReducingObjectCensus;
                case ScanStage.ReducingObjectCensus: return ScanStage.CapturingNetworkCensus;
                case ScanStage.CapturingNetworkCensus: return ScanStage.ReducingNetworkCensus;
                case ScanStage.ReducingNetworkCensus: return ScanStage.Finalizing;
                default: throw new InvalidOperationException("The scan is already in a terminal or final stage.");
            }
        }

        private static int GetStageNumber(ScanStage stage)
        {
            switch (stage)
            {
                case ScanStage.Idle: return 1;
                case ScanStage.Preparing: return 1;
                case ScanStage.CapturingCatalog: return 2;
                case ScanStage.ProcessingCatalog: return 3;
                case ScanStage.CapturingObjectCensus: return 4;
                case ScanStage.ReducingObjectCensus: return 5;
                case ScanStage.CapturingNetworkCensus: return 6;
                case ScanStage.ReducingNetworkCensus: return 7;
                case ScanStage.Finalizing: return 8;
                case ScanStage.Completed: return TotalStages;
                default: throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }

        private static ScanProgress CreateIndeterminateProgress(ScanStage stage)
        {
            return CreateIndeterminateProgress(stage, GetStageNumber(stage));
        }

        private static ScanProgress CreateIndeterminateProgress(ScanStage stage, int stageNumber)
        {
            return new ScanProgress(stage, stageNumber, TotalStages, null, null);
        }
    }
}
