using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    // Mutates a weapon Thing in place: adds/removes persona (bladelink) traits,
    // sets cosmetic properties (name), and spawns refunded resources.
    // Def conversion (base↔persona) lives in WeaponDefConversion; ingredient
    // gathering and reservation for a customization job lives in
    // HaulPlanning.IngredientReservation.
    //
    // CompBladelinkWeapon.TraitsListForReading is the live private trait list —
    // mutated directly, no reflection. But nothing re-fires the per-trait
    // Worker.Notify_* hooks or (un)applies hediffs on a raw list mutation, so
    // every add/remove here manually reproduces the bonding side effects
    // vanilla would fire at bond/equip time (fork spec §5).
    public static class WeaponModificationUtility
    {
        // CompGeneratedNames.name — the persona weapon's display name. Private, no
        // public setter, but Scribed as "name" so a reflected write persists across
        // save/load. Resolved once at static-init; verified by VerifyReflection() so
        // a RimWorld API rename surfaces as a startup error instead of a silent
        // no-op on every SetName.
        internal static readonly FieldInfo CompNameField = typeof(CompGeneratedNames)
            .GetField("name", BindingFlags.NonPublic | BindingFlags.Instance);

        // CompBladelinkWeapon.lastKillTick — private int, Scribed as "lastKillTick".
        // Reset to "now" when NeedKill is added to an already-bonded weapon so a
        // long-standing bond doesn't instantly fire the −4 kill-thirst mood (spec D9).
        internal static readonly FieldInfo LastKillTickField = typeof(CompBladelinkWeapon)
            .GetField("lastKillTick", BindingFlags.NonPublic | BindingFlags.Instance);

        // Verifies that every cached FieldInfo resolved. Should be called during
        // StaticConstructorOnStartup so a RimWorld API rename surfaces as a startup
        // error rather than a silent no-op on every later SetName/AddTrait. Pure
        // diagnostic — no state is built here; the FieldInfos are resolved at
        // class-load time by the field initializers above.
        public static void VerifyReflection()
        {
            if (CompNameField == null)
                Log.Error("[Persona Weapons Unbound] CompGeneratedNames.name field not found via reflection; "
                    + "weapon renaming will silently no-op. RimWorld API may have changed.");
            if (LastKillTickField == null)
                Log.Error("[Persona Weapons Unbound] CompBladelinkWeapon.lastKillTick field not found via reflection; "
                    + "adding a kill-thirst trait to a long-bonded weapon may fire an instant mood penalty. "
                    + "RimWorld API may have changed.");
        }

        // Adds a trait to the live list and reproduces vanilla's bond-time side
        // effects that a raw list mutation would skip (fork spec §5, D8, D9).
        public static void AddTrait(Thing weapon, WeaponTraitDef trait)
        {
            CompBladelinkWeapon comp = weapon.TryGetComp<CompBladelinkWeapon>();
            if (comp == null)
            {
                Log.Error("[Persona Weapons Unbound] AddTrait: weapon has no CompBladelinkWeapon.");
                return;
            }

            // NeverBond ("freewielder") added to a bonded weapon severs the bond
            // first (spec D8): once the trait is present, Biocodable flips to false,
            // so UnCode must run while the trait list is still intact for its
            // per-trait teardown to fire against the pre-mutation list.
            if (trait.neverBond && comp.Biocoded)
                comp.UnCode();

            Pawn codedPawn = comp.CodedPawn;
            bool bonded = comp.Biocoded && codedPawn != null;

            comp.TraitsListForReading.Add(trait);

            if (bonded)
            {
                // Apply the trait's bonded hediffs to the coded pawn — nothing
                // re-fires this on a live add.
                trait.Worker.Notify_Bonded(codedPawn);

                // NeedKill ("kill thirst"): the kill clock was set at the original
                // bond time. Adding NeedKill to a weapon bonded >20 days ago would
                // land the −4 kill-thirst mood on the next situational recalc with
                // zero grace — reset the clock so the intended 20-day grace applies
                // (spec D9).
                if (trait == WeaponTraitDefOf.NeedKill && LastKillTickField != null)
                    LastKillTickField.SetValue(comp, Find.TickManager.TicksAbs);
            }
        }

        // Removes a trait from the live list, firing vanilla's unbond/equip-lost
        // teardown BEFORE removal so its hediffs and lingering memories aren't
        // orphaned on the pawn (fork spec §5, D10). Vanilla's own teardown paths
        // iterate the current trait list, so a trait removed while its hediff is
        // applied would strand that hediff (NoPain / SpeedBoost / HungerMaker /
        // NeuralHeatRecoveryGain) permanently.
        public static void RemoveTrait(Thing weapon, WeaponTraitDef trait)
        {
            CompBladelinkWeapon comp = weapon.TryGetComp<CompBladelinkWeapon>();
            if (comp == null)
            {
                Log.Error("[Persona Weapons Unbound] RemoveTrait: weapon has no CompBladelinkWeapon.");
                return;
            }

            Pawn codedPawn = comp.CodedPawn;
            if (comp.Biocoded && codedPawn != null)
            {
                // Strip the trait's bonded hediffs, then purge its lingering
                // weapon-referencing memories (Jealous / OnKill_Thought*): those
                // are memories, not situational thoughts, so nothing culls them
                // when the trait goes away (spec D10).
                trait.Worker.Notify_Unbonded(codedPawn);
                PurgeTraitMemories(weapon, trait, codedPawn);
            }

            // If the weapon is somehow currently equipped, fire equip-lost teardown
            // too so its equipped hediffs are removed before the trait leaves the
            // list. During customization the weapon is carried, not equipped, so
            // this is a defensive edge case.
            if (weapon.ParentHolder is Pawn_EquipmentTracker eqTracker && eqTracker.pawn != null)
                trait.Worker.Notify_EquipmentLost(eqTracker.pawn);

            comp.TraitsListForReading.Remove(trait);
        }

        // Purges the coded pawn's weapon-referencing memory thoughts created by a
        // trait being removed. Covers killThought memories (OnKill_ThoughtGood/Bad)
        // and the Jealous trait's JealousRage memory. Only removes memories whose
        // scribed weapon reference is this very weapon, so bonds to other persona
        // weapons are untouched.
        private static void PurgeTraitMemories(Thing weapon, WeaponTraitDef trait, Pawn codedPawn)
        {
            MemoryThoughtHandler memories = codedPawn?.needs?.mood?.thoughts?.memories;
            if (memories == null)
                return;

            ThingWithComps twc = weapon as ThingWithComps;

            if (trait.killThought != null)
                memories.RemoveMemoriesOfDefIf(trait.killThought,
                    m => (m as Thought_WeaponTrait)?.weapon == twc);

            // Jealous emits JealousRage from its worker rather than via a def field,
            // so key off the worker type.
            if (trait.Worker is WeaponTraitWorker_Jealous)
                memories.RemoveMemoriesOfDefIf(ThoughtDefOf.JealousRage,
                    m => (m as Thought_WeaponTrait)?.weapon == twc);
        }

        // Scrubs the random state left behind by CompBladelinkWeapon.PostPostMake
        // (auto-rolled 1-2 traits) and CompGeneratedNames.Initialize (rolled
        // name) on a freshly minted persona weapon, so the customization pipeline
        // starts from a clean slate. Called by WeaponDefConversion right after
        // ThingMaker.MakeThing on a persona-weapon def.
        //
        // No-op on non-persona target defs (no CompBladelinkWeapon).
        internal static void ClearAutoGeneratedPersonaState(Thing weapon)
        {
            CompBladelinkWeapon comp = weapon.TryGetComp<CompBladelinkWeapon>();
            if (comp != null)
                comp.TraitsListForReading.Clear();

            // Clear the auto-generated persona name (Scribed as "name") so the
            // player-chosen name is applied to a blank slate rather than appended.
            CompGeneratedNames names = weapon.TryGetComp<CompGeneratedNames>();
            if (names != null && CompNameField != null)
                CompNameField.SetValue(names, null);
        }

        // Sets the persona weapon's display name by writing the private, Scribed
        // CompGeneratedNames.name field via cached reflection (persists in
        // saves). No-op if the weapon has no CompGeneratedNames.
        public static void SetName(Thing weapon, string name)
        {
            CompGeneratedNames comp = weapon.TryGetComp<CompGeneratedNames>();
            if (comp != null && CompNameField != null)
                CompNameField.SetValue(comp, name);
        }

        // Spawns resources near a position (e.g. the workbench).
        // Used to refund resources when removing traits.
        public static void SpawnResourcesNear(
            Map map, IntVec3 center, List<ThingDefCountClass> resources)
        {
            if (resources == null || resources.Count == 0)
                return;

            foreach (ThingDefCountClass resource in resources)
            {
                if (resource.count <= 0)
                    continue;

                Thing thing = ThingMaker.MakeThing(resource.thingDef);
                thing.stackCount = resource.count;
                GenPlace.TryPlaceThing(thing, center, map, ThingPlaceMode.Near);
            }
        }

    }
}
