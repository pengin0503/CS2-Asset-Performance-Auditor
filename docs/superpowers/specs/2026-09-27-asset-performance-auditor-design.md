# CS2 Asset Performance Auditor — Design Specification

**Date:** 2026-09-27  
**Status:** Approved conversational design; written-spec review pending  
**Target game baseline:** Cities: Skylines II 1.6.2f1  
**Repository:** `pengin0503/CS2-Asset-Performance-Auditor`

## 1. Purpose

CS2 Asset Performance Auditor is a read-mostly diagnostic Code Mod for Cities: Skylines II. It investigates assets registered by the game and assets referenced by the currently loaded city, then presents evidence about their static structure and city exposure.

The mod is intended to answer questions such as:

- What gameplay Prefabs and render assets exist?
- Which gameplay Prefabs are referenced by entities in the current city?
- How many top-level, subordinate, and network instances reference each Prefab?
- What Mesh, Submesh, LOD, Surface, Material, and Texture metadata are available?
- Are LODs reducing geometry meaningfully?
- Are an asset's geometry, material, texture, or exposure values unusual compared with comparable assets?
- Is an observed concern based on direct evidence, a mathematical derivation, a heuristic, or an estimate?

The mod does **not** automatically optimize assets, modify the city to improve performance, disable assets, or claim a precise FPS/GPU cost from static metadata.

## 2. Design principles

1. **Evidence before inference.** Direct observations, derived values, estimates, and unavailable information are explicitly distinguished.
2. **Read-mostly operation.** Scanning does not modify gameplay entities or Prefabs.
3. **Low diagnostic overhead.** No always-on full-world scans. Heavy work is user-initiated, frame-budgeted, cancellable, cached, and deduplicated.
4. **Static analysis is not runtime rendering cost.** Geometry, LOD, material, texture, and instance exposure remain separate from actual visibility, culling, batching, LOD selection, streaming, and GPU time.
5. **No global performance score.** The tool presents independent evidence dimensions rather than a single `82/100` style rating.
6. **Graceful degradation.** Failure of one collector should not disable unrelated collectors.
7. **Update resilience.** CS2-specific access is isolated behind compatibility/adaptation boundaries.
8. **No Harmony in Phases 1–4.** The initial product does not depend on `Lib.Harmony`, Prefix/Postfix patches, or Transpilers.

## 3. Scope

### 3.1 In scope

- Prefab cataloging
- Prefab classification and source evidence
- Snapshot Asset Instance Census
- Top-level, subordinate, live-object, and network-edge counts
- Render graph discovery
- Geometry metadata
- Topology-aware triangle calculation
- LOD structure and reduction metrics
- Surface/material metadata
- Texture references, dimensions, format, mip metadata, and safe storage estimates
- Findings with evidence
- Peer-comparison analysis
- Search, filtering, sorting, virtualized asset lists
- Asset details
- Asset comparison
- JSON export and flat CSV summary export
- Diagnostics and capability reporting
- Pure-core automated tests plus explicit in-game validation

### 3.2 Explicitly out of MVP / early phases

- Automatic optimization or asset mutation
- Always-on census tracking
- District-specific instance counts
- Map highlighting
- Persistent full `Prefab -> List<Entity>` storage
- Missing-Prefab forensic diagnosis
- Actual VRAM residency claims
- Actual per-asset frame-time claims
- Runtime renderer interception
- Harmony
- IL Transpilers
- Global performance score
- Unbounded deep inspection of every asset

Runtime rendering evidence is a separate future Phase 5 design problem.

## 4. Research basis and verified technical assumptions

The design was reviewed against the supplied current game assemblies, including `Game.dll`, `Unity.Entities.dll`, `Colossal.IO.AssetDatabase.dll`, and Unity engine assemblies corresponding to the current 1.6.2f1 game baseline.

Important verified capabilities include:

- `PrefabData.m_Index` and public `PrefabSystem.GetPrefab` / `TryGetPrefab` access.
- `ObjectGeometryPrefab.m_Meshes` relationships to `RenderPrefab`.
- `LodProperties.m_LodMeshes`.
- `RenderPrefab` geometry/surface/bounds/count metadata.
- `GeometryAsset` methods for vertex/index/submesh metadata, `SubMeshDescriptor`, topology, and compressed data size.
- `SurfaceAsset` texture/property metadata.
- `TextureAsset` dimensions, format, mip, wrapping/filtering, and related properties.

Public CS2 Mod implementations were also used as behavioral reference points:

- Find It demonstrates Prefab indexing, ECS `PrefabRef` census, source classification, and camera-location workflows.
- Extra Assets Importer demonstrates current use of `RenderPrefab`, `GeometryAsset`, and `SurfaceAsset`/Texture relationships.
- Existing CS2 ECS mods demonstrate that many useful tasks can be performed without Harmony.

These references inform feasibility; this project does not copy their architecture wholesale.

## 5. Dependency and intervention policy

Access priority is:

1. ECS and public Game managed members
2. Colossal AssetDatabase public members
3. Unity public APIs
4. Reflection only through a separately justified design change
5. Harmony only through a separately justified Phase 5 design change

### 5.1 Harmony policy

Phases 1–4 shall have:

- no Harmony package dependency,
- no Prefix,
- no Postfix,
- no Transpiler.

