# Bladelink / Persona Weapons — API Deep Dive

Reference document for Persona Weapons Unbound. Covers the internal structure of Royalty's bladelink weapons: the comp, trait system, bonding, naming, rendering/tint, techprints, and generation quirks — with emphasis on what we can offer players control over.

All claims verified against a local RimWorld 1.6 install (build dated 2026-07-01), decompiled from `Assembly-CSharp.dll` with `ilspycmd`, plus `Data/Core`, `Data/Royalty`, and `Data/Odyssey` def XML. Captured 2026-07-10.

**Scope:** Royalty `CompBladelinkWeapon` only. Odyssey unique weapons (`CompUniqueWeapon`) are covered here solely for cross-checks — they are the domain of the sibling mod UWU.

---

## Table of Contents

1. [CompBladelinkWeapon](#compbladelinkweapon)
2. [WeaponTraitDef — Schema & DLC Discrimination](#weapontraitdef--schema--dlc-discrimination)
3. [Royalty Bladelink Trait Catalog](#royalty-bladelink-trait-catalog)
4. [Persona Weapon ThingDefs](#persona-weapon-thingdefs)
5. [Rendering & the Persona Tint](#rendering--the-persona-tint)
6. [Name Generation](#name-generation)
7. [Techprints](#techprints)
8. [Bonding & Equip Restrictions](#bonding--equip-restrictions)
9. [Generation Quirks & Def-Conversion Notes](#generation-quirks--def-conversion-notes)
10. [Odyssey Cross-Check](#odyssey-cross-check)
11. [Customization Surface Summary](#customization-surface-summary)
12. [Key Item / Def Names](#key-item--def-names)

---

## CompBladelinkWeapon

`RimWorld.CompBladelinkWeapon : CompBiocodable` — it **extends** `CompBiocodable`, inheriting all biocode storage and behavior. `CompProperties_BladelinkWeapon : CompProperties_Biocodable` adds no XML fields of its own (persona defs set the inherited `<biocodeOnEquip>true</biocodeOnEquip>`).

### Fields

| Field | Type | Notes |
| --- | --- | --- |
| `traits` | `private List<WeaponTraitDef>` | **Trait storage.** Scribed as `"traits"`, `LookMode.Def` |
| `lastKillTick` | `private int` (init −1) | Kill tracking; exposed as `TicksSinceLastKill` |
| `TraitsRange` | `private static readonly IntRange (1, 2)` | Vanilla generation rolls 1–2 traits |
| `oldBonded*` | legacy | Save-migration only |

### Key members

- `public List<WeaponTraitDef> TraitsListForReading => traits;` — **returns the live list by reference**. A mod can add/remove entries directly, no reflection. Mutations persist in saves. Caveat: nothing re-fires `Worker.Notify_*` hooks or (un)applies hediffs on mutation — the mutator must do that (see [Customization Surface](#customization-surface-summary)).
- `public override bool Biocodable` — `false` if any trait has `neverBond` (freewielder), else `true`.
- `CodeFor(Pawn)` — sends the bond letter (player pawns, first bond), then `base.CodeFor` sets the biocode fields, then `OnCodedFor`: sets `lastKillTick`, sets `pawn.equipment.bondedWeapon = parent`, fires `traits[i].Worker.Notify_Bonded(pawn)` (applies `bondedHediffs`).
- `UnCode()` — **public**: clears `CodedPawn.equipment.bondedWeapon`, fires `Notify_Unbonded` per trait (removes bonded hediffs), clears biocode fields, resets `lastKillTick`. Called by vanilla on `PostDestroy`, `Notify_MapRemoved`, and `Pawn_EquipmentTracker.Notify_PawnDied`. There is no separate `TryUnbond`.
- **Bond storage**: reuses `CompBiocodable`'s `protected bool biocoded; protected string codedPawnLabel; protected Pawn codedPawn;` — there is no separate `bondedPawn` field on the comp. The pawn-side link is `pawn.equipment.bondedWeapon`.
- **Death**: `Pawn_EquipmentTracker.Notify_PawnDied` calls `UnCode()` on the bonded weapon — on the wielder's death the weapon comes back **fully unbonded** (biocode cleared, bonded hediffs stripped, `lastKillTick` reset). _(Corrected 2026-07-10; this doc previously claimed there was no death hook.)_
- Per-trait hooks forwarded to `WeaponTraitWorker`: `Notify_Equipped`, `Notify_Bonded`, `Notify_KilledPawn` (also updates `lastKillTick`), `Notify_EquipmentLost`, `Notify_OtherWeaponWielded`, `Notify_Unbonded`.

### Trait initialization at generation

```csharp
public override void PostPostMake() { InitializeTraits(); }

private void InitializeTraits() {
    ...
    using (new RandBlock(MapGenerator.mapBeingGenerated?.NextGenSeed ?? parent.HashOffset())) {
        int randomInRange = TraitsRange.RandomInRange;          // 1..2 traits
        for (int i = 0; i < randomInRange; i++) {
            IEnumerable<WeaponTraitDef> source = allDefs.Where(CanAddTrait);
            if (source.Any())
                traits.Add(source.RandomElementByWeight(x => x.commonality));
        }
    }
}

private bool CanAddTrait(WeaponTraitDef trait) {
    if (trait.weaponCategory != WeaponCategoryDefOf.BladeLink) return false;   // <-- the discriminator
    if (!traits.NullOrEmpty())
        for (int i = 0; i < traits.Count; i++)
            if (trait.Overlaps(traits[i])) return false;   // exclusionTags overlap
    return true;
}
```

Note: `CanAddTrait` never reads `canGenerateAlone` (that's Odyssey-only, see [cross-check](#odyssey-cross-check)), and `CompBladelinkWeapon` does not self-assign quality (unlike `CompUniqueWeapon`).

---

## WeaponTraitDef — Schema & DLC Discrimination

`RimWorld.WeaponTraitDef : Def`, full fields:

```csharp
public Type workerClass = typeof(WeaponTraitWorker);
public WeaponCategoryDef weaponCategory;          // THE bladelink/unique discriminator
public List<string> exclusionTags;
public float commonality;
public bool canGenerateAlone = true;
public DamageDef damageDefOverride;
public List<ExtraDamage> extraDamages;
public List<StatModifier> statOffsets;
public List<StatModifier> statFactors;
public List<StatModifier> equippedStatOffsets;
public float marketValueOffset;
public float burstShotSpeedMultiplier = 1f;
public float burstShotCountMultiplier = 1f;
public float additionalStoppingPower;
public bool ignoresAccuracyMaluses;
public ColorDef forcedColor;
[MustTranslate] public List<string> traitAdjectives = new List<string>();
public List<HediffDef> equippedHediffs;
public List<HediffDef> bondedHediffs;
public ThoughtDef bondedThought;
public ThoughtDef killThought;
public bool neverBond;
public CompProperties_EquippableAbilityReloadable abilityProps;
```

`Overlaps(other)` = the two share any `exclusionTags`.

**Discrimination:** `weaponCategory` is the single source of truth, exactly as both consuming comps filter:

- Bladelink: `trait.weaponCategory == WeaponCategoryDefOf.BladeLink` (Royalty defines exactly one category, `BladeLink`; all its traits inherit abstract `BladelinkBase` which sets it).
- Odyssey: `CompProperties_UniqueWeapon.weaponCategories.Contains(trait.weaponCategory)` against its 15 categories (`Ranged`, `Gun`, `Rifle`, …) — never `BladeLink`.

Categories are disjoint by construction; no shared defNames. A trait has exactly one `weaponCategory`, so it is bladelink **or** unique, never both. `WeaponCategoryDefOf.BladeLink` is `[MayRequireRoyalty]`.

**Field usage split** (who reads what):

- Bladelink-only: `equippedStatOffsets`, `bondedHediffs`, `bondedThought`, `killThought`, `neverBond`.
- Odyssey-only: `statOffsets`, `statFactors`, `burstShot*`, `additionalStoppingPower`, `ignoresAccuracyMaluses`, `damageDefOverride`, `extraDamages`, `forcedColor`, `traitAdjectives`, `abilityProps`, `canGenerateAlone`.
- Both: `equippedHediffs`, `exclusionTags`, `commonality`, `marketValueOffset`, `Worker.Notify_*` hooks.

Implication for a negative-trait heuristic: bladelink traits signal value through **`marketValueOffset`** (e.g. `ThoughtKind` +500, `ThoughtWailing` −1000), not MarketValue stat factors.

---

## Royalty Bladelink Trait Catalog

All 19, `weaponCategory=BladeLink`, from `Data/Royalty/Defs/WeaponTraitDefs/WeaponTraitDefs.xml`:

| defName | Effect |
| --- | --- |
| `PsychicSensitivityUpMajor` | +40% psychic sensitivity (equipped) |
| `PsychicSensitivityUpMinor` | +20% psychic sensitivity (equipped) |
| `PsychicSensitivityDownMinor` | −15% psychic sensitivity (equipped) |
| `PsychicSensitivityDownMajor` | −30% psychic sensitivity (equipped) |
| `ThoughtKind` | bonded mood +6 |
| `ThoughtCalm` | bonded mood +3 |
| `ThoughtMuttering` | bonded mood −3 |
| `ThoughtWailing` | bonded mood −6 |
| `OnKill_PsyfocusGain` | +20% psyfocus on kill (`WeaponTraitWorker_PsyfocusOnKill`) |
| `OnKill_ThoughtGood` | mood +6 for days after a kill (`killThought`) |
| `OnKill_ThoughtBad` | mood −3 after a kill |
| `NeedKill` ("kill thirst") | mood penalty if no kill in 20 days (reads `TicksSinceLastKill`) |
| `PsyfocusMeditationBonus` | +10% meditation focus gain (equipped) |
| `NoPain` ("painless") | equipped hediff, pain factor 0 |
| `SpeedBoost` ("fast mover") | equipped hediff, +0.15 move |
| `HungerMaker` ("hunger pangs") | bonded hediff, +50% hunger rate |
| `NeuralHeatRecoveryGain` ("neural cooling") | equipped hediff, +0.15 psychic entropy recovery |
| `NeverBond` ("freewielder") | `neverBond=true` — disables bonding; anyone can wield |
| `Jealous` | mood penalty when wielding another weapon |

**Zero** vanilla bladelink traits set `canGenerateAlone=false`, use `abilityProps`, or set `forcedColor`.

---

## Persona Weapon ThingDefs

Files: `Data/Royalty/Defs/ThingDefs_Misc/Weapons/MeleeBladelink.xml` (persona), `.../MeleeUltratech.xml` (base).

| Base defName | Persona defName | Persona label |
| --- | --- | --- |
| `MeleeWeapon_MonoSword` | `MeleeWeapon_MonoSwordBladelink` | persona monosword |
| `MeleeWeapon_PlasmaSword` | `MeleeWeapon_PlasmaSwordBladelink` | persona plasmasword |
| `MeleeWeapon_Zeushammer` | `MeleeWeapon_ZeusHammerBladelink` | persona zeushammer |

⚠ Casing quirk: base `Zeushammer` vs persona `ZeusHammerBladelink` — suffix-strip pairing must be case-insensitive.

Shared abstract parent `BaseWeapon_Bladelink`:

```xml
<ThingDef Name="BaseWeapon_Bladelink" ParentName="BaseWeapon" Abstract="True">
  <techLevel>Ultra</techLevel>
  <tradeNeverStack>true</tradeNeverStack>
  <relicChance>3</relicChance>
  <weaponTags><li>Bladelink</li></weaponTags>
  <comps>
    <li><compClass>CompQuality</compClass></li>
    <li Class="CompProperties_BladelinkWeapon"><biocodeOnEquip>true</biocodeOnEquip></li>
    <li Class="CompProperties_GeneratedName"><nameMaker>NamerWeaponBladelink</nameMaker></li>
  </comps>
  <thingCategories><li>WeaponsMeleeBladelink</li></thingCategories>
  <thingSetMakerTags><li>WeaponBladelink</li></thingSetMakerTags>
</ThingDef>
```

- Persona comps = CompQuality + CompBladelinkWeapon + CompGeneratedNames. **No `CompArt`, no `CompColorable`.**
- Base ultratech comps = CompQuality + `CompProperties_Art` (`NamerArtWeaponMelee`) + plain `CompProperties_Biocodable`.
- MarketValue: persona **3000**, base **2000**. Both `techLevel Ultra`, mass 2, not smeltable.
- **Neither is craftable in vanilla** — no `recipeMaker`/`costList` on either side. Sources: Empire traders (`weaponTag Bladelink` in `TraderKinds_*_Empire.xml`), quest rewards (`thingSetMakerTags WeaponBladelink`), relics (`relicChance 3`).
- Persona variants have better melee stats than the base (e.g. persona monosword 27 power / 1.6 cooldown vs base 25 / 2.0).

---

## Rendering & the Persona Tint

The persona tint **is a static def-level color over the base weapon's own texture**:

```xml
<graphicData>
  <graphicClass>Graphic_Single</graphicClass>
  <texPath>Things/Item/Equipment/WeaponMelee/Monosword</texPath>
  <color>(255,200,200)</color>
</graphicData>
```

Same `texPath` as the base def (which has no `<color>`, i.e. white). No separate persona texture, no texture variants (`Graphic_Single`), and `ignoreThingDrawColor` is **not** set.

**Per-thing recolor is feasible.** The whole pipeline honors `Thing.DrawColor`:

```csharp
// ThingWithComps.DrawColor
CompColorable comp = GetComp<CompColorable>();
if (comp != null && comp.Active) return comp.Color;
foreach (ThingComp allComp in AllComps) {
    Color? color = allComp.ForceColor();       // how CompUniqueWeapon recolors
    if (color.HasValue) return color.Value;
}
return base.DrawColor;                          // = def.graphicData.color
```

`Thing.Graphic` → `def.graphicData.GraphicColoredFor(this)` regenerates a colored graphic whenever `DrawColor` differs from the def color (cache invalidated by `Notify_ColorChanged()`). Both ground rendering and equipped rendering (`PawnRenderUtility.DrawEquipmentAiming` uses `eq.Graphic.MatSingleFor(eq)`) draw the per-thing graphic.

Two clean recolor options: **(a) patch `CompProperties_Colorable` onto the persona defs** and call `thing.SetColor(...)` (`CompColorable` is saved; inactive → falls back to def tint), or (b) a custom comp overriding `ForceColor()`. `CompBladelinkWeapon` itself does not override `ForceColor`. Option (a) is PWU's chosen approach (see spec §9).

---

## Name Generation

Persona names come from **`CompGeneratedNames`** (`CompProperties_GeneratedName`, `nameMaker: NamerWeaponBladelink` — the RulePack lives in `MeleeBladelink.xml`):

```csharp
private string name;                      // Scribe_Values.Look(ref name, "name")
public string Name => name;
public static string GenerateName(CompProperties_GeneratedName props) =>
  GenText.CapitalizeAsTitle(GrammarResolver.Resolve("r_weapon_name",
      new GrammarRequest { Includes = { props.nameMaker } }));
public override string TransformLabel(string label) {
  if (parent.StyleSourcePrecept != null) return label;                          // relic styling wins
  if (parent.GetComp<CompBladelinkWeapon>() != null) return name + ", " + label; // "'Ripjack', monosword"
  return name + " (" + label + ")";
}
public override void Initialize(CompProperties props) { base.Initialize(props); name = GenerateName(Props); }
```

Sample grammar (`NamerWeaponBladelink`, includes `NamerAnimalUtility`):

```
r_weapon_name(p=3)->[noun][verber]        # "Deathbringer", "Bloodkeeper"
r_weapon_name(p=2)->[beginSyl][middleSyl][endSyl]
r_weapon_name(p=1)->[NamePerson]
noun->oath/promise/death/pain/blood/doom/murder/justice/fear/terror/chaos/war...
verber->keeper/bringer/sender/giver/maker/crusher/breaker/smasher/bender/knower/doer
```

- The name is generated **once** in `Initialize()`; there is **no public setter**. Renaming requires reflection into the private `name` field (persists — it's scribed). Fresh random names via the public static `GenerateName(props)`.
- Display goes through `ThingWithComps.LabelNoCount` walking comp `TransformLabel`s — `CompGeneratedNames` adds the persona name; `CompBiocodable.TransformLabel` adds the biocode wrapper. No trait adjectives or colors feed the bladelink namer (unlike Odyssey's `NamerUniqueWeapon`).

---

## Techprints

`ResearchProjectDef` fields: `techprintCount` (int), `techprintCommonality` (=1), `techprintMarketValue` (=1000), `heldByFactionCategoryTags` (List<string>).

- `TechprintCount` property force-returns 0 without Royalty installed.
- `CanStartNow` requires `TechprintRequirementMet` = `TechprintsApplied >= TechprintCount` (always true at count 0). **The field is read live** — runtime changes to `techprintCount` take effect immediately for gating.
- The techprint **item def is implied**: `ThingDefGenerator_Techprints.ImpliedTechprintDefs()` runs once at startup and emits `Techprint_<projectDefName>` for every project with count > 0 (market value from `techprintMarketValue`, `tradeTags: Techprint`, `CompProperties_Techprint { project }`). So ship XML count ≥ 1 to guarantee the item exists; mutate the count afterward per settings.
- Sourcing: `StockGenerator_Techprints` (trader stock) and quest rewards, both via `TechprintUtility`, which filters projects by `faction.def.categoryTag ∈ heldByFactionCategoryTags` and weights by `techprintCommonality` (×0.02 if prerequisites incomplete). Count 0 projects are excluded from generation.
- **ConfigErrors**: count > 0 requires non-empty `heldByFactionCategoryTags`, and tags with count 0 is an error — validation runs at load against the XML value.
- Applying: `CompTechprint` float menu → `JobDefOf.ApplyTechprint` at a research bench → +1 applied, +2000 Intellectual XP.

Reference values: `AdvancedFabrication` (prereq `Fabrication`) = 4000, no techprint, HiTech bench + multi-analyzer; `JumpPack` = 2000, 1 techprint @2000, Empire+Outlander; `CataphractArmor` = 6000, 2 techprints @3000, Empire.

---

## Bonding & Equip Restrictions

- **No royal title / psylink requirement** — `ThingRequiringRoyalPermissionUtility.GetMinTitleToUse` only applies to implants. Bonding is automatic on equip (`biocodeOnEquip=true`) with a confirmation dialog (`GetPersonaWeaponConfirmationText`) and a letter.
- `EquipmentUtility.CanEquip` blocks: (1) equipping a weapon bonded to a **different** pawn (`BladelinkBondedToSomeoneElse`); (2) a pawn who already has a different `bondedWeapon` (`BladelinkAlreadyBondedMessage`). The freewielder trait (`neverBond`) makes `Biocodable` false, bypassing both — anyone can wield.
- Carrying/hauling a bonded weapon is unrestricted (only *equip* is gated) — so customization jobs by non-owners work.

---

## Generation Quirks & Def-Conversion Notes

Comp init order on `ThingMaker.MakeThing(personaDef)`: all comps `Initialize(props)` in declaration order, then all `PostPostMake()`:

1. `CompQuality` — quality **not** self-assigned (set later by generation context) → a converting mod must copy/roll quality explicitly.
2. `CompBladelinkWeapon` — `PostPostMake` runs `InitializeTraits()` (1–2 random traits, `RandBlock` seeded by `parent.HashOffset()` outside map-gen) → a converting mod should clear `TraitsListForReading` and apply the player-chosen traits.
3. `CompGeneratedNames.Initialize` — rolls a name immediately → re-set via reflection if the player chose one.

Other conversion notes:

- Fresh persona weapons are unbonded; they bond on first equip. Don't pre-bond.
- **Persona→base**: call `UnCode()` before the swap (clears `pawn.equipment.bondedWeapon`, bonded hediffs, biocode); persona name is dropped with the comp.
- Persona defs have no `CompArt`; base defs do. Art transfer must be conditional on both sides having the comp.
- No implied-def machinery exists for the weapons themselves (only techprints).
- Pawnkinds never field-generate persona weapons; traders/quests/relics only.

---

## Odyssey Cross-Check

`CompUniqueWeapon` (`CompProperties_UniqueWeapon { weaponCategories, namerLabels }`) — trait selection:

```csharp
if (!Props.weaponCategories.Contains(trait.weaponCategory)) return false;
if (TraitsListForReading.Empty() && !trait.canGenerateAlone) return false;   // Odyssey-only check
if (trait.Overlaps(trait2)) return false;
```

- `canGenerateAlone` is read **only here** — never by bladelink code. (Odyssey sets it false on `CustomGrip`, `BirdshotPellets`, `ShoddySights`, `Ornamental`, `Ugly`, `Cumbersome`, `GoldInlay`, `JadeInlay`.)
- Odyssey unique weapons: ranged only, comps include `CompEquippableAbilityReloadable` (which `abilityProps` traits require — persona defs lack it entirely), `CompStyleable`, `CompArt`, `CompBiocodable`; names via `RulePackDefOf.NamerUniqueWeapon` + `namerLabels` + trait adjectives; per-thing color via `CompUniqueWeapon.ForceColor()` (a `ColorDef` rolled from `ColorType.Weapon`).
- **Robust mod filter**: `trait.weaponCategory == WeaponCategoryDefOf.BladeLink` ⇒ bladelink; else ⇒ Odyssey/unique. Authoritative for third-party traits (it's what the comps themselves enforce).

---

## Customization Surface Summary

What a mod can expose, and how:

| Feature | Mechanism | Difficulty |
| --- | --- | --- |
| Add/remove traits | mutate `TraitsListForReading` (live list) + manually fire `Worker.Notify_Bonded/Notify_Unbonded` and manage hediffs | Easy (side-effect care) |
| Re-roll traits | clear list + reproduce `InitializeTraits` logic | Easy |
| Rename persona | reflection-set `CompGeneratedNames.name`; regen via public `GenerateName(props)` | Easy |
| Recolor | patch `CompColorable` onto defs; `SetColor()` | Easy |
| Unbond / rebond | public `UnCode()` / `CodeFor(pawn)` | Easy |
| Kill tracking | `lastKillTick` (private) / `TicksSinceLastKill`; feeds `NeedKill` | Reflection to reset |
| Trait abilities | not supported by vanilla bladelink — would require adding `CompEquippableAbilityReloadable` + Odyssey-style wiring | Hard / out of scope |
| Market value preview | sum `marketValueOffset` per trait | Easy |
| Bond ceremony | none exists — bonding is an on-equip event with a letter | n/a |

---

## Key Item / Def Names

| Thing | defName | Label |
| --- | --- | --- |
| AI persona core | `AIPersonaCore` | persona core |
| Advanced component | `ComponentSpacer` | advanced component (MarketValue 200) |
| Component | `ComponentIndustrial` | component |
| Plasteel | `Plasteel` | plasteel |
| Uranium | `Uranium` | uranium |
| Fabrication bench | `FabricationBench` | fabrication bench |
| Advanced fabrication research | `AdvancedFabrication` | advanced fabrication |
| Bladelink trait category | `BladeLink` (`WeaponCategoryDefOf.BladeLink`, `[MayRequireRoyalty]`) | — |
| Persona namer rule pack | `NamerWeaponBladelink` (root `r_weapon_name`) | — |

Vanilla fabrication-bench recipe template (research-gated), `Make_ComponentSpacer`:

```xml
<RecipeDef>
  <defName>Make_ComponentSpacer</defName>
  <label>make advanced component</label>
  <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
  <workAmount>10000</workAmount>
  <unfinishedThingDef>UnfinishedComponent</unfinishedThingDef>
  <ingredients>...</ingredients>
  <products><ComponentSpacer>1</ComponentSpacer></products>
  <researchPrerequisite>AdvancedFabrication</researchPrerequisite>
  <skillRequirements><Crafting>8</Crafting></skillRequirements>
  <workSkill>Crafting</workSkill>
  <displayPriority>10</displayPriority>
</RecipeDef>
```

(That recipe is attached via `FabricationBench`'s `<recipes>` list; a mod-added recipe should use `<recipeUsers><li>FabricationBench</li></recipeUsers>` instead to avoid patching the bench def.)
