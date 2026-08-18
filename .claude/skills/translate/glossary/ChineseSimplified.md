# Simplified Chinese — Persona Weapons Unbound glossary

Grounded in PWU's own 2026-07-28 zh-Hans generation (preseeded from
UniqueMeleeWeapons' 2026-07 run and the Royalty zh tar; no native review
yet). Family-shared engine mechanics, style rules, and vanilla-grounded
common vocabulary live in `l10n/languages/ChineseSimplified.md` (whose own
"Excluded from this reference" section names exactly the PWU-specific items
kept here) — this file holds only what is specific to Persona Weapons
Unbound.

## Coined / mod-specific terms

Royalty zh ships the persona-weapon name grammar
(`DefInjected/RulePackDef/MeleeBladelink.xml`, syllable-composition rules) —
the style reference for any zh naming-grammar work here.

**"Bladelink" has no standalone zh rendering anywhere in vanilla.** Vanilla
zh marks bladelink weapons with the Δ' label prefix and calls the mind 智能
人格 in prose; the `Bladelink*` keyed strings sidestep the term entirely
(这把武器…). So `PWU_BladelinkCustomization.label` is a coinage: 智能人格定制,
chosen to match vanilla's terse research-label style (人工代谢, 枪械联控器)
while staying grounded in 智能人格. Flagged for native review in the
2026-07-28 commit — do not silently "correct" it toward a literal 刃链/剑链
rendering without a native speaker's call.

**Trait divergence by DLC.** Royalty's `Stat_Thing_PersonaWeaponTrait_Label`
= 特性 — unlike Russian, zh agrees with Odyssey's unique-weapon term here, so
there is no divergence to navigate (the черта/свойство-style split has no zh
counterpart).

## Vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| persona weapon (labels) | Δ' prefix: Δ'单分子剑 | 人格单分子剑 | Royalty `MeleeWeapon_MonoSwordBladelink.label`=Δ'单分子剑 — vanilla zh marks persona weapons with Δ' and never translates "persona" inside a label |
| persona (the onboard mind, prose) | 智能人格 / 武器的人格 | | Royalty bladelink weapon descriptions |
| persona weapon trait (stat) | 特性 (desc prose: 人格特性) | 属性 | Royalty `Stat_Thing_PersonaWeaponTrait_Label`=特性 |
| persona core | 人格核心 | | Core `AIPersonaCore` |
| techprint | 科研蓝图 | 技术图纸 | Royalty `TechprintLabel`=科研蓝图（{PROJECT_label}） |
| monosword / plasmasword / zeushammer | 单分子剑 / 等离子剑 / 宙斯锤 | | Royalty weapon labels |
| wielder | 使用者 | | Royalty `SpeedBoost` desc |
| freewielder (trait label) | 自由 | 自由持有 | Royalty `NeverBond.label` — quote it as “自由”特性 when naming it |
| stopping power / burst count / burst speed | 抑止能力 / 连射次数 / 射速 | | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| bladelink customization (this mod's research) | 智能人格定制 | | **Coined** — see above |

## Landmine — ShipComputerCore research prerequisite

`PWU_UI.xml`'s translator comment calls it "machine persuasion (vanilla
research label)", and that is the correct *English* label, but the def is
`ShipComputerCore` and zh renders it **飞船电脑核心** — nothing like the
English. Never translate that `{2}` hint literally; resolve the defName
through the tar. (This is PWU's own instance of the general
"resolve-a-hint-through-the-tar" methodology recorded in `l10n/languages/
ChineseSimplified.md`'s Pitfalls section — only the specific defName mapping
is mod-specific and lives here.)

## Workshop title (2026-08-18, machine-assisted, pending native review)

**人格武器解放** (= `PWU_SettingsCategory`, coupled to line 1 of
`.steamworkshop/Description/ChineseSimplified.txt`). 人格武器 is vanilla
Royalty zh's prose term for persona weapons (`Royalty.description`:
"挥舞具有独特特性的人格武器"), the searchable anchor since the Δ' label prefix
is not prose-usable. 解放 renders "Unbound" without colliding with the mod's
自由 (freewielder) or 定制 (customize) vocabulary.

Workshop-description-only notes from this pass: vanilla zh localizes the DLC
brand names, so the description uses 皇权 / 奥德赛 / 文化 (unlike the other 7
languages, which keep them English per vanilla); Odyssey's unique weapon is
特化武器 (used for the UWU sibling-mod references).
