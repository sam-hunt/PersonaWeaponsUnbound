using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using PersonaWeaponsUnbound.HaulPlanning;
using UnityEngine;
using Verse;

namespace PersonaWeaponsUnbound
{
    // Dialog layout (traits tab) (950x750):
    //
    // +------------------------------------------------------------------+
    // |  Customize [Weapon Name]                                         |
    // +---------------------+--------------------------------------------+
    // |                     |   [[ Traits ]]                             |
    // |  [Graphic preview]  |  ┌───────────────────────────────────────┐ |
    // |                     |  | 🔎︎ [Search traits................][x] │ |
    // |  [Weapon Name]      |  +---------------------------------------+ |
    // |   x autoregen name  |  │ Lightweight                  ×4 steel │ │
    // |                     │  │ Gold Inlay    [conflicts]    ×50 gold │ │
    // |  Traits:            |  │ Charge Capacitor             ×3 comp  │ │
    // |   Lightweight       |  │ Pulse Charger                ×3 comp  │ │
    // |   Gold Inlay  [g]x8 │  | ...                                   │ |
    // |                     |  +---------------------------------------+ |
    // | Cost: [gold]x8      |  │ [x] Show negative traits              │ |
    // | Refund: [steel]×4   |  └───────────────────────────────────────┘ |
    // +---------------------+--------------------------------------------+
    // |  [Cancel]                  [Reset]                  [Confirm]    |
    // +------------------------------------------------------------------+
    //
    // Left pane (35%): Preview.cs — weapon icon, name, status, trait list with [x]
    // Right pane (65%): Controls.cs — name field, tab bar, tab content
    //   Traits tab: Traits.cs — scrollable checkbox list with per-trait costs and rejection reasons
    // Footer: Footer.cs — Cancel/Reset/Confirm buttons (ideology styling station layout)

    public partial class Dialog_WeaponCustomization : Window
    {
        // Reflection: delegate to WeaponModificationUtility which owns the FieldInfo static.
        // CompNameField targets CompGeneratedNames.name (the persona weapon's name).
        private static FieldInfo CompNameField => WeaponModificationUtility.CompNameField;

        // Immutable state — set in constructor, never modified
        private readonly Pawn pawn;
        private readonly Thing weapon;
        private readonly Building_WorkTable workbench;
        private readonly ThingDef personaDef;
        private readonly ThingDef baseDef; // null if persona weapon has no detected base
        private readonly List<WeaponTraitDef> originalTraits;
        private readonly List<WeaponTraitDef> compatibleTraits;
        private readonly string originalName;
        private readonly bool isRelic; // Ideology DLC: weapon is an ideoligion relic
        private readonly Ideo relicIdeo; // Ideology DLC: the ideoligion this relic belongs to

        // Snapshot of player-discoverable trait sources at construction time.
        // Only populated when the progression-mode setting is enabled; null otherwise.
        // See <see cref="TraitProgressionPool"/> for the scan rules.
        private readonly TraitProgressionPool progressionPool;

        // Desired state — mutated by user interaction
        private readonly List<WeaponTraitDef> desiredTraits;
        private string desiredName;

        // UI state
        private Vector2 traitListScroll;
        private Vector2 desiredTraitsScroll;
        private readonly QuickSearchWidget traitSearchWidget = new QuickSearchWidget();
        private int activeTab; // 0 = Traits (slot 1 reserved for a future Memory tab)
        private bool nameLocked;
        private bool hideNegativeTraits;
        private string lastAutoName;

        // Latch flipped by the DoWindowContents catch handler. Close() schedules
        // teardown but the window can still receive one more draw call before
        // it's removed from the stack — this guard short-circuits that frame so
        // a recurring render-loop exception can't spam the log or fire the
        // player-visible Messages.Message twice.
        private bool renderErrored;

        // Affordability state — recomputed each frame in DoWindowContents
        private HashSet<ThingDef> insufficientResources;
        private Dictionary<ThingDef, int> committedResources;
        private List<ThingDefCountClass> currentNetCost;
        private List<ThingDefCountClass> currentSurplus;
        private List<ThingDefCountClass> currentTotalRefund;
        private Dictionary<ThingDef, int> surplusBalance;

        // Available resource counts are stable for the dialog's lifetime:
        // forcePause halts pawn AI and absorbInputAroundWindow blocks
        // forbid/allowed-area edits, so stacks, reachability, and reservations
        // can't change. Populated lazily on first access per def.
        private readonly Dictionary<ThingDef, int> availableResources =
            new Dictionary<ThingDef, int>();

