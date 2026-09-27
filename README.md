# CS2 Asset Performance Auditor

CS2 Asset Performance Auditor is a read-mostly diagnostic Code Mod for Cities: Skylines II. It catalogs gameplay Prefabs, captures user-triggered current-city census snapshots, and exposes evidence without changing the city or claiming static metadata is runtime rendering cost.

The current implementation target is Cities: Skylines II `1.6.2f1`. Phases 1–4 use no Harmony. The project separates game-independent Core contracts from CS2/ECS integration, and the React UI requests bounded pages instead of receiving the full catalog on each update.

## Development

- Run Core tests: `dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj`
- Run UI tests and production build: `cd UI && npm ci && npm test && npm run build`
- Build the game mod when the CS2 modding toolchain and managed assemblies are available: `dotnet build src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj -c Release`

Set `CSII_TOOLPATH` to the directory containing the CS2 `Mod.props` and `Mod.targets`. Set `CSII_MANAGEDPATH` (or the toolchain's `ManagedPath` property) to the game's local `Cities2_Data/Managed` directory. Game and Unity assemblies are local build references and are not part of this repository.

GitHub Actions intentionally runs only checks that do not require redistribution of Cities: Skylines II or Unity game assemblies: Pure Core tests plus UI tests/build. A green CI run is not evidence that an in-game runtime scenario passed.

## Game-update validation procedure

After a Cities: Skylines II update, validate compatibility in this order before marking affected capabilities Supported:

1. Compile against the updated local CS2 modding toolchain and managed assemblies.
2. Run the Pure Core test suite.
3. Run Adapter/public-contract checks against the updated assemblies.
4. Inspect the capability probe results; unavailable or changed APIs remain Degraded/Unsupported rather than being assumed compatible.
5. Run the Vanilla/small-city runtime matrix scenarios.
6. Run custom-asset-heavy and broken/incomplete-asset scenarios.
7. Run large-city performance validation and record workload size, total scan time, and max/P95 managed-slice telemetry.
8. Run selected Deep Inspection ownership scenarios when that capability changed.
9. Only then update capability support claims and validation status.

The runtime matrix and required evidence are maintained in `docs/validation/runtime-validation.md`. Anything not actually executed remains `NOT RUN` or `BLOCKED`.

## Diagnostics and self-telemetry

Diagnostics expose compatibility/capability state, `Harmony: not used`, the last scan state/diagnostic code, unresolved/unmatched counts, aggregated diagnostic occurrences, and bounded scan self-telemetry. Self-telemetry measures only user-triggered managed scan slices and uses a bounded sample window; it is not a continuous game profiler.

## Evidence boundaries

The Auditor does not modify gameplay entities or Prefabs, run an always-on full-world scan, or produce a global performance score. Observed, derived, estimated, and unavailable values remain distinct in the UI and exports. Runtime rendering cost, visibility, draw calls, and GPU residency are not inferred from static asset metadata.
