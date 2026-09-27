import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { ScanStatus } from "../components/ScanStatus";

describe("ScanStatus", () => {
  it("shows exact item progress when the total is known", () => {
    const html = renderToStaticMarkup(
      <ScanStatus
        scan={{
          state: "Running",
          stage: "Processing catalog",
          stageNumber: 3,
          totalStages: 8,
          completedItems: 24,
          totalItems: 80,
        }}
      />,
    );

    expect(html).toContain("24 of 80");
    expect(html).toContain('aria-valuenow="30"');
    expect(html).not.toContain("Indeterminate");
  });

  it("shows an indeterminate progress state when the total is unknown", () => {
    const html = renderToStaticMarkup(
      <ScanStatus
        scan={{
          state: "Running",
          stage: "Capturing object census",
          stageNumber: 4,
          totalStages: 8,
          completedItems: null,
          totalItems: null,
        }}
      />,
    );

    expect(html).toContain("Capturing object census");
    expect(html).toContain("Indeterminate");
    expect(html).not.toContain("aria-valuenow");
  });
});
