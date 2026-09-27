using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using CS2AssetPerformanceAuditor.Core.Census;
using CS2AssetPerformanceAuditor.Core.Observations;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using CS2AssetPerformanceAuditor.Core.Query;
using CS2AssetPerformanceAuditor.Core.Scanning;
using CS2AssetPerformanceAuditor.GameIntegration;

namespace CS2AssetPerformanceAuditor.UI
{
    public sealed class UiSnapshotBuilder
    {
        private CensusSnapshot? _cachedCensus;
        private UiCensusCounts? _cachedCensusCounts;

        public UiSnapshot Build(
            AssetAuditSystem? auditSystem,
            AssetPage assetPage,
            ScanOptions scanOptions,
            string modVersion)
        {
            if (assetPage == null)
                throw new ArgumentNullException(nameof(assetPage));
            if (scanOptions == null)
                throw new ArgumentNullException(nameof(scanOptions));
            if (string.IsNullOrWhiteSpace(modVersion))
                throw new ArgumentException("A mod version is required.", nameof(modVersion));

            var session = auditSystem?.CurrentScan;
            var progress = session?.Progress;
            var census = auditSystem?.PublishedCensus;
            var capabilities = auditSystem?.Capabilities;
            var catalog = auditSystem?.CatalogRecords ?? Array.Empty<PrefabRecord>();
            var catalogGeneration = auditSystem?.CatalogGeneration ?? 0;
            var catalogCapturedAt = auditSystem != null && catalogGeneration > 0
                ? FormatTime(auditSystem.CatalogCapturedAt)
                : null;

            return new UiSnapshot
            {
                ScanStatus = new UiScanStatus
                {
                    State = session?.State.ToString() ?? ScanState.Idle.ToString(),
                    Stage = FormatStage(session?.Stage ?? ScanStage.Idle),
                    StageNumber = progress?.StageNumber ?? 0,
                    TotalStages = progress?.TotalStages ?? 8,
                    CompletedItems = progress?.CompletedItems,
                    TotalItems = progress?.TotalItems
                },
                Summary = new UiSummary
                {
                    GameVersion = capabilities?.GameVersion ?? Core.ProjectInfo.TargetGameVersion,
                    ModVersion = modVersion,
                    Compatibility = capabilities?.Compatibility.ToString() ?? "Untested",
                    Capabilities = capabilities?.Capabilities.Select(capability => new UiCapability
                    {
                        Id = capability.Id.ToString(),
                        State = capability.State.ToString(),
                        Detail = capability.Detail
                    }).ToArray() ?? new UiCapability[0],
                    CatalogCount = catalog.Count,
                    CatalogGeneration = catalogGeneration,
                    CatalogCapturedAt = catalogCapturedAt,
                    CensusWasScanned = census != null,
                    CensusCapturedAt = census == null ? null : FormatTime(census.CapturedAt),
                    CensusCatalogGeneration = census?.CatalogGeneration,
                    CensusMatchesCatalog = census != null && census.CatalogGeneration == catalogGeneration,
                    QueryProfileVersion = census?.QueryProfileVersion,
                    CensusCounts = GetCensusCounts(census)
                },
                AssetPage = MapPage(assetPage),
                Settings = new UiScanOptions
                {
                    CollectSubordinateObjects = scanOptions.CollectSubordinateObjects,
                    CollectNetworkEdges = scanOptions.CollectNetworkEdges
                }
            };
        }

        public static string Serialize<T>(T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static bool TryDeserialize<T>(string? json, out T value) where T : class
        {
            value = null!;
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    value = serializer.ReadObject(stream) as T ?? null!;
                return value != null;
            }
            catch
            {
                return false;
            }
        }

        private UiCensusCounts GetCensusCounts(CensusSnapshot? census)
        {
            if (ReferenceEquals(census, _cachedCensus) && _cachedCensusCounts != null)
                return _cachedCensusCounts;

            _cachedCensus = census;
            _cachedCensusCounts = new UiCensusCounts
            {
                TopLevelObjects = Aggregate(census, entry => entry.Counters.TopLevelObjects),
                SubordinateObjects = Aggregate(census, entry => entry.Counters.SubordinateObjects),
                LiveObjectReferences = Aggregate(census, entry => entry.Counters.LiveObjectReferences),
                NetworkEdges = Aggregate(census, entry => entry.Counters.NetworkEdges)
            };
            return _cachedCensusCounts;
        }

