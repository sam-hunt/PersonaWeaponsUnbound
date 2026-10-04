namespace PersonaWeaponsUnbound
{
    // Startup work that must run against the CURRENT DefDatabase: the weapon
    // pair registry, the fabrication-bench set (and its display label), the
    // def-level caches of the optional integrations, and the settings that are applied by mutating
    // live defs (techprint count, persona-core recipe cost and skill). Runs
    // once per play-data LOAD, not once per process: an in-process reload (a
    // mid-session language change) replaces every def instance, and a
    // [StaticConstructorOnStartup] type initializer never re-runs, which
    // would leave these caches pointing at the previous database's dead defs
    // (customization would stop finding persona variants and workbenches) and
    // the fresh defs carrying their XML values instead of the player's
    // settings.
    //
    // First load: called directly from ModInitializer's static ctor (which
    // runs inside the first CallAll, too late for its own postfix). Every
    // reload: invoked by Patches/StaticConstructorOnStartupUtility_CallAll_
    // Patch.cs at exactly the moment static ctors run, after defs, DefOf
    // rebinding and full language injection; that file carries the verified
    // load ordering, the DoPlayLoad trap, and the hot-reload caveat.
    //
    // Everything called here must stay idempotent: it fires once per load,
    // arbitrarily many times per process.
    public static class PWU_Startup
    {
        // Shared-report entry: the first load passes ModInitializer's report so
        // the whole init block logs a single summary line.
        public static void Run(InitDiagnostics report)
        {
            report.Time("WeaponRegistry", () => WeaponRegistry.Initialize(report));
            report.Time("WorkbenchUtility", () => WorkbenchUtility.Initialize(report));
            report.Time("VPWE caches", VPWEIntegration.ResetPerLoadCaches);
            report.Time("techprint count", PWU_ResearchDefOf.ApplyTechprintCount);
            report.Time("persona core recipe", PWU_RecipeDefOf.ApplyPersonaCoreRecipeSettings);
        }

        // Reload entry (the CallAll postfix): builds its own report so each
        // reload logs its own summary with the fresh per-mod def counts.
        public static void RunOnReload()
        {
            var report = new InitDiagnostics();
            Run(report);
            report.LogSummary();
        }
    }
}
