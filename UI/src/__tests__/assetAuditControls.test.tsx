import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { OverviewTab } from "../tabs/OverviewTab";
import { EMPTY_UI_SNAPSHOT, type AssetAuditorBindings } from "../types";

describe("Asset audit controls", () => {
  it("exposes a user-triggered asset audit without replacing census", () => {
    const bindings: AssetAuditorBindings = {
      requestCensus() {},
      requestAssetAudit() {},
      requestDeepInspection() {},
      cancelCensus() {},
      requestAssetsPage() {},
      requestExport() {},
      updateSettings() {},
    };

    const html = renderToStaticMarkup(
      <OverviewTab snapshot={EMPTY_UI_SNAPSHOT} bindings={bindings} />,
    );

    expect(html).toContain("Run Census");
    expect(html).toContain("Run Asset Audit");
  });
});
