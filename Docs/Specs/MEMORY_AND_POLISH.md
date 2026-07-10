# Persona Weapons Unbound — Memory Operations & Dialog Polish Spec

Status: **Final** — grounded in a codebase recon pass (file:line references, 2026-07-11) and RimWorld 1.6 decompilation (`ilspycmd` against Assembly-CSharp; see §5).
Scope: the four changes handed off in `Docs/HANDOFF.md` (2026-07-11). This document is the source of truth for their implementation.

Relationship to `PERSONA_FORK.md`: decision numbering continues from D11. **§2 reverses D3** (the color tab was kept there; it is removed here). Everything else in that spec stands.

---

## 1. Summary

| #   | Change                                                                                                  | Section |
| --- | ------------------------------------------------------------------------------------------------------- | ------- |
| 1   | Remove weapon recoloring entirely — vanilla's static pink tint `(255,200,200)` wins                     | §2      |
| 2   | LHS trait/op chips: give the label a greater share of the width (costs are single-resource)             | §3      |
| 3   | New **Memory** tab: a one-time memory wipe — bond or kill tracker, a 3-way radio — costed in components | §4      |
| 4   | Re-equip-after-customization pops vanilla's persona-bond confirmation when the equip would bond         | §5      |

Changes 1–3 are all inside `Dialog_WeaponCustomization` and its job pipeline; change 4 is in the job driver's recovery path. No Harmony patches are added or removed by any of them.

---

## 2. Remove weapon recoloring (reverses D3)

Verdict: the vanilla persona tint `(255,200,200)` looks better than almost every possible colorization; the per-thing recolor pipeline isn't worth its complexity. All color customization is deleted — the def's static tint is once again the only appearance.

### Files deleted outright

- `Source/1.6/UI/Dialog_WeaponCustomization.Color.cs` — the whole Color tab (`DrawColorTab`, `DrawColorGrid`, `MeasureColorSections`, `GridHeight`, `DrawIdeoColorOverlay`, `CanRecolor`). Nothing outside the file consumes any of it.
- `1.6/Patches/BladelinkColorable.xml` — the patch that added `CompColorable` to `BaseWeapon_Bladelink`. No other feature depends on the comp.

### Surgical edits

