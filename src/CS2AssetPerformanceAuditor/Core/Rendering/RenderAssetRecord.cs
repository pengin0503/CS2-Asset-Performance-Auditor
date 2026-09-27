using System;

namespace CS2AssetPerformanceAuditor.Core.Rendering
{
    public sealed class RenderAssetRecord
    {
        public RenderAssetRecord(RenderAssetKey key, string displayName)
        {
            if (!key.IsValid) throw new ArgumentException("A stable render-asset key is required.", nameof(key));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("A render-asset display name is required.", nameof(displayName));
            Key = key;
            DisplayName = displayName;
        }

        public RenderAssetKey Key { get; }
        public string DisplayName { get; }
    }
}
