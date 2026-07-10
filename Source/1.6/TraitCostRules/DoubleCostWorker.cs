using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    /// <summary>
    /// Doubles all costs. Used for traits like akimbo that add a second
    /// rendered weapon and double the fire rate.
    /// </summary>
    public class DoubleCostWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            CostRuleHelpers.ApplyCostMultiplier(costs, 2f);
        }
    }
}
