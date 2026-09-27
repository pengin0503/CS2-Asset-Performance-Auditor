using System.Collections.Generic;
using System;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using Unity.Entities;

namespace CS2AssetPerformanceAuditor.GameIntegration.Prefabs
{
    public interface IPrefabCatalogAccess
    {
        bool IsWorking { get; }

        bool HasPendingItems { get; }

        bool HasPendingPublication { get; }

        int CapturedEntityCount { get; }

        int ProcessedEntityCount { get; }

        int UnresolvedEntityCount { get; }

        long CatalogGeneration { get; }

        long PendingCatalogGeneration { get; }

        DateTimeOffset CatalogCapturedAt { get; }

        DateTimeOffset PendingCapturedAt { get; }

        IReadOnlyList<PrefabRecord> PublishedRecords { get; }

        IReadOnlyDictionary<Entity, PrefabKey> RuntimeEntityKeys { get; }

        IReadOnlyList<PrefabRecord> PendingRecords { get; }

        IReadOnlyDictionary<Entity, PrefabKey> PendingRuntimeEntityKeys { get; }

        void BeginCapture(bool deferPublication = false);

        int ProcessNextSlice(int maximumItems);

        void CommitPendingCapture();

        void DiscardPendingCapture();

        void CancelCapture();

        void ResetForWorld();
    }
}
