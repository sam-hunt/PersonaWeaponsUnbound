---
name: translate
description: Generate, update, or audit mod localization (Keyed + DefInjected) for a target language, grounded in vanilla RimWorld terminology. Use when asked to add a language, update translations, or check translation freshness.
argument-hint: "[language, e.g. German | update | check]"
---

# Translate

Produce or refresh localization files for Persona Weapons Unbound. English is
the source of truth; every other language derives from it.

**The family-wide process lives in the `l10n/` submodule — load these first,
and only these** (progressive disclosure; if `l10n/` is empty, run
`git submodule update --init`):

- `l10n/process.md` — non-negotiables, file/format conventions, terminology
  grounding method, and the generation / update / audit workflows. This is
  the workflow authority; follow it step by step.
- `l10n/languages/<Language>.md` — the target language's engine mechanics,
  style rules, and vanilla-grounded common vocabulary. Read ONLY the target
  language's file.
- `glossary/<Language>.md` (beside this file) — this mod's own coined-term
  table for the target language. Read it in the same pass.
- `l10n/lessons.md` — cross-language lessons; read when generating a new
  language, skim otherwise.
- `l10n/workshop.md` — Steam Workshop description/title conventions;
  `.steamworkshop/README.md` names this mod's title-coupling key
  (`PWU_SettingsCategory`).

**Where learnings land:** mod-independent findings (engine mechanics, a
language's grammar rule, corpus style facts) go in the `l10n/` submodule —
edit the canonical checkout at `~/dev/rimworld-l10n`, commit there, then bump
the pin here. Mod-specific findings (coined terms, phrasing decisions) go in
`glossary/<Language>.md`.

## This mod's translation surface

- English Keyed source: `1.6/Languages/English/Keyed/PWU_UI.xml` — a single
  file covering the customization gizmo/float-menu strings, dev-mode
  diagnostics, dialog buttons and labels, and settings prose. Every key is
  `PWU_`-prefixed. There is no second Keyed file.
- DefInjected surface: def-type folders under
  `1.6/Languages/English/DefInjected/` currently cover the vanilla type
  folders `JobDef` (`PWU_CustomizeWeapon.reportString`), `RecipeDef`
  (`label`/`description`/`jobString` for each `PWU_Make_*` recipe), and
  `ResearchProjectDef` (`PWU_BladelinkCustomization.label`/`.description`).
  This mod defines no custom def classes of its own, so every DefInjected
  target resolves to a bare vanilla type folder — a future custom def type
  would need a namespace-qualified folder instead (see `l10n/process.md`).
- No gated compat load root exists today (this mod ships no
  `MayRequire`-gated defs) — if one is ever added, route its translations to
  their own `1.6/Mods/<Mod Name>/Languages/<Language>/...` root, never the
  main `1.6` tree; `l10n/process.md` covers the mechanics and the checker
  enforces the placement both ways.
- Two mod-specific key quirks worth knowing before generating: `PWU_
  CostTableNotApplicable` (a bare `-` glyph) deliberately stays identical to
  English in every language, so the in-game translation report always lists
  it under "matching English" — expected, not a gap. `PWU_SettingsCategory`
  is NOT such an exception: it is that language's localized Steam Workshop
  title and must equal the title line (line 1) of
  `.steamworkshop/Description/<Language>.txt` (see CLAUDE.md's localization
  note and `.steamworkshop/README.md`'s title convention).

## This mod's grounding domain

Domain DLC: **Royalty** (plus Core; check Ideology too for the
relic/ideoligion vocabulary this mod's descriptions touch). Ground against
the Core + Royalty tars. Terms that MUST be grounded before use: persona
weapon, bladelink, trait (weapon — as distinct from pawn-personality trait,
and check the RIGHT DLC's stat: Royalty's `Stat_Thing_PersonaWeaponTrait_
Label`, never Odyssey's `Stat_ThingUniqueWeaponTrait_Label`, which is
UniqueWeaponsUnbound's domain), monosword, plasmasword, zeushammer, persona
core, techprint, fabrication bench, bladelink customization (this mod's
research), quality tiers, tech levels. All of these have official
vanilla/Royalty translations in the language tars; this mod's own coinages
(where vanilla has none, e.g. "bladelink customization" in Japanese/Chinese)
live in `glossary/<Language>.md`, which also records the per-language
trait-divergence answer — it is not the same answer in every language, and
none of it is generic enough to live upstream.

## Workflows

Follow `l10n/process.md`'s Initial generation / Update pass / Audit-only
workflows verbatim. This mod's specifics on top:

- The checker: `python3 Scripts/check-translations.py` (`--strict` for new
  languages). Sidecar regen: `python3
  Scripts/refresh-translation-expectations.py` (game must be closed; drives
  the deployed L10nProbe, whose source now lives at `l10n/probe/` — see
  CLAUDE.md).
- No compat-root routing needed today (see the surface section above).
- The public roster (and credits) is CONTRIBUTING.md's localization table —
  update it in the same commit as any language addition or native review.
