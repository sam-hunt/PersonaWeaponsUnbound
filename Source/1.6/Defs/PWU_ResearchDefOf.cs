using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    [DefOf]
    public static class PWU_ResearchDefOf
    {
        public static ResearchProjectDef PWU_BladelinkCustomization;

        // Vanilla ("machine persuasion"); gates the optional persona-core
        // recipe, and its label is quoted in that setting's tooltip.
        public static ResearchProjectDef ShipComputerCore;

        // Applies the configured techprint count (PWU_Settings.techprintCount)
        // to the live research def. RimWorld.ResearchProjectDef.TechprintRequirementMet
        // reads the field directly, so this takes effect immediately with no restart (fork
        // spec §7, D4). Called once at startup (ModInitializer) and again every
        // time settings are written (PWU_Mod.WriteSettings).
        public static void ApplyTechprintCount()
        {
            // Null if another mod removed the project; DefOf binding already
            // logged that, and there is nothing to apply the count to.
            if (PWU_BladelinkCustomization != null)
                PWU_BladelinkCustomization.techprintCount = PWU_Mod.Settings.techprintCount;
        }
    }
}
