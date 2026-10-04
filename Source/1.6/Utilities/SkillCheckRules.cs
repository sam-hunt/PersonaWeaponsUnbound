using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonaWeaponsUnbound
{
    // Who the optional skill prerequisite is evaluated against.
    public enum SkillCheckSubject
    {
        // No skill check; any pawn can customize (default).
        None,
        // The pawn ordered to perform the customization.
        CustomizingPawn,
        // Any player-faction colonist on the weapon's map.
        BestOnMap,
        // Any player-faction colonist anywhere in the world (maps, caravans,
        // travelling transporters).
        BestAnywhere,
    }

    // Which skill the prerequisite reads. Reprogramming a persona is closer to
    // hacking its AI than reworking the weapon, so Intellectual is the default;
    // Crafting treats it as bench craftsmanship instead (UWU's framing).
    public enum SkillCheckSkill
    {
        Intellectual,
        Crafting,
    }

    // What the skill prerequisite demands. Every level-based kind is demanded
    // in the checked skill (SkillCheckSkill), so a recipe that asks Crafting 10
    // becomes "Intellectual 10" under the default skill.
    public enum SkillCheckKind
    {
        // The level the weapon's own crafting recipe demands (see
        // WeaponRecipeIndex); weapons no recipe produces fall back to a
        // per-tech-tier minimum.
        RecipeOrTechTier,
        // The per-tech-tier minimum for every weapon, recipe or not (the same
        // table RecipeOrTechTier falls back to).
        TechTier,
        // A flat minimum from the settings slider.
        FlatMinimum,
        // Vanilla Skills Expanded's expertise for the checked skill (hacking
        // for Intellectual, weaponsmithing for Crafting). When that expertise
        // is unavailable (no VSE, or VSE without the DLC it gates the
        // expertise on) this resolves to FlatMinimum at ExpertiseFallbackLevel.
        Expertise,
    }

    // The optional skill prerequisite for customization: setting resolution
    // (including the VSE fallback), per-weapon requirement derivation, and the
    // pawn/colony evaluation the entry points consume as AcceptanceReports.
    //
    // Vanilla persona weapons are all ultratech and uncraftable, but nothing in
    // the API holds other mods to that, and several add persona weapons at
    // other tech levels or with recipes, so the recipe and tech-tier kinds are
    // derived from the weapon exactly as UWU does rather than assumed away.
    //
    // Placement in the prerequisite chain: after the research/quality checks
    // (global settings the player controls) and before pathing and the
    // workbench search (the expensive, situational checks). With the subject
    // set to None every entry here returns immediately, so players who never
    // touch the setting see no behaviour or cost change.
    public static class SkillCheckRules
    {
        // Flat skill minimum used when the expertise option is selected but
        // the expertise is unavailable. 15 is the skill level VSE itself
        // requires before a pawn can take an expertise.
        public const int ExpertiseFallbackLevel = 15;

        public const int MinFlatLevel = 0;
        public const int MaxFlatLevel = 20;

        // The tiers the table below distinguishes, lowest first; the settings
        // UI enumerates this to describe the table.
        public static readonly TechLevel[] TechTiers =
        {
            TechLevel.Neolithic, TechLevel.Medieval, TechLevel.Industrial,
            TechLevel.Spacer, TechLevel.Ultra, TechLevel.Archotech,
        };

        // Fallback skill minimum for weapons with no recipe, by tech tier. The
        // same table as UWU's, derived from a survey of every craftable vanilla
        // weapon's recipe requirement (Core + all DLC, 2026-08): Neolithic
        // median 3 / max 6, Medieval 3 / 5, Industrial 5 / 7, Spacer 8 / 9.
        // Uncraftable weapons at a tier are its exotic end, so each tier sits at
        // the upper end of its craftable range; Ultra and Archotech have no
        // craftable vanilla weapons at all and continue the roughly +3-per-tier
        // trend (Archotech lands on VSE's expertise threshold). Vanilla persona
        // weapons are Ultra, so without a recipe they land on 12. Animal and
        // Undefined fall in with Neolithic.
        public static int TechTierMinimumSkill(TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.Medieval:
                    return 5;
                case TechLevel.Industrial:
                    return 7;
                case TechLevel.Spacer:
                    return 9;
                case TechLevel.Ultra:
                    return 12;
                case TechLevel.Archotech:
                    return 15;
                default: // Undefined, Animal, Neolithic
                    return 4;
            }
        }

        // The tech level the tier table reads for a weapon: its own def's, or
        // its paired base/persona def's when its own is Undefined (a modded
        // persona variant that forgot the field inherits its base weapon's
        // tier rather than dropping to the Neolithic floor).
        public static TechLevel GetWeaponTechLevel(ThingDef baseDef, ThingDef personaDef, ThingDef ownDef)
        {
            if (ownDef != null && ownDef.techLevel != TechLevel.Undefined)
                return ownDef.techLevel;
            ThingDef other = ownDef == baseDef ? personaDef : baseDef;
            return other != null ? other.techLevel : TechLevel.Undefined;
        }

        public static bool Enabled => PWU_Mod.Settings.skillCheckSubject != SkillCheckSubject.None;

        public static SkillDef SkillDefFor(SkillCheckSkill skill)
        {
            return skill == SkillCheckSkill.Crafting ? SkillDefOf.Crafting : SkillDefOf.Intellectual;
        }

        // The VSE expertise paired with each skill.
        public static string ExpertiseDefNameFor(SkillCheckSkill skill)
        {
            return skill == SkillCheckSkill.Crafting
                ? VanillaSkillsExpandedIntegration.WeaponsmithDefName
                : VanillaSkillsExpandedIntegration.HackerDefName;
        }

        // Whether the expertise paired with the skill can actually be checked:
        // VSE's API resolved and the def exists in the live database (VSE
        // gates hacking on Ideology, which this mod does not require).
        public static bool ExpertiseAvailableFor(SkillCheckSkill skill)
        {
            return VanillaSkillsExpandedIntegration.Available
                && VanillaSkillsExpandedIntegration.ExpertiseDefExists(ExpertiseDefNameFor(skill));
        }

        // The expertise's live label for player-facing text, with a keyed
        // fallback when the def can't be read.
        public static string ExpertiseLabelFor(SkillCheckSkill skill)
        {
            string fallback = skill == SkillCheckSkill.Crafting
                ? "PWU_WeaponsmithingFallbackLabel".Translate()
                : "PWU_HackingFallbackLabel".Translate();
            return VanillaSkillsExpandedIntegration.ExpertiseLabel(ExpertiseDefNameFor(skill), fallback);
        }

        // Whether the stored Expertise selection is currently being substituted
        // by the flat fallback because the expertise is unavailable.
        public static bool ExpertiseFallbackActive =>
            PWU_Mod.Settings.skillCheckKind == SkillCheckKind.Expertise
            && !ExpertiseAvailableFor(PWU_Mod.Settings.skillCheckSkill);

        // The kind actually in force, after the VSE fallback. flatLevel is the
        // level the FlatMinimum kind would demand (the fallback level when
        // substituting, otherwise the slider value).
        public static SkillCheckKind EffectiveKind(out int flatLevel)
        {
            PWU_Settings settings = PWU_Mod.Settings;
            if (ExpertiseFallbackActive)
            {
                flatLevel = ExpertiseFallbackLevel;
                return SkillCheckKind.FlatMinimum;
            }
            flatLevel = Mathf.Clamp(settings.skillCheckMinimumLevel, MinFlatLevel, MaxFlatLevel);
            return settings.skillCheckKind;
        }

        // What a pawn must satisfy under the current settings: a skill
        // requirement (vanilla's own type, so PawnSatisfies covers player mechs
        // exactly as it does for bills) or an expertise, never both.
        public sealed class Requirement
        {
            public SkillRequirement Skill;
            public string ExpertiseDefName;

            public bool IsEmpty => Skill == null && ExpertiseDefName == null;
        }

        // Builds the requirement for a weapon Thing: resolves its base/persona
        // pairing and tech level, then derives as below.
        public static Requirement GetRequirement(Thing weapon)
        {
            WeaponRegistry.ResolveWeaponDefs(weapon, out ThingDef baseDef, out ThingDef personaDef);
            return GetRequirement(baseDef, personaDef, GetWeaponTechLevel(baseDef, personaDef, weapon.def));
        }

        // Builds the requirement for a weapon from its base/persona defs and
        // tech level under the current settings. Never null; empty when nothing
        // is demanded (a recipe with no skill requirement, so anyone who could
        // craft the weapon may reprogram it, or a flat minimum of 0).
        public static Requirement GetRequirement(ThingDef baseDef, ThingDef personaDef, TechLevel techLevel)
        {
            var requirement = new Requirement();
            SkillCheckSkill skill = PWU_Mod.Settings.skillCheckSkill;
            switch (EffectiveKind(out int flatLevel))
            {
                case SkillCheckKind.Expertise:
                    requirement.ExpertiseDefName = ExpertiseDefNameFor(skill);
                    break;

                case SkillCheckKind.FlatMinimum:
                    SetLevel(requirement, skill, flatLevel);
                    break;

                case SkillCheckKind.TechTier:
                    SetLevel(requirement, skill, TechTierMinimumSkill(techLevel));
                    break;

                default: // RecipeOrTechTier
                    // The base weapon's recipe is the natural craft path (this
                    // mod's own recipes make base weapons); a persona def with
                    // a recipe of its own is consulted when the base has none.
                    if (WeaponRecipeIndex.TryGetRequiredLevel(baseDef, out int level)
                        || WeaponRecipeIndex.TryGetRequiredLevel(personaDef, out level))
                    {
                        SetLevel(requirement, skill, level);
                    }
                    else
                    {
                        SetLevel(requirement, skill, TechTierMinimumSkill(techLevel));
                    }
                    break;
            }
            return requirement;
        }

        private static void SetLevel(Requirement requirement, SkillCheckSkill skill, int minLevel)
        {
            if (minLevel > 0)
            {
                requirement.Skill = new SkillRequirement
                {
                    skill = SkillDefFor(skill),
                    minLevel = minLevel,
                };
            }
        }

        // Whether one pawn meets the requirement.
        public static bool PawnSatisfies(Pawn pawn, Requirement requirement)
        {
            if (pawn == null)
                return false;
            if (requirement.ExpertiseDefName != null
                && !VanillaSkillsExpandedIntegration.HasExpertise(pawn, requirement.ExpertiseDefName))
            {
                return false;
            }
            return requirement.Skill?.PawnSatisfies(pawn) != false;
        }

        // The prerequisite check the entry points call. pawn may be null for a
        // pawn-independent evaluation (the ground-weapon gizmo before a colonist
        // is picked); under CustomizingPawn that defers the check to the
        // targeter, while the colony-wide subjects are answered fully. weapon
        // supplies the map for BestOnMap (MapHeld, so an equipped or carried
        // weapon resolves to its holder's map).
        public static AcceptanceReport GetReport(Pawn pawn, Thing weapon)
        {
            SkillCheckSubject subject = PWU_Mod.Settings.skillCheckSubject;
            if (subject == SkillCheckSubject.None)
                return true;

            Requirement requirement = GetRequirement(weapon);
            if (requirement.IsEmpty)
                return true;

            switch (subject)
            {
                case SkillCheckSubject.CustomizingPawn:
                    if (pawn == null || PawnSatisfies(pawn, requirement))
                        return true;
                    return PawnShortfall(pawn, requirement);

                case SkillCheckSubject.BestOnMap:
                {
                    Map map = weapon.MapHeld ?? pawn?.MapHeld;
                    if (map != null && AnyColonistSatisfies(
                        map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer), requirement))
                    {
                        return true;
                    }
                    return ColonyShortfall(requirement, onMap: true);
                }

                default: // BestAnywhere
                    if (AnyColonistSatisfies(
                        PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists, requirement))
                    {
                        return true;
                    }
                    return ColonyShortfall(requirement, onMap: false);
            }
        }

        private static bool AnyColonistSatisfies(List<Pawn> pawns, Requirement requirement)
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p.IsColonist && PawnSatisfies(p, requirement))
                    return true;
            }
            return false;
        }

        // Rejection text for a specific pawn: vanilla's SkillTooLow ("need
        // intellectual level 15, have 10") or the expertise line.
        private static string PawnShortfall(Pawn pawn, Requirement requirement)
        {
            if (requirement.ExpertiseDefName != null)
                return "PWU_RequiresExpertise".Translate(CurrentExpertiseLabel());
            SkillRequirement sr = requirement.Skill;
            return "SkillTooLow".Translate(sr.skill.skillLabel, SkillLevel(pawn, sr.skill), sr.minLevel);
        }

        // Rejection text when no colonist qualifies.
        private static string ColonyShortfall(Requirement requirement, bool onMap)
        {
            if (requirement.ExpertiseDefName != null)
            {
                return (onMap ? "PWU_NoColonistOnMapWithExpertise" : "PWU_NoColonistWithExpertise")
                    .Translate(CurrentExpertiseLabel());
            }
            SkillRequirement sr = requirement.Skill;
            return (onMap ? "PWU_NoColonistOnMapWithSkill" : "PWU_NoColonistWithSkill")
                .Translate(sr.skill.skillLabel, sr.minLevel);
        }

        // Mouse-attached line for the ground-weapon targeter under the
        // CustomizingPawn subject: the hovered pawn's shortfall in the same
        // words the float menus and the rejection message use (vanilla's
        // SkillTooLow, or the expertise line), so the player can pick a
        // qualified colonist without guessing. Null when the pawn qualifies
        // or nothing applies, since a qualifying pawn needs no annotation.
        public static string GetTargeterTip(Pawn pawn, Requirement requirement)
        {
            if (pawn == null || requirement?.IsEmpty != false || PawnSatisfies(pawn, requirement))
                return null;
            return PawnShortfall(pawn, requirement);
        }

        // The pawn's displayed level in a skill: the skill record's level, or a
        // player mech's fixed level when the mech can do work that uses the
        // skill (the same condition SkillRequirement.PawnSatisfies applies, so
        // the shortfall can't read "need level 10, have 10"), or 0 otherwise.
        private static int SkillLevel(Pawn pawn, SkillDef skill)
        {
            SkillRecord record = pawn.skills?.GetSkill(skill);
            if (record != null)
                return record.Level;
            if (pawn.IsColonyMechPlayerControlled
                && pawn.RaceProps.mechEnabledWorkTypes.Any(w => w.relevantSkills.NotNullAndContains(skill)))
            {
                return pawn.RaceProps.mechFixedSkillLevel;
            }
            return 0;
        }

        private static string CurrentExpertiseLabel()
        {
            return ExpertiseLabelFor(PWU_Mod.Settings.skillCheckSkill);
        }
    }
}
