using HarmonyLib;
using Verse;

namespace PersonaWeaponsUnbound.Patches
{
    // Gates the three PWU weapon-crafting recipes (fork spec §10) behind their
    // individual settings toggles by postfixing the AvailableNow property
    // getter. No def surgery, so it works mid-save with no restart.
    //
    // The optional persona-core recipe is deliberately not gated here: its
    // toggle is an XML patch gate (PatchOperation_UnlessPersonaCoreRecipeEnabled)
    // applied at load, because research-tree mods list unlocks straight from
    // the def fields and never consult this getter or the research one.
    //
    // AvailableNow's only callers are UI/event-scoped — the bills-tab
    // clipboard check, add-bill menus, quest generation — never the work scan
    // or tick code, so there is no perf concern patching a property getter here.
    //
    // Accepted behavior: because the work scan never consults AvailableNow,
    // toggling a recipe off hides it from the add-bill menu but does not suspend
    // bills that already exist for it — they keep producing. The toggle means
    // "stop offering this", not "ban it retroactively".
    [HarmonyPatch(typeof(RecipeDef), nameof(RecipeDef.AvailableNow), MethodType.Getter)]
    public static class RecipeDef_AvailableNow_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(RecipeDef __instance, ref bool __result)
        {
            if (__result && IsDisabledBySettings(__instance))
                __result = false;
        }

        // Whether this is one of the three toggled recipes and its toggle is
        // off. Also consulted by the skill check's recipe kind, so a recipe
        // the player has switched off stops standing in for "what crafting
        // the weapon would take".
        public static bool IsDisabledBySettings(RecipeDef recipe)
        {
            PWU_Settings settings = PWU_Mod.Settings;
            if (settings == null || recipe == null)
                return false;
            switch (recipe.defName)
            {
                case "PWU_Make_MonoSword":
                    return !settings.enableMonoswordRecipe;
                case "PWU_Make_PlasmaSword":
                    return !settings.enablePlasmaswordRecipe;
                case "PWU_Make_Zeushammer":
                    return !settings.enableZeushammerRecipe;
                default:
                    return false;
            }
        }
    }
}
