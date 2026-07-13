using System;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace PersonaWeaponsUnbound
{
    public partial class Dialog_WeaponCustomization
    {
        // --- Weapon name regeneration ---

        private const int NameRegenMaxAttempts = 3;

        // Generates a random persona weapon name via the persona def's own namer
        // (vanilla: NamerWeaponBladelink), reusing the exact static method
        // CompGeneratedNames.GenerateName the game uses for initial
        // generation — so modded persona weapons with a custom nameMaker are
        // respected. Returns null if generation fails after
        // NameRegenMaxAttempts attempts; callers should leave the name field
        // unchanged in that case.
        private string GenerateWeaponName()
        {
            Exception lastException = null;
            for (int attempt = 1; attempt <= NameRegenMaxAttempts; attempt++)
            {
                string name = null;
                try
                {
                    name = GenerateWeaponNameOnce();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                if (!string.IsNullOrWhiteSpace(name) && name != "ErrorName")
                    return name;

                Log.Warning(BuildNameRegenFailureMessage(attempt, lastException));
            }

            Log.Warning("[Persona Weapons Unbound] Skipping weapon name auto-regeneration "
                + "after " + NameRegenMaxAttempts
                + " failed attempts; the existing name will be preserved.");
            return null;
        }

        private string GenerateWeaponNameOnce()
        {
            // Use the persona def's own generated-name props (nameMaker). Bladelink
            // namers are self-contained (noun+verber / syllables / person-name) — no
            // trait adjectives, weapon-type, or color grammar inputs.
            CompProperties_GeneratedName props = GetGeneratedNameProps();
            if (props?.nameMaker == null)
                return null;

            // Exactly the game's own generation path (r_weapon_name + CapitalizeAsTitle).
            return CompGeneratedNames.GenerateName(props);
        }

        // The persona def's CompProperties_GeneratedName, or null if it has
        // none (an un-namered modded persona weapon).
        private CompProperties_GeneratedName GetGeneratedNameProps()
        {
            return personaDef?.comps?
                .OfType<CompProperties_GeneratedName>()
                .FirstOrDefault();
        }

        // Builds a diagnostic message pointing the user toward the most likely
        // source of the failure: a malformed translation of the persona weapon's
        // bladelink rule pack. The original raw rule string is discarded by
        // Rule_String when its regex parse fails, so we report the count of rules
        // whose keyword ended up null/empty alongside the active language and the
        // rule pack's owning mod.
        private string BuildNameRegenFailureMessage(int attempt, Exception ex)
        {
            string langName = LanguageDatabase.activeLanguage?.FriendlyNameNative
                ?? LanguageDatabase.activeLanguage?.LegacyFolderName
                ?? "(unknown)";

            RulePackDef pack = GetGeneratedNameProps()?.nameMaker;
            string packName = pack?.defName ?? "NamerWeaponBladelink";
            string ownerMod = pack?.modContentPack?.Name ?? "(unknown)";

            int badRuleCount = 0;
            if (pack != null)
            {
                try
                {
                    foreach (Rule rule in pack.RulesPlusIncludes)
                    {
                        if (rule == null || string.IsNullOrEmpty(rule.keyword))
                            badRuleCount++;
                    }
                }
                catch
                {
                    // Diagnostics are best-effort; never let them mask the original failure.
                }
            }

            string msg = "[Persona Weapons Unbound] Weapon name regeneration attempt "
                + attempt + "/" + NameRegenMaxAttempts + " failed."
                + " Active language: " + langName + "."
                + " RulePackDef '" + packName + "' (owned by '" + ownerMod + "').";

            if (badRuleCount > 0)
            {
                msg += " Detected " + badRuleCount + " malformed grammar rule(s) "
                    + "(parser left keyword null, original raw string is unrecoverable). "
                    + "Most likely cause: a translation/language mod is shipping malformed "
                    + "entries for " + packName + ".rulePack.rulesStrings — entries must use "
                    + "the form 'keyword(args)?->output' (e.g. 'weapon_noun(p=2)->[weapon_type]'), "
                    + "not a bare keyword like 'weapon_noun'. "
                    + "Check translation overrides for language '" + langName + "' "
                    + "in any installed language packs or translation mods.";
            }

            if (ex != null)
                msg += " Exception: " + ex;

            return msg;
        }
    }
}
