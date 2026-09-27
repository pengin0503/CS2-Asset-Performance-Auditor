# CS2 Asset Performance Auditor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a read-mostly Cities: Skylines II Code Mod that catalogs Prefabs, captures a snapshot census of current-city exposure, audits render/geometry/LOD/surface/texture metadata, produces evidence-backed findings, and exposes the results through a scalable in-game UI and versioned exports.

**Architecture:** Keep CS2/ECS/AssetDatabase/Unity types inside Game Integration. Convert runtime data into game-independent Core observations, publish scans atomically, run findings/query/export logic on Core data, and expose only bounded result windows to the React/TypeScript UI. Phases 0–4 share this architecture but have explicit milestone gates.

**Tech Stack:** C# / .NET Framework 4.8 CS2 Code Mod, Unity Entities/ECS, Colossal AssetDatabase, NUnit on .NET 8 for Pure Core tests, a local net48 Adapter Contract test project for game-API contracts, React 18 + TypeScript + Webpack + Vitest for UI, `DataContractJsonSerializer` for canonical JSON unless an implementation-time compatibility test proves another serializer necessary.

**Spec:** `docs/superpowers/specs/2026-09-27-asset-performance-auditor-design.md`

## Global Constraints

- Baseline game target: Cities: Skylines II `1.6.2f1`.
- Phases 1–4 use **no Harmony dependency, Prefix, Postfix, or Transpiler**.
- Reflection is not part of runtime implementation and must not be introduced as an automatic fallback. Reflection is permitted inside local Adapter Contract tests when used only to inspect public member contracts.
- Scanners do not mutate gameplay entities or Prefabs.
- Full-world Census and full Asset Audit are user-triggered snapshots, not always-on trackers.
- Gameplay Prefab identity and render-resource identity remain separate.
- `Observed`, `Derived`, `Estimated`, `NotScanned`, `NotApplicable`, `Unsupported`, and `Failed` semantics survive through Core, UI, and JSON export.
- Failed/cancelled scans never replace the last successful published snapshot.
- Numeric zero never substitutes for unavailable/unscanned states.
- Static metadata is never described as actual per-frame rendering cost or actual GPU residency.
- No global 0–100 performance score.
- Heavy Unity object materialization is selected-asset-only Deep Inspection.
- Shared RenderPrefab/GeometryAsset/SurfaceAsset/TextureAsset resources are deduplicated.
- Game DLLs are local/toolchain references and are not committed.
- New game versions remain `Untested` until runtime validation marks them `Supported`.
- Phase 5 Runtime Evidence is outside this plan.

## Review Focus

1. **World change during active scan:** current-world state is keyed by `WorldGeneration`; changing worlds cancels/disposes active work and invalidates the old published census.
2. **Ambiguous census classification:** an entity with multiple subordinate markers counts once; omitted metrics are `NotScanned`, not zero.
3. **Shared resources:** repeated Prefab references do not multiply Geometry/Surface/Texture collector work or unique-resource totals.
4. **Broken custom asset:** one bad Prefab/RenderPrefab/Texture remains item/feature-level failure where safe and does not abort unrelated collectors.
5. **Deep Inspection ownership:** mod-owned temporary resources are released on success/cancel/exception while game-owned shared resources are never destroyed.

---

## Repository structure locked by this plan

```text
CS2AssetPerformanceAuditor.sln
README.md
.gitignore

src/CS2AssetPerformanceAuditor/
  CS2AssetPerformanceAuditor.csproj
  Mod.cs
  Setting.cs
  Core/
    ProjectInfo.cs
    Capabilities/
    Observations/
    Prefabs/
    Census/
    Rendering/
    Findings/
    Scanning/
    Query/
    Diagnostics/
  GameIntegration/
    Capabilities/
    Prefabs/
    Census/
    Rendering/
    AssetAuditSystem.cs
  Export/
  UI/
    AssetAuditUISystem.cs
    UiContracts.cs
    UiSnapshotBuilder.cs

UI/
  package.json
  package-lock.json
  tsconfig.json
  webpack.config.js
  src/
    index.tsx
    bindings.ts
    types.ts
    AssetAuditorRoot.tsx
    assetAuditor.module.scss
    components/
    tabs/
    details/
    __tests__/

tests/CS2AssetPerformanceAuditor.Tests/
  CS2AssetPerformanceAuditor.Tests.csproj
  ScaffoldSmokeTests.cs
  ObservationTests.cs
  PrefabProjectionTests.cs
  CensusReducerTests.cs
  ScanSessionTests.cs
  RenderGraphTests.cs
  GeometryMathTests.cs
  TextureFootprintTests.cs
  PeerStatisticsTests.cs
  FindingEngineTests.cs
  AssetQueryServiceTests.cs
  ExportTests.cs
  PrivacySanitizerTests.cs
  ReportCompatibilityTests.cs

tests/CS2AssetPerformanceAuditor.AdapterTests/
  CS2AssetPerformanceAuditor.AdapterTests.csproj
  PublicApiContractTests.cs

docs/validation/runtime-validation.md
.github/workflows/core-ui-tests.yml
```

