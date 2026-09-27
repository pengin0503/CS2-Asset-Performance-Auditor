# CS2 Asset Performance Auditor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a read-mostly Cities: Skylines II Code Mod that catalogs Prefabs, captures an explicit snapshot census of city exposure, audits render/geometry/LOD/surface/texture metadata, produces evidence-backed findings, and exposes the results through a scalable in-game UI and versioned exports.

**Architecture:** Keep all CS2/ECS/AssetDatabase/Unity dependencies behind a narrow Game Integration layer. Convert runtime data into game-independent domain observations, publish scans atomically through a coordinator/store, run findings/query/export logic in pure Core code, and expose bounded result windows to a React/TypeScript UI. Phases 0–4 are one architecture but are implemented through explicit milestone gates so each phase remains reviewable and usable before the next begins.

**Tech Stack:** C# / .NET Framework 4.8 CS2 Code Mod, Unity Entities/ECS, Colossal AssetDatabase, NUnit on .NET 8 for Pure Core tests, React 18 + TypeScript + Webpack + Vitest for UI, JSON/CSV export.

**Spec:** `docs/superpowers/specs/2026-09-27-asset-performance-auditor-design.md`

## Global Constraints

- Baseline game target: Cities: Skylines II `1.6.2f1`.
- Phases 1–4 use **no Harmony dependency, Prefix, Postfix, or Transpiler**.
- Reflection is not part of the current implementation and must not be introduced as an automatic fallback.
- The mod is read-mostly: scanners do not mutate gameplay entities or Prefabs.
- Full-world Census and full Asset Audit are user-triggered snapshots, not always-on trackers.
- Gameplay Prefab identity and render-resource identity remain separate.
- `Observed`, `Derived`, `Estimated`, `NotScanned`, `NotApplicable`, `Unsupported`, and `Failed` semantics must survive through Core, UI, and JSON export.
- Failed/cancelled scans must not replace the last successfully published snapshot.
- A numeric zero must never substitute for `NotScanned`, `NotApplicable`, `Unsupported`, or `Failed`.
- Static metadata must not be described as actual per-frame rendering cost or actual GPU residency.
- No global 0–100 performance score.
- Heavy Unity object materialization is restricted to selected-asset Deep Inspection.
- Shared RenderPrefab/GeometryAsset/SurfaceAsset/TextureAsset resources are deduplicated.
- Game DLLs are local/toolchain references and are not committed to the repository.
- New game versions remain `Untested` until runtime validation marks them `Supported`.
- Phase 5 Runtime Evidence is outside this plan.

## Review Focus

1. **World changes during active work:** active capture/reduction must cancel/dispose safely, and previous-world census data must never be shown as current.
2. **Ambiguous census classification:** an Entity carrying multiple subordinate markers must count once, and unsupported/omitted counters must remain `NotScanned`, not zero.
3. **Shared render resources:** multiple Prefabs referencing the same render/geometry/surface/texture resource must not multiply static resource totals or collector work.
4. **Broken/incomplete custom assets:** one bad Prefab/RenderPrefab/Texture must degrade that item/capability without aborting unrelated data collection where safe.
5. **Deep Inspection ownership:** temporary Unity/AssetDatabase objects must be released on success, cancellation, and exception paths, while game-owned shared objects must never be destroyed by the mod.

---

# Repository structure locked by this plan

```text
CS2AssetPerformanceAuditor.sln
README.md
.gitignore

src/CS2AssetPerformanceAuditor/
  CS2AssetPerformanceAuditor.csproj
  Mod.cs
  Setting.cs
  Core/
    Capabilities/
      CapabilityId.cs
      CapabilityState.cs
      CapabilityReport.cs
      CompatibilityState.cs
    Observations/
      Availability.cs
      ObservationOrigin.cs
      Observation.cs
    Prefabs/
      PrefabKey.cs
      PrefabTraits.cs
      AssetOriginEvidence.cs
      PrefabRecord.cs
    Census/
      CensusCountKind.cs
      CensusCounters.cs
      CensusPresence.cs
      CensusQueryProfile.cs
      ScanOptions.cs
      CensusEntry.cs
      CensusSnapshot.cs
      CensusReducer.cs
    Rendering/
      RenderAssetKey.cs
      RenderCoverage.cs
      RenderRelationKind.cs
      PrefabRenderRelation.cs
      RenderAssetRecord.cs
      GeometryObservation.cs
      MeshObservation.cs
      SubMeshObservation.cs
      SurfaceObservation.cs
      TextureObservation.cs
    Findings/
      FindingStatus.cs
      FindingBasis.cs
      FindingCategory.cs
      Finding.cs
      RuleSetInfo.cs
      FindingEngine.cs
      PeerStatistics.cs
    Scanning/
      ScanKind.cs
      ScanStage.cs
      ScanState.cs
      ScanProgress.cs
      ScanSession.cs
      PublishedAuditState.cs
    Query/
      AssetQuery.cs
      AssetSort.cs
      AssetPage.cs
      AssetQueryService.cs
  GameIntegration/
    Capabilities/
      CapabilityProbe.cs
    Prefabs/
      IPrefabCatalogAccess.cs
      PrefabCatalogAccess.cs
      PrefabClassifier.cs
      SourceMetadataReader.cs
    Census/
      CensusSample.cs
      CensusCaptureBuffers.cs
      CensusAccess.cs
    Rendering/
      IRenderAssetResolver.cs
      ObjectGeometryResolver.cs
      RenderGraphBuilder.cs
      GeometryAssetReader.cs
      SurfaceAssetReader.cs
      TextureAssetReader.cs
      DeepInspectionReader.cs
    AssetAuditSystem.cs
  Export/
    ReportSchema.cs
    AuditReport.cs
    AuditReportBuilder.cs
    AuditReportSerializer.cs
    CsvSummaryExporter.cs
    PrivacySanitizer.cs
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
      ScanStatus.tsx
      VirtualAssetTable.tsx
      FindingBadge.tsx
      EvidencePanel.tsx
    tabs/
      OverviewTab.tsx
      AssetsTab.tsx
      CensusTab.tsx
      WarningsTab.tsx
      CompareTab.tsx
      SettingsTab.tsx
    details/
      AssetDetails.tsx
      RenderStructure.tsx
      GeometryDetails.tsx
      LodDetails.tsx
      MaterialDetails.tsx
      TextureDetails.tsx
    __tests__/
      queryState.test.ts
      scanStatus.test.tsx
      assetTable.test.tsx
      findings.test.tsx
      escapeClose.test.tsx

tests/CS2AssetPerformanceAuditor.Tests/
  CS2AssetPerformanceAuditor.Tests.csproj
  ObservationTests.cs
  CensusReducerTests.cs
  ScanSessionTests.cs
  GeometryMathTests.cs
  TextureFootprintTests.cs
  PeerStatisticsTests.cs
  FindingEngineTests.cs
  AssetQueryServiceTests.cs
  ExportTests.cs
  PrivacySanitizerTests.cs
  ReportCompatibilityTests.cs

docs/validation/
  runtime-validation.md
```