        // Layout constants
        private static readonly Vector2 ButtonSize = new Vector2(120f, 40f);
        private const float LeftPanePct = 0.35f;
        private const float TitleHeight = 40f;
        private const float GapBelowTitle = 10f;
        private const float FooterHeight = 50f;
        private const float PaneGap = 4f;
        private const float TraitRowHeight = 34f;
        private const float TraitRowGap = 2f;
        private const float SectionHeaderHeight = 30f;
        private const float RemoveButtonSize = 20f;
        private const float CostIconSize = 24f;
        private const float ControlRowHeight = 30f;
        private const float ControlRowGap = 4f;
        private const float NameFieldHeight = 35f;
        private const float ArrowButtonWidth = 28f;
        private const float RandomButtonWidth = 85f;
        private const float TabBarHeight = 32f;
        private const float ControlLabelWidth = 60f;

        public override Vector2 InitialSize => new Vector2(950f, 750f);

        public Dialog_WeaponCustomization(
            Pawn pawn, Thing weapon, Building_WorkTable workbench)
        {
            this.pawn = pawn;
            this.weapon = weapon;
            this.workbench = workbench;

            forcePause = true;
            closeOnAccept = false;
            closeOnCancel = false;
            doCloseX = true;
            absorbInputAroundWindow = true;
            onlyOneOfTypeAllowed = true;

            // Determine persona/base defs
            if (WeaponRegistry.IsPersonaWeapon(weapon.def))
            {
                personaDef = weapon.def;
                baseDef = WeaponRegistry.GetBaseVariant(weapon.def);
            }
            else
            {
                baseDef = weapon.def;
                personaDef = WeaponRegistry.GetPersonaVariant(weapon.def);
            }

            // Snapshot original traits from the live bladelink comp
            CompBladelinkWeapon bladelinkComp = weapon.TryGetComp<CompBladelinkWeapon>();
            if (bladelinkComp != null && bladelinkComp.TraitsListForReading != null)
                originalTraits = new List<WeaponTraitDef>(bladelinkComp.TraitsListForReading);
            else
                originalTraits = new List<WeaponTraitDef>();
            // Note: base weapons start with empty originalTraits — user can only add.

            desiredTraits = new List<WeaponTraitDef>(originalTraits);

            // Cache the full compatible trait list (all bladelink traits)
            compatibleTraits = TraitValidationUtility.GetCompatibleTraits(personaDef);

            // Progression mode: snapshot the player's currently-known trait pool so the
            // RHS list can hide traits the player can't yet see and disable traits only
            // available on hostile-held weapons. Snapshot is dialog-lifetime — see
            // availableResources comment for why it can't change while we're open.
            if (PWU_Mod.Settings.restrictTraitsToDiscovered)
                progressionPool = TraitProgressionPool.Build();

            // Default to hiding negative traits unless the weapon already has one
            hideNegativeTraits = !originalTraits.Any(t => TraitCostUtility.IsNegativeTrait(t));

            // Snapshot original name via reflection (CompGeneratedNames.name)
            CompGeneratedNames nameComp = weapon.TryGetComp<CompGeneratedNames>();
            if (nameComp != null && CompNameField != null)
                originalName = (string)CompNameField.GetValue(nameComp) ?? "";
            else
                originalName = "";
            desiredName = originalName;
            nameLocked = !string.IsNullOrEmpty(originalName);

            // Ideology DLC: if the weapon is a relic, use the precept's display name
            // as the desired name. This writes the relic name into CompGeneratedNames.name
            // so it persists even if relic status is later revoked via ideology reform.
            // The name field is disabled for relics — editing happens via form/reform.
            if (ModsConfig.IdeologyActive && weapon.StyleSourcePrecept is Precept_Relic relicPrecept)
            {
                isRelic = true;
                relicIdeo = relicPrecept.ideo;
                desiredName = relicPrecept.LabelCap;
                nameLocked = true;
            }
        }

        // --- Computed properties ---

        private ThingDef ResultingDef
        {
            get
            {
                if (desiredTraits.Count > 0)
                    return personaDef;
                // No desired traits — revert to base if one exists
                if (baseDef != null && PWU_Mod.Settings.allowDefConversion)
                    return baseDef;
                // Persona weapon with no detected base — keep persona def with zero traits.
                // This handles edge cases where a persona weapon has no base weapon mapping.
                return personaDef;
            }
        }

