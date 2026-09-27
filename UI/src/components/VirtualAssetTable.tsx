import React from "react";
import { formatCountKind, formatObservation } from "../bindings";
import { MAX_ASSET_PAGE_SIZE, type AssetPage, type AssetRow } from "../types";

interface VirtualAssetTableProps {
  page: AssetPage;
  onPageChange?: (offset: number) => void;
  onSelect?: (asset: AssetRow) => void;
  selectedKey?: string | null;
}

export function VirtualAssetTable({ page, onPageChange, onSelect, selectedKey }: VirtualAssetTableProps): React.JSX.Element {
  const items = page.items.slice(0, Math.min(page.limit, MAX_ASSET_PAGE_SIZE));
  const start = page.totalCount === 0 ? 0 : page.offset + 1;
  const end = page.offset + items.length;
  const previousOffset = Math.max(0, page.offset - Math.max(1, page.limit));
  const nextOffset = page.offset + items.length;

  return (
    <section className="apa__table-shell" aria-label="Asset results">
      <div className="apa__table-scroll">
        <table className="apa__table">
          <thead>
            <tr>
              <th scope="col">Name</th><th scope="col">Source</th><th scope="col">Type</th><th scope="col">Instances</th><th scope="col">Count kind</th><th scope="col">Geometry</th><th scope="col">Findings</th><th scope="col">Presence</th>
            </tr>
          </thead>
          <tbody>
            {items.length === 0 ? (
              <tr><td colSpan={8} className="apa__empty-row">No assets match this query.</td></tr>
            ) : items.map((item) => (
              <tr key={`${item.prefabType}:${item.prefabId}`} aria-selected={selectedKey === `${item.prefabType}:${item.prefabId}`}>
                <th scope="row">
                  <button type="button" className="apa__link-button" onClick={() => onSelect?.(item)} disabled={!onSelect}>
                    <span className="apa__asset-name">{item.displayName}</span>
                  </button>
                  <span className="apa__asset-id">{item.prefabId}</span>
                </th>
                <td>{item.sourceLabel}</td><td>{item.prefabType}</td><td>{formatObservation(item.instances)}</td><td>{formatCountKind(item.countKind)}</td>
                <td>{item.renderCoverage === "Available" ? formatObservation(item.lod0Vertices ?? { availability: "NotScanned" }) : (item.renderCoverage === "NotScanned" || !item.renderCoverage ? "Not scanned" : item.renderCoverage)}</td>
                <td>{item.findingCount ?? 0}</td><td>{item.presence}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <footer className="apa__table-footer">
        <span>{`${start}–${end} of ${page.totalCount}`}</span>
        <div className="apa__pager">
          <button type="button" className="apa__button apa__button--quiet" disabled={page.offset <= 0 || !onPageChange} onClick={() => onPageChange?.(previousOffset)}>Previous</button>
          <button type="button" className="apa__button apa__button--quiet" disabled={nextOffset >= page.totalCount || !onPageChange} onClick={() => onPageChange?.(nextOffset)}>Next</button>
        </div>
      </footer>
    </section>
  );
}
