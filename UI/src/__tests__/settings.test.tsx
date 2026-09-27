import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { normalizeUiSettings } from "../bindings";
import { SettingsTab } from "../tabs/SettingsTab";
import { DEFAULT_SCAN_OPTIONS } from "../types";

describe("analysis settings", () => {
  it("clamps unsafe numeric settings into documented bounds", () => {
    const normalized = normalizeUiSettings({
      ...DEFAULT_SCAN_OPTIONS,
      frameBudgetMs: -100,
      progressUpdateMs: 999999,
      pageSize: 10000,
      deepInspectionLimit: 0,
      uiScale: 9,
    });
    expect(normalized.frameBudgetMs).toBe(0.25);
    expect(normalized.progressUpdateMs).toBe(2000);
    expect(normalized.pageSize).toBe(200);
    expect(normalized.deepInspectionLimit).toBe(1);
    expect(normalized.uiScale).toBe(1.5);
  });

  it("disabled optional collection is described as Not scanned rather than zero", () => {
    const html = renderToStaticMarkup(
      <SettingsTab
        settings={{ ...DEFAULT_SCAN_OPTIONS, collectNetworkEdges: false }}
        onChange={() => undefined}
      />,
    );
    expect(html).toContain("Not scanned");
    expect(html).toContain("Collect network edges");
  });

  it("keeps heuristic and peer analysis independently configurable", () => {
    const html = renderToStaticMarkup(
      <SettingsTab settings={{ ...DEFAULT_SCAN_OPTIONS, enableHeuristicFindings: false, enablePeerOutliers: true }} onChange={() => undefined} />,
    );
    expect(html).toContain("Heuristic findings");
    expect(html).toContain("Peer-outlier analysis");
  });
});
