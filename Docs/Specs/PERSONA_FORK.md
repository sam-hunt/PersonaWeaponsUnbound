# Persona Weapons Unbound — Fork Conversion Spec

Status: **Final** — grounded in the codebase map and RimWorld 1.6 decompilation (see `Docs/Research/BLADELINK_WEAPONS.md`).
Scope: the full UWU → PWU conversion. This document is the source of truth for the implementation orchestration.

---

## 1. Summary

Fork Unique Weapons Unbound (Odyssey unique weapons) into **Persona Weapons Unbound** (Royalty bladelink/persona weapons). Same interaction skeleton — workbench customization dialog, job-driven application, haul planning, trait discovery progression — retargeted at `CompBladelinkWeapon`, with a radically simpler cost model themed as persona reprogramming.

**Fiction:** Bladelink traits are properties of the weapon persona's personality. Installing an AI persona core turns a base weapon into a persona weapon (and removing the last trait refunds it). Every other trait change is a forceful reprogramming of the persona via advanced computer components that burn out after use — so additions *and* removals both cost, never refund.

### Goals

- Royalty-only hard dependency. Players with Royalty but not Odyssey are fully served.
- Zero references to Odyssey-gated code or defs; no player-facing references to "unique weapons".
- Clean coexistence with UWU when both mods are enabled (disjoint packageId, Harmony ID, assembly, namespaces, defNames, localization keys; complementary trait filtering: UWU = not-bladelink, PWU = only-bladelink).

### Non-goals

