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
- **`<Language>` is the vanilla tar name with the native-name suffix stripped**
  (decompile-verified, `Verse.LoadedLanguage`): the ctor derives
  `legacyFolderName` by cutting `folderName` at the first `(` and trimming, and
  `AllDirectories` probes each mod for `Languages/<folderName>` first, then
  `Languages/<legacyFolderName>`. So `Korean (한국어).tar` → the mod folder is
  `Korean`, `ChineseSimplified (简体中文).tar` → `ChineseSimplified`, and so on.
  Either spelling loads; this repo uses the short one throughout. Anything else
  is silently ignored — no error, just an untranslated mod.
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

The check is mandatory but its answer is not always "they differ": zh-Hans
agrees across both DLCs (特性), German agrees across both DLCs *and* with the
pawn-trait word (Merkmale), and Korean diverges *within* Royalty rather than
between DLCs (개성 vs 무기 특성). Spanish is the cleanest divergence of all —
Royalty says **características** in *both* `Stat_Thing_PersonaWeaponTrait_Label`
and `BladelinkEquipWarningTraits`, while Odyssey's
`Stat_ThingUniqueWeaponTrait_Label` *and* Core's pawn `<Traits>` both say
**rasgos**. Brazilian Portuguese is the exact **mirror image of Spanish**: Royalty
says **traços** in both those keys and Core's pawn `<Traits>` agrees, while
Odyssey's `Stat_ThingUniqueWeaponTrait_Label` (plus `WeaponTraits` and
`StatsReport_WeaponTraits`) says **características** — so the same two words swap
sides between es and pt-BR, and picking by memory rather than by lookup gets it
backwards in one of them. French inverts the axis instead: there the *weapon* word
is the plain **Traits** (all three DLC keys agree) and the *pawn* header is the
special one ("Éléments marquants"). Run the lookup every time; record what came
back in that language's glossary either way.

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

### Glossary — Korean (machine-assisted ko landed 2026-07-28; no native review yet)

All rows below were grounded during PWU's own 2026-07-28 ko generation — Korean
had no preseed from a sibling mod. RimWorld's language folder is `Korean` (tar:
`Korean (한국어).tar`).

**Korean is the one language so far where vanilla *does* render "bladelink".**
Royalty ko marks persona weapons with the prefix **결속** ("bond") —
`MeleeWeapon_MonoSwordBladelink.label` = 결속 단분자검 — and calls the class
결속 무기 in prose. Do NOT coin here the way ja and zh had to; 결속 무기 is
vanilla's own term. (One stray 영혼무기 "soul weapon" appears in
`BladelinkAlreadyBondedDialog`; it is an outlier, not the term.)

Style rules from the vanilla ko data (mandatory):

- ASCII punctuation: `.` and `,` — never `。` or `、`. Descriptions/tooltips end
  with `.` in polite 합니다/입니다 form; labels and buttons take no period.
- Quote cited def labels and cross-referenced UI labels with ASCII single
  quotes — vanilla writes 연구 프로젝트 '{PROJECT_label}'. No 「」, no curly
  quotes. Pawn names are not quoted.
- `RecipeDef.label` is `〜 만들기`, `jobString` is `〜 만드는 중`, and
  `JobDef.reportString` is likewise `〜 중` — all three with no period
  (Core `Make_ComponentSpacer`, `Make_Wort`, `BuildSnowman`).
- Research labels are terse noun phrases (기계 설득, 심층 굴착, 정교한 강선).

**Josa (particle) resolution — Korean-specific, load-bearing.** Verse ships
`LanguageWorker_Korean`, whose `PostProcessed` runs `ReplaceJosa` over the
finished string. Write the ambiguous particle as a paren pair and the worker
picks the right form from the preceding syllable's batchim. Supported tokens,
exactly: `(이)가` `(와)과` `(을)를` `(은)는` `(아)야` `(이)어` `(으)로` `(이)`.
Anything else is left as literal text — so never invent a pair like `(과)와`
(wrong order) or `(에)에서`. Verified in the decompiled worker:

- `FindLastChar` explicitly skips a preceding `'` or `"`, so
  `'{0}'(와)과 충돌` resolves off the label's last syllable, not the quote.
  Quoting an injected label and then attaching a josa is safe.
- It also walks back past a trailing `(...)` parenthetical to the char before it.
- **`AlphabetEndPattern` is `b c k l m n p q t` only — no digits.** A josa
  placed directly after a number therefore *always* resolves to the
  no-batchim form, which is wrong for 1/3/6/7/8/0. Phrase around it instead:
  PWU's `PWU_CouldNotStartReservationConflict` says `{1} x{2} 예약에
  실패했습니다` rather than `x{2}(을)를 예약하지 못했습니다`.
- A particle after a *fixed* Korean noun should be written literally
  (`기본 기준(평범)과`), not as a paren pair — there is nothing to resolve.

| English | Use | Never | Why |
|---|---|---|---|
| bladelink / persona weapon | 결속 무기 (label prefix 결속) | 블레이드링크, 페르소나 무기 | Royalty `MeleeWeapon_*Bladelink.label`, `BladelinkEquipWarning*` |
| persona (the onboard mind) | 자아 (무기의 자아) | 인격, 페르소나 | Royalty bladelink descs, `NoPain`/`SpeedBoost` descs |
| AI persona core | 인공자아 핵 | 인공지능 코어, 페르소나 핵 | Core `AIPersonaCore.label` |
| trait (persona weapon) | 무기 특성 standalone; 특성 once context is the weapon | 개성 as a countable item | see the divergence note below |
| monosword / plasmasword / zeushammer | 단분자검 / 플라즈마검 / 제우스망치 | 모노소드 | Royalty weapon labels — ko translates, does not transliterate |
| longsword / mace | 장검 / 철퇴 | | Core labels |
| mechanite(s) | 기계입자 | 나노머신, 메카나이트 | Core, 36/36 occurrences (근섬유질 기계입자, 감각기 기계입자, 부활 기계입자) across 7 files incl. `Hediffs_Local_Infections`, `Luciferium`, `Items_Exotic`. **Corrected 2026-07-28** from 나노머신, which renders the *different* English word "nanomachines" (Royalty's Armorskin/Stoneskin glands); Royalty's monosword desc paraphrases to 나노 기술 and is not a term source. Grounding on Royalty+Biotech alone misses this |
| techprint | 기술청사진 | 기술 도면 | Core `TechprintLabel` |
| fabrication bench | 조립 작업대 | 제작 작업대, 정밀 작업대 | Core `FabricationBench.label` |
| advanced components | 고급 부품 | 고급 부품류, 첨단 부품 | Core `ComponentSpacer.label`; plain components are 부품 |
| plasteel / uranium / gold | 플라스틸 / 우라늄 / 금 | | Core labels |
| quality (noun) / tiers | 품질 / 끔찍·빈약·평범·상급·완벽·걸작·전설적 | | Core `Quality`, `QualityCategory_*` |
| "{0} quality or better" | `{0} 품질 이상` | | Core `NormalQualityOrBetter`=평범 품질 이상 |
| ultratech (tech level) | 미래 | 초과학, 울트라텍 | Core `TechLevel_Ultra` |
| Crafting (the skill) | 제작 | 수공예, 공예 | Core `Crafting.label` |
| bill (work bill) | 계획서 (add-bill menu: 계획서 추가) | 작업 지시, 청구서 | Core `TabBills`, `AddBill` |
| colonist | 정착민 | 식민지 주민 | Core `Colonist` |
| research project | 연구 프로젝트 | | Core `NeedResearchBenchDesc` |
| Cancel / Reset / Confirm / Randomize | 취소 / 초기화 / 확인 / 섞기 | 무작위 | Core `Cancel`, `Reset`, `Confirm`, `Randomize` |
| Reset to defaults | 기본값 복원 | | Core `RestoreToDefaultSettings` |
| Empire (faction) | 와해된 제국 | 제국 alone | Royalty `Empire.label` |
| freewielder (trait label) | 자유 의지 | 자유 소유자 | Royalty `NeverBond.label` — quote it as '자유 의지' 특성 |
| relic | 유물 | 성물 | Ideology `Relic`, `RelicOf` (the reliquary is 유물함) |
| ideoligion / reform | 사상 / 사상 개혁 | 이념 | Ideology `IdeoligionOf`, `ReformIdeoligion` |
| stopping power / burst count / burst speed | 저지력 / 연발 횟수 / 발사속도 | | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| armor penetration / damage | 관통력 / 피해량 | 방어 관통 | Core `ArmorPenetration`, `Damage` |
| None / EMP stun | 없음 / EMP에 기절함 | | Core `None`, `StunnedByEMP` |
| customize / customization | 개조 | 커스터마이즈, 맞춤 설정, 사용자 정의 | **Coined** — see the note below |
| bladelink customization (this mod's research) | 결속 무기 개조 | | **Coined** from 결속 무기 + 개조 |

Three Korean-specific notes.

**The trait divergence is *inside* Royalty ko, not between DLCs.** Royalty's
`Stat_Thing_PersonaWeaponTrait_Label` is **개성** ("individuality"), but
Royalty's own `BladelinkEquipWarningTraits` — the heading shown when a player is
told what traits a bladelink weapon has — is **무기 특성**, which is also
Odyssey's `Stat_ThingUniqueWeaponTrait_Label`. So the skill's usual rule ("use
Royalty's word, not Odyssey's") cannot decide this one: Royalty ships both. PWU
uses 무기 특성 / 특성 because (a) it is a Royalty word for exactly this list,
(b) 개성 appears only as the info-card *stat row name* and never as a countable
item, and 개성 reads badly across the 20+ strings that count, conflict, hide and
cap traits. Note ko does NOT get the Russian черта/свойство split for free: pawn
personality traits are `<Traits>특성</Traits>` too, so 무기 특성 is the
disambiguating form when there is no surrounding weapon context (PWU's Traits
tab uses it for that reason). A native reviewer may prefer aligning the tab with
the info card's 개성 — flagged in the 2026-07-28 commit; do not silently flip it
either way.

**"Customize" has essentially no vanilla ko anchor.** The only `Customize*` key
in Core/Royalty/Ideology/Odyssey ko is `CustomizeIdeoligion` = 사상 생성
("ideoligion creation"), a paraphrase that does not generalize; 사용자 지정
appears twice as generic software boilerplate. PWU therefore coins **개조**
("remodel/modification"), which is real RimWorld ko vocabulary
(`MedicalOperationsMechanoidsShort` = 개조) and natural for weapons. It was
chosen to stay single-valued across all 158 keys, where the word recurs
constantly and must compose: 자아 개조 (gizmo), 개조 창 (dialog),
개조가 중단되었습니다 (bail messages), 결속 무기 개조 (research), 개조용으로
해금 (discovery). Flagged for native review.

**Landmine that does NOT bite in Korean.** `PWU_UI.xml`'s translator comment
calls the persona-core recipe's research prerequisite "machine persuasion
(vanilla research label)"; the def is `ShipComputerCore`, which ja renders
AIコンピュータコア and zh 飞船电脑核心 — nothing like the English. Korean is the
exception: `ShipComputerCore.label` = **기계 설득**, a literal match for
"machine persuasion". Still resolve the defName through the tar rather than
trusting the hint — the coincidence is language-specific, not a general licence.

**Rows carried in from UMW's ko pass (2026-07-28), independently grounded.** That
pass reached the same josa conclusion from the same decompiled worker, so the
mechanics above are corroborated, not just asserted. What it adds beyond the
table:

- **Core's DamageDef cut/stab labels differ from its HediffDef ones**: DamageDef
  `Cut`=잘림 / `Stab`=찔림, but HediffDef `Cut`=베임 (and `Stab.labelNoun`=찔린
  상처). Point each of a mod's own defs at the right one. Toxic variants take a
  parenthetical: `ScratchToxic`=찢김 (독성), `ToxicBite`=물림 (독성).
- **`첨단` means "cutting-edge", not "tip/point"** — every vanilla ko occurrence
  is 첨단 기술 / 첨단 장치 / 최첨단 금속 검. For a weapon's point use 칼끝
  (Core's own tool label); 끝 for a spear.
- **Korean uses spaces where ja and zh concatenate.** The ko namer composes
  `[weapon_adjective] [weapon_noun]`, so ko trait adjectives may be attributive
  verb forms (가벼운, 저주받은) *or* bare noun modifiers (황금, 신속, 특제).
  Genitive epithets carry their own 의 (죽음의). Do not port ja's "must end in
  の/な/い" rule to ko.
- **Vanilla ko drops `[RECIPIENT_possessive]`** in every combat rulePack — 12
  textual occurrences, all inside EN comments, none in Korean values. Korean
  omits possessive pronouns, so a battle-log pack should drop it rather than
  render 그의.
- **Register split by def type**: `ThoughtDef` stage descriptions are casual
  first-person (`-어`, `-지`, `-거야`; vanilla `이제 거의 깼어.`), battle-log
  rulesStrings end in the nominalized `-함.`/`-임.`, and everything else is
  polite `-습니다.`. Anesthetic's stage labels (혼미함, 안정됨) show the `-됨`
  hediff-stage family.
- **`Reset to defaults` is 기본값 복원** (Core `RestoreToDefaultSettings`), which
  matches the row already in the table — worth noting because ja and ru also
  reuse that vanilla string verbatim for the same button, so it is the
  cross-language default rather than a ko quirk.

### Glossary — German (machine-assisted de landed 2026-07-28; no native review yet)

All rows below were grounded during PWU's own 2026-07-28 de generation — German
had no preseed from a sibling mod. RimWorld's language folder is `German` (tar:
`German (Deutsch).tar`).

**German needs no coinage for either of the two terms ja and zh had to invent.**
Royalty de calls the weapon class **Personawaffe** (`WeaponsMeleeBladelink.label`
= Personawaffen, and `BladelinkAlreadyBondedDialog` says "sich immer nur mit
einer Personawaffe verbinden"), and Core de renders "customize" as **anpassen**
(`CustomizeIdeoligion` = Ideologie anpassen). So both halves of this mod's
central phrase are vanilla vocabulary. Note the label prefix is hyphenated
(Persona-Monoschwert) while the class noun is a solid compound (Personawaffe).

**German is also the one language so far with NO trait divergence.** Royalty's
`Stat_Thing_PersonaWeaponTrait_Label`, Odyssey's
`Stat_ThingUniqueWeaponTrait_Label`, *and* Core's pawn-trait `<Traits>` are all
**Merkmale**. The skill's usual warning (never assume the pawn-trait word applies
to weapon traits) is satisfied here by coincidence, not by exemption — the check
still has to be run, it just comes back the same three times. Do not import the
Russian черта/свойство or Korean 무기 특성/개성 caution into German prose; a
disambiguating "Waffenmerkmal" is only needed if surrounding context is absent
(vanilla itself ships `StatsReport_WeaponTraits` = Waffenmerkmale for that case).

Style rules from the vanilla de data (mandatory):

- **ASCII single quotes** around cited def labels and UI labels — vanilla writes
  `Forschungsprojekt '{0}'` and `'{WEAPON_labelShort}'`. Counted in Core+Royalty
  Keyed: 140 single-quoted placeholders, **zero** German `„…"`. Never use `„ "`,
  `» «`, or curly quotes. ASCII `"` appears ~10 times, so single quotes are the
  house style even where English uses double. Pawn names are not quoted.
