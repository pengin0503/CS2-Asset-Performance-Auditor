import React, { useState } from "react";
import type { ExportAssetKey, ExportFormat, ExportRequest, ExportScope } from "../types";

export function ExportControls({
  selectedAsset,
  onExport,
}: {
  selectedAsset: ExportAssetKey | null;
  onExport: (request: ExportRequest) => void;
}): React.JSX.Element {
  const [format, setFormat] = useState<ExportFormat>("Json");
  const [scope, setScope] = useState<ExportScope>("Full");
  const selectedUnavailable = !selectedAsset;
  const effectiveScope: ExportScope = selectedUnavailable && scope === "Selected" ? "Full" : scope;

  return (
    <section className="apa__export-controls" aria-label="Export audit data">
      <div className="apa__section-heading">
        <div><p className="apa__eyebrow">Shared export action</p><h3>Export</h3></div>
      </div>
      <div className="apa__filters">
        <label className="apa__field">
          <span>Format</span>
          <select aria-label="Export format" value={format} onChange={(event) => setFormat(event.currentTarget.value as ExportFormat)}>
            <option value="Json">JSON</option>
            <option value="Csv">CSV</option>
          </select>
        </label>
        <label className="apa__field">
          <span>Scope</span>
          <select aria-label="Export scope" value={effectiveScope} onChange={(event) => setScope(event.currentTarget.value as ExportScope)}>
            <option value="Full">Full report</option>
            <option value="Filtered">Filtered assets</option>
            <option value="Selected" disabled={selectedUnavailable}>Selected asset</option>
            <option value="Census">Census only</option>
            <option value="Findings">Findings only</option>
          </select>
        </label>
        <button
          type="button"
          className="apa__button"
          onClick={() => onExport({
            format,
            scope: effectiveScope,
            selectedKeys: effectiveScope === "Selected" && selectedAsset ? [selectedAsset] : [],
          })}
        >
          Prepare {format === "Json" ? "JSON" : "CSV"} export
        </button>
      </div>
      {selectedUnavailable ? <p className="apa__muted">Select an asset to enable Selected export.</p> : null}
      <p className="apa__muted">Filtered export uses the complete current filter result in C#, not only the visible page.</p>
    </section>
  );
}