The game project remains one assembly initially. Pure Core tests link only game-independent files, matching the proven Runtime Profiler pattern. A separate Core assembly is not introduced unless a later approved plan amendment shows a concrete need.

---

# Milestone 0 — Compatibility foundation and testable Core

### Task 1: Scaffold a fully green repository baseline

**Files:**
- Create: `CS2AssetPerformanceAuditor.sln`
- Create: `.gitignore`
- Create: `README.md`
- Create: `src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj`
- Create: `src/CS2AssetPerformanceAuditor/Mod.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/ProjectInfo.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/ScaffoldSmokeTests.cs`
- Create: `UI/package.json`
- Create: `UI/tsconfig.json`
- Create: `UI/webpack.config.js`
- Create: `UI/src/index.tsx`
- Create: `UI/src/AssetAuditorRoot.tsx`
- Create: `UI/src/__tests__/smoke.test.tsx`

**Interfaces:**
- Produces: `ProjectInfo.ProductName == "CS2 Asset Performance Auditor"`, `ProjectInfo.UsesHarmony == false`, buildable net48 mod project, green net8 NUnit project, green Vitest/Webpack UI baseline.

- [ ] **Step 1: Write failing scaffold smoke tests**

`ScaffoldSmokeTests` asserts `ProjectInfo.ProductName` and `UsesHarmony == false`. UI smoke test renders the minimal `AssetAuditorRoot` with `renderToStaticMarkup` and asserts the product title.

- [ ] **Step 2: Run and verify the tests fail because the production types/files do not yet exist**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj
cd UI && npm install && npm test
```

- [ ] **Step 3: Implement the minimal scaffold**

Game project: `TargetFramework=net48`, `LangVersion=9`, `OutputType=Library`, `RootNamespace/AssemblyName=CS2AssetPerformanceAuditor`, CS2 `Mod.props`/`Mod.targets`, required public Game/Colossal/Unity references only, no Harmony package. UI baseline: React 18, TypeScript, Webpack, Vitest, Node >=18.

- [ ] **Step 4: Verify the task ends green**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj
cd UI && npm test && npm run build
```

Expected: PASS. Also run `dotnet build src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj -c Debug` where `CSII_TOOLPATH` is available.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "build: scaffold asset performance auditor"
```

### Task 2: Add capability, observation, and Prefab domain contracts

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Core/Capabilities/CapabilityId.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Capabilities/CapabilityState.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Capabilities/CapabilityReport.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Capabilities/CompatibilityState.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Observations/Availability.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Observations/ObservationOrigin.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Observations/Observation.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Prefabs/PrefabKey.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Prefabs/PrefabTraits.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Prefabs/AssetOriginEvidence.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Prefabs/PrefabRecord.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/ObservationTests.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/PrefabProjectionTests.cs`
- Modify: `tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj`

**Interfaces:**
- Produces: immutable `PrefabKey(string prefabId, string prefabType)`, `Observation<T>`, `CapabilityReport`, `PrefabRecord`. Implement with ordinary immutable classes/readonly structs rather than requiring C# record runtime compatibility.

- [ ] **Step 1: Write failing tests**

Assert available numeric zero differs from `NotScanned`; unsupported observations expose no fabricated value; PrefabKey ignores runtime Entity index; overlapping source evidence is preserved; Prefab traits can be combined; capability states may be mixed per feature.

- [ ] **Step 2: Run focused tests and verify failure**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj --filter "ObservationTests|PrefabProjectionTests"
```

- [ ] **Step 3: Implement the contracts without any Game/Unity/Colossal references**

- [ ] **Step 4: Re-run focused tests**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: add core observation contracts"
```