| File                                     | Remove                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| ---------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Dialog_WeaponCustomization.cs`          | Fields `originalColor`, `personaDefaultColor`, `availablePersonaColors`, `availableIdeoColors`, `availableStructureColors`, `initialDesiredColor`, `desiredColor`, `colorTabScroll`; consts `ColorSwatchSize`/`ColorSwatchGap`/`ColorIndicatorSize` (:126-128); ctor palette-build block (~:203-281, persona tint + Ideology loop + structure loop + `CompColorable` snapshot); methods `FindColorDefForColor`, `MakeRuntimeColorDef`, `EffectiveColor`, `GetForcedColor`, `IsScribeSafeColor`; the color clause in `HasChanges` (:429-430); the forced-color fallback block in `OnTraitsChanged` (:457-466); `desiredColor` reset in `ResetToOriginal` (:450); in `BuildOperations` (:598-743): `hasForcedColorTraits`, the removal-loop forced-color block (:626-643), all `colorChanged`/`deferredColor`/`colorToApply`/`clearColor` branches in the cosmetics section (:648-705) and addition loop (:720-735). Header ASCII diagram/comments (:17-36) updated. |
| `Dialog_WeaponCustomization.Controls.cs` | In `DrawTabs` (:157-183): the `PWU_TabColor` `TabRecord` (incl. its label-padding spaces) and the entire swatch-inside-tab drawing block (:167-181). **Keep the tab-container shell** (`activeTab`, `TabRecord` list, dispatch branch in `DrawControlsPanel` :32-35) — the Memory tab (§4) takes the vacated `activeTab == 1` slot.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| `Dialog_WeaponCustomization.Preview.cs`  | `cachedPreviewColor` field (:18); the `ColorDef` parameter/threading through `DrawPreviewIcon` → `RebuildPreviewRT` → `BuildPreviewGraphic`; the `WeaponModificationUtility.SetColor(previewThing, colorDef)` call (:351). Trait/def stamping and the RT blit stay.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| `Dialog_WeaponCustomization.Footer.cs`   | In `BuildCustomizationSpec` (:285-309): the `finalColor`/`finalColorClear` block (:294-298, :306-307).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| `Jobs/CustomizationSpec.cs`              | `CustomizationOp.colorToApply`/`.clearColor` + their `Scribe_*` lines; `CustomizationSpec.finalColor`/`.finalColorClear` + their `Scribe_*` lines.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| `Jobs/JobDriver_CustomizeWeapon.Work.cs` | The trailing `CompColorable` application block in `ApplyOperationInner` (:302-310).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| `Jobs/JobDriver_CustomizeWeapon.cs`      | The `finalize` toil's `CompColorable` body + its `PWU_FinalizeColorFailed` catch (:707-723); "color" in the header comment (:16).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| `Utilities/WeaponModificationUtility.cs` | `SetColor(Thing, ColorDef)` (:203-220) + its doc block; "(name, color)" → "(name)" in the class doc (:11).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| `Defs/PWU_Textures.cs`                   | `FavoriteColor` and `IdeoColor` (:13-16) — consumed only by the deleted Color tab. `Customize` stays.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |

The `forcedColor` handling goes too (dialog lock UI, fallback picks, per-op forced-color emission): with `CompColorable` gone the entire path is permanently dead — `WeaponTraitDef.forcedColor` is only otherwise read by Odyssey's `CompUniqueWeapon`, and no vanilla bladelink trait sets it (PERSONA_FORK §5).

### `ApplyCosmetics` → `Rename`

After the color fields are gone the op carries only `nameToApply` — rename it (handoff item 1 explicitly allows this):

- `OpType.ApplyCosmetics` → `OpType.Rename`; `CustomizationOp` keeps `nameToApply`.
- `BuildOperations`' cosmetics section condition collapses from `nameChanged || (colorChanged && !hasForcedColorTraits)` (:683) to `nameChanged`. The existing deferred-name merge into an addition op stays (a rename bundled with a trait add is still one op).
- `ApplyOperationInner`'s case body keeps only the `SetName` call (rename remains free — no `TryConsumeOpCost`, unchanged).
- Verb key `PWU_ApplyingCosmetics` ("Customizing appearance of {0}") → `PWU_RenamingWeapon` ("Renaming {0}"); bail key `PWU_BailOpCosmeticsFailed` → `PWU_BailOpRenameFailed`.

Pre-release freedom (D14): the mod has never shipped, so the scribed enum-member rename and dropped spec fields need no migration. Worst case is a dev save taken mid-customization-job; acceptable.

### Settings removed (triple invariant: declaration / `ResetToDefaults` / `ExposeData` + UI row)

- `enableIdeologyColors` — `PWU_Settings.cs:33/:58/:87`; UI `PWU_Mod.cs:199-207` (the whole `ModsConfig.IdeologyActive` block).
- `enableStructureColors` — `PWU_Settings.cs:34/:59/:88`; UI `PWU_Mod.cs:209-213`.

### Localization keys removed

`PWU_TabColor`, `PWU_SelectTraitsForColor`, `PWU_ColorDeterminedBy`, `PWU_PersonaColors`, `PWU_PersonaDefaultColor`, `PWU_IdeologyColors`, `PWU_StructureColors`, `PWU_ColorNotSupported`, `PWU_FinalizeColorFailed`, `PWU_RelicIdeoColorTip`, `PWU_EnableIdeoColors(Desc)`, `PWU_EnableStructureColors(Desc)`.
(Referenced-but-vanilla keys `FavoriteColorPickerTip`/`IdeoColorPickerTip` disappear with the file; no XML edit.)

### Save compatibility

Already analyzed for the mod-removal case in PERSONA_FORK §14 — the same mechanism applies to removing the comp patch: comps are rebuilt from the current def on load (`ThingWithComps.ExposeData` → `InitializeComps`), orphaned `<color>`/`<colorActive>` nodes are simply never read, no log noise. Recolored weapons in dev saves silently revert to the def tint. In-flight job specs drop their color fields the same way at the `IExposable` level.

### Player-facing copy sweep

Drop recolor/palette claims from: `About/About.xml` description (edit on top of the user's current working-tree wording — do not revert their copy pass), `README.md` (:16, :27-30 "Rename & Recolor" section, :62 palette-toggles bullet), `CHANGELOG.md` (:17, :23 — the 1.0.0 entry is unreleased; edit it in place), `Docs/SteamWorkshopDescription.txt` (:22-25), `CLAUDE.md` project overview (:7). `DESIGN.md` updates are consolidated in §8.

---

## 3. LHS chips: widen the label column

The LHS chip list (`DrawWeaponPreview`, `Dialog_WeaponCustomization.Preview.cs:74-140`) currently splits each chip 50/50: `labelRect` takes `chipRect.width * 0.5f` (:90-92) and the cost rect fills the remainder (:105-107).

UWU needed half a chip for costs because a single change could span several resource types. In PWU any op's cost list has at most one entry (1× `AIPersonaCore` or N× `ComponentSpacer` — `TraitCostUtility`), which renders as one 24px icon plus a short count (worst case "×35" at maxed sliders), well under 90px.

**Change:** the label fraction at `Preview.cs:92` goes from `0.5f` to `0.7f`. The cost rect's origin (`labelRect.xMax`) adjusts automatically. Memory-op chips (§4) share the same geometry. The RHS trait-tab rows (`Traits.cs:226-244`, 35% label / 35% rejection-reason / 30% cost) are **not** touched — they need the reason column.

---

## 4. Memory tab

### Fiction

The persona's memory is data: its bond to a wielder and its record of kills. Reprogramming can erase either — a one-time destructive operation, costed in advanced components like any other forced rewrite of the persona (PERSONA_FORK §6 fiction).

### Operations

The tab offers a **three-way radio choice** — "Do not wipe" (default, free), or exactly one of:

| Op                    | Effect                                                                                        | Setting (slider 0–5, `ComponentSpacer`) | Default |
| --------------------- | --------------------------------------------------------------------------------------------- | --------------------------------------- | ------- |
| **Wipe bonding**      | Severs the bond; biocode state returns to bond-on-next-equip. **Erases kill memory with it.** | `wipeBondingComponentCost`              | 3       |
| **Wipe kill tracker** | Clears the persona's kill memory (`lastKillTick`) only; the bond is untouched                 | `wipeKillTrackerComponentCost`          | 1       |

The ops are mutually exclusive by construction (D17): `UnCode()` resets `lastKillTick`, so a bond wipe strictly subsumes the kill wipe — "both" is not a distinct outcome and the UI must not express it (a checkbox model would either sell the same wipe twice or need disable/deselect machinery to hide the phantom state). The radio group also makes the subsumption legible: staging the bond wipe visibly deselects any kill-wipe choice, and the bond-wipe label/tooltip say the kill memory goes with it.

Costs are flat — no quality scaling (unlike trait changes; the handoff defines these as plain sliders). Never refunded. The wipe is one-time: it doesn't persist as weapon state, so it never shows as an "original" chip.

### Dialog state

- New field `MemoryOpKind desiredMemoryOp` with `enum MemoryOpKind { None, WipeBonding, WipeKillTracker }`, default `None` (display order = enum order; a future op — TODOs float re-roll/rebond ideas — is one more enum member and radio row).
- `HasChanges` (:416-433) gains `|| desiredMemoryOp != MemoryOpKind.None`; `ResetToOriginal` resets it to `None`.
- `OnTraitsChanged` re-validates the selection: if the selected option's row is now disabled (see gating below), the selection snaps back to `None` — a stale memory op can never reach the spec.
- When the preview flips to base (`IsRevertedToBase` becomes true — last trait removed), the selection resets to `None`; its chip disappears and the cost preview updates.

### Tab UI

- `activeTab == 1` becomes the Memory tab (Color's old slot): `TabRecord("PWU_TabMemory".Translate(), () => activeTab = 1, activeTab == 1)` in `DrawTabs`; `DrawMemoryTab(tabContentRect)` in the `DrawControlsPanel` dispatch (:32-35). No swatch or icon in the tab label.
- **Base-state gate** (handoff: ops require persona/bladelink form): when `IsRevertedToBase`, the tab body shows only a centered gray message — the exact pattern the Color tab used (`Color.cs:18-31`, `TextAnchor.MiddleCenter` + `Color.gray`): `PWU_MemoryRequiresPersona`.
- Otherwise: a **radio group of three rows** — "Do not wipe" (`PWU_MemoryNoWipe`, no cost, selected by default), "Wipe bonding", "Wipe kill tracker". Each row mirrors `DrawTraitRow`'s geometry (`Traits.cs:198-331`) — `TraitRowHeight` 34px + 2px gap, disabled-reason text centered in the middle gap, cost right-aligned via `DrawCostIcons(rightAlign: true, insufficientResources: insufficientResources)`, whole-row `Widgets.ButtonInvisible` select, disabled state = grayed label + reason text — plus a `Widgets.RadioButton` glyph at the left with the label beside it (the handoff's "label lhs next to the check"). The glyph is the one deliberate deviation from trait rows: a radio group has to communicate exactly-one-of, which a bare highlight can't (D19). Clicking a disabled row does nothing; the selected row also gets the `DrawBoxSolid` highlight for consistency with trait rows.
- Row tooltips: `PWU_MemoryWipeBondingDesc` (explicitly states the kill memory is erased with the bond) / `PWU_MemoryWipeKillTrackerDesc`. The kill-tracker tooltip appends the current memory when one exists ("Last kill: {0} days ago", from `comp.TicksSinceLastKill`) — this also satisfies the TODOs.md "surface the kill tracker" idea.

### Row gating (current-state, not preview-state)

The tab gates on the **preview** def state; the rows gate on the **current** weapon's comp state — you can't wipe what isn't there yet. "Do not wipe" is always enabled; when the selected op's row becomes disabled, the selection snaps back to it:

| Condition                                                                   | Wipe bonding                                                                      | Wipe kill tracker                                                                                                                                                        |
| --------------------------------------------------------------------------- | --------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Current weapon has no `CompBladelinkWeapon` (base being upgraded this spec) | disabled — `PWU_MemoryNotBonded`                                                  | disabled — `PWU_MemoryNoKillMemory`                                                                                                                                      |
| Comp present, not bonded (`!comp.Biocoded`)                                 | disabled — `PWU_MemoryNotBonded`                                                  | enabled iff `lastKillTick >= 0` (freewielders can kill without bonding), else disabled — `PWU_MemoryNoKillMemory`                                                        |
| Bonded, no `neverBond` trait staged                                         | enabled                                                                           | enabled (`lastKillTick` is always ≥ 0 once bonded — `OnCodedFor` stamps it)                                                                                              |
| Bonded, a `neverBond` trait staged for addition                             | disabled — `PWU_MemoryBondSeveredByTrait` (adding freewielder already severs, D8) | disabled — `PWU_MemoryKillWipedByTrait`: the trait add's `UnCode()` clears the kill tracker too, and additions run **before** the memory op — the wipe would buy a no-op |

Note the last row: staging freewielder onto a _bonded_ weapon disables **both** wipes (its `UnCode()` does everything either wipe would). Staging it onto an _unbonded_ weapon with kill memory triggers no `UnCode()`, so the kill wipe stays available — hence the gate is "bonded AND neverBond staged", not "neverBond staged".

`lastKillTick` is read through the existing `WeaponModificationUtility.LastKillTickField` reflection handle (already verified at startup).

No footer confirmation for wiping a bond: unlike the downgrade path's `PWU_BondSeveredWarning` (an implicit side effect), here severing **is** the explicitly selected operation.

### LHS chips

A selected wipe (at most one, by the radio model) renders as a chip beneath the trait chips, above the cost/refund summary — inside the same scroll view (`Preview.cs:72-142`), appended after the `desiredTraits` loop:

- Guard at `Preview.cs:60` widens to `(desiredTraits.Count > 0 || desiredMemoryOp != MemoryOpKind.None)`; content height (:65) becomes `(desiredTraits.Count + (desiredMemoryOp != MemoryOpKind.None ? 1 : 0)) * chipStride`.
- Styling identical to trait chips (background/hover colors, `MiddleLeft` label at the §3 fraction, cost right-aligned via `DrawCostIcons`, tooltip = the op's desc key).
- Click: `activeTab = 1` (jump to the Memory tab, mirroring how trait chips jump to the Traits tab; no scroll-to needed — the tab holds three rows).

### Spec & op model

- New `OpType.WipeMemory`; `CustomizationOp` gains a scribed `MemoryOpKind memoryOp` field (default `None`). (The radio model makes the handoff's "roll all memory operations into a single op" trivially true — at most one is ever staged.)
- **One op per spec**: `BuildOperations` appends it **after** the addition loop when `!IsRevertedToBase && desiredMemoryOp != MemoryOpKind.None`, with `cost = [ComponentSpacer × (that op's slider)]` (empty list when the slider is 0 — a free wipe still runs). No refund. Ordering rationale (D16): it operates on the spec's final persona state, and since it never changes the simulated trait count it can't perturb the §6 boundary-cost attribution.
- The cost flows through the existing generic plumbing untouched: `SumOpCosts` → `ComputeNetCostAndSurplus` → footer preview → `CustomizationSpec.totalCost` → `IngredientReservation.TryReserveIngredientsForJob` → haul planners → per-op `TryConsumeOpCost`. Zero haul-side changes.
- Work: one op = the flat `WorkTicksPerOp` 1000 ticks, like every other op.

### Execution (`ApplyOperationInner`, new case)

```csharp
case OpType.WipeMemory:
    if (!TryConsumeOpCost(op.cost)) { /* shortfall bail */ }
    CompBladelinkWeapon comp = weapon.TryGetComp<CompBladelinkWeapon>();
    if (comp == null) break;                       // defensive; op is only built for persona final states
    switch (op.memoryOp)
    {
        case MemoryOpKind.WipeBonding:
            if (comp.Biocoded)
                comp.UnCode();                     // severs bond, fires Notify_Unbonded per trait,
                                                   // strips bonded hediffs, resets lastKillTick (kill
                                                   // memory goes with the bond); biocodeOnEquip then
                                                   // re-arms bond-on-next-equip
            break;
        case MemoryOpKind.WipeKillTracker:
            LastKillTickField.SetValue(comp,
                comp.Biocoded ? Find.TickManager.TicksAbs : -1);   // D18
            break;
    }
    break;
```

D18 rationale: for a still-bonded weapon, "cleared" means a fresh kill clock (`TicksAbs`, the same 20-day grace D9 grants when adding NeedKill — writing −1 would risk an instant kill-thirst mood hit). For an unbonded weapon (a freewielder that has killed), −1 is vanilla's init/`UnCode` state. The bond wipe reaches −1 through `UnCode()` itself — the two branches are exclusive by the radio model, so the writes never stack.

Running last means `UnCode()` iterates the final trait list, so bonded hediffs of traits added earlier in the same spec are applied then correctly removed (transient but sound; the Notify machinery is PERSONA_FORK §5's).

- `GetReport()` (`JobDriver_CustomizeWeapon.cs:212-226`) gains: `case OpType.WipeMemory: return "PWU_WipingMemory".Translate(weaponLabel);` — "Wiping memory of {0}".
- `RecordOpFailureBail` gains a `WipeMemory` case (`PWU_BailOpMemoryFailed`). The shortfall-bail path currently assumes `op.trait` — give it a null-safe op label (trait label, or `PWU_MemoryWipeLabel` for memory ops).

### Settings

New section **Memory costs** (`PWU_SettingsMemoryCosts`) between "Trait costs" and "Prerequisites" (D21 — the user's copy pass renamed the cost section to "Trait costs"; memory wipes aren't trait changes, so they get their own header rather than squatting there). Two int sliders, 0–5, using the existing pattern (`"Key".Translate(value)` label + `PWU_DefaultSuffix` at the default + `Desc` tooltip + `listing.Slider` with `Mathf.RoundToInt`):

- `wipeBondingComponentCost` = 3
- `wipeKillTrackerComponentCost` = 1

Triple invariant: declaration + `ResetToDefaults()` + `ExposeData()` in UI display order with a `// Memory Costs` section comment in all three blocks, slotted between the Persona/Trait Costs and Prerequisites sections.

---

## 5. Bond confirmation on the re-equip path

### Problem

When an equipped **base** weapon is customized up to its persona variant, the job driver set `returnMode = Reequip` at job start (`JobDriver_CustomizeWeapon.cs:336-390`) and, on completion, enqueues a plain `JobDefOf.Equip` (`Recovery.cs:87-90`). That queued job bypasses the float-menu path entirely — vanilla's persona-bond confirmation lives only in `FloatMenuOptionProvider_Equip.GetSingleOptionFor`'s click delegate, never in `JobDriver_Equip` itself. The freshly converted weapon is unbonded with `biocodeOnEquip = true`, so the automatic re-equip **silently bonds it** — where a manual equip order would have paused the game and asked.

### Mechanism (no Harmony; decompiled vanilla APIs, all public)

- Predicate + text: `EquipmentUtility.GetPersonaWeaponConfirmationText(Thing, Pawn)` — returns non-null iff `comp != null && comp.Biocodable && comp.CodedPawn != pawn`. This single call already handles every case correctly: bonded-to-this-pawn → null (silent re-equip, the common customize-my-bonded-weapon flow); freewielder (`NeverBond` staged or present → `Biocodable` false) → null; downgraded to base (no comp) → null; fresh/foreign persona → warning text listing each trait + `RoyalWeaponEquipConfirmation`.
- Dialog: mirror vanilla's manual-equip dialog exactly — `Find.WindowStack.Add(new Dialog_MessageBox(text, "Yes".Translate(), equipAction, "No".Translate()))`. The `Dialog_MessageBox` ctor sets `forcePause = true` unconditionally, which is vanilla's pause mechanism. (Not `CreateConfirmation` — that's Confirm/GoBack; the persona equip dialog the player already knows is Yes/No.)

### Change (in `QueueWeaponRecoveryFor`, `Recovery.cs:85-90`)

The `Reequip` case becomes:

```csharp
case WeaponReturnMode.Reequip:
    string confirmText = EquipmentUtility.GetPersonaWeaponConfirmationText(recoverWeapon, pawn);
    if (confirmText.NullOrEmpty())
    {
        pawn.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(JobDefOf.Equip, recoverWeapon));  // unchanged fast path
        break;
    }
    Find.WindowStack.Add(new Dialog_MessageBox(confirmText, "Yes".Translate(), delegate
    {
        if (recoverWeapon.DestroyedOrNull() || !recoverWeapon.Spawned
            || pawn.DestroyedOrNull() || !pawn.Spawned || pawn.Map != recoverWeapon.Map)
            return;                                            // world changed while dialog was up
        recoverWeapon.SetForbidden(false);
        pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Equip, recoverWeapon), JobTag.Misc);
        FleckMaker.Static(recoverWeapon.DrawPos, recoverWeapon.MapHeld, FleckDefOf.FeedbackEquip);
    }, "No".Translate()));
    break;
```

- **Confirm** uses `TryTakeOrderedJob` (vanilla's manual-equip dispatch), not `EnqueueFirst`: the delegate runs from the window stack after the customize job has ended, where an ordered job is both safe and semantically right — it _is_ a player decision now. The fleck matches vanilla feedback; the tutorial `KnowledgeDemonstrated` ping is deliberately skipped (nothing was manually demonstrated). Bonding itself then happens downstream exactly as vanilla: `Notify_Equipped` → `CodeFor` → bond letter.
- **Reject** ("No", null action): nothing is queued. The weapon is already spawned at the workbench, unforbidden (dropped by the `placeWeapon` toil, forbidden cleared at job start and never re-set) — i.e. precisely the `LeaveOnWorkbench` terminal state, and the flow ends normally.
- The no-dialog path is byte-for-byte today's behavior (synchronous `EnqueueFirst` inside job teardown — `TryTakeOrderedJob` there would interrupt the ending job; don't touch it).

### Placement rationale (D20)

The check lives in `QueueWeaponRecoveryFor` rather than the success toil, so it covers **both** recovery call sites: the success path and the finish-action path that requeues after interruption. The silent-bond surprise exists on the interrupted path too — a base→persona spec whose first op has applied and is then interrupted (pawn drafted, etc.) requeues the same silent equip today.

### Edge cases

- Game saved while the dialog is open: no job was queued; on load the weapon sits at the bench like `LeaveOnWorkbench`. Acceptable — the player sees an unequipped weapon, same as a reject.
- Game is force-paused while the dialog is up, so the revalidation guard in the confirm delegate is belt-and-braces (dev-mode actions, other UI-driven mutations).
- `ReturnToInventory` mode never bonds (inventory doesn't fire `Notify_Equipped`) — untouched.

---

## 6. Settings inventory delta (`PWU_Settings`)

**Removed:** `enableIdeologyColors`, `enableStructureColors` (§2).
**Added:** `wipeBondingComponentCost` (3), `wipeKillTrackerComponentCost` (1) (§4).

Resulting section order: Progression / Trait costs (3 sliders + live cost table) / **Memory costs (2 sliders)** / Prerequisites / Crafting recipes / Ingredient hauling / Miscellaneous. Triple invariant maintained in this order in all three blocks.

---

## 7. Localization delta (`PWU_UI.xml`)

**Removed:** the 14 color keys listed in §2.
**Renamed:** `PWU_ApplyingCosmetics` → `PWU_RenamingWeapon` ("Renaming {0}"); `PWU_BailOpCosmeticsFailed` → `PWU_BailOpRenameFailed`.
**Added:**

| Key                                    | English                                                                                                              |
| -------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `PWU_TabMemory`                        | Memory                                                                                                               |
| `PWU_MemoryRequiresPersona`            | The weapon must host a persona before its memory can be wiped.                                                       |
| `PWU_MemoryNoWipe`                     | No change                                                                                                            |
| `PWU_MemoryWipeBonding`                | Wipe bonding                                                                                                         |
| `PWU_MemoryWipeBondingDesc`            | Erase the persona's bond, and its kill memory with it. The weapon returns to bonding with the next pawn to equip it. |
| `PWU_MemoryWipeKillTracker`            | Wipe kill tracker                                                                                                    |
| `PWU_MemoryWipeKillTrackerDesc`        | Erase the persona's memory of its kills. The bond is untouched. (+ "Last kill: {0} days ago" when known)             |
| `PWU_MemoryNotBonded`                  | Not bonded                                                                                                           |
| `PWU_MemoryNoKillMemory`               | No kills remembered                                                                                                  |
| `PWU_MemoryBondSeveredByTrait`         | The freewielder trait already severs the bond                                                                        |
| `PWU_MemoryKillWipedByTrait`           | Severing the bond wipes kill memory too                                                                              |
| `PWU_MemoryWipeLabel`                  | memory wipe (shortfall-bail op label)                                                                                |
| `PWU_WipingMemory`                     | Wiping memory of {0}                                                                                                 |
| `PWU_BailOpMemoryFailed`               | (op-failure bail, matching existing bail phrasing)                                                                   |
| `PWU_SettingsMemoryCosts`              | Memory costs                                                                                                         |
| `PWU_WipeBondingComponentCost`         | Bond wipe component cost: {0}                                                                                        |
| `PWU_WipeBondingComponentCostDesc`     | Advanced components charged to wipe a persona weapon's bond, returning it to bond-on-next-equip.                     |
| `PWU_WipeKillTrackerComponentCost`     | Kill tracker wipe component cost: {0}                                                                                |
| `PWU_WipeKillTrackerComponentCostDesc` | Advanced components charged to wipe a persona weapon's kill memory.                                                  |

Exact copy may be tuned during the copy review pass (TODOs.md); the key set is fixed. Final step: rerun the C#↔XML key cross-check (PERSONA_FORK §12).

---

## 8. Docs & repo sweep

- `DESIGN.md`: Core Feature 4 becomes "Rename" (drop Recolor bullet + §"Rename, Recolor" heading); dialog section swaps the Color-tab bullet for the Memory tab and notes the re-equip confirmation; drop the `forcedColor` honor claim from Mod Compatibility (dead with `CompColorable`); add the Memory operations to the cost-model section (flat component sliders).
- `PERSONA_FORK.md`: add a one-line pointer under its Status line — "§9 color verdict (D3) superseded by `MEMORY_AND_POLISH.md` §2".
- `TODOs.md` (Features): "Unbond / rebond as a paid dialog operation" → unbond half lands here; rewrite the line to keep only rebond (`CodeFor(pawn)`). "Surface the kill tracker … optional paid reset op" → lands here (tooltip + wipe op); remove. (Testing): add — memory ops in-game (wipe bonding severs + re-arms bond-on-next-equip; kill tracker reset respects D18), and the §5 dialog on the upgrade-while-equipped flow (confirm → equip + bond letter; reject → weapon stays on bench).
- `Docs/Research/TWO_COLOR_MASK_SHADER.md`: keep — its rendering research still underpins the (retained) preview blit; optionally retitle later. `CUSTOMIZATION_SYSTEM.md` retirement is already a TODO.
- Tests: no new xUnit surface — memory-op costing is a settings lookup and the op pipeline is engine-bound; the existing haul-planner suite is unaffected (cost lists remain `List<ThingDefCountClass>`).

---

## 9. Decisions

| #   | Decision                     | Resolution                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| --- | ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| D12 | Color feature                | **Removed** (reverses D3) — vanilla tint wins; delete comp patch, tab, settings, keys, forced-color path (handoff, 2026-07-11)                                                                                                                                                                                                                                                                                                                                                     |
| D13 | Tab container                | Keep the two-slot shell; Memory takes Color's `activeTab == 1` slot                                                                                                                                                                                                                                                                                                                                                                                                                |
| D14 | `ApplyCosmetics` op          | Rename to `Rename` (name-only after §2); pre-release, no scribe migration for in-flight specs                                                                                                                                                                                                                                                                                                                                                                                      |
| D15 | LHS chip split               | Label fraction `0.5f` → `0.7f` (`Preview.cs:92`); costs are single-resource in PWU                                                                                                                                                                                                                                                                                                                                                                                                 |
| D16 | Memory op shape              | The selected wipe compiles into **one** `WipeMemory` op, appended last (removals → rename → additions → memory); flat costs, no quality scaling, never crosses the persona-core boundary                                                                                                                                                                                                                                                                                           |
| D17 | Op exclusivity               | **3-way radio** — Do not wipe (default) / Wipe bonding / Wipe kill tracker (user, 2026-07-11; replaces the checkbox model): `UnCode()` resets `lastKillTick`, so the bond wipe strictly subsumes the kill wipe and "both" is not a distinct outcome — the UI must not express it                                                                                                                                                                                                   |
| D18 | Kill-tracker "cleared" value | `TicksAbs` while bonded (fresh 20-day grace, per D9's rationale); −1 when unbonded (vanilla init state); bond wipe reaches −1 via `UnCode()` itself                                                                                                                                                                                                                                                                                                                                |
| D19 | Memory gating                | Tab gates on preview state (`IsRevertedToBase` message, ex-Color-tab pattern); rows gate on current comp state with disabled-reasons — incl. **both** wipes disabling when freewielder is staged onto a bonded weapon (its `UnCode()` pre-empts either wipe); a disabled selection snaps to None via `OnTraitsChanged`; affordance = trait-row geometry + `Widgets.RadioButton` glyph (the glyph is the one deviation from trait rows — a radio group must read as exactly-one-of) |
| D20 | Bond confirm placement       | In `QueueWeaponRecoveryFor`'s `Reequip` case (covers success + interruption recovery); vanilla `GetPersonaWeaponConfirmationText` + `Dialog_MessageBox` Yes/No; confirm = revalidate + `TryTakeOrderedJob`; reject = `LeaveOnWorkbench` terminal state; no-dialog path unchanged                                                                                                                                                                                                   |
| D21 | Memory sliders' home         | New "Memory costs" settings section after "Trait costs" (which the user's copy pass scoped to trait changes)                                                                                                                                                                                                                                                                                                                                                                       |

---

## 10. Acceptance checklist (for the orchestrator)

- [ ] Builds clean; `grep -rn "CompColorable\|ColorDef\|desiredColor\|forcedColor" Source/` returns nothing; `1.6/Patches/` no longer ships `BladelinkColorable.xml`
- [ ] Dialog shows exactly two tabs (Traits, Memory), no swatch anywhere; rename works standalone (free `Rename` op) and bundled with a trait add
- [ ] LHS chips: label takes 70%; single-resource costs right-aligned; the memory chip is styled identically, click jumps to the Memory tab; the chip renders when only a wipe is selected (no trait changes)
- [ ] Memory tab: base preview state shows the centered gray message; otherwise a 3-row radio group with "Do not wipe" selected by default; exactly one row is ever selected; row gating matches the §4 table in all four conditions — in particular, staging freewielder on a bonded weapon disables **both** wipes, and a selection whose row becomes disabled snaps back to "Do not wipe"
- [ ] Wipe bonding on a bonded weapon: bond severed, bonded hediffs stripped, kill tracker cleared with it, weapon re-bonds on next equip; wipe kill tracker: bond untouched, `lastKillTick` = `TicksAbs` (still bonded) / −1 (unbonded freewielder); verb reads "Wiping memory of …"; cost = the selected op's slider in components, hauled like any other ingredients; slider at 0 → free op still runs
- [ ] Settings page: "Memory costs" section with the two sliders (defaults 3/1, range 0–5, default-suffix behavior); color toggles gone; triple invariant holds
- [ ] Upgrade-while-equipped flow: on completion the game pauses with vanilla's persona warning (traits listed); Yes → pawn equips, bond letter fires; No → weapon stays on the bench unforbidden, no job queued
- [ ] No dialog on re-equip when: weapon already bonded to the pawn, weapon downgraded to base, or weapon carries freewielder
- [ ] Dev save with a recolored weapon loads clean; the weapon shows the def tint; no scribe errors
- [ ] Localization cross-check passes (every C# key in XML and vice versa); tests pass (`./Scripts/test-windows.sh`)
