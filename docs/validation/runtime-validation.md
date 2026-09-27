# Runtime validation matrix

This document records scenarios that require an actual Cities: Skylines II runtime. Unit, UI, Adapter, and build checks do **not** upgrade these rows to PASS.

| Scenario | Status | Required evidence | Notes |
| --- | --- | --- | --- |
| Selected-asset Deep Inspection succeeds | NOT RUN | Selected RenderAssetKey, copied material/shader/property observations, clean return to gameplay | Verify no Unity object is retained by the observation store. |
| Deep Inspection cancellation | NOT RUN | Cancellation during inspection, cleanup completes, next inspection still works | Only resources acquired by this mod may be released. |
| Deep Inspection exception path | NOT RUN | Inject/encounter read exception, cleanup executes, game remains stable | Verify paired release in exceptional path. |
| Repeated Deep Inspection | NOT RUN | Repeat same asset at least 20 times without monotonically growing retained resources | Compare memory/allocation trend where measurable. |
| Game-owned shared material/object | NOT RUN | Inspect assets sharing materials/textures and verify other game rendering remains intact | Never Destroy/Unload a shared object merely because it was observed. |

## Ownership contract

Deep Inspection is selected-asset only. It may temporarily acquire game resources through a documented paired API such as `ObtainMaterials` / `ReleaseMaterials`. Retained analysis state must contain only copied primitive/string metadata. It must not retain `UnityEngine.Material`, `UnityEngine.Mesh`, `UnityEngine.Texture`, or other Unity object references.

A resource is released only when this mod performed the matching acquisition. Game-owned shared objects are never destroyed. Surface property data loaded by this mod is unloaded in a `finally`-equivalent path; pre-existing loaded data is left loaded.

## Broader runtime scenarios

The final validation task will add Vanilla, large-city, custom-asset-heavy, broken-asset, world-change, UI-close/Escape, timing, slice-percentile, and memory/allocation scenarios. Until actually executed, all such runtime scenarios remain NOT RUN or BLOCKED.
