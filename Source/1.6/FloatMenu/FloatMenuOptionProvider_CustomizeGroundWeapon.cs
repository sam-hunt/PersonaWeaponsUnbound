using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace PersonaWeaponsUnbound
{
    // Entry point 3: right-click a weapon on the ground to customize it.
    // Auto-selects the best workbench via WorkbenchUtility.FindBestWorkbench.
    public class FloatMenuOptionProvider_CustomizeGroundWeapon : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;

        protected override FloatMenuOption GetSingleOptionFor(
            Thing clickedThing, FloatMenuContext context)
        {
            // Outer guard so an unexpected throw inside the analysis (broken
            // building def during workbench search, modded weapon throwing
            // inside LabelShortCap, upstream cache NRE, etc.) drops only the
            // option instead of cascading into vanilla's menu construction.
            try
            {
                return BuildOption(clickedThing, context);
            }
            catch (Exception ex)
            {
                Log.Error("[Persona Weapons Unbound] Skipped ground-customization menu entry for "
                    + SafeLabel(clickedThing) + " ("
                    + (clickedThing?.def?.defName ?? "?") + ") due to error: " + ex);
                return null;
            }
        }

        private static FloatMenuOption BuildOption(Thing clickedThing, FloatMenuContext context)
        {
            Pawn pawn = context.FirstSelectedPawn;
            if (pawn == null)
                return null;
            return BuildOptionFor(clickedThing, pawn);
        }

        // Core option-building logic, factored out of BuildOption
        // so the VPWE/VEF float-menu-suppression patch
        // (VEF_CompGraphicCustomization_CompFloatMenuOptions_Patch) can ask
        // "would PWU show its own ground-customize option for this weapon/pawn,
        // and would it be enabled?" without needing a FloatMenuContext
        // — the only thing that type contributed here was
        // FirstSelectedPawn, so the pawn is taken directly. Returns null
        // when the option would be hidden entirely; a non-null result may still
        // be FloatMenuOption.Disabled (action == null) when it's
        // shown-but-blocked (no path, no research, etc.) — callers that only
        // care about a genuinely usable option must check both.
        internal static FloatMenuOption BuildOptionFor(Thing weapon, Pawn pawn)
        {
            if (PWU_Mod.Settings?.enableGroundCustomization != true)
                return null;
            if (weapon == null || weapon.def == null)
                return null;
            if (weapon is Building)
                return null;
            if (!weapon.def.IsWeapon)
                return null;
            if (!weapon.Spawned)
                return null;
            if (pawn == null)
                return null;

            // Variant exists + research gate
            AcceptanceReport customizable = CustomizationRules.IsCustomizable(weapon);
            if (!customizable.Accepted && customizable.Reason.NullOrEmpty())
                return null;

            // Resolve base/persona defs
            WeaponRegistry.ResolveWeaponDefs(weapon,
                out ThingDef baseDef, out ThingDef personaDef);

            // Customization research
            if (!customizable.Accepted)
                return DisabledOrHidden(weapon, customizable);

            string label = "PWU_CustomizeWeapon".Translate(weapon.LabelShortCap);

            // Weapon reachability + forbidden checks
            if (!pawn.CanReach(weapon, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return new FloatMenuOption(
                    label + " (" + "NoPath".Translate() + ")",
                    null);
            }

            // No forbidden check on the weapon itself — this is a direct
            // player order, matching vanilla's behavior for equipping forbidden weapons.

            // Find best workbench (most expensive check — runs last)
            var result = WorkbenchUtility.FindBestWorkbench(
                pawn, baseDef, personaDef, TechLevel.Undefined, weapon.Position);

            if (!result.Found)
                return DisabledOrHidden(weapon, result.BestRejection);

            Building_WorkTable workbench = result.Workbench;

            // Capture for the click delegate so a destroyed-mid-menu weapon
            // doesn't NRE inside vanilla's order dispatch.
            Thing capturedWeapon = weapon;
            Building_WorkTable capturedWorkbench = workbench;
            Pawn capturedPawn = pawn;

            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    label,
                    delegate
                    {
                        FloatMenuOptionProvider_CustomizeWeapon.TryQueueCustomizeJob(
                            capturedPawn, capturedWeapon, capturedWorkbench);
                    }),
                pawn, weapon);
        }

        private static string SafeLabel(Thing t)
        {
            if (t == null) return "(null)";
            try { return t.LabelShortCap; }
            catch { return t.def?.defName ?? "(unlabelled)"; }
        }

        private static FloatMenuOption DisabledOrHidden(Thing weapon, AcceptanceReport report)
        {
            if (report.Reason.NullOrEmpty())
                return null;

            string label = "PWU_CustomizeWeapon".Translate(weapon.LabelShortCap)
                + " (" + report.Reason + ")";
            return new FloatMenuOption(label, null);
        }
    }
}