If a future runtime-observation requirement cannot be obtained safely through ECS, Game APIs, AssetDatabase, or Unity APIs, Harmony may be reconsidered only through a separate design/spec amendment. Transpilers require especially strong justification.

## 6. High-level architecture

```text
Cities: Skylines II
 Game / ECS / AssetDatabase / Unity
              |
              v
+--------------------------------+
| Game Integration Layer         |
| Prefab / Census / Render /     |
| Geometry / Surface / Texture   |
+---------------+----------------+
                |
                v
+--------------------------------+
| Scan Coordinator               |
| lifecycle / progress / cancel  |
| frame budget / failure state   |
+---------------+----------------+
                |
                v
+--------------------------------+
| Observation Store              |
| Catalog / Render Graph /       |
| Census / Metadata / Findings   |
+----------+-----------+---------+
           |           |
           v           v
     Warning Engine  Query Service
           |           |
           |           +------> UI
           +------------------> Export
```

The architecture is intentionally layered. CS2-specific objects do not flow deeply into analysis, warning, UI-query, or export logic.

## 7. Primary systems

The initial game-side system count should remain small.

### 7.1 `AssetAuditSystem : GameSystemBase`

Responsibilities:

- receive scan requests,
- coordinate Prefab Catalog and Census scans,
- coordinate future geometry/surface/texture collectors,
- own scan lifecycle and cancellation state,
- enforce frame budgets,
- publish atomic snapshots,
- maintain capability state and diagnostics.

When idle, it must not perform full-world scans.

### 7.2 `AssetAuditUISystem : ExtendedUISystemBase`

Responsibilities:

- UI bindings,
- UI commands/triggers,
- search/filter/sort/page requests,
- scan start/cancel commands,
- export commands,
- scan progress/status exposure.

It must not independently scan entities.

## 8. Compatibility and capability layer

CS2-specific access is isolated in modules conceptually equivalent to:

```text
GameIntegration/
  Prefabs/
  Census/
  Rendering/
  AssetDatabase/
```

Logical access interfaces include:

- `PrefabAccess`
- `CensusAccess`
- `RenderAssetAccess`
- `GeometryAccess`
- `SurfaceAccess`
- `TextureAccess`
- `SourceMetadataAccess`

### 8.1 Capability Registry

Startup/runtime capability state is recorded independently of game-version labels.

Example capabilities:

```text
PrefabCatalog        Supported
ObjectCensus         Supported
NetworkEdgeCensus    Supported
GeometryMetadata     Supported
SubmeshMetadata      Supported
SurfaceMetadata      Supported
TextureMetadata      Supported
ShaderDeepInspection Supported
RuntimeGpuResidency  Unsupported
```

States:

- `Supported`
- `Degraded`
- `Unsupported`
- `Failed`

The game version itself is tracked as `Supported`, `Degraded`, `Unsupported`, or `Untested` based on runtime validation and capability results.

A new game version that passes capability probes but has not been validated in game is `Untested`, not automatically `Supported` or `Unsupported`.

## 9. Domain model

Core data models must avoid retaining Unity/CS2 runtime types when a stable domain representation is sufficient.

Primary records:

- `PrefabRecord`
- `RenderAssetRecord`
- `PrefabRenderRelation`
- `CatalogSnapshot`
- `CensusSnapshot`
- `GeometryObservation`
- `SurfaceObservation`
- `TextureObservation`
- `Finding`
- `CapabilityReport`

### 9.1 Prefab identity

Runtime joining may use Entity/PrefabData information, but persistent/report identity uses a stable Prefab identity such as:

```text
PrefabKey
  PrefabId
  PrefabType
```

Entity indexes are not treated as cross-session stable IDs.

### 9.2 Observation model

Observed metrics are wrapped conceptually as:

```text
Observation<T>
  Value
  Availability
  Origin
  CapturedAt
```

Availability:

- `Available`
- `NotScanned`
- `NotApplicable`
- `Unsupported`
- `Failed`

Origin:

- `ECS`
- `GameAssembly`
- `AssetDatabase`
- `Unity`
- `Derived`
- `Estimated`

This distinction must survive through UI and JSON export.

## 10. Prefab Catalog

Prefab enumeration uses ECS `PrefabData` and public `PrefabSystem.GetPrefab` / `TryGetPrefab` resolution. It does not depend on internal PrefabSystem collection getters or Reflection.

Conceptual flow:

```text
EntityQuery<PrefabData>
  -> Prefab entities
  -> PrefabSystem.TryGetPrefab
  -> classification
  -> source evidence
  -> PrefabRecord
```

The catalog keeps a runtime Entity-to-PrefabKey index for current-world joins while storing stable report keys separately.

### 10.1 Classification

Classification should support traits rather than force every Prefab into one exclusive enum. Examples include:

- Building
- ServiceBuilding
- Prop
- Tree
- Vehicle
- Network
- RenderOnly

A Prefab may carry multiple relevant traits.

### 10.2 Source evidence

Source must retain underlying evidence instead of forcing uncertain data into one label.

Possible evidence:

- `isBuiltin`
- `isSubscribedMod`
- `isPackaged`
- DLC prerequisites
- Asset Pack membership
- AssetDatabase source
- Paradox Mods platform ID where available

The UI may project this to labels such as Vanilla, DLC, Region/Asset Pack, Paradox Mods Asset, Local Asset, or Unknown.