The game project remains one assembly initially; the Pure Core test project links only game-independent source files, following the proven pattern used by the existing Runtime Profiler. If a later implementation task proves a separate Core assembly materially simpler, that requires an explicit plan amendment rather than ad-hoc restructuring.

---

## Milestone 0 — Compatibility foundation and testable Core

### Task 1: Scaffold the solution, game project, UI project, and Pure Core test project

**Files:**
- Create: `CS2AssetPerformanceAuditor.sln`
- Create: `.gitignore`
- Create: `src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj`
- Create: `src/CS2AssetPerformanceAuditor/Mod.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj`
- Create: `UI/package.json`
- Create: `UI/tsconfig.json`
- Create: `UI/webpack.config.js`
- Create: `UI/src/index.tsx`
- Create: `README.md`

**Interfaces:**
- Consumes: local `$(CSII_TOOLPATH)/Mod.props` and `Mod.targets`; no committed game DLLs.
- Produces: buildable `CS2AssetPerformanceAuditor` net48 mod project, `CS2AssetPerformanceAuditor.Tests` net8 NUnit project, and React/TypeScript UI build/test entry points.

- [ ] **Step 1: Create the failing smoke tests/build expectations**

Add one NUnit smoke test asserting the expected root namespace is loadable from linked Core source once Task 2 adds it, and one Vitest smoke test importing the UI root module. At this step they may fail because the Core/UI root types are not present yet.

- [ ] **Step 2: Run the baseline commands and record the expected initial failures**

Run:

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj
cd UI && npm install && npm test
```

Expected: test/build setup resolves; smoke tests fail only for the intentionally missing root/Core types, not due to malformed project configuration.

- [ ] **Step 3: Create the minimal project scaffolding**

Use `TargetFramework=net48`, `OutputType=Library`, `RootNamespace=CS2AssetPerformanceAuditor`, `AssemblyName=CS2AssetPerformanceAuditor`, import CS2 `Mod.props`/`Mod.targets`, reference only the game/Colossal/Unity assemblies needed by the approved design, and do **not** reference `Lib.Harmony`.

Use a UI toolchain compatible with the existing Runtime Profiler baseline: React 18, TypeScript, Webpack, Vitest, Node >=18.

- [ ] **Step 4: Verify project configuration**

Run:

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj
cd UI && npm test && npm run build
```

Expected: only tests that intentionally await Task 2 domain types remain failing; package/build configuration itself succeeds.

- [ ] **Step 5: Commit**

```bash
git add CS2AssetPerformanceAuditor.sln .gitignore README.md src tests UI
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
- Modify: `tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj`

**Interfaces:**
- Consumes: no game types.
- Produces:
  - `readonly record struct PrefabKey(string PrefabId, string PrefabType)`
  - `sealed record Observation<T>(T? Value, Availability Availability, ObservationOrigin Origin, DateTimeOffset CapturedAt)`
  - `sealed class CapabilityReport` with `Get(CapabilityId)` and immutable/public-read capability entries.
  - `sealed record PrefabRecord(PrefabKey Key, string DisplayName, PrefabTraits Traits, AssetOriginEvidence Origin)`.

- [ ] **Step 1: Write failing domain-semantic tests**

Tests must assert:
- `Observation<int>` can represent `Available(0)` distinctly from `NotScanned`.
- `Unsupported` observations do not expose a fabricated numeric value.
- `PrefabKey` equality does not depend on runtime Entity index.
- a capability report can represent mixed `Supported`, `Degraded`, `Unsupported`, and `Failed` capabilities.

- [ ] **Step 2: Run the focused tests and verify failure**

Run:

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj --filter ObservationTests
```

Expected: FAIL because the contracts do not exist.

- [ ] **Step 3: Implement the domain contracts**