### Task 3: Implement Census Query Profile v1, scan options, and world-safe atomic snapshots

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Core/Census/CensusCountKind.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Census/CensusCounters.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Census/CensusPresence.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Census/CensusQueryProfile.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Census/ScanOptions.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Census/CensusEntry.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Census/CensusSnapshot.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Census/CensusReducer.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Scanning/PublishedAuditState.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/CensusReducerTests.cs`

**Interfaces:**
- Produces: `CensusQueryProfile.V1 == "1"`; `CensusCounters`; `CensusReducer.AddObject(PrefabKey,bool isSubordinate)`; `AddNetworkEdge(PrefabKey)`; `CensusSnapshot` containing `WorldGeneration`, `queryProfileVersion`, `scanOptions`, timestamp, catalog generation; `PublishedAuditState.ResetForWorld(long worldGeneration)` and publish-on-success only.

- [ ] **Step 1: Write failing profile/snapshot tests**

Assert top-level/subordinate/live/network semantics; one logical subordinate input contributes once; omitted subordinate collection is `NotScanned`; cancelled/failed scan does not replace old snapshot; `ResetForWorld(2)` invalidates a snapshot from world generation 1; query profile and scan options are carried in the snapshot.

- [ ] **Step 2: Run and verify failure**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj --filter CensusReducerTests
```

- [ ] **Step 3: Implement reducer and immutable snapshot publication**

For supported object metrics enforce `LiveObjectReferences = TopLevelObjects + SubordinateObjects`.

- [ ] **Step 4: Re-run tests**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: define census query profile v1"
```

### Task 4: Implement the game-independent scan lifecycle

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Core/Scanning/ScanKind.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Scanning/ScanStage.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Scanning/ScanState.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Scanning/ScanProgress.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Scanning/ScanSession.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/ScanSessionTests.cs`

**Interfaces:**
- Produces: `ScanSession.Start(...)`, `RequestCancellation()`, validated stage transitions, exact or indeterminate `ScanProgress`, publication permitted only from successful Finalizing/Completed path.

- [ ] **Step 1: Write failing state-machine tests**

Cover valid Phase 1 order, rejected backwards transitions, cancel during managed reduction, cancellation-requested state during scheduled capture, publication only on success, and indeterminate progress when total is unknown.

- [ ] **Step 2: Run focused tests and verify failure**

- [ ] **Step 3: Implement the state machine with no JobHandle/Game dependency**

- [ ] **Step 4: Run tests**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: add scan lifecycle state machine"
```

### Task 5: Add public-API contract tests, capability probing, and idle game systems

**Files:**
- Create: `tests/CS2AssetPerformanceAuditor.AdapterTests/CS2AssetPerformanceAuditor.AdapterTests.csproj`
- Create: `tests/CS2AssetPerformanceAuditor.AdapterTests/PublicApiContractTests.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Capabilities/CapabilityProbe.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/AssetAuditSystem.cs`
- Create: `src/CS2AssetPerformanceAuditor/UI/AssetAuditUISystem.cs`
- Create: `src/CS2AssetPerformanceAuditor/Setting.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Mod.cs`

**Interfaces:**
- Produces: local net48 contract tests for required 1.6.2f1 public members; `CapabilityProbe.Probe(World)`; heavy-work coordinator `AssetAuditSystem`; bindings-only `AssetAuditUISystem`.

- [ ] **Step 1: Write failing Adapter Contract tests**

Test public contracts for `PrefabSystem.GetPrefab/TryGetPrefab`, `PrefabData`, `PrefabRef`, `RenderPrefab`, `LodProperties`, `GeometryAsset`, `SurfaceAsset`, and `TextureAsset`. Reflection may inspect **public** members only in this test project.

- [ ] **Step 2: Run local adapter tests/build and verify expected failure before implementation**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.AdapterTests/CS2AssetPerformanceAuditor.AdapterTests.csproj
```

Expected: FAIL until required adapter assumptions/tests compile against configured CS2 references.

- [ ] **Step 3: Implement capability probe and idle systems**

Probe capabilities independently. A single failed collector yields capability degradation rather than startup-wide failure. `AssetAuditSystem.OnUpdate` while idle must not enumerate the world/catalog.

- [ ] **Step 4: Verify**

