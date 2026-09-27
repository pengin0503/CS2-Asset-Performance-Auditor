// @vitest-environment jsdom
import React, { Profiler } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it, vi } from "vitest";
import { setBindingValueForTests } from "../testing/cs2ApiShim";
import { UI_BINDING_GROUP } from "../bindings";
import { AssetAuditorRoot } from "../AssetAuditorRoot";
import { EMPTY_UI_SNAPSHOT, type AssetAuditorBindings, type UiSnapshot } from "../types";

const wait = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

const snapshot: UiSnapshot = {
  ...EMPTY_UI_SNAPSHOT,
  assetPage: {
    offset: 0,
    limit: 100,
    totalCount: 1,
    items: [{
      prefabId: "House.A",
      prefabType: "Building",
      displayName: "House A",
      sourceLabel: "Built-in",
      traits: ["Building"],
      countKind: "TopLevelObjects",
      instances: { availability: "NotScanned" },
      presence: "Unknown",
      renderCoverage: "Available",
      renderRelations: [{ kind: "DirectMesh", from: "Building:House.A", to: "Game.Prefabs.RenderPrefab:Render.House.A", lodLevel: null }],
    }],
  },
};

function bindingsSpy(): AssetAuditorBindings {
  return {
    requestCensus: vi.fn(),
    requestAssetAudit: vi.fn(),
    requestDeepInspection: vi.fn(),
    cancelCensus: vi.fn(),
    requestAssetsPage: vi.fn(),
    requestExport: vi.fn(),
    updateSettings: vi.fn(),
  };
}

function button(container: HTMLElement, text: string): HTMLButtonElement {
  const found = [...container.querySelectorAll("button")].find((candidate) => candidate.textContent === text);
  if (!found) throw new Error(`Button "${text}" was not rendered.`);
  return found;
}

let root: Root | null = null;
afterEach(() => {
  root?.unmount();
  root = null;
  document.body.innerHTML = "";
});

describe("live snapshot binding", () => {
  it("settles after an asset is selected instead of re-rendering on every commit", async () => {
    setBindingValueForTests(UI_BINDING_GROUP, "snapshot", JSON.stringify(snapshot));
    const container = document.body.appendChild(document.createElement("div"));
    let commits = 0;
    root = createRoot(container);
    root.render(<Profiler id="auditor" onRender={() => { commits++; }}><AssetAuditorRoot initiallyOpen bindings={bindingsSpy()} /></Profiler>);
    await wait(50);
    button(container, "Assets").click();
    await wait(50);
    button(container, "House A").click();
    await wait(200);
    const settled = commits;
    await wait(300);

    expect(commits).toBe(settled);
    expect(container.textContent).toContain("Asset Details");
    expect(container.querySelector("option[value='Selected']")?.hasAttribute("disabled")).toBe(false);
  });

  it("routes Deep inspect from asset details to the native binding", async () => {
    setBindingValueForTests(UI_BINDING_GROUP, "snapshot", JSON.stringify(snapshot));
    const bindings = bindingsSpy();
    const container = document.body.appendChild(document.createElement("div"));
    root = createRoot(container);
    root.render(<AssetAuditorRoot initiallyOpen bindings={bindings} />);
    await wait(50);
    button(container, "Assets").click();
    await wait(50);
    button(container, "House A").click();
    await wait(50);
    button(container, "Deep inspect").click();

    expect(bindings.requestDeepInspection).toHaveBeenCalledWith("Game.Prefabs.RenderPrefab:Render.House.A");
  });
});
