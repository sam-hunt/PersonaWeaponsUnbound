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
- **The type folder is load-bearing, not organizational** (decompile-verified,
  `Verse.LoadedLanguage`): RimWorld enumerates only the top-level directories
  under `DefInjected/` and resolves each directory *name* to the def type its
  files target. An `.xml` placed directly in `DefInjected/` is never loaded,
  and the checker likewise iterates only directories — a misplaced file fails
  silently on both sides, so never flatten the tree. *Inside* a type folder
  everything is free: file names are arbitrary and files are found recursively,
  so one bundled file per type vs one-def-per-file is pure preference — this
  repo bundles per type, since reviewers work in whole-language passes and
  entries are found by their defName-prefixed keys, not by file. (The loader
  even tolerates a pluralized folder name by retrying with the last character
  stripped — `ThingDefs` → `ThingDef` — but the checker does not; use exact
  type names.)
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

### Glossary — native-review lessons (none yet)

No native review has landed for this mod yet. Add a table here (English / Use
/ Never / Why, one row per term) as soon as a native-speaker review corrects a
translation — mirror the glossary table format in UniqueWeaponsUnbound's
translate skill rather than inventing a new one.

One lesson carries over from Unique Weapons Unbound's Russian review and
applies to every language: weapon **trait** and pawn-personality **trait**
are different words in many official localizations. Never assume the
pawn-trait term applies to weapon traits — always check how the vanilla
weapon-trait stat/label is localized in the target language's tar and use
that word, not the personality-trait word. And check the RIGHT stat: the
DLC domains diverge. For THIS mod the authority is Royalty's
`Stat_Thing_PersonaWeaponTrait_Label` (persona weapon traits), NOT
Odyssey's `WeaponTraits` (unique weapons, UWU's domain) — in Russian the
two disagree (Royalty: черты, per `BladelinkEquipWarningTraits` and the
Royalty DLC description; Odyssey: свойства, which is where UWU's
"свойство, never черта" glossary row comes from). PWU's Russian therefore
uses «черта»; this is deliberate, flagged for native review in the
2026-07 generation commit. Japanese diverges the same way: Royalty's
`Stat_Thing_PersonaWeaponTrait_Label` is 特性・特徴, while Odyssey's
`Stat_ThingUniqueWeaponTrait_Label` is plain 特性 — use 特性・特徴 here.

### Glossary — Japanese (machine-assisted ja landed 2026-07-28; no native review yet)

Rows below the divider were grounded during PWU's own 2026-07-28 ja generation;
rows above it were preseeded from UniqueMeleeWeapons' 2026-07 run and the
Royalty JP tar. RimWorld's language folder is `Japanese` (tar:
`Japanese (日本語).tar`) — the mod folder must match it exactly.

Style rules discovered from the vanilla JP data (mandatory):

- Vanilla JP uses ASCII punctuation: `,` and `.` — never `、` or `。`.
- Descriptions/tooltips: polite です/ます form ending `.`; labels/buttons no
  period. Thought (`ThoughtDef` stage) descriptions are plain first-person.
- Quote injected def labels and cross-referenced UI labels with 「」. Suffixes
  and parentheticals take no leading space and use ASCII parens.
- `traitAdjectives` are **attributive** forms ending in の / な / い / a verb
  (Royalty and Odyssey ship 探知の, 正確な, 灼熱の). The JP namer concatenates
  with no space, so a bare noun reads broken.
- Name grammar: no spaces around [symbols]; "The X of Y" → `[Y]の[X]`; vanilla
  keeps `[RECIPIENT_possessive]` (unlike zh, which drops it).
- Battle-log entries end in plain past tense and JP `[skillAdv]` values are
  adverbials (巧みに, ゆっくりと), so `[skillAdvMaybe]` slots before the verb.
- `deathMessage` keeps vanilla's space after the pawn token: `{0}は 斬られて…`.
- DLC names stay in Latin script (Royalty, Odyssey), as does MOD.
- Ellipsis is ASCII `...` (vanilla Keyed uses it near-exclusively; `…` is rare).
- `RecipeDef.label` is `〜を作る` and `jobString` is `〜を作成中`;
  `JobDef.reportString` is continuous `〜している`. All three take no period
  (Core `Make_ComponentSpacer`, `Make_Wort`, `LayEgg`).
- Vanilla writes `: ` (ASCII colon + space) before an injected value —
  `テックプリントが適用された: {PROJECT_label}`. Use that, not a full-width `：`.
