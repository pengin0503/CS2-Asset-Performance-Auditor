import React, { useEffect, useState } from "react";
import {
  createAssetQuery,
  createEscapeCloseHandler,
  nativeBindings,
  normalizeUiSettings,
  updateAssetQueryState,
  useAuditorSnapshot,
  useExportedReport,
} from "./bindings";
import { ExportControls } from "./components/ExportControls";
import { ScanStatus } from "./components/ScanStatus";
import { AssetsTab } from "./tabs/AssetsTab";
import { CensusTab } from "./tabs/CensusTab";
import { CompareTab } from "./tabs/CompareTab";
import { OverviewTab } from "./tabs/OverviewTab";
import { SettingsTab } from "./tabs/SettingsTab";
import { WarningsTab } from "./tabs/WarningsTab";
import {
  DEFAULT_ASSET_QUERY_STATE,
  DEFAULT_SCAN_OPTIONS,
  type AssetAuditorBindings,
  type AssetQueryState,
  type ExportAssetKey,
  type UiSnapshot,
} from "./types";

type AuditorTab = "Overview" | "Assets" | "Census" | "Warnings" | "Compare" | "Settings";

interface AssetAuditorRootProps {
  snapshot?: UiSnapshot;
  bindings?: AssetAuditorBindings;
  initiallyOpen?: boolean;
  exportedReport?: string;
}

const tabs: AuditorTab[] = ["Overview", "Assets", "Census", "Warnings", "Compare", "Settings"];

export function AssetAuditorRoot({
  snapshot: suppliedSnapshot,
  bindings = nativeBindings,
  initiallyOpen = false,
  exportedReport: suppliedReport,
}: AssetAuditorRootProps = {}): React.JSX.Element {
  const boundSnapshot = useAuditorSnapshot();
  const boundReport = useExportedReport();
  const snapshot = suppliedSnapshot ?? boundSnapshot;
  const exportedReport = suppliedReport ?? boundReport;
  const [open, setOpen] = useState(initiallyOpen);
  const [activeTab, setActiveTab] = useState<AuditorTab>("Overview");
  const [query, setQuery] = useState<AssetQueryState>(DEFAULT_ASSET_QUERY_STATE);
  const [selectedAsset, setSelectedAsset] = useState<ExportAssetKey | null>(null);
  const settings = normalizeUiSettings(snapshot.settings ?? DEFAULT_SCAN_OPTIONS);
  const findings = snapshot.findings ?? [];

  useEffect(() => {
    if (!open) return;
    const timer = window.setTimeout(() => {
      bindings.requestAssetsPage(createAssetQuery(query));
    }, 180);
    return () => window.clearTimeout(timer);
  }, [bindings, open, query]);

  useEffect(() => {
    if (!open) return;
    const closeOnEscape = createEscapeCloseHandler(() => setOpen(false));
    window.addEventListener("keydown", closeOnEscape as (event: KeyboardEvent) => void);
    return () => window.removeEventListener("keydown", closeOnEscape as (event: KeyboardEvent) => void);
  }, [open]);

  useEffect(() => {
    setQuery((current) => current.pageSize === settings.pageSize ? current : { ...current, pageSize: settings.pageSize, offset: 0 });
  }, [settings.pageSize]);

  const updateQuery = (patch: Partial<AssetQueryState>) => {
    setQuery((current) => updateAssetQueryState(current, patch));
  };

  const updateSettings = (nextSettings: typeof settings) => {
    bindings.updateSettings(normalizeUiSettings(nextSettings));
  };

  const visibleFindings = settings.showNoticeFindings ? findings : findings.filter((finding) => finding.status !== "Notice");

  return (
    <main className="asset-auditor" aria-label="CS2 Asset Performance Auditor" style={{ fontSize: `${settings.uiScale}em` }}>
      <h1 className="apa__sr-only">CS2 Asset Performance Auditor</h1>
      {!open ? (
        <button type="button" className="apa__launcher" onClick={() => setOpen(true)}>Asset Auditor</button>
      ) : (
        <section className="apa__panel" role="dialog" aria-label="CS2 Asset Performance Auditor">
          <header className="apa__panel-header">
            <div><p className="apa__eyebrow">Cities: Skylines II</p><h1>Asset Performance Auditor</h1></div>
            <button type="button" className="apa__icon-button" aria-label="Close Auditor panel" title="Close" onClick={() => setOpen(false)}>×</button>
          </header>
          <ScanStatus scan={snapshot.scanStatus} onCancel={() => bindings.cancelCensus()} />
          <nav className="apa__tabs" aria-label="Auditor views">
            {tabs.map((tab) => (
              <button type="button" key={tab} className={`apa__tab ${activeTab === tab ? "apa__tab--active" : ""}`} aria-current={activeTab === tab ? "page" : undefined} onClick={() => setActiveTab(tab)}>{tab}</button>
            ))}
          </nav>
          <div className="apa__panel-body">
            {activeTab === "Overview" ? <OverviewTab snapshot={snapshot} bindings={bindings} /> : null}
            {activeTab === "Assets" ? <AssetsTab page={snapshot.assetPage} query={query} onQueryChange={updateQuery} findings={visibleFindings} onSelectedAssetChange={setSelectedAsset} onDeepInspect={(renderKey) => bindings.requestDeepInspection(renderKey)} /> : null}
            {activeTab === "Census" ? <CensusTab snapshot={snapshot} /> : null}
            {activeTab === "Warnings" ? <WarningsTab findings={visibleFindings} /> : null}
            {activeTab === "Compare" ? <CompareTab assets={snapshot.assetPage.items.slice(0, 4)} findings={visibleFindings} /> : null}
            {activeTab === "Settings" ? <SettingsTab settings={settings} onChange={updateSettings} /> : null}
            <ExportControls selectedAsset={selectedAsset} onExport={(request) => bindings.requestExport(request)} />
            {exportedReport ? (
              <details className="apa__export-result"><summary>Audit export is ready</summary><textarea aria-label="Export data" readOnly value={exportedReport} /></details>
            ) : null}
          </div>
        </section>
      )}
    </main>
  );
}