Use enums/immutable records only; do not reference `Game`, `Unity.Entities`, `UnityEngine`, or `Colossal.*` from these files.

- [ ] **Step 4: Run tests and verify pass**

Run the command from Step 2.

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CS2AssetPerformanceAuditor/Core tests/CS2AssetPerformanceAuditor.Tests
git commit -m "feat: add core observation contracts"
```

### Task 3: Implement Census Query Profile v1 and atomic snapshot semantics in Pure Core

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
- Consumes: `PrefabKey` from Task 2.
- Produces:
  - `const string CensusQueryProfile.V1 = "1"`
  - `readonly record struct CensusCounters(long TopLevelObjects, long SubordinateObjects, long LiveObjectReferences, long NetworkEdges)`
  - `sealed record CensusEntry(PrefabKey Key, CensusCounters Counters, CensusPresence Presence)`
  - `CensusReducer.AddObject(PrefabKey key, bool isSubordinate)`
  - `CensusReducer.AddNetworkEdge(PrefabKey key)`
  - `CensusSnapshot BuildSnapshot(...)`
  - `PublishedAuditState.TryPublish(CensusSnapshot workingSnapshot)` only after successful finalization.

- [ ] **Step 1: Write failing Query Profile v1 tests**

Tests must pin all Review Focus semantics owned here:
- top-level object => `TopLevelObjects=1`, `LiveObjectReferences=1`.
- subordinate object => `SubordinateObjects=1`, `LiveObjectReferences=1`.
- an input already classified subordinate is added once even if its adapter flags arose from both Owner and Controller.
- network edge affects `NetworkEdges` only.
- disabled subordinate collection serializes the affected observation as `NotScanned`, never numeric zero.
- failed/cancelled working snapshot does not replace the prior published snapshot.
- a current snapshot is tied to `queryProfileVersion` and `scanOptions`.

- [ ] **Step 2: Run and verify failure**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj --filter CensusReducerTests
```

Expected: FAIL.

- [ ] **Step 3: Implement the reducer and immutable snapshot contracts**

The reducer accepts already classified logical samples; exact ECS classification remains a Game Integration concern. Enforce the invariant `LiveObjectReferences = TopLevelObjects + SubordinateObjects` for supported object metrics.

- [ ] **Step 4: Run the focused tests**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CS2AssetPerformanceAuditor/Core/Census src/CS2AssetPerformanceAuditor/Core/Scanning tests/CS2AssetPerformanceAuditor.Tests/CensusReducerTests.cs
git commit -m "feat: define census query profile v1"
```

### Task 4: Implement the scan state machine, progress contract, and cancellation publication rules

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Core/Scanning/ScanKind.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Scanning/ScanStage.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Scanning/ScanState.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Scanning/ScanProgress.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Scanning/ScanSession.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/ScanSessionTests.cs`

**Interfaces:**
- Consumes: `PublishedAuditState`, `ScanOptions`.
- Produces:
  - `ScanSession Start(ScanKind kind, ScanOptions options)`
  - `void RequestCancellation()`
  - state transitions through `Preparing`, capture/reduction stages, `Finalizing`, `Completed`, `Cancelled`, `Failed`.
  - `ScanProgress` with either exact `(completed,total)` or indeterminate stage state, never fabricated percentage.

- [ ] **Step 1: Write failing state-machine tests**

Cover:
- valid Phase 1 transition order,
- invalid backwards transitions rejected,
- cancel during managed reduction becomes `Cancelled` before publication,
- cancel during scheduled capture records cancellation request but does not pretend the underlying job was force-killed,
- only `Completed` permits publish,
- progress without total is indeterminate.

- [ ] **Step 2: Run focused tests and verify failure**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj --filter ScanSessionTests
```

- [ ] **Step 3: Implement the minimal state machine**

Keep it game-independent; Game `JobHandle` ownership stays in the adapter/coordinator.

- [ ] **Step 4: Run tests and verify pass**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CS2AssetPerformanceAuditor/Core/Scanning tests/CS2AssetPerformanceAuditor.Tests/ScanSessionTests.cs
git commit -m "feat: add scan lifecycle state machine"
```

### Task 5: Add Phase 0 Game Integration capability probing and mod/system registration

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Capabilities/CapabilityProbe.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/AssetAuditSystem.cs`
- Create: `src/CS2AssetPerformanceAuditor/UI/AssetAuditUISystem.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Mod.cs`
- Create: `src/CS2AssetPerformanceAuditor/Setting.cs`

**Interfaces:**
- Consumes: Core capability/scan contracts.
- Produces:
  - `CapabilityReport CapabilityProbe.Probe(World world)`
  - `AssetAuditSystem` as the sole heavy-work coordinator.
  - `AssetAuditUISystem` as bindings/commands only.
  - mod registration that creates systems but leaves heavy work idle until requested.

- [ ] **Step 1: Add compile-time/contract assertions around required public API usage**

The game project must compile references to `PrefabSystem.GetPrefab/TryGetPrefab`, `PrefabData`, `PrefabRef`, `RenderPrefab`, `LodProperties`, `GeometryAsset`, `SurfaceAsset`, and `TextureAsset` without Reflection. Do not add Harmony.

- [ ] **Step 2: Build and verify the project fails before adapters exist**

Run:

```bash
dotnet build src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj -c Debug
```

Expected: FAIL only on intentionally missing adapter/system code.

- [ ] **Step 3: Implement capability probing and idle systems**

Probe each feature independently and map failures to `Degraded`/`Unsupported`/`Failed` without throwing startup-wide failure. `OnUpdate` while idle must not enumerate all entities or assets.

- [ ] **Step 4: Build and verify success**

Run the command from Step 2.

Expected: PASS in a correctly configured local CS2 modding environment.

- [ ] **Step 5: Commit**

```bash
git add src/CS2AssetPerformanceAuditor
git commit -m "feat: add compatibility foundation"
```

**Milestone 0 gate:** Core tests pass, UI build/test scaffolding passes, game project compiles with no Harmony reference, and idle systems perform no full scans.

---

## Milestone 1 — Prefab Catalog + Asset Instance Census

### Task 6: Implement Prefab catalog access, classification, and source evidence

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Prefabs/IPrefabCatalogAccess.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Prefabs/PrefabCatalogAccess.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Prefabs/PrefabClassifier.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Prefabs/SourceMetadataReader.cs`
- Modify: `src/CS2AssetPerformanceAuditor/GameIntegration/AssetAuditSystem.cs`

