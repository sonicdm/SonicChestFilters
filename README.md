# Sonic Chest Filters

Valheim BepInEx mod for chest filter controls on nearby containers.

## Identity

| | |
| --- | --- |
| Author | SonicDM |
| Plugin name | `Sonic Chest Filters` |
| Plugin GUID | `com.sonicdm.valheim.sonicchestfilters` |
| Assembly | `SonicChestFilters.dll` |
| Version | `1.0.1` |
| Config file | `BepInEx/config/com.sonicdm.valheim.sonicchestfilters.cfg` |

## Features

- **Filter:** a live Jötunn Valheim-styled text box on the open chest. Occupied slots that do not match the query are hidden; empty slots stay usable for deposits. Matching uses `*` wildcards and substring search on localized names, tokens, and prefabs.
- **Clear Filter:** shows every slot again and clears the box.
- **Sort:** one button. Merges compatible stacks, then orders items by type, localized name, and quality. Click again to reverse. Requires chest ownership; does not sort while an item is being dragged.
- **Find items:** console/chat command `locate` glows nearby eligible chests (same matching rules as the filter). Examples: `locate Resin`, `locate` (held item), `locate clear`.

When **Nearby Crafting Forked** is installed, this mod’s `locate` command is not registered. Use NCF’s `nearby` / `locate` instead. Filter and Sort still work.

Filter is visual-only and resets when the chest is closed. Sort writes the new layout into the container inventory.

## Configuration

The configuration file is generated at:

```text
BepInEx/config/com.sonicdm.valheim.sonicchestfilters.cfg
```

Settings can also be changed through a compatible mod-config interface (for example Official BepInEx Configuration Manager).

Config groups:

- `[General]` Enabled, AutoReloadConfig, DebugLogging
- `[Containers]` range and eligibility used by item locate
- `[UI]` EnableFilterBox, EnableSortButton
- `[Item Locate]` Enabled, MaxHighlights, DurationSeconds, GlowColor (`Enabled` is ignored while Nearby Crafting Forked is loaded)

## Installation

### Mod manager (r2modman / Thunderstore)

1. Install **Jötunn** (and BepInExPack) then **Sonic Chest Filters**, or import the local package into the profile.
2. Launch once so the config file is created.

Recommended r2modman plugin layout (same pattern as other managed mods):

```text
BepInEx/plugins/SonicDM-SonicChestFilters/
  SonicChestFilters.dll
  manifest.json
  README.md
  CHANGELOG.md
  icon.png
```

### Manual

1. Install BepInExPack for Valheim.
2. Place `SonicChestFilters.dll` in `BepInEx/plugins/` (or a subfolder as above).

## Credits

- Author: **SonicDM**

## Build (developers)

Build against a local folder of Valheim / BepInEx managed DLLs. Pass that folder with `-LibDir` (do not commit those refs).

```powershell
.\build.ps1 -LibDir "path\to\ValheimRefs"
.\build.ps1 -LibDir "path\to\ValheimRefs" -Package
.\package.ps1 -LibDir "path\to\ValheimRefs"
.\package.ps1 -SkipBuild
```

`package.ps1` builds a Thunderstore-compatible zip under `dist/` per [Thunderstore package docs](https://thunderstore.io/package/create/docs/):

- `manifest.json`, `README.md`, `icon.png` (256×256), optional `CHANGELOG.md`, and `SonicChestFilters.dll` at the **zip root**
- Validates manifest fields, UTF-8 readme, and icon dimensions before packing

## Release (developers)

GitHub-hosted Actions **cannot** compile this mod (Valheim `Managed` DLLs stay local). Releases are cut from your machine:

1. Bump `PluginVersion`, csproj `<Version>`, and `manifest.json` `version_number` together.
2. Add a `## X.Y.Z` section to `CHANGELOG.md`.
3. Package locally (`.\package.ps1`), then publish:

```powershell
.\release.ps1 -SkipPackage   # use existing dist\SonicChestFilters-X.Y.Z.zip
# or
.\release.ps1                # package then release
```

`release.ps1` validates versions, tags `vX.Y.Z`, pushes, and creates a GitHub Release whose body is the matching CHANGELOG section, with the Thunderstore zip attached. The `Release` workflow on tag push refreshes those notes if the tag lands first.
