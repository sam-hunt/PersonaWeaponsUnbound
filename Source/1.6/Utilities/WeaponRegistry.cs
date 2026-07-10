using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    /// <summary>
    /// Caches base↔persona weapon pair mappings at startup and provides
    /// runtime lookups for weapon pair resolution. A persona weapon is any
    /// ThingDef carrying Royalty's <see cref="CompBladelinkWeapon"/>.
    /// </summary>
    public static class WeaponRegistry
    {
        // Persona defNames follow the vanilla '{BaseDefName}Bladelink' convention
        // (e.g. MeleeWeapon_MonoSword → MeleeWeapon_MonoSwordBladelink). The base
        // lookup is case-insensitive because vanilla itself is inconsistent:
        // MeleeWeapon_Zeushammer pairs with MeleeWeapon_ZeusHammerBladelink.
        private const string PersonaSuffix = "Bladelink";

        private static Dictionary<ThingDef, ThingDef> baseToPersona;
        private static Dictionary<ThingDef, ThingDef> personaToBase;
        private static List<ThingDef> orphanPersonaDefs;

        // Case-insensitive index of every non-persona weapon def by defName, so a
        // suffix-stripped persona defName can resolve its base even when the two
        // defNames disagree on casing (the ZeusHammer trap). Built once during
        // Initialize; a plain DefDatabase.GetNamed lookup would be case-sensitive
        // and miss that pair.
        private static Dictionary<string, ThingDef> baseWeaponsByName;

        /// <summary>
        /// Builds the base↔persona weapon pair cache. Must be called during
        /// StaticConstructorOnStartup (after all defs are loaded). A non-null
        /// <paramref name="report"/> absorbs any fatal exception so the rest
        /// of the mod can still initialize; passing null preserves the
        /// throwing contract for direct callers.
        /// </summary>
        public static void Initialize(InitDiagnostics report = null)
        {
            try
            {
                baseToPersona = new Dictionary<ThingDef, ThingDef>();
                personaToBase = new Dictionary<ThingDef, ThingDef>();
                orphanPersonaDefs = new List<ThingDef>();

                // Index candidate base weapons (all weapons that are NOT persona
                // weapons) by defName, case-insensitively, for the pairing lookup.
                baseWeaponsByName = new Dictionary<string, ThingDef>(StringComparer.OrdinalIgnoreCase);
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def.IsWeapon && !IsPersonaWeapon(def)
                        && !baseWeaponsByName.ContainsKey(def.defName))
                        baseWeaponsByName[def.defName] = def;
                }

                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    try
                    {
                        RegisterPersonaWeaponDef(def);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("[Persona Weapons Unbound] Skipped weapon registration for "
                            + def.SourceForLog() + " due to error: " + ex);
                    }
                }

                // One warning per orphan, after the scan completes, so the log
                // groups them together rather than interleaving with per-def
                // error spam from the catch above. Orphans stay customizable
                // via the IsPersonaWeapon HasComp check; what they lose is the
                // ability to revert to a base def when the trait list empties.
                foreach (ThingDef orphan in orphanPersonaDefs)
                {
                    Log.Warning("[Persona Weapons Unbound] No base weapon detected for "
                        + orphan.SourceForLog()
                        + "; customizable but cannot revert to base. "
                        + "Add a descriptionHyperlinks entry or use the 'Bladelink' suffix.");
                }

                WarnOnPairConflicts();
            }
            catch (Exception ex)
            {
                if (report == null) throw;
                report.RecordFailure(nameof(WeaponRegistry), ex);
            }
        }

        private static void RegisterPersonaWeaponDef(ThingDef def)
        {
            if (!IsPersonaWeapon(def))
                return;

            ThingDef baseDef = FindBaseWeapon(def);
            if (baseDef == null)
            {
                orphanPersonaDefs.Add(def);
                return;
            }

            // Each persona def always knows its own base, so this side never collides.
            personaToBase[def] = baseDef;

            // The reverse mapping can collide: with mods installed, several persona
            // defs may resolve to the same base (e.g. multiple variants of a vanilla
            // weapon, or descriptionHyperlinks that all point back to it). Prefer the
            // one whose defName is exactly '{BaseDefName}Bladelink'; failing an exact
            // match, keep the first registered and let load order decide the winner.
            if (!baseToPersona.ContainsKey(baseDef) || IsExactNamingMatch(def, baseDef))
                baseToPersona[baseDef] = def;
        }

        /// <summary>
        /// Whether the persona def's name is exactly '{BaseDefName}Bladelink' for the
        /// given base (compared case-insensitively so the ZeusHammer pair still counts)
        /// — the strongest pairing signal, used to break ties when several persona defs
        /// resolve to the same base weapon.
        /// </summary>
        private static bool IsExactNamingMatch(ThingDef personaDef, ThingDef baseDef)
        {
            if (!personaDef.defName.EndsWith(PersonaSuffix, StringComparison.OrdinalIgnoreCase))
                return false;
            string stripped = personaDef.defName.Substring(
                0, personaDef.defName.Length - PersonaSuffix.Length);
            return string.Equals(stripped, baseDef.defName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Detects the base weapon for a persona weapon def.
        /// Primary: descriptionHyperlinks. Fallback: naming convention.
        /// </summary>
        private static ThingDef FindBaseWeapon(ThingDef personaDef)
        {
            // Primary: descriptionHyperlinks — works for modded weapons that may not follow naming conventions
            if (personaDef.descriptionHyperlinks != null)
            {
                foreach (DefHyperlink link in personaDef.descriptionHyperlinks)
                {
                    if (link.def is ThingDef linked && linked.IsWeapon && !IsPersonaWeapon(linked))
                        return linked;
                }
            }

            // Fallback: naming convention ({BaseDefName}Bladelink), resolved
            // case-insensitively so 'MeleeWeapon_ZeusHammerBladelink' pairs with
            // 'MeleeWeapon_Zeushammer'.
            if (personaDef.defName.EndsWith(PersonaSuffix, StringComparison.OrdinalIgnoreCase))
            {
                string baseName = personaDef.defName.Substring(
                    0, personaDef.defName.Length - PersonaSuffix.Length);
                if (baseWeaponsByName != null
                    && baseWeaponsByName.TryGetValue(baseName, out ThingDef baseDef))
                    return baseDef;
            }

            return null;
        }

        /// <summary>
        /// Logs one warning per base weapon that several persona defs resolved to
        /// (a mod conflict the player owns). Only one variant wins the base→persona
        /// mapping (see <see cref="RegisterPersonaWeaponDef"/>), so the rest can't be
        /// produced by customizing the base weapon. Each variant is reported with its
        /// source mod so players and modders know which defs to reconcile. Grouped
        /// after the scan, like the orphan warnings.
        /// </summary>
        private static void WarnOnPairConflicts()
        {
            Dictionary<ThingDef, List<ThingDef>> variantsByBase = new Dictionary<ThingDef, List<ThingDef>>();
            foreach (KeyValuePair<ThingDef, ThingDef> pair in personaToBase)
            {
                if (!variantsByBase.TryGetValue(pair.Value, out List<ThingDef> variants))
                    variantsByBase[pair.Value] = variants = new List<ThingDef>();
                variants.Add(pair.Key);
            }

            foreach (KeyValuePair<ThingDef, List<ThingDef>> entry in variantsByBase)
            {
                if (entry.Value.Count < 2)
                    continue;

                ThingDef winner = baseToPersona[entry.Key];
                string variantList = string.Join(", ", entry.Value
                    .OrderBy(v => v.defName)
                    .Select(v => v.SourceForLog()));

                Log.Warning("[Persona Weapons Unbound] Base weapon " + entry.Key.SourceForLog()
                    + " maps to multiple persona variants (" + variantList + "); using "
                    + winner.defName + " as the base's persona form. This is a mod conflict; "
                    + "reconcile the mods or load order if that is not the intended variant.");
            }
        }

        /// <summary>
        /// Returns the persona variant for a base weapon def, or null if none exists.
        /// </summary>
        public static ThingDef GetPersonaVariant(ThingDef baseDef)
        {
            return baseToPersona.TryGetValue(baseDef, out ThingDef persona) ? persona : null;
        }

        /// <summary>
        /// All registered persona-variant ThingDefs (one per base↔persona pair).
        /// Used by the startup diagnostic to bucket pairs by source mod.
        /// </summary>
        public static IEnumerable<ThingDef> AllPersonaDefs => personaToBase.Keys;

        /// <summary>
        /// Persona-comp ThingDefs that loaded with no detectable base weapon
        /// (no descriptionHyperlinks pointing at a non-persona weapon and no
        /// matching '{BaseDefName}Bladelink' naming). Still customizable, but
        /// cannot revert to base. Used by the startup diagnostic.
        /// </summary>
        public static IEnumerable<ThingDef> OrphanPersonaDefs =>
            orphanPersonaDefs ?? (IEnumerable<ThingDef>)System.Array.Empty<ThingDef>();

        /// <summary>
        /// Returns the base weapon for a persona weapon def, or null if not found.
        /// </summary>
        public static ThingDef GetBaseVariant(ThingDef personaDef)
        {
            return personaToBase.TryGetValue(personaDef, out ThingDef baseDef) ? baseDef : null;
        }

        /// <summary>
        /// Whether the def is a persona weapon (has CompBladelinkWeapon).
        /// </summary>
        public static bool IsPersonaWeapon(ThingDef def)
        {
            return def.HasComp(typeof(CompBladelinkWeapon));
        }

        /// <summary>
        /// Resolves the base and persona ThingDefs for a weapon, regardless of
        /// whether the weapon is currently in its base or persona form.
        /// </summary>
        public static void ResolveWeaponDefs(Thing weapon, out ThingDef baseDef, out ThingDef personaDef)
        {
            if (IsPersonaWeapon(weapon.def))
            {
                personaDef = weapon.def;
                baseDef = GetBaseVariant(weapon.def);
            }
            else
            {
                baseDef = weapon.def;
                personaDef = GetPersonaVariant(weapon.def);
            }
        }
    }
}