**Interfaces:**
- Consumes: Core `PrefabRecord`, `PrefabKey`, `AssetOriginEvidence`.
- Produces:
  - `IReadOnlyList<Entity> CapturePrefabEntities()` from `PrefabData` query.
  - `bool TryReadPrefab(Entity entity, out PrefabRecord record)` using public `PrefabSystem` resolution.
  - runtime `Dictionary<Entity, PrefabKey>` for the current world only.

- [ ] **Step 1: Write/extend pure tests for source projection and trait composition**

Add tests ensuring a Prefab may carry multiple traits and that overlapping source evidence is preserved rather than collapsed into false certainty.

- [ ] **Step 2: Run tests and verify they fail on missing projection/classification helpers**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj --filter "ObservationTests|Prefab"
```

- [ ] **Step 3: Implement catalog capture and frame-budgeted processing**

Enumerate `PrefabData` entities, resolve through public `PrefabSystem.TryGetPrefab`, build stable `PrefabKey` from PrefabID/type, preserve runtime Entity mapping separately, and process managed classification in time-bounded slices rather than one monolithic frame.

- [ ] **Step 4: Build/test**

Run Core tests plus game project build.

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: add prefab catalog collector"
```

### Task 7: Implement ECS Census capture and Query Profile v1 classification

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Census/CensusSample.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Census/CensusCaptureBuffers.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Census/CensusAccess.cs`
- Modify: `src/CS2AssetPerformanceAuditor/GameIntegration/AssetAuditSystem.cs`
- Modify: `tests/CS2AssetPerformanceAuditor.Tests/CensusReducerTests.cs`

**Interfaces:**
- Consumes: runtime `Entity -> PrefabKey`, `CensusReducer`, `ScanSession`.
- Produces:
  - `JobHandle BeginObjectCapture(...)`
  - `JobHandle BeginNetworkCapture(...)`
  - compact logical `CensusSample(Prefab Entity, CensusCountKind Kind)` data/buffers.
  - adapter classification preserving Query Profile v1: current object universe, mutually exclusive top-level/subordinate classification, live-reference union, current top-level network edge.

- [ ] **Step 1: Add a failing regression test for duplicate subordinate markers**

The adapter-facing reducer test must show that `Owner + Controller` classification produces one subordinate logical sample, not two.

- [ ] **Step 2: Run focused Core tests**

Expected: new test FAIL until the adapter-facing classification helper exists.

- [ ] **Step 3: Implement read-only EntityQueries/jobs**

Use `PrefabRef` plus verified object/network component sets. Exclude temporary/deleted/overridden/non-current states according to the target 1.6.2f1 components. Schedule capture and return to the game loop; do not `Complete()` immediately in the scheduling frame.

- [ ] **Step 4: Implement coordinator polling/cancellation cleanup**

When `JobHandle.IsCompleted` becomes true, complete safely, reduce in frame-budgeted slices, dispose native buffers in success/failure/cancel/world-unload paths, and publish only after `Finalizing` succeeds.

- [ ] **Step 5: Build/test**

Run Core tests and game project build.

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src tests
git commit -m "feat: capture asset instance census"
```

### Task 8: Add Phase 1 query service, diagnostics, and minimal JSON export

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Core/Query/AssetQuery.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Query/AssetSort.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Query/AssetPage.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Query/AssetQueryService.cs`
- Create: `src/CS2AssetPerformanceAuditor/Export/ReportSchema.cs`
- Create: `src/CS2AssetPerformanceAuditor/Export/AuditReport.cs`
- Create: `src/CS2AssetPerformanceAuditor/Export/AuditReportBuilder.cs`
- Create: `src/CS2AssetPerformanceAuditor/Export/AuditReportSerializer.cs`
- Create: `src/CS2AssetPerformanceAuditor/Export/PrivacySanitizer.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/AssetQueryServiceTests.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/ExportTests.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/PrivacySanitizerTests.cs`

**Interfaces:**
- Consumes: published Catalog/Census/capability state.
- Produces:
  - `AssetPage Query(AssetQuery query)` with bounded `offset`/`limit`.
  - canonical Phase 1 JSON metadata fields: `schemaVersion`, `ruleSetVersion`, `queryProfileVersion`, `scanOptions`, `modVersion`, `gameVersion`, timestamps, capabilities, catalog, census.
  - privacy sanitizer rejecting/omitting absolute local paths and machine-identifying fields.

- [ ] **Step 1: Write failing query/export/privacy tests**

Cover:
- search by display name, Prefab name/ID/source,
- type/source/presence filters,
- type-aware `Instances` and `CountKind`,
- bounded page size,
- `0` vs `NotScanned`,
- required version metadata,
- incompatible query profiles/scan options detectable for comparison,
- absolute Windows path and username/hostname fields are absent from canonical export.

- [ ] **Step 2: Run tests and verify failure**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj --filter "AssetQueryServiceTests|ExportTests|PrivacySanitizerTests"
```

