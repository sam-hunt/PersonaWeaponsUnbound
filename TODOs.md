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

## Cleanup

- Add `Docs/HANDOFF.md` to `.gitignore` so the working handoff can never be committed accidentally
- Purge stale `obj/`/`bin/` caches after the project renames (legacy `CustomizeUniqueWeapons.*` and `UniqueWeaponsUnbound.*` artifacts linger there)
- The old `Mods/UniqueWeaponsUnbound` deploy folder in the local RimWorld install belongs to the UWU repo now — confirm this fork stops writing there after the csproj `ModDeployPath` rename, and remove it only if the UWU checkout doesn't still deploy it
- Remove the "Fork status" note from CLAUDE.md once the rename/conversion lands (note says so itself)
- `PWU_Mod.DrawHaulPlannerOption` carries a never-used disabled-radio branch — prune or keep as extension point
- Retire the Odyssey-era sections of `Docs/Research/CUSTOMIZATION_SYSTEM.md` once the persona dialog stabilizes (kept as historical context for now)