        /// <summary>
        /// True when weapon will revert to its base def (no traits, base exists).
        /// Name controls are disabled in this state.
        /// </summary>
        private bool IsRevertedToBase => desiredTraits.Count == 0 && baseDef != null && PWU_Mod.Settings.allowDefConversion;

        /// <summary>
        /// True when the 0↔1 trait-count boundary actually converts the def (a base
        /// pairing exists and def conversion is enabled). When false — orphan persona
        /// weapon or conversion disabled — the weapon keeps its persona def at zero
        /// traits, so no change ever crosses the boundary: the persona core is never
        /// charged nor refunded and every change is priced as components (spec §6
        /// rules 1–2 tie the core strictly to actual def conversion).
        /// </summary>
        private bool ConversionAvailable => baseDef != null && PWU_Mod.Settings.allowDefConversion;

        /// <summary>
        /// True when the weapon is currently bonded (biocoded) to a pawn. Drives the
        /// NeverBond "severs the bond" warning and the footer bond-severed confirm.
        /// </summary>
        private bool WeaponIsBonded
        {
            get
            {
                CompBladelinkWeapon comp = weapon.TryGetComp<CompBladelinkWeapon>();
                return comp != null && comp.Biocoded && comp.CodedPawn != null;
            }
        }

        /// <summary>
        /// Display label of the pawn the weapon is bonded to, or empty if unbonded.
        /// </summary>
        private string BondedPawnLabel
        {
            get
            {
                CompBladelinkWeapon comp = weapon.TryGetComp<CompBladelinkWeapon>();
                if (comp == null || !comp.Biocoded || comp.CodedPawn == null)
                    return "";
                return !string.IsNullOrEmpty(comp.CodedPawnLabel)
                    ? comp.CodedPawnLabel
                    : comp.CodedPawn.LabelShortCap;
            }
        }

        private bool HasChanges
        {
            get
            {
                if (desiredTraits.Count != originalTraits.Count)
                    return true;
                for (int i = 0; i < desiredTraits.Count; i++)
                {
                    if (desiredTraits[i] != originalTraits[i])
                        return true;
                }
                if (desiredName != originalName)
                    return true;
                return false;
            }
        }

        private IEnumerable<WeaponTraitDef> TraitsToAdd =>
            desiredTraits.Where(t => !originalTraits.Contains(t));

        private IEnumerable<WeaponTraitDef> TraitsToRemove =>
            originalTraits.Where(t => !desiredTraits.Contains(t));

        // --- Actions ---

        private void ResetToOriginal()
        {
            desiredTraits.Clear();
            desiredTraits.AddRange(originalTraits);
            desiredName = originalName;
            nameLocked = !string.IsNullOrEmpty(originalName);
            lastAutoName = null;
            traitListScroll = Vector2.zero;
            desiredTraitsScroll = Vector2.zero;
        }

        private void OnTraitsChanged()
        {
            if (!nameLocked && !isRelic && desiredTraits.Count > 0 && !IsRevertedToBase)
            {
                string regenerated = GenerateWeaponName();
                if (regenerated != null)
                {
                    desiredName = regenerated;
                    lastAutoName = desiredName;
                }
            }
        }

        // --- Helpers ---

        /// <summary>
        /// Returns the available count for a material on the map, cached for
        /// the dialog's lifetime. See <see cref="availableResources"/> for why
        /// the count is stable while the dialog is open.
        /// </summary>
        private int GetAvailableCount(ThingDef thingDef)
        {
            if (availableResources.TryGetValue(thingDef, out int count))
                return count;
            count = IngredientReservation.CountAvailable(pawn.Map, thingDef, pawn);
            availableResources[thingDef] = count;
            return count;
        }

        /// <summary>
        /// Returns the set of insufficient materials if this trait's cost were added
        /// on top of the currently committed resources. Accounts for unused refund
        /// surplus that can offset the hypothetical trait's cost. Returns null if
        /// fully affordable.
        /// </summary>
        private HashSet<ThingDef> GetHypotheticalInsufficient(List<ThingDefCountClass> traitCosts)
        {
            if (traitCosts == null || traitCosts.Count == 0)
                return null;

            HashSet<ThingDef> result = null;
            foreach (ThingDefCountClass cost in traitCosts)
            {
                committedResources.TryGetValue(cost.thingDef, out int committed);
                int needed = committed + cost.count;

                // Subtract unused refund surplus — these resources will be
                // available via the virtual ledger even though they aren't on the map
                if (surplusBalance.TryGetValue(cost.thingDef, out int surplus))
                    needed = Mathf.Max(0, needed - surplus);

                if (GetAvailableCount(cost.thingDef) < needed)
                {
                    if (result == null)
                        result = new HashSet<ThingDef>();
                    result.Add(cost.thingDef);
                }
            }
            return result;
        }