- [ ] **Step 3: Implement minimal query/export pipeline**

Keep JSON full-fidelity for Phase 1 state. Do not add Phase 2–4 fields yet except schema-safe empty/optional sections.

- [ ] **Step 4: Run focused tests**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CS2AssetPerformanceAuditor/Core/Query src/CS2AssetPerformanceAuditor/Export tests
git commit -m "feat: add census query and export pipeline"
```

### Task 9: Build the Phase 1 in-game UI and bounded data bindings

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/UI/UiContracts.cs`
- Create: `src/CS2AssetPerformanceAuditor/UI/UiSnapshotBuilder.cs`
- Modify: `src/CS2AssetPerformanceAuditor/UI/AssetAuditUISystem.cs`
- Create: `UI/src/bindings.ts`
- Create: `UI/src/types.ts`
- Create: `UI/src/AssetAuditorRoot.tsx`
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
- Consumes: `AssetQueryService`, `ScanProgress`, capability/diagnostics state.
- Produces: UI triggers for `RunCensus`, `CancelScan`, bounded `QueryAssets`, minimal JSON export, and settings changes; bounded page/result bindings only.

- [ ] **Step 1: Write failing UI tests**

Cover Review Focus and spec behavior:
- no data => `Not scanned`, never `0`.
- exact progress shows count/percentage; unknown capture progress shows indeterminate stage.
- `Instances` displays/ exposes `CountKind`.
- scrolling/query state requests bounded windows rather than rendering a supplied full catalog.
- Escape and close button close panel; neither cancels scan.
- reopening while scan active renders current progress.

- [ ] **Step 2: Run UI tests and verify failure**

```bash
cd UI && npm test
```

- [ ] **Step 3: Implement the minimal Phase 1 UI/bindings**

Tabs in this milestone: Overview, Assets, Census, Settings. Keep Warnings/Compare hidden until their data exists. Debounce search before issuing C# query requests.

- [ ] **Step 4: Run UI tests/build and C# tests/build**

```bash
cd UI && npm test && npm run build
cd .. && dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj
dotnet build src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj -c Debug
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CS2AssetPerformanceAuditor/UI UI
git commit -m "feat: add census audit UI"
```

**Milestone 1 gate:** Catalog and Census work end-to-end, scan is user-triggered/cancellable, counts have explicit semantics, UI is bounded, minimal JSON export works, and failed/cancelled scans preserve the previous published snapshot.

---

## Milestone 2 — Render Graph + Geometry / LOD Auditor

### Task 10: Add render-domain contracts and verified resolver architecture

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/RenderAssetKey.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/RenderCoverage.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/RenderRelationKind.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/PrefabRenderRelation.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/RenderAssetRecord.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/IRenderAssetResolver.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/ObjectGeometryResolver.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/RenderGraphBuilder.cs`

**Interfaces:**
- Consumes: `PrefabRecord`/runtime Prefab mapping.
- Produces:
  - `IRenderAssetResolver.CanResolve(PrefabBase prefab)` and `Resolve(...)` isolated to Game Integration.
  - domain `PrefabRenderRelation(PrefabKey, RenderAssetKey, RenderRelationKind)`.
  - per-Prefab `RenderCoverage` = `Supported`, `NotApplicable`, `Unknown`, or `Failed`.

- [ ] **Step 1: Write failing Pure Core tests for render coverage/dedup semantics**

Assert unresolved coverage is not serialized/query-projected as zero geometry and duplicate `RenderAssetKey` references share one resource record.

- [ ] **Step 2: Run tests and verify failure**

- [ ] **Step 3: Implement ObjectGeometry resolver first**

Resolve verified `ObjectGeometryPrefab.m_Meshes` and `LodProperties.m_LodMeshes`; do not recursively reflect through arbitrary fields. Network and other unsupported families return explicit coverage until a verified resolver is added.

- [ ] **Step 4: Build/test**

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
- Consumes: deduplicated `RenderAssetRecord` and `GeometryAsset` references in the adapter.
- Produces observed mesh/vertex/index/submesh/bounds/compressed-payload metadata plus a domain `TriangleCount` that is numeric only for triangle topology.

- [ ] **Step 1: Write failing geometry tests**

Pin:
- Triangles + 300 indices => 100 triangles.
- Lines + 300 indices => `NotApplicable` triangle count.
- missing/failed geometry metadata => `Failed`/`Unsupported`, not zero.
- shared GeometryAsset key is read once per analysis generation.

- [ ] **Step 2: Run tests and verify failure**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj --filter GeometryMathTests
```

- [ ] **Step 3: Implement metadata-first GeometryAsset reader**

Use public AssetDatabase geometry APIs. Do not call `RenderPrefab.ObtainMeshes()` in normal Asset Audit. Copy metadata into domain records and release any temporary data according to API ownership.