- Pawn names are never quoted; def labels and cross-referenced UI labels are
  (「」). Quality labels take 「」 in prose but not in the vanilla compound
  `{0}以上の品質`.

| English | Use | Never | Why |
|---|---|---|---|
| trait (persona weapon) | 特性・特徴 | 特性 alone | Royalty `Stat_Thing_PersonaWeaponTrait_Label`, `BladelinkEquipWarningTraits`; plain 特性 is Odyssey's *unique*-weapon word |
| persona weapon / persona / bond | ペルソナ武器 / ペルソナ (人格 in the bond letter) / 絆, 絆を結ぶ | | Royalty `WeaponsMeleeBladelink`, `BladelinkEquipWarning*`, `LetterBladelinkWeaponBonded` |
| monosword / plasmasword / zeushammer | モノソード / プラズマソード / ゼウスハンマー (persona forms prefix ペルソナ) | | Royalty weapon labels |
| longsword / spear / mace / knife / gladius / axe / warhammer | ロングソード / スピア / メイス / ナイフ / グラディウス / 戦斧 / ウォーハンマー | 長剣, 槍 | Core/Royalty labels are mostly katakana |
| ultratech | 最先端の技術力 (noun) / 最先端技術級 (attributive) | ウルトラテック | vanilla `TechLevel_Ultra` |
| plasteel / jade / wood (stuff adjectives) | プラスチール製 / ヒスイ製 / 木製 | 塑鋼, 翡翠 | Core `stuffProps.stuffAdjective` — note the `〜製` shape, so `[stuff_adjective]の[noun]` composes cleanly |
| mechanite / mechanoid | メカナイト / メカノイド | | Royalty, Odyssey descs |
| wielder / bearer | 使用者 / 持ち主 | | Odyssey `EMPPulser` desc |
| stun / EMP / stagger | スタン / EMP / よろめき | | `StunnedByEMP`, `StaggerDurationFactor` |
| armor penetration / bleed rate / move speed | アーマー貫通力 / 出血量 / 移動速度 | | Core Keyed + StatDefs |
| cut / stab (DamageDef) | 斬る / 刺す | 切創, 刺し傷 (those are the *hediff* labels) | Core DamageDefs vs HediffDefs differ |
| humanlike / ability / quest / cooldown / cells | 人型 / 能力 / クエスト / クールダウン / セル | | Core Keyed |
| quality tiers | 壊れかけ/低品質/標準品/良品/秀品/名品/幻の一品 | | Core `QualityCategory_*` |
| Cancel / Reset / Reset to defaults | キャンセル / リセット / デフォルトに戻す | | vanilla Keyed buttons |
| Traders will pay more/less for it. | 貿易商は高値で/低い価格でこれを買い取ります. | | Odyssey `GoldInlay`/`Ugly` descs — reuse verbatim |
| — grounded in PWU's own 2026-07-28 run below — | | | |
| persona core / AI persona core | AI人格コア | ペルソナコア | Core `AIPersonaCore.label` — vanilla JP renders "persona core" with 人格, not ペルソナ, in *this* noun |
| techprint | テックプリント | 技術図面 | Core `TechprintLabel` |
| fabrication bench | コンポーネント工作台 | 製造台, 精密工作機械 (that's the `Machining` research) | Core `FabricationBench.label` |
| advanced components | 先進コンポーネント | 高度部品 | Core `ComponentSpacer.label`; plain components are コンポーネント |
| Crafting (the skill) | 工芸 | 製作, クラフト | Core `Crafting.label` |
| bill (work bill) | 加工 (add-bill menu: 新しい加工) | 請求, ビル | Core `TabBills`=加工, `AddBill`=新しい加工 |
| recipe | レシピ | | Core `Stat_Recipe_*`, `RecipeRequiresSkills` |
| Empire (faction) | 落日の帝国 | 帝国 alone | Royalty `Empire.label` |
| ideoligion / reform | 思想 / 思想を改変 | イデオロギー | Ideology `IdeoligionOf`, `ReformIdeoligion` |
| relic | レリック | 聖遺物 (that's the *reliquary*, 聖遺物箱) | Ideology `RelicOf`, `RelicTip` |
| freewielder (trait label) | 自由支配者 | 自由 | Royalty `NeverBond.label` — quote it as 「自由支配者」 when naming it |
| stopping power / burst count / burst speed | 威力 / バースト時の弾数 / 連射速度 | 抑止力 | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| "{0} quality or better" | `{0}以上の品質` | | Core `NormalQualityOrBetter`=普通以上の品質 |
| Confirm / Randomize | 了承 / ランダム | 確定 | Core `Confirm`, `Randomize` |
| colonist / research project / appearance | 入植者 / 研究プロジェクト / 外観 | | Core `Colonist`, `NeedResearchBenchDesc`, `Appearance` |
| plasteel / uranium | プラスチール / ウラン | | Core `Plasteel.label`, `Uranium.label` |
| gizmo button | コマンドボタン | ギズモ | Core `Command*Desc` calls them コマンド |
| bladelink customization (this mod's research) | ペルソナ武器のカスタマイズ | | **Coined** — see the note below |

Two persona-specific notes.

**"Bladelink" has no standalone ja rendering anywhere in vanilla.** Vanilla ja
prefixes the persona weapons with ペルソナ (ペルソナモノソード) and calls the
mind ペルソナ / 武器に宿るペルソナ in prose, while the `MeleeWeapon_*Bladelink`
ThingDef descriptions switch to 搭載されたAI and 生体認証 — the term itself is
always sidestepped. So `PWU_BladelinkCustomization.label` is a coinage:
ペルソナ武器のカスタマイズ, chosen to keep カスタマイズ single-valued across all
158 keys (the word recurs constantly: "customization interrupted",
"customization dialog", "customization research"). It is longer than vanilla's
typical research label, and Royalty *does* transliterate the parallel
construction — `Gunlink.label` is ガンリンク — so a native reviewer may well
prefer a terser ブレードリンク改変 or ペルソナ武器改変. Flagged for native review
in the 2026-07-28 commit; do not silently "correct" it either way. Note 改変 is
vanilla's own verb for altering a persona core (`ShipComputerCore`'s
`generalRules` ship `subject->AI人格コア改変`) and for `ReformIdeoligion`, so it
is the grounded alternative if the reviewer wants brevity.

**Landmine — the persona-core recipe's research prerequisite.** `PWU_UI.xml`'s
translator comment calls it "machine persuasion (vanilla research label)", and
that is the correct *English* label, but the def is `ShipComputerCore` and ja
renders it **AIコンピュータコア** — nothing like the English. It reaches the
player through `{2}` at runtime, so nothing is translated there, but never
resolve that hint literally when phrasing around it; look the defName up in the
tar. (No def named `MachinePersuasion` exists, so grepping the hint text finds
nothing.)

### Glossary — Simplified Chinese (machine-assisted zh landed 2026-07-28; no native review yet)

Rows below the divider were grounded during PWU's own 2026-07-28 zh-Hans
generation; rows above it were preseeded from UniqueMeleeWeapons' 2026-07 run
and the Royalty zh tar. RimWorld's language folder is `ChineseSimplified` (tar:
`ChineseSimplified (简体中文).tar`) — the mod folder must match it exactly.

Style rules from the vanilla zh data (mandatory):

- Full-width punctuation in prose (，。、；：（）……); descriptions end with 。;
  labels and buttons carry no trailing period. Placeholders, digits and units
  stay ASCII. Vanilla labels use full-width parens: 科研蓝图（{PROJECT_label}）.
- Quote cited names in prose with full-width curly quotes — vanilla writes
  任务“{0}”. Terse stat and job-report templates take no quotes.
- Vanilla zh files can contain untranslated English values — vanilla
  incompleteness is not style guidance. Some vanilla zh files carry a BOM;
  ours never do.

| English | Use | Never | Why |
|---|---|---|---|
| persona weapon (labels) | Δ' prefix: Δ'单分子剑 | 人格单分子剑 | Royalty `MeleeWeapon_MonoSwordBladelink.label`=Δ'单分子剑 — vanilla zh marks persona weapons with Δ' and never translates "persona" inside a label |
| persona (the onboard mind, prose) | 智能人格 / 武器的人格 | | Royalty bladelink weapon descriptions |
| persona weapon trait (stat) | 特性 (desc prose: 人格特性) | 属性 | Royalty `Stat_Thing_PersonaWeaponTrait_Label`=特性 — unlike Russian, zh agrees with Odyssey's unique-weapon term; the черта/свойство divergence has no zh counterpart |
| persona core | 人格核心 | | Core `AIPersonaCore` |
| techprint | 科研蓝图 | 技术图纸 | Royalty `TechprintLabel`=科研蓝图（{PROJECT_label}） |
| monosword / plasmasword / zeushammer | 单分子剑 / 等离子剑 / 宙斯锤 | | Royalty weapon labels |
| ultratech (attributive) | 极致科技 | 超科技 | `TechLevel_Ultra`=极致时代; `BodyPartsUltra`=极致科技 |
| wielder | 使用者 | | Royalty `SpeedBoost` desc |
| plasteel | 玻璃钢 | 塑钢 | Core `Plasteel` — counterintuitive, always check |
| quality tiers | 极差/较差/一般/良好/极佳/大师级/传奇级 | | Core `QualityCategory_*` |
| — grounded in PWU's own 2026-07-28 run below — | | | |
| fabrication bench | 精密装配台 | 制造台 | Core `FabricationBench.label` |
| advanced components | 高级零部件 | 高级元件 | Core `ComponentSpacer.label`; plain components are 零部件 |
| quality (the noun) | 品质 | 质量 | Core `Quality`/`QualityIs` |
| Crafting (the skill) | 手工 | 制作 | Core `Crafting.label` — 制作 is the *verb*, never the skill name |
| bill (work bill) | 清单 | 工单/账单 | Core `AddBill`=添加清单 — the common community rendering 工单 is not vanilla |
| Empire (faction) | 破碎帝国 | 帝国 | Royalty `Empire.label` |
| ideoligion / reform | 文化形态 / 文化改革 | 意识形态 | Ideology `IdeoligionOf`, `ReformIdeoligion` |
| relic | 圣物 | 遗物 | Ideology `RelicTip`, `RelicOf` |
| freewielder (trait label) | 自由 | 自由持有 | Royalty `NeverBond.label` — quote it as “自由”特性 when naming it |
| stopping power / burst count / burst speed | 抑止能力 / 连射次数 / 射速 | | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| bladelink customization (this mod's research) | 智能人格定制 | | **Coined** — see the note below |

Three persona-specific notes.

Royalty zh ships the persona-weapon name grammar
(`DefInjected/RulePackDef/MeleeBladelink.xml`, syllable-composition rules) —
the style reference for any zh naming-grammar work here.

**"Bladelink" has no standalone zh rendering anywhere in vanilla.** Vanilla zh
marks bladelink weapons with the Δ' label prefix and calls the mind 智能人格 in
prose; the `Bladelink*` keyed strings sidestep the term entirely (这把武器…).
So `PWU_BladelinkCustomization.label` is a coinage: 智能人格定制, chosen to
match vanilla's terse research-label style (人工代谢, 枪械联控器) while staying
grounded in 智能人格. Flagged for native review in the 2026-07-28 commit —
do not silently "correct" it toward a literal 刃链/剑链 rendering without a
native speaker's call.

**Landmine — the persona-core recipe's research prerequisite.** `PWU_UI.xml`'s
translator comment calls it "machine persuasion (vanilla research label)", and
that is the correct *English* label, but the def is `ShipComputerCore` and zh
renders it **飞船电脑核心** — nothing like the English. Never translate that
`{2}` hint literally; resolve the defName through the tar. (No def named
`MachinePersuasion` exists, so grepping the hint text finds nothing.)

### Cross-language lessons (from UniqueWeaponsUnbound's translation work)

- Japanese vanilla style: ASCII punctuation (`,` `.`, never `、` `。`),
  です/ます descriptions, continuous-form job strings (〜している, no period),
  「」 around quoted labels.
- Wrap injected `{0}` def labels in the language's quote marks (JP 「{0}」,
  RU «{0}», zh-Hans “{0}”) — injected labels never inflect, and quoting
  sidesteps case and agreement problems.
- Coined vanilla terms (ideoligion) may be a portmanteau in one language
  (RU идеолигия) and a plain word in another (JP 思想, zh-Hans 文化) — always
  check, never extrapolate between languages. Relevant here for
  `PWU_RelicNameTooltip` (zh-Hans relic = 圣物).
- Mod-coined terms recur in def labels AND in Keyed settings prose that
  restates them. When generation is chunked across files or subagents,
  reconcile those terms across the whole language before committing (UMW's
  zh-Hans run needed an alignment pass for its ability/hediff/trait names).
- When an English string is reworded, refresh every language's EN comments in
  the same commit — the checker reports mismatches as STALE either way.
- The fuller per-language glossaries live in UniqueWeaponsUnbound's translate
  skill; consult them when the same vanilla domain terms come up.

## Workflows

### Initial generation (`/translate <Language>`)

1. Run the checker; confirm English itself is clean.
2. Enumerate English Keyed keys and DefInjected-translatable def fields
   (mirror the structure of an existing language if one exists — Russian,
   as of 2026-07).
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