        /// <summary>
        /// Number of the weapon's original traits still kept in <see cref="desiredTraits"/>
        /// (i.e. not staged for removal). Boundary-crossing during the removal phase of
        /// <see cref="BuildOperations"/> depends on this count, not <c>desiredTraits.Count</c>
        /// as a whole — new additions staged alongside a removal haven't happened yet at
        /// the point removals are processed, so they must not inflate the denominator.
        /// </summary>
        private int KeptOriginalsCount => originalTraits.Count(t => desiredTraits.Contains(t));

        /// <summary>
        /// Preview cost for adding a not-yet-selected <paramref name="trait"/> right now,
        /// appended after every other currently staged addition: crosses the base→persona
        /// boundary exactly when no original traits remain kept and no addition is staged
        /// yet (i.e. <c>desiredTraits</c> is currently empty). Used for the "unselected"
        /// row hover preview in Traits.cs. A trait already staged as an addition should
        /// use <see cref="StagedAdditionCost"/> instead, which reflects its real position
        /// in the addition sequence rather than re-deriving it in isolation.
        /// </summary>
        private List<ThingDefCountClass> PreviewAdditionCost(WeaponTraitDef trait)
        {
            bool crossesBoundary = desiredTraits.Count == 0 && ConversionAvailable;
            return TraitCostUtility.GetChangeCost(weapon, crossesBoundary, isRemoval: false);
        }

        /// <summary>
        /// The real cost of a trait already staged as an addition (in <see cref="desiredTraits"/>,
        /// not in <see cref="originalTraits"/>). Only the first staged addition can cross the
        /// base→persona boundary, and only when no original traits remain kept — mirrors
        /// <see cref="BuildOperations"/>'s addition loop exactly, so this matches the price
        /// that trait's real op will carry at confirm time.
        /// </summary>
        private List<ThingDefCountClass> StagedAdditionCost(WeaponTraitDef trait)
        {
            List<WeaponTraitDef> stagedAdds = TraitsToAdd.ToList();
            bool crossesBoundary = KeptOriginalsCount == 0 && ConversionAvailable
                && stagedAdds.Count > 0 && stagedAdds[0] == trait;
            return TraitCostUtility.GetChangeCost(weapon, crossesBoundary, isRemoval: false);
        }

        /// <summary>
        /// Preview cost for removing a still-kept original <paramref name="trait"/> right
        /// now, in isolation from any other staged change: crosses the persona→base
        /// boundary exactly when this is the last original trait still kept.
        /// </summary>
        private List<ThingDefCountClass> PreviewRemovalCost(WeaponTraitDef trait)
        {
            bool crossesBoundary = KeptOriginalsCount == 1 && ConversionAvailable;
            return TraitCostUtility.GetChangeCost(weapon, crossesBoundary, isRemoval: true);
        }

        /// <summary>
        /// Preview refund for removing a still-kept original <paramref name="trait"/> right
        /// now, mirroring <see cref="PreviewRemovalCost"/>'s boundary check.
        /// </summary>
        private List<ThingDefCountClass> PreviewRemovalRefund(WeaponTraitDef trait)
        {
            bool crossesBoundary = KeptOriginalsCount == 1 && ConversionAvailable;
            return TraitCostUtility.GetChangeRefund(crossesBoundary, isRemoval: true);
        }

