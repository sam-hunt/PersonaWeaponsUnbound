# TODOs

## Features

- Negative thoughts on memory ops
- Multiplayer support

## Testing

- Eyeball research project x/y placement in the tree
- Confirm VE Persona weapons compatibility
- Before release, explicitly test the biocode inversion (§4 Bonding) works smoothly: downgrade severs any bond via `UnCode()`, upgrade never inherits a bond from an already-biocoded base weapon and only bonds on next equip
- Before release, visually confirm mod add/removal save safety: add PWU to an existing save (recolor works, no log errors), then remove it (save loads, recolored weapon reverts to vanilla tint, only expected def warnings)

## Cleanup

- Review pass on the copy.
- Retire the Odyssey-era sections of `Docs/Research/CUSTOMIZATION_SYSTEM.md` once the persona dialog stabilizes (kept as historical context for now)
