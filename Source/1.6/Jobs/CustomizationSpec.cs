using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    public enum OpType
    {
        RemoveTrait,
        AddTrait,
        ApplyCosmetics,
    }

    public class CustomizationOp : IExposable
    {
        public OpType type;
        public WeaponTraitDef trait;
        public List<ThingDefCountClass> cost;
        public List<ThingDefCountClass> refund;

        // Optional color change on this specific op (null = no color change at this step)
        public ColorDef colorToApply;

        // If true, clear the color to default (no tint). Used when removing a
        // forced-color trait with no remaining forced-color traits active.
        public bool clearColor;

        // Only for ApplyCosmetics ops
        public string nameToApply;

        public void ExposeData()
        {
            Scribe_Values.Look(ref type, "type");
            Scribe_Defs.Look(ref trait, "trait");
            Scribe_Collections.Look(ref cost, "cost", LookMode.Deep);
            Scribe_Collections.Look(ref refund, "refund", LookMode.Deep);
            Scribe_Defs.Look(ref colorToApply, "colorToApply");
            Scribe_Values.Look(ref clearColor, "clearColor", false);
            Scribe_Values.Look(ref nameToApply, "nameToApply", null);
        }
    }

    /// <summary>
    /// Data transfer object between Dialog_WeaponCustomization and
    /// JobDriver_CustomizeWeapon. The dialog writes this directly to the
    /// driver's spec field on confirm via JobDriver_CustomizeWeapon.SetSpec,
    /// so the (scribed) field carries it across save/reload taken in the
    /// gap between the dialog's Close() and the consumeSpec toil.
    /// </summary>
    public class CustomizationSpec : IExposable
    {
        /// <summary>
        /// Ordered operations: removals → cosmetics → additions.
        /// Each op carries its own per-op cost and optional cosmetic changes.
        /// </summary>
        public List<CustomizationOp> operations;

        /// <summary>
        /// The final ThingDef the weapon should have after all operations.
        /// Used for def conversion decisions (base↔persona).
        /// </summary>
        public ThingDef resultingDef;

        /// <summary>
        /// Aggregate net resource cost across all operations (addition costs minus
        /// expected refunds). Used for pre-flight ingredient reservation and hauling.
        /// </summary>
        public List<ThingDefCountClass> totalCost;

        /// <summary>
        /// Aggregate resource refund from all removal operations — under the persona
        /// cost model, the AI persona core refunded by any removal that crosses the
        /// persona→base boundary (§6). Initializes the job driver's virtual refund
        /// ledger, which offsets addition costs and spawns any surplus at job end.
        /// </summary>
        public List<ThingDefCountClass> totalRefund;

        /// <summary>
        /// The final color to apply after all operations complete.
        /// Set from EffectiveColor in the dialog. Applied in the finalize toil
        /// to ensure it persists through Setup() calls and def conversions.
        /// Null means no color change (e.g., reverting to base with no colorable comp).
        /// Only ever holds a DefDatabase-registered ColorDef so it Scribes safely;
        /// the "revert to default tint" case is carried by <see cref="finalColorClear"/>.
        /// </summary>
        public ColorDef finalColor;

        /// <summary>
        /// When true, the finalize toil deactivates CompColorable (reverts the weapon
        /// to its default persona tint) instead of applying <see cref="finalColor"/>.
        /// Used when the chosen color is the default swatch (an unregistered runtime
        /// ColorDef that must not be Scribed).
        /// </summary>
        public bool finalColorClear;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref operations, "operations", LookMode.Deep);
            Scribe_Defs.Look(ref resultingDef, "resultingDef");
            Scribe_Collections.Look(ref totalCost, "totalCost", LookMode.Deep);
            Scribe_Collections.Look(ref totalRefund, "totalRefund", LookMode.Deep);
            Scribe_Defs.Look(ref finalColor, "finalColor");
            Scribe_Values.Look(ref finalColorClear, "finalColorClear", false);
        }
    }
}
