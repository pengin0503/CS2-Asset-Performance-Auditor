# CS2 Asset Performance Auditor

CS2 Asset Performance Auditor is a read-mostly diagnostic Code Mod for Cities: Skylines II. It catalogs gameplay Prefabs, captures user-triggered current-city census snapshots, and exposes evidence without changing the city or claiming static metadata is runtime rendering cost.

The current implementation target is Cities: Skylines II `1.6.2f1`. Phases 1–4 use no Harmony. The project separates game-independent Core contracts from CS2/ECS integration, and the React UI requests bounded pages instead of receiving the full catalog on each update.

## Installation

### Current distribution status

There is currently no prebuilt GitHub Release for this repository. Until a packaged release is published, the supported installation method is to build the repository locally with the official Cities: Skylines II Modding Toolchain and let the toolchain deploy the resulting Code Mod to the local game installation.

BepInEx is not required. Do not install the source repository directly into the game's `Mods` folder, and do not copy only `CS2AssetPerformanceAuditor.dll`; the mod also includes a generated UI bundle that must be deployed together with the code assembly.

The current implementation target is Cities: Skylines II `1.6.2f1`. If your installed game is newer, the project may still build, but that alone does not prove runtime compatibility with the newer game version.

### 1. Prerequisites

Before downloading the repository, make sure the following are available:

- **Cities: Skylines II for Windows**, installed locally.
- **The official Cities: Skylines II Modding Toolchain**, installed and initialized for your current game installation.
- **A .NET SDK** compatible with the installed CS2 Modding Toolchain.
- **Node.js 18 or newer**. npm is included with normal Node.js installations.
- **Git**, only if you want to clone/pull the repository. Git is not required if you use GitHub's source ZIP download.

You can verify the two normal command-line prerequisites from PowerShell:

```powershell
dotnet --version
node --version
npm --version
```

`node --version` must report Node.js `18` or newer. If any command is not recognized, install or repair that prerequisite before continuing.

### 2. Make sure the CS2 Modding Toolchain is initialized

Start Cities: Skylines II and initialize/install the official Modding Toolchain through the game's modding/development options before trying to build this repository.

This project imports the official toolchain files `Mod.props` and `Mod.targets`. A distributable game build also references the managed assemblies from your own Cities: Skylines II installation. Those Paradox/Unity assemblies are intentionally not stored in this repository.

You need to know two local directories:

1. **CS2 Toolchain directory** — the directory that contains both:

   ```text
   Mod.props
   Mod.targets
   ```

2. **CS2 Managed directory** — the game's:

   ```text
   Cities2_Data\Managed
   ```

   directory. It must contain `Game.dll`.

For a default Steam installation, the managed directory is commonly similar to:

```text
C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed
```

Your actual Steam library may be on another drive, so use the real path from your installation rather than copying this example blindly.

### 3. Download the source

Choose either method below.

#### Option A: Download the source ZIP

1. Open this repository on GitHub.
2. Select **Code**.
3. Select **Download ZIP**.
4. Extract the ZIP to a normal writable folder, for example:

   ```text
   C:\Users\<your-user-name>\Documents\CS2-Asset-Performance-Auditor
   ```

5. Open that extracted folder in File Explorer.

Avoid building directly inside the downloaded ZIP, a temporary browser directory, or a protected directory such as `C:\Program Files`.

#### Option B: Clone with Git

From PowerShell:

```powershell
git clone https://github.com/pengin0503/CS2-Asset-Performance-Auditor.git
cd CS2-Asset-Performance-Auditor
```

If you downloaded the ZIP instead, open PowerShell in the extracted repository root. The correct root contains at least:

```text
README.md
CS2AssetPerformanceAuditor.sln
src\
UI\
tests\
```

### 4. Configure the CS2 paths

The project recognizes these environment variables:

- `CSII_TOOLPATH` — directory containing `Mod.props` and `Mod.targets`.
- `CSII_MANAGEDPATH` — the current game's `Cities2_Data\Managed` directory containing `Game.dll`.

If your official toolchain already supplies its equivalent `ManagedPath` automatically, `CSII_MANAGEDPATH` may not be necessary. Setting both explicitly is the clearest setup when building the repository manually.

#### Temporary setup for the current PowerShell window

Replace the example paths with the actual paths on your PC:

```powershell
$env:CSII_TOOLPATH = "C:\path\to\CS2\ModdingToolchain"
$env:CSII_MANAGEDPATH = "C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed"
```

Verify them before building:

```powershell
Test-Path "$env:CSII_TOOLPATH\Mod.props"
Test-Path "$env:CSII_TOOLPATH\Mod.targets"
Test-Path "$env:CSII_MANAGEDPATH\Game.dll"
```

All three commands should return:

```text
True
```

If any returns `False`, correct that path before continuing.

These `$env:` assignments apply only to the current PowerShell process. Closing the terminal removes them.

#### Optional: save the paths as Windows user environment variables

If you build the project regularly, you can store them for your Windows user account:

```powershell
[Environment]::SetEnvironmentVariable("CSII_TOOLPATH", "C:\path\to\CS2\ModdingToolchain", "User")
[Environment]::SetEnvironmentVariable("CSII_MANAGEDPATH", "C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed", "User")
```

Close and reopen PowerShell after doing this so the new user environment variables are loaded into the terminal.

### 5. Install the UI dependencies

From the repository root, run:

```powershell
cd UI
npm ci
cd ..
```

