# TODOs

## Features

Further bladelink-comp surfaces we could offer customization of (review 2026-07-10, grounded in `Docs/Research/BLADELINK_WEAPONS.md`):

- Unbond / rebond as a paid dialog operation — `UnCode()` / `CodeFor(pawn)` are public API
- "Re-roll persona" randomize button — clear traits + reroll 1–2 by commonality + regenerate name in one op
- Surface the kill tracker (`TicksSinceLastKill`) in the dialog for kill-thirst personas; optional paid reset op
- Per-trait market value delta preview (`WeaponTraitDef.marketValueOffset`) in the trait rows
- Preview bonded/kill mood thoughts a trait will give the bonded pawn (bondedThought/killThought tooltips)
- Freewielder conversion as a highlighted special op (adding/removing `NeverBond` severs/enables bonding)
- Ability-granting persona traits — requires injecting `CompEquippableAbilityReloadable` + Odyssey-style `abilityProps` wiring; vanilla bladelink has no ability support, so this is a from-scratch feature
- Multiplayer support

## Testing

- Eyeball research project x/y placement in the tree
- Confirm VE Persona weapons compatibility
- Before release, explicitly test the biocode inversion (§4 Bonding) works smoothly: downgrade severs any bond via `UnCode()`, upgrade never inherits a bond from an already-biocoded base weapon and only bonds on next equip
- Before release, visually confirm mod add/removal save safety: add PWU to an existing save (recolor works, no log errors), then remove it (save loads, recolored weapon reverts to vanilla tint, only expected def warnings)

## Cleanup

- Retire the Odyssey-era sections of `Docs/Research/CUSTOMIZATION_SYSTEM.md` once the persona dialog stabilizes (kept as historical context for now)