- **Hazard from that choice:** `LanguageWorker_German.PostProcessed` runs a regex
  over every finished string that rewrites a trailing `'s` (English genitive) to
  `s` — or to a bare `'` after s/ß/z/x/ce. So a closing ASCII single quote
  immediately followed by a lowercase `s` at a word boundary gets silently
  mangled. Never write `'{0}'s`; keep a space or a non-`s` character after a
  closing quote. Nothing in this mod's de file trips it, and the checker cannot
  see it — it is a runtime rewrite.
- `LanguageWorker_German.PostProcessThingLabelForRelic` truncates a weapon label
  to its bare weapon noun when the weapon becomes a relic, matching `EndsWith`
  against a hardcoded 26-noun list (Horn, Lanze, Pulser, Werfer, Axt, Flinte,
  Bogen, Revolver, Gewehr, Stoßzahn, Stab, Hammer, Schwert, Pistole, Dolch,
  Büchse, Kanone, Granaten, Granate, Keule, Säbel, Messer, Rapier, Klinge, Sense,
  Speer); on no match it falls back to the substring after the last space or
  hyphen. Royalty's own labels land safely (Persona-Monoschwert → Monoschwert →
  *Schwert*; Zeushammer → *Hammer*), but note **Waffe is not on the list** — a
  de label ending in a noun outside those 26 produces a poor relic name.
- **En dash `–`, never em dash `—`** (20 vs 0 in Core Keyed + Royalty). English
  source uses `—`, so every dash must be converted. The `<!-- EN: -->` comments
  keep the English `—` verbatim — only translated values change.
- Ellipsis is ASCII `...` (74 in Core Keyed, `…` zero).
- Descriptions/tooltips end with `.`; labels, buttons and float-menu reasons take
  none. Settings prose addresses the player with informal **du** and imperatives
  (Aktiviere dies, Deaktiviere es) — vanilla de is consistently du, never Sie.
- `RecipeDef.label` is `X herstellen` with **no article** (Core
  `Make_ComponentSpacer` = Hightech-Bauteil herstellen); `jobString` is
  third-person `Stellt X her.` **with** a period; `JobDef.reportString` is
  likewise third-person lowercase with a period (`passt Waffe an.` — cf. Core
  `ApplyTechprint` = wendet TargetB an.). Note de job strings differ from ja/ko
  here: they take terminal periods.
- Research labels are lowercase noun phrases (Hightech-Fabrikation, mehrläufige
  Waffen, lange Klingen) or **verb-final phrases** (`ShipComputerCore` =
  Maschinenpersona überreden, `Brewing` = Bier brauen).

**The German landmine is grammatical CASE, not gender** (decompile-verified —
`Verse.GrammarResolverSimple`, `Verse.LanguageWorker_German`,
`Verse.LanguageWordInfo`). The two are worth separating, because the obvious
guess is wrong in both directions:

- `"key".Translate(args)` routes through `TaggedString.Formatted` →
  **`GrammarResolverSimple`**, not the full rulepack `GrammarResolver`. A plain
  `string` arg becomes a `NamedArgument` with a null label and lands in that
  class's `obj is string` branch.
- That branch **does** support, on a plain string:
  `{0_gender ? masc : fem : neut}`, `{0_definite}`, `{0_indefinite}`,
  `{0_plural}`, `{0_pronoun}`, `{0_possessive}`, `{0_objective}`. Gender comes
  from `LoadedLanguage.ResolveGender` → `LanguageWordInfo`, i.e. it is looked up
  **from the word itself**, so no arg metadata is required. German ships those
  tables: `WordInfo/Gender/{Male,Female,Neuter,Other}.txt`, one lowercase noun
  per line (~2450 entries in Core). So gender IS available here.
- **`lookup` is not a function in `GrammarResolverSimple` at all.** So
  `{lookup: {0}; decline; N}` — the only mechanism that yields *case* forms,
  from the 2457-row `WordInfo/decline.txt` — silently fails on this path; it is
  reachable only from the rulepack/letter resolver. And
  `LanguageWorker_German.WithDefiniteArticle`/`WithIndefiniteArticle` return
  **nominative only** (der/die/das, ein/eine). Case is therefore unfixable.
- `ResolveGender`'s `defaultGender` is **Male**, so any noun absent from the
  Gender lists — every mod-coined label — silently resolves masculine.

Net rule: a **nominative** slot holding a **vanilla** noun may legitimately use
`{0_definite}` / `{0_gender ? … : … : …}`. Anything oblique, or any mod-coined
noun, must be restructured so no article or adjective has to agree. That is why
this mod phrases around it everywhere:

- `PWU_RequiresWorkbench` ("requires a {0}") drops the article: `erfordert {0}`.
  There is no safe way to write "einen/eine/ein {0}".
- `PWU_RequiresMinimumQuality` becomes `erfordert Qualität {0} oder besser`,
  putting the noun *before* the injected label, because German quality labels are
  adjectives that would have to inflect (vanilla's own
  `NormalQualityOrBetter` = "normale Qualität oder besser" is pre-inflected and
  cannot be templated).
- Bail/error messages use `Anpassung von '{0}' unterbrochen: …` — the `von`
  +quoted-label frame is case-proof and is the German counterpart of the
  cross-language "quote the injected label" rule.
- The one place an inflected article IS used is `am {1}` in the four
  `PWU_Enable*RecipeDesc` strings. That is deliberate and safe: `{1}` is
  hard-bound to `PWU_ThingDefOf.FabricationBench.label` in `PWU_Mod.cs`, which
  de renders **Fabrikationstisch** (masculine), so the dative contraction is
  always correct. Do not "generalize" it, and do not copy the pattern to a
  placeholder whose def isn't pinned.
- Fixed German nouns inflect normally (eine meisterliche Waffe) — only *injected*
  values need the workaround.

