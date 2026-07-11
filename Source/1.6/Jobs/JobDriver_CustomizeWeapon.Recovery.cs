using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PersonaWeaponsUnbound
{
    // Recovery phase: cleanup that runs when the job ends, whether
    // successfully or via interruption. Drops any haul-phase inventory
    // the pawn was still holding so ingredients don't ride into the
    // next job, and queues a follow-up Equip/TakeInventory job so the
    // finished weapon ends up back where it started (matching returnMode).
    public partial class JobDriver_CustomizeWeapon
    {
        /// <summary>
        /// Drops haul-phase inventory items the pawn is still holding when
        /// the job ends (interrupted between pickup and workbench unload).
        /// Without this, the pawn would silently carry the ingredients into
        /// future jobs — confusing for the player and effectively a stockpile
        /// leak from the world's perspective.
        /// </summary>
        private void DropPendingHaulInventory()
        {
            if (currentTripInvLoad == null || currentTripInvLoad.Count == 0) return;
            if (pawn.Map == null || pawn.inventory == null) return;

            foreach (ThingDefCountClass entry in currentTripInvLoad)
            {
                int remaining = entry.count;
                for (int i = pawn.inventory.innerContainer.Count - 1; i >= 0 && remaining > 0; i--)
                {
                    Thing inv = pawn.inventory.innerContainer[i];
                    if (inv.def != entry.thingDef) continue;
                    int dropAmt = Mathf.Min(remaining, inv.stackCount);
                    pawn.inventory.innerContainer.TryDrop(
                        inv, pawn.Position, pawn.Map, ThingPlaceMode.Near, dropAmt, out _);
                    remaining -= dropAmt;
                }
            }
            currentTripInvLoad.Clear();
        }

        /// <summary>
        /// Queues a follow-up job so the pawn walks to the weapon and picks it
        /// up via the standard equip/take-inventory job drivers. Used for both
        /// normal completion (pawn is at workbench, job completes near-instantly)
        /// and interruption recovery (pawn walks back to retrieve weapon).
        ///
        /// Reads <see cref="weapon"/>; called from the finish action where the
        /// success-path toil has already nulled the field, so this short-circuits
        /// on Succeeded and only runs on actual interruption.
        ///
        /// TODO: the finish action fires on Succeeded too, so the
        /// returnWeaponToil's separate recovery call is structurally redundant.
        /// Removing the toil and relying solely on the finish action would
        /// collapse the two call sites into one and obsolete the field-null
        /// guard added for the double-recovery footgun.
        /// </summary>
        private void QueueWeaponRecovery() => QueueWeaponRecoveryFor(weapon);

        /// <summary>
        /// Explicit-weapon variant used by the success-path toil so the field
        /// can be nulled before recovery runs. The toil nulls
        /// <see cref="weapon"/> first, then calls this with a stashed reference;
        /// if recovery throws after enqueueing the follow-up job, the finish
        /// action's <see cref="QueueWeaponRecovery"/> call sees null and bails
        /// instead of double-recovering.
        /// </summary>
        private void QueueWeaponRecoveryFor(Thing recoverWeapon)
        {
            if (recoverWeapon == null || recoverWeapon.Destroyed)
                return;

            if (pawn.Map == null)
                return;

            // Drop from carry if the pawn is still holding the weapon
            if (pawn.carryTracker?.CarriedThing == recoverWeapon)
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);

            if (!recoverWeapon.Spawned || recoverWeapon.Destroyed)
                return;

            switch (returnMode)
            {
                case WeaponReturnMode.Reequip:
                    // Vanilla's persona-bond confirmation lives only in the float-menu
                    // click delegate, never in JobDriver_Equip itself, so an auto-queued
                    // Equip job would otherwise bond the weapon silently. Mirror the
                    // manual-equip dialog here; GetPersonaWeaponConfirmationText already
                    // returns null for the common cases (bonded to this pawn, freewielder,
                    // downgraded to base), so most re-equips still take the fast path below.
                    //
                    // Vanilla also checks AlreadyBondedToWeapon before that confirmation
                    // (see FloatMenuOptionProvider_Equip) and shows an info-only dialog
                    // instead of queuing an Equip job, since JobDriver_Equip itself has no
                    // messaging and would just silently fail to reserve the weapon. Without
                    // this check, a pawn who forgot about an existing bond (weapon dropped,
                    // hauled off, left on another map) would see the "will bond" confirm
                    // dialog, click yes, and have the weapon left stranded on the workbench
                    // with no explanation.
                    if (EquipmentUtility.AlreadyBondedToWeapon(recoverWeapon, pawn))
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox("BladelinkAlreadyBondedDialog".Translate(
                            pawn.Named("PAWN"), recoverWeapon.Named("WEAPON"), pawn.equipment.bondedWeapon.Named("BONDEDWEAPON"))));
                        break;
                    }

                    string confirmText = EquipmentUtility.GetPersonaWeaponConfirmationText(recoverWeapon, pawn);
                    if (confirmText.NullOrEmpty())
                    {
                        pawn.jobs.jobQueue.EnqueueFirst(
                            JobMaker.MakeJob(JobDefOf.Equip, recoverWeapon));
                        break;
                    }

                    Find.WindowStack.Add(new Dialog_MessageBox(confirmText, "Yes".Translate(), delegate
                    {
                        // World may have moved on while the dialog was up (pawn/weapon
                        // downed, despawned, hauled off-map, etc.) — bail rather than
                        // order a job against a stale reference.
                        if (recoverWeapon.DestroyedOrNull() || !recoverWeapon.Spawned
                            || pawn.DestroyedOrNull() || !pawn.Spawned || pawn.Map != recoverWeapon.Map)
                            return;

                        // Bond state can also change while the dialog was open (e.g. the
                        // pawn bonded to another persona weapon in the meantime) — re-check
                        // rather than queue a job that would silently fail to reserve.
                        if (EquipmentUtility.AlreadyBondedToWeapon(recoverWeapon, pawn))
                        {
                            Find.WindowStack.Add(new Dialog_MessageBox("BladelinkAlreadyBondedDialog".Translate(
                                pawn.Named("PAWN"), recoverWeapon.Named("WEAPON"), pawn.equipment.bondedWeapon.Named("BONDEDWEAPON"))));
                            return;
                        }

                        recoverWeapon.SetForbidden(false);
                        pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Equip, recoverWeapon), JobTag.Misc);
                        FleckMaker.Static(recoverWeapon.DrawPos, recoverWeapon.MapHeld, FleckDefOf.FeedbackEquip);
                    }, "No".Translate()));
                    break;

                case WeaponReturnMode.ReturnToInventory:
                    Job takeJob = JobMaker.MakeJob(JobDefOf.TakeInventory, recoverWeapon);
                    takeJob.count = 1;
                    pawn.jobs.jobQueue.EnqueueFirst(takeJob);
                    break;

                case WeaponReturnMode.LeaveOnWorkbench:
                    break;
            }
        }
    }
}
