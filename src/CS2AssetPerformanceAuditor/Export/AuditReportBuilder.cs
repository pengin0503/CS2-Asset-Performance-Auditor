using System;
using System.Collections.Generic;
using System.Linq;
using CS2AssetPerformanceAuditor.Core.Capabilities;
using CS2AssetPerformanceAuditor.Core.Census;
using CS2AssetPerformanceAuditor.Core.Diagnostics;
using CS2AssetPerformanceAuditor.Core.Findings;
using CS2AssetPerformanceAuditor.Core.Observations;
using CS2AssetPerformanceAuditor.Core.Prefabs;

namespace CS2AssetPerformanceAuditor.Export
{
    public sealed class AuditReportBuilder
    {
        private readonly PrivacySanitizer _sanitizer;

        public AuditReportBuilder(PrivacySanitizer sanitizer)
        {
            _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
        }

        public AuditReport Build(
            IEnumerable<PrefabRecord> catalog,
            long catalogGeneration,
            DateTimeOffset? catalogCapturedAt,
            CensusSnapshot? census,
            CapabilityReport capabilities,
            string modVersion,
            DateTimeOffset generatedAt,
            IEnumerable<DiagnosticAggregate>? diagnostics = null,
            IEnumerable<Finding>? findings = null,
            ExportScope scope = ExportScope.Full,
            IEnumerable<PrefabKey>? includedKeys = null)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (capabilities == null) throw new ArgumentNullException(nameof(capabilities));
            if (string.IsNullOrWhiteSpace(modVersion)) throw new ArgumentException("A mod version is required.", nameof(modVersion));
            if (census != null && census.CatalogGeneration != catalogGeneration)
                throw new InvalidOperationException("Cannot export a Census snapshot against a different Prefab catalog generation.");
            if (!Enum.IsDefined(typeof(ExportScope), scope)) throw new ArgumentOutOfRangeException(nameof(scope));

            var records = catalog.ToArray();
            var allFindings = (findings ?? Array.Empty<Finding>()).ToArray();
            var keySet = includedKeys == null ? null : new HashSet<PrefabKey>(includedKeys);
            var scopedRecords = ScopeRecords(records, scope, keySet);
            var scopedCensus = ScopeCensus(census?.Entries ?? Array.Empty<CensusEntry>(), scope, keySet);
            var scopedFindings = ScopeFindings(allFindings, scope, keySet);

            return new AuditReport
            {
                SchemaVersion = ReportSchema.SchemaVersion,
                RuleSetVersion = ReportSchema.RuleSetVersion,
                QueryProfileVersion = census?.QueryProfileVersion,
                ScanOptions = new ReportScanOptions
                {
                    WasCensusScanned = census != null,
                    CollectSubordinateObjects = census == null ? (bool?)null : census.ScanOptions.CollectSubordinateObjects,
                    CollectNetworkEdges = census == null ? (bool?)null : census.ScanOptions.CollectNetworkEdges
                },
                ModVersion = _sanitizer.SanitizeText(modVersion),
                GameVersion = _sanitizer.SanitizeText(capabilities.GameVersion),
                GeneratedAt = FormatTime(generatedAt),
                CatalogCapturedAt = catalogCapturedAt.HasValue ? FormatTime(catalogCapturedAt.Value) : null,
                CensusCapturedAt = census == null ? null : FormatTime(census.CapturedAt),
                CatalogGeneration = catalogGeneration,
                CensusCatalogGeneration = census?.CatalogGeneration,
                WorldGeneration = census?.WorldGeneration,
                CapabilityReport = new ReportCapabilityReport
                {
                    Compatibility = capabilities.Compatibility.ToString(),
                    Capabilities = capabilities.Capabilities.Select(MapCapability).ToArray()
                },
                Catalog = scopedRecords
                    .OrderBy(record => record.Key.PrefabType, StringComparer.Ordinal)
                    .ThenBy(record => record.Key.PrefabId, StringComparer.Ordinal)
                    .Select(MapPrefab).ToArray(),
                Census = scopedCensus
                    .OrderBy(entry => entry.Key.PrefabType, StringComparer.Ordinal)
                    .ThenBy(entry => entry.Key.PrefabId, StringComparer.Ordinal)
                    .Select(MapCensusEntry).ToArray(),
                Diagnostics = (diagnostics ?? Array.Empty<DiagnosticAggregate>())
                    .OrderBy(diagnostic => diagnostic.Code.Value, StringComparer.Ordinal)
                    .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
                    .Select(MapDiagnostic).ToArray(),
                ExportScope = scope.ToString(),
                Analysis = new ReportAnalysis
                {
                    Findings = scopedFindings
                        .OrderBy(finding => finding.RuleId, StringComparer.Ordinal)
                        .ThenBy(finding => finding.Title, StringComparer.Ordinal)
                        .Select(MapFinding).ToArray()
                }
            };
        }

        private static IEnumerable<PrefabRecord> ScopeRecords(IEnumerable<PrefabRecord> records, ExportScope scope, HashSet<PrefabKey>? keys)
        {
            if (scope == ExportScope.Census || scope == ExportScope.Findings) return Array.Empty<PrefabRecord>();
            if (scope == ExportScope.Filtered || scope == ExportScope.Selected)
                return keys == null ? Array.Empty<PrefabRecord>() : records.Where(record => keys.Contains(record.Key));
            return records;
        }