        private static UiCensusCounts MapCounters(CensusEntry? entry)
        {
            return new UiCensusCounts
            {
                TopLevelObjects = entry == null ? Unscanned() : MapObservation(entry.Counters.TopLevelObjects),
                SubordinateObjects = entry == null ? Unscanned() : MapObservation(entry.Counters.SubordinateObjects),
                LiveObjectReferences = entry == null ? Unscanned() : MapObservation(entry.Counters.LiveObjectReferences),
                NetworkEdges = entry == null ? Unscanned() : MapObservation(entry.Counters.NetworkEdges)
            };
        }

        private static UiAssetPage MapPage(AssetPage page)
        {
            return new UiAssetPage
            {
                Offset = page.Offset,
                Limit = page.Limit,
                TotalCount = page.TotalCount,
                Items = page.Items.Select(item => new UiAssetRow
                {
                    PrefabId = item.Asset.Key.PrefabId,
                    PrefabType = item.Asset.Key.PrefabType,
                    DisplayName = item.Asset.DisplayName,
                    SourceLabel = GetSourceLabel(item.Asset.OriginEvidence),
                    Traits = item.Asset.Traits.ToString().Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries),
                    CountKind = item.CountKind.ToString(),
                    Instances = MapObservation(item.Instances),
                    Presence = item.Presence.ToString(),
                    Counters = MapCounters(item.CensusEntry)
                }).ToArray()
            };
        }

        private static UiObservation Aggregate(CensusSnapshot? census, Func<CensusEntry, Observation<long>> select)
        {
            if (census == null)
                return Unscanned();

            long total = 0;
            var foundApplicable = false;
            foreach (var entry in census.Entries)
            {
                var observation = select(entry);
                if (observation.Availability == Availability.NotApplicable)
                    continue;

                foundApplicable = true;
                if (!observation.HasValue)
                    return MapObservation(observation);
                total = checked(total + observation.Value);
            }

            if (!foundApplicable)
                return new UiObservation
                {
                    Availability = Availability.NotApplicable.ToString(),
                    Origin = ObservationOrigin.Derived.ToString(),
                    CapturedAt = FormatTime(census.CapturedAt)
                };

            return new UiObservation
            {
                Availability = Availability.Available.ToString(),
                Value = total,
                Origin = ObservationOrigin.Derived.ToString(),
                CapturedAt = FormatTime(census.CapturedAt)
            };
        }

        private static UiObservation MapObservation(Observation<long> observation)
        {
            return new UiObservation
            {
                Availability = observation.Availability.ToString(),
                Value = observation.HasValue ? observation.Value : (long?)null,
                Origin = observation.Origin.ToString(),
                CapturedAt = FormatTime(observation.CapturedAt),
                DiagnosticCode = observation.DiagnosticCode
            };
        }

        private static UiObservation Unscanned()
        {
            return new UiObservation
            {
                Availability = Availability.NotScanned.ToString(),
                Origin = ObservationOrigin.Ecs.ToString()
            };
        }

        private static string GetSourceLabel(AssetOriginEvidence evidence)
        {
            var labels = new List<string>();
            if (evidence.IsBuiltin == true) labels.Add("Built-in");
            if (evidence.IsSubscribedMod == true) labels.Add("Subscribed mod");
            if (evidence.IsPackaged == true) labels.Add("Packaged");
            if (labels.Count > 0) return string.Join(" + ", labels);
            if (evidence.IsBuiltin == false && evidence.IsSubscribedMod == false && evidence.IsPackaged == false)
                return "No source flags";
            return "Unknown";
        }

        private static string FormatTime(DateTimeOffset value) => value.ToUniversalTime().ToString("O");

        private static string FormatStage(ScanStage stage)
        {
            switch (stage)
            {
                case ScanStage.Preparing: return "Preparing";
                case ScanStage.CapturingCatalog: return "Capturing Prefab catalog";
                case ScanStage.ProcessingCatalog: return "Processing catalog";
                case ScanStage.CapturingObjectCensus: return "Capturing object census";
                case ScanStage.ReducingObjectCensus: return "Reducing object census";
                case ScanStage.CapturingNetworkCensus: return "Capturing network census";
                case ScanStage.ReducingNetworkCensus: return "Reducing network census";
                case ScanStage.Finalizing: return "Finalizing";
                case ScanStage.Completed: return "Complete";
                default: return "Idle";
            }
        }
    }
}