## 11. Render graph

Gameplay Prefabs and rendering assets are modeled separately.

```text
Gameplay Prefab
      |
      v
PrefabRenderRelation
      |
      v
RenderPrefab
  |-- GeometryAsset
  |-- SurfaceAsset[]
  `-- LOD relations -> RenderPrefab[]
```

This avoids conflating city instance counts with render-resource identity and supports shared RenderPrefabs/resources.

`PrefabRenderRelation` records relation origin where known, for example:

- `DirectMesh`
- `LOD`
- `Child`
- `Unknown`

Shared `RenderPrefab`, `GeometryAsset`, `SurfaceAsset`, and `TextureAsset` resources are deduplicated in the observation cache.

### 11.1 Render coverage

The auditor must not imply that every Prefab type has a successfully resolved render graph. Per Prefab, render discovery records a coverage state such as:

- `Supported`
- `NotApplicable`
- `Unknown`
- `Failed`

Phase 2 begins with resolvers backed by verified 1.6.2f1 structures such as `ObjectGeometryPrefab`. Additional Prefab families, including network-specific render paths, require explicit resolvers once their public-access path is verified. Failure to resolve a render path is represented as evidence, not silently treated as zero geometry.

## 12. Scan model

Scans are explicit user-triggered snapshot operations. There is no default periodic full census or full asset audit.

Supported conceptual scan modes:

- `Census Scan`
- `Asset Audit`
- `Deep Inspection` for selected assets only

### 12.1 Scan state machine

Phase 1 states include:

```text
Idle
 -> Preparing
 -> CapturingCatalog (if needed)
 -> ProcessingCatalog
 -> CapturingObjectCensus
 -> ReducingObjectCensus
 -> CapturingNetworkCensus
 -> ReducingNetworkCensus
 -> Finalizing
 -> Completed
```

Transitions to `Cancelled` or `Failed` are supported from active stages where safe.

### 12.2 Progress

Known progress may use exact counts. Unknown job progress uses indeterminate state and stage numbers rather than fabricated percentages.

Examples:

- `Processing objects 183,241 / 287,520 — 63.7%`
- `Capturing city entities… Stage 3 of 6`

UI progress events are throttled; completion/failure/cancellation notifications are immediate.

### 12.3 Cancellation

Scheduled ECS jobs are not force-killed. If cancellation is requested during capture, the job is allowed to complete safely and its result is discarded. Frame-sliced managed reduction can stop on the next slice.

## 13. Asset Instance Census

The census records multiple count semantics instead of one ambiguous `Instances` value.

Conceptual counters:

```text
CensusCounters
  LiveObjectReferences
  TopLevelObjects
  SubordinateObjects
  NetworkEdges
```

Examples:

- Top-level placed buildings contribute to `TopLevelObjects` and `LiveObjectReferences`.
- Owned/controlled child objects contribute to `SubordinateObjects` and `LiveObjectReferences`.
- Network Prefabs use `NetworkEdges` as the primary MVP network exposure metric.

### 13.1 Why multiple counters exist

A diagnostic performance tool must distinguish:

- a small number of top-level assets,
- a large number of child/subordinate entities generated by those assets,
- dynamic vehicle/object presence,
- network edge exposure.

None of these counters is called actual render-instance count or actual render workload.

### 13.2 Primary display semantics

The generic `Instances` column is a **type-aware primary exposure value**, never a single universal count definition. It carries or exposes a `CountKind` so the UI can explain what was counted.

Initial mapping:

- Building: Top-Level Objects
- Service Building: Top-Level Objects
- Prop: Live Object References, with Top-Level/Subordinate breakdown
- Tree: Top-Level Objects
- Vehicle: active Live Object References at snapshot
- Network: Network Edges
- Render-only asset: Census not applicable

The UI must expose the count kind via column metadata, tooltip, detail view, or equivalent so a Building count cannot be mistaken for the same semantic as a Vehicle count.

### 13.3 Presence state

Internal state uses:

- `Present`
- `NotPresentAtSnapshot`
- `NotApplicable`
- `Unknown`

`0` is never used as a substitute for `NotScanned` or `NotApplicable`.

### 13.4 Query-profile versioning

Census semantics are versioned through `queryProfileVersion`. Any change to inclusion/exclusion or classification semantics that changes the meaning of a counter requires a version update.

User scan options do **not** silently redefine the profile. Options that omit supported data, such as choosing not to collect subordinate counts, are serialized separately as `scanOptions`. Omitted metrics remain `NotScanned`, not zero. Report comparison must warn when query-profile versions are incompatible or when compared scans used materially different scan options.

### 13.5 Census Query Profile v1 logical semantics

The first query profile has the following normative meaning even if exact ECS query construction is optimized during implementation:

- **Current object universe:** current-world object entities carrying `PrefabRef`, excluding temporary/deleted/overridden states that do not represent current city exposure.
- **Top-Level Object:** an object in that universe that is not owned or controlled as a subordinate entity according to the verified 1.6.2f1 `Owner`/`Controller` semantics.
- **Subordinate Object:** an object in that universe that is owned or controlled by another entity. An entity that satisfies both subordinate markers is counted once, not twice.
- **Live Object References:** the union of Top-Level and Subordinate object references included by the profile; therefore a supported object entity contributes at most once to this counter.
- **Network Edge:** a current, non-temporary, non-overridden top-level network edge carrying `PrefabRef`; child/helper network entities are not folded into the primary `NetworkEdges` metric.

Exact component/query syntax is an adapter concern, but it must preserve these semantics and be covered by contract/runtime validation. If the public API cannot preserve a semantic on a future version, the affected capability degrades instead of quietly changing the meaning of the counter.

## 14. Census collection implementation strategy

The preferred design separates ECS capture from managed reduction.

```text
ECS World
  -> read-only capture job / chunk access
  -> compact native snapshot
  -> frame-budgeted reducer
  -> WorkingSnapshot
  -> atomic publish
