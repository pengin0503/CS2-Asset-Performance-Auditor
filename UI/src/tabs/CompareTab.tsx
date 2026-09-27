import React from "react";
import { formatObservation } from "../bindings";
import type { AssetRow, UiFinding } from "../types";

export function normalizeCompareSelection(ids: string[]): string[] {
  const result: string[] = [];
  for (const id of ids) {
    if (!result.includes(id)) result.push(id);
    if (result.length === 4) break;
  }
  return result;
}

export function CompareTab({ assets, findings }: { assets: AssetRow[]; findings: UiFinding[] }): React.JSX.Element {
  const selected = assets.slice(0, 4);
  return (
    <section className="apa__tab-content" aria-labelledby="apa-compare-title">
      <div className="apa__section-heading">
        <div><p className="apa__eyebrow">Direct evidence comparison</p><h2 id="apa-compare-title">Compare</h2></div>
        <span className="apa__muted">2–4 assets</span>
      </div>
      {selected.length < 2 ? <p>Select at least two assets from the current bounded result page to compare them.</p> : (
        <div className="apa__table-scroll">
          <table className="apa__table">
            <thead><tr><th>Dimension</th>{selected.map((asset) => <th key={`${asset.prefabType}:${asset.prefabId}`}>{asset.displayName}</th>)}</tr></thead>
            <tbody>
              <tr><th>Instances</th>{selected.map((asset) => <td key={asset.prefabId}>{formatObservation(asset.instances)}</td>)}</tr>
              <tr><th>Geometry / LOD0 vertices</th>{selected.map((asset) => <td key={asset.prefabId}>{formatObservation(asset.lod0Vertices ?? { availability: "NotScanned" })}</td>)}</tr>
              <tr><th>LOD1 retention</th>{selected.map((asset) => <td key={asset.prefabId}>{formatObservation(asset.lod1RetentionPercent ?? { availability: "NotScanned" })}</td>)}</tr>
              <tr><th>Materials</th>{selected.map((asset) => <td key={asset.prefabId}>{formatObservation(asset.materialCount ?? { availability: "NotScanned" })}</td>)}</tr>
              <tr><th>Estimated texture payload</th>{selected.map((asset) => <td key={asset.prefabId}>{formatObservation(asset.estimatedTexturePayload ?? { availability: "NotScanned" })}</td>)}</tr>
              <tr><th>Findings</th>{selected.map((asset) => <td key={asset.prefabId}>{findings.filter((finding) => !finding.prefabId || finding.prefabId === asset.prefabId).length}</td>)}</tr>
            </tbody>
          </table>
        </div>
      )}
      <p className="apa__muted">Comparison presents independent evidence dimensions only; it does not calculate an overall performance score.</p>
    </section>
  );
}
