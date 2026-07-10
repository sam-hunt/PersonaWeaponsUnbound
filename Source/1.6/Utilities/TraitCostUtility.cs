using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonaWeaponsUnbound
{
    /// <summary>
    /// Prices persona-weapon trait changes (fork spec §6). Costs no longer depend
    /// on which trait is changing — only on whether the change crosses the 0↔1
    /// trait-count boundary (base↔persona def conversion) and, for non-boundary
    /// changes, the weapon's quality:
    ///   - First trait added to a base weapon: 1 AI persona core, no components.
    ///   - Last trait removed from a persona weapon: refunds 1 AI persona core.
    ///   - Every other addition or removal: N advanced components (both
    ///     directions cost — never refund).
    /// </summary>
    public static class TraitCostUtility
    {
        /// <summary>
        /// The resource cost of one trait change on <paramref name="weapon"/>.
        /// Boundary additions cost a persona core; boundary removals cost nothing
        /// (the refund side is reported separately by <see cref="GetChangeRefund"/>);
        /// every other change costs advanced components scaled by the weapon's
        /// current quality.
        /// </summary>
        public static List<ThingDefCountClass> GetChangeCost(
            Thing weapon, bool crossesConversionBoundary, bool isRemoval)
        {
            if (crossesConversionBoundary)
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

        /// <summary>
        /// The resource refund for one trait change. The only refund in the
        /// persona cost model is the whole AI persona core, paid when a removal
        /// crosses the 0↔1 boundary (the weapon's last trait comes off and it
        /// reverts to its base def). Every other change — including boundary
        /// additions — refunds nothing.
        /// </summary>
        public static List<ThingDefCountClass> GetChangeRefund(
            bool crossesConversionBoundary, bool isRemoval)
        {
            if (crossesConversionBoundary && isRemoval)
            {
                return new List<ThingDefCountClass>
                {
                    new ThingDefCountClass(ThingDefOf.AIPersonaCore, 1),
                };
            }
            return new List<ThingDefCountClass>();
        }

        /// <summary>
        /// The advanced-component count for a non-boundary trait change: a flat
        /// base cost plus a per-level surcharge for every quality tier above the
        /// given threshold. Takes the three cost knobs as explicit parameters
        /// (rather than reading <see cref="PWU_Mod.Settings"/> directly) so the
        /// settings-page live cost table can recompute this every frame from
        /// the current slider values — including unsaved ones — without
        /// duplicating the formula.
        /// </summary>
        public static int ComponentCostForQuality(
            QualityCategory quality,
            int baseComponentCost,
            QualityCategory surchargeThreshold,
            int surchargePerLevel)
        {
            int levelsAboveThreshold = Mathf.Max(0, (int)quality - (int)surchargeThreshold);
            return baseComponentCost + Mathf.RoundToInt(levelsAboveThreshold * surchargePerLevel);
        }

        /// <summary>
        /// The weapon's quality, or Normal if it has no <see cref="CompQuality"/>.
        /// </summary>
        private static QualityCategory GetQuality(Thing weapon)
        {
            CompQuality qualityComp = weapon?.TryGetComp<CompQuality>();
            return qualityComp?.Quality ?? QualityCategory.Normal;
        }

        /// <summary>
        /// Returns true if the trait is "negative" (undesirable): either its
        /// MarketValue stat factor is below 1, or its flat marketValueOffset is
        /// negative (e.g. vanilla's ThoughtWailing at -1000). Purely a UI signal
        /// (row tint, hide-negative filter) — under the persona cost model it has
        /// no effect on cost or refund (both directions cost the same regardless
        /// of trait polarity).
        /// </summary>
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