        /// <summary>
        /// Builds the ordered operation list (removals → rename → additions),
        /// pricing each op by sequential simulation: a running trait count starting
        /// at the weapon's current trait count crosses the base↔persona boundary on
        /// whichever op actually takes it to/from zero, and only that op is priced
        /// as the persona-core install/refund — every other op costs advanced
        /// components (fork spec §6 rule 4). Pure and side-effect-free, so it's
        /// cheap enough to rebuild every frame; shared by the live cost preview
        /// (<see cref="DoWindowContentsInner"/>) and the confirmed spec
        /// (<see cref="BuildCustomizationSpec"/>).
        /// </summary>
        private List<CustomizationOp> BuildOperations()
        {
            var ops = new List<CustomizationOp>();
            List<WeaponTraitDef> removes = TraitsToRemove.ToList();
            List<WeaponTraitDef> adds = TraitsToAdd.ToList();

            int simulatedTraitCount = originalTraits.Count;

            // 1. Removal ops
            var remainingOriginalTraits = new List<WeaponTraitDef>(originalTraits);
            foreach (WeaponTraitDef trait in removes)
            {
                bool crossesBoundary = simulatedTraitCount == 1 && ConversionAvailable;
                var op = new CustomizationOp
                {
                    type = OpType.RemoveTrait,
                    trait = trait,
                    cost = TraitCostUtility.GetChangeCost(weapon, crossesBoundary, isRemoval: true),
                    refund = TraitCostUtility.GetChangeRefund(crossesBoundary, isRemoval: true),
                };
                simulatedTraitCount--;

                remainingOriginalTraits.Remove(trait);

                ops.Add(op);
            }

            // 2. Rename op (only if result is persona)
            // If the weapon will be in base state after removals (all original
            // traits removed) and there are additions, a rename can't apply to
            // a base weapon. Merge it into the first AddTrait op instead, which
            // will convert base→persona and then apply the rename atomically.
            // A rename can only apply to a persona weapon. We must defer it onto
            // the first AddTrait op if the weapon will be in base state when the
            // rename step would run. Two cases:
            // (a) Weapon starts as persona but all original traits are removed → base
            // (b) Weapon starts as base → already in base state
            bool willBeBaseAfterRemovals = remainingOriginalTraits.Count == 0
                && WeaponRegistry.IsPersonaWeapon(weapon.def)
                && ConversionAvailable;
            bool startsAsBase = !WeaponRegistry.IsPersonaWeapon(weapon.def)
                && PWU_Mod.Settings.allowDefConversion;
            bool deferRename = (willBeBaseAfterRemovals || startsAsBase) && adds.Count > 0;

            string deferredName = null;

            if (ResultingDef == personaDef)
            {
                bool nameChanged = desiredName != originalName;

                if (deferRename)
                {
                    // Always save the desired name when deferring. The round-trip
                    // through base state (persona→base→persona) destroys the comps,
                    // so the existing name is lost even if unchanged by the player.
                    // It must be re-applied after the first AddTrait converts back
                    // to persona.
                    deferredName = desiredName;
                }
                else if (nameChanged)
                {
                    ops.Add(new CustomizationOp
                    {
                        type = OpType.Rename,
                        nameToApply = desiredName,
                    });
                }
            }

            // 3. Addition ops
            bool firstAdd = true;
            foreach (WeaponTraitDef trait in adds)
            {
                bool crossesBoundary = simulatedTraitCount == 0 && ConversionAvailable;
                var op = new CustomizationOp
                {
                    type = OpType.AddTrait,
                    trait = trait,
                    cost = TraitCostUtility.GetChangeCost(weapon, crossesBoundary, isRemoval: false),
                };
                simulatedTraitCount++;

                // Merge a deferred rename into the first AddTrait op
                if (firstAdd && deferRename)
                {
                    op.nameToApply = deferredName;
                    firstAdd = false;
                }

                ops.Add(op);
            }

            return ops;
        }

        /// <summary>
        /// Sums a per-op cost or refund list (selected via <paramref name="selector"/>)
        /// across every op, collapsing duplicate ThingDefs.
        /// </summary>
        private static List<ThingDefCountClass> SumOpCosts(
            List<CustomizationOp> ops, Func<CustomizationOp, List<ThingDefCountClass>> selector)
        {
            var totals = new Dictionary<ThingDef, int>();
            foreach (CustomizationOp op in ops)
            {
                List<ThingDefCountClass> entries = selector(op);
                if (entries == null)
                    continue;
                foreach (ThingDefCountClass entry in entries)
                {
                    totals.TryGetValue(entry.thingDef, out int existing);
                    totals[entry.thingDef] = existing + entry.count;
                }
            }
            return totals.Select(kv => new ThingDefCountClass(kv.Key, kv.Value)).ToList();
        }

