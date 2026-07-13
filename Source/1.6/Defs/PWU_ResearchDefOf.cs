using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    [DefOf]
    public static class PWU_ResearchDefOf
    {
        public static ResearchProjectDef PWU_BladelinkCustomization;

        // Applies the configured techprint count (PWU_Settings.techprintCount)
        // to the live research def. RimWorld.ResearchProjectDef.TechprintRequirementMet
        // reads the field directly, so this takes effect immediately with no restart (fork
        // spec §7, D4). Called once at startup (ModInitializer) and again every
        // time settings are written (PWU_Mod.WriteSettings).
        public static void ApplyTechprintCount()
        {
            PWU_BladelinkCustomization.techprintCount = PWU_Mod.Settings.techprintCount;
        }
    }
}
