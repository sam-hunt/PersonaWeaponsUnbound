# Korean — Persona Weapons Unbound glossary

Grounded in PWU's own 2026-07-28 ko generation (Korean had no preseed from a
sibling mod; no native review yet). Family-shared engine mechanics
(including the josa/particle-resolution mechanism and its digit-batchim
gap), style rules, and vanilla-grounded common vocabulary live in
`l10n/languages/Korean.md` — this file holds only what is specific to
Persona Weapons Unbound.

## Coined / mod-specific terms

**Korean is the one language so far where vanilla *does* render
"bladelink".** Royalty ko marks persona weapons with the prefix **결속**
("bond") — `MeleeWeapon_MonoSwordBladelink.label` = 결속 단분자검 — and
calls the class 결속 무기 in prose. Do NOT coin here; 결속 무기 is vanilla's
own term. (One stray 영혼무기 "soul weapon" appears in
`BladelinkAlreadyBondedDialog`; it is an outlier, not the term.)

**The trait divergence is *inside* Royalty ko, not between DLCs.** Royalty's
`Stat_Thing_PersonaWeaponTrait_Label` is **개성** ("individuality"), but
Royalty's own `BladelinkEquipWarningTraits` — the heading shown when a
player is told what traits a bladelink weapon has — is **무기 특성**, which
is also Odyssey's `Stat_ThingUniqueWeaponTrait_Label`. So the usual rule
("use Royalty's word, not Odyssey's") cannot decide this one: Royalty ships
both. PWU uses 무기 특성 / 특성 because (a) it is a Royalty word for exactly
this list, (b) 개성 appears only as the info-card *stat row name* and never
as a countable item, and reads badly across the 20+ strings that count,
conflict, hide and cap traits. Note ko does NOT get a free disambiguation
from pawn personality traits either: `<Traits>` is 특성 too, so 무기 특성 is
the disambiguating form when there is no surrounding weapon context (PWU's
Traits tab uses it for that reason). A native reviewer may prefer aligning
the tab with the info card's 개성 — flagged in the 2026-07-28 commit; do not
silently flip it either way.

**"Customize" has essentially no vanilla ko anchor.** The only `Customize*`
key in Core/Royalty/Ideology/Odyssey ko is `CustomizeIdeoligion` = 사상 생성
("ideoligion creation"), a paraphrase that does not generalize; 사용자 지정
appears twice as generic software boilerplate. PWU therefore coins **개조**
("remodel/modification"), real RimWorld ko vocabulary
(`MedicalOperationsMechanoidsShort` = 개조) and natural for weapons. Chosen
to stay single-valued across all 158 keys, where it recurs constantly and
must compose: 자아 개조 (gizmo), 개조 창 (dialog), 개조가 중단되었습니다 (bail
messages), 결속 무기 개조 (research), 개조용으로 해금 (discovery). Flagged for
native review.

**Mechanite — corrected 2026-07-28.** 기계입자 (36/36 occurrences across 7
Core files incl. `Hediffs_Local_Infections`, `Luciferium`,
`Items_Exotic`), not 나노머신 — 나노머신 renders the *different* English word
"nanomachines" (Royalty's Armorskin/Stoneskin glands); Royalty's monosword
desc paraphrases to 나노 기술 and is not a term source. Grounding on
Royalty+Biotech alone misses this.

## Vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| bladelink / persona weapon | 결속 무기 (label prefix 결속) | 블레이드링크, 페르소나 무기 | Royalty `MeleeWeapon_*Bladelink.label`, `BladelinkEquipWarning*` |
| persona (the onboard mind) | 자아 (무기의 자아) | 인격, 페르소나 | Royalty bladelink descs, `NoPain`/`SpeedBoost` descs |
| AI persona core | 인공자아 핵 | 인공지능 코어, 페르소나 핵 | Core `AIPersonaCore.label` |
| trait (persona weapon) | 무기 특성 standalone; 특성 once context is the weapon | 개성 as a countable item | see the divergence note above |
| monosword / plasmasword / zeushammer | 단분자검 / 플라즈마검 / 제우스망치 | 모노소드 | Royalty weapon labels — ko translates, does not transliterate |
| longsword / mace | 장검 / 철퇴 | | Core labels |
| mechanite(s) | 기계입자 | 나노머신, 메카나이트 | see the correction note above |
| techprint | 기술청사진 | 기술 도면 | Core `TechprintLabel` |
| fabrication bench | 조립 작업대 | 제작 작업대, 정밀 작업대 | Core `FabricationBench.label` |
| advanced components | 고급 부품 | 고급 부품류, 첨단 부품 | Core `ComponentSpacer.label`; plain components are 부품 |
| Crafting (the skill) | 제작 | 수공예, 공예 | Core `Crafting.label` |
| bill (work bill) | 계획서 (add-bill menu: 계획서 추가) | 작업 지시, 청구서 | Core `TabBills`, `AddBill` |
| Empire (faction) | 와해된 제국 | 제국 alone | Royalty `Empire.label` |
| freewielder (trait label) | 자유 의지 | 자유 소유자 | Royalty `NeverBond.label` — quote it as '자유 의지' 특성 |
| stopping power / burst count / burst speed | 저지력 / 연발 횟수 / 발사속도 | | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| armor penetration / damage | 관통력 / 피해량 | 방어 관통 | Core `ArmorPenetration`, `Damage` |
| EMP stun | EMP에 기절함 | | Core `StunnedByEMP` |
| customize / customization | 개조 | 커스터마이즈, 맞춤 설정, 사용자 정의 | **Coined** — see above |
| bladelink customization (this mod's research) | 결속 무기 개조 | | **Coined** from 결속 무기 + 개조 |

## Worked example — digits and josa

`AlphabetEndPattern` (the josa-resolution mechanism, see `l10n/languages/
Korean.md`) covers only `b c k l m n p q t` — no digits — so a particle
placed directly after a number always resolves to the no-batchim form,
wrong for 1/3/6/7/8/0. PWU phrases around it:
`PWU_CouldNotStartReservationConflict` says `{1} x{2} 예약에 실패했습니다`
rather than `x{2}(을)를 예약하지 못했습니다`.

## Landmine that does NOT bite in Korean

`PWU_UI.xml`'s translator comment calls the persona-core recipe's research
prerequisite "machine persuasion (vanilla research label)"; the def is
`ShipComputerCore`, which ja renders AIコンピュータコア and zh 飞船电脑核心 —
nothing like the English. Korean is the exception:
`ShipComputerCore.label` = **기계 설득**, a literal match for "machine
persuasion". Still resolve the defName through the tar rather than trusting
the hint — the coincidence is language-specific, not a general licence.

## Workshop title (2026-08-18, machine-assisted, pending native review)

**결속 무기 자유 개조** (= `PWU_SettingsCategory`, coupled to line 1 of
`.steamworkshop/Description/Korean.txt`). 결속 is vanilla Royalty ko's
bladelink prefix (`MeleeWeapon_MonoSwordBladelink.label` = 결속 단분자검), so
결속 무기 is the searchable anchor; 자유 개조 renders "Unbound" using the
mod's own coined 개조 (customize) vocabulary.

New coinage from this pass: **배타 태그 충돌** = "exclusion-tag conflicts"
(no vanilla anchor exists), pending native review.
