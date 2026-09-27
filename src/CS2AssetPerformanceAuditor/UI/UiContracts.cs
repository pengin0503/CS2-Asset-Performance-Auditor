using System.Runtime.Serialization;

namespace CS2AssetPerformanceAuditor.UI
{
    public static class UiBindingContract
    {
        public const string Group = "CS2AssetPerformanceAuditor";
        public const string Snapshot = "snapshot";
        public const string ExportedReport = "exportedReport";
        public const string RequestCensus = "requestCensus";
        public const string CancelCensus = "cancelCensus";
        public const string QueryAssets = "queryAssets";
        public const string RequestExport = "requestExport";
        public const string UpdateSettings = "updateSettings";
    }

    [DataContract]
    public sealed class UiSnapshot
    {
        [DataMember(Name = "scanStatus", Order = 1)] public UiScanStatus ScanStatus { get; set; } = new UiScanStatus();
        [DataMember(Name = "summary", Order = 2)] public UiSummary Summary { get; set; } = new UiSummary();
        [DataMember(Name = "assetPage", Order = 3)] public UiAssetPage AssetPage { get; set; } = new UiAssetPage();
        [DataMember(Name = "settings", Order = 4)] public UiScanOptions Settings { get; set; } = new UiScanOptions();
    }

    [DataContract]
    public sealed class UiScanStatus
    {
        [DataMember(Name = "state", Order = 1)] public string State { get; set; } = "Idle";
        [DataMember(Name = "stage", Order = 2)] public string Stage { get; set; } = "Idle";
        [DataMember(Name = "stageNumber", Order = 3)] public int StageNumber { get; set; }
        [DataMember(Name = "totalStages", Order = 4)] public int TotalStages { get; set; } = 8;
        [DataMember(Name = "completedItems", Order = 5, EmitDefaultValue = true)] public long? CompletedItems { get; set; }
        [DataMember(Name = "totalItems", Order = 6, EmitDefaultValue = true)] public long? TotalItems { get; set; }
    }

    [DataContract]
    public sealed class UiSummary
    {
        [DataMember(Name = "gameVersion", Order = 1)] public string GameVersion { get; set; } = "Unknown";
        [DataMember(Name = "modVersion", Order = 2)] public string ModVersion { get; set; } = string.Empty;
        [DataMember(Name = "compatibility", Order = 3)] public string Compatibility { get; set; } = "Untested";
        [DataMember(Name = "capabilities", Order = 4)] public UiCapability[] Capabilities { get; set; } = new UiCapability[0];
        [DataMember(Name = "catalogCount", Order = 5)] public int CatalogCount { get; set; }
        [DataMember(Name = "catalogGeneration", Order = 6)] public long CatalogGeneration { get; set; }
        [DataMember(Name = "catalogCapturedAt", Order = 7, EmitDefaultValue = true)] public string? CatalogCapturedAt { get; set; }
        [DataMember(Name = "censusWasScanned", Order = 8)] public bool CensusWasScanned { get; set; }
        [DataMember(Name = "censusCapturedAt", Order = 9, EmitDefaultValue = true)] public string? CensusCapturedAt { get; set; }
        [DataMember(Name = "censusCatalogGeneration", Order = 10, EmitDefaultValue = true)] public long? CensusCatalogGeneration { get; set; }
        [DataMember(Name = "censusMatchesCatalog", Order = 11)] public bool CensusMatchesCatalog { get; set; }
        [DataMember(Name = "queryProfileVersion", Order = 12, EmitDefaultValue = true)] public string? QueryProfileVersion { get; set; }
        [DataMember(Name = "censusCounts", Order = 13)] public UiCensusCounts CensusCounts { get; set; } = new UiCensusCounts();
    }

    [DataContract]
    public sealed class UiCapability
    {
        [DataMember(Name = "id", Order = 1)] public string Id { get; set; } = string.Empty;
        [DataMember(Name = "state", Order = 2)] public string State { get; set; } = string.Empty;
        [DataMember(Name = "detail", Order = 3, EmitDefaultValue = true)] public string? Detail { get; set; }
    }

