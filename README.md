# CS2 Asset Performance Auditor

CS2 Asset Performance Auditor is a diagnostic Code Mod for Cities: Skylines II. It catalogs gameplay Prefabs, captures current-city census snapshots when requested, and presents evidence that can help identify assets worth investigating without changing the city.

The Auditor keeps observed, derived, estimated, and unavailable values distinct. Static asset metadata is not presented as proof of runtime rendering cost.

## What it does

- Catalogs gameplay Prefabs and their available metadata.
- Captures a snapshot of asset usage in the currently loaded city when you request a census.
- Shows compatibility and scan diagnostics when data is unavailable, incomplete, or cannot be matched reliably.
- Records bounded self-telemetry for user-triggered scans so you can see the cost of the diagnostic operation itself.
- Keeps substantial scans manual rather than continuously scanning the entire city in the background.
- Does not automatically modify gameplay entities, Prefabs, or city data.

## Compatibility

The current supported development target is Cities: Skylines II `1.6.2f1`.

A newer game version may still work, but a successful build alone does not prove that the mod is runtime-compatible with that version. If the game has updated, rebuild against the current official toolchain and game assemblies before testing.

## Installation

### Current distribution status

There is currently no prebuilt GitHub Release for this repository. Until a packaged release is published, install the mod by building the repository locally with the official Cities: Skylines II Modding Toolchain and allowing the toolchain to deploy the resulting Code Mod.

BepInEx is not required. Do not copy the source repository directly into the game's `Mods` folder, and do not copy only `CS2AssetPerformanceAuditor.dll`; the in-game panel also requires the generated UI bundle.

### 1. Prerequisites

Before downloading the repository, make sure the following are available:

- **Cities: Skylines II for Windows**, installed locally.
- **The official Cities: Skylines II Modding Toolchain**, installed and initialized for the current game installation.
- **A .NET SDK** compatible with the installed CS2 Modding Toolchain.
- **Node.js 18 or newer**. npm is included with normal Node.js installations.
- **Git**, only if you want to clone/pull the repository. Git is not required when using GitHub's source ZIP download.

You can verify the normal command-line prerequisites from PowerShell:

```powershell
dotnet --version
node --version
npm --version
```

`node --version` must report Node.js `18` or newer.

### 2. Initialize the CS2 Modding Toolchain

Start Cities: Skylines II and install/initialize the official Modding Toolchain for the current game installation before building this repository.

You need to know two local directories:

1. **CS2 Toolchain directory** — the directory containing:

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

Your Steam library may be on another drive, so use the actual path from your installation.

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

Avoid building directly inside the downloaded ZIP, a temporary browser directory, or a protected directory such as `C:\Program Files`.

#### Option B: Clone with Git

From PowerShell:

```powershell
git clone https://github.com/pengin0503/CS2-Asset-Performance-Auditor.git
cd CS2-Asset-Performance-Auditor
```

If you downloaded the ZIP instead, open PowerShell in the extracted repository root. The repository root contains at least:

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

If the official toolchain already supplies its managed-assembly path automatically, `CSII_MANAGEDPATH` may not be necessary. Setting both explicitly is the clearest setup when building manually.

For the current PowerShell window, replace the example paths with the actual paths on your PC:

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

If any returns `False`, correct the corresponding path before continuing.

These `$env:` assignments apply only to the current PowerShell process.

### 5. Install the UI dependencies

From the repository root, run:

```powershell
cd UI
npm ci
cd ..
```

`npm ci` installs the dependency versions recorded in `UI/package-lock.json`.

### 6. Build and deploy the mod

From the repository root, run:

```powershell
dotnet build src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj -c Release
```

With the official toolchain configured correctly, the build also produces the UI bundle and deploys the development mod to the local Cities: Skylines II mods area.

A successful build should finish without MSBuild errors. If the build cannot find the CS2 toolchain or game assemblies, re-check `CSII_TOOLPATH` and `CSII_MANAGEDPATH`.

The deployed development mod is normally placed under:

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\
```

Confirm that the Asset Auditor directory contains both the mod code and its UI files.

### 7. Start the game

1. Restart Cities: Skylines II after building/deploying the mod.
2. Make sure **CS2 Asset Performance Auditor** is enabled in the active mod/playset configuration.
3. Load a city.
4. Open **Asset Auditor** from its in-game UI entry.
5. Use the panel's **Settings** tab to change the Auditor's own settings. These settings persist between sessions.

The Auditor performs substantial scans only when requested. Enabling the mod does not start an automatic periodic full-city audit.

## Updating a source installation

If you cloned the repository with Git:

```powershell
git pull --ff-only origin main
```

If you use source ZIPs, download the current ZIP again and extract it to a clean source folder rather than mixing files from different revisions.

Then:

1. Run `npm ci` again if `UI/package-lock.json` changed or dependencies were removed.
2. Confirm `CSII_TOOLPATH` and `CSII_MANAGEDPATH` still point to the current toolchain/game files.
3. Rebuild with:

   ```powershell
   dotnet build src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj -c Release
   ```

4. Restart the game before testing the updated build.

If a future GitHub Release provides a packaged build, follow the installation/update instructions included with that release instead.

## Uninstalling the local development build

1. Exit Cities: Skylines II.
2. Open:

   ```text
   %USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\
   ```

3. Find the directory belonging to **CS2 Asset Performance Auditor**.
4. Remove only that mod directory.
5. Start the game and verify that the mod is no longer listed/enabled.

Deleting the source repository from your development folder does not necessarily remove an already deployed copy from the game's local `Mods` directory.

## Troubleshooting

### `Mod.props` or `Mod.targets` cannot be found

Check:

```powershell
Test-Path "$env:CSII_TOOLPATH\Mod.props"
Test-Path "$env:CSII_TOOLPATH\Mod.targets"
```

Both must return `True`. `CSII_TOOLPATH` must point to the directory containing those files.

### `Game.dll` cannot be found / CS2 API is not available during build

Check:

```powershell
Test-Path "$env:CSII_MANAGEDPATH\Game.dll"
```

It must return `True`. Point `CSII_MANAGEDPATH` at `Cities2_Data\Managed`, not the game root directory.

### `npm` or `node` is not recognized

Install/reinstall Node.js 18 or newer, open a new PowerShell window, and check:

```powershell
node --version
npm --version
```

### UI build fails because packages are missing

From the repository root:

```powershell
cd UI
npm ci
npm run build
cd ..
```

If this succeeds, run the normal .NET Release build again.

### .NET build succeeds but the mod is not visible in the game

Check that:

- The official CS2 Modding Toolchain is initialized for the currently installed game version.
- `CSII_TOOLPATH` points to the active toolchain.
- `CSII_MANAGEDPATH\Game.dll` exists.
- An Asset Auditor directory exists under `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\`.
- The deployed directory contains both the mod assembly and UI output.
- The mod is enabled in the active mod/playset configuration.
- The game was restarted after rebuilding the mod.

### The game updated after the last successful build

Re-check the official toolchain and `Cities2_Data\Managed` path, then rebuild against the current local assemblies. Treat compatibility with the new game version as unverified until it has been tested in-game.

## Diagnostics and limitations

Diagnostics can report compatibility/capability state, the latest scan state, unresolved or unmatched items, aggregated diagnostic occurrences, and bounded scan self-telemetry.

Self-telemetry measures only user-triggered managed scan work and is not a continuous game profiler.

The Auditor does not:

- Modify gameplay entities or Prefabs.
- Run an always-on full-world scan.
- Produce a single global performance score.
- Infer runtime rendering cost, visibility, draw calls, or GPU residency from static asset metadata alone.
