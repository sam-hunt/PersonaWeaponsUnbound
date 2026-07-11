using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PersonaWeaponsUnbound
{
    // Work phase: ledger reads/writes against placedIngredients (populated
    // by the Haul phase) and the refundLedger float credits, then per-op
    // mutation of the weapon Thing. Pays each op's cost from the ledger
    // before applying the trait/rename/memory-wipe change, and converts
    // base<->persona atomically when the trait count crosses the 0<->1
    // boundary. A throw inside an op bails the whole job rather than
    // continuing onto the next op (which could blow trait limits or
    // depend on a refund credit that never materialised).
    public partial class JobDriver_CustomizeWeapon
    {
        /// <summary>
        /// Consumes resources from the tracked placedIngredients list rather than
        /// scanning nearby cells. Mirrors vanilla's pattern of consuming from
        /// job.placedThings. Destroyed stacks are removed from the list.
        /// </summary>
        private bool ConsumeFromPlacedIngredients(List<ThingDefCountClass> costs)
        {
            foreach (ThingDefCountClass cost in costs)
            {
                int remaining = cost.count;
                for (int i = placedIngredients.Count - 1; i >= 0 && remaining > 0; i--)
                {
                    Thing stack = placedIngredients[i];
                    if (stack.Destroyed || !stack.Spawned || stack.def != cost.thingDef)
                        continue;

                    int take = Mathf.Min(remaining, stack.stackCount);
                    remaining -= take;

                    if (take >= stack.stackCount)
                    {
                        stack.Destroy();
                        placedIngredients.RemoveAt(i);
                    }
                    else
                    {
                        stack.SplitOff(take).Destroy();
                    }
                }

                if (remaining > 0)
                {
                    Log.Warning($"[Persona Weapons Unbound] Could not consume all " +
                        $"{cost.thingDef.LabelCap} from placed ingredients: " +
                        $"needed {cost.count}, short by {remaining}.");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the total reservable count of <paramref name="thingDef"/> across all
        /// currently placed ingredient stacks, ignoring destroyed/despawned ones.
        /// </summary>
        private int CountInPlaced(ThingDef thingDef)
        {
            int available = 0;
            for (int i = 0; i < placedIngredients.Count; i++)
            {
                Thing stack = placedIngredients[i];
                if (stack.Destroyed || !stack.Spawned || stack.def != thingDef)
                    continue;
                available += stack.stackCount;
            }
            return available;
        }

        /// <summary>
        /// Returns true if an op's cost could currently be paid from the refund
        /// ledger plus placed ingredients, without committing any state. Used as a
        /// pre-flight check before starting an op's work cycle so the pawn doesn't
        /// waste 1000 ticks of work on an op we already know will abort.
        /// </summary>
        private bool CanAffordOpCost(List<ThingDefCountClass> opCost)
        {
            if (opCost == null || opCost.Count == 0)
                return true;

            foreach (ThingDefCountClass cost in opCost)
            {
                int remaining = cost.count;
                if (refundLedger.TryGetValue(cost.thingDef, out float credit) && credit > 0f)
                    remaining -= Mathf.Min(remaining, Mathf.FloorToInt(credit));
                if (remaining > 0 && CountInPlaced(cost.thingDef) < remaining)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Pays an op's cost: debits the refund ledger first, then consumes the
        /// remainder from placed ingredient stacks at the workbench. Pre-checks
        /// availability and only commits if the cost can be fully paid, so a
        /// shortfall (e.g. ingredients destroyed by fire/explosion/deterioration)
        /// leaves the ledger and the weapon untouched. Returns false on shortfall —
        /// caller should notify the player and abort the job.
        /// </summary>
        private bool TryConsumeOpCost(List<ThingDefCountClass> opCost)
        {
            if (opCost == null || opCost.Count == 0)
                return true;

            // First pass: compute what we'd take from the ledger and from placed
            // ingredients, without committing.
            var fromPlaced = new List<ThingDefCountClass>();
            var pendingDebit = new Dictionary<ThingDef, int>();
            foreach (ThingDefCountClass cost in opCost)
            {
                int remaining = cost.count;
                if (refundLedger.TryGetValue(cost.thingDef, out float credit) && credit > 0f)
                {
                    int debit = Mathf.Min(remaining, Mathf.FloorToInt(credit));
                    if (debit > 0)
                    {
                        pendingDebit[cost.thingDef] = debit;
                        remaining -= debit;
                    }
                }
                if (remaining > 0)
                    fromPlaced.Add(new ThingDefCountClass(cost.thingDef, remaining));
            }

            // Verify the placed-ingredient remainder can be satisfied before
            // mutating any state.
            foreach (ThingDefCountClass need in fromPlaced)
            {
                if (CountInPlaced(need.thingDef) < need.count)
                    return false;
            }

            // Commit ledger debits and ingredient consumption.
            foreach (KeyValuePair<ThingDef, int> kv in pendingDebit)
                refundLedger[kv.Key] -= kv.Value;
            if (fromPlaced.Count > 0)
                ConsumeFromPlacedIngredients(fromPlaced);
            return true;
        }

        private void ApplyOperation(CustomizationOp op)
        {
            try
            {
                ApplyOperationInner(op);
            }
            catch (Exception ex)
            {
                // The weapon may be in a partial state — e.g. cost paid but
                // trait not yet added, or trait added but ability comp not
                // wired. Continuing would compound the damage: a failed remove
                // leaves the trait in place (and no refund credit to the
                // ledger), so a subsequent add could push the count past the
                // trait limit and/or run short on materials the refund was
                // funding. Bail here; placed ingredients consumed by
                // TryConsumeOpCost prior to the throw are not recovered.
                RecordOpFailureBail(op, ex);
                EndJobWith(JobCondition.Incompletable);
            }
        }

        /// <summary>
        /// Records a structured log line plus a translated, op-type-specific
        /// bail message for an unexpected throw inside ApplyOperation. The log
        /// names the op index, op type, trait defName, and weapon defName so
        /// post-mortem triage doesn't have to reconstruct the failing op from
        /// the surrounding toil context. The bail message is routed through
        /// the first-set-wins <see cref="SetBailMessage"/> channel so a cascade
        /// failure can't overwrite the original cause.
        /// </summary>
        private void RecordOpFailureBail(CustomizationOp op, Exception ex)
        {
            string opDescr;
            string bailMessageText;
            switch (op.type)
            {
                case OpType.AddTrait:
                    opDescr = "adding trait " + (op.trait?.defName ?? "(null)");
                    bailMessageText = "PWU_BailOpAddTraitFailed".Translate(
                        WeaponLabel, op.trait?.LabelCap ?? "");
                    break;
                case OpType.RemoveTrait:
                    opDescr = "removing trait " + (op.trait?.defName ?? "(null)");
                    bailMessageText = "PWU_BailOpRemoveTraitFailed".Translate(
                        WeaponLabel, op.trait?.LabelCap ?? "");
                    break;
                case OpType.Rename:
                    opDescr = "renaming weapon";
                    bailMessageText = "PWU_BailOpRenameFailed".Translate(WeaponLabel);
                    break;
                case OpType.WipeMemory:
                    opDescr = "wiping memory (" + op.memoryOp + ")";
                    bailMessageText = "PWU_BailOpMemoryFailed".Translate(WeaponLabel);
                    break;
                case OpType.Restyle:
                    opDescr = "restyling weapon (VPWE/VEF texture)";
                    bailMessageText = "PWU_BailOpRestyleFailed".Translate(WeaponLabel);
                    break;
                default:
                    opDescr = "op type " + op.type;
                    bailMessageText = "PWU_BailUnexpected".Translate(WeaponLabel);
                    break;
            }

            int totalOps = spec?.operations?.Count ?? -1;
            string weaponDefName = weapon?.def?.defName
                ?? job?.GetTarget(WeaponIndex).Thing?.def?.defName
                ?? "(null)";
            Log.Error("[Persona Weapons Unbound] Customization aborted while " + opDescr
                + " on " + WeaponLabel + " [" + weaponDefName + "] "
                + "(op " + (currentOpIndex + 1) + "/" + totalOps + "): " + ex);
            SetBailMessage(bailMessageText);
        }

        private void ApplyOperationInner(CustomizationOp op)
        {
            switch (op.type)
            {
                case OpType.RemoveTrait:
                    // Non-boundary removals carry a component cost (op.cost is empty
                    // for the boundary-crossing removal that refunds the persona core
                    // instead — §6). Pay it before removing the trait so a placed-
                    // ingredient shortfall can't leave the trait already gone with no
                    // payment recorded.
                    if (!TryConsumeOpCost(op.cost))
                    {
                        RecordShortfallBail(op);
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    WeaponModificationUtility.RemoveTrait(weapon, op.trait);

                    // Credit refund to the virtual ledger atomically with the removal.
                    // Under the persona cost model the only refund is the whole AI
                    // persona core paid on a boundary-crossing removal (§6) — no
                    // rates or multipliers to apply, so the op's raw refund count
                    // is credited as-is.
                    if (op.refund != null)
                    {
                        foreach (ThingDefCountClass refund in op.refund)
                        {
                            if (refundLedger.ContainsKey(refund.thingDef))
                                refundLedger[refund.thingDef] += refund.count;
                            else
                                refundLedger[refund.thingDef] = refund.count;
                        }
                    }

                    // If removing the last trait, convert persona→base atomically
                    CompBladelinkWeapon removeComp = weapon.TryGetComp<CompBladelinkWeapon>();
                    if (removeComp != null && removeComp.TraitsListForReading.Count == 0
                        && PWU_Mod.Settings.allowDefConversion)
                    {
                        ThingDef baseDef = WeaponRegistry.GetBaseVariant(weapon.def);
                        if (baseDef != null)
                            ConvertWeaponInPlace(baseDef);
                    }
                    break;

                case OpType.Rename:
                    if (weapon.TryGetComp<CompBladelinkWeapon>() != null)
                    {
                        if (op.nameToApply != null)
                            WeaponModificationUtility.SetName(weapon, op.nameToApply);
                    }
                    break;

                case OpType.AddTrait:
                    // Pay the cost first — if placed ingredients have been destroyed
                    // (fire, explosion, deterioration), abort cleanly before any
                    // mutation (def conversion, trait add) leaves a partial state.
                    if (!TryConsumeOpCost(op.cost))
                    {
                        RecordShortfallBail(op);
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    // If weapon is currently base, convert base→persona first.
                    // AddTrait then bonds naturally on next equip (biocodeOnEquip);
                    // the fresh persona weapon must not be pre-bonded (spec §4).
                    if (!WeaponRegistry.IsPersonaWeapon(weapon.def) && PWU_Mod.Settings.allowDefConversion)
                    {
                        ThingDef personaDef = WeaponRegistry.GetPersonaVariant(weapon.def);
                        if (personaDef != null)
                            ConvertWeaponInPlace(personaDef);
                    }

                    WeaponModificationUtility.AddTrait(weapon, op.trait);

                    // Apply a bundled rename (merged from a Rename op that would
                    // have been a no-op when the weapon was in base state)
                    if (weapon.TryGetComp<CompBladelinkWeapon>() != null)
                    {
                        if (op.nameToApply != null)
                            WeaponModificationUtility.SetName(weapon, op.nameToApply);
                    }
                    break;

                case OpType.WipeMemory:
                    // Pay the flat component cost first, mirroring the trait ops.
                    if (!TryConsumeOpCost(op.cost))
                    {
                        RecordShortfallBail(op);
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    CompBladelinkWeapon memoryComp = weapon.TryGetComp<CompBladelinkWeapon>();

                    // defensive; op is only built for persona final states
                    if (memoryComp == null)
                        break;

                    switch (op.memoryOp)
                    {
                        case MemoryOpKind.WipeBonding:
                            if (memoryComp.Biocoded)
                            {
                                // severs bond, fires Notify_Unbonded per trait, strips bonded hediffs,
                                // resets lastKillTick (kill memory goes with the bond); biocodeOnEquip
                                // then re-arms bond-on-next-equip
                                memoryComp.UnCode();

                                // A pawn who walked in wearing this weapon had it auto-
                                // queued to reequip; wiping the bond mid-job means an
                                // auto-reequip would just re-bond it right back, defeating
                                // the point of the wipe. Once the wipe actually lands,
                                // leave the weapon on the bench instead. If the job gets
                                // interrupted before this op runs, returnMode is untouched
                                // and the pawn reequips as normal.
                                if (returnMode == WeaponReturnMode.Reequip)
                                    returnMode = WeaponReturnMode.LeaveOnWorkbench;
                            }
                            break;
                        case MemoryOpKind.WipeKillTracker:
                            // D18: "cleared" means a fresh kill clock while still
                            // bonded (TicksAbs — same 20-day grace D9 grants when
                            // adding NeedKill; −1 would risk an instant kill-thirst
                            // mood hit), or vanilla's −1 init state when unbonded
                            // (a freewielder that has killed).
                            WeaponModificationUtility.LastKillTickField?.SetValue(memoryComp,
                                memoryComp.Biocoded ? Find.TickManager.TicksAbs : -1);
                            break;
                    }
                    break;

                case OpType.Restyle:
                    // Marker op — the payload rides spec.vpweTexPaths, not this
                    // op (see OpType.Restyle's doc comment). No cost to pay.
                    // Null-safe/no-op on a weapon with no VPWE/VEF comp (base
                    // weapon, or VPWE/VEF absent) or when nothing was staged;
                    // ApplyTexPaths now also dirties the map mesh itself when
                    // the weapon is spawned, so a live re-texture renders
                    // immediately without any extra plumbing here.
                    VPWEIntegration.ApplyTexPaths(weapon, spec?.vpweTexPaths);
                    break;
            }
        }

        /// <summary>
        /// Converts the weapon to a different ThingDef in-place (base↔persona).
        /// Destroys the current weapon, spawns a new one at the same position,
        /// and updates reservations. Called atomically within an ApplyOperation
        /// step when a trait change crosses the 0↔1 boundary.
        /// </summary>
        private void ConvertWeaponInPlace(ThingDef targetDef)
        {
            // Downgrade (persona→base): sever the bond BEFORE the def swap while
            // the old weapon still exists, so UnCode clears the coded pawn's
            // bondedWeapon back-reference and biocode fields (spec §4). Biocode
            // state is never carried onto the new base weapon. On an upgrade the
            // old weapon has no CompBladelinkWeapon, so this is skipped.
            CompBladelinkWeapon oldBladelink = weapon.TryGetComp<CompBladelinkWeapon>();
            if (oldBladelink != null)
                oldBladelink.UnCode();

            Thing newWeapon = WeaponDefConversion.ConvertWeaponDef(weapon, targetDef);

            // Preserve the weapon's VPWE/VEF skin across the swap: a fresh persona
            // Thing would otherwise roll a new random skin on first render. Applied
            // before spawn so the first composition uses these paths. No-op on a
            // base target (no comp), when VPWE/VEF is absent, or when nothing was
            // captured (non-VPWE weapon). See VPWEIntegration.
            VPWEIntegration.ApplyTexPaths(newWeapon, spec?.vpweTexPaths);

            IntVec3 pos = weapon.Position;
            Map map = weapon.Map;

            // Transfer relic status and authored art BEFORE destroying the old
            // weapon: relic transfer keeps Thing.Destroy() from firing
            // Notify_ThingLost on the precept, and art transfer hands off the
            // TaleReference before PostDestroy would tear it down.
            WeaponDefConversion.TransferRelicStatus(weapon, newWeapon);
            WeaponDefConversion.TransferArt(weapon, newWeapon);

            if (weapon.Spawned)
                weapon.Destroy();
            else if (!weapon.Destroyed)
                weapon.Destroy();

            GenSpawn.Spawn(newWeapon, pos, map);
            pawn.Reserve(newWeapon, job);
            pawn.Map.physicalInteractionReservationManager.Reserve(pawn, job, newWeapon);
            weapon = newWeapon;
            // Keep the job target in sync with the live weapon: a save taken
            // after a conversion must not scribe targetB as a destroyed ref.
            job.SetTarget(WeaponIndex, newWeapon);
        }
    }
}
