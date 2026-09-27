# CS2 Asset Performance Auditor

CS2 Asset Performance Auditor is a read-mostly diagnostic Code Mod for Cities: Skylines II. It catalogs gameplay Prefabs, captures user-triggered current-city census snapshots, and exposes evidence without changing the city or claiming static metadata is runtime rendering cost.

The current implementation target is Cities: Skylines II `1.6.2f1`. Phases 1–4 use no Harmony. The project separates game-independent Core contracts from CS2/ECS integration, and the React UI requests bounded pages instead of receiving the full catalog on each update.

## Development

- Run Core tests: `dotnet test tests/CS2AssetPerformanceAuditor.Tests/CS2AssetPerformanceAuditor.Tests.csproj`
- Run UI tests and production build: `cd UI && npm ci && npm test && npm run build`
- Build the game mod when the CS2 modding toolchain and managed assemblies are available: `dotnet build src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj -c Debug`

Set `CSII_TOOLPATH` to the directory containing the CS2 `Mod.props` and `Mod.targets`. Set `CSII_MANAGEDPATH` (or the toolchain's `ManagedPath` property) to the game's local `Cities2_Data/Managed` directory. Game and Unity assemblies are local build references and are not part of this repository.

## Evidence boundaries

The Auditor does not modify gameplay entities or Prefabs, run an always-on full-world scan, or produce a global performance score. Observed, derived, estimated, and unavailable values remain distinct in the UI and exports. Runtime rendering cost, visibility, draw calls, and GPU residency are not inferred from static asset metadata.
