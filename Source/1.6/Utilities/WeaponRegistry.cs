using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    // Caches base↔persona weapon pair mappings at startup and provides
    // runtime lookups for weapon pair resolution. A persona weapon is any
    // ThingDef carrying Royalty's CompBladelinkWeapon.
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

        // Index of non-persona weapons by graphicData.texPath, for the art-based
        // fallback in FindBaseWeapon. A persona variant almost always reuses its base
        // weapon's texture, so a persona whose texPath matches exactly one base weapon
        // (cardinality one) is very likely that weapon's persona form — even when its
        // mod-prefixed or infixed defName defeats the naming convention (e.g. Medieval
        // Persona Weapons' 'MPW_Bladelink_Mace'). Case-sensitive: texPaths are literal
        // asset paths. The match is corroborated by a shared weaponTag (see
        // FindBaseByReusedArt) so a coincidental texture reuse across unrelated weapon
        // families can't mis-pair.
        private static Dictionary<string, List<ThingDef>> basesByTexPath;

        // Curated pairings for cross-mod persona variants, used for two things the
        // automatic strategies in FindBaseWeapon can't do on their own:
        //   1. Priority — when several persona variants legitimately resolve to one
        //      base (e.g. the eltex staff, offered by both VPWE and MPT), only the
        //      player's preferred variant should win the base→persona slot. The
        //      automatic winner is otherwise load-order-dependent.
        //   2. Backstop — a hard-coded link for a pair that no automatic strategy
        //      catches (no hyperlink, non-conventional defName, bespoke art).
        // Each entry maps a base weapon defName to candidate persona defNames in
        // priority order; the first present candidate wins the base's persona slot and
        // every present candidate is reverse-mapped so it stays revertible. Applied
        // after the automatic scan, and authoritative — it overrides the automatic
        // base→persona winner. See ResolveCuratedPairings.
        private static readonly (string BaseDefName, string[] PersonaDefNames)[] CuratedPairings =
        {
            // Eltex staff (Royalty base) — prefer Vanilla Persona Weapons Expanded's
            // persona eltex staff, else fall back to More Persona Traits' amplifying rod.
            ("MeleeWeapon_PsyfocusStaff", new[]
            {
                "VPWE_MeleeWeapon_PsyfocusStaffBladelink",
                "MPT_MeleeWeapon_PsyfocusStaffBladelink",
            }),
        };

        // Builds the base↔persona weapon pair cache. Must be called during
        // StaticConstructorOnStartup (after all defs are loaded). A non-null
        // report absorbs any fatal exception so the rest
        // of the mod can still initialize; passing null preserves the
        // throwing contract for direct callers.
        public static void Initialize(InitDiagnostics report = null)
        {
            try
            {
                baseToPersona = new Dictionary<ThingDef, ThingDef>();
                personaToBase = new Dictionary<ThingDef, ThingDef>();
                orphanPersonaDefs = new List<ThingDef>();

                // Index candidate base weapons (all weapons that are NOT persona
                // weapons) by defName (case-insensitive) for the naming lookup, and by
                // texPath for the art-based fallback. Persona weapons are excluded from
                // both, so a persona sharing a base's texPath sees only the base.
                baseWeaponsByName = new Dictionary<string, ThingDef>(StringComparer.OrdinalIgnoreCase);
                basesByTexPath = new Dictionary<string, List<ThingDef>>(StringComparer.Ordinal);
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (!def.IsWeapon || IsPersonaWeapon(def))
                        continue;

                    if (!baseWeaponsByName.ContainsKey(def.defName))
                        baseWeaponsByName[def.defName] = def;

                    string texPath = def.graphicData?.texPath;
                    if (!string.IsNullOrEmpty(texPath))
                    {
                        if (!basesByTexPath.TryGetValue(texPath, out List<ThingDef> withTex))
                            basesByTexPath[texPath] = withTex = new List<ThingDef>();
                        withTex.Add(def);
                    }
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

                // Apply curated priority/backstop pairings after the automatic scan:
                // pin the preferred persona for known multi-variant bases (e.g. the
                // eltex staff, offered by both VPWE and MPT) and rescue any hard-coded
                // pair the automatic strategies missed, pruning it from the orphan list
                // before the warnings below.
                ResolveCuratedPairings();

                // One warning per orphan, after the scan completes, so the log
                // groups them together rather than interleaving with per-def
                // error spam from the catch above. Orphans stay customizable
                // via the IsPersonaWeapon HasComp check; what they lose is the
                // ability to revert to a base def when the trait list empties.
                foreach (ThingDef orphan in orphanPersonaDefs)
                {
                    Log.Warning("[Persona Weapons Unbound] No base weapon detected for "
                        + orphan.SourceForLog()
                        + "; customizable but cannot revert to base. Add a "
                        + "descriptionHyperlinks entry, use the 'Bladelink' suffix, or "
                        + "reuse the base weapon's texPath and a shared weaponTag.");
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

        // Whether the persona def's name is exactly '{BaseDefName}Bladelink' for the
        // given base (compared case-insensitively so the ZeusHammer pair still counts)
        // — the strongest pairing signal, used to break ties when several persona defs
        // resolve to the same base weapon.
        private static bool IsExactNamingMatch(ThingDef personaDef, ThingDef baseDef)
        {
            if (!personaDef.defName.EndsWith(PersonaSuffix, StringComparison.OrdinalIgnoreCase))
                return false;
            string stripped = personaDef.defName.Substring(
                0, personaDef.defName.Length - PersonaSuffix.Length);
            return string.Equals(stripped, baseDef.defName, StringComparison.OrdinalIgnoreCase);
        }

        // Applies the CuratedPairings table after the automatic scan.
        // For each entry whose base weapon is present, walks its candidate persona
        // defNames in priority order: every present candidate that carries the
        // bladelink comp is reverse-mapped to the base (rescuing any the automatic
        // scan left orphaned), and the first present candidate is pinned as the base's
        // persona form — overriding the automatic base→persona winner, since curated
        // priority is authoritative for known multi-variant conflicts.
        private static void ResolveCuratedPairings()
        {
            foreach ((string baseDefName, string[] personaDefNames) in CuratedPairings)
            {
                // Base must exist as a real (non-persona) weapon; baseWeaponsByName
                // already excludes persona defs.
                if (!baseWeaponsByName.TryGetValue(baseDefName, out ThingDef baseDef))
                    continue;

                ThingDef preferred = null;
                foreach (string personaName in personaDefNames)
                {
                    ThingDef personaDef = DefDatabase<ThingDef>.GetNamedSilentFail(personaName);
                    if (personaDef == null || !IsPersonaWeapon(personaDef))
                        continue;

                    // Reverse mapping: the persona knows its base. A candidate the
                    // automatic scan missed (unconventional name, bespoke art) is
                    // rescued here and dropped from the orphan list.
                    if (!personaToBase.ContainsKey(personaDef))
                    {
                        personaToBase[personaDef] = baseDef;
                        orphanPersonaDefs.Remove(personaDef);
                    }

                    // First present candidate = highest priority.
                    if (preferred == null)
                        preferred = personaDef;
                }

                // Forward mapping: pin the base's persona form to the preferred
                // candidate, overriding whatever the automatic scan picked.
                if (preferred != null)
                    baseToPersona[baseDef] = preferred;
            }
        }

        // Detects the base weapon for a persona weapon def, trying each signal from
        // most to least authoritative:
        //   1. descriptionHyperlinks — an explicit author-declared link (opt-in).
        //   2. Naming convention — defName is '{BaseDefName}Bladelink'.
        //   3. Reused art — the persona's texPath matches exactly one base weapon and
        //      the two share a weaponTag (see FindBaseByReusedArt).
        // Returns null if none resolve (the def becomes a customizable orphan).
        private static ThingDef FindBaseWeapon(ThingDef personaDef)
        {
            // 1. descriptionHyperlinks — works for modded weapons that may not follow naming conventions.
            if (personaDef.descriptionHyperlinks != null)
            {
                foreach (DefHyperlink link in personaDef.descriptionHyperlinks)
                {
                    if (link.def is ThingDef linked && linked.IsWeapon && !IsPersonaWeapon(linked))
                        return linked;
                }
            }

            // 2. Naming convention ({BaseDefName}Bladelink), resolved case-insensitively
            // so 'MeleeWeapon_ZeusHammerBladelink' pairs with 'MeleeWeapon_Zeushammer'.
            if (personaDef.defName.EndsWith(PersonaSuffix, StringComparison.OrdinalIgnoreCase))
            {
                string baseName = personaDef.defName.Substring(
                    0, personaDef.defName.Length - PersonaSuffix.Length);
                if (baseWeaponsByName != null
                    && baseWeaponsByName.TryGetValue(baseName, out ThingDef baseDef))
                    return baseDef;
            }

            // 3. Reused art (corroborated by a shared weaponTag).
            return FindBaseByReusedArt(personaDef);
        }

        // Fallback pairing for persona defs whose name defeats the naming convention
        // (mod prefixes/infixes) but which reuse their base weapon's texture. Matches
        // only when the persona's texPath belongs to exactly one non-persona weapon
        // (cardinality one — an ambiguous texture is rejected) AND that weapon shares
        // at least one weaponTag with the persona. Requiring both signals keeps a
        // coincidental texture reuse from mis-pairing unrelated weapons, and leaves
        // deliberately base-less persona weapons (bespoke art, e.g. warcasket weapons)
        // as orphans.
        private static ThingDef FindBaseByReusedArt(ThingDef personaDef)
        {
            string texPath = personaDef.graphicData?.texPath;
            if (string.IsNullOrEmpty(texPath) || basesByTexPath == null)
                return null;

            if (!basesByTexPath.TryGetValue(texPath, out List<ThingDef> bases) || bases.Count != 1)
                return null;

            ThingDef candidate = bases[0];
            return SharesWeaponTag(personaDef, candidate) ? candidate : null;
        }

        // Whether two weapon defs list at least one weaponTag in common. Persona-only
        // tags inherited from the bladelink base (e.g. 'Bladelink') never appear on a
        // non-persona base, so only genuine weapon-family tags can produce a match.
        private static bool SharesWeaponTag(ThingDef a, ThingDef b)
        {
            if (a.weaponTags == null || b.weaponTags == null)
                return false;
            foreach (string tag in a.weaponTags)
            {
                if (b.weaponTags.Contains(tag))
                    return true;
            }
            return false;
        }

        // Logs one warning per base weapon that several persona defs resolved to
        // (a mod conflict the player owns). Only one variant wins the base→persona
        // mapping (see RegisterPersonaWeaponDef), so the rest can't be
        // produced by customizing the base weapon. Each variant is reported with its
        // source mod so players and modders know which defs to reconcile. Grouped
        // after the scan, like the orphan warnings.
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

        // Returns the persona variant for a base weapon def, or null if none exists.
        public static ThingDef GetPersonaVariant(ThingDef baseDef)
        {
            return baseToPersona.TryGetValue(baseDef, out ThingDef persona) ? persona : null;
        }

        // All registered persona-variant ThingDefs (one per base↔persona pair).
        // Used by the startup diagnostic to bucket pairs by source mod.
        public static IEnumerable<ThingDef> AllPersonaDefs => personaToBase.Keys;

        // Persona-comp ThingDefs that loaded with no detectable base weapon
        // (no descriptionHyperlinks pointing at a non-persona weapon and no
        // matching '{BaseDefName}Bladelink' naming). Still customizable, but
        // cannot revert to base. Used by the startup diagnostic.
        public static IEnumerable<ThingDef> OrphanPersonaDefs =>
            orphanPersonaDefs ?? (IEnumerable<ThingDef>)System.Array.Empty<ThingDef>();

        // Returns the base weapon for a persona weapon def, or null if not found.
        public static ThingDef GetBaseVariant(ThingDef personaDef)
        {
            return personaToBase.TryGetValue(personaDef, out ThingDef baseDef) ? baseDef : null;
        }

        // Whether the def is a persona weapon (has CompBladelinkWeapon).
        public static bool IsPersonaWeapon(ThingDef def)
        {
            return def.HasComp(typeof(CompBladelinkWeapon));
        }

        // Resolves the base and persona ThingDefs for a weapon, regardless of
        // whether the weapon is currently in its base or persona form.
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
