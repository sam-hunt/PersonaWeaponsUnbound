# Persona Weapons Unbound - Design Document

## Background

RimWorld's Royalty DLC introduced **bladelink weapons** (player-facing: **persona weapons**) — the persona monosword, persona plasmasword, and persona zeushammer. Each carries an onboard AI persona with 1–2 **weapon traits** (psychic sensitivity shifts, mood links, kill drives, pain suppression, and so on) and bonds permanently to the first pawn who equips it. In vanilla, persona weapons are only obtainable from Empire traders, quest rewards, and relics — and you're stuck with whatever traits they rolled.

This mod lets players customize them: add and remove persona traits, rename the persona, and convert base weapons to and from their persona variants at a fabrication bench.

**A note on language:** _Bladelink_ and _persona_ are largely interchangeable. Bladelink is ubiquitous in code (`CompBladelinkWeapon`, the `BladeLink` weapon category); persona is the player-facing adjective (persona monosword, persona core). We use "persona" in player-facing text and "bladelink" freely in code and internal docs.

### Scope: Royalty Persona Weapons Only

This mod targets **Royalty bladelink weapons** exclusively. Odyssey's unique weapons (`CompUniqueWeapon`) are categorically excluded — they are served by our sibling mod **Unique Weapons Unbound** (UWU), which this project was forked from.

**Why a separate mod:** UWU hard-depends on Odyssey. Supporting persona weapons inside UWU would lock out players who own Royalty but not Odyssey. The two comps are also different enough (bonding, generated names, no art comp, no ability wiring) that most of UWU's Odyssey machinery is dead weight for personas — and the fiction differs: Odyssey traits are physical modifications a crafter makes; bladelink traits are properties of an AI personality that must be _reprogrammed_.