```

The implementation should minimize per-entity managed allocations and avoid retaining all Entity lists after a scan.

### 14.1 EntityQuery guidance

Find It demonstrates the feasibility of `PrefabRef` census with `Object`/`Edge` and exclusions such as `Owner`, `Controller`, and `Overridden`. This project uses that as evidence of feasibility but uses distinct logical query profiles so top-level, subordinate, object, and network semantics remain explicit.

The adapter must implement the Query Profile v1 semantics above and validate its exclusion components against the target DLL/runtime.

### 14.2 Async job behavior

Scheduling an async query/job and immediately calling `Complete()` in the same frame defeats the purpose. The coordinator schedules, returns, checks completion in later updates, and completes only once ready.

## 15. Atomic snapshot publication

Working results are isolated from published results.

```text
PublishedSnapshot
       |
Start new scan
       v
WorkingSnapshot
       |
       +-- cancel/fail -> discard
       |
       `-- complete -> atomic replace PublishedSnapshot
```

Cancelled or failed scans never partially overwrite a previously successful snapshot.

World changes invalidate current-world census/runtime lookup state and cancel/dispose active scans safely.

## 16. Catalog and analysis generations

Catalog, Census, and Asset Analysis use independent generations/timestamps.

Examples:

- `CatalogGeneration`
- `CensusSnapshot.CatalogGeneration`
- `AnalysisGeneration`

If the catalog changes after a census was captured, UI can report that the catalog changed since the snapshot. City simulation age is displayed directly rather than declaring a snapshot stale after an arbitrary timeout.

Prefab creation/update can mark `CatalogDirty`; it does not automatically trigger a full rescan.

## 17. Geometry analysis

Normal Asset Audit uses `GeometryAsset` metadata before loading Unity `Mesh` objects.

Mesh, vertex, and index totals come from the `RenderPrefab`'s serialized metadata, which is resident with the Prefab. `GeometryAsset` mesh data is streamed on demand, so per-mesh and submesh detail is read only when that data is already resident and not being loaded; the audit never triggers geometry or texture loads. Detail that is not resident is reported as Not scanned, never as zero. Likewise, `TextureAsset` dimensions and format are only known after a load has read the texture header; unread textures are Not scanned rather than failed.

`GeometryObservation` includes, where available:

- mesh count
- vertex count
- index count
- submesh count
- bounds
- compressed data size
- per-mesh metadata

Per-mesh observations may include:

- vertex count
- index count
- index format
- submesh list

Per-submesh observations include:

- topology
- index count
- vertex count
- bounds

### 17.1 Triangle calculation

Triangle count is topology-aware.

For `MeshTopology.Triangles`:

```text
triangleCount = indexCount / 3
```

Other topology types are not silently interpreted as triangles.

### 17.2 Geometry memory terminology

Three concepts remain separate:

1. Observed compressed geometry payload (`GeometryAsset.compressedDataSize` where available)
2. Derived/estimated vertex/index buffer footprint where safely calculable
3. Actual GPU-resident geometry memory — unavailable from static analysis

## 18. LOD analysis

LOD relationships are represented as Render Graph relations using `LodProperties` and related RenderPrefabs.

Per LOD, the auditor may expose:

- vertices
- indices
- topology-aware triangles
- mesh/submesh counts
- material count
- geometry payload

Derived inter-LOD metrics include:

- vertex retention/reduction
- triangle retention/reduction
- index retention/reduction
- material retention/reduction

Example evidence statement:

`LOD1 retains 83.98% of LOD0 vertices.`

Absence of a lower LOD is an observation, not automatically a performance warning.

## 19. Surface and material analysis

Normal scanning uses SurfaceAsset metadata rather than materializing Unity Materials for every asset.

`SurfaceObservation` may include:

- material/surface identity
- material template metadata where available
- virtual-texturing state
- float/int/vector/color property counts
- keywords
- texture relations

Unity Material / Shader construction and detailed material-property inspection are Deep Inspection features for selected assets.

## 20. Texture analysis

`TextureObservation` may include:

- width
- height
- depth
- format
- dimension
- mip count
- filter mode
- wrap mode
- anisotropy
- virtual-texture-related state where available

### 20.1 Texture footprint

A derived estimate may calculate full logical texture payload from dimensions, format/block compression, and mip chain.

It is labeled an estimate and is not called actual VRAM usage. Virtual Texturing, streaming, mip residency, engine behavior, and the graphics driver prevent static metadata from proving actual residency.

Shared texture resources are deduplicated. Asset details may distinguish referenced texture payload from unique texture payload.

## 21. Deep Inspection

Deep Inspection is selected-asset-only and may use heavier Unity APIs for:

