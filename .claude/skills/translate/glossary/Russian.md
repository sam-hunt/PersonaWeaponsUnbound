# Russian — Persona Weapons Unbound glossary

This mod ships a full Russian Keyed translation (machine-assisted, credited
to Fable 5 in CONTRIBUTING.md, landed circa 2026-07). The old
(pre-shared-toolkit) `translate` skill never documented a Russian glossary
for it; that gap was closed on 2026-08-18 by grounding the domain vocabulary
below fresh against the Core + Royalty Russian tars (per `l10n/process.md`'s
terminology-grounding method) and auditing the shipped Keyed/DefInjected
against every row (all consistent). The translation as a whole remains
machine-assisted, pending native review. Family-shared engine mechanics,
style rules, and vanilla-grounded common vocabulary live in
`l10n/languages/Russian.md`.

## Trait divergence by DLC (mandatory check)

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
finding as the reason the rule doesn't generalize). Audit 2026-08-18: the
shipped translation uses черта throughout (48 occurrences) and свойство
never.

## Domain vocabulary (grounded 2026-08-18 against Core + Royalty ru tars)

| English | Use | Never | Why / vanilla source |
|---|---|---|---|
| persona weapon (category/prose) | оружие с личностью | персона-оружие | Core `WeaponsMeleeBladelink.label` = "оружие с личностью", verbatim |
| bladelink weapon (system noun) | синхрооружие | бладлинк | mod coinage extending vanilla's синхро- bladelink prefix (see weapon rows below); vanilla ru has no standalone noun for the system |
| persona (the onboard mind) | личность / ИИ-личность | персона | Royalty `BladelinkEquipWarning`: "ИИ-личность этого оружия" |
| bond / bonding | связь / синхронизация | привязка (as the mechanic noun) | Royalty bladelink descriptions ("После формирования связи..."); `BladelinkEquipWarning` ("После синхронизации с владельцем") |
| monosword | мономеч | | Royalty `MeleeWeapon_MonoSword.label` |
| plasmasword | плазменный меч | | Royalty `MeleeWeapon_PlasmaSword.label` |
| zeushammer | электромолот | | Royalty `MeleeWeapon_Zeushammer.label` |
| persona monosword | синхромономеч | | Royalty `MeleeWeapon_MonoSwordBladelink.label` |
| persona plasmasword | плазменный синхромеч | | Royalty `MeleeWeapon_PlasmaSwordBladelink.label` |
| persona zeushammer | (no vanilla ru label) | | `MeleeWeapon_ZeushammerBladelink` has NO label in the ru tars as of 1.6 (vanilla gap, verified 2026-08-18); if a rendering is ever needed, follow the синхро- pattern (синхроэлектромолот) and flag for native review |
| AI persona core | ядро ИИ | ядро персоны | Core `AIPersonaCore.label` |
| techprint | чертёж | техносхема | Royalty Keyed `LetterTechprintAppliedLabel` ("Применён чертёж: ...") |
| fabrication bench | высокоточный станок | сборочный стол | Core `FabricationBench.label`; same row in UWU's ru glossary (PR #6) |
| freewielder / free-wielding trait | свободолюбие | | Royalty `NeverBond.label` |
| ultratech (tech level) | ультратехнологичный | | Core Keyed `TechLevel_Ultra` |
| ideoligion | идеолигия | идеология | vanilla's coined portmanteau (`ReformIdeoligion`); shipped in `PWU_RelicNameTooltip`; matches UWU's PR #6 row |

The shipped translation's own key term split is deliberate:
**синхрооружие** names the bladelink weapon class/system (e.g.
`PWU_BladelinkCustomization.label` = "настройка синхрооружия", "черты
синхрооружия" in recipe prose), while **оружие с личностью** names a weapon
in its converted persona state — both anchored to vanilla as cited above.

## Workshop title (2026-08-18, machine-assisted, pending native review)

**Синхрооружие без ограничений** (= `PWU_SettingsCategory`, coupled to line 1
of `.steamworkshop/Description/Russian.txt`). синхрооружие is the shipped ru
translation's own term for bladelink weapon (see the vocabulary table);
без ограничений mirrors UWU's ru title pattern (Уникальное оружие без
ограничений) for family consistency. The Workshop description reuses the
table's terms verbatim.

Vanilla-corpus note: Core ru's `AdvancedFabrication.label` is
«сверхвысокоточное производство» (verified against the Core ru tar during
this pass; an initial «Высокоточное производство» draft was corrected).
Slated to move upstream to `l10n/languages/Russian.md` with the other
languages' AdvancedFabrication rows (see TODOs.md).