**Technical note:** Both systems share the `WeaponTraitDef` class, discriminated by `weaponCategory`: bladelink traits use the single Royalty category `BladeLink`; Odyssey traits use its 15 disjoint categories. `CompBladelinkWeapon.CanAddTrait` hardcodes `weaponCategory == WeaponCategoryDefOf.BladeLink`, so that check is authoritative. Our trait filtering is **only-bladelink** (UWU's is not-bladelink), so both mods can coexist — even on a hypothetical modded weapon carrying both comps — without conflict.

### Trait System Constraints (Vanilla Bladelink)

- Vanilla generation rolls **1–2 traits** per persona weapon (`TraitsRange`), weighted by `commonality`.
- Traits with overlapping `exclusionTags` cannot coexist (e.g. the four psychic-sensitivity variants).
- Bonding: persona weapons biocode-on-equip to their first wielder and apply per-trait bonded hediffs/thoughts. The `NeverBond` ("freewielder") trait disables bonding entirely.
- No vanilla bladelink trait grants abilities or forces a color, and none set `canGenerateAlone=false` — but all those fields exist on the shared def, so modded traits may use them and we honor them.

---

## Core Features

### 1. Add Traits

- Add traits to a **base monosword/plasmasword/zeushammer**, converting it into its persona variant. The first trait costs an **AI persona core** — the persona is physically installed.
- Add traits to **existing persona weapons** (up to the trait limit).

### 2. Remove Traits

- Remove individual traits from persona weapons.
- Removing the **last trait** reverts the weapon to its base variant and **refunds the persona core**.

### 3. Trait Validation

Player-initiated changes respect the rules vanilla enforces for generated persona weapons:

- Maximum trait count (2, from the vanilla range).
- `exclusionTags` conflict rules.
- Only `BladeLink`-category traits are offered.
- Bond side effects handled correctly: bonded hediffs applied/removed with their trait; adding freewielder severs an existing bond (with warning); reverting a bonded weapon to base severs the bond (with confirm warning).

### 4. Rename

- **Rename** the persona using vanilla's `NamerWeaponBladelink` grammar (or type a custom name). Relic names stay locked per Ideology rules.

---

## The Reprogramming Cost Model

Bladelink traits are facets of the persona's personality, not bolt-on parts. Changing them means forcefully reprogramming the persona with advanced computer components that burn out in the process:

- **First trait / last trait** (def conversion): costs / refunds exactly **1 AI persona core**.
- **Every other change** (add _or_ remove): costs **advanced components**. Both directions are costs — reprogramming is destructive; there are no refunds.
- The component count scales with weapon quality above a configurable threshold:
  `N = base + levelsAboveThreshold × perLevel` (all three knobs are mod settings; the settings page shows a live table of the resulting cost per quality level).
- **Memory wipe** (bond or kill tracker, at most one per customization): a flat advanced-component cost set by a plain mod-setting slider — no quality scaling, and never refunded, same as every other change.

Defaults: base 2, threshold Normal, +1/level → Normal-quality changes cost 2 advanced components; a Legendary weapon costs 6 per change. Memory wipes default to 3 components (bond) / 1 component (kill tracker).

---

## Research Gating

A single ultratech research project: **Bladelink Customization** (`PWU_BladelinkCustomization`).

| Property     | Value                                   |
| ------------ | --------------------------------------- |
| Prerequisite | Advanced fabrication                    |
| Bench        | Hi-tech research bench + multi-analyzer |
| Cost         | 4000                                    |
| Techprint    | 1 × Empire-sourced (market value 2000)  |

The required techprint count is a mod setting (0–3, default 1), applied live to the def — 0 removes the requirement entirely. The research also gates the crafting recipes below.

---

## Crafting Recipes

Persona weapons' base variants aren't craftable in vanilla. With Bladelink Customization researched, the fabrication bench offers (each individually toggleable in settings):

| Product     | Ingredients                                    |
| ----------- | ---------------------------------------------- |
| Monosword   | 140 plasteel, 4 advanced components            |
| Plasmasword | 100 plasteel, 6 advanced components            |
| Zeushammer  | 80 plasteel, 20 uranium, 8 advanced components |

Craft the base weapon, then install a persona core through customization — a fully in-colony path to a bespoke persona weapon.

---

## Workbench Integration

Customization happens at the **fabrication bench**, statically — persona weapons are all ultratech, so there is no workbench tier system. Entry points (unchanged from UWU):

1. **Weapon gizmo** — select a weapon, click Customize persona, pick a colonist.
2. **Workbench right-click** — colonist selected, right-click a fabrication bench, pick an equipped/carried weapon.
3. **Ground weapon right-click** — sends a colonist to the nearest operational fabrication bench.

Blocker ordering (hidden → disabled-with-reason → enabled) is preserved, minus the tier and craftability checks.

---

## Weapon Customization Dialog

Same styling-station-inspired dialog as UWU:

- **Weapon preview** — live, tint-faithful (never-spawned preview Thing). Two vanilla "i" info-card buttons — one beside the dialog title for the weapon as it stands, one beside the preview for the weapon as staged — let the player compare full before/after stats without leaving the dialog.
- **Traits tab** — only-bladelink traits, search, discovery-progression filtering, per-trait cost display.
- **Memory tab** — a one-time wipe of the persona's bond or kill tracker (three-way radio: no wipe / wipe bonding / wipe kill tracker), costed in advanced components.
- **Texture tab** (conditional) — a "< [Part] >" selector over a grid of rendered thumbnails, one per declared variant of the selected graphic part. Unlike UWU's texture tab, this isn't driven by vanilla `Graphic_Random` variants (persona weapons render via `Graphic_Single`, so there are none) — it surfaces **VEF's subtexture composition system** (`VEF.Graphics.CompGraphicCustomization`), which composes a weapon's appearance from a per-part variant pick. Shown only when the setting is on, the reflected VEF UI surface resolved, and the resulting def actually declares a part catalog. Restyling is always free — it stages an `OpType.Restyle` and gets no cost chip.
- **Naming** — auto-generate via the bladelink namer or type your own.
- **Confirm** — builds a job spec; each change is a separate work op; resources consumed on completion; interruption is non-destructive. If the completed job re-equips the weapon onto its wielder, vanilla's persona-bond confirmation pops first whenever that equip would newly bond it — exactly as a manual equip order would.

### Trait Discovery Progression (optional setting)

When enabled, only traits present on persona weapons the player has _seen_ (on any map, caravan, or hostile pawn) are offered — hostile-held sources show but can't be added until captured. No persistent save state.

---

## Mod Compatibility Goals

- **Automatic support** for modded persona weapons: any ThingDef with `CompBladelinkWeapon` participates; base↔persona pairing via `descriptionHyperlinks` or the `*Bladelink` defName suffix (case-insensitive — vanilla itself has a `Zeushammer`/`ZeusHammerBladelink` casing quirk); custom `nameMaker`s respected.
- **Automatic support** for modded bladelink traits: anything with `weaponCategory == BladeLink`, including `exclusionTags`, `canGenerateAlone` (opt-in enforcement setting), and bonded/equipped hediffs.
- **Automatic support** for VEF-composited persona weapons: detection keys on the reflected `VEF.Graphics.CompGraphicCustomization` type, never on a packageId. The integration is named after Vanilla Persona Weapons Expanded because that's what motivated it, but VPWE contributes nothing of its own — it just attaches VEF's comp to the Royalty persona weapons. Any mod using the same VEF system participates out of the box: Vanilla Races Expanded — Archons' Archoblade, for instance, gets skin preservation across def conversion and brings up the texture tab with no VPWE installed. The reflection surface is split into two gates so a drift in the (larger, more speculative) UI surface can disable the texture tab without taking skin preservation down with it.
- **Coexistence with UWU**: disjoint identifiers everywhere (packageId, Harmony ID, assembly, namespaces, defNames, localization keys) and complementary trait filters.
- No hard-coded def references for detection; the three vanilla def pairs resolve through the same dynamic pairing as modded ones.

See `Docs/Specs/PERSONA_FORK.md` for the implementation-level specification and `Docs/Research/BLADELINK_WEAPONS.md` for the decompilation research this design is grounded in.