- [ ] **Step 4: Run Core tests/game build**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: audit geometry metadata"
```

### Task 12: Add LOD metrics, peer statistics, and Phase 2 findings

**Files:**
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
- Consumes: Catalog traits/source, geometry observations, Render Graph, Census where available.
- Produces:
  - deterministic LOD retention/reduction metrics.
  - `Finding` with `RuleId`, `Status`, `Category`, explanation, evidence, basis, threshold origin/version.
  - Phase 2 rule families: `GEOMETRY_PEER_OUTLIER`, `SUBMESH_COUNT_OUTLIER`, `GEOMETRY_PAYLOAD_OUTLIER`, `NO_LOWER_LOD_OBSERVED`, `LOD_REDUCTION_LOW`, `LOD_MATERIAL_REDUCTION_LOW`, `LOD_GEOMETRY_INCREASES`.

- [ ] **Step 1: Write failing statistic/finding tests**

Cover:
- exact LOD retention/reduction math,
- missing lower LOD => Observed/Notice only, not automatic Warning,
- heuristic rule => `PotentialIssue` and `FindingBasis.Heuristic`,
- deterministic broken reference may be Warning,
- insufficient peer sample => comparison unavailable, no fabricated percentile finding,
- missing evidence => no fabricated finding,
- same-category population selection is stable.

- [ ] **Step 2: Run tests and verify failure**

- [ ] **Step 3: Implement statistics and rule engine**

No Game APIs in the rule engine. Keep threshold definitions versioned under `RuleSetInfo`; do not label heuristic values as official guidance.

- [ ] **Step 4: Run tests and verify pass**

- [ ] **Step 5: Commit**

```bash
git add src/CS2AssetPerformanceAuditor/Core/Findings tests
git commit -m "feat: add geometry and lod findings"
```

**Milestone 2 gate:** verified Prefab families resolve to a deduplicated Render Graph, GeometryAsset metadata and topology-aware triangles are available without mass Unity Mesh materialization, LOD evidence is computed, unsupported render families remain explicit, and findings are evidence-backed.

---

## Milestone 3 — Surface / Texture Auditor

### Task 13: Collect SurfaceAsset and TextureAsset metadata with safe resource ownership

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/SurfaceObservation.cs`
- Create: `src/CS2AssetPerformanceAuditor/Core/Rendering/TextureObservation.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/SurfaceAssetReader.cs`
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/TextureAssetReader.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/TextureFootprintTests.cs`
- Modify: `src/CS2AssetPerformanceAuditor/GameIntegration/AssetAuditSystem.cs`

**Interfaces:**
- Consumes: deduplicated RenderAsset/SurfaceAsset references.
- Produces observed surface/material-template/VT/property/texture-relation metadata and texture width/height/depth/format/dimension/mips/filter/wrap/anisotropy observations.

- [ ] **Step 1: Write failing texture-footprint tests**

Cover representative compressed and uncompressed formats, block rounding, mip chains, and unsupported format behavior. Assert estimated payload is explicitly `Estimated` and never named/serialized as actual VRAM.

- [ ] **Step 2: Run tests and verify failure**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj --filter TextureFootprintTests
```

- [ ] **Step 3: Implement metadata-first Surface/Texture readers**

Prefer `SurfaceAsset`/`TextureAsset` properties and property-loading APIs over Unity Material/Texture construction. Deduplicate shared texture assets. Any property data loaded by the adapter must be unloaded in deterministic cleanup paths when the API gives the mod ownership of that loaded state.

- [ ] **Step 4: Run tests/game build**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: audit surface and texture metadata"
```

### Task 14: Add material/texture/exposure finding families and unique-resource aggregation

**Files:**
- Modify: `src/CS2AssetPerformanceAuditor/Core/Findings/FindingEngine.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Core/Findings/RuleSetInfo.cs`
- Modify: `tests/CS2AssetPerformanceAuditor.Tests/FindingEngineTests.cs`
- Modify: `tests/CS2AssetPerformanceAuditor.Tests/TextureFootprintTests.cs`

**Interfaces:**
- Consumes: Surface/Texture observations plus Census exposure.
- Produces rule families `MATERIAL_COUNT_OUTLIER`, `TEXTURE_DIMENSION_OUTLIER`, `TEXTURE_PAYLOAD_OUTLIER`, `HIGH_UNIQUE_TEXTURE_FOOTPRINT`, `HIGH_CITY_EXPOSURE`, `HIGH_SUBORDINATE_EXPOSURE`, and integrity findings for failed/unresolved metadata.

- [ ] **Step 1: Add failing finding tests**

Assert:
- shared texture referenced three times contributes once to unique payload and three times only to reference count.
- high exposure alone is Notice/PotentialIssue evidence, never proof of render cost.
- item-level failed texture read produces integrity evidence without aborting unrelated asset findings.
- peer-outlier sample rules remain enforced.

- [ ] **Step 2: Run tests and verify failure**

- [ ] **Step 3: Implement the Phase 3 rules/aggregations**

Keep separate geometry, LOD, material, texture, and city exposure dimensions; do not introduce a combined score.

- [ ] **Step 4: Run tests and verify pass**

- [ ] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: add surface texture and exposure findings"
```

**Milestone 3 gate:** surface/texture metadata and estimated logical payloads work without claiming actual VRAM, shared resources are deduplicated, and Phase 3 findings remain evidence/basis/version aware.

---

## Milestone 4 — Analysis UX, compare, full export, and Deep Inspection

### Task 15: Expand query/export schemas for complete audit data and comparison compatibility

