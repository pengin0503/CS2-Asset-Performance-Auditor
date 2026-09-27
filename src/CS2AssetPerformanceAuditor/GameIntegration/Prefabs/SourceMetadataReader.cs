using System;
using System.Collections.Generic;
using CS2AssetPerformanceAuditor.Core.Prefabs;

namespace CS2AssetPerformanceAuditor.GameIntegration.Prefabs
{
    public static class SourceMetadataReader
    {
        public static AssetOriginEvidence Read(
            bool? isBuiltin,
            bool? isSubscribedMod,
            bool? isPackaged,
            IEnumerable<string>? dlcPrerequisiteIds = null,
            IEnumerable<string>? assetPackMembership = null,
            string? assetDatabaseSource = null,
            string? paradoxModsPlatformId = null)
        {
            return new AssetOriginEvidence(
                isBuiltin,
                isSubscribedMod,
                isPackaged,
                dlcPrerequisiteIds,
                assetPackMembership,
                assetDatabaseSource,
                paradoxModsPlatformId);
        }
    }
}