Run Pure Core tests, local Adapter Contract tests, and game project build where the CS2 toolchain is available. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: add compatibility foundation"
```

**Milestone 0 gate:** Pure Core/UI baselines pass; local public API contracts pass against supplied/current references; game project builds without Harmony; idle systems do no full scans.

---

# Milestone 1 — Prefab Catalog + Asset Instance Census

### Task 6: Implement Prefab catalog access, classification, and source evidence

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Prefabs/IPrefabCatalogAccess.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Prefabs/PrefabCatalogAccess.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Prefabs/PrefabClassifier.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Prefabs/SourceMetadataReader.cs`
- Modify: `src/CS2AssetPerformanceAuditor/GameIntegration/AssetAuditSystem.cs`
- Modify: `tests/CS2AssetPerformanceAuditor.Tests/PrefabProjectionTests.cs`

**Interfaces:**
- Produces: capture of `PrefabData` entities, public `PrefabSystem.TryGetPrefab` resolution, stable PrefabKey, frame-sliced PrefabRecord processing, runtime-only `Entity -> PrefabKey` current-world map.

- [ ] **Step 1: Add failing classification/source tests**

Pin Building/ServiceBuilding/Prop/Tree/Vehicle/Network trait composition and source-evidence projection without collapsing overlapping evidence.

- [ ] **Step 2: Run focused tests and verify failure**

- [ ] **Step 3: Implement catalog capture and frame-budgeted processing**

Never enumerate internal PrefabSystem collections and never use Reflection.

- [ ] **Step 4: Run Core tests + Adapter tests + game build**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: add prefab catalog collector"
```

### Task 7: Implement read-only ECS Census capture and coordinator cleanup

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Census/CensusSample.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Census/CensusCaptureBuffers.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Census/CensusAccess.cs`
- Modify: `src/CS2AssetPerformanceAuditor/GameIntegration/AssetAuditSystem.cs`
- Modify: `tests/CS2AssetPerformanceAuditor.Tests/CensusReducerTests.cs`
- Modify: `tests/CS2AssetPerformanceAuditor.Tests/ScanSessionTests.cs`

**Interfaces:**
- Consumes: runtime Prefab map, `CensusReducer`, `ScanSession`, `WorldGeneration`.
- Produces: async/chunk capture for object and network-edge universes; each logical sample classified exactly once as top-level/subordinate/network-edge; safe native-buffer ownership.

- [ ] **Step 1: Add failing adapter-facing regression tests to Core seams**

Assert a classification result with both Owner+Controller markers becomes one subordinate sample; world generation mismatch blocks publication; cancel/fail leaves previous same-world snapshot intact.

- [ ] **Step 2: Run tests and verify failure**

- [ ] **Step 3: Implement read-only EntityQueries/jobs**

Use verified 1.6.2f1 components. Exclude temporary/deleted/overridden/non-current states required by Query Profile v1. Schedule and return; do not immediately `Complete()` in the same frame.

- [ ] **Step 4: Implement polling, frame-budgeted reduction, world-change cancellation, and `finally`-equivalent buffer disposal**

- [ ] **Step 5: Verify Core/Adapter/game build**

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src tests
git commit -m "feat: capture asset instance census"
```

### Task 8: Add bounded query service, diagnostics base, privacy sanitizer, and Phase 1 JSON

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Core/Query/AssetQuery.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Query/AssetSort.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Query/AssetPage.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Query/AssetQueryService.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Diagnostics/DiagnosticCode.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Diagnostics/DiagnosticAggregator.cs`
- Create: `src/CS2AssetPerformanceAuditor/Export/ReportSchema.cs`
- Create: `src/CS2AssetPerformanceAuditor/Export/AuditReport.cs`
- Create: `src/CS2AssetPerformanceAuditor/Export/AuditReportBuilder.cs`
- Create: `src/CS2AssetPerformanceAuditor/Export/AuditReportSerializer.cs`
- Create: `src/CS2AssetPerformanceAuditor/Export/PrivacySanitizer.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/AssetQueryServiceTests.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/ExportTests.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/PrivacySanitizerTests.cs`

**Interfaces:**
- Produces: bounded `AssetPage Query(AssetQuery)`; canonical Phase 1 JSON containing schema/rule/query-profile/scan-options/mod/game/timestamp/capability/catalog/census metadata; aggregated diagnostics; no private local paths/runtime Entity indexes.

- [ ] **Step 1: Write failing tests**

Cover name/PrefabID/source search, type/source/presence filters, bounded page size, type-aware `Instances` + `CountKind`, zero vs `NotScanned`, required report versions, repeated diagnostic aggregation, and absence of Windows username/hostname/absolute paths/entity indexes from export.