- Unity `Material`
- Shader name/properties
- advanced material/texture bindings
- Unity `Mesh` fallback information if a required metric cannot be obtained from safe metadata

Deep Inspection is not automatically run across every asset.

### 21.1 Resource ownership

Deep Inspection must copy required results into domain observations and then release temporary Unity/AssetDatabase resources deterministically. Temporary Materials/Textures/Meshes or loaded property data must be destroyed, unloaded, or disposed through the appropriate API in `finally`-equivalent cleanup paths. The Observation Store must not retain Unity engine objects merely to keep a detail page alive.

If the game API returns a shared object whose lifetime is owned by the game, the adapter must not destroy it; instead it records only safe metadata and follows the API's documented ownership semantics. Resource ownership is therefore an adapter-level contract and must be covered by runtime validation.

## 22. Analysis evidence classes

All analysis results belong conceptually to one of:

### Observed
Directly read from ECS/Game/AssetDatabase/Unity APIs.

### Derived
Calculated deterministically from observations.

### Estimated
Calculated through a documented model or assumptions.

### Unknown / Unsupported
Not safely available from the current scan/API path.

The UI and export preserve this distinction.

## 23. Warning / Finding Engine

The Warning Engine consumes domain observations only. It does not access Game APIs directly.

Conceptual model:

```text
Observations
   -> Rule Evaluator
   -> Finding[]
```

A `Finding` includes:

- Rule ID
- status
- category
- title
- explanation
- evidence
- threshold/parameters where relevant
- threshold origin
- confidence/basis metadata where useful

### 23.1 Finding statuses

- `Observed`
- `Notice`
- `PotentialIssue`
- `Warning`
- `Unknown`

`Warning` is reserved for relatively strong structural/integrity evidence. Heuristics normally produce `PotentialIssue` rather than pretending to prove a performance defect.

### 23.2 Rule types

#### Deterministic
Examples:

- invalid/unresolved metadata
- missing referenced RenderAsset
- failed required metadata read

#### Heuristic
Examples:

- unusually weak LOD reduction according to a configured heuristic
- high material complexity
- very large texture dimensions

Heuristic thresholds are versioned and never presented as official guidance unless an authoritative source explicitly supports them.

#### Peer Outlier
Compare an asset with a defined reference population, such as the same category.

Possible populations:

- all comparable assets
- Vanilla/DLC comparable assets
- custom comparable assets
- same asset category
- same source pack

A Vanilla asset is a reference sample, not assumed to be optimal.

Peer-outlier rules require a minimum sample size; otherwise comparison is reported unavailable.

### 23.3 No arbitrary global score

The UI keeps separate dimensions such as:

- Geometry Complexity
- LOD Quality
- Material Complexity
- Texture Footprint
- City Exposure

No global `Performance Score` is produced.

## 24. Exposure metrics

Instance counts indicate city exposure, not render cost.

A future derived value such as `Geometry Exposure Proxy = LOD0 vertices × live object references` may be exposed only as a proxy. It must never be described as vertices rendered per frame because culling, LOD selection, visibility, batching, occlusion, and instancing are not represented.

High subordinate-object exposure is a useful evidence dimension because a small number of top-level assets may generate many child references.

## 25. Initial finding families

Candidate Phase 2–3 rule IDs include:

### Geometry

- `GEOMETRY_PEER_OUTLIER`
- `SUBMESH_COUNT_OUTLIER`
- `GEOMETRY_PAYLOAD_OUTLIER`

### LOD

- `NO_LOWER_LOD_OBSERVED`
- `LOD_REDUCTION_LOW`
- `LOD_MATERIAL_REDUCTION_LOW`
- `LOD_GEOMETRY_INCREASES`

### Material

- `MATERIAL_COUNT_OUTLIER`

### Texture

- `TEXTURE_DIMENSION_OUTLIER`
- `TEXTURE_PAYLOAD_OUTLIER`
- `HIGH_UNIQUE_TEXTURE_FOOTPRINT`

### Census / exposure

- `HIGH_CITY_EXPOSURE`
- `HIGH_SUBORDINATE_EXPOSURE`

### Integrity

- `UNRESOLVED_RENDER_ASSET`
- `UNRESOLVED_LOD_REFERENCE`
- `METADATA_READ_FAILED`

These IDs define intended rule families, not fixed numeric thresholds. Thresholds must be justified, versioned, and tested before implementation.

## 26. Rule-set versioning

Reports include `ruleSetVersion`. Changes to heuristic semantics or thresholds that affect findings must update the rule-set version.

Each finding can expose its rule version/basis so users can understand why it appeared.

## 27. UI information architecture

Top-level UI:

- Overview
- Assets
- Census
- Warnings
- Compare
- Settings

Export is a shared action/dialog, not a permanent top-level tab. Asset Details is a drill-down from the Assets view.

### 27.1 Overview

Shows data state and independent dimensions, not a combined score.

Examples:

- Prefab count
- census age and totals
- render assets analyzed
- findings by status/category
- geometry/LOD/material/texture/exposure summaries

### 27.2 Scan status

The UI shows scan state at all times while relevant. Accurate progress is shown when known; indeterminate state is used otherwise.

Closing the Auditor panel does not automatically cancel a scan. Reopening displays current progress. Changing/unloading the game World cancels/disposes world-bound work.

Escape and the close button both close the panel/modal; Escape does not cancel an active scan.

