using System.Collections.Generic;
using System.Xml;
using Verse;

namespace PersonaWeaponsUnbound
{
    // XML-patch gate for the optional persona-core recipe
    // (PWU_Settings.enablePersonaCoreRecipe, default off). Runs its nested
    // operation only while the setting is off, and reports success without
    // touching the XML while it's on. Used by 1.6/Patches/PersonaCoreRecipeGate.xml
    // to orphan PWU_Make_AIPersonaCore (drop its researchPrerequisite and
    // recipeUsers) so nothing anywhere advertises it: vanilla's research
    // "Unlocks" row keys off the prerequisite, the bench's bill menu keys off
    // recipeUsers, and research-tree mods (Fluffy's lineage: Research Tree,
    // ResearchPal) rebuild unlock lists from those same two fields directly
    // rather than through vanilla's getters, which is why a Harmony filter on
    // ResearchProjectDef.UnlockedDefs was never enough for them.
    //
    // The def itself stays loaded, deliberately. Removing it would make the
    // eight DefInjected language files log a translation warning every boot,
    // break the PWU_RecipeDefOf binding, and drop any queued bills from saves
    // with a red error. An orphaned recipe is an ordinary def state: bills
    // already queued for it keep producing (the work scan never checks
    // recipeUsers), and the cost/skill sliders keep live-applying to it.
    //
    // Mod settings are readable here because LoadedModManager creates Mod
    // classes (which is where PWU_Mod loads its settings) before it applies
    // XML patches. The gate is evaluated once at load, so flipping the toggle
    // mid-session takes effect at the next restart; RecipeEnabledAtLoad lets
    // the settings UI say so.
    public class PatchOperation_UnlessPersonaCoreRecipeEnabled : PatchOperation
    {
        // Loaded from XML by DirectXmlToObject, like PatchOperationConditional's
        // match/nomatch.
        private PatchOperation operation;

        // The toggle value the gate saw when it ran, or null if it never ran
        // (patch failed to load). The settings window compares this against
        // the current value to show its restart note.
        public static bool? RecipeEnabledAtLoad { get; private set; }

        public PatchOperation_UnlessPersonaCoreRecipeEnabled()
        {
        }

        // For tests, which can't go through DirectXmlToObject headlessly.
        internal PatchOperation_UnlessPersonaCoreRecipeEnabled(PatchOperation operation)
        {
            this.operation = operation;
        }

        // Settings null (a corrupt settings file leaving GetSettings returning
        // null) reads as the default, off, so the recipe stays hidden: the
        // opt-in direction is the safe one to fail towards.
        internal static bool RecipeEnabled(PWU_Settings settings)
            => settings != null && settings.enablePersonaCoreRecipe;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            bool enabled = RecipeEnabled(PWU_Mod.Settings);
            RecipeEnabledAtLoad = enabled;
            if (enabled)
                return true;

            if (operation == null)
            {
                Log.Error("[Persona Weapons Unbound] " + GetType().Name
                    + " has no nested <operation>; the persona core recipe was left fully enabled.");
                return false;
            }

            return operation.Apply(xml);
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (operation == null)
                yield return GetType().Name + " requires a nested <operation>.";
        }

        public override string ToString()
            => base.ToString() + "(" + (operation != null ? operation.ToString() : "null") + ")";
    }
}
