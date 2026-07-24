---
name: translate
description: Generate, update, or audit mod localization (Keyed + DefInjected) for a target language, grounded in vanilla RimWorld terminology. Use when asked to add a language, update translations, or check translation freshness.
argument-hint: "[language, e.g. German | update | check]"
---

# Translate

Produce or refresh localization files for Persona Weapons Unbound. English is
the source of truth; every other language derives from it.

## Non-negotiables

- **Run the checker first and last.** `python3 Scripts/check-translations.py`
  validates key sets, placeholders, DefInjected paths, staleness, and file
  hygiene deterministically. Never hand-derive anything it reports; never
  finish with it failing.
- **Community translations are owned by their contributors.** Update
  stale/missing keys in an existing language when asked, but do not rewrite a
  contributor's phrasing wholesale without the user's explicit direction.
- **Machine-assisted output is a first pass.** PRs and commits containing
  generated translations must say so and invite native-speaker review.
- **Keep the public roster current.** CONTRIBUTING.md's localization table
  (Planned / Machine-assisted / Native, plus credit) must be updated in the
  same commit whenever a language is added or a native review lands. The
  target roster lives there — consult it before proposing new languages.

## File map and conventions

- English Keyed source: `1.6/Languages/English/Keyed/PWU_UI.xml`
- Target layout: `1.6/Languages/<Language>/Keyed/*.xml` and
  `1.6/Languages/<Language>/DefInjected/<DefTypeFolder>/*.xml`
- `<DefTypeFolder>` must be the def's resolvable type name: bare for vanilla
  types (`JobDef`, `RecipeDef`, `ResearchProjectDef`) — this mod currently
  defines no custom def classes of its own, so its DefInjected targets are all
  vanilla type folders. If this mod ever adds its own def classes, the folder
  must be namespace-qualified, e.g. `PersonaWeaponsUnbound.SomeDef` — a bare
  custom name silently drops every translation in the folder.
- Current DefInjected targets and their translatable fields (checked against
  `1.6/Defs/`):
  - `JobDef` (`1.6/Defs/JobDefs/PWU_CustomizeWeapon.xml`) — `reportString`
  - `RecipeDef` (`1.6/Defs/RecipeDefs/PWU_Make_*.xml`) — `label`,
    `description`, `jobString`
  - `ResearchProjectDef`
    (`1.6/Defs/ResearchProjectDefs/PWU_BladelinkCustomization.xml`) —
    `label`, `description`
  - DefInjected keys are `DefName.field` paths
    (`PWU_BladelinkCustomization.description`) — the checker warns on
    uncovered `label`/`description`.
- **EN comment convention (required):** every translated entry carries the
  current English source directly above it:
  `<!-- EN: Customize persona: {0} -->` — this is how the checker detects
  staleness.
- Formatting: UTF-8 without BOM, LF endings, 2-space indent, final newline,
  root element `<LanguageData>`.
- Placeholders (`{0}`, `{1}`, named args) must match English exactly per key.
  Translator comments above placeholdered English keys explain what gets
  injected — injected values are lowercase def labels; phrase around them
  accordingly. See `PWU_RequireCustomizationResearchDesc`,
  `PWU_TechprintCountDesc`, and `PWU_EnableMonoswordRecipeDesc` /
  `PWU_EnablePlasmaswordRecipeDesc` / `PWU_EnableZeushammerRecipeDesc` in
  `PWU_UI.xml` for the convention in practice (each carries a comment like
  `<!-- {0} = bladelink customization (research label) -->` directly above
  the entry).

## Terminology grounding (do not skip)

Every game term must match the official localization, not a plausible
translation. Sources, in order:

1. Vanilla language data:
   `"$RIMWORLD_PATH"/Data/<Expansion>/Languages/<Language> (<Native>).tar`
   (read entries with `tar -xOf`). Check Core plus Royalty (this mod's DLC),
   and Ideology for relic/ideoligion strings.
2. This file's glossary below (lessons already learned — apply them).
3. If a term appears nowhere official, flag it in the PR for native review
   rather than inventing silently.

Terms that MUST be grounded before use: persona weapon, bladelink, trait
(weapon), monosword, plasmasword, zeushammer, persona core, techprint,
fabrication bench, bladelink customization (research), quality tiers, tech
levels. All of these have official vanilla/Royalty translations in the
language tars — use them rather than inventing a rendering.

### Glossary — (none yet)

No native review has landed for this mod yet. Add a table here (English / Use
/ Never / Why, one row per term) as soon as a native-speaker review corrects a
translation — mirror the glossary table format in UniqueWeaponsUnbound's
translate skill rather than inventing a new one.

One lesson carries over from Unique Weapons Unbound's Russian review and
applies to every language: weapon **trait** and pawn-personality **trait**
are different words in many official localizations. Never assume the
pawn-trait term applies to weapon traits — always check how the vanilla
`WeaponTraits` stat/label is localized in the target language's tar and use
that word, not the personality-trait word. (RU: свойство, never черта;
JP: 特性, shared with pawn traits — it varies per language.)

### Cross-language lessons (from UniqueWeaponsUnbound's translation work)

- Japanese vanilla style: ASCII punctuation (`,` `.`, never `、` `。`),
  です/ます descriptions, continuous-form job strings (〜している, no period),
  「」 around quoted labels.
- Wrap injected `{0}` def labels in the language's quote marks (JP 「{0}」,
  RU «{0}») — injected labels never inflect, and quoting sidesteps case and
  agreement problems.
- Coined vanilla terms (ideoligion) may be a portmanteau in one language
  (RU идеолигия) and a plain word in another (JP 思想) — always check, never
  extrapolate between languages. Relevant here for `PWU_RelicNameTooltip`.
- When an English string is reworded, refresh every language's EN comments in
  the same commit — the checker reports mismatches as STALE either way.
- The fuller per-language glossaries live in UniqueWeaponsUnbound's translate
  skill; consult them when the same vanilla domain terms come up.

## Workflows

### Initial generation (`/translate <Language>`)

1. Run the checker; confirm English itself is clean.
2. Enumerate English Keyed keys and DefInjected-translatable def fields
   (mirror the English file structure — there is no other language to mirror
   yet).
3. Extract the vanilla tar for the target language into the scratchpad;
   build a term list for the grounded terms above.
4. Translate via subagent(s) carrying: the glossary, the vanilla term list,
   the EN-comment requirement, placeholder rules, and formatting rules.
   Chunk by file section if the key count is large.
5. Run the checker (`--strict` for new languages); fix everything.
6. Review the diff yourself before committing. Commit message and PR text
   must state machine-assisted origin and invite native review.

### Update pass (`/translate update`)

1. Run the checker; it lists missing keys and stale entries per language.
2. Translate only that delta, refreshing each entry's EN comment.
3. Leave correct existing entries untouched. Re-run the checker.

### Audit only (`/translate check`)

Run the checker and report; change nothing.

## Optional in-game verification

RimWorld Dev Mode offers "Save translation report" and "clean up translation
files" (Verse.LanguageReportGenerator / TranslationFilesCleaner). These need a
running game with the mod loaded — useful as a final QA pass, not a substitute
for the checker.
