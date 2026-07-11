using System;
using System.Collections.Generic;
using Verse;

namespace PersonaWeaponsUnbound.Patches
{
    /// <summary>
    /// Postfix on VEF's <c>VEF.Graphics.CompGraphicCustomization.CompFloatMenuOptions</c>
    /// — resolved and registered manually in <see cref="ModInitializer"/> (not via
    /// <c>[HarmonyPatch(typeof(...))]</c>, since the target type only exists when
    /// VPWE/VEF is loaded; see <see cref="VPWEIntegration.CompFloatMenuOptionsMethod"/>).
    ///
    /// <para>When the player has opted into PWU's unified texture tab
    /// (<see cref="PWU_Settings.integrateVpweCustomization"/>) and PWU's own
    /// ground-customize float-menu option would be shown <em>and enabled</em> for
    /// the same weapon/pawn, VEF's "Customize" option is redundant — its dialog
    /// only edits the same composed texture the Texture tab now covers — so it's
    /// suppressed to avoid two competing "customize this weapon" entries on the
    /// same right-click menu. If PWU's option would be hidden or disabled (no
    /// research, unreachable, no workbench, etc.), VEF's own option is left alone
    /// as the player's fallback.</para>
    /// </summary>
    public static class VEF_CompGraphicCustomization_CompFloatMenuOptions_Patch
    {
        // The patched method is a single-yield iterator
        // (VEF.Graphics.CompGraphicCustomization.CompFloatMenuOptions: yields
        // exactly one FloatMenuOption, gated on Props.customizable, when it
        // yields anything at all) — so wholesale-replacing __result with an
        // empty enumerable when we suppress is correct; there is no second
        // option in the original iterator to preserve.
        public static void Postfix(ref IEnumerable<FloatMenuOption> __result, ThingComp __instance, Pawn selPawn)
        {
            if (PWU_Mod.Settings == null || !PWU_Mod.Settings.integrateVpweCustomization)
                return;

            Thing weapon = __instance?.parent;
            if (weapon == null || selPawn == null)
                return;

            FloatMenuOption ours;
            try
            {
                ours = FloatMenuOptionProvider_CustomizeGroundWeapon.BuildOptionFor(weapon, selPawn);
            }
            catch (Exception ex)
            {
                // Mirror the provider's own outer guard (GetSingleOptionFor) — a
                // throw here must not propagate into VEF's float-menu construction.
                string defName = weapon.def?.defName ?? "(null)";
                Log.Error("[Persona Weapons Unbound] Failed to evaluate our ground-customize option "
                    + "while checking whether to suppress VEF's texture-customize option for "
                    + defName + ": " + ex);
                return;
            }

            // Only suppress when ours would actually be usable — a hidden
            // (null) or disabled (action == null) option must not swallow
            // VEF's fallback, or the player would lose all access to
            // texture customization for that weapon/pawn combination.
            if (ours != null && !ours.Disabled)
                __result = Array.Empty<FloatMenuOption>();
        }
    }
}
