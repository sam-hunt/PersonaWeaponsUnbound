using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace PersonaWeaponsUnbound.Patches
{
    // Comp-scoped postfix instead of ThingWithComps.GetGizmos so the patch
    // body only runs for Things whose def carries CompForbiddable — items,
    // some buildings, doors. Pawns, walls, plants, and terrain features
    // never enter this code. Every weapon that can be selected on the
    // ground carries CompForbiddable, so the gizmo's reachability set is
    // unchanged.
    //
    // CompBladelinkWeapon would be the narrowest possible target (persona
    // weapons only) but vanilla doesn't override CompGetGizmosExtra on it,
    // so there's no method body to postfix. It would also miss base
    // weapons with a registered persona variant, which still need the
    // gizmo to start a base→persona conversion.
    [HarmonyPatch(typeof(CompForbiddable), nameof(CompForbiddable.CompGetGizmosExtra))]
    public static class CompForbiddable_CompGetGizmosExtra_Patch
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(
            IEnumerable<Gizmo> __result, CompForbiddable __instance)
        {
            foreach (Gizmo g in __result)
                yield return g;

            // Analysis is wrapped in a try/catch helper because an uncaught
            // throw here would propagate into vanilla's gizmo iterator and
            // break selection rendering on the host Thing — including
            // unrelated gizmos contributed by other comps.
            Gizmo customize = TryBuildCustomizeGizmo(__instance.parent);
            if (customize != null)
                yield return customize;
        }

        private static Gizmo TryBuildCustomizeGizmo(Thing parent)
        {
            try
            {
                return BuildCustomizeGizmo(parent);
            }
            catch (Exception ex)
            {
                // ErrorOnce keyed by defName so a recurring per-frame failure
                // on a selected weapon doesn't flood the log.
                string defName = parent?.def?.defName ?? "(null)";
                Log.ErrorOnce(
                    "[Persona Weapons Unbound] Customize gizmo failed for "
                        + defName + ": " + ex,
                    ("PWU_GizmoFail_" + defName).GetHashCode());
                return null;
            }
        }

        private static Gizmo BuildCustomizeGizmo(Thing parent)
        {
            // Layer 1: Hidden — skip non-weapons and non-customizable weapons.
            // Registry membership isn't checked here so CustomizationRules.IsCustomizable
            // can still surface its HiddenUnlessDev rejection reasons as a
            // visible-but-disabled gizmo in dev mode.
            if (!parent.def.IsWeapon || !parent.Spawned)
                return null;

            AcceptanceReport customizable = CustomizationRules.IsCustomizable(parent);
            if (!customizable.Accepted && customizable.Reason.NullOrEmpty())
                return null;

            WeaponRegistry.ResolveWeaponDefs(parent,
                out ThingDef baseDef, out ThingDef personaDef);
            TechLevel techLevel = CustomizationRules.GetWeaponTechLevel(parent);

            Command_Action gizmo = new Command_Action();
            gizmo.defaultLabel = "PWU_CustomizeGizmoLabel".Translate();
            gizmo.defaultDesc = "PWU_CustomizeGizmoDesc".Translate();
            gizmo.icon = PWU_Textures.Customize;

            // Layer 2: Disabled state (pawn-independent checks)
            if (!customizable.Accepted)
            {
                gizmo.Disabled = true;
                gizmo.disabledReason = customizable.Reason;
            }
            else
            {
                var workbenchCheck = WorkbenchUtility.FindBestWorkbench(
                    parent.Map, baseDef, personaDef, techLevel, parent.Position);
                if (!workbenchCheck.Found)
                {
                    gizmo.Disabled = true;
                    gizmo.disabledReason = workbenchCheck.BestRejection.Reason;
                }
            }

            // Capture locals for the delegate closures
            Thing weapon = parent;
            ThingDef capturedBaseDef = baseDef;
            ThingDef capturedPersonaDef = personaDef;
            TechLevel capturedTechLevel = techLevel;

            gizmo.action = delegate
            {
                // Click-time hardening: a throw here would bubble through
                // vanilla's UI handler. ErrorOnce keyed by defName so a
                // pathological weapon doesn't flood the log on repeat clicks.
                try
                {
                    BeginCustomizeTargeting(
                        weapon, capturedBaseDef, capturedPersonaDef, capturedTechLevel);
                }
                catch (Exception ex)
                {
                    string defName = weapon?.def?.defName ?? "(null)";
                    Log.ErrorOnce(
                        "[Persona Weapons Unbound] Customize action failed for "
                            + defName + ": " + ex,
                        ("PWU_GizmoAction_" + defName).GetHashCode());
                }
            };

            return gizmo;
        }

        private static void BeginCustomizeTargeting(
            Thing weapon, ThingDef baseDef, ThingDef personaDef, TechLevel techLevel)
        {
            TargetingParameters parms = TargetingParameters.ForColonist();

            // Layer 3: pawn-specific validation on the targeter
            parms.validator = delegate(TargetInfo targetInfo)
            {
                if (!(targetInfo.Thing is Pawn p))
                    return false;
                return WorkbenchUtility.FindBestWorkbench(
                    p, baseDef, personaDef, techLevel, weapon.Position).Found;
            };

            Find.Targeter.BeginTargeting(parms,
                delegate(LocalTargetInfo target)
                {
                    // Layer 4: create job
                    Pawn pawn = target.Pawn;
                    if (pawn == null)
                        return;

                    var result = WorkbenchUtility.FindBestWorkbench(
                        pawn, baseDef, personaDef, techLevel, weapon.Position);
                    if (!result.Found)
                    {
                        Messages.Message(
                            "PWU_CustomizeWeapon".Translate(weapon.LabelShortCap)
                                + " (" + result.BestRejection.Reason + ")",
                            weapon, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    Job job = JobMaker.MakeJob(PWU_JobDefOf.PWU_CustomizeWeapon);
                    job.targetB = weapon;
                    job.targetC = result.Workbench;
                    job.count = 1;
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
        }
    }
}
