using HarmonyLib;
using Verse;

namespace PersonaWeaponsUnbound.Patches
{
    // Re-runs PWU_Startup on every play-data RELOAD, where
    // [StaticConstructorOnStartup] alone would run it only once per process.
    // This file is the full rationale for that divergence; PWU_Startup and
    // CLAUDE.md carry only pointers here.
    //
    // Why the attribute's contract is too weak for us: PWU_Startup's work is
    // all state derived from, or written into, the live DefDatabase: the
    // base/persona weapon pair registry, the fabrication-bench set and its
    // baked display label, the weapon recipe index, and the settings applied
    // by mutating defs (techprint count, persona-core recipe cost and skill).
    // An in-process play-data reload (LanguageDatabase.SelectLanguage runs
    // ClearAllPlayData + LoadAllPlayData; the mid-session language switch is
    // the one player-facing trigger) replaces every def instance, but a type
    // initializer can never run twice (StaticConstructorOnStartupUtility.
    // CallAll goes through RuntimeHelpers.RunClassConstructor, which no-ops on
    // an initialized type). With attribute-only startup the fresh defs are
    // never registered: base/persona conversion stops resolving, no live
    // bench passes the fabrication gate (customization becomes impossible),
    // bench-requirement messages keep the previous language's label, and the
    // research def reverts to its XML techprint count. Vanilla itself never
    // needs a re-run hook: its own cross-load state is either [DefOf] fields
    // (rebound every load) or load-agnostic static texture/material caches.
    // Mods that cache or mutate defs own the re-application problem, and
    // vanilla ships no standing "play data loaded" callback.
    //
    // Why THIS hook (decompile-verified, RimWorld 1.6): PlayDataLoader.
    // DoPlayLoad queues its finishing work as ExecuteWhenFinished delegates
    // that run on the main thread after the method returns, in order:
    // InjectIntoData_AfterImpliedDefs (full DefInjected application) +
    // GenLabel.ClearCache, then StaticConstructorOnStartupUtility.CallAll,
    // then atlas baking. A postfix on CallAll therefore fires at exactly the
    // moment static ctors run, after defs, cross-refs, DefOf rebinding and
    // full language injection, and it stays correct for any future reload
    // trigger because it hooks the load pipeline, not the language switch.
    //
    // The trap this shape avoids: a postfix on PlayDataLoader.DoPlayLoad
    // itself LOOKS equivalent but fires before those queued delegates, i.e.
    // before DefInjected is applied, and would rebuild every cache from
    // untranslated labels, subtly wrong in exactly the scenario this fixes.
    //
    // First-load coverage: this repo applies its patches from ModInitializer's
    // [StaticConstructorOnStartup] ctor (load-bearing; see the patch-timing
    // hazard in CLAUDE.md), which executes INSIDE the first CallAll invocation;
    // detouring a method never affects the activation already on the stack, so
    // this postfix cannot fire for that first call. ModInitializer therefore
    // calls PWU_Startup.Run directly for the first load, and this postfix owns
    // every load after it.
    //
    // Deliberately out of scope: dev-mode PlayDataLoader.HotReloadDefs never
    // calls CallAll (nor RebindAllDefOfs; vanilla does not uphold even its
    // own DefOf contract there), so def hot reload stays best-effort for us
    // exactly as it is for vanilla.
    //
    // Everything PWU_Startup.Run calls must stay idempotent: reloads make this
    // fire once per load, arbitrarily many times per process.
    [HarmonyPatch(typeof(StaticConstructorOnStartupUtility), nameof(StaticConstructorOnStartupUtility.CallAll))]
    public static class StaticConstructorOnStartupUtility_CallAll_Patch
    {
        public static void Postfix()
        {
            PWU_Startup.RunOnReload();
        }
    }
}