        /// <summary>
        /// Computes net cost and net surplus by subtracting refunds from costs per-material.
        /// Net cost contains materials where addition costs exceed refunds.
        /// Net surplus contains materials where refunds exceed addition costs (or appear
        /// only in refunds). These are what the player actually receives back.
        /// </summary>
        private static void ComputeNetCostAndSurplus(
            List<ThingDefCountClass> costs, List<ThingDefCountClass> refunds,
            out List<ThingDefCountClass> netCost, out List<ThingDefCountClass> netSurplus)
        {
            if (refunds == null || refunds.Count == 0)
            {
                netCost = costs;
                netSurplus = new List<ThingDefCountClass>();
                return;
            }

            var net = new Dictionary<ThingDef, int>();
            foreach (ThingDefCountClass cost in costs)
                net[cost.thingDef] = cost.count;
            foreach (ThingDefCountClass refund in refunds)
            {
                if (net.ContainsKey(refund.thingDef))
                    net[refund.thingDef] -= refund.count;
                else
                    net[refund.thingDef] = -refund.count;
            }

            netCost = new List<ThingDefCountClass>();
            netSurplus = new List<ThingDefCountClass>();
            foreach (KeyValuePair<ThingDef, int> kv in net)
            {
                if (kv.Value > 0)
                    netCost.Add(new ThingDefCountClass(kv.Key, kv.Value));
                else if (kv.Value < 0)
                    netSurplus.Add(new ThingDefCountClass(kv.Key, -kv.Value));
            }
        }

        // --- Main drawing ---

        public override void DoWindowContents(Rect inRect)
        {
            if (renderErrored)
                return;
            try
            {
                DoWindowContentsInner(inRect);
            }
            catch (Exception ex)
            {
                renderErrored = true;
                // Per-frame surfaces (cost pipelines, modded trait workers,
                // the JobDriver cast in the Confirm path) can throw inside
                // vanilla's window pump. Without this guard the dialog would
                // be torn down by vanilla with only a stack trace in the log:
                // the JobDriver's tickAction then sees window-closed +
                // null-spec and bails silently. Surface a player-visible
                // message and close cleanly so the bail is attributable.
                string label;
                try { label = weapon?.LabelShortCap ?? "(unknown weapon)"; }
                catch { label = weapon?.def?.defName ?? "(unknown weapon)"; }
                Log.Error("[Persona Weapons Unbound] Customization dialog errored for "
                    + label + ": " + ex);
                Messages.Message(
                    "PWU_DialogErrored".Translate(label),
                    weapon, MessageTypeDefOf.NegativeEvent, historical: false);
                Close();
            }
        }

        private void DoWindowContentsInner(Rect inRect)
        {
            // Compute affordability state for cost coloring across all draw calls.
            // Sequential simulation (§6 rule 4) means costs/refunds depend on the
            // full staged op order, not just the individual trait — so the preview
            // rebuilds the same op list the confirmed spec will use.
            List<CustomizationOp> previewOps = BuildOperations();
            List<ThingDefCountClass> frameCost = SumOpCosts(previewOps, op => op.cost);
            currentTotalRefund = SumOpCosts(previewOps, op => op.refund);
            ComputeNetCostAndSurplus(frameCost, currentTotalRefund,
                out currentNetCost, out currentSurplus);

            surplusBalance = new Dictionary<ThingDef, int>();
            if (currentSurplus != null)
            {
                foreach (ThingDefCountClass surplus in currentSurplus)
                    surplusBalance[surplus.thingDef] = surplus.count;
            }

            committedResources = new Dictionary<ThingDef, int>();
            insufficientResources = null;
            foreach (ThingDefCountClass cost in currentNetCost)
            {
                committedResources[cost.thingDef] = cost.count;
                int available = GetAvailableCount(cost.thingDef);
                if (available < cost.count)
                {
                    if (insufficientResources == null)
                        insufficientResources = new HashSet<ThingDef>();
                    insufficientResources.Add(cost.thingDef);
                }
            }

            // Title
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, TitleHeight);
            string titleLabel = "PWU_CustomizeWeapon".Translate(weapon.LabelShortCap);
            Widgets.Label(titleRect, titleLabel);
            Text.Font = GameFont.Small;

            // Content area between title and footer
            float contentTop = inRect.y + TitleHeight + GapBelowTitle;
            float contentHeight = inRect.height - TitleHeight - GapBelowTitle - FooterHeight;
            Rect contentRect = new Rect(inRect.x, contentTop, inRect.width, contentHeight);

            // Left pane: weapon preview
            Rect leftPane = new Rect(
                contentRect.x,
                contentRect.y,
                contentRect.width * LeftPanePct,
                contentRect.height);
            DrawWeaponPreview(leftPane);

            // Right pane: controls
            Rect rightPane = new Rect(
                leftPane.xMax + PaneGap,
                contentRect.y,
                contentRect.width - leftPane.width - PaneGap,
                contentRect.height);
            DrawControlsPanel(rightPane);

            // Footer
            DrawFooter(inRect);
        }
    }
}
