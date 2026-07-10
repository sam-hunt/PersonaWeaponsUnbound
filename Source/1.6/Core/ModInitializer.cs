using HarmonyLib;
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

            report.LogSummary();
        }
    }
}
