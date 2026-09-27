import React from "react";
import type { UiFinding } from "../types";

export function EvidencePanel({ finding }: { finding: UiFinding }): React.JSX.Element {
  return (
    <details className="apa__evidence">
      <summary>{finding.title}</summary>
      <p>{finding.explanation}</p>
      <dl>
        <dt>Evidence</dt>
        <dd>{finding.evidence.length > 0 ? finding.evidence.join(" · ") : "No additional evidence recorded."}</dd>
        <dt>Basis</dt>
        <dd>{finding.basis}</dd>
        <dt>Rule version</dt>
        <dd>{finding.ruleVersion}</dd>
        <dt>Rule ID</dt>
        <dd>{finding.ruleId}</dd>
      </dl>
    </details>
  );
}
