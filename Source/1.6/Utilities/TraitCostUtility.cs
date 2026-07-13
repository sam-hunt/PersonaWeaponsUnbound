using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonaWeaponsUnbound
{
    // Prices persona-weapon trait changes (fork spec §6). Costs no longer depend
    // on which trait is changing — only on whether the change crosses the 0↔1
    // trait-count boundary (base↔persona def conversion) and, for non-boundary
    // changes, the weapon's quality:
    //   - First trait added to a base weapon: 1 AI persona core, no components.
    //   - Last trait removed from a persona weapon: refunds 1 AI persona core.
    //   - Every other addition or removal: N advanced components (both
    //     directions cost — never refund).
    // When PWU_Settings.firstTraitCostsPersonaCore is disabled, the
    // two boundary cases above lose their persona-core special-casing and are
    // priced exactly like any other change: N advanced components in both
    // directions, never refunded.
    public static class TraitCostUtility
    {
        // The resource cost of one trait change on weapon.
        // Boundary additions cost a persona core; boundary removals cost nothing
        // (the refund side is reported separately by GetChangeRefund);
        // every other change costs advanced components scaled by the weapon's
        // current quality.
        public static List<ThingDefCountClass> GetChangeCost(
            Thing weapon, bool crossesConversionBoundary, bool isRemoval)
        {
            if (crossesConversionBoundary && PWU_Mod.Settings.firstTraitCostsPersonaCore)
            {
                if (isRemoval)
                    return new List<ThingDefCountClass>();
                return new List<ThingDefCountClass>
                {
                    new ThingDefCountClass(ThingDefOf.AIPersonaCore, 1),
                };
            }

            int componentCount = ComponentCostForQuality(
                GetQuality(weapon),
                PWU_Mod.Settings.traitChangeBaseComponentCost,
                PWU_Mod.Settings.traitChangeQualitySurchargeThreshold,
                PWU_Mod.Settings.traitChangeQualitySurchargePerLevel);
            if (componentCount <= 0)
                return new List<ThingDefCountClass>();
            return new List<ThingDefCountClass>
            {
                new ThingDefCountClass(ThingDefOf.ComponentSpacer, componentCount),
            };
        }

        // The resource refund for one trait change. The only refund in the
        // persona cost model is the whole AI persona core, paid when a removal
        // crosses the 0↔1 boundary (the weapon's last trait comes off and it
        // reverts to its base def). Every other change — including boundary
        // additions — refunds nothing. When
        // PWU_Settings.firstTraitCostsPersonaCore is disabled the
        // core is never installed, so this refund never applies either.
        public static List<ThingDefCountClass> GetChangeRefund(
            bool crossesConversionBoundary, bool isRemoval)
        {
            if (crossesConversionBoundary && isRemoval
                && PWU_Mod.Settings.firstTraitCostsPersonaCore)
            {
                return new List<ThingDefCountClass>
                {
                    new ThingDefCountClass(ThingDefOf.AIPersonaCore, 1),
                };
            }
            return new List<ThingDefCountClass>();
        }

        // The advanced-component count for a non-boundary trait change: a flat
        // base cost plus a per-level surcharge for every quality tier above the
        // given threshold. Takes the three cost knobs as explicit parameters
        // (rather than reading PWU_Mod.Settings directly) so the
        // settings-page live cost table can recompute this every frame from
        // the current slider values — including unsaved ones — without
        // duplicating the formula.
        public static int ComponentCostForQuality(
            QualityCategory quality,
            int baseComponentCost,
            QualityCategory surchargeThreshold,
            int surchargePerLevel)
        {
            int levelsAboveThreshold = Mathf.Max(0, (int)quality - (int)surchargeThreshold);
            return baseComponentCost + Mathf.RoundToInt(levelsAboveThreshold * surchargePerLevel);
        }

        // The weapon's quality, or Normal if it has no CompQuality.
        private static QualityCategory GetQuality(Thing weapon)
        {
            CompQuality qualityComp = weapon?.TryGetComp<CompQuality>();
            return qualityComp?.Quality ?? QualityCategory.Normal;
        }

        // Returns true if the trait is "negative" (undesirable): either its
        // MarketValue stat factor is below 1, or its flat marketValueOffset is
        // negative (e.g. vanilla's ThoughtWailing at -1000). Purely a UI signal
        // (row tint, hide-negative filter) — under the persona cost model it has
        // no effect on cost or refund (both directions cost the same regardless
        // of trait polarity).
        public static bool IsNegativeTrait(WeaponTraitDef trait)
        {
            if (trait.statFactors != null)
            {
                for (int i = 0; i < trait.statFactors.Count; i++)
                {
                    if (trait.statFactors[i].stat == StatDefOf.MarketValue
                        && trait.statFactors[i].value < 1f)
                        return true;
                }
            }
            return trait.marketValueOffset < 0f;
        }
    }
}