- [ ] **Step 2: Run focused tests and verify failure**

- [ ] **Step 3: Implement query/diagnostic/export pipeline**

Use `DataContractJsonSerializer` for net48 compatibility unless the serializer contract tests demonstrate a concrete blocker.

- [ ] **Step 4: Run tests**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: add census query and export pipeline"
```

### Task 9: Build the Phase 1 in-game UI with bounded bindings

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/UI/UiContracts.cs`
- Create: `src/CS2AssetPerformanceAuditor/UI/UiSnapshotBuilder.cs`
- Modify: `src/CS2AssetPerformanceAuditor/UI/AssetAuditUISystem.cs`
- Create: `UI/src/bindings.ts`
- Create: `UI/src/types.ts`
- Modify: `UI/src/AssetAuditorRoot.tsx`
- Create: `UI/src/assetAuditor.module.scss`
- Create: `UI/src/components/ScanStatus.tsx`
- Create: `UI/src/components/VirtualAssetTable.tsx`
- Create: `UI/src/tabs/OverviewTab.tsx`
- Create: `UI/src/tabs/AssetsTab.tsx`
- Create: `UI/src/tabs/CensusTab.tsx`
- Create: `UI/src/tabs/SettingsTab.tsx`
- Create: `UI/src/__tests__/queryState.test.ts`
- Create: `UI/src/__tests__/scanStatus.test.tsx`
- Create: `UI/src/__tests__/assetTable.test.tsx`
- Create: `UI/src/__tests__/escapeClose.test.tsx`

**Interfaces:**
- Produces: Run Census/Cancel/query/export/settings triggers; bounded result pages; no full-catalog binding on each UI update.

- [ ] **Step 1: Write failing UI tests using Vitest + `renderToStaticMarkup` where possible**

Assert `Not scanned` vs zero, exact vs indeterminate progress, visible CountKind, bounded query windows, Escape/close behavior without scan cancellation, and active progress visible after reopen.

- [ ] **Step 2: Run UI tests and verify failure**

```bash
cd UI && npm test
```

- [ ] **Step 3: Implement Overview/Assets/Census/Settings and debounced server-side query flow**

Warnings/Compare remain hidden until their milestones.

- [ ] **Step 4: Run UI tests/build + C# tests/build**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CS2AssetPerformanceAuditor/UI UI
git commit -m "feat: add census audit UI"
```

**Milestone 1 gate:** Prefab Catalog + Snapshot Census work end-to-end; cancellation/world change are safe; counts are explicit; UI data transfer is bounded; minimal JSON export is truthful.

---

# Milestone 2 — Render Graph + Geometry / LOD Auditor

### Task 10: Implement render-domain contracts and verified resolver coverage

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/RenderAssetKey.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/RenderCoverage.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/RenderRelationKind.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/PrefabRenderRelation.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/RenderAssetRecord.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/IRenderAssetResolver.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/ObjectGeometryResolver.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/RenderGraphBuilder.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/RenderGraphTests.cs`

**Interfaces:**
- Produces: explicit `RenderCoverage`; Prefab→RenderAsset relations; deduplicated RenderAsset keys; only verified resolver families report Supported.

- [ ] **Step 1: Write failing tests**

Assert unresolved/unsupported family is not zero geometry; two Prefabs sharing one RenderAssetKey produce one resource record; relation kind remains attributable.

- [ ] **Step 2: Run and verify failure**

- [ ] **Step 3: Implement ObjectGeometry resolver using verified `m_Meshes`/LOD relations only**

No recursive Reflection. Network render path remains explicit Unknown/Unsupported until separately verified.

- [ ] **Step 4: Run tests + Adapter tests + game build**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: add render graph resolution"
```

### Task 11: Collect GeometryAsset metadata and topology-aware geometry observations

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/GeometryObservation.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/MeshObservation.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/SubMeshObservation.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/GeometryAssetReader.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/GeometryMathTests.cs`
- Modify: `src/CS2AssetPerformanceAuditor/GameIntegration/AssetAuditSystem.cs`

**Interfaces:**
- Produces: observed mesh/vertex/index/submesh/bounds/compressed payload; triangle count only for Triangle topology; GeometryAsset cache keyed per analysis generation.

- [ ] **Step 1: Write failing tests**

