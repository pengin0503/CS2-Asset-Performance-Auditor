namespace CS2AssetPerformanceAuditor.Core.Scanning
{
    public enum ScanStage
    {
        Idle,
        Preparing,
        CapturingCatalog,
        ProcessingCatalog,
        CapturingObjectCensus,
        ReducingObjectCensus,
        CapturingNetworkCensus,
        ReducingNetworkCensus,
        Finalizing,
        Completed
    }
}
