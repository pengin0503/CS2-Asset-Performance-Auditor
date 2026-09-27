import React from "react";
import type { UiScanOptions } from "../types";

export function SettingsTab({
  settings,
  onChange,
}: {
  settings: UiScanOptions;
  onChange: (settings: UiScanOptions) => void;
}): React.JSX.Element {
  return (
    <section className="apa__tab-content" aria-labelledby="apa-settings-title">
      <div className="apa__section-heading">
        <div>
          <p className="apa__eyebrow">Recorded in each snapshot</p>
          <h2 id="apa-settings-title">Settings</h2>
        </div>
      </div>
      <label className="apa__setting-row">
        <span>
          <strong>Collect subordinate objects</strong>
          <small>Include owned or controlled child objects in live references.</small>
        </span>
        <input
          type="checkbox"
          checked={settings.collectSubordinateObjects}
          onChange={(event) => onChange({ ...settings, collectSubordinateObjects: event.currentTarget.checked })}
        />
      </label>
      <label className="apa__setting-row">
        <span>
          <strong>Collect network edges</strong>
          <small>Include current top-level network edge references.</small>
        </span>
        <input
          type="checkbox"
          checked={settings.collectNetworkEdges}
          onChange={(event) => onChange({ ...settings, collectNetworkEdges: event.currentTarget.checked })}
        />
      </label>
      <p className="apa__muted">Changes apply to the next Census. Omitted measurements are shown as Not scanned.</p>
    </section>
  );
}
