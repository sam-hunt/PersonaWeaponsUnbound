using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    [DefOf]
    public static class PWU_ResearchDefOf
    {
        public static ResearchProjectDef PWU_BladelinkCustomization;

        /// <summary>
        /// Applies the configured techprint count (<see cref="PWU_Settings.techprintCount"/>)
        /// to the live research def. <see cref="RimWorld.ResearchProjectDef.TechprintRequirementMet"/>
        /// reads the field directly, so this takes effect immediately with no restart (fork
        /// spec §7, D4). Called once at startup (<see cref="ModInitializer"/>) and again every
        /// time settings are written (<see cref="PWU_Mod.WriteSettings"/>).
        /// </summary>
        public static void ApplyTechprintCount()
        {
            PWU_BladelinkCustomization.techprintCount = PWU_Mod.Settings.techprintCount;
        }
    }
}