- No dynamic cost rule pipeline, no modder extension API (delete `MODDERS.md`).
- No VEF / Alpha Armoury integration.
- No multi-tier research ladder or dynamic workbench tiers.
- No customization of Odyssey unique weapons (that is UWU's job).

---

## 2. Identity & branding renames

| Surface | From | To |
| --- | --- | --- |
| Mod name (About.xml `<name>`) | Unique Weapons Unbound | Persona Weapons Unbound |
| packageId | `shunter.uniqueweaponsunbound` | `shunter.personaweaponsunbound` |
| Harmony ID (`ModInitializer.cs`) | `shunter.uniqueweaponsunbound` | `shunter.personaweaponsunbound` |
| Root C# namespace (66 files) | `UniqueWeaponsUnbound[.…]` | `PersonaWeaponsUnbound[.…]` |
| Assembly / DLL | `UniqueWeaponsUnbound.dll` | `PersonaWeaponsUnbound.dll` |
| AssemblyInfo (`Title`/`Product`/`InternalsVisibleTo`) | `UniqueWeaponsUnbound(.Tests)` | `PersonaWeaponsUnbound(.Tests)` |
| Solution / csproj filenames | `UniqueWeaponsUnbound.sln`, `Source/1.6/UniqueWeaponsUnbound.csproj`, `Tests/1.6/UniqueWeaponsUnbound.Tests.csproj` | `PersonaWeaponsUnbound.*` equivalents (update sln project paths, Tests `ProjectReference`) |
| Class names | `UWU_Mod`, `UWU_Settings`, `UWU_ResearchDefOf`, `UWU_JobDefOf`, `UWU_Textures` | `PWU_Mod`, `PWU_Settings`, `PWU_ResearchDefOf`, `PWU_JobDefOf`, `PWU_Textures` (rename files with classes) |
| Class/filename mismatch fix | class `UniqueWeaponsUnboundMod` in `ModInitializer.cs` | rename class to `ModInitializer` (match filename) |
| JobDef | `UWU_CustomizeWeapon` | `PWU_CustomizeWeapon` (file rename too) |
| Localization keys + file | `UWU_*` in `1.6/Languages/English/Keyed/UWU_UI.xml` | `PWU_*` in `PWU_UI.xml` (find/replace across XML + every `.Translate()` call site) |
| Texture | `Textures/UI/UWU_Customize.png` + `ContentFinder…Get("UI/UWU_Customize")` | `Textures/UI/PWU_Customize.png` + matching path string |
| Log prefix (61 call sites) | `[Unique Weapons Unbound] ` | `[Persona Weapons Unbound] ` |
| Deploy path (csproj `ModDeployPath`) | `$(RimWorldPath)/Mods/UniqueWeaponsUnbound` | `$(RimWorldPath)/Mods/PersonaWeaponsUnbound` |
| CI (`.github/workflows/release.yml`) | sln/csproj paths, zip name `UniqueWeaponsUnbound-*.zip`, staging folder | `PersonaWeaponsUnbound` equivalents |
| `Scripts/test-windows.sh` | csproj/dll names + banner | `PersonaWeaponsUnbound` equivalents |
| `.claude/hooks/sync-mod.sh` (gitignored, local-only) | csproj path + failure banner + stamp filename | update in place locally (not committed) |
| Docs (`README.md`, `CHANGELOG.md`, About description, `Docs/SteamWorkshopDescription.txt`) | UWU branding/features | PWU branding/features (README badges → new repo URL) |

**Coexistence guarantee:** after these renames there is no shared symbol, defName, settings file, Harmony ID, or translation key with UWU. The two mods patch disjoint comp types and their trait filters are complementary (§5), so a hypothetical modded weapon carrying *both* comps is customized independently by each mod without conflict.

`About/PublishedFileId.txt` was already deleted (new Workshop item; file reappears on first upload). New ModIcon/Preview assets are handled by the user — do not recreate them.

---

## 3. Dependencies

`About/About.xml`:

- `modDependencies`: Harmony (keep) + `ludeon.rimworld.royalty` ("RimWorld - Royalty"); **remove** `ludeon.rimworld.odyssey`.
- `loadAfter`: remove `ludeon.rimworld.odyssey`; keep royalty/ideology/biotech/anomaly (Ideology soft support — relics, ideo colors — is kept).
- After conversion there must be no compile-time references to Odyssey-only types: `CompUniqueWeapon`, `CompProperties_UniqueWeapon`, `RulePackDefOf.NamerUniqueWeapon`. Royalty types (`CompBladelinkWeapon`, `WeaponCategoryDefOf.BladeLink`, `CompGeneratedNames`) are safe to reference unconditionally — Royalty is a hard dependency. (`WeaponCategoryDefOf.BladeLink` is `[MayRequireRoyalty]`; it resolves with Royalty active.)

---

## 4. Weapon model: registry & def conversion

### Vanilla def pairs (Royalty)

| Base (`MeleeUltratech.xml`) | Persona (`MeleeBladelink.xml`) |
| --- | --- |
| `MeleeWeapon_MonoSword` | `MeleeWeapon_MonoSwordBladelink` ("persona monosword") |
| `MeleeWeapon_PlasmaSword` | `MeleeWeapon_PlasmaSwordBladelink` ("persona plasmasword") |
| `MeleeWeapon_Zeushammer` | `MeleeWeapon_ZeusHammerBladelink` ("persona zeushammer") |

⚠ **Casing trap:** base `Zeushammer` vs persona `ZeusHammerBladelink` — a naive `base.defName + "Bladelink"` match fails. Pairing must be case-insensitive.

Persona defs = base def's texture + static pink tint `(255,200,200)`, better melee stats, `MarketValue 3000` (vs 2000), `relicChance 3`, comps: `CompQuality` + `CompProperties_BladelinkWeapon (biocodeOnEquip=true)` + `CompProperties_GeneratedName (nameMaker=NamerWeaponBladelink)`. **No `CompArt`** (base ultratech weapons *do* have CompArt + plain CompBiocodable). Neither base nor persona is craftable in vanilla.

### Registry (`WeaponRegistry.cs`, adapted)

- Scan for defs with `CompBladelinkWeapon` (replaces `CompUniqueWeapon` scan). `IsPersonaWeapon(ThingDef)` = has bladelink comp.
- Base↔persona pairing, in order: (1) `descriptionHyperlinks` to a non-persona weapon (vanilla defs don't ship hyperlinks, but modded ones may); (2) naming convention: strip trailing `Bladelink` from the persona defName and look up the base **case-insensitively** (handles the ZeusHammer trap). Keep dynamic detection + collision warning diagnostics. Unpaired persona weapons stay customizable minus def conversion (mirrors UWU orphan handling).

### Def conversion (`WeaponDefConversion.cs`, adapted)

Copy set stays: stuff (n/a for these defs, keep for modded), quality (persona generation does **not** self-assign quality, so explicit copy is required and sufficient), hitpoint %, biocode state, Ideology relic status. Changes:

- **Art:** persona defs have no `CompArt` — `TransferArt` must no-op gracefully when either side lacks the comp. Base→persona drops art (the generated persona name replaces it); persona→base gets a fresh untitled CompArt.
- **`ClearAutoGeneratedUniqueState` → bladelink equivalent:** after `ThingMaker.MakeThing(personaDef)`, `PostPostMake` auto-rolls 1–2 random traits and `CompGeneratedNames.Initialize` rolls a name. Clear them: `comp.TraitsListForReading.Clear()` (live list, **no reflection needed**) and reflection-set `CompGeneratedNames.name` (private field, `Scribe`d as `"name"`).
- **Bonding:** `CompBladelinkWeapon : CompBiocodable`; the bond IS the biocode (`biocoded`/`codedPawn`/`codedPawnLabel`) plus `pawn.equipment.bondedWeapon` back-reference and per-trait `Notify_Bonded` hediffs. **Persona→base conversion must call `UnCode()` first** (public API — clears `bondedWeapon`, fires `Notify_Unbonded` per trait, removes bonded hediffs, clears biocode) before the def swap. Base→persona must NOT pre-bond; the fresh weapon bonds on next equip via `biocodeOnEquip`.
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

- **Add trait:** `traits.Add(def)`; if weapon is bonded → `def.Worker.Notify_Bonded(codedPawn)` (applies `bondedHediffs`). Equipped hediffs are irrelevant mid-job (weapon is carried, not equipped, during customization; they apply on re-equip via `Notify_Equipped`).
- **Remove trait:** if bonded → `def.Worker.Notify_Unbonded(codedPawn)` before removal (removes `bondedHediffs`). Fire `Notify_EquipmentLost` if somehow equipped.
- **`NeverBond` (freewielder) special case:** adding it to a *bonded* weapon flips `Biocodable` to false — call `UnCode()` first (weapon becomes free-wielding). Surface this in the dialog as an informational warning on the trait row.
- Verb burst-cache invalidation stays (shared `burstShot*Multiplier` fields exist on the def even though vanilla bladelink traits don't use them).
- Drop the reflected `ignoreAccuracyMaluses` cache handling (`CompUniqueWeapon`-specific).

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
levelsAboveThreshold = max(0, (int)quality − (int)extraCostQualityThreshold)
N = spacerBaseCost + RoundToInt(levelsAboveThreshold × extraCostPerLevel × extraCostPerLevelMultiplier)
```

| Setting | UI | Range | Default |
| --- | --- | --- | --- |
| `spacerBaseCost` | slider | 0–10 (int) | 2 |
| `extraCostQualityThreshold` | slider (enum, same pattern as existing `minimumQuality`) | Awful–Legendary | Normal |
| `extraCostPerLevel` | slider | 0–5 (int) | 1 |
| `extraCostPerLevelMultiplier` | slider | 0.0–3.0 (float, 0.25 steps) | 1.0 |

Defaults yield: Awful/Poor/Normal 2, Good 3, Excellent 4, Masterwork 5, Legendary 6.

> **NOTE for user review (D1):** "extra cost per level multiplier" is implemented as a linear scalar on the per-level extra (formula above). An alternative reading is geometric compounding per level. Linear chosen for least surprise; both are identical at the recommended default 1.0.

### Settings page cost table

Below the four sliders, render a compact table: one row per `QualityCategory` (Awful→Legendary) showing the derived per-trait-change component count, recomputed live from current slider values. No table helper exists in the settings UI — a simple two-column `Listing_Standard`/`Widgets` grid consistent with existing styling.

### Plumbing

- Replace `TraitCostUtility` with a small static `PersonaCostUtility`: `GetChangeCost(Thing weapon, bool crossesConversionBoundary, bool isRemoval) → List<ThingDefCountClass>`. Output type unchanged, so `CustomizationSpec` per-op `cost`/`refund`, `IngredientReservation.TryReserveIngredientsForJob`, all three haul planners, and the job-driver consumption/ledger code keep working as-is (haul subsystem confirmed fully decoupled).
- **Negative-trait predicate:** UWU detected negatives via a `MarketValue` stat-*factor* < 1 — bladelink traits don't use stat factors; they use `WeaponTraitDef.marketValueOffset` (e.g. `ThoughtWailing` −1000). Re-implement as `marketValueOffset < 0`. It only drives UI (row tint, hide-negative filter) — no cost effect.
- Refund ledger simplifies: the only refund is the persona core (rule 2), whole, no multipliers/rates.
- Delete: `TraitCostRuleDef.cs`, `Source/1.6/TraitCostRules/` (16 files), `CostRuleHelpers.cs`, `TraitCostUtility.cs`, `1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml`, `MODDERS.md`, the dialog-local pipeline cache (`Dialog_WeaponCustomization.cs:459-574` region), settings `useRecipeBaseCost`/`traitCostMultiplier`/`traitRefundRate`, and `InitDiagnostics`' cost-rule bucketing.

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

## 8. Workbench: static Fabrication bench

- Delete the tier system, VEF tier expansion, and the `requireAppropriateWorkbench` setting.
- Keep `WorkbenchUtility` as a thin shim so its four call sites (two float-menu providers, gizmo patch, job flow) stay untouched: `IsCustomizationWorkbench(b)` = `b.def == ThingDefOf.FabricationBench`; `FindBestWorkbench` = nearest reachable, unforbidden, operational fabrication bench; keep `GetWorkbenchOperationalReport` (power check).
- `CustomizationRules.IsCustomizable` drops: tech-level ceiling logic and the recipe-research/craftability chain (`requireRecipeResearch`, `allowUncraftableCustomization` removed — persona weapons are all ultratech; mod installation is treated as intent to customize). Remaining gates: persona-pairable weapon, research finished (if `requireCustomizationResearch`), `minimumQuality`, `allowDefConversion` for base weapons.
- Note: customization does not require *equipping*, so `EquipmentUtility.CanEquip`'s bonded-to-someone-else block doesn't prevent a non-owner colonist from hauling/customizing a bonded weapon. Acceptable; the persona fiction is reprogramming at a bench, not wielding.

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
- Bonded-weapon conversion warning: when a spec's final state reverts a *bonded* persona weapon to base, show a confirm warning in the footer ("bond with {pawn} will be severed").

---

## 10. Crafting recipes (new)

Three `RecipeDef`s modeled on the `Make_ComponentSpacer` pattern (`recipeUsers: FabricationBench` wiring on the recipe side; `researchPrerequisite: PWU_BladelinkCustomization`; `workSkill: Crafting`, `skillRequirements: Crafting 8`; `unfinishedThingDef: UnfinishedWeapon`; `workSpeedStat: GeneralLaborSpeed`; `effectWorking`/`soundWorking` matching vanilla weapon smithing):

| defName | Product | Ingredients | workAmount (suggested) |
| --- | --- | --- | --- |
| `PWU_Make_MonoSword` | `MeleeWeapon_MonoSword` | 140 plasteel, 4 `ComponentSpacer` | 45000 |
| `PWU_Make_PlasmaSword` | `MeleeWeapon_PlasmaSword` | 100 plasteel, 6 `ComponentSpacer` | 45000 |
| `PWU_Make_Zeushammer` | `MeleeWeapon_Zeushammer` | 80 plasteel, 20 uranium, 8 `ComponentSpacer` | 45000 |

- **Per-def setting gating** (`enableMonoswordRecipe` etc., default true): Harmony postfix on `RecipeDef.AvailableNow` returning false when the toggle is off — no def surgery, works mid-save.
- Products are the **base** variants; players then convert them to persona weapons through customization (1 persona core for the first trait), completing the fiction loop.

---

## 11. Settings inventory (`PWU_Settings`)

Maintain the **triple invariant** (declaration / `ResetToDefaults()` / `ExposeData()` in UI display order with section comments) per CLAUDE.md.

**Removed** (fields + UI rows + localization keys): `useRecipeBaseCost`, `traitCostMultiplier`, `traitRefundRate`, `requireRecipeResearch`, `requireAppropriateWorkbench`, `allowUncraftableCustomization`, `allowUltratechCustomization`, `allowArchotechCustomization` (incl. the archotech-implies-ultratech greyed-checkbox UI logic in the mod settings window).

**Kept:** `restrictTraitsToDiscovered`, `minimumQuality`, `allowDefConversion`, `requireCustomizationResearch`, `haulPlannerKind`, `enableGroundCustomization`, `enableIdeologyColors`, `enableStructureColors`, `enforceMaxTraitLimit` (cap now 2, from the bladelink range), `enforceCanGenerateAlone` (default false; see §5 verdict).

**Added:** `spacerBaseCost` (2), `extraCostQualityThreshold` (Normal), `extraCostPerLevel` (1), `extraCostPerLevelMultiplier` (1.0), `techprintCount` (1), `enableMonoswordRecipe` (true), `enablePlasmaswordRecipe` (true), `enableZeushammerRecipe` (true).

Suggested section order: Progression / Persona Costs (4 sliders + live cost table) / Prerequisites (minimumQuality, allowDefConversion, requireCustomizationResearch, techprintCount) / Crafting Recipes (3 toggles) / Ingredient Hauling / Miscellaneous.

---

## 12. Localization plan

- Rename file `UWU_UI.xml` → `PWU_UI.xml`; prefix `UWU_` → `PWU_` everywhere (XML + all `.Translate()` literals).
- Rewrite Odyssey-flavored strings: `UWU_SettingsCategory` ("Persona Weapons Unbound"), `UWU_NoTraitsDiscovered` (quests/world-loot framing → persona weapons from Empire traders/quests), `UWU_RequireCustomizationResearch(Desc)` (three projects → one), `UWU_WeaponColors` → persona palette label, `UWU_RestrictTraitsToDiscoveredDesc` (unique → persona wording). The gizmo label is already "Customize persona" (user's working-tree edit — preserve it).
- Remove keys for deleted features: texture tab (incl. its disabled-state message), trait-cost settings, removed prerequisite settings, workbench-tier strings (`UWU_RequiresWorkbench` variants collapse to a constant "requires fabrication bench" string).
- Add keys: 4 cost sliders + descs, cost-table header, techprint slider + desc, 3 recipe toggles + descs, persona palette label, persona-core cost/refund footer strings, bond-severed warning, freewielder unbond note.
- Final implementation step: automated cross-check that every key referenced in C# exists in XML and vice versa.

---

## 13. Deletions checklist

- `Source/1.6/TraitCostRules/` (16 files), `Source/1.6/Defs/TraitCostRuleDef.cs`, `Source/1.6/Utilities/TraitCostUtility.cs`, `Source/1.6/Utilities/CostRuleHelpers.cs`, `1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml`, `MODDERS.md`
- `Source/1.6/Utilities/AlphaArmouryIntegration.cs`, `VEFRecipeInheritanceIntegration.cs`, `VEFWeaponTraitGraphicsIntegration.cs` + their `ModInitializer` probes and call sites (Preview, JobDriver finalize, TraitProgressionPool, WorkbenchUtility)
- **`Source/1.6/Utilities/EquippableAbilityUtility.cs` — DELETE** (revised from "keep"): vanilla bladelink traits grant no abilities, persona defs lack `CompEquippableAbilityReloadable`, and `CompBladelinkWeapon` has no ability wiring at all (only `CompUniqueWeapon.Setup` consumes `abilityProps`). Remove its call sites in `WeaponModificationUtility`, `JobDriver_CustomizeWeapon`, `ModInitializer`. (Future feature idea logged in TODOs.)
- `1.6/Defs/ResearchProjectDefs/{UniqueFabrication,UniqueMachining,UniqueSmithing}.xml`, `1.6/Patches/UniqueFabrication_Royalty.xml` (replaced per §7)
- `Dialog_WeaponCustomization.Texture.cs` + tab wiring
- Workbench tier machinery inside `WorkbenchUtility.cs` (class kept as shim)
- Settings + UI + localization for everything in §11 "Removed"

---

## 14. Compatibility

- **With UWU:** §2 coexistence guarantee + §5 complementary trait filters.
- **Save compat:** PWU is a new mod — no UWU migration. Adding to saves: safe (no world/game components; progression pool is request-scoped). Removing: converted weapons are plain Royalty defs; `CompColorable` state on patched defs degrades gracefully (comp patch missing → scribe warnings only).
- **Third-party persona weapons:** any ThingDef with `CompBladelinkWeapon` + resolvable pairing participates automatically; custom `nameMaker`s respected via `CompProperties_GeneratedName`.
- **CE/PUAH/Simple Sidearms:** interaction surface unchanged (same job/haul architecture).

---

## 15. Decisions

| # | Decision | Resolution |
| --- | --- | --- |
| D1 | Multiplier semantics | **Linear scalar** (flagged for user review, §6 NOTE) |
| D2 | `enforceCanGenerateAlone` | **Keep**, default false — correct abstraction; vanilla bladelink no-op; honors modded trait intent |
| D3 | Color tab | **Keep** — per-thing recolor via `CompColorable` patch; single persona swatch `(255,200,200)` + Ideology/Structure palettes |
| D4 | Techprint slider | Live-applied (`TechprintRequirementMet` reads the field); XML ships count 1 so the implied item def always generates; **no restart needed** |
| D5 | Converting a bonded persona weapon to base | Allowed; `UnCode()` first; footer confirm warning |
| D6 | Trait-change work amount | Keep `WorkTicksPerOp = 1000` |
| D7 | CHANGELOG | Reset to a `1.0.0` PWU entry; credit UWU lineage in README |
| D8 | Adding `NeverBond` to a bonded weapon | `UnCode()` first + informational warning on the trait row |

## 16. Acceptance checklist (for the orchestrator)

- [ ] Builds clean; deploys to `Mods/PersonaWeaponsUnbound`; `grep -r "CompUniqueWeapon\|NamerUniqueWeapon\|CompProperties_UniqueWeapon" Source/` returns nothing
- [ ] `grep -ri "uniqueweaponsunbound\|UWU_" --exclude-dir=Docs` over the repo returns nothing (UWU-as-history references allowed in Docs only)
- [ ] In-game: monosword + persona core + research → first trait converts def (case-trap pair `Zeushammer↔ZeusHammerBladelink` included); last-trait removal refunds the core and severs any bond with warning; middle changes cost components per the settings table
- [ ] Settings page renders 4 sliders + live cost table + techprint slider + 3 recipe toggles; triple invariant holds
- [ ] Recipes appear at the fabrication bench only with research done + toggle on; techprint requirement respects the slider without restart
- [ ] Trait list shows only `BladeLink`-category traits; with UWU co-loaded, neither mod lists the other's traits
- [ ] Renaming works (reflection into `CompGeneratedNames.name`); persona namer generates "noun+verber" style names; relic name lock still works
- [ ] Recolor: swatch applies via `CompColorable`, visible on ground and equipped; default swatch restores vanilla tint
- [ ] Tests pass (`./Scripts/test-windows.sh`) after renames