Triangles+300 indices => 100; Lines+300 => NotApplicable; missing metadata => Failed/Unsupported, not zero; shared geometry is read once per generation.

- [ ] **Step 2: Run and verify failure**

- [ ] **Step 3: Implement metadata-first reader**

Normal Asset Audit must not call `ObtainMeshes()` across all assets.

- [ ] **Step 4: Verify**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: audit geometry metadata"
```

### Task 12: Add LOD metrics, peer statistics, and Phase 2 findings

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/LodMetrics.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Findings/FindingStatus.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Findings/FindingBasis.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Findings/FindingCategory.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Findings/Finding.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Findings/RuleSetInfo.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Findings/PeerStatistics.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Findings/FindingEngine.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/PeerStatisticsTests.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/FindingEngineTests.cs`

**Interfaces:**
- Produces: deterministic LOD retention/reduction; Finding with RuleId/status/category/evidence/basis/rule version; geometry/LOD finding families from the approved spec.

- [ ] **Step 1: Write failing tests**

Cover exact retention math; no lower LOD is observation/notice, not automatic warning; heuristic => PotentialIssue; broken required reference may be Warning; insufficient peer sample => unavailable; missing evidence => no fabricated finding; same-category population selection is stable.

- [ ] **Step 2: Run and verify failure**

- [ ] **Step 3: Implement pure Core statistics/rule engine with versioned heuristic definitions**

- [ ] **Step 4: Run tests**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: add geometry and lod findings"
```

**Milestone 2 gate:** verified render families resolve to a deduplicated Render Graph; Geometry/LOD metadata is truthful without mass Mesh materialization; findings carry evidence and basis.

---

# Milestone 3 — Surface / Texture Auditor

### Task 13: Collect SurfaceAsset/TextureAsset metadata and estimate logical texture payload

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/SurfaceObservation.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/TextureObservation.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/TextureFootprintEstimator.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/SurfaceAssetReader.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/TextureAssetReader.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/TextureFootprintTests.cs`
- Modify: `src/CS2AssetPerformanceAuditor/GameIntegration/AssetAuditSystem.cs`

**Interfaces:**
- Produces: observed surface/template/VT/property/texture-relation metadata; observed texture dimensions/format/mips/filter/wrap/aniso; Estimated logical full payload; shared texture cache.

- [ ] **Step 1: Write failing footprint/resource tests**

Cover representative block-compressed/uncompressed formats, block rounding, mip chains, unsupported formats, estimated-vs-VRAM terminology, and one unique shared texture despite multiple references.

- [ ] **Step 2: Run and verify failure**

- [ ] **Step 3: Implement metadata-first readers and estimator**

Prefer SurfaceAsset/TextureAsset metadata/property APIs; do not materialize Unity Material/Texture globally. Deterministically unload temporary property data when owned by the adapter.

- [ ] **Step 4: Verify**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: audit surface and texture metadata"
```

### Task 14: Add material/texture/exposure finding families and resource aggregation

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/ResourceAggregation.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Core/Findings/FindingEngine.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Core/Findings/RuleSetInfo.cs`
- Modify: `tests/CS2AssetPerformanceAuditor.Tests/FindingEngineTests.cs`
- Modify: `tests/CS2AssetPerformanceAuditor.Tests/TextureFootprintTests.cs`

**Interfaces:**
- Produces: referenced-vs-unique texture/resource totals and Phase 3 material/texture/exposure/integrity finding families.

- [ ] **Step 1: Write failing tests**

Shared texture referenced three times => one unique payload; high city/subordinate exposure remains exposure evidence, not render-cost proof; failed texture read remains item-level finding and unrelated assets still evaluate; no global score is produced.

- [ ] **Step 2: Run and verify failure**

- [ ] **Step 3: Implement resource aggregation and Phase 3 rules**

- [ ] **Step 4: Run tests**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: add surface texture and exposure findings"
```

**Milestone 3 gate:** Surface/Texture metadata and safe estimates are available; actual VRAM is never inferred; shared resources and failures are handled truthfully.

---

# Milestone 4 — Analysis UX, export, Deep Inspection, settings, validation

### Task 15: Complete report compatibility, full JSON, and CSV summary export

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Export/ReportCompatibility.cs`
- Create: `src/CS2AssetPerformanceAuditor/Export/CsvSummaryExporter.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Export/AuditReport.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Export/AuditReportBuilder.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Export/AuditReportSerializer.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Core/Query/AssetQueryService.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/ReportCompatibilityTests.cs`
- Modify: `tests/CS2AssetPerformanceAuditor.Tests/ExportTests.cs`