    [DataContract]
    public sealed class UiCensusCounts
    {
        [DataMember(Name = "topLevelObjects", Order = 1)] public UiObservation TopLevelObjects { get; set; } = new UiObservation();
        [DataMember(Name = "subordinateObjects", Order = 2)] public UiObservation SubordinateObjects { get; set; } = new UiObservation();
        [DataMember(Name = "liveObjectReferences", Order = 3)] public UiObservation LiveObjectReferences { get; set; } = new UiObservation();
        [DataMember(Name = "networkEdges", Order = 4)] public UiObservation NetworkEdges { get; set; } = new UiObservation();
    }

    [DataContract]
    public sealed class UiObservation
    {
        [DataMember(Name = "availability", Order = 1)] public string Availability { get; set; } = "NotScanned";
        [DataMember(Name = "value", Order = 2, EmitDefaultValue = true)] public long? Value { get; set; }
        [DataMember(Name = "origin", Order = 3, EmitDefaultValue = true)] public string? Origin { get; set; }
        [DataMember(Name = "capturedAt", Order = 4, EmitDefaultValue = true)] public string? CapturedAt { get; set; }
        [DataMember(Name = "diagnosticCode", Order = 5, EmitDefaultValue = true)] public string? DiagnosticCode { get; set; }
    }

    [DataContract]
    public sealed class UiAssetPage
    {
        [DataMember(Name = "offset", Order = 1)] public int Offset { get; set; }
        [DataMember(Name = "limit", Order = 2)] public int Limit { get; set; }
        [DataMember(Name = "totalCount", Order = 3)] public int TotalCount { get; set; }
        [DataMember(Name = "items", Order = 4)] public UiAssetRow[] Items { get; set; } = new UiAssetRow[0];
    }

    [DataContract]
    public sealed class UiAssetRow
    {
        [DataMember(Name = "prefabId", Order = 1)] public string PrefabId { get; set; } = string.Empty;
        [DataMember(Name = "prefabType", Order = 2)] public string PrefabType { get; set; } = string.Empty;
        [DataMember(Name = "displayName", Order = 3)] public string DisplayName { get; set; } = string.Empty;
        [DataMember(Name = "sourceLabel", Order = 4)] public string SourceLabel { get; set; } = string.Empty;
        [DataMember(Name = "traits", Order = 5)] public string[] Traits { get; set; } = new string[0];
        [DataMember(Name = "countKind", Order = 6)] public string CountKind { get; set; } = "None";
        [DataMember(Name = "instances", Order = 7)] public UiObservation Instances { get; set; } = new UiObservation();
        [DataMember(Name = "presence", Order = 8)] public string Presence { get; set; } = "Unknown";
        [DataMember(Name = "counters", Order = 9, EmitDefaultValue = true)] public UiCensusCounts? Counters { get; set; }
    }

    [DataContract]
    public sealed class UiScanOptions
    {
        [DataMember(Name = "collectSubordinateObjects", Order = 1)] public bool CollectSubordinateObjects { get; set; } = true;
        [DataMember(Name = "collectNetworkEdges", Order = 2)] public bool CollectNetworkEdges { get; set; } = true;
    }

    [DataContract]
    public sealed class UiAssetQueryRequest
    {
        [DataMember(Name = "searchText", Order = 1, EmitDefaultValue = true)] public string? SearchText { get; set; }
        [DataMember(Name = "traitFilter", Order = 2, EmitDefaultValue = true)] public string? TraitFilter { get; set; }
        [DataMember(Name = "sourceFilter", Order = 3, EmitDefaultValue = true)] public string? SourceFilter { get; set; }
        [DataMember(Name = "presenceFilter", Order = 4, EmitDefaultValue = true)] public string? PresenceFilter { get; set; }
        [DataMember(Name = "sort", Order = 5, EmitDefaultValue = true)] public string? Sort { get; set; }
        [DataMember(Name = "offset", Order = 6)] public int Offset { get; set; }
        [DataMember(Name = "limit", Order = 7)] public int Limit { get; set; } = 100;
    }
}
