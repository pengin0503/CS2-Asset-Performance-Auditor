# Runtime validation matrix

This document records scenarios that require an actual Cities: Skylines II runtime. Unit tests, UI tests, Adapter contract checks, compilation, and GitHub Actions do **not** upgrade a runtime row to PASS.

## Status contract

- `PASS` — the stated scenario was actually executed in game and the required evidence was recorded.
- `FAIL` — the stated scenario was actually executed and violated an acceptance criterion.
- `NOT RUN` — the scenario has not yet been executed in game.
- `BLOCKED` — execution is currently prevented by a missing local game/toolchain/reference dependency.

## Runtime matrix

| Scenario | Status | Counts to record | Timing to record | Memory/allocation evidence | Acceptance evidence |
| --- | --- | --- | --- | --- | --- |
| Vanilla small city Census + audit | NOT RUN | Prefabs, captured object refs, network edges, render assets | total scan time, max/P95 managed slice | allocation/memory delta where measurable | completes without gameplay mutation; published generations agree |
| Large city | NOT RUN | same as above | total time, max/P95 managed slice, processed items | allocation/memory delta | UI remains responsive; no unbounded managed work |
| Custom-asset-heavy city | NOT RUN | Prefabs by source, render assets, unique textures | total time, max/P95 managed slice | allocation/memory delta | shared resources deduplicate; failures remain item-scoped |
| Broken/incomplete asset | NOT RUN | failed/unresolved asset/resource counts | scan time | allocation/memory delta if meaningful | affected observation is Failed/Unsupported; unrelated assets continue |
| World change during active scan | NOT RUN | pre-change and post-change world/catalog generation | cancellation/cleanup duration | retained native/managed resources | old world work is cancelled/disposed and cannot publish into new world |
| Close button during scan | NOT RUN | current stage before/after reopen | progress continuity | n/a | panel closes without cancelling; reopen shows active/current scan |
| Escape during scan | NOT RUN | current stage before/after reopen | progress continuity | n/a | Escape closes UI and does not cancel scan |
| Selected-asset Deep Inspection succeeds | NOT RUN | selected RenderAssetKey and copied observations | inspection duration | retained-object trend | no Unity object is retained by the observation store |
| Deep Inspection cancellation | NOT RUN | selected key, cancellation state | cleanup duration | retained-object trend | cleanup completes; next inspection works; only mod acquisitions are released |
| Deep Inspection exception path | NOT RUN | diagnostic code and selected key | cleanup duration | retained-object trend | paired cleanup executes; game remains stable |
| Repeated Deep Inspection (20+) | NOT RUN | repetitions and observation counts | per-run/aggregate timing | memory/allocation trend | no monotonic retained-resource growth attributable to inspection |
| Game-owned shared material/object | NOT RUN | assets sharing resource | inspection duration | shared-object lifetime evidence | observing one asset never destroys/unloads a game-owned shared object |
| Local Release game build with current CS2 toolchain | BLOCKED | n/a | build duration optional | n/a | requires local `CSII_TOOLPATH`/managed game references; GitHub CI intentionally does not redistribute them |

## Telemetry interpretation

`ScanTelemetry` is low-cost self-telemetry for user-triggered scan managed slices. It records elapsed scan time, processed-item count, lifetime maximum managed-slice duration, and a nearest-rank P95 over a bounded rolling sample (maximum 256 samples by default). It is not a continuous profiler and does not measure GPU time, render-thread cost, or all engine work.

For each runtime performance scenario, record the entity/Prefab/render-asset counts that explain workload size alongside total time and max/P95 managed slice. Record memory/allocation deltas only where the measurement method is reliable; otherwise state `not measured` rather than inventing a value.

## Ownership contract

Deep Inspection is selected-asset only. It may temporarily acquire game resources through a documented paired API such as `ObtainMaterials` / `ReleaseMaterials`. Retained analysis state contains only copied primitive/string metadata. It must not retain `UnityEngine.Material`, `UnityEngine.Mesh`, `UnityEngine.Texture`, or other Unity object references.

A resource is released only when this mod performed the matching acquisition. Game-owned shared objects are never destroyed. Surface property data loaded by this mod is unloaded in a `finally`-equivalent path; pre-existing loaded data is left loaded.

## Automated evidence versus runtime evidence

GitHub Actions executes the game-independent Core test suite and the UI tests/production build. These checks can establish deterministic math, query/export contracts, UI semantics, and TypeScript build health. They cannot establish in-game safety, compatibility with a particular Cities: Skylines II build, actual frame-time impact, or Deep Inspection ownership behavior under the running engine.

Adapter tests and a Release game build should be run locally whenever the required CS2 managed assemblies and modding toolchain are available. Until those checks and the scenarios above are actually executed, their status remains `NOT RUN` or `BLOCKED` as shown.
