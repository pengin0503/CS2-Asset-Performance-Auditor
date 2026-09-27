using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CS2AssetPerformanceAuditor.Export
{
    public sealed class CsvSummaryExporter
    {
        public string Export(AuditReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            var census = report.Census.ToDictionary(entry => entry.PrefabType + "\n" + entry.PrefabId, StringComparer.Ordinal);
            var builder = new StringBuilder();
            builder.AppendLine("prefabId,prefabType,displayName,traits,presence,topLevelObjects,findingCount");
            foreach (var prefab in report.Catalog.OrderBy(item => item.PrefabType, StringComparer.Ordinal).ThenBy(item => item.PrefabId, StringComparer.Ordinal))
            {
                census.TryGetValue(prefab.PrefabType + "\n" + prefab.PrefabId, out var entry);
                var findingCount = report.Analysis.Findings.Count(finding => finding.Evidence.Any(evidence => StringComparer.Ordinal.Equals(evidence, "asset=" + prefab.PrefabId)));
                builder.Append(Escape(prefab.PrefabId)).Append(',')
                    .Append(Escape(prefab.PrefabType)).Append(',')
                    .Append(Escape(prefab.DisplayName)).Append(',')
                    .Append(Escape(prefab.Traits)).Append(',')
                    .Append(Escape(entry?.Presence ?? string.Empty)).Append(',')
                    .Append(Escape(entry?.Counters?.TopLevelObjects?.Value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)).Append(',')
                    .Append(findingCount.ToString(CultureInfo.InvariantCulture)).AppendLine();
            }
            return builder.ToString();
        }

        private static string Escape(string value)
        {
            value ??= string.Empty;
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }
    }
}
