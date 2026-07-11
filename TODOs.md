# TODOs

## Features

- Negative thoughts on memory ops
- Multiplayer support

## Testing

- Eyeball research project x/y placement in the tree
- Confirm VE Persona weapons compatibility
- Before release, explicitly test the biocode inversion (§4 Bonding) works smoothly: downgrade severs any bond via `UnCode()`, upgrade never inherits a bond from an already-biocoded base weapon and only bonds on next equip
- Before release, visually confirm mod add/removal save safety: add PWU to an existing save (customization works, no log errors), then remove it (save loads, only expected def warnings)
- Before release, visually confirm the memory ops in-game: wipe bonding severs the bond and re-arms bond-on-next-equip; wipe kill tracker resets the tracker to `TicksAbs` while still bonded, or −1 once unbonded
- Before release, confirm the upgrade-while-equipped bond-confirmation dialog (`Docs/Specs/MEMORY_AND_POLISH.md` §5): confirming equips the weapon and fires the bond letter; rejecting leaves it on the bench

## Cleanup

- Review pass on the copy.
- Retire the Odyssey-era sections of `Docs/Research/CUSTOMIZATION_SYSTEM.md` once the persona dialog stabilizes (kept as historical context for now)
