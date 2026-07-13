using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    public enum OpType
    {
        RemoveTrait,
        AddTrait,
        Rename,
        WipeMemory,

        // VPWE/VEF unified texture tab: a marker op only — carries no trait,
        // no cost, no refund. The actual payload rides
        // CustomizationSpec.vpweTexPaths, not this op;
        // JobDriver_CustomizeWeapon.ApplyOperationInner just re-applies
        // that field via VPWEIntegration.ApplyTexPaths when it sees this
        // op type. Staged by Dialog_WeaponCustomization.BuildOperations
        // whenever the Texture tab's selection diverges from the weapon's
        // original skin (see Dialog_WeaponCustomization.Texture.cs's
        // TextureChanged).
        Restyle,
    }

    // Which persona memory a OpType.WipeMemory op erases
    // (memory/polish spec §4). The two wipes are mutually exclusive by
    // construction (D17): a bond wipe's UnCode() also resets the kill
    // tracker, so at most one is ever staged per spec.
    public enum MemoryOpKind
    {
        None,
        WipeBonding,
        WipeKillTracker,
    }

    public class CustomizationOp : IExposable
    {
        public OpType type;
        public WeaponTraitDef trait;
        public List<ThingDefCountClass> cost;
        public List<ThingDefCountClass> refund;

        // Only for Rename ops (or merged into an AddTrait op when a rename is
        // deferred across a base→persona conversion).
        public string nameToApply;

        // Only for WipeMemory ops: which persona memory to erase.
        public MemoryOpKind memoryOp;

        public void ExposeData()
        {
            Scribe_Values.Look(ref type, "type");
            Scribe_Defs.Look(ref trait, "trait");
            Scribe_Collections.Look(ref cost, "cost", LookMode.Deep);
            Scribe_Collections.Look(ref refund, "refund", LookMode.Deep);
            Scribe_Values.Look(ref nameToApply, "nameToApply", null);
            Scribe_Values.Look(ref memoryOp, "memoryOp", MemoryOpKind.None);
        }
    }

    // Data transfer object between Dialog_WeaponCustomization and
    // JobDriver_CustomizeWeapon. The dialog writes this directly to the
    // driver's spec field on confirm via JobDriver_CustomizeWeapon.SetSpec,
    // so the (scribed) field carries it across save/reload taken in the
    // gap between the dialog's Close() and the consumeSpec toil.
    public class CustomizationSpec : IExposable
    {
        // Ordered operations: removals -> rename -> additions -> memory wipe.
        // Each op carries its own per-op cost and optional cosmetic changes.
        public List<CustomizationOp> operations;

        // The final ThingDef the weapon should have after all operations.
        // Used for def conversion decisions (base<->persona).
        public ThingDef resultingDef;

        // Aggregate net resource cost across all operations (addition costs minus
        // expected refunds). Used for pre-flight ingredient reservation and hauling.
        public List<ThingDefCountClass> totalCost;

        // Aggregate resource refund from all removal operations — under the persona
        // cost model, the AI persona core refunded by any removal that crosses the
        // persona->base boundary (§6). Initializes the job driver's virtual refund
        // ledger, which offsets addition costs and spawns any surplus at job end.
        public List<ThingDefCountClass> totalRefund;

        // The original weapon's VPWE/VEF composed-texture "skin" (VEF's
        // texPaths), captured by the dialog so the job can re-apply it onto
        // the new Thing whenever a base->persona conversion would otherwise roll a
        // fresh random skin (see VPWEIntegration). Null when VPWE/VEF
        // isn't active or the weapon has no customization comp — the common case.
        // A plain string list so it scribes without any VEF type dependency.
        public List<string> vpweTexPaths;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref operations, "operations", LookMode.Deep);
            Scribe_Defs.Look(ref resultingDef, "resultingDef");
            Scribe_Collections.Look(ref totalCost, "totalCost", LookMode.Deep);
            Scribe_Collections.Look(ref totalRefund, "totalRefund", LookMode.Deep);
            Scribe_Collections.Look(ref vpweTexPaths, "vpweTexPaths", LookMode.Value);
        }
    }
}
