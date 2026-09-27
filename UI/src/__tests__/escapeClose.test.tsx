import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";
import { AssetAuditorRoot } from "../AssetAuditorRoot";
import { createEscapeCloseHandler } from "../bindings";
import register from "../index";
import { EMPTY_UI_SNAPSHOT, type UiSnapshot } from "../types";

describe("panel close behavior", () => {
  it("registers the launcher with the native game UI module registry", () => {
    const append = vi.fn();

    register({ append } as never);

    expect(append).toHaveBeenCalledWith("GameTopLeft", expect.any(Function));
  });

  it("Escape closes the panel without invoking scan cancellation", () => {
    const closePanel = vi.fn();
    const cancelScan = vi.fn();
    const preventDefault = vi.fn();
    const handler = createEscapeCloseHandler(closePanel);

    handler({ key: "Escape", preventDefault } as unknown as KeyboardEvent);

    expect(preventDefault).toHaveBeenCalledOnce();
    expect(closePanel).toHaveBeenCalledOnce();
    expect(cancelScan).not.toHaveBeenCalled();
  });

  it("shows the active scan again when an opened panel receives the current snapshot", () => {
    const snapshot = {
      ...EMPTY_UI_SNAPSHOT,
      scanStatus: {
        state: "Running",
        stage: "Reducing network census",
        stageNumber: 7,
        totalStages: 8,
        completedItems: null,
        totalItems: null,
      },
      summary: {
        ...EMPTY_UI_SNAPSHOT.summary,
        gameVersion: "1.6.2f1",
        compatibility: "Untested",
        catalogCount: 12,
      },
      assetPage: { offset: 0, limit: 100, totalCount: 12, items: [] },
      settings: { collectSubordinateObjects: true, collectNetworkEdges: true },
    } as UiSnapshot;
    const html = renderToStaticMarkup(
      <AssetAuditorRoot snapshot={snapshot} initiallyOpen />,
    );

    expect(html).toContain("Reducing network census");
    expect(html).toContain("Indeterminate");
  });
});
