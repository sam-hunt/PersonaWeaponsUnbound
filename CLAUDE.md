# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Persona Weapons Unbound** (PWU) is a RimWorld 1.6 mod that allows players to customize Royalty's bladelink/persona weapons — add/remove persona traits, rename, and convert base weapons to/from their persona variants. Requires the Harmony mod and the Royalty DLC.

This repo is a fork of **Unique Weapons Unbound** (UWU, Odyssey unique weapons), carrying its git history. Design: `Docs/DESIGN.md`. Conversion spec: `Docs/Specs/PERSONA_FORK.md`. Bladelink internals research: `Docs/Research/BLADELINK_WEAPONS.md`.

## Build Commands

```bash
# Build the mod (outputs to 1.6/Assemblies/ AND atomically redeploys to the RimWorld Mods folder)
dotnet build PersonaWeaponsUnbound.sln -c Release

# Build only the main project (also triggers the deploy)
dotnet build Source/1.6/PersonaWeaponsUnbound.csproj

# Clean build artifacts
dotnet clean PersonaWeaponsUnbound.sln

# Override RimWorld install path
RIMWORLD_PATH="/path/to/RimWorld" dotnet build PersonaWeaponsUnbound.sln -c Release
# Or: dotnet build -p:RimWorldPath="/path/to/RimWorld"
```

The build system auto-detects the RimWorld installation path on Windows/Linux/Mac (including WSL targeting a Windows install). For CI builds without RimWorld installed, it falls back to the `Krafs.Rimworld.Ref` NuGet package. For local development and api inspection (monodis, ilspycmd etc), the local installation should be preferred as the source of truth.

### Deployment

Every local build auto-deploys into the RimWorld `Mods/` folder (when a local install is detected) — no separate clean/copy step. The `StageMod` target in `Source/1.6/PersonaWeaponsUnbound.csproj` is the **single source of truth** for what ships: it wipes the target dir and recopies a whitelist of runtime file types, so deleted/renamed files never linger. To change what ships, edit its `_ModFiles` ItemGroup. CI (`.github/workflows/release.yml`) and the local Stop hook both reuse this target, so the release zip can't drift from the local deploy.

A gitignored Stop hook (`.claude/hooks/sync-mod.sh`) rebuilds + redeploys after any turn that touched mod source/content.

**WSL Setup:** Requires `RIMWORLD_PATH` env var in `~/.bashrc` pointing to the Windows RimWorld install (e.g., `/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld`).

### Tests

xUnit suite under `Tests/1.6/` (a separate project, never shipped). Run natively with `dotnet test Tests/1.6/PersonaWeaponsUnbound.Tests.csproj` — vstest hosts the net472 suite through Mono on Linux/WSL (requires `mono` on PATH). Debug builds don't redeploy (`DeployToModFolder` is Release-gated). CI builds the suite but can't run it (tests need a live RimWorld install; the Krafs ref assemblies are compile-only).

## Architecture

### Entry Point