## 28. Assets table

Default columns:

```text
Name | Source | Type | Instances | Geometry | LOD | Materials | Textures | Findings
```

`Instances` is the type-aware primary count defined in section 13.2 and must expose its `CountKind`; it is not semantically identical across asset types.

Optional columns can expose detailed metrics such as:

- LOD0 vertices/triangles
- LOD1 retention
- submeshes
- compressed geometry
- unique textures
- estimated texture payload
- top-level instances
- subordinate instances
- network edges

### 28.1 Large-data strategy

The UI does not receive the entire dataset for every change.

```text
UI AssetQuery
  search
  filters
  sort
  offset
  limit
        |
        v
C# QueryService
        |
        v
filtered/sorted page
```

Search/filter/sort is primarily C#-side. Virtual scrolling or paging requests bounded result windows.

Search covers display name, Prefab name, Prefab ID, and source/pack identifiers where available.

## 29. Asset Details

Sections:

- Summary
- City Exposure
- Render Structure
- Geometry
- LOD
- Materials
- Textures
- Findings
- Technical

Render Structure visualizes the Prefab -> RenderPrefab -> Geometry/Surface/LOD relationships so measurements remain attributable to the resource from which they came.

Findings provide expandable evidence, for example:

```text
Potential Issue
LOD1 retains 83.98% of LOD0 vertices

Evidence
LOD0: 75,339
LOD1: 63,270

Basis
Heuristic
```

## 30. Census UI

Census table includes semantic counters rather than one ambiguous count:

```text
Asset | Type | Top-Level | Subordinate | Live Refs | Edges
```

Snapshot metadata includes capture time, catalog generation, query-profile version, scan options, and total counts.

Vehicle values are explicitly described as active/present at snapshot where appropriate.

## 31. Warnings UI

The Warnings view actually displays all Findings and supports filtering by:

Statuses:

- Warning
- Potential Issue
- Notice
- Observed

Categories:

- Geometry
- LOD
- Material
- Texture
- Exposure
- Integrity

Every non-trivial finding is expandable to show evidence and rule basis.

## 32. Compare

Initial Compare supports 2–4 assets.

Comparison dimensions include:

- Geometry
- LOD
- Materials
- Textures
- Census
- Findings

Peer context may be shown alongside direct asset comparison. The tool displays evidence and does not automatically declare one asset "better" overall.

Reports/snapshots with incompatible `queryProfileVersion` or materially different scan options must display a compatibility warning before comparing census-derived metrics.

## 33. Export

### 33.1 JSON

JSON is the canonical full-fidelity export.

Required report metadata includes:

- `schemaVersion`
- `ruleSetVersion`
- `queryProfileVersion`
- `scanOptions`
- `modVersion`
- `gameVersion`
- capture timestamps
- capability report

Full reports may contain catalog, census, render-resource observations, and findings.

Export scopes:

- Full report
- Current filtered assets
- Selected assets
- Census only
- Warnings/findings only

### 33.2 CSV

CSV is a flat summary export, not a substitute for the hierarchical JSON schema. It can include identifiers, type/source, instance metrics, selected geometry/LOD/material/texture summary metrics, and highest finding status.

### 33.3 Privacy and path handling

Exports must not include unnecessary personal/local-machine information such as:

- Windows username
- machine hostname
- absolute local asset/mod paths
- savegame path
- game installation path

Runtime-only Entity indexes are not exported unless a future diagnostic requirement explicitly justifies them.

Export failure never destroys in-memory audit data.

## 34. Settings

Settings categories:

### General

- default opening page
- remembered columns/filters
- UI scale

### Scanning

- frame-processing budget within safe bounds
- progress update rate within safe bounds
- refresh catalog at scan start
- subordinate-count inclusion where supported
- default audit mode

### Analysis

- enable heuristic findings
- enable peer-outlier analysis
- comparison population preference
- show/hide Notice-level findings

### Advanced

- bounded reducer/main-thread budget
- bounded page-size controls
- metadata cache limits
- Deep Inspection limits

Settings indicate whether changes apply immediately, on the next scan, or require restart.

Automatic periodic full Census, full Asset Audit, and full Deep Inspection are not MVP features.

Settings that change which optional metrics are collected are recorded in `scanOptions`; unavailable/disabled metrics remain `NotScanned` rather than zero.

## 35. Diagnostics

Diagnostics exposes:

- game version
- mod version
- compatibility state
- capability states
- Harmony: not used
- last scan result/timing
- aggregated diagnostic failures
- failed-asset counts

Diagnostics should help users create actionable issue reports without exposing private local paths.

## 36. Error model

Errors are isolated by scope.

### Item-level

One asset/texture/etc. fails. Scan continues; the affected observation is `Failed` with a diagnostic code.

### Feature-level

A collector/capability is unavailable, for example Texture metadata after a game update. Other collectors remain operational.

### Scan-level

The active scan cannot continue safely. It stops, working buffers are disposed, and the previous successful published snapshot is retained.

### 36.1 Diagnostic codes

Subsystem-scoped codes use a stable pattern such as:

- `APA-CAT-###`
- `APA-CEN-###`
- `APA-GEO-###`
- `APA-SRF-###`
- `APA-TEX-###`
- `APA-EXP-###`
- `APA-AUD-###` (Asset Audit scan)
- `APA-DEEP-###` (selected-asset Deep Inspection)

