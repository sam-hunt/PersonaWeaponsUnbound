using System.Reflection;
using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    // Transforms a weapon Thing into a different ThingDef while preserving
    // identity-bearing properties: stuff (material), quality, hitpoint
    // percentage, texture override, authored/relic art, and (when applicable)
    // Ideology relic status. Used during customization at the 0↔1 trait
    // boundary to swap between a weapon's base def and its persona counterpart.
    //
    // Biocode/bond state is deliberately NOT carried across the swap: the
    // persona core is added or stripped at the boundary, so the bond belongs to
    // the persona, not the steel (fork spec §4). The job driver severs the bond
    // (UnCode) on a persona→base downgrade before the swap, and a fresh
    // persona weapon spawns unbonded and bonds on next equip via
    // biocodeOnEquip.
    [StaticConstructorOnStartup]
    public static class WeaponDefConversion
    {
        // Ideology DLC: Precept_Relic.generatedRelic (private Thing).
        // Resolved once at startup; null if Ideology is not installed.
        // A null value when Ideology IS installed means the field surface has
        // drifted — the static ctor below logs that as an error so a silently
        // stale relic→thing pointer doesn't corrupt a relic during def
        // conversion (see TransferRelicStatus).
        private static readonly FieldInfo GeneratedRelicField =
            GenTypes.GetTypeInAnyAssembly("RimWorld.Precept_Relic")
                ?.GetField("generatedRelic", BindingFlags.NonPublic | BindingFlags.Instance);

        // CompArt persisted state (RimWorld core). No public setters exist for the
        // author or the backing TaleReference, so the three scribed fields are
        // moved wholesale during conversion to keep an authored/relic weapon's
        // title, author, and art description intact. See TransferArt.
        private static readonly FieldInfo ArtAuthorField = typeof(CompArt)
            .GetField("authorNameInt", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo ArtTitleField = typeof(CompArt)
            .GetField("titleInt", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo ArtTaleRefField = typeof(CompArt)
            .GetField("taleRef", BindingFlags.NonPublic | BindingFlags.Instance);

        static WeaponDefConversion()
        {
            if (ModsConfig.IdeologyActive && GeneratedRelicField == null)
            {
                Log.Error("[Persona Weapons Unbound] Ideology active but "
                    + "Precept_Relic.generatedRelic could not be resolved via reflection; "
                    + "relic-flagged weapons that undergo a base<->persona def conversion "
                    + "during customization will leave the precept pointing at the "
                    + "destroyed pre-conversion weapon. RimWorld API may have changed.");
            }

            if (ArtAuthorField == null || ArtTitleField == null || ArtTaleRefField == null)
            {
                Log.Error("[Persona Weapons Unbound] CompArt private fields "
                    + "(authorNameInt/titleInt/taleRef) could not be resolved via reflection; "
                    + "authored/relic art (title, author, description) will be dropped when a "
                    + "weapon crosses the base<->persona boundary. RimWorld API may have changed.");
            }
        }

        // Creates a new weapon Thing from targetDef, copying stuff, quality,
        // hitpoints, and texture from oldWeapon. If targetDef has
        // CompBladelinkWeapon (base→persona conversion), clears the auto-generated
        // traits/name from PostPostMake()/Initialize(). Returns the new weapon.
        // Biocode/bond state is intentionally not copied (see class remarks).
        //
        // Does NOT destroy oldWeapon, and does NOT transfer art or relic status:
        // both move a reference whose teardown must be sequenced against the old
        // weapon's Destroy(), so the caller invokes TransferArt and
        // TransferRelicStatus before destroying oldWeapon.
        public static Thing ConvertWeaponDef(Thing oldWeapon, ThingDef targetDef)
        {
            // Carry the material across when the target is stuffable. Passing the
            // stuff to MakeThing (rather than letting it default) preserves a
            // modded stuffable weapon's material and avoids vanilla's "madeFromStuff
            // but stuff=null" error log + silent reset to DefaultStuffFor. A
            // stuffable target with a stuffless source (degenerate) still falls
            // back to the default so we never hand MakeThing a null stuff.
            ThingDef stuff = targetDef.MadeFromStuff
                ? (oldWeapon.Stuff ?? GenStuff.DefaultStuffFor(targetDef))
                : null;
            Thing newWeapon = ThingMaker.MakeThing(targetDef, stuff);

            // Copy quality. Pass a null art source so SetQuality does NOT run
            // CompArt.InitializeArt — that would roll a fresh random title/tale on
            // the new weapon and strand its TaleReference. Authored art is moved
            // verbatim afterwards by TransferArt instead.
            if (oldWeapon.TryGetQuality(out QualityCategory quality))
            {
                CompQuality qualityComp = newWeapon.TryGetComp<CompQuality>();
                qualityComp?.SetQuality(quality, null);
            }

            // Copy hitpoints as a percentage of max. Read after stuff is set so the
            // percentage maps onto the stuff-adjusted MaxHitPoints.
            if (oldWeapon.MaxHitPoints > 0 && newWeapon.MaxHitPoints > 0)
            {
                float hpPct = (float)oldWeapon.HitPoints / oldWeapon.MaxHitPoints;
                newWeapon.HitPoints = (int)(newWeapon.MaxHitPoints * hpPct);
                if (newWeapon.HitPoints < 1)
                    newWeapon.HitPoints = 1;
            }

            // Copy texture index (harmless preservation for modded weapons that
            // vary by index; persona weapons render via Graphic_Single).
            newWeapon.overrideGraphicIndex = oldWeapon.overrideGraphicIndex;

            // Scrub the random state a fresh persona weapon rolls in PostPostMake
            // (1–2 auto traits) and CompGeneratedNames.Initialize (a rolled name)
            // so customization starts from a clean slate. No-op on a base target.
            WeaponModificationUtility.ClearAutoGeneratedPersonaState(newWeapon);

            return newWeapon;
        }

        // Transfers authored/relic art (author, title, and the backing
        // TaleReference that produces the art description) from oldWeapon to
        // newWeapon. No-op if either weapon lacks CompArt.
        //
        // Must be called BEFORE destroying oldWeapon: the TaleReference is moved,
        // not cloned, so the old weapon's pointer is nulled here to stop
        // CompArt.PostDestroy from calling TaleReference.ReferenceDestroyed() on
        // the tale the new weapon now owns (which would decrement the tale's
        // reference count and could free a tale still in use). Same
        // before-destroy contract as TransferRelicStatus.
        public static void TransferArt(Thing oldWeapon, Thing newWeapon)
        {
            CompArt oldArt = oldWeapon.TryGetComp<CompArt>();
            CompArt newArt = newWeapon.TryGetComp<CompArt>();
            if (oldArt == null || newArt == null)
                return;

            // Drift already logged at startup; bail rather than half-transfer.
            if (ArtAuthorField == null || ArtTitleField == null || ArtTaleRefField == null)
                return;

            // Defensive: if the new weapon somehow already holds a generated
            // taleRef, release it before overwriting so its reference count isn't
            // stranded. (ConvertWeaponDef passes a null art source to SetQuality
            // precisely so this stays null, but don't depend on that here.)
            if (ArtTaleRefField.GetValue(newArt) is TaleReference staleNew)
                staleNew.ReferenceDestroyed();

            ArtAuthorField.SetValue(newArt, ArtAuthorField.GetValue(oldArt));
            ArtTitleField.SetValue(newArt, ArtTitleField.GetValue(oldArt));
            ArtTaleRefField.SetValue(newArt, ArtTaleRefField.GetValue(oldArt));

            // Hand off ownership of the TaleReference: clear it on the old weapon
            // so its impending Destroy() doesn't tear down the shared tale.
            ArtTaleRefField.SetValue(oldArt, null);
        }

        // Transfers Ideology relic status from the old weapon to the new weapon.
        // Must be called BEFORE destroying the old weapon — clears the old weapon's
        // StyleSourcePrecept so that Thing.Destroy() does not fire Notify_ThingLost,
        // which would trigger RelicDestroyed events, mood debuffs, and permanently
        // orphan the relic precept.
        //
        // Updates both sides of the bidirectional reference:
        //   Thing.StyleSourcePrecept → Precept_Relic (via CompStyleable)
        //   Precept_Relic.generatedRelic → Thing (via reflection)
        //
        // No-op if Ideology is not active or the weapon is not a relic.
        public static void TransferRelicStatus(Thing oldWeapon, Thing newWeapon)
        {
            if (!ModsConfig.IdeologyActive)
                return;

            Precept_ThingStyle precept = oldWeapon.StyleSourcePrecept;
            if (precept == null)
                return;

            // Clear from old weapon BEFORE it gets destroyed to prevent
            // Precept_Relic.Notify_ThingLost from firing RelicDestroyed/RelicLost events.
            oldWeapon.StyleSourcePrecept = null;

            // Pre-seed the new weapon's CompStyleable state from the old weapon
            // before the StyleSourcePrecept setter runs below.
            //
            // styleDef: the SourcePrecept setter only writes styleDef when the
            // new def's randomStyleChance is 0 AND the ideo has a style mapping
            // for the new def. Either condition failing leaves the new weapon
            // styleless after conversion. Copying first preserves the visual
            // continuity; the setter still overwrites our copy when it has a
            // valid ideo-driven answer for the new def.
            //
            // everSeenByPlayer: the setter never touches this, so the copy is
            // strictly additive. Direct field assignment rather than
            // SetEverSeenByPlayer so we don't re-fire Notify_RelicSeenByPlayer
            // (which would spam the "relics collected" letter on every
            // customization of an already-seen relic).
            if (oldWeapon is ThingWithComps oldTwc && newWeapon is ThingWithComps newTwc
                && oldTwc.compStyleable != null && newTwc.compStyleable != null)
            {
                newTwc.compStyleable.styleDef = oldTwc.compStyleable.styleDef;
                newTwc.compStyleable.everSeenByPlayer = oldTwc.compStyleable.everSeenByPlayer;
            }

            // Point the new weapon back at the precept. The setter may overwrite
            // the styleDef we just copied via ideo.style.StyleForThingDef for
            // the new def — that's the right behavior when the lookup succeeds.
            newWeapon.StyleSourcePrecept = precept;

            // Update the Precept_Relic's private generatedRelic field to point
            // at the new weapon instance, keeping the precept→thing reference valid.
            if (GeneratedRelicField != null && precept is Precept_Relic)
            {
                GeneratedRelicField.SetValue(precept, newWeapon);
            }
        }
    }
}
