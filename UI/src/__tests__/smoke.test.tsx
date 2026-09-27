import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { AssetAuditorRoot } from "../AssetAuditorRoot";

describe("AssetAuditorRoot", () => {
  it("renders the product title", () => {
    const html = renderToStaticMarkup(<AssetAuditorRoot />);
    expect(html).toContain("CS2 Asset Performance Auditor");
  });
});