        private static IEnumerable<CensusEntry> ScopeCensus(IEnumerable<CensusEntry> entries, ExportScope scope, HashSet<PrefabKey>? keys)
        {
            if (scope == ExportScope.Findings) return Array.Empty<CensusEntry>();
            if (scope == ExportScope.Filtered || scope == ExportScope.Selected)
                return keys == null ? Array.Empty<CensusEntry>() : entries.Where(entry => keys.Contains(entry.Key));
            return entries;
        }

        private static IEnumerable<Finding> ScopeFindings(IEnumerable<Finding> findings, ExportScope scope, HashSet<PrefabKey>? keys)
        {
            if (scope == ExportScope.Census) return Array.Empty<Finding>();
            if (scope != ExportScope.Filtered && scope != ExportScope.Selected) return findings;
            if (keys == null) return Array.Empty<Finding>();
            var ids = new HashSet<string>(keys.Select(key => key.PrefabId), StringComparer.Ordinal);
            return findings.Where(finding => finding.Evidence.Any(evidence => evidence.StartsWith("asset=", StringComparison.Ordinal) && ids.Contains(evidence.Substring("asset=".Length))));
        }

        private ReportCapability MapCapability(CapabilityStatus status) => new ReportCapability
        {
            Id = status.Id.ToString(), State = status.State.ToString(), Detail = status.Detail == null ? null : _sanitizer.SanitizeText(status.Detail)
        };

        private ReportPrefab MapPrefab(PrefabRecord record)
        {
            var evidence = record.OriginEvidence;
            return new ReportPrefab
            {
                PrefabId = _sanitizer.SanitizeText(record.Key.PrefabId),
                PrefabType = _sanitizer.SanitizeText(record.Key.PrefabType),
                DisplayName = _sanitizer.SanitizeText(record.DisplayName),
                Traits = record.Traits.ToString(),
                IsBuiltin = evidence.IsBuiltin,
                IsSubscribedMod = evidence.IsSubscribedMod,
                IsPackaged = evidence.IsPackaged,
                DlcPrerequisiteIds = SanitizeIdentifiers(evidence.DlcPrerequisiteIds),
                AssetPackMembership = SanitizeIdentifiers(evidence.AssetPackMembership),
                AssetDatabaseSource = evidence.AssetDatabaseSource == null ? null : _sanitizer.SanitizeText(evidence.AssetDatabaseSource),
                ParadoxModsPlatformId = evidence.ParadoxModsPlatformId == null ? null : _sanitizer.SanitizeText(evidence.ParadoxModsPlatformId)
            };
        }

        private ReportCensusEntry MapCensusEntry(CensusEntry entry)
        {
            var counters = entry.Counters;
            return new ReportCensusEntry
            {
                PrefabId = _sanitizer.SanitizeText(entry.Key.PrefabId),
                PrefabType = _sanitizer.SanitizeText(entry.Key.PrefabType),
                Presence = entry.Presence.ToString(),
                Counters = new ReportCensusCounters
                {
                    TopLevelObjects = MapObservation(counters.TopLevelObjects),
                    SubordinateObjects = MapObservation(counters.SubordinateObjects),
                    LiveObjectReferences = MapObservation(counters.LiveObjectReferences),
                    NetworkEdges = MapObservation(counters.NetworkEdges)
                }
            };
        }

        private ReportFinding MapFinding(Finding finding) => new ReportFinding
        {
            RuleId = _sanitizer.SanitizeText(finding.RuleId),
            Status = finding.Status.ToString(),
            Category = finding.Category.ToString(),
            Title = _sanitizer.SanitizeText(finding.Title),
            Explanation = _sanitizer.SanitizeText(finding.Explanation),
            Evidence = finding.Evidence.Select(_sanitizer.SanitizeText).ToArray(),
            Basis = finding.Basis.ToString(),
            RuleVersion = _sanitizer.SanitizeText(finding.RuleVersion)
        };

        private ReportObservation MapObservation(Observation<long> observation) => new ReportObservation
        {
            Availability = observation.Availability.ToString(),
            Origin = observation.Origin.ToString(),
            CapturedAt = FormatTime(observation.CapturedAt),
            Value = observation.HasValue ? observation.Value : (long?)null,
            DiagnosticCode = observation.DiagnosticCode == null ? null : _sanitizer.SanitizeText(observation.DiagnosticCode)
        };

        private ReportDiagnostic MapDiagnostic(DiagnosticAggregate diagnostic) => new ReportDiagnostic
        {
            Code = diagnostic.Code.Value,
            Message = _sanitizer.SanitizeText(diagnostic.Message),
            Count = diagnostic.Count,
            FirstSeenAt = FormatTime(diagnostic.FirstSeenAt),
            LastSeenAt = FormatTime(diagnostic.LastSeenAt)
        };

        private string[]? SanitizeIdentifiers(IReadOnlyList<string>? identifiers) => identifiers?.Select(_sanitizer.SanitizeText).ToArray();
        private static string FormatTime(DateTimeOffset value) => value.ToUniversalTime().ToString("O");
    }
}
