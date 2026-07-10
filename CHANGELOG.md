# Changelog

All notable changes to Persona Weapons Unbound will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-07-10

### Added

- Initial release. Fork of [Unique Weapons Unbound](https://github.com/sam-hunt/UniqueWeaponsUnbound) retargeted at Royalty's bladelink/persona weapons (monosword, plasmasword, zeushammer)
- Customize persona weapons at the fabrication bench: add and remove bladelink traits, with full vanilla validation (max trait count, exclusion tags, sole-trait rules)
- Convert base weapons to their persona variant by adding a first trait (installs an AI persona core), and revert to base by removing the last trait (refunds the core)
- Reprogramming cost model: every trait change past the first/last costs advanced components, scaling with weapon quality above a configurable threshold; no refunds on removal
- Rename the persona using vanilla's bladelink namer or a custom name; Ideology relic names stay locked
- Recolor via a patched `CompColorable`: persona tint plus Ideology and structure palettes
- Single "Bladelink Customization" research project gates customization, the crafting recipes below, and an Empire-sourced techprint requirement (count configurable, 0 disables it, no restart needed)
- Craftable base monosword, plasmasword, and zeushammer recipes at the fabrication bench, each individually toggleable
- Optional trait discovery progression: only offer traits seen on persona weapons held by the colony, in caravans, or on hostiles (no persistent save state)
- Three customization entry points: weapon gizmo, workbench right-click, and ground weapon right-click
- Sweep and Thorough haul planners for gathering customization ingredients in fewer trips, alongside the vanilla-equivalent Sequential fallback
- Mod settings panel: persona cost sliders with a live cost table, techprint count, recipe toggles, minimum quality gate, color palette toggles, trait limit/sole-trait enforcement, discovery progression, and ground-menu toggle
- Coexists cleanly with Unique Weapons Unbound (disjoint packageId, Harmony ID, assembly, namespaces, defNames, and localization keys; complementary trait filters — UWU handles Odyssey unique weapons, PWU handles Royalty persona weapons)
- VEF recipe-inheritance benches (e.g. VFE's compact fabrication bench) are recognized as customization benches automatically

[1.0.0]: https://github.com/sam-hunt/PersonaWeaponsUnbound/releases/tag/v1.0.0