**Interfaces:**
- Produces: hierarchical full JSON; flat CSV; export scopes Full/Filtered/Selected/Census/Findings; compatibility warning for schema/query-profile/materially-different scan options.

- [ ] **Step 1: Write failing tests**

Assert hierarchy preserves evidence classes; CSV remains flat; incompatible profiles/options warn before census comparison; runtime IDs/private paths stay absent; additive optional fields do not silently change existing meaning.

- [ ] **Step 2: Run and verify failure**

- [ ] **Step 3: Implement report compatibility and export builders**

Increment schema version if implementation changes an already-defined field meaning.

- [ ] **Step 4: Run tests**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: add full audit report exports"
```

### Task 16: Implement selected-asset Deep Inspection with explicit ownership boundaries

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/DeepInspectionReader.cs`
- Modify: `src/CS2AssetPerformanceAuditor/GameIntegration/AssetAuditSystem.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Core/Rendering/RenderAssetRecord.cs`
- Create: `docs/validation/runtime-validation.md`

**Interfaces:**
- Consumes: one selected RenderAssetKey.
- Produces: copied shader/material/advanced-binding observations only; Observation Store never retains Unity Material/Mesh/Texture objects.

- [ ] **Step 1: Write the runtime ownership scenarios before implementation**

Add explicit NOT RUN scenarios for success, cancellation, exception, repeated inspection, and game-owned shared objects.

- [ ] **Step 2: Implement `try/finally`-equivalent cleanup and ownership rules**

Destroy/unload/dispose only resources acquired/owned by the mod. Copy retained values to Core observations before cleanup.

- [ ] **Step 3: Run all automated tests and game build**

Expected: PASS. Runtime ownership scenarios remain NOT RUN until actually executed.

- [ ] **Step 4: Commit**

```bash
git add src docs/validation/runtime-validation.md
git commit -m "feat: add selected asset deep inspection"
```

### Task 17: Build Warnings, Asset Details, Render Structure, Compare, and final settings UI

**Files:**
- Create: `UI/src/components/FindingBadge.tsx`
- Create: `UI/src/components/EvidencePanel.tsx`
- Create: `UI/src/tabs/WarningsTab.tsx`
- Create: `UI/src/tabs/CompareTab.tsx`
- Create: `UI/src/details/AssetDetails.tsx`
- Create: `UI/src/details/RenderStructure.tsx`
- Create: `UI/src/details/GeometryDetails.tsx`
- Create: `UI/src/details/LodDetails.tsx`
- Create: `UI/src/details/MaterialDetails.tsx`
- Create: `UI/src/details/TextureDetails.tsx`
- Create: `UI/src/__tests__/findings.test.tsx`
- Create: `UI/src/__tests__/settings.test.tsx`
- Modify: `UI/src/AssetAuditorRoot.tsx`
- Modify: `UI/src/tabs/AssetsTab.tsx`
- Modify: `UI/src/tabs/SettingsTab.tsx`
- Modify: `src/CS2AssetPerformanceAuditor/Setting.cs`
- Modify: `src/CS2AssetPerformanceAuditor/UI/UiContracts.cs`
- Modify: `src/CS2AssetPerformanceAuditor/UI/AssetAuditUISystem.cs`

**Interfaces:**
- Produces: expandable evidence, 2–4 asset comparison, render-structure detail, bounded settings, scan options, no automatic winner/global score.

- [ ] **Step 1: Write failing UI/settings tests**

Assert finding status/evidence/basis/rule version; unsupported render coverage != zero geometry; Compare supports 2–4 and no winner; estimated payload != GPU residency; disabled optional metric changes scanOptions and renders NotScanned; unsafe settings clamp/reject; Escape closes UI without cancel.

- [ ] **Step 2: Run UI tests and verify failure**

- [ ] **Step 3: Implement detail/warnings/compare/settings flow**

List queries remain bounded; one selected asset may request its full detail hierarchy.

