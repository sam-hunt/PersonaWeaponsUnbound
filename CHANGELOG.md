# Changelog

All notable changes to Persona Weapons Unbound will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-07-31

### Added

- Localization in eight new languages: Russian, Simplified Chinese, Japanese, Korean,
  German, Spanish, French, and Brazilian Portuguese. Machine-assisted with terminology
  grounded against the vanilla translations; corrections from native speakers are welcome
- Optional AI persona core recipe at the fabrication bench, gated on vanilla's machine
  persuasion research. Off by default, with configurable component cost and Crafting
  skill requirement
- Info card buttons in the customization dialog, one for the weapon as it stands and one
  for the weapon as staged, for comparing full before/after stats

## [1.0.0] - 2026-07-18

### Added

- Initial release. Fork of [Unique Weapons Unbound](https://github.com/sam-hunt/UniqueWeaponsUnbound) retargeted at Royalty's bladelink/persona weapons (monosword, plasmasword, zeushammer)
- Customize persona weapons at the fabrication bench: add and remove bladelink traits, with full vanilla validation (max trait count, exclusion tags, sole-trait rules)
- Convert base weapons to their persona variant by adding a first trait (installs an AI persona core), and revert to base by removing the last trait (refunds the core)
- Reprogramming cost model: every trait change past the first/last costs advanced components, scaling with weapon quality above a configurable threshold; no refunds on removal
- Configurable whether adding the first trait (and removing the last) costs a persona core; when disabled, those changes cost advanced components like every other trait change
- Rename the persona using vanilla's bladelink namer or a custom name; Ideology relic names stay locked
- Single "Bladelink Customization" research project gates customization, the crafting recipes below, and an Empire-sourced techprint requirement (count configurable, 0 disables it, no restart needed)
- Craftable base monosword, plasmasword, and zeushammer recipes at the fabrication bench, each individually toggleable
- Optional trait discovery progression: only offer traits seen on persona weapons held by the colony, in caravans, or on hostiles (no persistent save state)
- Three customization entry points: weapon gizmo, workbench right-click, and ground weapon right-click
- Sweep and Thorough haul planners for gathering customization ingredients in fewer trips, alongside the vanilla-equivalent Sequential fallback
- Mod settings panel: persona cost sliders with a live cost table, techprint count, recipe toggles, minimum quality gate, trait limit/sole-trait enforcement, discovery progression, and ground-menu toggle
- Coexists cleanly with Unique Weapons Unbound (disjoint packageId, Harmony ID, assembly, namespaces, defNames, and localization keys; complementary trait filters — UWU handles Odyssey unique weapons, PWU handles Royalty persona weapons)
- VEF recipe-inheritance benches (e.g. VFE's compact fabrication bench) are recognized as customization benches automatically
- Deep Vanilla Persona Weapons Expanded / Vanilla Expanded Framework integration: preserves composed weapon skins across persona conversion and dialog previews, adds a Texture tab to PWU's customization dialog for editing appearance parts directly (suppressing VEF's redundant float-menu entry), and links cross-mod persona weapons to their base by reused art when defName conventions don't match

[1.1.0]: https://github.com/sam-hunt/PersonaWeaponsUnbound/releases/tag/v1.1.0
[1.0.0]: https://github.com/sam-hunt/PersonaWeaponsUnbound/releases/tag/v1.0.0
