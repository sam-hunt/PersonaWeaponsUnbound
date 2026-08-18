# TODOs

## Features

- Mod option for gating customization on colony crafting skill recipe +2 or 12
- Negative thoughts on memory ops?
- Multiplayer support?

## Testing

## Cleanup

- Decide whether to standardize `generate_release_notes` in release.yml across
  the family (UMW uses false + manual changelog paste; UWU/PWU use true).
- Backfill glossary/Russian.md's mod-coined terms on the next Russian pass (the old skill never had a ru glossary section despite shipped ru Keyed; noted during the 2026-08-18 l10n consolidation; the 2026-08-18 Workshop pass seeded a partial table but a full grounding pass is still owed)
- Upstream to the l10n repo's `languages/<Language>.md` vocabulary tables: vanilla `AdvancedFabrication.label` values verified against the game tars during the 2026-08-18 Workshop pass (de "Hightech-Fabrikation", fr "fabrication avancée", es "fabricación avanzada", ko "고급 부품", ja "先進組立製造", ru "сверхвысокоточное производство", zh "高级精密装配") — mod-independent corpus facts that belong upstream per the content contract, then bump the pin here.
