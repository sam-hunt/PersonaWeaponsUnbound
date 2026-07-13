using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    // Validates trait combinations and provides filtered trait lists for the
    // weapon customization dialog. Operates purely on defs — no initialization needed.
    public static class TraitValidationUtility
    {
        // Maximum trait count, derived from the bladelink comp's own generation
        // range (vanilla IntRange(1, 2) → cap 2) rather than hardcoded, so a
        // mod that widens the range is honoured. The range is a private static
        // field on CompBladelinkWeapon, so it's read via reflection
        // once; falls back to 2 if the field can't be resolved.
        public static readonly int MaxTraits = DeriveMaxTraits();

        private static int DeriveMaxTraits()
        {
            FieldInfo field = typeof(CompBladelinkWeapon)
                .GetField("TraitsRange", BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null && field.GetValue(null) is IntRange range && range.max > 0)
                return range.max;
            Log.Warning("[Persona Weapons Unbound] Could not read CompBladelinkWeapon.TraitsRange "
                + "via reflection; defaulting the max trait cap to 2. RimWorld API may have changed.");
            return 2;
        }

        // Returns all persona (bladelink) weapon traits — the only ones a persona
        // weapon can carry. This is the full list shown in the UI; individual
        // traits may still be disabled based on the current desired selection.
        // personaDef is accepted for symmetry with the call sites but not
        // consulted: bladelink is a single category shared by every persona
        // weapon.
        public static List<WeaponTraitDef> GetCompatibleTraits(ThingDef personaDef)
        {
            var result = new List<WeaponTraitDef>();
            foreach (WeaponTraitDef trait in DefDatabase<WeaponTraitDef>.AllDefs)
            {
                // Only bladelink/persona traits. This is exactly
                // CompBladelinkWeapon.CanAddTrait's discriminator, so it's
                // authoritative for mod-added traits too.
                if (IsBladeLink(trait))
                    result.Add(trait);
            }
            return result;
        }

        // Returns null if the candidate trait can be added to the desired trait set,
        // or a human-readable rejection reason if it cannot.
        public static string GetRejectionReason(
            List<WeaponTraitDef> desiredTraits, WeaponTraitDef candidate)
        {
            if (desiredTraits.Contains(candidate))
                return "PWU_AlreadyApplied".Translate();

            if (PWU_Mod.Settings.enforceMaxTraitLimit && desiredTraits.Count >= MaxTraits)
                return "PWU_MaxTraitsReached".Translate();

            foreach (WeaponTraitDef existing in desiredTraits)
            {
                if (TraitsOverlap(candidate, existing))
                    return "PWU_ConflictsWith".Translate(existing.LabelCap);
            }

            if (PWU_Mod.Settings.enforceCanGenerateAlone
                && desiredTraits.Count == 0 && !candidate.canGenerateAlone)
                return "PWU_CannotBeOnlyTrait".Translate();

            return null;
        }

        // Whether the given trait can be removed from the desired set without
        // leaving an invalid configuration. Returns false if removal would leave
        // a single trait that has canGenerateAlone=false.
        public static bool CanRemoveTrait(
            List<WeaponTraitDef> desiredTraits, WeaponTraitDef toRemove)
        {
            if (!desiredTraits.Contains(toRemove))
                return false;

            if (PWU_Mod.Settings.enforceCanGenerateAlone && desiredTraits.Count == 2)
            {
                WeaponTraitDef remaining = desiredTraits[0] == toRemove
                    ? desiredTraits[1]
                    : desiredTraits[0];
                if (!remaining.canGenerateAlone)
                    return false;
            }

            return true;
        }

        // Returns the reason a trait cannot be removed, or null if removal is allowed.
        public static string GetRemovalRejectionReason(
            List<WeaponTraitDef> desiredTraits, WeaponTraitDef toRemove)
        {
            if (PWU_Mod.Settings.enforceCanGenerateAlone
                && !CanRemoveTrait(desiredTraits, toRemove) && desiredTraits.Count == 2)
            {
                WeaponTraitDef remaining = desiredTraits[0] == toRemove
                    ? desiredTraits[1]
                    : desiredTraits[0];
                if (!remaining.canGenerateAlone)
                    return "PWU_TraitCannotBeOnlyTrait".Translate(remaining.LabelCap);
            }
            return null;
        }

        // Whether two traits overlap — same def or shared exclusion tags.
        // Mirrors the vanilla WeaponTraitDef.Overlaps() logic.
        public static bool TraitsOverlap(WeaponTraitDef a, WeaponTraitDef b)
        {
            if (a == b)
                return true;

            if (a.exclusionTags.NullOrEmpty() || b.exclusionTags.NullOrEmpty())
                return false;

            return a.exclusionTags.Any(tag => b.exclusionTags.Contains(tag));
        }

        private static bool IsBladeLink(WeaponTraitDef trait)
        {
            return trait.weaponCategory == WeaponCategoryDefOf.BladeLink;
        }
    }
}
