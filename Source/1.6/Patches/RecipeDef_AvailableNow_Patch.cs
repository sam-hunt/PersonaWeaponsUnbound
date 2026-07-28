using HarmonyLib;
using Verse;

namespace PersonaWeaponsUnbound.Patches
{
    // Gates the three PWU weapon-crafting recipes (fork spec §10) and the
    // optional persona-core recipe behind their
    // individual settings toggles by postfixing the AvailableNow property
    // getter. No def surgery, so it works mid-save with no restart.
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
            if (!__result || PWU_Mod.Settings == null)
                return;

            switch (__instance.defName)
            {
                case "PWU_Make_MonoSword":
                    if (!PWU_Mod.Settings.enableMonoswordRecipe)
                        __result = false;
                    break;
                case "PWU_Make_PlasmaSword":
                    if (!PWU_Mod.Settings.enablePlasmaswordRecipe)
                        __result = false;
                    break;
                case "PWU_Make_Zeushammer":
                    if (!PWU_Mod.Settings.enableZeushammerRecipe)
                        __result = false;
                    break;
                case "PWU_Make_AIPersonaCore":
                    if (!PWU_Mod.Settings.enablePersonaCoreRecipe)
                        __result = false;
                    break;
            }
        }
    }
}
