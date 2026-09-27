import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { ExportControls } from "../components/ExportControls";

const selected = { prefabId: "House.A", prefabType: "Building" };

describe("export controls", () => {
  it("exposes canonical JSON, flat CSV, and all documented scopes", () => {
    const html = renderToStaticMarkup(
      <ExportControls selectedAsset={selected} onExport={() => undefined} />,
    );

    expect(html).toContain("Full report");
    expect(html).toContain("Filtered assets");
    expect(html).toContain("Selected asset");
    expect(html).toContain("Census only");
    expect(html).toContain("Findings only");
    expect(html).toContain("JSON");
    expect(html).toContain("CSV");
  });

  it("disables selected scope when no asset is selected", () => {
    const html = renderToStaticMarkup(
      <ExportControls selectedAsset={null} onExport={() => undefined} />,
    );

    expect(html).toContain("Select an asset to enable Selected export");
  });
});
