using System.Runtime.Serialization;

namespace CS2AssetPerformanceAuditor.Export
{
    public enum ExportScope
    {
        Full,
        Filtered,
        Selected,
        Census,
        Findings
    }

    [DataContract]
    public sealed class AuditReport
    {
        [DataMember(Name = "schemaVersion", Order = 1)] public string SchemaVersion { get; set; } = string.Empty;
        [DataMember(Name = "ruleSetVersion", Order = 2)] public string RuleSetVersion { get; set; } = string.Empty;
        [DataMember(Name = "queryProfileVersion", Order = 3, EmitDefaultValue = true)] public string? QueryProfileVersion { get; set; }
        [DataMember(Name = "scanOptions", Order = 4)] public ReportScanOptions ScanOptions { get; set; } = new ReportScanOptions();
        [DataMember(Name = "modVersion", Order = 5)] public string ModVersion { get; set; } = string.Empty;
        [DataMember(Name = "gameVersion", Order = 6)] public string GameVersion { get; set; } = string.Empty;
        [DataMember(Name = "generatedAt", Order = 7)] public string GeneratedAt { get; set; } = string.Empty;
        [DataMember(Name = "catalogCapturedAt", Order = 8, EmitDefaultValue = true)] public string? CatalogCapturedAt { get; set; }
        [DataMember(Name = "censusCapturedAt", Order = 9, EmitDefaultValue = true)] public string? CensusCapturedAt { get; set; }
        [DataMember(Name = "catalogGeneration", Order = 10)] public long CatalogGeneration { get; set; }
        [DataMember(Name = "censusCatalogGeneration", Order = 11, EmitDefaultValue = true)] public long? CensusCatalogGeneration { get; set; }
        [DataMember(Name = "worldGeneration", Order = 12, EmitDefaultValue = true)] public long? WorldGeneration { get; set; }
        [DataMember(Name = "capabilityReport", Order = 13)] public ReportCapabilityReport CapabilityReport { get; set; } = new ReportCapabilityReport();
        [DataMember(Name = "catalog", Order = 14)] public ReportPrefab[] Catalog { get; set; } = new ReportPrefab[0];
        [DataMember(Name = "census", Order = 15)] public ReportCensusEntry[] Census { get; set; } = new ReportCensusEntry[0];
        [DataMember(Name = "diagnostics", Order = 16)] public ReportDiagnostic[] Diagnostics { get; set; } = new ReportDiagnostic[0];
        [DataMember(Name = "exportScope", Order = 17)] public string ExportScope { get; set; } = "Full";
        [DataMember(Name = "analysis", Order = 18)] public ReportAnalysis Analysis { get; set; } = new ReportAnalysis();
    }

    [DataContract]
    public sealed class ReportAnalysis
    {
        [DataMember(Name = "findings", Order = 1)] public ReportFinding[] Findings { get; set; } = new ReportFinding[0];
    }

    [DataContract]
    public sealed class ReportFinding
    {
        [DataMember(Name = "ruleId", Order = 1)] public string RuleId { get; set; } = string.Empty;
        [DataMember(Name = "status", Order = 2)] public string Status { get; set; } = string.Empty;
        [DataMember(Name = "category", Order = 3)] public string Category { get; set; } = string.Empty;
        [DataMember(Name = "title", Order = 4)] public string Title { get; set; } = string.Empty;
        [DataMember(Name = "explanation", Order = 5)] public string Explanation { get; set; } = string.Empty;
        [DataMember(Name = "evidence", Order = 6)] public string[] Evidence { get; set; } = new string[0];
        [DataMember(Name = "basis", Order = 7)] public string Basis { get; set; } = string.Empty;
        [DataMember(Name = "ruleVersion", Order = 8)] public string RuleVersion { get; set; } = string.Empty;
    }

    [DataContract] public sealed class ReportCapabilityReport
    {
        [DataMember(Name = "compatibility", Order = 1)] public string Compatibility { get; set; } = string.Empty;
        [DataMember(Name = "capabilities", Order = 2)] public ReportCapability[] Capabilities { get; set; } = new ReportCapability[0];
    }

    [DataContract] public sealed class ReportScanOptions
    {
        [DataMember(Name = "wasCensusScanned", Order = 1)] public bool WasCensusScanned { get; set; }
        [DataMember(Name = "collectSubordinateObjects", Order = 2, EmitDefaultValue = true)] public bool? CollectSubordinateObjects { get; set; }
        [DataMember(Name = "collectNetworkEdges", Order = 3, EmitDefaultValue = true)] public bool? CollectNetworkEdges { get; set; }
    }

