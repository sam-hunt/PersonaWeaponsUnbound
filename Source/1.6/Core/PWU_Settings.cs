using RimWorld;
using PersonaWeaponsUnbound.HaulPlanning;
using Verse;

namespace PersonaWeaponsUnbound
{
    public class PWU_Settings : ModSettings
    {
        // Progression
        public bool restrictTraitsToDiscovered;

        // Persona Costs
        public int traitChangeBaseComponentCost = 2;
        public QualityCategory traitChangeQualitySurchargeThreshold = QualityCategory.Normal;
        public int traitChangeQualitySurchargePerLevel = 1;

        // Prerequisites
        public QualityCategory minimumQuality = QualityCategory.Awful;
        public bool allowDefConversion = true;
        public bool requireCustomizationResearch = true;
        public int techprintCount = 1;

        // Crafting Recipes
        public bool enableMonoswordRecipe = true;
        public bool enablePlasmaswordRecipe = true;
        public bool enableZeushammerRecipe = true;

        // Haul Planner
        public HaulPlannerKind haulPlannerKind = HaulPlannerKind.Sweep;

        // Miscellaneous
        public bool enableGroundCustomization = true;
        public bool enforceMaxTraitLimit = true;
        public bool enforceCanGenerateAlone;

        public void ResetToDefaults()
        {
            restrictTraitsToDiscovered = false;

            traitChangeBaseComponentCost = 2;
            traitChangeQualitySurchargeThreshold = QualityCategory.Normal;
            traitChangeQualitySurchargePerLevel = 1;

            minimumQuality = QualityCategory.Awful;
            allowDefConversion = true;
            requireCustomizationResearch = true;
            techprintCount = 1;

            enableMonoswordRecipe = true;
            enablePlasmaswordRecipe = true;
            enableZeushammerRecipe = true;

            haulPlannerKind = HaulPlannerKind.Sweep;

            enableGroundCustomization = true;
            enforceMaxTraitLimit = true;
            enforceCanGenerateAlone = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref restrictTraitsToDiscovered, "restrictTraitsToDiscovered");

            Scribe_Values.Look(ref traitChangeBaseComponentCost, "traitChangeBaseComponentCost", 2);
            Scribe_Values.Look(ref traitChangeQualitySurchargeThreshold,
                "traitChangeQualitySurchargeThreshold", QualityCategory.Normal);
            Scribe_Values.Look(ref traitChangeQualitySurchargePerLevel,
                "traitChangeQualitySurchargePerLevel", 1);

            Scribe_Values.Look(ref minimumQuality, "minimumQuality", QualityCategory.Awful);
            Scribe_Values.Look(ref allowDefConversion, "allowDefConversion", true);
            Scribe_Values.Look(ref requireCustomizationResearch, "requireCustomizationResearch", true);
            Scribe_Values.Look(ref techprintCount, "techprintCount", 1);

            Scribe_Values.Look(ref enableMonoswordRecipe, "enableMonoswordRecipe", true);
            Scribe_Values.Look(ref enablePlasmaswordRecipe, "enablePlasmaswordRecipe", true);
            Scribe_Values.Look(ref enableZeushammerRecipe, "enableZeushammerRecipe", true);

            Scribe_Values.Look(ref haulPlannerKind, "haulPlannerKind", HaulPlannerKind.Sweep);

            Scribe_Values.Look(ref enableGroundCustomization, "enableGroundCustomization", true);
            Scribe_Values.Look(ref enforceMaxTraitLimit, "enforceMaxTraitLimit", true);
            Scribe_Values.Look(ref enforceCanGenerateAlone, "enforceCanGenerateAlone");
        }
    }
}
