using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    // Stateless game-rule predicates for determining whether weapons are
    // customizable and what research is required.
    public static class CustomizationRules
    {
        // Whether this weapon has a customization path and the player has unlocked
        // the required customization research. Returns AcceptanceReport with a
        // rejection reason if not customizable. Returns false with no reason when
        // the option should be hidden entirely.
        public static AcceptanceReport IsCustomizable(Thing weapon)
        {
            ThingDef def = weapon.def;

            if (WeaponRegistry.IsPersonaWeapon(def))
            {
                // Persona weapons are always in the customization system
                // regardless of whether a base def exists.
            }
            else
            {
                if (WeaponRegistry.GetPersonaVariant(def) == null)
                    return HiddenUnlessDev("PWU_DevNoPersonaVariant".Translate());

                // When def conversion is disabled, only already-persona weapons
                // can enter the customization system.
                if (!PWU_Mod.Settings.allowDefConversion)
                    return HiddenUnlessDev("PWU_DevDefConversionDisabled".Translate());
            }

            if (PWU_Mod.Settings.requireCustomizationResearch
                && !PWU_ResearchDefOf.PWU_BladelinkCustomization.IsFinished)
            {
                // Don't surface customization UI until the player has completed the research,
                // so we don't clutter menus for uninterested players. Bypassed in dev mode so the
                // per-weapon research blocker is shown instead.
                if (!Prefs.DevMode)
                    return false;

                return "PWU_RequiresResearch".Translate(PWU_ResearchDefOf.PWU_BladelinkCustomization.label);
            }

            QualityCategory minQuality = PWU_Mod.Settings.minimumQuality;
            if (minQuality > QualityCategory.Awful
                && weapon.TryGetQuality(out QualityCategory quality)
                && quality < minQuality)
            {
                return "PWU_RequiresMinimumQuality".Translate(minQuality.GetLabel());
            }

            return true;
        }

        // Rejection report for paths that are normally hidden (silent false).
        // In dev mode, surfaces the reason so the option/gizmo renders as visible-but-disabled,
        // letting modders diagnose why a weapon isn't customizable without exporting logs.
        private static AcceptanceReport HiddenUnlessDev(string devReason)
        {
            if (!Prefs.DevMode)
                return false;
            return devReason;
        }
    }
}
