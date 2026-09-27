using System;
using CS2AssetPerformanceAuditor.Core.Census;

namespace CS2AssetPerformanceAuditor.Core.Scanning
{
    public sealed class PublishedAuditState
    {
        public long? WorldGeneration { get; private set; }

        public CensusSnapshot? Census { get; private set; }

        public void ResetForWorld(long worldGeneration)
        {
            WorldGeneration = worldGeneration;
            Census = null;
        }

        public bool TryPublishCensus(CensusSnapshot snapshot, bool scanSucceeded)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (!scanSucceeded || !WorldGeneration.HasValue || snapshot.WorldGeneration != WorldGeneration.Value)
                return false;

            Census = snapshot;
            return true;
        }
    }
}
