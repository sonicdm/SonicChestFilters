# AGENTS.md — Sonic Chest Filters

Guidance for Cursor agents (and humans) working in this repo.

## What this is

Valheim **BepInEx** mod: chest filter controls for nearby containers.

| | |
| --- | --- |
| GUID | `com.sonicdm.valheim.sonicchestfilters` |
| Assembly | `SonicChestFilters.dll` |
| Source | `src/` (`SonicChestFilters.cs`, `NearbyContainers.cs`, `ItemLocate.cs`, `ChestPanelUi.cs`, `ChestFilter.cs`, `ChestSort.cs`) |
| GitHub | https://github.com/sonicdm/SonicChestFilters |

## Valheim references (local only)

Build needs Valheim/BepInEx managed DLLs. Default path:

```text
E:\Scripts\Valheim Mods\Reqs
```

Refresh that folder from:

- Valheim Managed: `\\allan-pc\h\Games\Valheim Server\Docker\data\server\valheim_server_Data\Managed`
- BepInEx/Harmony: `%AppData%\r2modmanPlus-local\Valheim\profiles\Default\BepInEx\core`
- Jötunn: `%AppData%\r2modmanPlus-local\Valheim\profiles\Default\BepInEx\plugins\ValheimModding-Jotunn\Jotunn.dll`

Override with `-LibDir` on `build.ps1` / `package.ps1` / `release.ps1`.

- **Never** commit those DLLs or the Reqs folder.
- **Never** expect GitHub-hosted Actions to compile this mod.
- GitHub Actions only refreshes **release notes** from `CHANGELOG.md` on tag push (`.github/workflows/release.yml`). The Thunderstore zip is attached by the local `release.ps1`.

## Build / package (session habits)

```powershell
.\build.ps1                          # DLL → bin\Release\ (+ copy to dist\SonicChestFilters.dll)
.\package.ps1                        # Thunderstore zip → dist\SonicChestFilters-<ver>.zip
.\package.ps1 -SkipBuild             # zip from existing Release DLL
.\build.ps1 -Package                 # build then package
```

During an active coding session:

- After code changes, always verify with **`.\build.ps1`** (expect 0 errors) before considering the change done.
- Prefer **build only** when iterating — do **not** run `package.ps1` / create a new versioned zip unless the user asks, or right before a release.

`package.ps1` asserts `manifest.json` `version_number` == `PluginVersion` in source == csproj `<Version>`.

## Version bumps

When shipping a new version, update **all** of these to the same `MAJOR.MINOR.PATCH`:

1. `src/SonicChestFilters.cs` → `PluginVersion`
2. `SonicChestFilters.csproj` → `<Version>`, `<AssemblyVersion>`, `<FileVersion>`
3. `manifest.json` → `version_number`
4. `README.md` → Version row in the Identity table
5. `CHANGELOG.md` → new `## X.Y.Z` section at the top (this becomes the GitHub release body)

Working tree must be **clean** before `release.ps1` (commit first).

## Release flow (reproduce this)

GitHub-hosted runners cannot build. Releases are cut **locally**:

```text
1. Bump versions + write CHANGELOG ## X.Y.Z
2. Commit and push to main (if needed)
3. .\package.ps1                    # or use an existing zip
4. .\release.ps1 -SkipPackage       # if zip already exists
   # or: .\release.ps1             # package then release
```

What `release.ps1` does:

1. Validates version alignment (manifest / PluginVersion / csproj)
2. Ensures `dist\SonicChestFilters-<ver>.zip` exists
3. Extracts the matching `## <ver>` block from `CHANGELOG.md` for release notes
4. Creates annotated tag `v<ver>`, pushes branch + tag
5. Creates or updates the GitHub Release and uploads the zip

Useful flags:

```powershell
.\release.ps1 -DryRun          # validate + print only
.\release.ps1 -SkipPackage     # use existing dist zip
.\release.ps1 -SkipPush        # local tag only; print push/gh commands
```

Docs-only changes (README wording, etc.): **commit + push `main`**, do **not** cut a new release unless the user asks.

## Install path (local testing)

r2modman profile plugin folder (typical):

```text
%AppData%\r2modmanPlus-local\Valheim\profiles\Default\BepInEx\plugins\SonicDM-SonicChestFilters\
```

**Do not copy the DLL (or anything else) into an r2modman profile.** The user installs mods themselves. After `.\build.ps1`, the DLL is at `bin\Release\SonicChestFilters.dll` and `dist\SonicChestFilters.dll`.

## Code / product notes

- Filter focus uses a `Player.TakeInput` prefix (not `GUIManager.BlockInput`, which spoofs `TextInput.IsVisible` and hides item tooltips).
- UI strings are Jötunn localization tokens (`$sonic_chestfilters_*`).
- Item locate (`locate`) is registered with Jötunn `CommandManager` unless `com.sonicdm.valheim.nearbycraftingforked` is in `Chainloader.PluginInfos`.

## Scripts map

| Script | Role |
| --- | --- |
| `build.ps1` | `dotnet build` against local Reqs |
| `package.ps1` | Thunderstore zip + validation |
| `release.ps1` | Tag + GitHub Release + attach zip |
| `.github/workflows/release.yml` | On `v*` tag: changelog notes only |

## Git

- Default branch: `main`
- Release tags: `vMAJOR.MINOR.PATCH` (must match `manifest.json`)
- Do not commit: `bin/`, `obj/`, `dist/`, `.package/`, Valheim refs, `.cursor/` (except `!.cursor/rules/**` per `.gitignore`)
