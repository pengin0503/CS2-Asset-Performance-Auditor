import React, { useMemo, useState } from "react";
import { EvidencePanel } from "../components/EvidencePanel";
import { FindingBadge } from "../components/FindingBadge";
import type { FindingCategory, FindingStatus, UiFinding } from "../types";

const statuses: Array<FindingStatus | "All"> = ["All", "Warning", "PotentialIssue", "Notice", "Observed"];
const categories: Array<FindingCategory | "All"> = ["All", "Geometry", "Lod", "Material", "Texture", "Exposure", "Integrity"];

export function WarningsTab({ findings }: { findings: UiFinding[] }): React.JSX.Element {
  const [status, setStatus] = useState<FindingStatus | "All">("All");
  const [category, setCategory] = useState<FindingCategory | "All">("All");
  const visible = useMemo(() => findings.filter((finding) =>
    (status === "All" || finding.status === status)
    && (category === "All" || finding.category === category)), [findings, status, category]);

  return (
    <section className="apa__tab-content" aria-labelledby="apa-warnings-title">
      <div className="apa__section-heading"><div><p className="apa__eyebrow">Evidence-based findings</p><h2 id="apa-warnings-title">Warnings</h2></div><span className="apa__muted">{visible.length} findings</span></div>
      <div className="apa__filters">
        <label className="apa__field"><span>Status</span><select aria-label="Filter findings by status" value={status} onChange={(event) => setStatus(event.currentTarget.value as FindingStatus | "All")}>{statuses.map((value) => <option key={value} value={value}>{value === "PotentialIssue" ? "Potential Issue" : value}</option>)}</select></label>
        <label className="apa__field"><span>Category</span><select aria-label="Filter findings by category" value={category} onChange={(event) => setCategory(event.currentTarget.value as FindingCategory | "All")}>{categories.map((value) => <option key={value} value={value}>{value}</option>)}</select></label>
      </div>
      {visible.length === 0 ? <p>No findings match the current filters.</p> : visible.map((finding, index) => (
        <article className="apa__finding" key={`${finding.ruleId}:${finding.prefabType ?? ""}:${finding.prefabId ?? ""}:${index}`}>
          <FindingBadge finding={finding} />
          <EvidencePanel finding={finding} />
        </article>
      ))}
    </section>
  );
}
