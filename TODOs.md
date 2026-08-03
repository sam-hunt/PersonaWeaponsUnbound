# TODOs

## Features

- Mod option for gating customization on colony crafting skill recipe +2 or 12
- Negative thoughts on memory ops?
- Multiplayer support?

## Testing

## Cleanup

- Run the `roslynator` CLI bulk fix for the RCS1146 (conditional access) warnings
  surfaced by the newly added analyzers; register the sweep commit in
  `.git-blame-ignore-revs`.
- Decide whether to standardize `generate_release_notes` in release.yml across
  the family (UMW uses false + manual changelog paste; UWU/PWU use true).
- Evaluate whether `Scripts/test-windows.sh` is still necessary or the suite can
  run natively with `dotnet test Tests/1.6/PersonaWeaponsUnbound.Tests.csproj` — the idiomatic
  pattern BetterTradersGuild uses (its CLAUDE.md warns the Windows-interop script
  corrupts shared `obj/` incremental state; ArchotechAndroidHardware verified
  native runs work and dropped the script, AAH 9bc240f). `DeployToModFolder` is
  already Release-gated here, so Debug `dotnet test` builds won't redeploy.
