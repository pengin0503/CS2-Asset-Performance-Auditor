import React from "react";
import type { RenderCoverage, UiRenderRelation } from "../types";

export function RenderStructure({ coverage, relations }: { coverage: RenderCoverage; relations: UiRenderRelation[] }): React.JSX.Element {
  if (coverage !== "Available") {
    const label = coverage === "NotScanned" ? "Not scanned" : coverage;
    return (
      <section className="apa__detail-section" aria-label="Render structure">
        <h3>Render Structure</h3>
        <p>{label}</p>
        <p className="apa__muted">Render coverage is unavailable; no zero-geometry inference is made.</p>
      </section>
    );
  }

  return (
    <section className="apa__detail-section" aria-label="Render structure">
      <h3>Render Structure</h3>
      {relations.length === 0 ? <p>No render relations were recorded.</p> : (
        <ul>{relations.map((relation, index) => <li key={`${relation.kind}:${relation.from}:${relation.to}:${index}`}>{relation.from} → {relation.to} ({relation.kind}{relation.lodLevel == null ? "" : ` LOD${relation.lodLevel}`})</li>)}</ul>
      )}
    </section>
  );
}