- [ ] **Step 4: Run UI tests/build + C# tests/build**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add UI src
git commit -m "feat: add asset analysis ux"
```

### Task 18: Finish diagnostics/self-telemetry, runtime validation matrix, update procedure, and CI-safe checks

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Core/Diagnostics/ScanTelemetry.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Core/Diagnostics/DiagnosticAggregator.cs`
- Modify: `src/CS2AssetPerformanceAuditor/GameIntegration/AssetAuditSystem.cs`
- Modify: `src/CS2AssetPerformanceAuditor/UI/UiContracts.cs`
- Modify: `docs/validation/runtime-validation.md`
- Modify: `README.md`
- Create: `.github/workflows/core-ui-tests.yml`

**Interfaces:**
- Produces: low-cost elapsed/processed/max/P95 slice telemetry where measurable; Diagnostics showing compatibility/capabilities/`Harmony: not used`; explicit runtime PASS/FAIL/NOT RUN/BLOCKED matrix; CI that does not redistribute game DLLs.

- [ ] **Step 1: Add failing Core tests for telemetry/diagnostic aggregation where game-independent**

Repeated same code aggregates; telemetry percentile/max calculations are deterministic; no heavy continuous profiler dependency is introduced.

- [ ] **Step 2: Complete runtime validation scenarios**

Include Vanilla small city, large city, custom-asset-heavy city, broken/incomplete asset, world change during scan, UI close/Escape during scan, and Deep Inspection ownership. Record entity/Prefab/render-asset counts, total time, max/P95 managed slice, memory/allocation delta where measurable.

- [ ] **Step 3: Add CI and game-update procedure**

CI runs Pure Core tests and UI tests/build. Local Adapter/game build remain documented local checks when CS2 references/toolchain are required. Update order: compile new DLL/toolchain -> Core tests -> Adapter contracts -> capability probe -> Vanilla validation -> custom-asset validation -> large-city performance validation -> mark Supported.

- [ ] **Step 4: Run all executable automated verification**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj
cd UI && npm test && npm run build
```

Also run Adapter tests/game Release build where the local CS2 toolchain is available. Expected: all executed checks PASS; unexecuted game scenarios remain NOT RUN/BLOCKED.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "test: add auditor validation and ci"
```

**Milestone 4 gate:** Asset Details, Findings, Compare, full exports, selected Deep Inspection, settings, diagnostics, validation docs, and CI-safe checks are complete. Runtime status remains evidence-based.

---

# Final verification

### Task 19: Whole-project spec-conformance review

**Files:**
- Review: `docs/superpowers/specs/2026-09-27-asset-performance-auditor-design.md`
- Review: `docs/superpowers/plans/2026-09-27-asset-performance-auditor-implementation.md`
- Review: `docs/validation/runtime-validation.md`
- Modify only files required by discovered defects.

**Interfaces:**
- Produces: code/tests/docs whose claims all agree with the approved spec.

- [ ] **Step 1: Run all automated verification**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj
cd UI && npm test && npm run build
```

Run local Adapter tests and `dotnet build ... -c Release` where the CS2 toolchain exists.

- [ ] **Step 2: Search source/project files for prohibited regressions**

Confirm no `Lib.Harmony`, Harmony patches, runtime private-member Reflection fallback, unconditional `indexCount / 3`, global performance score, periodic automatic full scan, or committed game DLLs.

- [ ] **Step 3: Re-check all five Review Focus items**

Each must have an automated regression test and/or explicit executed runtime validation scenario owned by the relevant task. Add any missing coverage before completion.

- [ ] **Step 4: Reconcile runtime claims**

Anything not actually executed remains NOT RUN/BLOCKED. Build/unit-test evidence never becomes in-game PASS.

- [ ] **Step 5: Commit verification fixes if any**

```bash
git add -A
git commit -m "test: verify asset performance auditor"
```

Skip this commit if no files changed.

---

## Delivery map

- **Phase 0:** Tasks 1–5 — green repository baseline, Core contracts, scan semantics, Adapter contracts, compatibility/capability layer.
- **Phase 1:** Tasks 6–9 — Prefab Catalog, Snapshot Census, bounded query/export/UI.
- **Phase 2:** Tasks 10–12 — Render Graph, Geometry/LOD analysis, geometry/LOD findings.
- **Phase 3:** Tasks 13–14 — Surface/Texture analysis, safe estimates, material/texture/exposure findings.
- **Phase 4:** Tasks 15–18 — full export, Deep Inspection, Analysis UX, settings, diagnostics, validation/CI.
- **Final verification:** Task 19.

Stop at each milestone gate for tests/review before advancing. Internal ECS/job optimizations may change only when profiling justifies them and must preserve the public/domain semantics in the approved spec and this plan.
