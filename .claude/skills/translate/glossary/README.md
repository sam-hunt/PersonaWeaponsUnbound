# Glossary — PWU-specific terminology

These per-language files (`Japanese.md`, `ChineseSimplified.md`,
`Korean.md`, `German.md`, `Spanish.md`, `French.md`,
`PortugueseBrazilian.md`, `Russian.md`) hold everything about a language's
translation that is specific to Persona Weapons Unbound: mod-coined terms
(the bladelink-customization research label, haul planner modes, kill
tracker/memory vocabulary), the persona-weapon/bladelink terminology
choices and their per-DLC trait divergence (Royalty's
`Stat_Thing_PersonaWeaponTrait_Label` vs Odyssey's
`Stat_ThingUniqueWeaponTrait_Label` vs the pawn-trait word — the answer
differs by language and none of it is generic), the localized Workshop
title (`PWU_SettingsCategory`), and worked phrasing decisions tied to
specific `PWU_` strings (e.g. the German/Spanish/French/Portuguese
grammar-workaround rewrites forced by case/gender/contraction rules on
`PWU_RequiresWorkbench`, `PWU_RequiresMinimumQuality`, and friends).

Family-shared, mod-independent findings — LanguageWorker mechanics, style
and corpus rules, and vanilla-grounded common vocabulary (trader, quality
tiers, Cancel/Reset buttons, plasteel/uranium, ideoligion/relic, and so on)
— live upstream in the `l10n/` submodule at `l10n/languages/<Language>.md`
(canonical checkout: `~/dev/rimworld-l10n`), since they apply to any mod in
the family, not just this one.

When a future translation pass coins a new PWU-specific term, record it
here. If a pass instead surfaces a correction to shared mechanics or
vocabulary, send that fix upstream to the l10n repo rather than duplicating
it here.
