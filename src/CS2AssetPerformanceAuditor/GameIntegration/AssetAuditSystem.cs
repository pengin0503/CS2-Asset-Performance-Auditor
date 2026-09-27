using CS2AssetPerformanceAuditor.Core.Capabilities;
using CS2AssetPerformanceAuditor.GameIntegration.Capabilities;
using Game;

namespace CS2AssetPerformanceAuditor.GameIntegration
{
    public sealed class AssetAuditSystem : GameSystemBase
    {
        public CapabilityReport? Capabilities { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Capabilities = CapabilityProbe.Probe(World);
        }

        protected override void OnUpdate()
        {
            // Scan coordinators will attach here; the compatibility foundation stays idle.
        }
    }
}