**Files:**
- Modify: `src/CS2AssetPerformanceAuditor/Core/Query/AssetQueryService.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Export/AuditReport.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Export/AuditReportBuilder.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Export/AuditReportSerializer.cs`
- Create: `src/CS2AssetPerformanceAuditor/Export/CsvSummaryExporter.cs`
- Create: `tests/CS2AssetPerformanceAuditor.Tests/ReportCompatibilityTests.cs`
- Modify: `tests/CS2AssetPerformanceAuditor.Tests/ExportTests.cs`

**Interfaces:**
- Consumes: full published Observation Store state.
- Produces:
  - canonical hierarchical JSON with catalog/census/render/geometry/surface/texture/findings and all evidence classifications.
  - CSV flat summary.
  - export scopes: full, current filtered assets, selected assets, census only, findings only.
  - compatibility result for `schemaVersion`, `queryProfileVersion`, and materially different `scanOptions`.

- [ ] **Step 1: Write failing export/compatibility tests**

Cover:
- full hierarchy preserves Observed/Derived/Estimated/Unavailable classifications,
- JSON contains no runtime Entity index/local absolute path,
- CSV is flat and does not pretend to preserve Render Graph hierarchy,
- incompatible query profile or materially different scan options raises comparison warning,
- additive optional fields do not silently change existing field meaning.

- [ ] **Step 2: Run tests and verify failure**

- [ ] **Step 3: Implement full report/CSV builders**

Keep schema version explicit. If implementation changes a previously specified field meaning, increment schema version rather than silently reusing the old contract.

- [ ] **Step 4: Run tests**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CS2AssetPerformanceAuditor/Export src/CS2AssetPerformanceAuditor/Core/Query tests
git commit -m "feat: add full audit report exports"
```

### Task 16: Implement selected-asset Deep Inspection with deterministic resource cleanup

**Files:**
- Create: `src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/DeepInspectionReader.cs`
- Modify: `src/CS2AssetPerformanceAuditor/GameIntegration/AssetAuditSystem.cs`
- Modify: `src/CS2AssetPerformanceAuditor/Core/Rendering/RenderAssetRecord.cs`
- Create: `docs/validation/runtime-validation.md`

**Interfaces:**
- Consumes: selected `RenderAssetKey` only.
- Produces: copied domain observations for shader/material/advanced binding data; never stores Unity Material/Mesh/Texture objects in the Observation Store.

- [ ] **Step 1: Add cleanup-path design assertions/documented runtime scenarios**

The runtime validation matrix must explicitly cover success, cancellation, exception, repeated inspection, and game-owned shared-object cases.

- [ ] **Step 2: Implement Deep Inspection ownership boundaries**

Use `try/finally`-equivalent cleanup. Destroy/unload/dispose only resources whose ownership was acquired by the mod; do not destroy game-owned shared objects. Copy all retained values into domain records before cleanup.

- [ ] **Step 3: Build and run automated tests**

Expected: PASS. Runtime ownership scenarios remain `NOT RUN` until actually tested in game.

- [ ] **Step 4: Commit**

```bash
git add src docs/validation/runtime-validation.md
git commit -m "feat: add selected asset deep inspection"
```

### Task 17: Build Warnings, Asset Details, Render Structure, and Compare UI

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
- Modify: `UI/src/AssetAuditorRoot.tsx`
- Modify: `UI/src/tabs/AssetsTab.tsx`

**Interfaces:**
- Consumes: bounded asset detail/compare/finding DTOs from `AssetAuditUISystem`.
- Produces: expandable evidence, 2–4 asset comparison, render-structure drill-down, no automatic winner/global score.

- [ ] **Step 1: Write failing UI tests**

Cover:
- Finding shows status, evidence, basis, and rule ID/version.
- `LOD1 retains 83.98%` evidence renders source metrics when supplied.
- unsupported render coverage renders Unknown/Unsupported, not zero geometry.
- Compare accepts 2–4 assets and does not render a “best asset” verdict.
- actual GPU residency unavailable label remains distinct from estimated texture payload.

- [ ] **Step 2: Run UI tests and verify failure**

```bash
cd UI && npm test
```

- [ ] **Step 3: Implement the Phase 4 detail/warning/compare UI**

Keep large list data bounded; detail endpoints may return one selected asset's full hierarchy. Do not ship full catalog state into every React update.

- [ ] **Step 4: Run UI test/build**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add UI
git commit -m "feat: add asset analysis details and compare UI"
```

### Task 18: Finish Settings, diagnostics, performance telemetry, and UI scan controls

**Files:**
- Modify: `src/CS2AssetPerformanceAuditor/Setting.cs`
- Modify: `src/CS2AssetPerformanceAuditor/GameIntegration/AssetAuditSystem.cs`
- Modify: `src/CS2AssetPerformanceAuditor/UI/UiContracts.cs`
- Modify: `src/CS2AssetPerformanceAuditor/UI/AssetAuditUISystem.cs`
- Modify: `UI/src/tabs/SettingsTab.tsx`
- Modify: `UI/src/components/ScanStatus.tsx`
- Create: `UI/src/__tests__/settings.test.tsx`

**Interfaces:**
- Consumes: scan coordinator, diagnostics/capability state.
- Produces bounded settings for frame-processing budget, progress update rate, catalog refresh, subordinate collection, heuristic/peer findings, UI scale, cache/deep-inspection limits; diagnostics include game/mod version, compatibility, capability states, `Harmony: not used`, last-scan timings, aggregated failures.

- [ ] **Step 1: Write failing settings/diagnostics tests**