`Source/1.6/Core/ModInitializer.cs` - Static constructor with `[StaticConstructorOnStartup]` auto-patches via Harmony attribute discovery. Harmony ID: `shunter.personaweaponsunbound` (must never collide with the UWU sibling mod's Harmony ID — the two mods are designed to coexist).

### Key Patterns

**Harmony Patching:** Attribute-discovered patches (`[HarmonyPatch]`) plus one manually registered patch (the VEF `CompGraphicCustomization.CompFloatMenuOptions` postfix in `ModInitializer`). Patches are organized by target class in subdirectories under `Source/1.6/`.

**Patch-timing hazard (other mods' methods):** applying a Harmony detour JIT-compiles the target method, which runs its declaring type's static ctor — done before defs load, a target cctor that resolves defs breaks permanently (the BetterTradersGuild v1.1.0 CWTL incident). This repo patches from `ModInitializer`'s `[StaticConstructorOnStartup]` ctor (post-defs), which guards the VEF foreign-target patch; that placement is load-bearing — never move `PatchAll()` or the manual `Patch()` onto the `Mod` constructor path. Worked example of deferring foreign-target patches when ctor-time patching is required: BetterTradersGuild's `Core/DeferredModPatches.cs`.

**Namespace Convention:** Use `*Patches` suffix for patch namespaces to avoid RimWorld type conflicts.

**Serialized Fields:** Use camelCase for fields serialized via `Scribe_Values.Look` to match save file XML element names (per .editorconfig). PascalCase for all other public members.

**Settings Triple Invariant (`PWU_Settings.cs`):** Every settings field must appear in three places with matching defaults: (1) field declaration, (2) `ResetToDefaults()`, (3) `ExposeData()`'s `Scribe_Values.Look` default. Missing a spot fails silently — drops from save, skips reset, or drifts from declared default. All three lists are kept in the UI's display order (the section ordering from `PWU_Mod.DoSettingsWindowContents`) with section comments, so a diff across the three blocks lines up row-for-row. When adding/removing/renaming a setting, update all three and slot it into its UI section.

**No em-dashes in player-facing strings:** Never use `—` or `–` in text the player sees: values in `1.6/Languages/**` (every language, not just English) and `<label>`/`<description>`/`<jobString>` in `1.6/Defs/**`. Use a colon, comma, or separate sentence. Code comments, `<!-- EN: -->` mirrors, and `Log.*` strings are exempt.

**Shared l10n toolkit (`l10n/` submodule):** the family-wide translation process, per-language mechanics references, cross-language lessons, Workshop conventions, and the checker/refresh script engines live in the `rimworld-l10n` repo, consumed here as the `l10n/` git submodule (canonical working checkout: `~/dev/rimworld-l10n`). `Scripts/check-translations.py` and `Scripts/refresh-translation-expectations.py` are thin per-repo config shims over its engines. If `l10n/` is empty, run `git submodule update --init`. Never edit `l10n/` in place here: mod-independent learnings go upstream in the canonical checkout, then the pin is bumped in each consuming repo; mod-specific learnings go in this repo's `translate` skill/glossary.

**Editing an English string breaks every translation:** `Scripts/check-translations.py` hard-errors STALE when a language's `<!-- EN: -->` comment no longer matches the English source verbatim. Change an English value and you must update that key's EN comment in all 8 language folders in the same commit. Run the script before committing.

**Workshop title coupling:** each language's `PWU_SettingsCategory` Keyed value is the localized Steam Workshop title and must equal the title line (line 1) of `.steamworkshop/Description/<Language>.txt` — always change the two together (English keeps `Persona Weapons Unbound` in both).

**The DefInjected expected set is a probe dump, not the def XML:** `Scripts/expected-injections.json` is a checked-in dump of every injection point the *live* game sees for this mod — including vanilla-inherited fields and C#-default comp strings that never appear in this repo's XML (this mod's defs are self-contained today, but that is reproved each regen, not assumed) — produced by `Scripts/refresh-translation-expectations.py` driving the L10nProbe dev mod (source lives at `l10n/probe/`; build/deploy it only from the canonical `~/dev/rimworld-l10n` checkout — a submodule copy refuses to deploy by design) through the game's own walker. The checker refuses to run against stale expectations (any defName in `1.6/Defs/` the sidecar has never seen, or label/description text that drifted), so new content forces a regen and the regen sees everything the game sees; the `/release` skill regenerates every release, which also covers vanilla updates changing inherited text under unchanged defNames.

**No XML-doc comments:** We don't use `///` XML-doc style comments (`<summary>`, `<param>`, etc.) anywhere in this codebase — no tooling here consumes them. Use plain `//` comments instead.

**Label casing (vanilla convention):** thing/trait/def labels placed mid-sentence in player-facing text use the lowercase form (`LabelShort`, `.label`) — vanilla renders "Pick up monosword x1", never "Pick up Monosword x1"; named persona weapons keep their proper-noun capitalization either way since `LabelShort` doesn't lowercase, it just stops force-capitalizing. Keyed strings carry their own sentence-start capital; where a `{0}` placeholder can begin the sentence (bail messages, some translations reorder it there), `CapitalizeFirst()` the composed string instead of capitalizing the argument. `LabelCap`/`LabelShortCap` is for standalone display (list rows, name fields) and proper nouns (pawns, precepts).

**No `?.`/`??` on Unity objects:** Never use null propagation or null coalescing on receivers deriving from `UnityEngine.Object` (`Material`, `Texture`, `RenderTexture`, `GameObject`, ...). Unity overloads `==` so destroyed objects compare equal to null; `?.` bypasses the overload with a raw reference check and then throws `MissingReferenceException` on the member access. Use explicit `== null`/`!= null` guards for those types. Verse types (`Thing`, `Pawn`, `ThingComp`, defs) are plain classes where `?.` is fine. Enforced at build time by UNT0007/UNT0008 (Microsoft.Unity.Analyzers). Corollary: never bulk-apply Roslynator's RCS1146 (use conditional access) fixer to Unity-typed receivers; see the note in `.editorconfig`.

## Debugging

1. **Enable RimWorld Dev Mode:** Settings → Dev Mode → Logging
2. **Log locations:**
   - **Windows:** `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`
   - **WSL:** `/mnt/c/Users/*/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`
   - **Linux (Steam):** `~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`
3. **Logging:** Use `Log.Message("[Persona Weapons Unbound] ...")` for mod-specific logs
4. **Inspect RimWorld API:** `monodis "/mnt/c/.../RimWorldWin64_Data/Managed/Assembly-CSharp.dll"`
