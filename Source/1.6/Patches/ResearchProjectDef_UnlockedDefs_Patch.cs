using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound.Patches
{
    // Keeps "persona core" out of machine persuasion's unlock list while the
    // optional persona-core recipe is toggled off
    // (PWU_Settings.enablePersonaCoreRecipe).
    //
    // Verse.ResearchProjectDef.UnlockedDefs builds its list by scanning every
    // RecipeDef whose researchPrerequisite is this project and collecting their
    // products, so PWU_Make_AIPersonaCore advertises the core the moment the def
    // loads, whatever the setting says. That one property feeds every "unlocks"
    // surface in the game: the research tab's Unlocks row (via
    // ResearchPrerequisitesUtility), the research-completed dialog
    // (ResearchManager.FinishProject), the project's info-card hyperlinks
    // (ResearchProjectDef.InfoCardHyperlinks), and Ideology's missing-meme
    // warning (MainTabWindow_Research.ComputeUnlockedDefsThatHaveMissingMemes).
    // Filtering the getter covers all four at once, and unlike stripping
    // researchPrerequisite off the recipe def it still shows the entry when the
    // recipe *is* enabled.
    //
    // Companion to RecipeDef_AvailableNow_Patch, which does the same job for the
    // bill itself.
    [HarmonyPatch(typeof(ResearchProjectDef), nameof(ResearchProjectDef.UnlockedDefs), MethodType.Getter)]
    public static class ResearchProjectDef_UnlockedDefs_Patch
    {
        // ResearchProjectDef.cachedHyperlinks — private, built once from
        // UnlockedDefs (so, from our filtered list) and never invalidated by
        // vanilla. Nulled on every settings write, otherwise toggling the recipe
        // leaves the research project's info card stale until the next restart.
        private static readonly FieldInfo HyperlinksCacheField = typeof(ResearchProjectDef)
            .GetField("cachedHyperlinks", BindingFlags.NonPublic | BindingFlags.Instance);

        // MainTabWindow_Research.cachedUnlockedDefsGroupedByPrerequisites —
        // private. The window already clears it in PreOpen, so this only covers
        // the narrow case of the research tab sitting open behind the settings
        // dialog while the toggle flips.
        private static readonly FieldInfo GroupedUnlocksCacheField = typeof(MainTabWindow_Research)
            .GetField("cachedUnlockedDefsGroupedByPrerequisites", BindingFlags.NonPublic | BindingFlags.Instance);

        // Unlike MainTabWindow_Research_VisibleResearchProjects_Patch, which
        // RemoveAlls in place, this one must never touch __result: the getter
        // hands back its own cachedUnlockedDefs by reference and nothing in
        // vanilla ever rebuilds it, so an in-place removal would be permanent and
        // would survive the player toggling the recipe back on. We keep a
        // filtered copy instead, keyed on the source list's reference identity so
        // it survives (and notices) a vanilla cache rebuild without re-filtering
        // on every call — the research tab hits this getter repeatedly per frame
        // through ResearchPrerequisitesUtility.
        private static List<Def> filterSource;
        private static List<Def> filteredUnlocks;

        // True when some other mod also produces a persona core off machine
        // persuasion (a few starship mods do). The unlock entry is then earned
        // independently of our recipe, so we leave the list alone rather than
        // hide someone else's unlock.
        private static bool foreignPersonaCoreRecipe;

        // The postfix stays inert until the startup scan has run: UnlockedDefs is
        // reachable during def load (CompProperties_Techprint.ResolveReferences),
        // where DefDatabase is only partly populated and no unlock UI exists yet.
        private static bool initialized;

        public static void Initialize(InitDiagnostics report)
        {
            try
            {
                if (HyperlinksCacheField == null)
                    Log.Error("[Persona Weapons Unbound] ResearchProjectDef.cachedHyperlinks field not found "
                        + "via reflection; toggling the persona core recipe will leave a stale unlock list on "
                        + "the machine persuasion info card until the game is restarted. "
                        + "RimWorld API may have changed.");
                if (GroupedUnlocksCacheField == null)
                    Log.Error("[Persona Weapons Unbound] "
                        + "MainTabWindow_Research.cachedUnlockedDefsGroupedByPrerequisites field not found via "
                        + "reflection; toggling the persona core recipe with the research tab already open will "
                        + "leave a stale Unlocks row until the tab is reopened. RimWorld API may have changed.");

                foreignPersonaCoreRecipe = AnyForeignPersonaCoreRecipe();
                if (foreignPersonaCoreRecipe)
                    Log.Message("[Persona Weapons Unbound] Another mod also crafts a persona core from "
                        + "machine persuasion; leaving that research project's unlock list untouched.");

                initialized = true;
            }
            catch (Exception ex)
            {
                // Leaves initialized false, so the filter is skipped entirely and
                // the unlock entry stays visible: same behavior as not having
                // this patch, which is the safe direction to fail.
                report.RecordFailure("PersonaCoreUnlockFilter", ex);
            }
        }

        // Mirrors vanilla's own prerequisite test in UnlockedDefs
        // (researchPrerequisite or researchPrerequisites), so we only decline to
        // filter when a foreign recipe would genuinely put the core in this
        // project's list.
        private static bool AnyForeignPersonaCoreRecipe()
        {
            ResearchProjectDef gate = PWU_ResearchDefOf.ShipComputerCore;

            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe == PWU_RecipeDefOf.PWU_Make_AIPersonaCore || recipe.products == null)
                    continue;
                if (recipe.researchPrerequisite != gate
                    && (recipe.researchPrerequisites == null || !recipe.researchPrerequisites.Contains(gate)))
                    continue;

                foreach (ThingDefCountClass product in recipe.products)
                {
                    if (product.thingDef == ThingDefOf.AIPersonaCore)
                        return true;
                }
            }

            return false;
        }

        // Called from PWU_Mod.WriteSettings. Drops the two vanilla caches that
        // are built from UnlockedDefs and never invalidated on their own, so the
        // toggle takes effect with no restart like the rest of PWU's settings.
        // Our own filteredUnlocks needs no invalidation: it is keyed on the
        // source list's identity and only consulted while the recipe is off.
        public static void Notify_SettingsChanged()
        {
            try
            {
                if (PWU_ResearchDefOf.ShipComputerCore != null)
                    HyperlinksCacheField?.SetValue(PWU_ResearchDefOf.ShipComputerCore, null);

                MainTabWindow_Research researchTab = Find.WindowStack?.WindowOfType<MainTabWindow_Research>();
                if (researchTab != null)
                    GroupedUnlocksCacheField?.SetValue(researchTab, null);
            }
            catch (Exception ex)
            {
                Log.Warning("[Persona Weapons Unbound] Could not refresh the research tab's unlock caches "
                    + "after a settings change; the persona core entry under machine persuasion may be stale "
                    + "until the game is restarted: " + ex);
            }
        }

        [HarmonyPostfix]
        public static void Postfix(ResearchProjectDef __instance, ref List<Def> __result)
        {
            // A throw here propagates into every unlock-list consumer, including
            // research tab rendering. ErrorOnce so a recurring per-frame failure
            // doesn't flood the log.
            try
            {
                __result = Filtered(__instance, __result);
            }
            catch (Exception ex)
            {
                Log.ErrorOnce(
                    "[Persona Weapons Unbound] Persona core unlock filter failed: " + ex,
                    "PWU_UnlockFilterFail".GetHashCode());
            }
        }

        private static List<Def> Filtered(ResearchProjectDef project, List<Def> unlocked)
        {
            if (!initialized || unlocked == null || foreignPersonaCoreRecipe)
                return unlocked;
            if (project != PWU_ResearchDefOf.ShipComputerCore)
                return unlocked;

            // Settings null (startup ordering, or a corrupt settings file leaving
            // GetSettings returning null) defaults to leaving the entry visible,
            // matching RecipeDef_AvailableNow_Patch's own null guard.
            if (PWU_Mod.Settings == null || PWU_Mod.Settings.enablePersonaCoreRecipe)
                return unlocked;

            if (!ReferenceEquals(filterSource, unlocked))
            {
                var copy = new List<Def>(unlocked);
                copy.Remove(ThingDefOf.AIPersonaCore);
                filterSource = unlocked;
                filteredUnlocks = copy;
            }

            return filteredUnlocks;
        }
    }
}
