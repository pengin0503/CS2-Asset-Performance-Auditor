import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";
import { RenderStructure } from "../details/RenderStructure";

describe("Deep inspection", () => {
  it("offers deep inspection only for a resolved render relation", () => {
    const onDeepInspect = vi.fn();
    const html = renderToStaticMarkup(
      <RenderStructure
        coverage="Available"
        relations={[{
          kind: "DirectMesh",
          from: "Building:House.A",
          to: "Game.Prefabs.RenderPrefab:Render.House.A",
          lodLevel: null,
        }]}
        onDeepInspect={onDeepInspect}
      />,
    );

    expect(html).toContain("Deep inspect");
    expect(html).toContain("Game.Prefabs.RenderPrefab:Render.House.A");
  });

  it("does not offer deep inspection when render coverage is unavailable", () => {
    const html = renderToStaticMarkup(
      <RenderStructure
        coverage="Unsupported"
        relations={[]}
        onDeepInspect={() => {}}
      />,
    );

    expect(html).not.toContain("Deep inspect");
  });
});
