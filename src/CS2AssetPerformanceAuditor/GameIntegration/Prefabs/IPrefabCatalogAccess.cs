using System.Collections.Generic;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using Unity.Entities;

namespace CS2AssetPerformanceAuditor.GameIntegration.Prefabs
{
    public interface IPrefabCatalogAccess
    {
        bool IsWorking { get; }

        bool HasPendingItems { get; }

        int CapturedEntityCount { get; }

        int ProcessedEntityCount { get; }

        int UnresolvedEntityCount { get; }

        long CatalogGeneration { get; }

        IReadOnlyList<PrefabRecord> PublishedRecords { get; }

        IReadOnlyDictionary<Entity, PrefabKey> RuntimeEntityKeys { get; }

        void BeginCapture();

        int ProcessNextSlice(int maximumItems);

        void CancelCapture();

        void ResetForWorld();
    }
}