Cover:
- optional metric disabled => `scanOptions` changes and metric becomes `NotScanned`, not zero.
- unsafe numeric settings clamp/reject to safe bounds.
- settings indicate Immediate/Next Scan/Restart semantics.
- repeated identical diagnostic codes aggregate instead of flooding UI/logs.
- `Harmony: not used` is exposed in Diagnostics.

- [ ] **Step 2: Run tests and verify failure**

- [ ] **Step 3: Implement settings/diagnostics/self-telemetry**

Record low-cost scan elapsed time, processed count, max/P95 managed slice where practical without enabling continuous heavy profiling.

- [ ] **Step 4: Run all automated tests/builds**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj
cd UI && npm test && npm run build
cd .. && dotnet build src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj -c Debug
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src UI
git commit -m "feat: finalize auditor settings and diagnostics"
```

### Task 19: Add runtime validation matrix, update procedure, and CI-safe checks

**Files:**
- Modify: `docs/validation/runtime-validation.md`
- Modify: `README.md`
- Create: `.github/workflows/core-ui-tests.yml`

**Interfaces:**
- Consumes: all implemented Phases 0–4.
- Produces: explicit `PASS` / `FAIL` / `NOT RUN` / `BLOCKED` runtime matrix and update checklist; CI that runs what can run without redistributing game DLLs.

- [ ] **Step 1: Write the validation matrix before claiming runtime success**

Include at minimum:
- Vanilla small city,
- large city,
- custom-asset-heavy city,
- broken/incomplete asset,
- world change during scan,
- UI close/Escape during scan,
- Deep Inspection ownership/repetition.

Record entity/Prefab/render-asset count, total scan time, max/P95 managed slice, and memory/allocation delta where measurable.

- [ ] **Step 2: Add CI for Pure Core and UI tests**

CI must not require committed game DLLs. If game-project build cannot run legally/technically on hosted CI, document it as a local/runtime validation step rather than marking it as CI PASS.

- [ ] **Step 3: Add the game-update checklist to README/validation docs**

Order: new DLL/toolchain compile -> Pure Core tests -> Adapter Contract/build checks -> capability probe -> Vanilla runtime validation -> Custom Asset validation -> large-city performance validation -> mark version Supported.

- [ ] **Step 4: Run the locally available verification commands**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj
cd UI && npm test && npm run build
```

Run the game-project build as well when the CS2 toolchain is available.

Expected: all executable automated checks PASS; unexecuted game scenarios remain `NOT RUN`, never inferred PASS.

- [ ] **Step 5: Commit**

```bash
git add .github README.md docs/validation/runtime-validation.md
git commit -m "test: add auditor validation and ci workflow"
```

**Milestone 4 gate:** Asset Details, Warnings, Compare, full JSON/CSV export, selected Deep Inspection, settings, diagnostics, validation docs, and CI-safe tests are complete. All automated checks pass; runtime scenarios are truthfully marked with their executed status.

---

## Final verification before release/merge

### Task 20: Whole-project verification and spec conformance review

**Files:**
- Modify only files required by discovered defects.
- Review: `docs/superpowers/specs/2026-09-27-asset-performance-auditor-design.md`
- Review: `docs/superpowers/plans/2026-09-27-asset-performance-auditor-implementation.md`
- Review: `docs/validation/runtime-validation.md`

**Interfaces:**
- Consumes: all prior tasks.
- Produces: a branch where code, tests, documentation, and runtime claims agree with the approved spec.

- [ ] **Step 1: Run all automated verification**

```bash
dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj
cd UI && npm test && npm run build
cd .. && dotnet build src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj -c Release
```

Expected: PASS in an environment with the CS2 toolchain available for the final command.

- [ ] **Step 2: Search the source for forbidden/unsafe design regressions**

Verify there is no `Lib.Harmony` dependency, no Harmony patch attribute/use, no Reflection-based hidden-member fallback, no unconditional `indexCount / 3` outside topology-aware logic, no global performance score, and no automatic periodic full scan.

- [ ] **Step 3: Review the five Review Focus conditions against tests/runtime validation**

Each condition at the top of this plan must have either an automated regression test or an explicit runtime validation scenario. Fix any uncovered gap before completion.

- [ ] **Step 4: Reconcile runtime claims**

Any runtime scenario not actually executed remains `NOT RUN`/`BLOCKED`. Do not convert build/unit-test evidence into in-game PASS.

- [ ] **Step 5: Commit any verification fixes**

```bash
git add -A
git commit -m "test: verify asset performance auditor"
```

Skip this commit if verification required no source/document changes.

---

## Phase delivery summary

- **Phase 0:** Tasks 1–5 — project foundation, testable Core, compatibility/capability layer.
- **Phase 1:** Tasks 6–9 — Prefab Catalog, Snapshot Census, bounded UI/query/export.
- **Phase 2:** Tasks 10–12 — Render Graph, Geometry/LOD analysis, evidence-backed geometry/LOD findings.
- **Phase 3:** Tasks 13–14 — Surface/Texture analysis, safe footprint estimates, material/texture/exposure findings.
- **Phase 4:** Tasks 15–19 — full export, Deep Inspection, Asset Details, Warnings, Compare, Settings, diagnostics, validation/CI.
- **Final verification:** Task 20.

Implementation should stop at each milestone gate for tests/review before advancing. The implementation may optimize internal ECS/job details if profiling justifies it, but it must preserve the domain contracts and observable semantics specified here and in the approved design document.
