using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    /// <summary>
    /// Stateless game-rule predicates for determining whether weapons are
    /// customizable and what research is required.
    /// </summary>
    public static class CustomizationRules
    {
        /// <summary>
        /// Whether this weapon has a customization path and the player has unlocked
        /// the required customization research. Returns AcceptanceReport with a
        /// rejection reason if not customizable. Returns false with no reason when
        /// the option should be hidden entirely.
        /// </summary>
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

            if (PWU_Mod.Settings.requireCustomizationResearch)
            {
                // Don't surface customization UI until the player has completed UniqueSmithing,
                // so we don't clutter menus for uninterested players. Bypassed in dev mode so the
                // per-weapon research blocker is shown instead.
                if (!Prefs.DevMode && !PWU_ResearchDefOf.UniqueSmithing.IsFinished)
                    return false;

                ResearchProjectDef requiredResearch = GetRequiredResearch(def.techLevel);
                if (!requiredResearch.IsFinished)
                    return "PWU_RequiresResearch".Translate(requiredResearch.label);
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

        /// <summary>
        /// Returns the required research project for customizing weapons of the
        /// given tech level. Persona weapons are all ultratech, but the fallthrough
        /// still lets modded weapons with unusual tech levels resolve to the
        /// nearest tier. Kept as a three-project ladder for now; a future pass
        /// collapses this to the single PWU_BladelinkCustomization project (§7).
        /// </summary>
        public static ResearchProjectDef GetRequiredResearch(TechLevel techLevel)
        {
            if (techLevel >= TechLevel.Spacer)
                return PWU_ResearchDefOf.UniqueFabrication;
            if (techLevel >= TechLevel.Industrial)
                return PWU_ResearchDefOf.UniqueMachining;
            return PWU_ResearchDefOf.UniqueSmithing;
        }

        /// <summary>
        /// Whether the player has completed the required research for the given tech level.
        /// </summary>
        public static bool HasRequiredResearch(TechLevel techLevel)
        {
            ResearchProjectDef required = GetRequiredResearch(techLevel);
            return required != null && required.IsFinished;
        }

        /// <summary>
        /// Rejection report for paths that are normally hidden (silent <c>false</c>).
        /// In dev mode, surfaces the reason so the option/gizmo renders as visible-but-disabled,
        /// letting modders diagnose why a weapon isn't customizable without exporting logs.
        /// </summary>
        private static AcceptanceReport HiddenUnlessDev(string devReason)
        {
            if (!Prefs.DevMode)
                return false;
            return devReason;
        }

        /// <summary>
        /// Returns the weapon's tech level if it participates in the customization system.
        /// Returns TechLevel.Undefined if the weapon has no customization path.
        /// </summary>
        public static TechLevel GetWeaponTechLevel(Thing weapon)
        {
            ThingDef def = weapon.def;

            if (WeaponRegistry.IsPersonaWeapon(def))
                return def.techLevel;

            if (WeaponRegistry.GetPersonaVariant(def) != null)
                return def.techLevel;

            return TechLevel.Undefined;
        }
    }
}
