# CS2 Asset Performance Auditor

CS2 Asset Performance Auditor is a read-mostly diagnostic Code Mod for Cities: Skylines II. It catalogs gameplay Prefabs, captures user-triggered current-city census snapshots, and exposes evidence without changing the city or claiming static metadata is runtime rendering cost.

The current implementation target is Cities: Skylines II `1.6.2f1`. Phases 1–4 use no Harmony. The project separates game-independent Core contracts from CS2/ECS integration, and the React UI requests bounded pages instead of receiving the full catalog on each update.

## Installation

### Current distribution status

There is currently no prebuilt GitHub Release for this repository. Until a packaged release is published, install the mod by building the repository with the official Cities: Skylines II Modding Toolchain. BepInEx is not required.

The current build target is Cities: Skylines II `1.6.2f1`. A newer game version may require compatibility validation before the mod works correctly.

### Requirements

- Windows with Cities: Skylines II installed.
- The official Cities: Skylines II Modding Toolchain installed/initialized from the game's Modding options.
- A .NET SDK accepted by the installed CS2 Modding Toolchain.
- Node.js `18` or newer and npm for building the React UI.
- Git is optional; you can either clone the repository or download the source ZIP from GitHub.

The build expects:

- `CSII_TOOLPATH` to point to the CS2 toolchain directory containing `Mod.props` and `Mod.targets`.
- `CSII_MANAGEDPATH` to point to the game's local `Cities2_Data/Managed` directory, unless the toolchain already supplies the equivalent `ManagedPath` value.

Game and Unity DLLs are local build references. They are not included in this repository and must not be copied into the mod package.

### Build and install

1. Clone this repository, or download and extract the source ZIP.
2. Open a terminal in the repository root.
3. Install the UI dependencies once:

   ```text
   cd UI
   npm ci
   cd ..
   ```

4. Build the mod in Release configuration:

   ```text
   dotnet build src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj -c Release
   ```

   The project runs the UI production build as part of the CS2 deployment target and copies the generated UI bundle into the mod output before deployment.

5. With the official CS2 toolchain configured correctly, its `Mod.targets` deployment step installs the built mod into the local Cities: Skylines II mods area under:

   ```text
   %USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\
   ```

   Do not copy only `CS2AssetPerformanceAuditor.dll` by itself; the deployed mod also needs the generated UI files.

6. Start Cities: Skylines II. The mod registers its settings in the game's Mods/Options UI. Load a city and open **Asset Auditor** to use the in-game panel.

### Updating a source installation

Pull/download the newer source, run `npm ci` again if `UI/package-lock.json` changed, and rebuild the same Release project. The CS2 toolchain deployment step will refresh the local development copy.

If a future GitHub Release provides a packaged build, prefer the installation instructions shipped with that release rather than rebuilding from source.

### If the build does not deploy

Check these items first:

- `CSII_TOOLPATH` resolves to a directory containing both `Mod.props` and `Mod.targets`.
- `CSII_MANAGEDPATH` or the toolchain `ManagedPath` resolves to the current game's `Cities2_Data/Managed` directory and contains `Game.dll`.
- `node --version` reports Node.js 18 or newer.
- `UI/node_modules` exists after `npm ci`.
- The official CS2 Modding Toolchain has been initialized for the currently installed game version.

A successful Core/UI CI run does not prove that a local game/toolchain build or an in-game runtime scenario passed; those checks require the local Cities: Skylines II installation.

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
