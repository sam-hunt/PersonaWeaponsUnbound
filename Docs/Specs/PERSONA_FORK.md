# Persona Weapons Unbound — Fork Conversion Spec

Status: **Final** — grounded in the codebase map and RimWorld 1.6 decompilation (see `Docs/Research/BLADELINK_WEAPONS.md`).
Superseded: the §9 color verdict (D3, "Color tab: KEPT") is reversed by `MEMORY_AND_POLISH.md` §2 — recoloring is removed entirely.
Scope: the full UWU → PWU conversion. This document is the source of truth for the implementation orchestration.

---

## 1. Summary

Fork Unique Weapons Unbound (Odyssey unique weapons) into **Persona Weapons Unbound** (Royalty bladelink/persona weapons). Same interaction skeleton — workbench customization dialog, job-driven application, haul planning, trait discovery progression — retargeted at `CompBladelinkWeapon`, with a radically simpler cost model themed as persona reprogramming.

**Fiction:** Bladelink traits are properties of the weapon persona's personality. Installing an AI persona core turns a base weapon into a persona weapon (and removing the last trait refunds it). Every other trait change is a forceful reprogramming of the persona via advanced computer components that burn out after use — so additions _and_ removals both cost, never refund.

### Goals

- Royalty-only hard dependency. Players with Royalty but not Odyssey are fully served.
- Zero references to Odyssey-gated code or defs; no player-facing references to "unique weapons".
- Clean coexistence with UWU when both mods are enabled (disjoint packageId, Harmony ID, assembly, namespaces, defNames, localization keys; complementary trait filtering: UWU = not-bladelink, PWU = only-bladelink).

### Non-goals

