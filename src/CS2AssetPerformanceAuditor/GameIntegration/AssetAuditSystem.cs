using System.Collections.Generic;
using CS2AssetPerformanceAuditor.Core.Capabilities;
using CS2AssetPerformanceAuditor.Core.Prefabs;
using CS2AssetPerformanceAuditor.GameIntegration.Capabilities;
using CS2AssetPerformanceAuditor.GameIntegration.Prefabs;
using Game;
using Unity.Entities;

namespace CS2AssetPerformanceAuditor.GameIntegration
{
    public sealed class AssetAuditSystem : GameSystemBase
    {
        private const int CatalogSliceSize = 64;
        private IPrefabCatalogAccess? _catalog;
        private bool _catalogScanRequested;

        public CapabilityReport? Capabilities { get; private set; }

        public long CatalogGeneration => _catalog?.CatalogGeneration ?? 0;

        public int CatalogCapturedEntityCount => _catalog?.CapturedEntityCount ?? 0;

        public int CatalogProcessedEntityCount => _catalog?.ProcessedEntityCount ?? 0;

        public int CatalogUnresolvedEntityCount => _catalog?.UnresolvedEntityCount ?? 0;

        public IReadOnlyList<PrefabRecord> CatalogRecords => _catalog?.PublishedRecords ?? System.Array.Empty<PrefabRecord>();

        public IReadOnlyDictionary<Entity, PrefabKey> RuntimeEntityKeys => _catalog?.RuntimeEntityKeys
            ?? new Dictionary<Entity, PrefabKey>();

        public void RequestCatalogScan()
        {
            _catalogScanRequested = true;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Capabilities = CapabilityProbe.Probe(World);
            _catalog = new PrefabCatalogAccess(World);
        }

        protected override void OnUpdate()
        {
            if (_catalog == null)
                return;

            if (_catalogScanRequested && !_catalog.IsWorking)
            {
                _catalogScanRequested = false;
                try
                {
                    _catalog.BeginCapture();
                }
                catch
                {
                    _catalog.CancelCapture();
                }
            }

            if (_catalog.IsWorking)
                _catalog.ProcessNextSlice(CatalogSliceSize);
        }
    }
}