    [DataContract] public sealed class ReportCapability
    {
        [DataMember(Name = "id", Order = 1)] public string Id { get; set; } = string.Empty;
        [DataMember(Name = "state", Order = 2)] public string State { get; set; } = string.Empty;
        [DataMember(Name = "detail", Order = 3, EmitDefaultValue = true)] public string? Detail { get; set; }
    }

    [DataContract] public sealed class ReportPrefab
    {
        [DataMember(Name = "prefabId", Order = 1)] public string PrefabId { get; set; } = string.Empty;
        [DataMember(Name = "prefabType", Order = 2)] public string PrefabType { get; set; } = string.Empty;
        [DataMember(Name = "displayName", Order = 3)] public string DisplayName { get; set; } = string.Empty;
        [DataMember(Name = "traits", Order = 4)] public string Traits { get; set; } = string.Empty;
        [DataMember(Name = "isBuiltin", Order = 5, EmitDefaultValue = true)] public bool? IsBuiltin { get; set; }
        [DataMember(Name = "isSubscribedMod", Order = 6, EmitDefaultValue = true)] public bool? IsSubscribedMod { get; set; }
        [DataMember(Name = "isPackaged", Order = 7, EmitDefaultValue = true)] public bool? IsPackaged { get; set; }
        [DataMember(Name = "dlcPrerequisiteIds", Order = 8, EmitDefaultValue = true)] public string[]? DlcPrerequisiteIds { get; set; }
        [DataMember(Name = "assetPackMembership", Order = 9, EmitDefaultValue = true)] public string[]? AssetPackMembership { get; set; }
        [DataMember(Name = "assetDatabaseSource", Order = 10, EmitDefaultValue = true)] public string? AssetDatabaseSource { get; set; }
        [DataMember(Name = "paradoxModsPlatformId", Order = 11, EmitDefaultValue = true)] public string? ParadoxModsPlatformId { get; set; }
    }

    [DataContract] public sealed class ReportObservation
    {
        [DataMember(Name = "availability", Order = 1)] public string Availability { get; set; } = string.Empty;
        [DataMember(Name = "origin", Order = 2)] public string Origin { get; set; } = string.Empty;
        [DataMember(Name = "capturedAt", Order = 3)] public string CapturedAt { get; set; } = string.Empty;
        [DataMember(Name = "value", Order = 4, EmitDefaultValue = true)] public long? Value { get; set; }
        [DataMember(Name = "diagnosticCode", Order = 5, EmitDefaultValue = true)] public string? DiagnosticCode { get; set; }
    }

    [DataContract] public sealed class ReportCensusCounters
    {
        [DataMember(Name = "topLevelObjects", Order = 1)] public ReportObservation TopLevelObjects { get; set; } = new ReportObservation();
        [DataMember(Name = "subordinateObjects", Order = 2)] public ReportObservation SubordinateObjects { get; set; } = new ReportObservation();
        [DataMember(Name = "liveObjectReferences", Order = 3)] public ReportObservation LiveObjectReferences { get; set; } = new ReportObservation();
        [DataMember(Name = "networkEdges", Order = 4)] public ReportObservation NetworkEdges { get; set; } = new ReportObservation();
    }

    [DataContract] public sealed class ReportCensusEntry
    {
        [DataMember(Name = "prefabId", Order = 1)] public string PrefabId { get; set; } = string.Empty;
        [DataMember(Name = "prefabType", Order = 2)] public string PrefabType { get; set; } = string.Empty;
        [DataMember(Name = "presence", Order = 3)] public string Presence { get; set; } = string.Empty;
        [DataMember(Name = "counters", Order = 4)] public ReportCensusCounters Counters { get; set; } = new ReportCensusCounters();
    }

    [DataContract] public sealed class ReportDiagnostic
    {
        [DataMember(Name = "code", Order = 1)] public string Code { get; set; } = string.Empty;
        [DataMember(Name = "message", Order = 2)] public string Message { get; set; } = string.Empty;
        [DataMember(Name = "count", Order = 3)] public int Count { get; set; }
        [DataMember(Name = "firstSeenAt", Order = 4)] public string FirstSeenAt { get; set; } = string.Empty;
        [DataMember(Name = "lastSeenAt", Order = 5)] public string LastSeenAt { get; set; } = string.Empty;
    }
}
