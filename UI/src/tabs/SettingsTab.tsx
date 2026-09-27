import React from "react";
import type { UiScanOptions } from "../types";

export function SettingsTab({
  settings,
  onChange,
}: {
  settings: UiScanOptions;
  onChange: (settings: UiScanOptions) => void;
}): React.JSX.Element {
  const set = <K extends keyof UiScanOptions>(key: K, value: UiScanOptions[K]) => onChange({ ...settings, [key]: value });
  return (
    <section className="apa__tab-content" aria-labelledby="apa-settings-title">
      <div className="apa__section-heading"><div><p className="apa__eyebrow">Bounded and snapshot-aware</p><h2 id="apa-settings-title">Settings</h2></div></div>

      <h3>Scanning</h3>
      <label className="apa__setting-row"><span><strong>Collect subordinate objects</strong><small>Disabled collection is recorded as Not scanned, never zero.</small></span><input type="checkbox" checked={settings.collectSubordinateObjects} onChange={(event) => set("collectSubordinateObjects", event.currentTarget.checked)} /></label>
      <label className="apa__setting-row"><span><strong>Collect network edges</strong><small>{settings.collectNetworkEdges ? "Included in the next Census." : "Not scanned on the next Census."}</small></span><input type="checkbox" checked={settings.collectNetworkEdges} onChange={(event) => set("collectNetworkEdges", event.currentTarget.checked)} /></label>
      <label className="apa__setting-row"><span><strong>Refresh catalog at scan start</strong><small>Applies to the next scan.</small></span><input type="checkbox" checked={settings.refreshCatalogAtScanStart} onChange={(event) => set("refreshCatalogAtScanStart", event.currentTarget.checked)} /></label>
      <label className="apa__field"><span>Managed frame budget (ms)</span><input type="number" min={0.25} max={8} step={0.25} value={settings.frameBudgetMs} onChange={(event) => set("frameBudgetMs", Number(event.currentTarget.value))} /></label>
      <label className="apa__field"><span>Progress update interval (ms)</span><input type="number" min={50} max={2000} step={50} value={settings.progressUpdateMs} onChange={(event) => set("progressUpdateMs", Number(event.currentTarget.value))} /></label>

      <h3>Analysis</h3>
      <label className="apa__setting-row"><span><strong>Heuristic findings</strong><small>Potential issues remain evidence-based and versioned.</small></span><input type="checkbox" checked={settings.enableHeuristicFindings} onChange={(event) => set("enableHeuristicFindings", event.currentTarget.checked)} /></label>
      <label className="apa__setting-row"><span><strong>Peer-outlier analysis</strong><small>Requires a sufficient comparable population.</small></span><input type="checkbox" checked={settings.enablePeerOutliers} onChange={(event) => set("enablePeerOutliers", event.currentTarget.checked)} /></label>
      <label className="apa__setting-row"><span><strong>Show Notice findings</strong><small>Display informational findings in Warnings.</small></span><input type="checkbox" checked={settings.showNoticeFindings} onChange={(event) => set("showNoticeFindings", event.currentTarget.checked)} /></label>
      <label className="apa__field"><span>Comparison population</span><select value={settings.comparisonPopulation} onChange={(event) => set("comparisonPopulation", event.currentTarget.value as UiScanOptions["comparisonPopulation"])}><option value="SameCategory">Same category</option><option value="BuiltinDlc">Vanilla / DLC</option><option value="Custom">Custom assets</option><option value="SameSourcePack">Same source pack</option></select></label>

      <h3>Advanced</h3>
      <label className="apa__field"><span>Asset page size</span><input type="number" min={25} max={200} value={settings.pageSize} onChange={(event) => set("pageSize", Number(event.currentTarget.value))} /></label>
      <label className="apa__field"><span>Metadata cache limit</span><input type="number" min={64} max={4096} value={settings.metadataCacheLimit} onChange={(event) => set("metadataCacheLimit", Number(event.currentTarget.value))} /></label>
      <label className="apa__field"><span>Deep Inspection limit</span><input type="number" min={1} max={16} value={settings.deepInspectionLimit} onChange={(event) => set("deepInspectionLimit", Number(event.currentTarget.value))} /></label>
      <label className="apa__field"><span>UI scale</span><input type="number" min={0.75} max={1.5} step={0.05} value={settings.uiScale} onChange={(event) => set("uiScale", Number(event.currentTarget.value))} /></label>
      <p className="apa__muted">Unsafe numeric values are clamped before they are sent to the game. Collection changes apply to the next Census.</p>
    </section>
  );
}