`npm ci` installs the exact dependency versions recorded in `UI/package-lock.json`. It may take some time on the first run and creates `UI\node_modules`.

Confirm that Node.js/npm is working before continuing if this step fails:

```powershell
node --version
npm --version
```

You normally need to run `npm ci` again only after downloading a revision where `UI/package-lock.json` changed, after deleting `UI\node_modules`, or when repairing the local dependency installation.

### 6. Build the mod

Still from the repository root, run:

```powershell
dotnet build src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj -c Release
```

The project imports the official CS2 `Mod.props`/`Mod.targets` when `CSII_TOOLPATH` is valid. When the CS2 deployment target runs, the project also runs the Asset Auditor UI production build before deployment and copies the generated UI files into the mod output.

A successful build should end without MSBuild errors. If the project builds only the game-independent fallback instead of the actual CS2 mod, re-check that `CSII_MANAGEDPATH` points to a directory containing `Game.dll` and that the official toolchain files are being imported.

### 7. Verify the deployed files

With the official CS2 toolchain configured correctly, its deployment target places the development mod under the local Cities: Skylines II mods area:

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\
```

You can open that directory directly from PowerShell with:

```powershell
explorer "$env:USERPROFILE\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods"
```

After the build/deployment step, confirm that an Asset Auditor mod directory has been created or refreshed there and that it contains the deployed code plus UI files.

Do **not** manually copy only:

```text
CS2AssetPerformanceAuditor.dll
```

into a random mod directory. The in-game panel depends on the UI bundle produced from the `UI` project, so an assembly-only installation is incomplete.

If `dotnet build` succeeds but nothing is deployed under the CS2 `Mods` directory, see **Troubleshooting** below. The exact deployment behavior is supplied by the locally installed official `Mod.targets`, not by custom deployment code in this repository.

### 8. Start the game and check the mod

1. Close Cities: Skylines II if it was running while you built/deployed the mod.
2. Start Cities: Skylines II normally.
3. Make sure the local Code Mod is enabled in the game's mod/playset configuration if the game presents it as disabled.
4. Open the game's Options/Mods UI and confirm that **CS2 Asset Performance Auditor** registers its settings.
5. Load a city.
6. Open **Asset Auditor** from its in-game UI entry.

The Auditor performs its substantial scans only when requested by the user. Simply installing/enabling the mod is not intended to start an automatic periodic full-city audit.

### 9. Updating an existing source installation

If you cloned the repository with Git:

```powershell
git pull --ff-only origin main
```

If you use source ZIPs instead, download the current ZIP again and extract it to a clean/new source folder rather than mixing files from different revisions.

Then:

1. Run `npm ci` again if `UI/package-lock.json` changed or dependencies were removed.
2. Confirm `CSII_TOOLPATH` and `CSII_MANAGEDPATH` still point to the current toolchain/game files.
3. Rebuild:

   ```powershell
   dotnet build src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj -c Release
   ```

4. Confirm that the corresponding local mod directory under `AppData\LocalLow\Colossal Order\Cities Skylines II\Mods` was refreshed.
5. Restart the game before testing the new build.

If a future GitHub Release provides a packaged build, use the installation/update instructions included with that release instead of assuming the source-build procedure is identical.

### 10. Uninstalling the local development build

1. Exit Cities: Skylines II.
2. Open:

   ```text
   %USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\
   ```

3. Find the directory belonging to **CS2 Asset Performance Auditor**.
4. Remove that mod directory.
5. Start the game and verify that the mod is no longer listed/enabled.

Only delete the Asset Auditor's own directory. Do not remove the entire `Mods` directory because it may contain other local mods.

Deleting the source repository from your development folder does **not** necessarily uninstall an already deployed copy from the game's local `Mods` directory.

### Troubleshooting

#### `Mod.props` or `Mod.targets` cannot be found

Check:

```powershell
Test-Path "$env:CSII_TOOLPATH\Mod.props"
Test-Path "$env:CSII_TOOLPATH\Mod.targets"
```

Both must be `True`. `CSII_TOOLPATH` must be the directory containing those files, not a parent directory several levels above them.

#### `Game.dll` cannot be found / CS2 API is not available during build

Check:

```powershell
Test-Path "$env:CSII_MANAGEDPATH\Game.dll"
```

It must be `True`. Point `CSII_MANAGEDPATH` at `Cities2_Data\Managed`, not at the game root directory.

#### `npm` or `node` is not recognized

Install/reinstall Node.js 18 or newer, then open a new PowerShell window and check:

```powershell
node --version
npm --version
```

#### UI build fails because packages are missing

From the repository root:

```powershell
cd UI
npm ci
npm run build
cd ..
```

If this succeeds, run the normal .NET Release build again.

#### .NET build succeeds but the mod is not visible in the game

Check all of the following:

- The official CS2 Modding Toolchain was initialized for the currently installed game version.
- The build used the correct `CSII_TOOLPATH`.
- `CSII_MANAGEDPATH\Game.dll` exists.
- A deployed Asset Auditor directory exists under `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\`.
- The deployed directory contains the UI output as well as the mod assembly.
- The mod is enabled in the game's active mod/playset configuration.
- The game was restarted after rebuilding the mod.

#### The game updated after the last successful build

Re-check the official toolchain and the `Cities2_Data\Managed` path, rebuild against the current local assemblies, and treat the mod as not runtime-validated for the new game version until the relevant compatibility/runtime checks have actually been executed.

A successful Core/UI GitHub Actions run or a successful local compilation does not by itself prove that an in-game runtime scenario passed.

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
