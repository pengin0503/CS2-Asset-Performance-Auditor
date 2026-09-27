namespace CS2AssetPerformanceAuditor.Core.Scanning
{
    public enum ScanState
    {
        Idle,
        Running,
        CancellationRequested,
        Cancelled,
        Failed,
        Completed
    }
}
