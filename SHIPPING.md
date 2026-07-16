# Shipping Auto Bulldozer to Paradox Mods — detailed guide

> **Superseded (2026-07-16): see [PUBLISHING.md](PUBLISHING.md) for the workflow that
> actually works.** The Visual Studio "Publish" steps below don't function in this project
> (no publish profiles exist — VS errors with "ModPublisherCommand is not set"), and the
> credentials-file step (Step 1) was never used and shouldn't be: ModPublisher auto-logs-in
> with a cached Paradox session after one interactive login. The asset-preparation and
> troubleshooting sections below are still accurate.

## Step 0: One-time account setup

1. Create a Paradox account at paradoxplaza.com if you don't have one (top-right → Log in → Create Account). This is the account your mod will be published under.
2. Optional but recommended: create a forum thread for your mod at the [Cities Skylines 2: User Mods forum](https://forum.paradoxplaza.com/forum/forums/cities-skylines-2-user-mods.1170/). Users report bugs and ask questions there. You'll paste its URL into `ForumLink` below.

## Step 1: Create the Paradox account data file

The publish tooling logs into Paradox using a plain text file with your credentials.

1. Create a text file **outside this repo** (so it never ends up in source control), e.g. `C:\Users\richa\Desktop\pdx_account.txt`, containing exactly two lines:

   ```
   YOUR_PARADOX_USERNAME
   YOUR_PARADOX_PASSWORD
   ```

2. In Visual Studio, right-click the **AutoBulldozer** project → **Edit Project File**, and add inside the first `<PropertyGroup>`:

   ```xml
   <PDXAccountDataPath>$(USERPROFILE)\Desktop\pdx_account.txt</PDXAccountDataPath>
   ```

## Step 2: Prepare the listing assets

1. **Thumbnail (required):** square PNG or JPG, at least 256×256 (512×512 looks better). Save as `Properties\Thumbnail.png`. No spaces in the filename — spaced filenames cause the upload error "Couldn't upload all files to the backend".
2. **Screenshots (recommended):** in-game shots (e.g. the options page, a before/after of an abandoned block). Save them in `Properties\` and add one line per image in `PublishConfiguration.xml`:

   ```xml
   <Screenshot Value="Properties/Screenshot1.png" />
   <Screenshot Value="Properties/Screenshot2.png" />
   ```

## Step 3: Finalize PublishConfiguration.xml

Open `Properties\PublishConfiguration.xml` and check each field:

- `ModId` — leave **empty** for the first publish. The publish step returns your ID; you save it here afterwards.
- `DisplayName` — the name shown in the mod browser ("Auto Bulldozer").
- `ShortDescription` — one sentence shown in listings.
- `LongDescription` — Markdown works, but **every line must start at the left margin** (no leading indentation) or formatting breaks on the mod page. Tip: draft it in a `Properties\LongDescription.md` file (not uploaded) to preview the Markdown, then paste it in and Shift+Tab the block to the margin.
- `Tag` — for code mods this must be exactly one tag: `Code Mod`.
- `ForumLink` — your forum thread URL from Step 0.
- `ModVersion` — start at `1.0.0`. Bump on every update.
- `GameVersion` — check the game's current version in the main menu and use a wildcard on the patch, e.g. if the game is 1.3.x use `1.3.*`. If this doesn't match players' game version, the mod page shows a compatibility warning.
- `Dependency` — leave empty (this mod has none).
- `ChangeLog` — required for updates; harmless on first publish. Either a single line (`<ChangeLog Value="..." />`) or a multi-line `<ChangeLog>...</ChangeLog>` block (same left-margin rule as LongDescription).
- `ExternalLink` — optional: GitHub repo, Discord, YouTube, PayPal.

## Step 4: Publish (first time)

1. **Exit Cities: Skylines II completely** — the build fails with "Access to the path ... denied" if the game holds the mod DLL open.
2. Build **Release** and verify the mod works in-game one last time (then exit again).
3. Right-click the **AutoBulldozer** project → **Publish**.
4. Leave the action as **PublishNewMod** and click **Publish**.
5. Watch the Output window: on success it prints your **Mod ID** (5–6 digits). **Copy it immediately** and paste it into `PublishConfiguration.xml`:

   ```xml
   <ModId Value="123456" />
   ```

   Without it you can't push updates.

   (No Publish menu? You can also run `dotnet publish` from the project folder — the toolchain's Mod.targets drives the upload.)
6. Verify the listing at [mods.paradoxplaza.com](https://mods.paradoxplaza.com/games/cities_skylines_2) (search "Auto Bulldozer"). New mods can take a little while to appear in search.
7. **Delete your local copy** from `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\AutoBulldozer\`, then subscribe to your own mod in-game. Otherwise the local build always shadows the published one and you're never testing what players get.

## Step 5: Publishing updates

1. Make and test your changes locally (local copy in the Mods folder overrides the subscribed one while it exists — handy for testing, delete it when done).
2. In `PublishConfiguration.xml`: bump `ModVersion`, update `ChangeLog` (mandatory for updates — publish fails with "ChangeLog must be set" otherwise), update `GameVersion` if the game patched.
3. Confirm `ModId` is set.
4. Right-click project → **Publish** → select **PublishNewVersion** → Publish.
   - CLI alternative: `dotnet publish /p:ModPublisherCommand=NewVersion`
   - To change only the listing (description, images) without a new binary, use the **UpdateModDetails** action instead.

## Common errors

- **"Unable to remove directory ... Access denied"** — the game is running. Exit to desktop, rebuild.
- **"PDX account data file is not found"** — `PDXAccountDataPath` wrong or file missing (Step 1).
- **"ChangeLog must be set in configuration"** — add a ChangeLog value (updates only).
- **"Couldn't upload all files to the backend"** — an image filename contains spaces.
- **Compatibility warning on the mod page** — `GameVersion` doesn't match the current game version; test, update the value, publish a new version.

## Maintenance expectations

Game patches (especially big ones) can break code mods. After each CS2 update: test the mod, bump `GameVersion`, republish. If a patch changes the `Abandoned`/`Condemned`/`Destroyed` components or the deletion pipeline, the mod may need code changes — the log file at `AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\AutoBulldozer.log` is your first stop.
