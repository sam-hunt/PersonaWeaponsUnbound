# Russian — Persona Weapons Unbound glossary

This mod ships a full Russian Keyed translation (machine-assisted, credited
to Fable 5 in CONTRIBUTING.md, landed circa 2026-07 mirroring the structure
of an early language pass), but — unlike Japanese, Simplified Chinese,
Korean, German, Spanish, French, and Brazilian Portuguese — the old
(pre-shared-toolkit) `translate` skill never documented a dedicated Russian
coined-term glossary for it. This file preserves the one PWU-specific
finding that *was* recorded (below) rather than inventing vocabulary rows
that were never grounded and written down. Family-shared engine mechanics,
style rules, and vanilla-grounded common vocabulary (including the general
Cancel-button and job-report-register findings from UniqueWeaponsUnbound's
PR #6 native review) live in `l10n/languages/Russian.md`.

## Trait divergence by DLC (the one recorded PWU-specific finding)

Weapon **trait** and pawn-personality **trait** are different words in
Russian, and the DLC domains diverge: Royalty's
`Stat_Thing_PersonaWeaponTrait_Label` (persona weapon traits — this mod's
domain) is **черты**, per `BladelinkEquipWarningTraits` and the Royalty DLC
description, while Odyssey's `WeaponTraits` (unique weapons —
UniqueWeaponsUnbound's domain) is **свойства**. PWU's Russian therefore uses
**«черта»** — this is deliberate, flagged for native review in the 2026-07
generation commit, and must not be "corrected" toward свойство by anyone
porting UWU's Odyssey-scoped glossary row (UWU's own Russian glossary,
grounded through its PR #6 native review, explicitly scopes its
"свойство, never черта" rule to Odyssey and calls out this mod's Royalty
finding as the reason the rule doesn't generalize).

## Gap

No vocabulary table, worked-rewrite notes, or coinage discussion for
Russian exists elsewhere in this repo's history — a future generation or
native-review pass for PWU's Russian should ground its terms fresh against
the Royalty tar (per `l10n/process.md`'s terminology-grounding method) and
record them here, using UniqueWeaponsUnbound's `glossary/Russian.md` (itself
grounded through PR #6, the one native review anywhere in the mod family)
as the structural model — not as a source of PWU-domain terms to copy,
since UWU's own vocabulary is Odyssey-scoped and does not cover
persona/bladelink content at all.

## Workshop title and seed vocabulary (2026-08-18, machine-assisted, pending native review)

**Синхрооружие без ограничений** (= `PWU_SettingsCategory`, coupled to line 1
of `.steamworkshop/Description/Russian.txt`). синхрооружие is the shipped ru
translation's own term for bladelink weapon (`PWU_BladelinkCustomization.label`
= "настройка синхрооружия"); без ограничений mirrors UWU's ru title pattern
(Уникальное оружие без ограничений) for family consistency.

Terms this Workshop pass reused verbatim from the shipped ru translation
(partial backfill of the gap above; a full grounding pass is still owed):

| EN | ru (as shipped) | source |
|---|---|---|
| bladelink weapon (system noun) | синхрооружие | `PWU_BladelinkCustomization.label` |
| persona weapon (converted state) | оружие с личностью | shipped DefInjected descriptions (deliberately distinct from синхрооружие) |
| fabrication bench | высокоточный станок | shipped RecipeDef/JobDef strings |
| AI persona core | ядро ИИ | `PWU_Make_AIPersonaCore.label` |
| ideoligion | идеолигия | `PWU_RelicNameTooltip` |
| free-wielding trait | свободолюбие | `PWU_MemoryBondSeveredByTrait` |

Vanilla-corpus note: Core ru's `AdvancedFabrication.label` is
«сверхвысокоточное производство» (verified against the Core ru tar during
this pass; an initial «Высокоточное производство» draft was corrected).
