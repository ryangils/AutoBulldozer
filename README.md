# Auto Bulldozer — Cities: Skylines II mod

Automatically demolishes abandoned, condemned, and destroyed buildings. Each category is toggleable in Options → Mods → Auto Bulldozer, with a master switch, a configurable grace period for abandoned buildings, an adjustable sweep frequency, and per-category session demolition counters (with a reset button).

## Prerequisites (one-time)

1. Windows with Cities: Skylines II installed.
2. Visual Studio 2022 (free Community edition works) with the ".NET desktop development" workload.
3. Install the CS2 modding toolchain: launch the game, go to **Options → Modding** (or via the Paradox launcher's game settings) and download/install all modding toolsets. This sets the `CSII_TOOLPATH` environment variable that this project's `.csproj` relies on. Restart Visual Studio afterwards so it picks up the variable.

## Build

Either run `.\build.ps1` from the project folder (needs only the .NET SDK — no Visual Studio; the script sets `DOTNET_ROLL_FORWARD=LatestMajor` because the toolchain's post-processor targets .NET 6), or:

1. Open `AutoBulldozer.sln` in Visual Studio.
2. Set configuration to **Release** and build (Ctrl+Shift+B).

Either way, the toolchain automatically copies the built mod to `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\AutoBulldozer\`.

If the build fails with missing references or import errors, `CSII_TOOLPATH` isn't set — redo step 3 of the prerequisites.

## Test in-game

1. Launch CS2 and load a city (or start one).
2. Check **Options → Mods → Auto Bulldozer** — you should see the toggles.
3. Let the simulation run. Abandoned buildings are cleared a few times per in-game day. The log is at `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\AutoBulldozer.log`.

## Publish to Paradox Mods (PDX Mods)

1. Add a thumbnail: place a square PNG (at least 256×256) at `Properties/Thumbnail.png`. Optionally add screenshots and reference them in `Properties/PublishConfiguration.xml`.
2. Review `Properties/PublishConfiguration.xml` — display name, descriptions, tags, version.
3. In Visual Studio, right-click the **AutoBulldozer** project → the toolchain adds publish options (**Publish New Mod**). You'll be asked to log in with your Paradox account the first time.
4. After the first publish, the toolchain writes your `ModId` into `PublishConfiguration.xml`. Keep it — future updates use **Publish New Version** (bump `ModVersion` and update `ChangeLog` first).
5. Your mod appears at https://mods.paradoxplaza.com/games/cities_skylines_2 under your Paradox account.

## How it works

`AutoBulldozerSystem` is an ECS system that runs during the simulation phase (1–64 sweeps per in-game day, configurable, default 16; no cost while paused or in menus). It queries buildings tagged `Abandoned`, `Condemned`, or `Destroyed` (excluding temporary tool previews and already-deleted entities) and adds the game's `Deleted` component, letting the vanilla deletion pipeline tear them down cleanly — sub-buildings, renters, and network connections included. Because it only adds a vanilla component, it's save-safe and can be added or removed from a save at any time.

**Destroyed buildings** are handled more conservatively: only zoned growables (residential/commercial/industrial/office, identified by `SpawnableBuildingData` on their prefab) are cleared, since the game regrows a building on the freed zone cells. Service buildings, signature buildings, and anything the player placed by hand are skipped, so the option can never delete a service the player would otherwise rebuild.

The optional **grace period** delays demolition of abandoned buildings by N in-game days, read from the game's own `Abandoned.m_AbandonmentTime` timestamp — so it's exact and survives saving/loading. Note that vanilla CS2 (`DestroyAbandonedSystem`) eventually collapses long-abandoned buildings on its own, so a very long grace period may see the building collapse into rubble before the mod demolishes it.

## Files

- `Mod.cs` — entry point (`IMod`): loads settings, registers options UI and locale, schedules the system
- `Setting.cs` — options UI definition and persisted settings
- `LocaleEN.cs` — English strings for the options UI
- `AutoBulldozerSystem.cs` — the actual demolition logic
- `Properties/PublishConfiguration.xml` — PDX Mods listing metadata
