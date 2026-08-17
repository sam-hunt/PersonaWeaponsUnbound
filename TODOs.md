# TODOs

## Features

- Mod option for gating customization on colony crafting skill recipe +2 or 12
- Negative thoughts on memory ops?
- Multiplayer support?

## Testing

## Cleanup

- Decide whether to standardize `generate_release_notes` in release.yml across
  the family (UMW uses false + manual changelog paste; UWU/PWU use true).
- Run the initial Steam Workshop description translation pass: create
  `.steamworkshop/Description/<Language>.txt` for each non-English language
  under `1.6/Languages/`, localize titles per `.steamworkshop/README.md`
  (vanilla Royalty persona weapon/bladelink term, no English brand appended),
  and sync each language's `PWU_SettingsCategory` Keyed value to its title
  line. Structure/process landed 2026-08-18; only `English.txt` exists so
  far.
