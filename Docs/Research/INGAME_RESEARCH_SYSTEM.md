# RimWorld Research System - API & Integration Guide

Reference document for Persona Weapons Unbound's research gating implementation (originally researched for the UWU fork; the engine API sections are mod-agnostic).

---

## Table of Contents

1. [ResearchProjectDef XML Schema](#researchprojectdef-xml-schema)
   - [Item-Gated Research Prerequisites (DLC Systems)](#item-gated-research-prerequisites-dlc-systems)
2. [Research Tab System](#research-tab-system)
3. [Research Gating Mechanisms](#research-gating-mechanisms)
4. [C# API Reference](#c-api-reference)
5. [Design Integration Plan](#design-integration-plan)

---

## ResearchProjectDef XML Schema

**Namespace:** `Verse.ResearchProjectDef` (extends `Verse.Def`)

### Core Fields

| Field | Type | Description |
|-------|------|-------------|
| `defName` | `string` | Unique identifier (e.g. `"Smithing"`) |
| `label` | `string` | Display name (lowercase by convention, e.g. `"smithing"`) |
| `description` | `string` | Flavor text shown in research UI |
| `baseCost` | `float` | Research points required (vanilla range: ~300-6000) |
| `techLevel` | `TechLevel` enum | `Neolithic`, `Medieval`, `Industrial`, `Spacer`, `Ultratech`, `Archotech` |

### Research Tree Positioning

| Field | Type | Description |
|-------|------|-------------|
| `researchViewX` | `float` | Horizontal position on the research tree |
| `researchViewY` | `float` | Vertical position on the research tree |
| `tab` | `ResearchTabDef` | Which tab to display on (default: `Main`) |

Vanilla X coordinates range from 0.00 (leftmost Neolithic) to ~19.00 (rightmost Spacer). Y coordinates range from ~0.00 to ~5.60. Projects are visually laid out left-to-right roughly by tech progression, with prerequisite arrows auto-drawn.

### Prerequisites

| Field | Type | Description |
|-------|------|-------------|
| `prerequisites` | `List<ResearchProjectDef>` | Visible required research projects. All must be completed. |
| `hiddenPrerequisites` | `List<ResearchProjectDef>` | Invisible prerequisites — must be completed but NOT shown in UI arrows. |

Hidden prerequisites are used extensively in vanilla to enforce logical ordering without cluttering the research tree. For example, `FlakArmor` has `Machining` as a visible prerequisite and `PlateArmor` as a hidden prerequisite.

### Research Bench Requirements

| Field | Type | Description |
|-------|------|-------------|
| `requiredResearchBuilding` | `ThingDef` | Bench required to research (e.g. `"HiTechResearchBench"`). If null, any bench works. |
| `requiredResearchFacilities` | `List<ThingDef>` | Support structures the bench needs (e.g. `"MultiAnalyzer"`). |

Vanilla patterns:
- Neolithic/Medieval/early Industrial: No bench requirement (simple research bench suffices).
- Mid-to-late Industrial: `requiredResearchBuilding: HiTechResearchBench`.
- Post-MultiAnalyzer: Both `requiredResearchBuilding: HiTechResearchBench` and `requiredResearchFacilities: [MultiAnalyzer]`.

### Item-Gated Research Prerequisites (DLC Systems)

Several DLCs add systems where physical items must be acquired and studied before a research project can begin. These are additional gates checked by `CanStartNow` beyond basic prerequisite research completion.

#### Techprints (Royalty DLC — Hard Dependency)

Techprints are consumable items that must be applied at a research bench before certain research projects become available. Each techprint-gated project specifies how many techprints are required.

**DLC guard:** The `TechprintCount` property has an explicit `ModLister.RoyaltyInstalled` check — if Royalty is not active, the property always returns 0 (skipping the requirement). This means `techprintCount` in XML is silently ignored without Royalty.

**XML fields on ResearchProjectDef:**

| Field | Type | Description |
|-------|------|-------------|
| `techprintCount` | `int` | Number of techprints the player must apply before research can begin. If 0 or Royalty not installed, requirement is skipped. |
| `techprintCommonality` | `float` | Relative weight for techprint generation in loot/trade (higher = more common). |
| `techprintMarketValue` | `float` | Silver value of the generated techprint item. |
| `heldByFactionCategoryTags` | `List<string>` | Faction category tags that can hold/sell this techprint (e.g. `"Empire"`, `"Outlander"`). Controls which traders and faction bases stock the techprint. |

**Example — Cataphract Armor (requires 2 techprints from Empire factions):**

```xml
<ResearchProjectDef>
  <defName>CataphractArmor</defName>
  <label>cataphract armor</label>
  <baseCost>6000</baseCost>
  <techLevel>Spacer</techLevel>
  <prerequisites>
    <li>PoweredArmor</li>
  </prerequisites>
  <requiredResearchBuilding>HiTechResearchBench</requiredResearchBuilding>
  <requiredResearchFacilities>
    <li>MultiAnalyzer</li>
  </requiredResearchFacilities>
  <techprintCount>2</techprintCount>
  <techprintCommonality>3</techprintCommonality>
  <techprintMarketValue>3000</techprintMarketValue>
  <heldByFactionCategoryTags>
    <li>Empire</li>
  </heldByFactionCategoryTags>
</ResearchProjectDef>
```

**How techprints work mechanically:**

1. The game auto-generates a `ThingDef` for each techprint-gated research project. The `Techprint` property on `ResearchProjectDef` dynamically finds the matching ThingDef from `DefDatabase<ThingDef>`.
2. Techprints appear as trade goods from factions matching `heldByFactionCategoryTags`, as quest rewards, or as loot.
3. The player right-clicks a techprint to use it, queuing the `ApplyTechprint` job. The pawn carries the techprint to a research bench and applies it (consuming the item).
4. `ResearchManager.ApplyTechprint()` increments the applied count. Once `TechprintsApplied >= TechprintCount`, the `TechprintRequirementMet` property returns true and the research can be started.

**C# API:**

```csharp
// On ResearchProjectDef:
int TechprintCount { get; }          // Returns 0 if Royalty not installed
int TechprintsApplied { get; }       // Queries ResearchManager.GetTechprints()
bool TechprintRequirementMet { get; } // TechprintsApplied >= TechprintCount
ThingDef Techprint { get; }          // The auto-generated techprint ThingDef

// On ResearchManager:
int GetTechprints(ResearchProjectDef proj);
void ApplyTechprint(ResearchProjectDef proj, ...);
void AddTechprints(ResearchProjectDef proj, ...);
```

**Abstract base pattern — Royalty uses abstract defs to share techprint settings across tiers:**

```xml
<ResearchProjectDef Abstract="True" Name="BaseBodyPartEmpire_TierA">
  <baseCost>2000</baseCost>
  <techLevel>Spacer</techLevel>
  <techprintCount>1</techprintCount>
  <techprintCommonality>1</techprintCommonality>
  <techprintMarketValue>1000</techprintMarketValue>
  <heldByFactionCategoryTags><li>Empire</li></heldByFactionCategoryTags>
</ResearchProjectDef>

<!-- Concrete projects inherit these settings: -->
<ResearchProjectDef ParentName="BaseBodyPartEmpire_TierA">
  <defName>BrainWiring</defName>
  <!-- ... only specifies what differs ... -->
</ResearchProjectDef>
```

#### Analyzable Items / Mechanoid Chips (Core API — Used by Biotech DLC)

The analyzable items system is **part of the core game API** (`Assembly-CSharp.dll`), not a Biotech-specific feature. All relevant classes — `AnalysisManager`, `CompAnalyzableUnlockResearch`, `CompProperties_CompAnalyzableUnlockResearch`, and the `requiredAnalyzed` field on `ResearchProjectDef` — are defined in the core assembly.

**No DLC guard:** Unlike `TechprintCount` (which checks `ModLister.RoyaltyInstalled`), `AnalyzedThingsRequirementsMet` has no DLC-specific check. It simply returns true if `requiredAnalyzed` is null or empty, and checks the `AnalysisManager` otherwise. Any mod can define analyzable items and use `requiredAnalyzed` without depending on Biotech.

Biotech merely defines the XML content that uses this system (boss mechanoid chip ThingDefs with `CompAnalyzableUnlockResearch`, and mechtech research projects with `requiredAnalyzed`).

**XML fields on ResearchProjectDef:**

| Field | Type | Description |
|-------|------|-------------|
| `requiredAnalyzed` | `List<ThingDef>` | ThingDefs that must be analyzed (all of them) before this research can start. Core API — no DLC guard. |
| `requiresMechanitor` | `bool` | If true, a pawn with the Mechanitor trait is required to perform the research. **Has a Biotech DLC guard:** `PlayerMechanitorRequirementMet` checks `ModsConfig.BiotechActive` and returns true (skipping the requirement) if Biotech isn't active. |

**Example — Standard Mechtech (requires analyzing a SignalChip):**

```xml
<ResearchProjectDef ParentName="MechtechBase">
  <defName>StandardMechtech</defName>
  <label>standard mechtech</label>
  <baseCost>1000</baseCost>
  <prerequisites>
    <li>BasicMechtech</li>
  </prerequisites>
  <requiredAnalyzed>
    <li>SignalChip</li>
  </requiredAnalyzed>
</ResearchProjectDef>
```

**The item side — CompProperties_CompAnalyzableUnlockResearch:**

Each analyzable item has this comp, which defines the analysis parameters:

```xml
<ThingDef>
  <defName>SignalChip</defName>
  <label>signal chip</label>
  <comps>
    <li Class="CompProperties_CompAnalyzableUnlockResearch">
      <analysisID>92349061</analysisID>
      <requiresMechanitor>true</requiresMechanitor>
      <analysisDurationHours>0.5</analysisDurationHours>
      <destroyedOnAnalyzed>false</destroyedOnAnalyzed>
      <completedLetterLabel>Signal chip studied: {RESEARCH} unlocked</completedLetterLabel>
    </li>
  </comps>
</ThingDef>
```

**How analyzed items work mechanically:**

1. Boss mechanoids (Diabolus, War Queen, Apocriton) drop specific chip items on death.
2. A mechanitor pawn can study the chip at a research bench (analysis job).
3. The `AnalysisManager` tracks progress via `analysisID`. Once `AnalysisDetails.Satisfied` returns true for all items in `requiredAnalyzed`, the `AnalyzedThingsRequirementsMet` property returns true.
4. Unlike techprints, analyzed items are **not consumed** by default (`destroyedOnAnalyzed: false`).
5. The mechtech chain escalates: SignalChip -> PowerfocusChip -> NanostructuringChip, each dropped by progressively harder bosses.

**C# API:**

```csharp
// On ResearchProjectDef:
int RequiredAnalyzedThingCount { get; }      // requiredAnalyzed list count (0 if null)
int AnalyzedThingsCompleted { get; }         // How many items have been fully analyzed
bool AnalyzedThingsRequirementsMet { get; }  // AnalyzedThingsCompleted >= RequiredAnalyzedThingCount

// Checks via AnalysisManager (Find.AnalysisManager):
bool TryGetAnalysisProgress(int analysisID, out AnalysisDetails details);
```

#### CanStartNow — Full Gating Chain

The `CanStartNow` property checks ALL gates in this order (all must pass):

```
1. !IsFinished                        — Not already completed
2. PrerequisitesCompleted              — All prerequisites + hiddenPrerequisites are IsFinished
3. TechprintRequirementMet             — Enough techprints applied (Royalty)
4. PlayerHasAnyAppropriateResearchBench — Required bench + facilities exist
5. PlayerMechanitorRequirementMet      — Has a mechanitor pawn (Biotech, if requiresMechanitor)
6. AnalyzedThingsRequirementsMet       — All requiredAnalyzed items studied (Core API, no DLC guard)
7. !IsHidden                           — Not hidden by Anomaly codex
8. InspectionRequirementsMet           — Grav engine inspected (Odyssey, if requireGravEngineInspected)
```

If any check fails, the research cannot be started. The research UI shows the unmet requirements to the player.

#### Relevance to Our Mod

PWU uses the first of these item-gated mechanisms:

- **Techprints (Royalty):** `PWU_BladelinkCustomization` requires an Empire-sourced techprint. Royalty is a hard dependency for PWU, so no conditional guard is needed (the `techprintCount` field's built-in `ModLister.RoyaltyInstalled` guard exists regardless). The required count is a mod setting — see the Design Integration section at the end of this document.
- **Required Analysis (Core API):** the `requiredAnalyzed` field and `CompAnalyzableUnlockResearch` system (core API, no DLC guard) can gate research behind analyzing specific things, with "all of" semantics. *Not used by PWU* — kept as engine reference.

### Metadata & Misc

| Field | Type | Description |
|-------|------|-------------|
| `tags` | `List<ResearchProjectTagDef>` | Scenario tags (e.g. `ClassicStart`, `TribalStart`) — controls which research starts already completed for certain scenarios. |
| `discoveredLetterTitle` | `string` | Title of a letter sent when research is completed. |
| `discoveredLetterText` | `string` | Body text of that letter. |
| `hideWhen` | `DifficultyConditionConfig` | Conditions under which this research is hidden (e.g. `<turretsDisabled>true</turretsDisabled>`). |
| `teachConcept` | `ConceptDef` | Tutorial concept to teach on completion. |
| `customUnlockTexts` | `List<string>` | Extra text shown in the "unlocks" section of the research UI. |
| `generalRules` | `RulePack` | Story-generator grammar rules for lore generation. |
| `knowledgeCategory` | `KnowledgeCategoryDef` | Anomaly knowledge category. |
| `knowledgeCost` | `float` | Knowledge points cost (alternative to baseCost for anomaly research). |
| `techprintCount` | `int` | Number of techprints required before research can start. Has `ModLister.RoyaltyInstalled` guard — returns 0 if Royalty not active. Safe to define unconditionally. |
| `techprintCommonality` | `float` | Controls spawn frequency of the techprint item in trade/loot. |
| `techprintMarketValue` | `float` | Market value of the techprint item. |
| `heldByFactionCategoryTags` | `List<string>` | Faction category tags that determine which factions stock the techprint (e.g. `Empire`, `Outlander`). |
| `requiredAnalyzed` | `List<ThingDef>` | List of ThingDefs that must be analyzed before research can start. Core API — no DLC guard. Uses "all of" semantics (every listed item must be analyzed). |
| `requiresMechanitor` | `bool` | Requires a mechanitor pawn in the colony. Has `ModsConfig.BiotechActive` guard — always returns true if Biotech not active. |
| `recalculatePower` | `bool` | If true, recalculate power grid on completion (used by `ColoredLights`). |
| `requireGravEngineInspected` | `bool` | Requires grav engine inspection (Odyssey DLC). |

### Constant

```csharp
public static const TechLevel MaxEffectiveTechLevel = TechLevel.Industrial; // 4
```

Research cost scaling: the faction's tech level compared to the project's tech level applies a multiplier (0.5 per tech level difference) via `CostFactor(TechLevel)`.

---

## Research Tab System

### ResearchTabDef

Defined via `ResearchTabDef` XML. Vanilla has a single `Main` tab.

```xml
<ResearchTabDef>
  <defName>Main</defName>
  <label>Main</label>
  <generalTitle>Main research projects</generalTitle>
  <generalDescription>Unlock new technologies by researching...</generalDescription>
</ResearchTabDef>
```

Mods can define their own tabs and assign research projects to them via `<tab>MyTab</tab>`. However, most mods (including large ones like Vanilla Expanded) place projects on the `Main` tab to keep things discoverable.

**For our mod:** Placing projects on the `Main` tab is the standard approach and avoids fragmenting the research UI.

### ResearchProjectTagDef

Simple marker defs used for scenario start conditions:

```xml
<ResearchProjectTagDef>
  <defName>ClassicStart</defName>
</ResearchProjectTagDef>
```

Vanilla tags: `ClassicStart`, `TribalStart`, `ShipRelated`, `ClassicStartTechprints`, `TribalStartTechprints`.

Projects tagged `ClassicStart` are auto-completed when using the Crashlanded scenario; `TribalStart` for Tribal starts. **Our research projects should NOT use these tags** — weapon customization should always require explicit research.

---

## Research Gating Mechanisms

Research gates content through several parallel systems. All are checked dynamically via the `IsFinished` property.

### 1. RecipeDef.researchPrerequisite (Singular)

Gates a crafting recipe behind a single research project.

```xml
<RecipeDef>
  <defName>Make_Pemmican</defName>
  <researchPrerequisite>Pemmican</researchPrerequisite>
  <!-- Recipe only appears in workbench bill list when Pemmican research is finished -->
</RecipeDef>
```

**C# check:**
```csharp
RecipeDef recipe = ...;
if (recipe.researchPrerequisite == null || recipe.researchPrerequisite.IsFinished)
{
    // recipe is available
}
```

**Note:** This is a singular field — only one `ResearchProjectDef`. There is also a plural `researchPrerequisites` field on `RecipeDef` (all must be finished).

### 2. BuildableDef.researchPrerequisites (Plural)

Gates building construction behind one or more research projects (all must be completed).

```xml
<ThingDef ParentName="BenchBase">
  <defName>FabricationBench</defName>
  <researchPrerequisites>
    <li>Fabrication</li>
  </researchPrerequisites>
</ThingDef>
```

Used on workbenches (smithy, machining table, fabrication bench), buildings (turrets, generators), floors, etc.

### 3. DesignationCategoryDef.researchPrerequisites

Gates an entire architect menu category behind research.

### 4. RecipeMakerProperties.researchPrerequisite

Used in `ThingDef` for auto-generated recipes (e.g. weapons with `recipeMaker`). The weapon's ThingDef specifies:

```xml
<ThingDef>
  <defName>Gun_Revolver</defName>
  <recipeMaker>
    <researchPrerequisite>Gunsmithing</researchPrerequisite>
    <recipeUsers>
      <li>TableMachining</li>
    </recipeUsers>
  </recipeMaker>
</ThingDef>
```

This is how vanilla gates weapon crafting — the weapon's own ThingDef controls which research unlocks its recipe and which workbenches it can be made at. **This is the field we inspect to determine a weapon's crafting research prerequisite** for our dynamic craftability detection.

### 5. Custom C# Gating (Our Approach)

Since weapon customization is a new interaction (float menu + dialog), not a standard recipe, we control research gating entirely in C#:

```csharp
// Pseudocode for float menu availability
ResearchProjectDef required = GetRequiredResearch(weapon.def.techLevel);
if (required != null && !required.IsFinished)
{
    // Don't show "Customize" float menu option, or show it disabled with reason
}
```

This is the standard pattern — research completion is checked at the point where the player-facing action is offered.

---

## C# API Reference

### ResearchProjectDef (Verse)

**Key Properties:**

```csharp
// Completion state
bool IsFinished { get; }            // ProgressReal >= Cost
bool CanStartNow { get; }           // Full gating chain (see "CanStartNow — Full Gating Chain" above)
bool PrerequisitesCompleted { get; } // All prerequisites AND hiddenPrerequisites are IsFinished
bool IsHidden { get; }              // Hidden by Anomaly codex

// Progress
float ProgressReal { get; }          // Raw accumulated research points
float ProgressPercent { get; }       // ProgressReal / Cost, clamped 0-1
float ProgressApparent { get; }      // ProgressReal * CostFactor (display value)
float Cost { get; }                  // baseCost (or knowledgeCost if baseCost is 0)
float CostApparent { get; }          // Cost * CostFactor (display value)

// Techprints (Royalty DLC)
int TechprintCount { get; }          // Returns 0 if Royalty not installed
int TechprintsApplied { get; }       // Number of techprints the player has applied
bool TechprintRequirementMet { get; } // TechprintsApplied >= TechprintCount (or no techprints needed)
ThingDef Techprint { get; }          // The auto-generated techprint ThingDef (null if no techprints needed)

// Analyzable items (Core API, used by Biotech)
int RequiredAnalyzedThingCount { get; }      // Count of requiredAnalyzed items (0 if null)
int AnalyzedThingsCompleted { get; }         // How many have been analyzed (queries AnalysisManager)
bool AnalyzedThingsRequirementsMet { get; }  // All required items analyzed (or none required)

// Other DLC gates
bool PlayerMechanitorRequirementMet { get; } // Has mechanitor if requiresMechanitor (Biotech)
bool PlayerHasAnyAppropriateResearchBench { get; } // Required bench + facilities exist
bool InspectionRequirementsMet { get; }      // Grav engine inspected (Odyssey)

// Unlocks
List<Def> UnlockedDefs { get; }      // All defs this research unlocks (recipes, buildings, etc.)
```

**Key Fields (public, XML-settable):**

```csharp
float baseCost;
TechLevel techLevel;
List<ResearchProjectDef> prerequisites;
List<ResearchProjectDef> hiddenPrerequisites;
List<ResearchProjectDef> requiredByThis;
ThingDef requiredResearchBuilding;
List<ThingDef> requiredResearchFacilities;
List<ResearchProjectTagDef> tags;
ResearchTabDef tab;
float researchViewX;
float researchViewY;
```

### ResearchManager (RimWorld)

Accessed via `Find.ResearchManager`. Implements `IExposable` for save/load.

**Key Fields:**

```csharp
private ResearchProjectDef currentProj;        // Currently researched project
private Dictionary<ResearchProjectDef, float> progress;  // Accumulated points per project
```

**Key Methods:**

```csharp
// Query
float GetProgress(ResearchProjectDef proj);
bool IsCurrentProject(ResearchProjectDef proj);
ResearchProjectDef GetProject();                // Returns currentProj
bool AnyProjectIsAvailable { get; }

// Mutation
void SetCurrentProject(ResearchProjectDef proj);
void StopProject(ResearchProjectDef proj);
void AddProgress(ResearchProjectDef proj, float amount);
void ResearchPerformed(float amount, Pawn researcher);
void FinishProject(ResearchProjectDef proj, bool doCompletionDialog, Pawn researcher);
void ResetAllProgress();
void ReapplyAllMods();                          // Re-runs all ResearchMod effects
```

### DefDatabase<ResearchProjectDef>

```csharp
// Get all research defs
IEnumerable<ResearchProjectDef> allDefs = DefDatabase<ResearchProjectDef>.AllDefs;
List<ResearchProjectDef> allDefsList = DefDatabase<ResearchProjectDef>.AllDefsListForReading;

// Lookup by defName
ResearchProjectDef proj = DefDatabase<ResearchProjectDef>.GetNamed("Smithing");
ResearchProjectDef projOrNull = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Smithing");
```

### TechLevel Enum

```csharp
public enum TechLevel : byte
{
    Undefined = 0,
    Animal = 1,
    Neolithic = 2,
    Medieval = 3,
    Industrial = 4,
    Spacer = 5,
    Ultra = 6,
    Archotech = 7
}
```

---

## Design Integration (PWU)

The original UWU fork of this document ended with a three-tier research ladder (Basic/Standard/Advanced weapon customization) plus a craftability-gating plan. That design is retired.

Persona Weapons Unbound uses a **single ultratech project**, `PWU_BladelinkCustomization` — prerequisite `AdvancedFabrication`, hi-tech bench + multi-analyzer, 1 Empire-sourced techprint (count configurable 0–3 via mod setting, applied live to the def; the XML always ships count 1 so the implied `Techprint_PWU_BladelinkCustomization` item def is generated at startup). See `Docs/Specs/PERSONA_FORK.md` §7 for the full def and the techprint-mutation safety analysis, and `Docs/Research/BLADELINK_WEAPONS.md` § Techprints for the decompiled mechanics.