The UI shows a concise message and code; logs may contain technical exception details and context.

Repeated identical diagnostics are aggregated to avoid log flooding.

## 37. Performance strategy

The Auditor must avoid becoming a significant performance source itself.

### 37.1 Idle behavior

Idle state performs no full-world scan and no automatic full metadata walk.

### 37.2 Frame-budgeted managed work

Managed catalog/reduction/analysis work is sliced by elapsed-time budget rather than a fixed item count.

Implementation begins with a conservative budget (roughly around 1 ms/frame as an experimental starting point) and final defaults are determined through in-game validation. The specification does not guarantee a universal 1 ms cap across all machines/workloads.

### 37.3 ECS/native work

Large entity reads use appropriate EntityQuery/chunk/job mechanisms where beneficial. The design avoids scheduling async work and then blocking the same frame with immediate completion.

### 37.4 GC policy

Avoid per-entity managed allocation, repeated large temporary collections, and LINQ-heavy hot paths. Zero-allocation purity is not a goal if it harms safety/readability outside hot paths.

### 37.5 Native memory

Capture data should be compact. If validation shows that a full sample array creates excessive memory pressure, implementation may reduce per chunk/job so long as the observable census semantics and cancellation/atomic-publication guarantees remain unchanged.

### 37.6 Deduplication

Shared RenderPrefab/GeometryAsset/SurfaceAsset/TextureAsset resources are analyzed once per relevant generation/key rather than once per referencing Prefab.

### 37.7 UI update throttling

Progress/UI telemetry is throttled (approximately several updates per second as a starting point) rather than emitted per entity/item.

### 37.8 Self-telemetry

Low-overhead scan diagnostics should record where feasible:

- total elapsed scan time
- entities/items processed
- managed frame-slice statistics such as max/P95
- allocation/memory information where it can be measured without heavy continuous profiling

## 38. Testing strategy

Testing is explicitly divided into:

1. Pure Core Tests
2. Adapter Contract Tests
3. UI Tests
4. In-Game Runtime Validation

Automated tests and static inspection do not convert an unexecuted in-game scenario into `PASS`.

### 38.1 Pure Core Tests

Game-independent tests cover:

- LOD reduction calculations
- topology-aware triangle calculations
- texture footprint estimation
- percentile/peer statistics
- finding generation
- filtering/sorting/query behavior
- export serialization/schema versioning
- census aggregation
- snapshot lifecycle
- cancellation/atomic publication

Examples:

- Triangles topology with 300 indices -> 100 triangles.
- Lines topology with 300 indices -> triangle count `NotApplicable`.
- Cancelled/failed scans do not replace the previously published snapshot.
- Missing evidence does not generate fabricated findings.

### 38.2 Peer-analysis tests

Tests cover percentile calculations, minimum sample-size behavior, population selection, and unavailable peer comparison for insufficient samples.

### 38.3 Texture-estimate tests

Fixtures cover representative compressed/uncompressed formats and mip-chain handling. Estimated payload must never be serialized/displayed as actual VRAM residency.

### 38.4 Export compatibility tests

Golden JSON fixtures protect schema semantics. Meaningful schema-breaking changes require a schema-version update.

### 38.5 Adapter Contract Tests

With the target DLL references available, contract tests verify required public member presence/signatures and graceful capability degradation.

Passing contract tests does not prove correct real-game behavior.

### 38.6 UI Tests

Tests cover:

- query construction
- filters/sorts
- bounded paging/virtualization behavior
- progress states
- `NotScanned` vs numeric zero
- `Unknown`/`Unsupported`
- finding evidence expansion
- Escape/close behavior
- type-aware `Instances`/`CountKind` presentation

### 38.7 Query-profile tests

Tests must verify Query Profile v1 invariants independently of adapter syntax:

- each supported object entity contributes at most once to `LiveObjectReferences`,
- an entity satisfying multiple subordinate markers is counted once in `SubordinateObjects`,
- omitted subordinate collection produces `NotScanned`, not zero,
- incompatible query-profile versions or materially different scan options are detectable during comparison/export workflows.

## 39. In-game runtime validation

Runtime validation is documented separately, for example under `docs/validation/runtime-validation.md`.

Validation statuses:

- `PASS`
- `FAIL`
- `NOT RUN`
- `BLOCKED`

Required scenarios include:

### Vanilla small city

- Census completes
- counts are plausible
- cancel works
- no unacceptable main-thread freeze

### Large city

- progress remains responsive
- frame-slice timings recorded
- memory is reclaimed after scan

### Custom-asset-heavy city

- source metadata identified where available
- geometry metadata read
- texture metadata read
- Asset Details works

### Broken/incomplete asset

- failure remains item-level where possible
- scan continues

### World change during scan

- scan cancels safely
- native resources disposed
- previous-world census is not shown as current-world data

### UI close / Escape during scan

- panel closes
- scan continues
- reopening shows current progress

### Deep Inspection ownership

- temporary resources are cleaned up after success, cancellation, and exception paths,
- shared game-owned resources are not incorrectly destroyed,
- repeated Deep Inspection does not show unbounded retained Unity-object growth.

## 40. Performance validation matrix

Runtime performance results should record representative combinations of:

- small / medium / large city
- Vanilla-heavy / custom-asset-heavy environment

