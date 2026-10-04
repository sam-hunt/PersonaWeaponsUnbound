using System;
using System.Collections.Generic;
using Verse;

namespace PersonaWeaponsUnbound
{
    // Per-load index of the skill level each weapon's crafting recipe demands,
    // for the skill check's recipe-based kind. Built from every RecipeDef that
    // produces a weapon, so it covers recipeMaker-generated recipes (modded
    // persona weapons that declare one), standalone RecipeDefs (this mod's own
    // PWU_Make_* recipes for the vanilla base weapons, whose defs carry no
    // recipeMaker), and recipes other mods add for weapons they don't own.
    // Vanilla ships no craftable persona weapon, but nothing in the API stops
    // a mod from adding one at any tech level, so the index is driven by the
    // live database rather than by assumptions about the content.
    //
    // A weapon produced by several recipes takes the LOWEST level any of them
    // demands: "whoever could craft it" means the easiest craft path. A recipe
    // with no skill requirement records 0, which the skill check treats as
    // "craftable by anyone, so customizable by anyone". Disabled recipes (this
    // mod's own toggles hide them from the bill menu, not from the database)
    // still count: the toggle means "stop offering this", and the skill level
    // it declares is still the right proxy for the weapon's difficulty.
    //
    // Rebuilt once per play-data load via PWU_Startup.Run; an in-process reload
    // replaces every def instance, so a stale index would never match a live
    // weapon def and every weapon would fall back to its tech tier.
    public static class WeaponRecipeIndex
    {
        private static Dictionary<ThingDef, int> minRequiredLevelByProduct =
            new Dictionary<ThingDef, int>();

        // Builds the index from the live database. A non-null report absorbs
        // any fatal exception so the rest of the mod can still initialize;
        // passing null preserves the throwing contract for direct callers.
        public static void Initialize(InitDiagnostics report = null)
        {
            try
            {
                Rebuild(DefDatabase<RecipeDef>.AllDefsListForReading);
            }
            catch (Exception ex)
            {
                if (report == null) throw;
                report.RecordFailure(nameof(WeaponRecipeIndex), ex);
            }
        }

        // Rebuilds the index from the given recipes; exposed for the headless
        // tests, which have no DefDatabase.
        internal static void Rebuild(IEnumerable<RecipeDef> recipes)
        {
            var index = new Dictionary<ThingDef, int>();
            foreach (RecipeDef recipe in recipes)
            {
                if (recipe?.products == null)
                    continue;
                int level = MaxRequiredLevel(recipe);
                foreach (ThingDefCountClass product in recipe.products)
                {
                    ThingDef def = product?.thingDef;
                    if (def?.IsWeapon != true)
                        continue;
                    if (!index.TryGetValue(def, out int existing) || level < existing)
                        index[def] = level;
                }
            }
            minRequiredLevelByProduct = index;
        }

        // The highest level among the recipe's skill requirements (a recipe
        // demanding Crafting 10 and Artistic 5 is a level-10 craft), or 0 when
        // it has none.
        private static int MaxRequiredLevel(RecipeDef recipe)
        {
            int level = 0;
            if (recipe.skillRequirements != null)
            {
                foreach (SkillRequirement sr in recipe.skillRequirements)
                {
                    if (sr?.skill != null && sr.minLevel > level)
                        level = sr.minLevel;
                }
            }
            return level;
        }

        // Whether any recipe produces the weapon, and if so the lowest skill
        // level such a recipe demands (0 when a recipe demands none).
        public static bool TryGetRequiredLevel(ThingDef weaponDef, out int level)
        {
            if (weaponDef == null)
            {
                level = 0;
                return false;
            }
            return minRequiredLevelByProduct.TryGetValue(weaponDef, out level);
        }
    }
}
