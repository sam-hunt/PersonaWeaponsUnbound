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
            var report = new InitDiagnostics();

            var harmony = new Harmony("shunter.personaweaponsunbound");
            report.Time("Harmony patching", () => harmony.PatchAll());

            report.Time("WeaponRegistry", () => WeaponRegistry.Initialize(report));
            report.Time("WorkbenchUtility", () => WorkbenchUtility.Initialize(report));
            report.Time("reflection checks", () => WeaponModificationUtility.VerifyReflection());
            report.Time("techprint count", () => PWU_ResearchDefOf.ApplyTechprintCount());

            // Force the optional VPWE/VEF skin integration to resolve now so any
            // API drift is reported at startup rather than lazily on first use.
            report.Time("integration probes", () =>
            {
                _ = VPWEIntegration.Available;
                _ = VPWEIntegration.UiSurfaceAvailable;
            });

            // Manual patch: suppresses VEF's own "Customize" ground float-menu
            // option when the player has opted into PWU's unified texture tab
            // and PWU's own ground-customize option is usable for the same
            // weapon/pawn. Registered manually — not attribute-discovered by
            // harmony.PatchAll() above — because the target type
            // (VEF.Graphics.CompGraphicCustomization) only exists when
            // VPWE/VEF is loaded; gated on UiSurfaceAvailable so a reflection
            // drift never leaves a half-applied patch pointing at a stale
            // MethodInfo.
            // Timed inside the guard, so the phase only shows up in the startup
            // summary on the runs where the patch is actually attempted.
            if (VPWEIntegration.UiSurfaceAvailable)
            {
                report.Time("VEF float-menu patch", () =>
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
                });
            }

            report.LogSummary();
        }
    }
}
