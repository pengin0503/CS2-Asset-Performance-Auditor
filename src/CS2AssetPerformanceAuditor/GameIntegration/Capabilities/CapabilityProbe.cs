using System;
using System.Collections.Generic;
using CS2AssetPerformanceAuditor.Core;
using CS2AssetPerformanceAuditor.Core.Capabilities;
using Game;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace CS2AssetPerformanceAuditor.GameIntegration.Capabilities
{
    public static class CapabilityProbe
    {
        public static CapabilityReport Probe(World world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            var capabilities = new List<CapabilityStatus>
            {
                ProbeIndependently(CapabilityId.PrefabCatalog, () => ProbePrefabCatalog(world)),
                ProbeIndependently(CapabilityId.ObjectCensus, () => ProbeQuery(world, CapabilityId.ObjectCensus,
                    ComponentType.ReadOnly<Game.Objects.Object>(), ComponentType.ReadOnly<PrefabRef>())),
                ProbeIndependently(CapabilityId.NetworkEdgeCensus, () => ProbeQuery(world, CapabilityId.NetworkEdgeCensus,
                    ComponentType.ReadOnly<Game.Net.Edge>(), ComponentType.ReadOnly<PrefabRef>())),
                new CapabilityStatus(CapabilityId.GeometryMetadata, CapabilityState.Supported, "geometry_api_contract_available"),
                new CapabilityStatus(CapabilityId.SubmeshMetadata, CapabilityState.Supported, "geometry_api_contract_available"),
                new CapabilityStatus(CapabilityId.SurfaceMetadata, CapabilityState.Supported, "surface_api_contract_available"),
                new CapabilityStatus(CapabilityId.TextureMetadata, CapabilityState.Supported, "texture_api_contract_available"),
                new CapabilityStatus(CapabilityId.ShaderDeepInspection, CapabilityState.Degraded, "deep_shader_inspection_not_implemented"),
                new CapabilityStatus(CapabilityId.RuntimeGpuResidency, CapabilityState.Unsupported, "runtime_gpu_residency_is_out_of_scope")
            };

            var version = Application.version;
            if (string.IsNullOrWhiteSpace(version))
                version = ProjectInfo.TargetGameVersion;

            return new CapabilityReport(version, CompatibilityState.Untested, capabilities);
        }

        private static CapabilityStatus ProbePrefabCatalog(World world)
        {
            return world.GetExistingSystemManaged<PrefabSystem>() != null
                ? new CapabilityStatus(CapabilityId.PrefabCatalog, CapabilityState.Supported, "prefab_system_available")
                : new CapabilityStatus(CapabilityId.PrefabCatalog, CapabilityState.Degraded, "prefab_system_not_present_in_current_world");
        }

        private static CapabilityStatus ProbeQuery(World world, CapabilityId capability, params ComponentType[] required)
        {
            var query = world.EntityManager.CreateEntityQuery(required);
            query.Dispose();
            return new CapabilityStatus(capability, CapabilityState.Supported, "required_component_query_available");
        }

        private static CapabilityStatus ProbeIndependently(CapabilityId capability, Func<CapabilityStatus> probe)
        {
            try
            {
                return probe();
            }
            catch (Exception exception)
            {
                return new CapabilityStatus(capability, CapabilityState.Degraded, "probe_failed_" + exception.GetType().Name);
            }
        }
    }
}
