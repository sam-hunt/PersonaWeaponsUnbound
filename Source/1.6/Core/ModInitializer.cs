using System;
using HarmonyLib;
using PersonaWeaponsUnbound.Patches;
using Verse;

namespace PersonaWeaponsUnbound
{
    [StaticConstructorOnStartup]
    public static class ModInitializer
    {
        static ModInitializer()
        {
            var harmony = new Harmony("shunter.personaweaponsunbound");
            harmony.PatchAll();

            var report = new InitDiagnostics();
            WeaponRegistry.Initialize(report);
            WorkbenchUtility.Initialize(report);
            WeaponModificationUtility.VerifyReflection();
            PWU_ResearchDefOf.ApplyTechprintCount();

            // Force the optional VPWE/VEF skin integration to resolve now so any
            // API drift is reported at startup rather than lazily on first use.
            _ = VPWEIntegration.Available;
            _ = VPWEIntegration.UiSurfaceAvailable;

            // Manual patch: suppresses VEF's own "Customize" ground float-menu
            // option when the player has opted into PWU's unified texture tab
            // and PWU's own ground-customize option is usable for the same
            // weapon/pawn. Registered manually — not attribute-discovered by
            // harmony.PatchAll() above — because the target type
            // (VEF.Graphics.CompGraphicCustomization) only exists when
            // VPWE/VEF is loaded; gated on UiSurfaceAvailable so a reflection
            // drift never leaves a half-applied patch pointing at a stale
            // MethodInfo.
            if (VPWEIntegration.UiSurfaceAvailable)
            {
                try
                {
                    harmony.Patch(
                        VPWEIntegration.CompFloatMenuOptionsMethod,
                        postfix: new HarmonyMethod(
                            typeof(VEF_CompGraphicCustomization_CompFloatMenuOptions_Patch),
                            nameof(VEF_CompGraphicCustomization_CompFloatMenuOptions_Patch.Postfix)));
                }
                catch (Exception ex)
                {
                    Log.Warning("[Persona Weapons Unbound] Could not patch VEF's "
                        + "CompGraphicCustomization.CompFloatMenuOptions for texture-tab "
                        + "float-menu integration; VEF's own \"Customize\" option may "
                        + "appear alongside PWU's: " + ex);
                }
            }

            report.LogSummary();
        }
    }
}