| English | Use | Never | Why |
|---|---|---|---|
| persona weapon / bladelink weapon | Personawaffe (label prefix Persona-) | Klingenverbindung, Bladelink-Waffe | Royalty `WeaponsMeleeBladelink.label`, `MeleeWeapon_*Bladelink.label` |
| persona (the onboard mind) | die Persona | Persönlichkeit, KI | Royalty bladelink descs, `LetterBladelinkWeaponBonded` |
| customize / customization | anpassen / Anpassung | anwenden, individualisieren, konfigurieren | Core `CustomizeIdeoligion` = Ideologie anpassen |
| trait (weapon and pawn alike) | Merkmal / Merkmale | Eigenschaft, Attribut | Royalty `Stat_Thing_PersonaWeaponTrait_Label`, `BladelinkEquipWarningTraits`, Core `<Traits>` — all three agree |
| bond (noun / verb) | Bindung / binden, gebunden | Verbindung, Bund | Royalty `BladelinkAlreadyBonded*`, `LetterBladelinkWeaponBonded` |
| wielder / bearer | Träger | Anwender, Nutzer | Royalty weapon-trait descs |
| persona core / AI persona core | Personakern | Persona-Kern, KI-Kern | Core `AIPersonaCore.label` |
| techprint | Techplan / Techpläne | Techdruck, Blaupause | Core `TechprintLabel` = Techplan ({PROJECT_label}) |
| fabrication bench | Fabrikationstisch | Fertigungstisch, Werkbank alone | Core `FabricationBench.label` |
| advanced components | Hightech-Bauteile | fortschrittliche Komponenten | Core `ComponentSpacer.label`; plain components are Bauteile |
| monosword / plasmasword / zeushammer | Monoschwert / Plasmaschwert / Zeushammer | | Royalty weapon labels (persona forms prefix Persona-) |
| longsword / mace / knife / spear | Langschwert / Streitkolben / Messer / Speer | | Core labels |
| mechanite | Mechaniten | Mechanite | Royalty monosword desc |
| plasteel / uranium / gold | Plastahl / Uran / Gold | Plastahl→*Plasteel* | Core labels — Plastahl is translated, unlike ja/ko |
| quality (noun) / tiers | Qualität / übel·schlecht·normal·gut·exzellent·meisterlich·legendär | | Core `Quality`, `QualityCategory_*` (tiers are lowercase adjectives) |
| ultratech (tech level) | Ultra | Ultratech, Hochtechnologie | Core `TechLevel_Ultra`; "tech level" itself is Techstufe |
| Crafting (the skill) | Handwerk | Herstellung, Basteln | Core `Crafting.label` |
| bill / recipe (both) | Auftrag (add-bill menu: Auftrag hinzufügen) | Rezept, Rechnung | Core `TabBills`, `AddBill`, and every `Stat_Recipe_*_Desc` says Auftrag — de collapses bill and recipe into one word |
| ingredients / hauling | Zutaten / Transport | Bestandteile, Schleppen | Core `Ingredients`, `WorkTagHauling` |
| colonist / research project | Kolonist / Forschungsprojekt | | Core `Colonist`, `NeedResearchBenchDesc` |
| appearance | Erscheinung | Aussehen, Erscheinungsbild | Core `Appearance` |
| Cancel / Reset / Confirm / Randomize | Abbrechen / Zurücksetzen / Bestätigen / Zufällig | Zufällig machen | Core buttons |
| Reset to defaults | Auf Standard zurücksetzen | Standardwerte wiederherstellen | Core `ResetBinding`; `Default` = Standard |
| None | Nichts | Keine, Kein | Core `None` |
| Empire (faction) | zerrüttetes Imperium | Imperium alone | Royalty `Empire.label` |
| freewielder (trait label) | frei schwingend | Freiträger, frei führbar | Royalty `NeverBond.label` — quote it as `'frei schwingend'`; de weapon-trait labels are all lowercase adjectives/participles |
| relic | Reliquie | Relikt | Ideology `Relic`, `RelicOf` (reliquary = Reliquienschrein) |
| ideoligion / reform | Ideologie / Ideologie reformieren | Ideoligion, Weltanschauung | Ideology `IdeoligionOf`, `ReformIdeoligion` — de uses the plain word, no portmanteau |
| stopping power / burst count / burst speed | Mannstoppwirkung / Schüsse pro Feuerstoß / Feuerrate | Durchschlagskraft | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| armor penetration / damage / accuracy | Rüstungsdurchdringung / Schaden / Genauigkeit | Panzerdurchdringung, Treffsicherheit | Core `ArmorPenetration`, `Damage`, `Accuracy` |
| cut / stab (DamageDef) | Schnitt / Stich | Schnittwunde, Stichwunde (those are hediffs) | Core DamageDefs |
| EMP stun | Betäubt durch EMP | | Core `StunnedByEMP` |
| gizmo button | Befehlsknopf | Gizmo | no vanilla de `Gizmo*` key exists; Befehlsknopf is the descriptive form |
| hostiles / quest | Feinde / Quest | Gegner (that's `Enemies`) | Core `Hostiles`, `Quest` |
| bladelink customization (this mod's research) | Personawaffen anpassen | | composed from two grounded terms — see the note below |

Three German-specific notes.

**`PWU_BladelinkCustomization.label` is composed, not coined.** Personawaffen +
anpassen are both vanilla de, and the verb-final shape mirrors
`ShipComputerCore.label` = "Maschinenpersona überreden" — the one vanilla
research project that is also about reprogramming a persona, and therefore the
closest available style anchor. It is longer than the median de research label,
so a native reviewer may prefer a nominal "Personawaffen-Anpassung"; flagged in
the 2026-07-28 commit, do not silently flip it either way. Because the label is a
verb phrase, every string that injects it quotes it (`Forschung '{0}'`) — that
quoting is load-bearing for readability, not decoration.

**Landmine — the persona-core recipe's research prerequisite.** `PWU_UI.xml`'s
translator comment calls it "machine persuasion (vanilla research label)". The
def is `ShipComputerCore`, and de renders it **Maschinenpersona überreden** —
close enough to the English hint to be seductive, but still not a literal match
("Maschinenpersona", not "Maschine"). ja gives AIコンピュータコア and zh
飞船电脑核心, nothing like the English, so the hint is only *approximately*
reliable in de. Resolve the defName through the tar every time. (No def named
`MachinePersuasion` exists, so grepping the hint text finds nothing.)

**Kill tracker vs kill memory.** English uses "kill tracker" as the toggle label
and "kill memory" in prose; de collapses both to **Tötungsgedächtnis** so the
Memory tab reads consistently against `PWU_TabMemory` = Gedächtnis. The haul
planner modes are mod-coined with no vanilla anchor: **Sequenziell / Sammelgang /
Gründlich**.

### Glossary — Spanish (machine-assisted es landed 2026-07-29; no native review yet)

All rows below were grounded during PWU's own 2026-07-29 es generation — Spanish
had no preseed from a sibling mod. RimWorld ships **two** Spanish variants:
`Spanish (Español(Castellano)).tar` → folder `Spanish`, and
`SpanishLatin (Español(Latinoamérica)).tar` → folder `SpanishLatin`. This mod
targets Castellano, so the folder is `Spanish`. SpanishLatin is a *separate*
language with its own tar — never assume a term carries across; re-ground if
that variant is ever added.

**"Persona" is a false friend and vanilla es never uses it for the weapon mind.**
Spanish *persona* means "person", so "arma persona" reads as "person weapon".
Vanilla es systematically renders the onboard persona as **la IA** and the weapon
class as **arma vinculada** ("bonded weapon"):
`MeleeWeapon_MonoSwordBladelink.label` = mono-espada **vinculada**,
`WeaponsMeleeBladelink.label` = armas de filo vinculadas,
`BladelinkEquipWarning` = "la **IA** del arma", the bladelink ThingDef
descriptions = "una **IA** incorporada" / "el arma inteligente", and
`AIPersonaCore.label` = **núcleo de IA** (not "núcleo de persona"). PWU follows
this throughout: the gizmo is "Personalizar IA", not "Personalizar persona". This
is the single most important es rule — a literal "persona" anywhere in a value is
a bug. (It legitimately appears in EN comments, which quote the English verbatim.)

**Spanish needs no coinage for the two terms ja and zh had to invent.** Both
halves of this mod's central phrase are vanilla es: `CustomizeIdeoligion` =
"**Personalizar** ideoligión" gives customize = personalizar / personalización,
and *vinculada* covers bladelink. So `PWU_BladelinkCustomization.label` is
composed, not coined: **personalización de armas vinculadas**. Length is in line
with vanilla es research labels (televisión de pantalla plana, sarcófagos de
criptosueño, escáner de largo alcance). A native reviewer might prefer a terser
"personalización de armas con IA"; flagged in the 2026-07-29 commit — do not
silently flip it either way.

Style rules from the vanilla es data (mandatory):

- **ASCII double quotes** `"{0}"` around cited def labels and UI labels. Counted
  in Core+Royalty Keyed: 40 `"{`, **zero** `«` and **zero** `“`. Never use
  `«…»` or curly quotes — es vanilla ships neither. ASCII single quotes appear
  11 times; double is the house style. Pawn names are not quoted.
- **Inverted `¿` and `¡` are required** (51 and 50 in Core Keyed). `¿Continuar?`
  is vanilla's own exact phrasing (`ChangedTextureCompressionRestart`), which is
  what `PWU_BondSeveredWarning` reuses.
- **No em dash AND no en dash** — 0 `—` and 0 `–` in Core Keyed, 3 and 2 across
  the whole Core+Royalty corpus, i.e. effectively unused. English source uses
  `—`, so every dash must be *restructured away*, not swapped for `–` the way
  German does. PWU converts them to a colon (`PWU_IntegrateVpweCustomizationDesc`)
  and to parentheses (`PWU_Make_AIPersonaCore.description`).
- Ellipsis is ASCII `...` (75 in Core Keyed, `…` zero).
- Descriptions/tooltips end with `.`; labels, buttons and float-menu reasons take
  none. Settings prose uses informal **tú** imperatives (Actívalo, Desactívalo,
  Ponlo a 0) — vanilla es is consistently tú, never usted.
- `RecipeDef.label` is lowercase infinitive with **no article** (Core
  `Make_Gun_AssaultRifle` = "fabricar fusil de asalto"); `description` is
  third-person present **with** article and period ("Fabrica un fusil de
  asalto."); `jobString` is a capitalized gerund **with** period ("Fabricando
  fusil de asalto."). `JobDef.reportString` is a lowercase gerund with period
  ("aplicando tecnoplanos", "poniendo huevo."). Like German — and unlike ja/ko —
  es job strings take terminal periods.
- Research labels are lowercase noun phrases (fabricación avanzada, biónica,
  metabolismo artificial).

**The Spanish landmine is GENDER agreement** (decompile-verified —
`Verse.LanguageWorker_Spanish`, `Verse.LanguageWordInfo`,
`Verse.GrammarResolverSimple`). Unlike German, gender resolution genuinely works
on the `.Translate()` path — and unlike German, that still doesn't save you here:

- `LanguageWorker_Spanish` overrides only `WithIndefiniteArticle` (un/una/unos/
  unas), `WithDefiniteArticle` (el/la/los/las), `OrdinalNumber` and `Pluralize`.
  There is **no `PostProcessed` override**, so es has no German-style silent
  string rewrite to trip over.
- Vanilla es *does* use the resolver freely: 20 `{0_gender ? o : a}`, 18
  `{0_indefinite}`, 9 `{0_definite}` in Core+Royalty Keyed. Gender tables are
  well populated (Female 2771 / Male 1771 / Neuter 163 lines).
- **But the specific nouns PWU injects are absent from those tables.** Checked:
  `arma`, `calidad`, `característica`, `investigación`, `núcleo`, `tecnoplano`,
  `reliquia`, and every weapon label (`mono-espada`, `espada de plasma`,
  `martillo de Zeus`) all return no match. `ResolveGender`'s `defaultGender` is
  **Male**, so each would silently resolve masculine — wrong for `calidad`,
  `característica` and `investigación`. Only `mesa de ensamblaje` is present
  (Female).
- `WithDefiniteArticle` is plain `"el "`/`"la "` concatenation: it produces **no
  contractions**, so `de {0_definite}` renders "de el ..." instead of "del", and
  `a {0_definite}` gives "a el" instead of "al". It also doesn't handle the
  "el agua" class (feminine nouns taking *el*).

Net rule: same as German — restructure so no article or adjective has to agree
with an injected value. Worked rewrites in this mod:

- `PWU_RequiresWorkbench` ("requires a {0}") drops the article: `requiere {0}`.
  Necessary, not stylistic: `{0}` comes from `ResolveWorkbenchLabel`, which
  returns an arbitrary member of a VEF-expandable bench set, so the noun is
  genuinely unknown at write time.
- `PWU_RequiresMinimumQuality` quotes the label: `requiere calidad "{0}" o
  mejor`. Quality labels are **adjectives**, and vanilla es is itself
  gender-inconsistent about them (`QualityCategory_Good` = bueno *masculine*,
  `QualityCategory_Legendary` = legendaria *feminine*), so no unquoted form
  agrees with feminine *calidad* for all seven tiers. Vanilla's own
  `NormalQualityOrBetter` = "calidad normal o mejor" is pre-inflected and cannot
  be templated. Quoting turns the mismatch into a cited value. Same treatment in
  `PWU_CostTableNotApplicableDesc` (`fijado en "{0}"`).
- Bail/error messages use `Personalización de {0} interrumpida: …` — `de` + no
  article is case- and gender-proof, the es counterpart of German's `von`-frame.
- After a **colon** no quotes are needed (`Personalizar IA: {0}`,
  `Calidad mínima del arma: {0}`) — matches vanilla's
  `LetterTechprintAppliedLabel` = "Tecnoplano aplicado: {PROJECT_label}".
- The one place an inflected article IS used is `en la {1}` in the four
  `PWU_Enable*RecipeDesc` strings. Deliberate and safe: `{1}` is hard-bound to
  `PWU_ThingDefOf.FabricationBench.label` in `PWU_Mod.cs`, which es renders
  **mesa de ensamblaje** (feminine, and the one injected noun actually present in
  the Female table). Do not generalize it to an unpinned placeholder. This is the
  exact analogue of German's `am {1}`.
- Fixed Spanish nouns inflect normally (una espada larga, un arma de obra
  maestra) — only *injected* values need the workaround.

| English | Use | Never | Why |
|---|---|---|---|
| persona (the onboard mind) | la IA | la persona, el personaje | `BladelinkEquipWarning` = "la IA del arma"; *persona* means "person" in Spanish |
| persona weapon / bladelink weapon | arma vinculada (label suffix vinculada; category: armas de filo vinculadas) | arma persona, arma de enlace | Royalty `MeleeWeapon_*Bladelink.label`, Core `WeaponsMeleeBladelink.label` |
| trait (persona weapon) | característica | rasgo | Royalty `Stat_Thing_PersonaWeaponTrait_Label` **and** `BladelinkEquipWarningTraits`; *rasgo* is Odyssey's unique-weapon word AND Core's pawn-trait word |
| customize / customization | personalizar / personalización | adaptar, configurar, modificar | Core `CustomizeIdeoligion` = "Personalizar ideoligión" |
| bond (noun / verb) | vínculo / vincularse, vinculada | enlace, unión | Core `BladelinkAlreadyBonded*`, `LetterBladelinkWeaponBonded` |
| AI persona core | núcleo de IA | núcleo de persona, núcleo de personalidad | Core `AIPersonaCore.label` |
| wielder / bearer | portador | usuario, empuñador | Royalty WeaponTraitDef descs, consistently |
| techprint | tecnoplano | plano técnico, anteproyecto | Core `TechprintLabel` = "tecnoplano ({PROJECT_label})" |
| fabrication bench | mesa de ensamblaje | mesa de fabricación (that's the *blueprint* label), banco de trabajo | Core `FabricationBench.label` — note the Blueprint/Frame defs disagree with the ThingDef; the ThingDef label is what PWU injects |
| workbench (generic) | mesa de trabajo | puesto de trabajo, banco | Core bench `description`s, ~6 uses |
| advanced components | componentes avanzados | componentes de alta tecnología | Core `ComponentSpacer.label`; plain components are componentes |
| monosword / plasmasword / zeushammer | mono-espada / espada de plasma / martillo de Zeus | monoespada, plasmaespada, zeusmartillo | Royalty weapon labels — es translates and hyphenates mono-espada |
| longsword / handle / edge / point | espada larga / empuñadura / filo / punta | mandoble | Core + Royalty `tools.*.label` |
| mechanite | mecanita(s) | nanomáquina, mecanito | Core `FibrousMechanites`, Royalty monosword desc. Vanilla es is gender-inconsistent here ("las mecanitas" but also "mecanitas fibrosos"); PWU uses feminine, matching the dominant Royalty desc |
| plasteel / uranium / gold | plastiacero / uranio / oro | plasacero, plastilita | Core labels — **plastiacero**, counterintuitive, always check |
| quality (noun) / tiers | calidad / horrible·mediocre·normal·bueno·excelente·obra maestra·legendaria | | Core `Quality`, `QualityCategory_*` — tiers are lowercase and **gender-inconsistent** (see the landmine above) |
| "{0} quality or better" | `calidad "{0}" o mejor` | `calidad {0} o mejor` | quoting is required; `NormalQualityOrBetter` = "calidad normal o mejor" is pre-inflected feminine |
| ultratech (tech level) | ultra | ultratecnología, supertecnología | Core `TechLevel_Ultra` |
| Crafting (the skill) | Fabricación | artesanía, manufactura | Core `Crafting.label` |
| bill (work bill) | proyecto (add-bill menu: Añadir proyecto) | pedido, factura, encargo | Core `TabBills` = Proyectos, `AddBill` = Añadir proyecto. **Collides with "research project" (proyecto de investigación)** — keep the qualifier whenever both could be meant |
| recipe | receta | | Core `Stat_Recipe_*` |
| pawn | personaje | peón, muñeco | Core `Stat_Recipe_WorkSpeedStat_Desc` = "característica del personaje" |
| colonist / research project | colono / proyecto de investigación | colonizador | Core `Colonist`, `NeedResearchBenchDesc` |
| Cancel / Reset / Confirm / Randomize | Cancelar / Restablecer / Confirmar / Aleatorizar | Reiniciar, Al azar | Core buttons |
| Reset to defaults | Restablecer valores por defecto | Valores predeterminados | Core `RestoreToDefaultSettings` = Restablecer, `Default` = Por defecto |
| None / Warning / Appearance | Ninguno / Advertencia / Apariencia | Nada, Aviso, Aspecto | Core `None`, `Warning`, `Appearance` |
| Empire (faction) | imperio destrozado | imperio alone | Royalty `Empire.label` |
| Empire traders | comerciantes imperiales | comerciantes del imperio | Royalty `Orbital_Empire.label` = comerciante imperial |
| freewielder (trait label) | liberal | libre, sin vínculo | Royalty `NeverBond.label` — quote it as `"liberal"` when naming it |
| relic / reliquary | reliquia / relicario | reliquiario, vestigio | Ideology `Relic`, `RelicOf`, `Reliquary.label` |
| ideoligion / reform | ideoligión / reformar ideoligión | ideología | Ideology `IdeoligionOf`, `ReformIdeoligion` — es keeps the portmanteau, like Russian and unlike ja/zh/de |
| stopping power / burst count / burst speed | Potencia de parada / Tiros por ráfaga / Cadencia de tiro | Poder de parada | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| armor penetration / damage / accuracy | Penetración de blindaje / Daño / Precisión | Penetración de armadura | Core `ArmorPenetration`, `Damage`, `Accuracy` |
| EMP / EMP stun | PEM / Aturdido por PEM | EMP | Core `StunnedByEMP`, Royalty zeushammer desc — es localizes the acronym |
| quest / hostiles | misión / hostiles | búsqueda | Ideology `RelicTip`, Core `Hostiles` |
| unreachable / no power | inalcanzable / sin energía | inaccesible (reserve for "not accessible") | Core `CannotReach`, `NoPower` |
| hauling / stack | transporte / montón | acarreo, pila | Core `WorkTagHauling` |
| rename | renombrar | cambiar el nombre | Core `Rename` |
| "{0} days ago" | `hace {0} días` | | Core `AwokeDaysAgo` = "Despertó hace {0} días" |
| gizmo button | botón de comando | gizmo, artilugio | no vanilla es `Gizmo*` key exists; `Command*Desc` establishes "comando" |

Two further Spanish notes.

**Landmine — the persona-core recipe's research prerequisite.** `PWU_UI.xml`'s
translator comment calls it "machine persuasion (vanilla research label)". The
def is `ShipComputerCore`, and es renders it **persuasión de IA** — "AI
persuasion", not "machine persuasion". Close enough to the English hint to be
seductive, still not a literal match, and the noun is *IA* rather than *máquina*,
which matters because es reuses IA for the weapon persona. It reaches the player
through `{2}` at runtime so nothing is translated there, but resolve the defName
through the tar every time rather than trusting the hint. (No def named
`MachinePersuasion` exists, so grepping the hint text finds nothing.)

**Kill tracker vs kill memory, and the haul planner.** English's "kill tracker"
(toggle label) and "kill memory" (prose) split into **registro de muertes** and
**memoria de muertes** respectively — es keeps the distinction rather than
collapsing it the way German does, because both read naturally against
`PWU_TabMemory` = Memoria. Haul planner modes are mod-coined with no vanilla
anchor: **Secuencial / Barrido / Exhaustivo**.

### Glossary — French (machine-assisted fr landed 2026-07-29; no native review yet)

All rows below were grounded during PWU's own 2026-07-29 fr generation — French
had no preseed from a sibling mod. RimWorld's language folder is `French` (tar:
`French (Français).tar`).

**French needs no coinage for either central term, but it does need a choice.**
Vanilla fr renders "persona weapon" four different ways and you have to pick:
`WeaponsMeleeBladelink.label` = **armes intelligentes** (the official *category*
name), the individual labels use the adjective **conscient(e)**
(`MeleeWeapon_MonoSwordBladelink.label` = "épée mono-moléculaire consciente"),
`BladelinkAlreadyBondedDialog` says "arme intelligente", and
`LetterBladelinkWeaponBondedLabel` keeps the English — "Lien **Bladelink** :
{PAWN_labelShort}" (the only value in the whole corpus that does). PWU uses
**arme intelligente** for the class, because it is the ThingCategoryDef label and
therefore the one term the game itself presents as the category's name. For the
onboard mind, **la conscience** is dominant (28 hits; every bladelink ThingDef
description says "Cette arme a une conscience qui ne peut se lier qu'à une seule
et unique personne", and `BladelinkEquipWarning` says "la conscience de l'arme").
Note `AIPersonaCore.label` = "noyau IA de **personnalité**" and
`NeverBond.description` says "La **personnalité** de cette arme" — so
*personnalité* is vanilla too, and a native reviewer may prefer it. PWU keeps
conscience for the mind and reserves personnalité for the core's fixed label;
flagged in the 2026-07-29 commit, do not silently flip it either way.
"Customize" is **personnaliser / personnalisation** (Ideology
`CustomizeIdeoligion` = "Personnalisez votre idéoligion").

**French diverges on trait — but in the opposite direction from every other
language so far.** Royalty's `Stat_Thing_PersonaWeaponTrait_Label`, Odyssey's
`Stat_ThingUniqueWeaponTrait_Label` and Core's `WeaponTraits` all say plain
**Traits** (`WeaponTraits` = "Traits d'arme"), while Core's *pawn* trait header
`<Traits>` is **"Éléments marquants :"**. So the bare word "trait" is the
weapon word here and the pawn word is the special one — the reverse of Korean
(개성 vs 무기 특성) and Russian (черта vs свойство). Royalty ships one outlier:
`BladelinkEquipWarningTraits` = "L'arme possède les **caractéristiques**
suivantes"; it is a paraphrase in running prose, not the term. Use **trait**.

Style rules from the vanilla fr data (mandatory):

- **A space before `:` `?` `!` — and it is a plain ASCII space, not a
  non-breaking one.** Counted in Core Keyed: 593 ` :`, 183 ` ?`, 55 ` !`, versus
  3 U+00A0 and 0 U+202F in the entire file set. Typographically French wants
  NBSP; vanilla does not use it, so neither do we.
- **Semicolons are effectively unused** (1 with space, 1 without, in Core Keyed).
  English source uses `;` in four settings strings — split them into two
  sentences rather than reproducing the semicolon.
- **No em dash and no en dash** (0 `—` and 2 `–` in Core Keyed; 5 and 9 across
  Core+Royalty). Like Spanish and unlike German, dashes must be *restructured
  away*, not swapped. PWU converts them to a colon
  (`PWU_IntegrateVpweCustomizationDesc`, `PWU_EnablePersonaCoreRecipeDesc`) and
  to parentheses (`PWU_Make_AIPersonaCore.description`).
- Ellipsis is ASCII `...` (56 in Core Keyed vs 18 `…`).
- **Two quote systems, and they are not interchangeable.** ASCII double quotes
  `"{0}"` for *injected* values (24 in Core Keyed — quests, projects, bills,
  stats: `La quête "{0}" expirera dans {1}`); ASCII single `'{0}'` also occurs
  (10, mostly research labels). Guillemets `« … »` **with inner spaces** are
  reserved for naming a *fixed* UI element the player must go click (« Secourir »,
  « Lit médical », « Options »). PWU follows both: `"{0}"` everywhere a
  placeholder is cited, `« Masquer les traits négatifs »` and « Personnaliser »
  where the English quotes one of the mod's own labels. Curly `“ ”` never
  appears — 0 occurrences. Pawn names are not quoted.
- **Register is vous, not tu** — 262 `vous` / 171 `votre|vos` against 3 `tu` and
  **0** `ton|ta|tes` in Core Keyed. This is the reverse of German (du) and
  Spanish (tú); settings prose uses vouvoiement imperatives (Activez ceci,
  Désactivez, Réglez ceci sur 0).
- Descriptions/tooltips end with `.`; labels, buttons and float-menu reasons take
  none.
- `RecipeDef.label` is a lowercase infinitive **with** the article (Core
  `Make_Gun_AssaultRifle` = "fabriquer un fusil d'assaut",
  `Make_ComponentSpacer` = "fabriquer un composant avancé") — fr keeps the
  article where es and de drop it. `description` is an infinitive with a period
  ("Fabriquer un composant avancé." — 3 of 4 sampled Core recipes; `Make_Kibble`
  is the third-person outlier). `jobString` is third-person **with** a period
  ("Fabrique un composant avancé."), and `JobDef.reportString` is likewise
  third-person lowercase with a period ("construit un bonhomme de neige.").
- Research labels are lowercase noun phrases (assemblage de composant, usinage,
  longues lames, brassage de la bière).

**The French landmine is `LanguageWorker_French.PostProcessed`, which silently
rewrites five patterns in every finished string** (decompile-verified). Unlike
German's single genitive regex, these fire constantly — and most of the time
they *help*, which is what makes the two that bite so easy to miss:

- `ElisionE`: whole-word `ce|de|je|le|me|ne|se|te|que|quoique|lorsque` + space +
  vowel/`h` → apostrophe. `ElisionLa`: `la ` + vowel → `l'`. `ElisionSi`:
  `si il(s)` → `s'il(s)`. All three are *correct* French, so **writing
  `de {0}` / `la {0}` is self-repairing when the injected value starts with a
  vowel** — "de or" becomes "d'or" at runtime. This is the opposite of the
  German/Spanish situation and worth exploiting rather than avoiding.
- **Trap 1 — enclitic pronouns.** The `\b` in `ElisionLa`/`ElisionE` matches
  after a hyphen, so an imperative like `Convertissez-la ensuite` becomes
  **`Convertissez-l'ensuite`**, and `Convertissez-le ensuite` the same. Never
  write `-la`/`-le` (or `-ce`, `-te`, `-me`) immediately before a vowel-initial
  word. PWU's three weapon recipes originally said "Convertissez-la ensuite…"
  and were rewritten to "**Il faut ensuite la convertir…**" for exactly this
  reason (`la c` is safe, and it avoids `à le` below).
- **Trap 2 — `de le` is mangled, not contracted.** `DeLe` is
  `\b(d)e l(es?)\b` → `$1$2`, so "de les" correctly yields "des" but **"de le"
  yields "de", not "du"**. Write `du` directly; never let `de le` reach the
  worker.
- **Trap 3 — `à le`/`à les` are consumed.** `ALe` rewrites them to `au`/`aux`,
  which is right for an article but destroys a pronoun: `à le convertir` becomes
  **`au convertir`**. That is the second reason PWU's recipe descriptions use
  "Il faut ensuite le convertir" rather than "Il reste ensuite à le convertir".
- `WithDefiniteArticle` does handle vowel-initial values (`l'` + str) and
  `WithIndefiniteArticle` gives un/une/des — but **gender still comes from
  `LanguageWordInfo`, and none of the nouns PWU injects are in the fr tables.**
  Checked against `Core/WordInfo/Gender/`: `arme`, `qualité`, `trait`,
  `caractéristique`, `recherche`, `relique`, `épée`, `marteau`, `mémoire` all
  miss; only `noyau`, `schéma`, `atelier`, `composant` are present (all Male).
  `ResolveGender` defaults to Male, so `{0_definite}` on a weapon label is a
  coin flip. Same net rule as German and Spanish: restructure so no article or
  adjective has to agree with an injected value.

Worked rewrites in this mod:

- `PWU_RequiresWorkbench` ("requires a {0}") drops the article: `nécessite {0}`.
  `{0}` comes from `ResolveWorkbenchLabel` over a VEF-expandable bench set, so
  the noun is genuinely unknown. Vanilla sanctions the bare form —
  `NeedResearchBenchDesc` = "Ce projet nécessite que vous construisiez {1}."
- `PWU_RequiresMinimumQuality` quotes the label: `nécessite la qualité "{0}" ou
  mieux`. Quality tiers are adjectives (horrible, médiocre, normal, bon,
  excellent, **merveille** — a feminine *noun* — légendaire), so no unquoted form
  agrees with feminine *qualité* across all seven. Vanilla's own
  `NormalQualityOrBetter` = "qualité normale ou mieux" is pre-inflected and
  cannot be templated. Same treatment in `PWU_CostTableNotApplicableDesc`
  (`réglée sur "{0}"`).
- Bail/error messages use `Personnalisation de "{0}" interrompue : …` — quoting
  blocks the elision *and* removes the need for an article, the fr counterpart of
  German's `von`-frame and Spanish's `de`-frame.
- After a **colon** no quotes are needed (`Personnaliser la conscience : {0}`,
  `Qualité minimale de l'arme : {0}`) — matches vanilla `ResearchFinished` =
  "Recherche terminée : {0}".
- The one place an elided article IS hard-coded is `à l'{1}` in the four
  `PWU_Enable*RecipeDesc` strings. Deliberate and safe: `{1}` is bound to
  `PWU_ThingDefOf.FabricationBench.label` in `PWU_Mod.cs`, which fr renders
  **atelier de fabrication** — vowel-initial, so `l'` is always right. Do not
  generalize it to an unpinned placeholder (a consonant-initial value would give
  "l'plastacier"), and note you cannot write `à le {1}` instead because `ALe`
  would turn it into "au atelier". Exact analogue of German's `am {1}` and
  Spanish's `en la {1}`.
- Fixed French nouns inflect normally (une épée longue, une arme de qualité
  merveille) — only *injected* values need the workaround.

| English | Use | Never | Why |
|---|---|---|---|
| persona weapon / bladelink weapon | arme intelligente (label adjective conscient/consciente) | arme persona, arme à lien de lame | Core `WeaponsMeleeBladelink.label` = armes intelligentes; Royalty `MeleeWeapon_*Bladelink.label` |
| persona (the onboard mind) | la conscience | l'âme, l'esprit | Royalty bladelink descs, `BladelinkEquipWarning` = "la conscience de l'arme" |
| trait (weapon) | trait | caractéristique, éléments marquants (that's the *pawn* word) | Royalty `Stat_Thing_PersonaWeaponTrait_Label`, Core `WeaponTraits` — see the divergence note above |
| customize / customization | personnaliser / personnalisation | adapter, configurer, modifier | Ideology `CustomizeIdeoligion` |
| bond (noun / verb) | lien / se lier, lié | attache, liaison | Royalty `LetterBladelinkWeaponBonded`, `BladelinkAlreadyBonded*`, Core `BondedTo` = "Lié à" |
| AI persona core | noyau IA de personnalité | noyau de conscience, cœur IA | Core `AIPersonaCore.label` |
| wielder / bearer | porteur | manieur, utilisateur | Royalty bladelink descs ("lié à un porteur") |
| techprint | schéma technique | plan technique, tirage tech | Core `TechprintLabel` = "schéma technique ({PROJECT_label})" |
| fabrication bench | atelier de fabrication | table de fabrication, établi de fabrication | Core `FabricationBench.label` |
| workbench (generic) | établi | plan de travail, poste de travail | Core bench labels — établi de boucher, établi d'assemblage, établi de sculpture |
| advanced components | composants avancés | composants de pointe | Core `ComponentSpacer.label`; plain components are composants |
| monosword / plasmasword / zeushammer | épée mono-moléculaire / épée plasmique / marteau de Zeus | monoépée, épée à plasma | Royalty weapon labels (persona forms append conscient/consciente) |
| longsword / warhammer / mace | épée longue / marteau de guerre / masse | | Core labels |
| handle / hilt / edge / point | poignée (marteau: manche) / tranchant / pointe | manche for a sword | Royalty `MeleeWeapon_*Bladelink.tools.*.label` |
| mechanite | mécanites | nanomachines, mécanites (sg. in prose) | Core + Royalty, 47 occurrences |
| plasteel / uranium / gold | plastacier / uranium / or | plastier, plasteel | Core labels — **plastacier**, counterintuitive, always check |
| quality (noun) / tiers | qualité / horrible·médiocre·normal·bon·excellent·merveille·légendaire | | Core `Quality`, `QualityCategory_*` — note *merveille* is a noun among adjectives |
| "{0} quality or better" | `la qualité "{0}" ou mieux` | `qualité {0} ou mieux` | quoting is required; `NormalQualityOrBetter` = "qualité normale ou mieux" is pre-inflected |
| ultratech (tech level) | ultra | ultratechnologie, supertech | Core `TechLevel_Ultra` |
| Crafting (the skill) | artisanat | fabrication, façonnage | Core `Crafting.label` |
| bill (work bill) | tâche (add-bill menu: Ajouter une tâche) | facture, commande, ordre | Core `TabBills` = Tâches, `AddBill` = Ajouter une tâche |
| recipe | recette | | Core `Stat_Recipe_*` |
| ingredients / hauling | ingrédients / transport | composants, portage | Core `Ingredients`, `WorkTagHauling` |
| pawn / colonist | personnage / colon | pion, colonisateur | Core `StatsReport_CharacterQuality` = "Qualité du personnage", `Colonist` = colon |
| research project / research | projet de recherche / recherche | | Core `NeedResearchBenchDesc`, `Research` |
| Cancel / Reset / Confirm / Randomize | Annuler / Réinitialiser / Confirmer / Aléatoire | Au hasard | Core buttons |
| Reset to defaults | Utiliser les paramètres par défaut | Valeurs par défaut | Core `RestoreToDefaultSettings`; `Default` = Par défaut |
| None / Warning / Appearance | Aucun(e) / Avertissement / Apparence | Néant, Attention, Aspect | Core `None` = Aucune (feminine — PWU's `PWU_RefundNone` uses **Aucun** to agree with *remboursement*), `Warning`, `Appearance` |
| Empire (faction) | empire brisé | empire alone | Royalty `Empire.label` |
| freewielder (trait label) | porteur libre | libre porteur, sans lien | Royalty `NeverBond.label` — quote it as `« porteur libre »` when naming it |
| relic / reliquary | relique / reliquaire | vestige, châsse | Ideology `Relic`, `RelicOf`, `Reliquary.label` |
| ideoligion / reform | idéoligion / réformer l'idéoligion | idéologie | Ideology `IdeoligionOf`, `ReformIdeoligion` — fr keeps the portmanteau, like es and ru |
| stopping power / burst count / burst speed | Puissance d'arrêt / Nombre de tirs par rafale / Cadence de tir | Force d'arrêt | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| armor penetration / damage / accuracy | Pénétration d'armure / Dégâts / Précision | Perforation, Justesse | Core `ArmorPenetration`, `Damage`, `Accuracy` |
| cut / stab (DamageDef) | taillade / blessure par lame | coupure, estocade | Core DamageDefs |
| EMP / EMP stun | IEM / Étourdi par une IEM | EMP | Core `StunnedByEMP`, Royalty zeushammer desc — fr localizes the acronym |
| forbidden / reserved / unreachable / no power | interdit / réservé / inaccessible / pas d'énergie | | Core `ForbiddenLower`, `ReservedBy`, `NoPath`, `NoPower` |
| quest / hostiles | quête / hostiles | | Core `Quest`, `Hostiles` (`Enemies` = ennemis is a different key) |
| rename | renommer | | Core `Rename` |
| "{0} days ago" | `il y a {0} jours` | | Core `AwokeDaysAgo` = "S'est éveillé il y a {0} jours" |
| gizmo button | bouton de commande | gadget, gizmo | no vanilla fr `Gizmo*` key exists; `Command*Desc` establishes "commande" |
| bladelink customization (this mod's research) | personnalisation des armes intelligentes | | composed from two grounded terms — see the note below |

Three further French notes.

**`PWU_BladelinkCustomization.label` is composed, not coined.** personnalisation
and armes intelligentes are both vanilla fr, so no invention was needed. It is
longer than the median fr research label (usinage, longues lames), so a native
reviewer may prefer a terser "personnalisation des consciences"; flagged in the
2026-07-29 commit, do not silently flip it either way.

**Landmine — the persona-core recipe's research prerequisite.** `PWU_UI.xml`'s
translator comment calls it "machine persuasion (vanilla research label)". The
def is `ShipComputerCore`, and fr renders it **noyau central de l'ordinateur de
bord** — nothing like the English, exactly as in ja (AIコンピュータコア) and zh
(飞船电脑核心). Its description does give the sense ("Vous apprend à brider une IA
existante…"), but the label does not. Resolve the defName through the tar every
time. (No def named `MachinePersuasion` exists, so grepping the hint text finds
nothing.)

**Kill tracker vs kill memory, and the haul planner.** English's "kill tracker"
(toggle label) and "kill memory" (prose) split into **registre des victimes** and
**mémoire des victimes** — fr keeps the distinction like Spanish rather than
collapsing it like German, because both read naturally against `PWU_TabMemory` =
Mémoire. Haul planner modes are mod-coined with no vanilla anchor: **Séquentiel /
Balayage / Exhaustif**.

### Glossary — Brazilian Portuguese (machine-assisted pt-BR landed 2026-07-29; no native review yet)

All rows below were grounded during PWU's own 2026-07-29 pt-BR generation — pt-BR
had no preseed from a sibling mod. **RimWorld ships two Portuguese variants and
neither folder name matches the obvious guess:** `PortugueseBrazilian (Português
Brasileiro).tar` → folder **`PortugueseBrazilian`** (NOT "BrazilianPortuguese"),
and `Portuguese (Português).tar` → folder `Portuguese` (European, a *separate*
language with its own tar). This mod targets pt-BR, so the folder is
`PortugueseBrazilian`. Never assume a term carries across the two variants;
re-ground if European Portuguese is ever added.

**pt-BR is the one language so far where "persona" is vanilla vocabulary for the
weapon.** This is the exact opposite of Spanish, where *persona* means "person"
and is a bug. Portuguese has *pessoa* for "person", leaving *persona* free as a
loanword, and vanilla uses it: `WeaponsMeleeBladelink.label` = **armas persona**,
`MeleeWeapon_MonoSwordBladelink.label` = "espada monomolecular **persona**"
(suffix, not prefix), `LetterBladelinkWeaponBondedLabel` = "**Vínculo persona**".
So no coinage is needed for the class noun. For the onboard mind vanilla is split:
**persona** in `BladelinkEquipWarning` ("a persona da arma") and
`NeverBond.description` ("A persona desta arma"), but **personalidade** in the
bladelink ThingDef descriptions and `LetterBladelinkWeaponBonded`. PWU uses
**persona** for the mind, for three reasons: it keeps one word across all 158 keys,
it matches "arma persona", and *personalidade* is already spoken for twice over —
by `AIPersonaCore.label` ("núcleo de personalidade IA") and by the pawn-trait
phrase "traço de personalidade", which would collide badly with this mod's
constant talk of weapon traços. Flagged for native review in the 2026-07-29
commit; do not silently flip it either way.

**pt-BR is a THIRD trait pattern — the mirror image of Spanish.** Royalty's
`Stat_Thing_PersonaWeaponTrait_Label` = **Traços** and Royalty's
`BladelinkEquipWarningTraits` = "os seguintes **traços**", while Odyssey's
`Stat_ThingUniqueWeaponTrait_Label`, Core's `WeaponTraits` and
`StatsReport_WeaponTraits` all say **Características**. Core's *pawn* trait header
`<Traits>` is also **Traços**. So in pt-BR the Royalty word coincides with the pawn
word and Odyssey is the odd one out — whereas in Spanish, Royalty
(características) was the odd one out against Odyssey+pawn (rasgos). Use
**traço/traços**; *características* would be importing UWU's domain word. The
pawn/weapon collision is harmless because the disambiguator is the trailing
qualifier: pawn traits are "traço **de personalidade**" (Core
`BrawlerHasRangedWeaponDesc`), so a bare "traço" in weapon context reads correctly.

Style rules from the vanilla pt-BR data (mandatory):

- **No em dash, no en dash, AND no `…`** — 0 `—`, 0 `–`, 0 `…` in Core Keyed.
  Ellipsis is ASCII `...` (68). English source uses `—`, so every dash must be
  *restructured away*, not swapped for `–` the way German does. PWU converts them
  to a colon (`PWU_IntegrateVpweCustomizationDesc`,
  `PWU_EnablePersonaCoreRecipeDesc`) and to parentheses
  (`PWU_Make_AIPersonaCore.description`).
- **Semicolons are effectively unused** — 0 `; ` in Core Keyed, 1 in Core+Royalty.
  English uses `;` in the four `PWU_Enable*RecipeDesc` strings and in
  `PWU_RestrictTraitsToDiscoveredDesc`; split each into two sentences, exactly as
  French does.
- **No guillemets, no curly quotes, no inverted `¿`/`¡`** — 0 of each. Quoting is
  ASCII and **split by what is being cited**: single `'{0}'` for a research
  project or quest (22 in Core+Royalty; `NeedResearchBenchDesc` = "o projeto de
  pesquisa '{0}'", `TechprintDesc` = "o projeto de pesquisa '{PROJECT_label}'"),
  double `"{0}"` for a stat, bill or zone name (21; `ScenPart_StatFactor` = 'O
  status "{0}"'). PWU follows both: `'{2}'` for the research label, `"{0}"` for
  recipe/bill labels, quality labels and its own UI labels. Pawn names are never
  quoted.
- **No space before `:` `?` `!`** — 16 ` : ` against 5395 `: `, i.e. incidental.
  This is the reverse of French; do not port fr's spacing rule.
- **Register is você**, never tu — 240 `você` and 247 `seu|sua` against **0** `tu`
  and **0** `teu|tua` in Core Keyed. Settings prose uses você-imperatives (Ative
  isto, Desative, Defina isto como 0). Note this is the third register pattern
  across languages: de=du, es=tú, fr=vous, pt-BR=você.
- Descriptions/tooltips end with `.`; labels, buttons and float-menu reasons take
  none.
- `RecipeDef.label` is **`fazer X`** with no article (Core `Make_ComponentSpacer` =
  "fazer componente avançado", `Make_Wort` = "fazer mosto") — *fazer*, not
  *fabricar*. `description` is an imperative **with** article and period ("Faça um
  componente avançado."). `jobString` is a capitalized gerund with period ("Fazendo
  componente avançado."). `JobDef.reportString` is a lowercase gerund with period
  ("botando ovo.", "mantendo TargetA."). Like de/es/fr and unlike ja/ko, pt-BR job
  strings take terminal periods.
- Research labels are lowercase noun phrases (usinagem, lâminas longas, fabricação
  de cerveja, substituições biônicas).
- **Vanilla pt-BR files carry a UTF-8 BOM; ours never do.** Vanilla also contains
  outright sloppiness — `LetterTechprintAppliedLabel` leaves "Techprint" in
  English, and `AppearedDaysAgo` = "Apareceu há {0} dias atrás" doubles the
  "ago". Do not treat either as style guidance; prefer the clean sibling
  (`ActivatedDaysAgo` = "há {0} dias").

**The pt-BR landmine is that gender resolution is effectively DEAD, and the
article helpers produce no contractions** (decompile-verified —
`Verse.LanguageWorker_Portuguese`, `Verse.LanguageWordInfo`,
`Verse.GrammarResolverSimple`). This is the most severe of any language so far,
and for a reason that is invisible unless you look at the data files:

- `LanguageWorker_Portuguese` overrides **only** `WithIndefiniteArticle` and
  `WithDefiniteArticle`. There is **no `PostProcessed` override**, so pt inherits
  the base `LanguageWorker.PostProcessed`, which is just
  `str.MergeMultipleSpaces()`. That returns the string untouched unless it
  contains a double space, and never trims — so pt-BR has **no** German-style
  genitive rewrite and **no** French-style elision engine to trip over, and the
  intentional leading space in ` (padrão)` / trailing space in "Reembolso
  líquido: " are safe.
- **The Gender tables are 23 entries of livestock and nobility.** pt-BR ships
  `WordInfo/Gender/Female.txt` with **14** lines (amazona, baronesa, cabra,
  condessa, corça, duquesa, égua, galinha, ovelha, porca, …) and `Male.txt` with
  **9** (bode íbex, carneiro, galo, garanhão, iaque, touro, veado, …). Compare
  German ~2450 or Spanish 2771/1771. There is not a single common noun. It also
  ships `Singular.txt` (1350), `Plural.txt` and `new_words.txt` — but
  `LanguageWordInfo.LoadFrom` reads **only** `Male.txt`, `Female.txt` and
  `Neuter.txt` for gender, so those larger files contribute nothing here. Every
  noun PWU injects — arma, qualidade, traço, pesquisa, núcleo, bancada, relíquia,
  espada, martelo, memória, projeto — misses, and `ResolveGender`'s `defaultGender`
  is **Male**, so `{0_gender ? … : …}` and `{0_definite}` are a silent coin flip
  on essentially everything. (Vanilla itself uses the resolver freely —
  `BladelinkAlreadyBondedDialog` has `{PAWN_gender ? o : a }` — but that is a
  *pawn*, whose gender comes from the pawn, not from a word lookup.)
- **`WithDefiniteArticle` is plain `"o "`/`"a "` concatenation, so it emits no
  contractions — and Portuguese contractions are mandatory, not optional.**
  `de {0_definite}` yields "de o núcleo" (must be *do*), `em {0_definite}` yields
  "em a bancada" (must be *na*), `a {0_definite}` yields "a o" (must be *ao*), and
  a+a must be *à*. Portuguese loses six contractions this way where Spanish only
  loses *del* and *al*, which makes pt-BR strictly worse than the es case.

Net rule: same as German, Spanish and French, but with no escape hatch at all —
restructure so no article, adjective or participle has to agree with an injected
value, and write every contraction literally against a noun you control. Worked
rewrites in this mod:

- `PWU_RequiresWorkbench` ("requires a {0}") drops the article: **`requer {0}`**.
  `{0}` comes from `ResolveWorkbenchLabel` over a VEF-expandable bench set, so the
  noun is genuinely unknown. Vanilla sanctions the bare form —
  `NeedResearchBenchDesc` = "requer que você construa {1}". Note this makes
  `PWU_RequiresWorkbench` and `PWU_RequiresResearch` identical strings in pt-BR
  ("requer {0}"), since English differs only by the article. That is correct, not
  a copy-paste slip.
- `PWU_RequiresMinimumQuality` quotes the label: **`requer qualidade "{0}" ou
  melhor`**, head noun before the placeholder. Quality tiers are adjectives and
  vanilla pt-BR is itself gender-inconsistent about them (`QualityCategory_Good` =
  bom *masc*, `QualityCategory_Masterwork` = obra-prima *fem noun*,
  `QualityCategory_Legendary` = lendário *masc*), so no unquoted form agrees with
  feminine *qualidade* across all seven. Vanilla's own `NormalQualityOrBetter` =
  "qualidade normal ou melhor" is pre-inflected and cannot be templated. Same
  treatment in `PWU_CostTableNotApplicableDesc` (`definido como "{0}"`).
- Bail/error messages use **`Personalização de {0} interrompida: …`** — `de` + no
  article is gender-proof, and *interrompida* agrees with the fixed feminine
  *Personalização*, never with `{0}`. Each trailing clause carries its own fixed
  subject ("a bancada ficou inalcançável", "a arma foi perdida"). This is the pt-BR
  counterpart of German's `von`-frame, Spanish's `de`-frame and French's quoted
  frame.
- After a **colon** no quotes are needed (`Personalizar persona: {0}`, `Qualidade
  mínima da arma: {0}`) — matches vanilla `ResearchFinished` = "Pesquisa terminada:
  {0}".
- Job strings keep injected targets **bare** (`Adicionando {0} a {1}`, `Removendo
  {0} de {1}`, `Colocando {0} em {1}`) rather than risking a contraction, matching
  vanilla's own bare-target report strings ("mantendo TargetA.").
- The one place a contracted article IS hard-coded is **`na {1}`** in the four
  `PWU_Enable*RecipeDesc` strings. Deliberate and safe: `{1}` is bound to
  `PWU_ThingDefOf.FabricationBench.label` in `PWU_Mod.cs`, which pt-BR renders
  **bancada de fabricação** (feminine), so em+a → *na* is always right. Do not
  generalize it to an unpinned placeholder. Exact analogue of German's `am {1}`,
  Spanish's `en la {1}` and French's `à l'{1}`.
- Fixed Portuguese nouns contract and inflect normally — `PWU_BondSeveredWarning`
  writes `à sua forma base` (a+a on the fixed feminine *forma*). Only *injected*
  values need the workaround.

**Participle agreement on injected TRAIT labels is a real trap here, not a
theoretical one.** Royalty pt-BR trait labels are mixed gender — `Jealous` =
**Ciumenta** (fem), `Ugly` = **feia** (fem), `SpeedBoost` = Movimentação ágil
(fem), against `NeverBond` = Vínculo livre (masc), `NoPain` = Indolor. So any
participle agreeing with an injected trait is a coin flip. Three PWU strings were
rewritten for exactly this during the 2026-07-29 run:

- `PWU_IngredientShortfall` — "antes que {1} pudesse ser **aplicado**" became
  "antes de **aplicar** {1}" (infinitive, no agreement). `{1}` here is either a
  trait label or `PWU_MemoryWipeLabel`, so its gender is doubly unknowable.
- `PWU_AlreadyApplied` — "Já **aplicado**" became **"Já está na arma"**. The string
  has no placeholder, but its implicit subject is a trait of varying gender.
- `PWU_OnlyOnHostiles` — "Precisa ser **desarmado** de um hostil" became
  **"Somente ao desarmar um hostil"**.

Prefer an infinitive, a noun phrase, or a clause with its own fixed subject over
any participle whose subject is a trait, a weapon or a quality.

| English | Use | Never | Why |
|---|---|---|---|
| persona weapon / bladelink weapon | arma persona (label **suffix** persona: espada monomolecular persona) | arma vinculada, arma de lâmina ligada | Core `WeaponsMeleeBladelink.label` = armas persona; Royalty `MeleeWeapon_*Bladelink.label` |
| persona (the onboard mind) | a persona | a personalidade (reserve for the core's label), a alma | Royalty `BladelinkEquipWarning`, `NeverBond.description` — see the note above |
| trait (persona weapon) | traço / traços | característica (that's Odyssey's *unique*-weapon word) | Royalty `Stat_Thing_PersonaWeaponTrait_Label`, `BladelinkEquipWarningTraits` |
| trait (pawn, when disambiguation needed) | traço de personalidade | | Core `BrawlerHasRangedWeaponDesc` |
| customize / customization | personalizar / personalização | customizar, adaptar, configurar | Ideology `CustomizeIdeoligion` = "Personalização da Ideologia" |
| bond (noun / verb) | vínculo / criar vínculo, vincular, vinculado | laço (vanilla uses it, but mixes registers), ligação | Royalty `LetterBladelinkWeaponBondedLabel` = "Vínculo persona", `BladelinkBondedToSomeoneElse`, `NeverBond.description` |
| AI persona core | núcleo de personalidade IA | núcleo de persona, núcleo de IA | Core `AIPersonaCore.label` — the full label is long but it is vanilla |
| wielder / bearer | portador | usuário, manejador | Core `ShieldUserHasRangedWeapon`, Royalty bladelink descs |
| techprint | projeto técnico | techprint (vanilla leaves it English in `LetterTechprintAppliedLabel` — a vanilla bug), planta técnica | Core `TechprintLabel` = "projeto técnico ({PROJECT_label})". **Collides with "projeto de pesquisa"** — keep the qualifier whenever both could be meant |
| fabrication bench | bancada de fabricação | mesa de fabricação, banco de trabalho | Core `FabricationBench.label` (feminine → *na* bancada) |
| workbench (generic) | bancada | mesa de trabalho | Core `FabricationBench.description` = "Uma bancada equipada com…" |
| machining table / crafting spot | mesa de usinagem / ponto de fabricação | | Core labels |
| advanced components | componentes avançados | componentes de ponta | Core `ComponentSpacer.label`; plain components are componentes |
| monosword / plasmasword / zeushammer | espada monomolecular / espada de plasma / martelo de zeus | monoespada, espada mono | Royalty weapon labels — note zeushammer is **lowercase z** in vanilla |
| longsword / handle / point / edge | espada longa / cabo / ponta / lâmina | | Core + Royalty `tools.*.label` |
| mechanite | mecanitos | nanomáquinas, mecanitas | Core `Chemical_Luciferium`, `SkillNeurotrainerDescription`, `ResurrectionPsychosis`, Royalty monosword desc |
| plasteel / uranium / gold | plastiaço / urânio / ouro | plasteel, plastaço | Core labels — **plastiaço**, counterintuitive, always check |
| quality (noun) / tiers | qualidade / horrível·pobre·normal·bom·excelente·obra-prima·lendário | | Core `Quality`, `QualityCategory_*` — lowercase and **gender-inconsistent** (see the landmine) |
| "{0} quality or better" | `qualidade "{0}" ou melhor` | `qualidade {0} ou melhor` | quoting is required; `NormalQualityOrBetter` = "qualidade normal ou melhor" is pre-inflected |
| ultratech (tech level) | Ultra | ultratecnologia, supertecnologia | Core `TechLevel_Ultra`; spacer = Espacial |
| Crafting (the skill) | Fabricação | artesanato, manufatura | Core `Crafting.label`; `Crafting.labelShort` = fabricação |
| bill (work bill) | tarefa (add-bill menu: Adicionar Tarefa) | pedido, fatura, ordem | Core `TabBills` = Tarefas, `AddBill` = Adicionar Tarefa |
| recipe | receita | | Core `Stat_Recipe_*` |
| ingredients / hauling | ingredientes / transporte | acarreto | Core `Ingredients`, `WorkTagHauling` = transportar |
| pawn / colonist | personagem / colono | peão, boneco | Core Keyed uses personagem (9×); `Colonist` = colono |
| research / research project | pesquisa / projeto de pesquisa | | Core `Research`, `NeedResearchBenchDesc`, `ResearchFinished` = "Pesquisa terminada: {0}" |
| Cancel / Reset / Confirm / Randomize | Cancelar / Redefinir / **Aceitar** / Aleatorizar | Confirmar, Ao acaso | Core buttons — `Confirm` is **Aceitar**, not Confirmar |
| Reset to defaults | Restaurar padrões | Valores padrão | Core `RestoreToDefaultSettings` = "Restaurar Padrões"; `Default` = Padrão |
| None / Warning / Appearance / Rename | Nenhum / Aviso / Aparência / Renomear | Nada, Atenção, Aspecto | Core `None`, `Warning`, `Appearance`, `Rename` |
| Prerequisites / Cost / Unlocks | Pré-requisitos / custo / Desbloqueia | | Core `Prerequisites`, `Cost`, `Unlocks` |
| Empire (faction) | Império Fragmentado | Império alone | Royalty `Empire.label` |
| Empire traders | comerciantes imperiais | comerciantes do Império | Royalty `Orbital_Empire.label` = comerciante imperial |
| freewielder (trait label) | Vínculo livre | portador livre, sem vínculo | Royalty `NeverBond.label` — note it is **capitalized** in vanilla, unlike de/fr where trait labels are lowercase |
| relic / reliquary | relíquia / relicário | vestígio | Ideology `Relic`, `RelicOf`, `Reliquary.label` |
| ideoligion / reform | ideologia / reformar ideologia | ideoligião | Ideology `IdeoligionOf`, `ReformIdeoligion` — pt-BR uses the plain word with **no portmanteau**, like German and unlike es/fr/ru |
| stopping power / burst count / burst speed | Poder de parada / Contagem de tiros por disparo / Taxa de disparo | | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| armor penetration / damage / accuracy | Penetração de Armadura / Dano / Precisão | | Core `ArmorPenetration`, `Damage`, `Accuracy` |
| EMP / EMP stun | PEM / Atordoado por PEM | EMP | Core `StunnedByEMP`, Royalty zeushammer desc — pt-BR localizes the acronym |
| forbidden / reserved / unreachable / no power / no path | proibido / reservado / inalcançável / sem energia / sem caminho | inacessível (reserve for "not accessible") | Core `ForbiddenLower`, `Reserved`, `CannotReach`, `NoPower`, `NoPath` |
| quest / hostiles / enemies | missão / hostis / inimigos | busca | Core `Quest` (vanilla value is plural "Missões"), `Hostiles`, `Enemies` |
| "{0} days ago" | `há {0} dias` | `há {0} dias atrás` (vanilla's own `AppearedDaysAgo` doubles it — a bug) | Core `ActivatedDaysAgo` |
| vanilla (the base game) | original / do jogo original | vanilla, baunilha | no vanilla pt-BR `Keyed` term exists; 0 occurrences of "vanilla" |
| gizmo button | botão de comando | gizmo | no vanilla pt-BR `Gizmo*` key exists; `GameplayTips.RightClickGizmos` establishes "botões no menu de ordens" |
| bladelink customization (this mod's research) | personalização de armas persona | | composed from two grounded terms — see the note below |

Three further pt-BR notes.

**`PWU_BladelinkCustomization.label` is composed, not coined.** Both halves are
vanilla pt-BR (personalização from `CustomizeIdeoligion`, armas persona from
`WeaponsMeleeBladelink.label`), so unlike ja/zh no invention was needed. It is
longer than the median pt-BR research label (usinagem, lâminas longas) though in
line with the longest (substituições biônicas, sarcófago de criptosono), so a
native reviewer may prefer a terser "personalização de personas"; flagged in the
2026-07-29 commit — do not silently flip it either way.

**Landmine — the persona-core recipe's research prerequisite.** `PWU_UI.xml`'s
translator comment calls it "machine persuasion (vanilla research label)". The def
is `ShipComputerCore`, and pt-BR renders it **persuasão mecânica** — a literal
match for the English hint. pt-BR joins Korean (기계 설득) as the exception here;
ja gives AIコンピュータコア, zh 飞船电脑核心, fr "noyau central de l'ordinateur de
bord" and es "persuasión de IA", none of which resemble the English. Still resolve
the defName through the tar every time — the coincidence is language-specific, not
a general licence. (No def named `MachinePersuasion` exists, so grepping the hint
text finds nothing.)

**Kill tracker vs kill memory, and the haul planner.** English's "kill tracker"
(toggle label) and "kill memory" (prose) split into **registro de mortes** and
**memória de mortes** — pt-BR keeps the distinction like Spanish and French rather
than collapsing it like German, because both read naturally against `PWU_TabMemory`
= Memória. Haul planner modes are mod-coined with no vanilla anchor: **Sequencial /
Varredura / Minucioso**.

### Cross-language lessons (from UniqueWeaponsUnbound's translation work)

- Japanese vanilla style: ASCII punctuation (`,` `.`, never `、` `。`),
  です/ます descriptions, continuous-form job strings (〜している, no period),
  「」 around quoted labels.
- Wrap injected `{0}` def labels in the language's quote marks (JP 「{0}」,
  RU «{0}», zh-Hans “{0}”, ko '{0}', de '{0}', es "{0}", fr "{0}", pt-BR both —
  see below) — note de, es, fr and pt-BR all use ASCII quotes, but de takes single
  and es/fr take double, and none of them uses its own typographic pair
  (`„…"` / `«…»`) for a placeholder in Keyed data. **Two languages have a *split*
  convention, and they split on different axes.** French splits by whether the
  quoted thing is a runtime value or a fixed label: ASCII `"{0}"` for injected
  values, guillemets with inner spaces (`« Secourir »`) when naming a UI element
  the player must click. Brazilian Portuguese splits by *what kind of entity* the
  injected value is: ASCII single `'{0}'` for a research project or quest
  (`NeedResearchBenchDesc`), ASCII double `"{0}"` for a stat, bill or zone name
  (`ScenPart_StatFactor`) — both are placeholders, so the fr rule does not
  transfer. Injected labels never inflect,
  and quoting sidesteps case and agreement problems. Korean is the one language
  where quoting also interacts with grammar and still works:
  `LanguageWorker_Korean` looks *through* a preceding `'`/`"` to find the
  syllable that decides the josa, so `'{0}'(와)과` resolves correctly. Note the
  de quote mark is the **ASCII** single quote, not `„…"` — vanilla de never
  uses German typographic quotes in Keyed data.
- **Always decompile `LanguageWorker_<Language>` before writing a value, and
  check `PostProcessed` specifically.** It rewrites every finished string, the
  checker cannot see it, and its edits are silent. So far: ko's `ReplaceJosa`
  (a feature to use), de's genitive `'s` regex (one narrow trap), and fr's five
  elision/contraction regexes (mostly a feature — `de {0}` self-repairs to
  `d'{0}` for vowel-initial values — but with three real traps: enclitic
  `-la`/`-le` before a vowel becomes `-l'`, `de le` becomes `de` instead of `du`,
  and `à le` is consumed into `au` even when "le" is a pronoun). **A worker with
  no `PostProcessed` override is also a finding worth recording** — es and pt-BR
  both inherit the base `LanguageWorker.PostProcessed`, which is only
  `MergeMultipleSpaces()`: it returns the string untouched unless it contains a
  double space and never trims, so keys that intentionally carry a leading or
  trailing space (`PWU_DefaultSuffix`, `PWU_NetRefund`) are safe. The cheap way to
  verify a whole language at once is to port `PostProcessedInt` to a throwaway
  script and diff it over every translated value: any value the worker rewrites
  is a value you did not actually write. Do this with placeholders substituted
  too, since the traps fire on the *filled* string.
- **Know which resolver your strings actually reach.** `"key".Translate(args)`
  goes to `GrammarResolverSimple`, *not* the full rulepack `GrammarResolver`, so
  the two are not interchangeable in what they support. On a plain `string` arg
  `GrammarResolverSimple` gives you `{N_gender ? … : … : …}`, `{N_definite}`,
  `{N_indefinite}`, `{N_plural}` and the pronoun family — gender is looked up
  from the word itself via `LanguageWordInfo`, so no arg metadata is needed. It
  does **not** implement `lookup` at all, so `{lookup: {0}; decline; N}` and
  every case form it would produce are unavailable. For inflecting languages
  that means gender is usually solvable and **case is not**: restructure so no
  article or adjective has to agree (drop the article, or move the head noun in
  front of the placeholder). See the German glossary for worked rewrites.
- **A gender lookup that misses defaults to masculine** (`ResolveGender`'s
  `defaultGender`), and mod-coined nouns are never in the vanilla Gender tables —
  so `{N_gender ? …}` on a mod's own label is a silent coin-flip, not a fix.
  Reserve it for vanilla nouns in nominative slots.
- **Check the size of `WordInfo/Gender/*.txt` before trusting gender at all, and
  check which files the loader actually reads.** Coverage varies enormously:
  German ~2450 entries, Spanish 2771/1771, but **pt-BR ships 14 female + 9 male,
  all livestock and noble titles** — not one common noun, so gender is effectively
  dead there and every injected value silently resolves masculine.
  `LanguageWordInfo.LoadFrom` reads only `Male.txt`, `Female.txt` and
  `Neuter.txt`, so a language's larger `Singular.txt` / `Plural.txt` /
  `new_words.txt` (pt-BR has 1350+ lines of them) contribute nothing to gender —
  do not mistake their size for coverage.
- **Check whether the article helpers emit contractions, separately from gender.**
  `WithDefiniteArticle` is plain concatenation in several workers, so
  `de {0_definite}` produces "de o …" rather than the required contraction. How
  much that costs depends on the language: Spanish loses only *del* and *al*,
  while **Portuguese loses six mandatory contractions** (de+o=do, de+a=da,
  em+o=no, em+a=na, a+o=ao, a+a=à), making pt-BR the worst case so far. Write
  every contraction literally against a noun you control, and never against an
  injected one.
- **Agreement can bite with no placeholder in sight.** A fixed participle whose
  implicit subject is an injected-gender thing is just as broken as `{0_definite}`:
  pt-BR's `PWU_AlreadyApplied` ("Já aplicado") had to become "Já está na arma"
  because Royalty's trait labels are mixed gender (Ciumenta, feia vs Vínculo
  livre). When auditing, grep for participles and adjectives near *and* about
  placeholders, and prefer infinitives, noun phrases, or clauses with their own
  fixed subject.
- Coined vanilla terms (ideoligion) may be a portmanteau in one language
  (RU идеолигия, es ideoligión, fr idéoligion) and a plain word in another
  (JP 思想, zh-Hans 文化, de Ideologie, pt-BR ideologia) — always check, never
  extrapolate between languages. Relevant here for `PWU_RelicNameTooltip`
  (zh-Hans relic = 圣物, de = Reliquie).
- **A false friend in one language can be the correct vanilla term in its
  neighbour.** Spanish *persona* means "person" and is a bug in any value; but
  Portuguese has *pessoa* for that, leaving *persona* free, and vanilla pt-BR uses
  it as the weapon term ("armas persona"). Two languages sharing a Latin root is
  not evidence they share a rendering — ground each one independently.
- **Folder names are not derivable from how the user names the language.** Ask the
  tar, not intuition: "Brazilian Portuguese" is `PortugueseBrazilian`, and it sits
  beside a separate `Portuguese` (European) with its own tar and its own
  terminology. Same trap as `Spanish` vs `SpanishLatin`.
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