- No dynamic cost rule pipeline, no modder extension API (delete `MODDERS.md`).
- No Alpha Armoury integration. VEF integration is reduced to one kept slice: recipe-inheritance bench equivalence (§8); the VEF trait-graphics integration is deleted.
- No multi-tier research ladder or dynamic workbench tiers.
- No customization of Odyssey unique weapons (that is UWU's job).

---

## 2. Identity & branding renames

| Surface                                                                                    | From                                                                                                                | To                                                                                                         |
| ------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Mod name (About.xml `<name>`)                                                              | Unique Weapons Unbound                                                                                              | Persona Weapons Unbound                                                                                    |
| packageId                                                                                  | `shunter.uniqueweaponsunbound`                                                                                      | `shunter.personaweaponsunbound`                                                                            |
| Harmony ID (`ModInitializer.cs`)                                                           | `shunter.uniqueweaponsunbound`                                                                                      | `shunter.personaweaponsunbound`                                                                            |
| Root C# namespace (66 files)                                                               | `UniqueWeaponsUnbound[.…]`                                                                                          | `PersonaWeaponsUnbound[.…]`                                                                                |
| Assembly / DLL                                                                             | `UniqueWeaponsUnbound.dll`                                                                                          | `PersonaWeaponsUnbound.dll`                                                                                |
| AssemblyInfo (`Title`/`Product`/`InternalsVisibleTo`)                                      | `UniqueWeaponsUnbound(.Tests)`                                                                                      | `PersonaWeaponsUnbound(.Tests)`                                                                            |
| Solution / csproj filenames                                                                | `UniqueWeaponsUnbound.sln`, `Source/1.6/UniqueWeaponsUnbound.csproj`, `Tests/1.6/UniqueWeaponsUnbound.Tests.csproj` | `PersonaWeaponsUnbound.*` equivalents (update sln project paths, Tests `ProjectReference`)                 |
| Class names                                                                                | `UWU_Mod`, `UWU_Settings`, `UWU_ResearchDefOf`, `UWU_JobDefOf`, `UWU_Textures`                                      | `PWU_Mod`, `PWU_Settings`, `PWU_ResearchDefOf`, `PWU_JobDefOf`, `PWU_Textures` (rename files with classes) |
| Class/filename mismatch fix                                                                | class `UniqueWeaponsUnboundMod` in `ModInitializer.cs`                                                              | rename class to `ModInitializer` (match filename)                                                          |
| JobDef                                                                                     | `UWU_CustomizeWeapon`                                                                                               | `PWU_CustomizeWeapon` (file rename too)                                                                    |
| Localization keys + file                                                                   | `UWU_*` in `1.6/Languages/English/Keyed/UWU_UI.xml`                                                                 | `PWU_*` in `PWU_UI.xml` (find/replace across XML + every `.Translate()` call site)                         |
| Texture                                                                                    | `Textures/UI/UWU_Customize.png` + `ContentFinder…Get("UI/UWU_Customize")`                                           | `Textures/UI/PWU_Customize.png` + matching path string                                                     |
| Log prefix (61 call sites)                                                                 | `[Unique Weapons Unbound] `                                                                                         | `[Persona Weapons Unbound] `                                                                               |
| Deploy path (csproj `ModDeployPath`)                                                       | `$(RimWorldPath)/Mods/UniqueWeaponsUnbound`                                                                         | `$(RimWorldPath)/Mods/PersonaWeaponsUnbound`                                                               |
| CI (`.github/workflows/release.yml`)                                                       | sln/csproj paths, zip name `UniqueWeaponsUnbound-*.zip`, staging folder                                             | `PersonaWeaponsUnbound` equivalents                                                                        |
| `Scripts/test-windows.sh`                                                                  | csproj/dll names + banner                                                                                           | `PersonaWeaponsUnbound` equivalents                                                                        |
| `.claude/hooks/sync-mod.sh` (gitignored, local-only)                                       | csproj path + failure banner + stamp filename                                                                       | update in place locally (not committed)                                                                    |
| Docs (`README.md`, `CHANGELOG.md`, About description, `Docs/SteamWorkshopDescription.txt`) | UWU branding/features                                                                                               | PWU branding/features (README badges → new repo URL)                                                       |

**Coexistence guarantee:** after these renames there is no shared symbol, defName, settings file, Harmony ID, or translation key with UWU. The two mods patch disjoint comp types and their trait filters are complementary (§5), so a hypothetical modded weapon carrying _both_ comps is customized independently by each mod without conflict.

`About/PublishedFileId.txt` was already deleted (new Workshop item; file reappears on first upload). New ModIcon/Preview assets are handled by the user — do not recreate them.

---

## 3. Dependencies

`About/About.xml`:

- `modDependencies`: Harmony (keep) + `ludeon.rimworld.royalty` ("RimWorld - Royalty"); **remove** `ludeon.rimworld.odyssey`.
- `loadAfter`: keep royalty/ideology/biotech/anomaly/odyssey (Ideology soft support — relics, ideo colors — is kept).
- After conversion there must be no compile-time references to Odyssey-only types: `CompUniqueWeapon`, `CompProperties_UniqueWeapon`, `RulePackDefOf.NamerUniqueWeapon`. Royalty types (`CompBladelinkWeapon`, `WeaponCategoryDefOf.BladeLink`, `CompGeneratedNames`) are safe to reference unconditionally — Royalty is a hard dependency. (`WeaponCategoryDefOf.BladeLink` is `[MayRequireRoyalty]`; it resolves with Royalty active.)

---

## 4. Weapon model: registry & def conversion

### Vanilla def pairs (Royalty)

| Base (`MeleeUltratech.xml`) | Persona (`MeleeBladelink.xml`)                             |
| --------------------------- | ---------------------------------------------------------- |
| `MeleeWeapon_MonoSword`     | `MeleeWeapon_MonoSwordBladelink` ("persona monosword")     |
| `MeleeWeapon_PlasmaSword`   | `MeleeWeapon_PlasmaSwordBladelink` ("persona plasmasword") |
| `MeleeWeapon_Zeushammer`    | `MeleeWeapon_ZeusHammerBladelink` ("persona zeushammer")   |

⚠ **Casing trap:** base `Zeushammer` vs persona `ZeusHammerBladelink` — a naive `base.defName + "Bladelink"` match fails. Pairing must be case-insensitive.

Persona defs = base def's texture + static pink tint `(255,200,200)`, better melee stats, `MarketValue 3000` (vs 2000), `relicChance 3`, comps: `CompQuality` + `CompProperties_BladelinkWeapon (biocodeOnEquip=true)` + `CompProperties_GeneratedName (nameMaker=NamerWeaponBladelink)`. **No `CompArt`** (base ultratech weapons _do_ have CompArt + plain CompBiocodable). Neither base nor persona is craftable in vanilla.

### Registry (`WeaponRegistry.cs`, adapted)

- Scan for defs with `CompBladelinkWeapon` (replaces `CompUniqueWeapon` scan). `IsPersonaWeapon(ThingDef)` = has bladelink comp.
- Base↔persona pairing, in order: (1) `descriptionHyperlinks` to a non-persona weapon (vanilla defs don't ship hyperlinks, but modded ones may); (2) naming convention: strip trailing `Bladelink` from the persona defName and look up the base **case-insensitively** (handles the ZeusHammer trap). Keep dynamic detection + collision warning diagnostics. Unpaired persona weapons stay customizable minus def conversion (mirrors UWU orphan handling).

### Def conversion (`WeaponDefConversion.cs`, adapted)

Copy set stays: stuff (n/a for these defs, keep for modded), quality (persona generation does **not** self-assign quality, so explicit copy is required and sufficient), hitpoint %, Ideology relic status. **Biocode state is deliberately dropped, not copied** — see Bonding below. Changes:

- **Art:** persona defs have no `CompArt` — `TransferArt` must no-op gracefully when either side lacks the comp. Base→persona drops art (the generated persona name replaces it); persona→base gets a fresh untitled CompArt.
- **`ClearAutoGeneratedUniqueState` → bladelink equivalent:** after `ThingMaker.MakeThing(personaDef)`, `PostPostMake` auto-rolls 1–2 random traits and `CompGeneratedNames.Initialize` rolls a name. Clear them: `comp.TraitsListForReading.Clear()` (live list, **no reflection needed**) and reflection-set `CompGeneratedNames.name` (private field, `Scribe`d as `"name"`).
- **Bonding — inverted, not preserved:** `CompBladelinkWeapon : CompBiocodable`; the bond IS the biocode (`biocoded`/`codedPawn`/`codedPawnLabel`) plus `pawn.equipment.bondedWeapon` back-reference and per-trait `Notify_Bonded` hediffs. Implementation truth: bonding _is_ biocoding. Fiction: the bond belongs to the persona, not the steel. Def conversion always adds or strips the persona core, so biocode state crosses that boundary inverted rather than carried through:
  - **Downgrade (persona→base) must call `UnCode()` first** (public API — clears `bondedWeapon`, fires `Notify_Unbonded` per trait, removes bonded hediffs, clears biocode) before the def swap. Ordering constraint: `UnCode()` (and any unequip) must run while the old Thing's trait list is still intact — vanilla teardown iterates the live list, so clearing or mutating traits first orphans bonded/equipped hediffs on the pawn.
  - **Upgrade (base→persona) must NOT pre-bond**, even if the base weapon already carries a plain biocode (base ultratech weapons have plain `CompBiocodable`, §4 vanilla def pairs) — that biocode is dropped at the swap, not copied. The fresh persona weapon spawns unbound and follows the regular bladelink spawn behavior: bond-on-equip via `biocodeOnEquip`.
- The 0↔1 trait-count boundary remains the conversion trigger, now costed as the persona core (§6).

---

## 5. Trait selection, validation & mutation

### Filter inversion

`TraitValidationUtility`: keep **only** traits where `trait.weaponCategory == WeaponCategoryDefOf.BladeLink` (today those are excluded; this is exactly `CompBladelinkWeapon.CanAddTrait`'s own discriminator, so it is authoritative for mod-added traits too). Delete the Odyssey category-membership layer (`CompProperties_UniqueWeapon.weaponCategories`) — bladelink is a single category.

Vanilla ships 19 bladelink traits (psychic sensitivity ±, bonded/kill thoughts, `NeedKill`, `NoPain`, `SpeedBoost`, `HungerMaker`, `NeuralHeatRecoveryGain`, `PsyfocusMeditationBonus`, `OnKill_PsyfocusGain`, `NeverBond` "freewielder", `Jealous`). None grant abilities; none set `forcedColor`; none set `canGenerateAlone=false`.

### Validation

- **Max trait count:** vanilla bladelink `TraitsRange = IntRange(1, 2)` → default cap is **2** (was 3 for Odyssey). Keep `enforceMaxTraitLimit` setting; derive the cap from the bladelink range rather than hardcoding 3.
- **Conflicts:** keep `exclusionTags` overlap validation (`WeaponTraitDef.Overlaps`, shared machinery — vanilla uses it, e.g. the four psychic-sensitivity traits share tags).
- **`enforceCanGenerateAlone`: KEEP** (handoff decision rule satisfied): the field is on the shared `WeaponTraitDef` abstraction. Vanilla bladelink code never reads it and no Royalty trait sets it false, so it's a no-op by default — but when enabled we honor mod-added bladelink traits' intent. Default stays `false`.

### Trait mutation side effects (`WeaponModificationUtility`, adapted)

`TraitsListForReading` is the live private list — mutate directly, no reflection. But nothing re-fires hooks, so:

- **Add trait:** `traits.Add(def)`; if weapon is bonded → `def.Worker.Notify_Bonded(codedPawn)` (applies `bondedHediffs`). Equipped hediffs are irrelevant mid-job (weapon is carried, not equipped, during customization — the job's unequip already fired `Notify_EquipmentLost` against the full pre-mutation trait list; they re-apply on re-equip via `Notify_Equipped`).
- **Remove trait:** if bonded → `def.Worker.Notify_Unbonded(codedPawn)` before removal (removes `bondedHediffs`). Fire `Notify_EquipmentLost` if somehow equipped. This is mandatory correctness, not hygiene: vanilla's teardown paths (`CompBladelinkWeapon.Notify_EquipmentLost`, `UnCode`) iterate the _current_ trait list, so a trait removed while its hediff is applied orphans that hediff on the pawn permanently (NoPain / SpeedBoost / NeuralHeatRecoveryGain / HungerMaker).
- **`NeedKill` ("kill thirst") added to a bonded weapon:** `lastKillTick` was set at bond time (`OnCodedFor`) and `ThoughtWorker_WeaponTraitKillNeed` fires at `TicksSinceLastKill > 1,200,000` (20 days) — so adding NeedKill to a weapon bonded >20 days ago lands the −4 mood penalty on the next situational recalc (≤100 ticks), with zero grace period. When adding NeedKill, reflection-set `lastKillTick = Find.TickManager.TicksAbs` (private, scribed) to grant the intended 20-day grace (D9).
- **Removing `Jealous` / `OnKill_ThoughtGood` / `OnKill_ThoughtBad`:** their mood effects are _memories_ (`Thought_WeaponTraitNotEquipped` "JealousRage", 1 day; `Thought_WeaponTrait` kill thoughts, 3 days × stack 5) holding a scribed reference to the weapon Thing — nothing removes them when the trait goes away, so the mood effect lingers up to its full duration. On removal, purge the coded pawn's memories of the trait's thought def where `thought.weapon == weapon` (`MemoryThoughtHandler.RemoveMemoriesOfDefIf`) (D10). Def conversion needs no purge: destroying the old Thing flips `Thought_WeaponTrait.ShouldDiscard` via `HasWeapon` and the memory self-culls within ~150 ticks.
- **`NeverBond` (freewielder) special case:** adding it to a _bonded_ weapon flips `Biocodable` to false — call `UnCode()` first (weapon becomes free-wielding). Surface this in the dialog as an informational warning on the trait row.
- **Delete the Verb burst-cache invalidation** (revised from "stays"): `Verb.TicksBetweenBurstShots` / `BurstShotCount` fold trait multipliers from `EquipmentSource.TryGetComp<CompUniqueWeapon>` only — no vanilla consumer reads `burstShot*` off a bladelink comp, so on persona weapons there is no cache to evict and the carried-over scrub is dead code.
- Drop the reflected `ignoreAccuracyMaluses` cache handling — every consumer (`ShotReport`, `VerbProperties`, `VerbTracker`, `AttackTargetFinder`) is `CompUniqueWeapon`-gated. Likewise `statOffsets`, `statFactors`, `damageDefOverride`, `extraDamages`, `additionalStoppingPower`, `forcedColor`, `abilityProps`, `traitAdjectives`, and `canGenerateAlone` are inert on bladelink weapons (all readers gate on `CompUniqueWeapon`).

**Verified-live paths (decomp scouting pass, 2026-07-10 — no invalidation needed):** `equippedStatOffsets` reaches pawn stats via `StatWorker.StatOffsetFromGear`, which reads `CompBladelinkWeapon.TraitsListForReading` live on every stat query; `StatDef.PopulateMutableStats` force-marks every stat referenced by any `WeaponTraitDef.equippedStatOffsets` or hediff stage as non-immutable at startup, so the permanent `immutableStatCache` never captures them (worst case is the 1-tick `Pawn_PsychicEntropyTracker` sensitivity cache). `marketValueOffset` flows through `StatPart_WeaponTraitsMarketValueOffset` (bladelink-specific, live; colony wealth lags ≤5000 ticks via `WealthWatcher` and self-heals). Bonded-thought and kill-thirst situational thoughts recalc from the live trait list every ≤100 ticks — removal self-heals; only the memory thoughts above linger. Rename is applied by `CompGeneratedNames.TransformLabel` _after_ the def-keyed `GenLabel` cache, so it shows immediately everywhere; recolor invalidates fully via `Notify_ColorChanged` (equipped rendering draws live `eq.Graphic`, no cached render node). Melee verb selection self-heals across def conversion (`CompEquippable.VerbsStillUsableBy` membership check + 60-tick refresh; `curMeleeVerb` is guarded on save/load). Unlike Odyssey (deep-scribed `Ability`), **no scribed derived cache exists anywhere on the bladelink path** — every transient cache resets on reload.

### Discovery progression (kept)

`TraitProgressionPool` swaps: `IsUniqueWeapon` → `IsPersonaWeapon`, `CompUniqueWeapon.TraitsListForReading` → `CompBladelinkWeapon.TraitsListForReading`; delete the three Alpha Armoury kit branches. Scan logic (maps, pawns, caravans, hostile/non-hostile buckets) unchanged.

---

## 6. Cost model (replaces the entire TraitCostRule pipeline)

### Rules

1. **First trait added to a base weapon** (base→persona conversion): costs **1 persona core** (`AIPersonaCore`). No component cost for that trait.
2. **Last trait removed from a persona weapon** (persona→base conversion): **refunds 1 persona core**. No component cost for that removal.
3. **Every other trait change** (addition or removal not crossing the 0↔1 boundary): costs **N advanced components** (`ComponentSpacer`). Both directions cost — never refund.
4. Ops in one confirmed spec are costed by **sequential simulation** in the job's op order (removals → cosmetics → additions), so boundary crossings are attributed to the op that actually crosses. E.g. base + add 2 traits = 1 persona core + 1×N components; persona(2) remove both = 1×N components + 1 core refund.

### Per-change component count

```
levelsAboveThreshold = max(0, (int)quality − (int)traitChangeQualitySurchargeThreshold)
N = traitChangeBaseComponentCost + RoundToInt(levelsAboveThreshold × traitChangeQualitySurchargePerLevel)
```

| Setting                                 | UI                                                       | Range           | Default |
| --------------------------------------- | -------------------------------------------------------- | --------------- | ------- |
| `traitChangeBaseComponentCost`          | slider                                                   | 0–10 (int)      | 2       |
| `traitChangeQualitySurchargeThreshold`  | slider (enum, same pattern as existing `minimumQuality`) | Awful–Legendary | Normal  |
| `traitChangeQualitySurchargePerLevel`   | slider                                                   | 0–5 (int)       | 1       |

Naming rationale: every field prices a **trait change** (the per-change advanced-component cost, §6 rule 3) and says so — `Base` is charged regardless of quality; the `QualitySurcharge*` pair defines the extra components above the threshold quality (threshold → where it starts, per-level → how fast it grows). No refund settings exist by design (rule 3: both directions cost).

Defaults yield: Awful/Poor/Normal 2, Good 3, Excellent 4, Masterwork 5, Legendary 6.

### Settings page cost table

Below the three sliders, render a compact table: one row per `QualityCategory` (Awful→Legendary) showing the derived per-trait-change component count, recomputed live from current slider values. No table helper exists in the settings UI — a simple two-column `Listing_Standard`/`Widgets` grid consistent with existing styling.

### Plumbing

- Rewrite `TraitCostUtility` in place as a small static utility, **keeping the name** (it still prices trait changes; "persona" is the mod's category — redundant inside PWU's namespace, and coexistence with UWU is assembly/namespace-scoped so there is no symbol collision): `GetChangeCost(Thing weapon, bool crossesConversionBoundary, bool isRemoval) → List<ThingDefCountClass>`. Output type unchanged, so `CustomizationSpec` per-op `cost`/`refund`, `IngredientReservation.TryReserveIngredientsForJob`, all three haul planners, and the job-driver consumption/ledger code keep working as-is (haul subsystem confirmed fully decoupled).
- **Negative-trait predicate:** UWU detected negatives via a `MarketValue` stat-_factor_ < 1 — bladelink traits don't use stat factors; they use `WeaponTraitDef.marketValueOffset` (e.g. `ThoughtWailing` −1000). Add the additional condition for `marketValueOffset < 0` too. It only drives UI (row tint, hide-negative filter) — no cost effect.
- Refund ledger simplifies: the only refund is the persona core (rule 2), whole, no multipliers/rates.
- Delete: `TraitCostRuleDef.cs`, `Source/1.6/TraitCostRules/` (16 files), `CostRuleHelpers.cs`, `1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml`, `MODDERS.md`, the dialog-local pipeline cache (`Dialog_WeaponCustomization.cs:459-574` region), settings `useRecipeBaseCost`/`traitCostMultiplier`/`traitRefundRate`, and `InitDiagnostics`' cost-rule bucketing. (`TraitCostUtility.cs` is rewritten in place, not deleted — see plumbing above.)

---

## 7. Research & techprint gating

Replace the three-project ladder with a single project (no Royalty conditional patch needed anymore — delete `1.6/Patches/UniqueFabrication_Royalty.xml`; the techprint fields live directly in the def):

```xml
<ResearchProjectDef>
  <defName>PWU_BladelinkCustomization</defName>
  <label>bladelink customization</label>
  <techLevel>Ultra</techLevel>
  <baseCost>4000</baseCost>
  <prerequisites><li>AdvancedFabrication</li></prerequisites>
  <requiredResearchBuilding>HiTechResearchBench</requiredResearchBuilding>
  <requiredResearchFacilities><li>MultiAnalyzer</li></requiredResearchFacilities>
  <techprintCount>1</techprintCount>
  <techprintMarketValue>2000</techprintMarketValue>
  <techprintCommonality>2</techprintCommonality>
  <heldByFactionCategoryTags><li>Empire</li></heldByFactionCategoryTags>
  <!-- researchViewX/Y: place right of AdvancedFabrication -->
</ResearchProjectDef>
```

(Reference points: `AdvancedFabrication` = 4000, no techprint; `JumpPack` = 2000 + 1 techprint @2000 value; `CataphractArmor` = 6000 + 2 techprints @3000. 4000 + 1 Empire techprint sits appropriately.)

- `ResearchProjectDef.ConfigErrors` **requires** `heldByFactionCategoryTags` non-empty when `techprintCount > 0` (and errors on tags with count 0) — XML must always ship count ≥ 1 with the Empire tag.
- **Techprint count setting** (slider 0–3, default 1): the implied `Techprint_PWU_BladelinkCustomization` item def is generated **once at startup** by `ThingDefGenerator_Techprints` for projects with count > 0 — shipping XML count 1 guarantees it always exists. `TechprintRequirementMet` reads the live field, so apply the configured value to the def in `[StaticConstructorOnStartup]` **and** on settings write — **effective immediately, no restart**. At 0 the requirement vanishes and traders stop stocking the print (`TechprintUtility` filters on `TechprintCount > 0`); already-spawned prints linger harmlessly.
- `PWU_ResearchDefOf` exposes the one project. `CustomizationRules` tech-level branching collapses to a single `IsFinished` check. `MainTabWindow_Research_VisibleResearchProjects_Patch` hides the one def when `requireCustomizationResearch` is off.

---

## 8. Workbench: static Fabrication bench (+ VEF equivalents)

- Delete the tier system and the `requireAppropriateWorkbench` setting. **Keep `VEFRecipeInheritanceIntegration`** (reflection shim; compiles and stays silent without VEF): the startup bench scan survives, collapsed from three tiers to one set — `FabricationBench` plus any def whose VEF `RecipeInheritanceExtension.inheritRecipesFrom` reaches it (directly, or through another already-classified bench), e.g. VFE's compact fabrication bench (D11).
- Keep `WorkbenchUtility` as a thin shim so its four call sites (two float-menu providers, gizmo patch, job flow) stay untouched: `IsCustomizationWorkbench(b)` = `b.def` ∈ that fabrication-bench set; `FindBestWorkbench` = nearest reachable, unforbidden, operational such bench; keep `GetWorkbenchOperationalReport` (power check).
- `CustomizationRules.IsCustomizable` drops: tech-level ceiling logic and the recipe-research/craftability chain (`requireRecipeResearch`, `allowUncraftableCustomization` removed — persona weapons are all ultratech; mod installation is treated as intent to customize). Remaining gates: persona-pairable weapon, research finished (if `requireCustomizationResearch`), `minimumQuality`, `allowDefConversion` for base weapons.
- Note: customization does not require _equipping_, so `EquipmentUtility.CanEquip`'s bonded-to-someone-else block doesn't prevent a non-owner colonist from hauling/customizing a bonded weapon. Acceptable; the persona fiction is reprogramming at a bench, not wielding.

---

## 9. Customization dialog

### Tabs

- **Traits tab:** only-bladelink filter (§5); search, progression filtering, hide-negative all kept.
- **Texture tab: DELETED.** Persona weapons use `Graphic_Single` on the base def's texture — no variant-index concept. Remove `desiredTextureIndex` plumbing + `SetTextureIndex` op + tab keys (keep the `overrideGraphicIndex` copy inside def conversion as harmless preservation for modded weapons).
- **Color tab: KEPT** (tint verdict: static def color `(255,200,200)`, but per-thing recolor is fully supported by the engine — `ThingWithComps.DrawColor` checks `CompColorable` first, `GraphicColoredFor` regenerates on `Notify_ColorChanged`, and both ground rendering and `PawnRenderUtility.DrawEquipmentAiming` draw `thing.Graphic`):
  - Add `CompProperties_Colorable` to the three vanilla persona defs via an XML patch (`1.6/Patches/`), targeting the abstract parent `BaseWeapon_Bladelink` if patchable, else the three concrete defs. Modded persona weapons without the comp: color section shows disabled with a reason.
  - Recolor via `thing.SetColor(color)` / `CompColorable` (no reflection; persists in saves; inactive comp falls back to the def tint).
  - Replace the top `UWU_WeaponColors` section with a "Persona weapon colors" section containing a single swatch = Royalty's default persona tint `(255,200,200)`. Keep Ideology + Structure palette sections (settings `enableIdeologyColors`/`enableStructureColors` stay).
  - `WeaponModificationUtility.SetColor` retargets from `CompUniqueWeapon.color` (ColorDef, reflection) to `CompColorable.SetColor` (Color).
  - Trait-forced-color logic: vanilla bladelink traits never set `forcedColor`, but the field is shared — keep the forced-color lock UI for mod-added traits.

### Naming

- Swap `RulePackDefOf.NamerUniqueWeapon` → `DefDatabase<RulePackDef>.GetNamed("NamerWeaponBladelink")` (or the def's own `CompProperties_GeneratedName.nameMaker`, preferred — respects modded persona weapons with custom namers). Root keyword stays `r_weapon_name`; wrap with `GenText.CapitalizeAsTitle` exactly like `CompGeneratedNames.GenerateName(props)` — reuse that static method directly.
- Remove `namerLabels` / trait-adjective / color grammar inputs (the bladelink namer is self-contained: noun+verber / syllables / person-name rules).
- **Name storage:** `CompGeneratedNames.name` — private, no setter; set via reflection (persists — `Scribe_Values.Look(ref name, "name")`). Display comes from `TransformLabel` → `"'Name', monosword"`. Keep relic name-lock (`StyleSourcePrecept` overrides `TransformLabel`, which matches the existing relic behavior).
- Keep the name-regen failure diagnostic, retargeted at the bladelink rule pack.

### Preview

- Keep the never-spawned preview Thing + RenderTexture blit. Delete the `VEFWeaponTraitGraphicsIntegration.RefreshTraitGraphic` calls. Stamp desired traits via `TraitsListForReading` and desired color via `CompColorable` on the preview Thing; the pink tint falls out of `GraphicColoredFor` naturally.
- Bonded-weapon conversion warning: when a spec's final state reverts a _bonded_ persona weapon to base, show a confirm warning in the footer ("bond with {pawn} will be severed").

---

## 10. Crafting recipes (new)

Three `RecipeDef`s modeled on the `Make_ComponentSpacer` pattern (`recipeUsers: FabricationBench` wiring on the recipe side; `researchPrerequisite: PWU_BladelinkCustomization`; `workSkill: Crafting`, `skillRequirements: Crafting 8`; `unfinishedThingDef: UnfinishedWeapon`; `workSpeedStat: GeneralLaborSpeed`; `effectWorking`/`soundWorking` matching vanilla weapon smithing):

Each recipe lives in its own file under `1.6/Defs/RecipeDefs/` (`PWU_Make_MonoSword.xml`, `PWU_Make_PlasmaSword.xml`, `PWU_Make_Zeushammer.xml`).

| defName                | Product                   | Ingredients                                  | workAmount |
| ---------------------- | ------------------------- | -------------------------------------------- | ---------- |
| `PWU_Make_MonoSword`   | `MeleeWeapon_MonoSword`   | 140 plasteel, 4 `ComponentSpacer`            | 75000      |
| `PWU_Make_PlasmaSword` | `MeleeWeapon_PlasmaSword` | 120 plasteel, 5 `ComponentSpacer`            | 75000      |
| `PWU_Make_Zeushammer`  | `MeleeWeapon_Zeushammer`  | 70 plasteel, 30 uranium, 6 `ComponentSpacer` | 75000      |

- **Work-amount rationale (ultratech premium):** 75000 = charge lance (60000, the vanilla craftable ceiling, Spacer tier) × 1.25. These are Ultra-tier weapons — a full tech step above the charge lance — that vanilla makes quest/reward-only. A uniform floor above the top craftable spacer weapon keeps the tier legible. (Vanilla reference points: plasteel longsword 39600, charge rifle 45000, charge lance 60000.)
- **Material cost, tuned to ~break-even at Normal quality:** base weapons all inherit `MarketValue` 2000. Ingredient market values (plasteel 9, uranium 6, `ComponentSpacer` 200) total ≈2060 / 2080 / 2010 for mono/plasma/zeus — a few silver underwater at Normal, positive from Good quality up, before the persona-conversion payoff. Spacer components dominate the bill (~800–1200 each), so the recipes are gated by the scarce resource. Material cost lands ≈1.8× the charge lance's 1140, matching the 75000 vs 60000 work premium.

- **Per-def setting gating** (`enableMonoswordRecipe` etc., default true): Harmony postfix on `RecipeDef.AvailableNow` returning false when the toggle is off — no def surgery, works mid-save. Perf is a non-issue: the property's only callers are UI/event-scoped (bills-tab clipboard check, add-bill menus, quest gen) — never work-scan or tick code. **Accepted behavior:** because the work scan never consults `AvailableNow`, toggling a recipe off hides it from the add-bill menu but does not suspend bills that already exist — they keep producing. The toggle means "stop offering this", not "ban it retroactively".
- Products are the **base** variants; players then convert them to persona weapons through customization (1 persona core for the first trait), completing the fiction loop.
- VEF benches inheriting from `FabricationBench` pick these recipes up automatically — that propagation is the entire function of `RecipeInheritanceExtension`, so no recipe-side work is needed; the kept §8 integration only affects the customization-job bench check.

---

## 11. Settings inventory (`PWU_Settings`)

Maintain the **triple invariant** (declaration / `ResetToDefaults()` / `ExposeData()` in UI display order with section comments) per CLAUDE.md.

**Removed** (fields + UI rows + localization keys): `useRecipeBaseCost`, `traitCostMultiplier`, `traitRefundRate`, `requireRecipeResearch`, `requireAppropriateWorkbench`, `allowUncraftableCustomization`, `allowUltratechCustomization`, `allowArchotechCustomization` (incl. the archotech-implies-ultratech greyed-checkbox UI logic in the mod settings window).

**Kept:** `restrictTraitsToDiscovered`, `minimumQuality`, `allowDefConversion`, `requireCustomizationResearch`, `haulPlannerKind`, `enableGroundCustomization`, `enableIdeologyColors`, `enableStructureColors`, `enforceMaxTraitLimit` (cap now 2, from the bladelink range), `enforceCanGenerateAlone` (default false; see §5 verdict).

**Added:** `traitChangeBaseComponentCost` (2), `traitChangeQualitySurchargeThreshold` (Normal), `traitChangeQualitySurchargePerLevel` (1), `techprintCount` (1), `enableMonoswordRecipe` (true), `enablePlasmaswordRecipe` (true), `enableZeushammerRecipe` (true).

Suggested section order: Progression / Persona Costs (3 sliders + live cost table) / Prerequisites (minimumQuality, allowDefConversion, requireCustomizationResearch, techprintCount) / Crafting Recipes (3 toggles) / Ingredient Hauling / Miscellaneous.

---

## 12. Localization plan

- Rename file `UWU_UI.xml` → `PWU_UI.xml`; prefix `UWU_` → `PWU_` everywhere (XML + all `.Translate()` literals).
- Rewrite Odyssey-flavored strings: `UWU_SettingsCategory` ("Persona Weapons Unbound"), `UWU_NoTraitsDiscovered` (quests/world-loot framing → persona weapons from Empire traders/quests), `UWU_RequireCustomizationResearch(Desc)` (three projects → one), `UWU_WeaponColors` → persona palette label, `UWU_RestrictTraitsToDiscoveredDesc` (unique → persona wording). The gizmo label is already "Customize persona" (user's working-tree edit — preserve it).
- Remove keys for deleted features: texture tab (incl. its disabled-state message), trait-cost settings, removed prerequisite settings, workbench-tier strings (`UWU_RequiresWorkbench` variants collapse to a constant "requires fabrication bench" string).
- Add keys: 4 cost sliders + descs, cost-table header, techprint slider + desc, 3 recipe toggles + descs, persona palette label, persona-core cost/refund footer strings, bond-severed warning, freewielder unbond note.
- Final implementation step: automated cross-check that every key referenced in C# exists in XML and vice versa.

---

## 13. Deletions checklist

- `Source/1.6/TraitCostRules/` (16 files), `Source/1.6/Defs/TraitCostRuleDef.cs`, `Source/1.6/Utilities/CostRuleHelpers.cs`, `1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml`, `MODDERS.md` (`Source/1.6/Utilities/TraitCostUtility.cs` is rewritten in place, not deleted — §6 plumbing)
- `Source/1.6/Utilities/AlphaArmouryIntegration.cs`, `VEFWeaponTraitGraphicsIntegration.cs` + their `ModInitializer` probes and call sites (Preview, JobDriver finalize, TraitProgressionPool). **`VEFRecipeInheritanceIntegration.cs` is KEPT** (revised — §8, D11) along with its `ModInitializer` probe and `WorkbenchUtility` call site.
- **`Source/1.6/Utilities/EquippableAbilityUtility.cs` — DELETE** (revised from "keep"): vanilla bladelink traits grant no abilities, persona defs lack `CompEquippableAbilityReloadable`, and `CompBladelinkWeapon` has no ability wiring at all (only `CompUniqueWeapon.Setup` consumes `abilityProps`). Remove its call sites in `WeaponModificationUtility`, `JobDriver_CustomizeWeapon`, `ModInitializer`. (Future feature idea logged in TODOs.)
- `1.6/Defs/ResearchProjectDefs/{UniqueFabrication,UniqueMachining,UniqueSmithing}.xml`, `1.6/Patches/UniqueFabrication_Royalty.xml` (replaced per §7)
- `Dialog_WeaponCustomization.Texture.cs` + tab wiring
- Workbench tier machinery inside `WorkbenchUtility.cs` (class kept as shim; the VEF recipe-inheritance classification scan is kept, collapsed to the single fabrication set — §8)
- Settings + UI + localization for everything in §11 "Removed"

---

## 14. Compatibility

- **With UWU:** §2 coexistence guarantee + §5 complementary trait filters.
- **Save compat:** PWU is a new mod — no UWU migration. Adding to saves: safe (no world/game components; progression pool is request-scoped). Removing: converted weapons are plain Royalty defs; `CompColorable` state on patched defs degrades silently — comps are rebuilt from the def on load (`ThingWithComps.ExposeData` → `InitializeComps`), so with the comp patch gone the orphaned `<color>`/`<colorActive>`/`<desiredColor>` nodes are never read and Scribe ignores unread value nodes without logging; the weapon reverts to the def tint. Any removal log noise comes from PWU's own defs instead (research progress: one warning; in-flight bills/jobs: red error, dropped, save recovers).
- **Third-party persona weapons:** any ThingDef with `CompBladelinkWeapon` + resolvable pairing participates automatically; custom `nameMaker`s respected via `CompProperties_GeneratedName`.
- **VEF:** only the recipe-inheritance surface is consumed (reflection, silent when VEF is absent, drift-warned when present); benches inheriting from `FabricationBench` participate as customization benches (§8) and expose the §10 recipes via VEF's own propagation.
- **CE/PUAH/Simple Sidearms:** interaction surface unchanged (same job/haul architecture).

---

## 15. Decisions

| #   | Decision                                   | Resolution                                                                                                                                                                                  |
| --- | ------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| D1  | Multiplier semantics                       | **Dropped** — the per-level surcharge slider already gives linear control; a separate multiplier was redundant (§6 and DESIGN.md aligned to three cost sliders, 2026-07-10)                 |
| D2  | `enforceCanGenerateAlone`                  | **Keep**, default false — correct abstraction; vanilla bladelink no-op; honors modded trait intent                                                                                          |
| D3  | Color tab                                  | **Keep** — per-thing recolor via `CompColorable` patch; single persona swatch `(255,200,200)` + Ideology/Structure palettes                                                                 |
| D4  | Techprint slider                           | Live-applied (`TechprintRequirementMet` reads the field); XML ships count 1 so the implied item def always generates; **no restart needed**                                                 |
| D5  | Converting a bonded persona weapon to base | Allowed; `UnCode()` first; footer confirm warning                                                                                                                                           |
| D6  | Trait-change work amount                   | Keep `WorkTicksPerOp = 1000`                                                                                                                                                                |
| D7  | CHANGELOG                                  | Reset to a `1.0.0` PWU entry; credit UWU lineage in README                                                                                                                                  |
| D8  | Adding `NeverBond` to a bonded weapon      | `UnCode()` first + informational warning on the trait row                                                                                                                                   |
| D9  | Adding `NeedKill` to a bonded weapon       | Reflection-set `lastKillTick` to now — otherwise a >20-day-old bond fires the −4 kill-thirst mood instantly (scouting pass, §5)                                                             |
| D10 | Removing `Jealous`/`OnKill_Thought*`       | Purge the trait's weapon-referencing memories on removal — they otherwise linger 1–3 days (scouting pass, §5)                                                                               |
| D11 | VEF workbench equivalence                  | **Keep** the `RecipeInheritanceExtension` slice — benches inheriting recipes from `FabricationBench` (e.g. VFE compact fabrication bench) count as customization benches (user, 2026-07-10) |

## 16. Acceptance checklist (for the orchestrator)

- [ ] Builds clean; deploys to `Mods/PersonaWeaponsUnbound`; `grep -r "CompUniqueWeapon\|NamerUniqueWeapon\|CompProperties_UniqueWeapon" Source/` returns nothing
- [ ] `grep -ri "uniqueweaponsunbound\|UWU_" --exclude-dir=Docs` over the repo returns nothing (UWU-as-history references allowed in Docs only)
- [ ] In-game: monosword + persona core + research → first trait converts def (case-trap pair `Zeushammer↔ZeusHammerBladelink` included); last-trait removal refunds the core and severs any bond with warning; middle changes cost components per the settings table
- [ ] Settings page renders 3 cost sliders + live cost table + techprint slider + 3 recipe toggles; triple invariant holds
- [ ] Recipes appear at the fabrication bench only with research done + toggle on; techprint requirement respects the slider without restart
- [ ] With VEF + VFE loaded: a bench inheriting from `FabricationBench` (e.g. compact fabrication bench) offers weapon customization; without VEF, no warning is logged
- [ ] Trait list shows only `BladeLink`-category traits; with UWU co-loaded, neither mod lists the other's traits
- [ ] Renaming works (reflection into `CompGeneratedNames.name`); persona namer generates "noun+verber" style names; relic name lock still works
- [ ] Recolor: swatch applies via `CompColorable`, visible on ground and equipped; default swatch restores vanilla tint
- [ ] Mutation side effects: removing a hediff trait from a bonded weapon strips its hediff (no orphaned NoPain/SpeedBoost/HungerMaker/NeuralHeatRecoveryGain); adding `NeedKill` to a long-bonded weapon grants a fresh 20-day grace; removing `Jealous`/`OnKill_*` purges lingering memories
- [ ] Tests pass (`./Scripts/test-windows.sh`) after renames
