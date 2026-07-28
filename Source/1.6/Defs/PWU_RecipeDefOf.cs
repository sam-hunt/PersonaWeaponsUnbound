using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    [DefOf]
    public static class PWU_RecipeDefOf
    {
        public static RecipeDef PWU_Make_MonoSword;
        public static RecipeDef PWU_Make_PlasmaSword;
        public static RecipeDef PWU_Make_Zeushammer;
        public static RecipeDef PWU_Make_AIPersonaCore;

        // Applies the configured advanced-component count and crafting skill
        // requirement (PWU_Settings.personaCoreRecipeComponentCost /
        // personaCoreRecipeMinSkill) to the live persona-core recipe def. Both
        // are read per-bill at work time, so this takes effect immediately with
        // no restart — existing bills repriced included. Called once at startup
        // (ModInitializer) and again every time settings are written
        // (PWU_Mod.WriteSettings).
        public static void ApplyPersonaCoreRecipeSettings()
        {
            RecipeDef recipe = PWU_Make_AIPersonaCore;
            if (recipe == null)
                return;

            // Matched by fixed ingredient rather than by index so a patch from
            // another mod inserting an ingredient can't silently reprice the
            // wrong line.
            if (recipe.ingredients != null)
            {
                foreach (IngredientCount ingredient in recipe.ingredients)
                {
                    if (ingredient.FixedIngredient == ThingDefOf.ComponentSpacer)
                    {
                        ingredient.SetBaseCount(PWU_Mod.Settings.personaCoreRecipeComponentCost);
                        break;
                    }
                }
            }

            int minSkill = PWU_Mod.Settings.personaCoreRecipeMinSkill;
            if (minSkill <= 0)
            {
                // An explicit 0-level requirement would still render a "Crafting
                // 0" line in the bill's requirements; dropping the list is how
                // vanilla expresses "anyone can make this".
                recipe.skillRequirements = null;
            }
            else
            {
                recipe.skillRequirements = new List<SkillRequirement>
                {
                    new SkillRequirement { skill = SkillDefOf.Crafting, minLevel = minSkill },
                };
            }
        }
    }
}