Metrics include:

- entity count
- Prefab count
- render-asset count
- total scan time
- maximum managed slice
- P95 managed slice
- memory/allocation delta where measurable

Final default scan budgets are chosen from this evidence.

## 41. Update strategy

For each new target game version:

1. compile against new DLLs/toolchain,
2. run Pure Core tests,
3. run Adapter Contract Tests,
4. launch the game and inspect Capability Registry,
5. validate Vanilla scenario,
6. validate Custom Asset scenario,
7. validate representative large-city performance,
8. mark the version `Supported` only after required runtime validation.

A capability failure disables that capability where safe rather than automatically disabling the entire mod.

Reflection is not used as an automatic escape hatch when a public API changes; unsupported functionality is preferable to fragile hidden-member access unless a separate design explicitly justifies it.

## 42. External game DLL handling

Supplied game DLLs are research/build references and are not committed to this repository. The build should resolve CS2 references through the official/local modding toolchain or documented local reference setup.

This avoids redistribution, binary bloat, and stale binary copies.

## 43. CI strategy

CI can validate:

- Pure Core tests
- UI tests
- schema/golden-file tests
- static checks
- builds where required references/toolchain are legally and technically available

CI cannot independently prove:

- that Cities: Skylines II loads the mod,
- that runtime AssetDatabase behavior matches assumptions,
- that large-city scan behavior is acceptable,
- that game-integrated UI bindings behave correctly.

Those remain runtime validation concerns.

## 44. Phase definitions and completion criteria

Phases 0–4 belong to one product architecture but are intentionally milestone-separated. The implementation plan may be long; it must preserve these phase gates rather than attempting all features as one undifferentiated implementation step.

### Phase 0 — Compatibility Foundation

- game-version reporting
- Capability Registry
- Game Integration interfaces/adapters
- core data contracts

### Phase 1 — Prefab Catalog + Asset Instance Census

Required:

- Prefab catalog
- source evidence/classification
- Prefab type/traits
- snapshot census
- top-level/subordinate/live/network counts
- search/filter/sort
- progress
- cancellation
- atomic snapshot publication
- snapshot age/generation metadata
- minimal JSON export
- diagnostics

**Success condition:** without modifying the city, a user-triggered snapshot can enumerate supported Prefabs, relate current city entities to them, expose source/type/count semantics, and provide searchable/exportable evidence.

### Phase 2 — Render Graph + Geometry / LOD Auditor

Required:

- Prefab -> RenderPrefab relations for verified resolver families
- explicit render-coverage state for unsupported/unresolved families
- GeometryAsset metadata
- mesh/submesh metadata
- topology-aware triangle counts
- bounds
- compressed geometry payload
- LOD relations and reduction metrics
- geometry/LOD findings

Normal scanning must not materialize Unity Mesh objects for every asset.

### Phase 3 — Surface / Texture Auditor

Required:

- SurfaceAsset metadata
- texture relations
- dimensions
- format
- mip metadata
- VT-related state where available
- safe estimated texture payload
- material/texture findings

Actual VRAM is not inferred from these static values.

### Phase 4 — Analysis UX

Required:

- Asset Details
- Compare
- richer Findings UI
- full JSON export
- CSV summary export
- selected-asset Deep Inspection
- quality-of-life UI/settings

### Phase 5 — Runtime Evidence (separate future design)

Potential topics:

- actual render/runtime observations
- renderer integration
- residency/runtime evidence
- possible Harmony evaluation

Phase 5 is not part of the implementation plan derived from this specification.

## 45. MVP non-functional requirements

- read-only city behavior
- no Harmony
- no always-on full entity scan
- bounded/scalable UI data transfer
- explicit progress/cancellation
- no partial snapshot publication
- safe disposal on cancellation/failure/world unload
- no unnecessary absolute local paths in exports
- meaningful diagnostics
- capability degradation rather than broad failure where possible

## 46. Repository/documentation expectations

The implementation plan derived from this spec should create a repository structure that keeps:

- Game Integration code isolated from Core analysis,
- pure Core logic testable without the game,
- UI separate from scanning/data access,
- validation documentation separate from automated test claims.

The implementation plan, not this specification, will determine exact file names/project layout after the written specification is approved.

## 47. Final architectural invariants

The following are design invariants unless a later approved spec explicitly changes them:

- The mod is a read-mostly diagnostic tool.
- Harmony is not used in Phases 1–4.
- Reflection is not required by the current design and is not an automatic fallback.
- Full-world Census is user-triggered snapshot work, not an always-on tracker.
- Gameplay Prefab identity is distinct from RenderPrefab/resource identity.
- Static metadata is distinct from runtime rendering cost.
- Observed, Derived, Estimated, and Unknown/Unsupported values remain distinguishable.
- Findings carry evidence and basis.
- No global performance score is produced.
- Failed/cancelled scans do not corrupt the last successful snapshot.
- Shared render resources are deduplicated.
- Heavy Unity object materialization is restricted to selected-asset Deep Inspection.
- New game versions are capability-probed and runtime-validated before being marked Supported.
- Phase 5 requires a separate design decision.

## 48. Approval and next step

This written specification captures the approved conversational design. After user review and approval of this file, the next Superpowers stage is `writing-plans`, which will create a separate detailed implementation plan. Implementation must not begin before that plan is written and its execution approach is selected.
