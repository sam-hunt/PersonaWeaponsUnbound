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
between DLCs (개성 vs 무기 특성). Run the lookup every time; record what came
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

**The German landmine is grammatical gender, and it bites harder than Korean
josa.** Every `Translate()` call site in this mod passes a plain `string`, not a
`NamedArgument`, so vanilla's `{lookup: {0}; decline; 3}` and
`{1_gender ? einen : eine : ein}` machinery **cannot resolve** — an injected def
label is invariant text with no gender or case attached. Phrase around it:

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

### Cross-language lessons (from UniqueWeaponsUnbound's translation work)

- Japanese vanilla style: ASCII punctuation (`,` `.`, never `、` `。`),
  です/ます descriptions, continuous-form job strings (〜している, no period),
  「」 around quoted labels.
- Wrap injected `{0}` def labels in the language's quote marks (JP 「{0}」,
  RU «{0}», zh-Hans “{0}”, ko '{0}', de '{0}') — injected labels never inflect,
  and quoting sidesteps case and agreement problems. Korean is the one language
  where quoting also interacts with grammar and still works:
  `LanguageWorker_Korean` looks *through* a preceding `'`/`"` to find the
  syllable that decides the josa, so `'{0}'(와)과` resolves correctly. Note the
  de quote mark is the **ASCII** single quote, not `„…"` — vanilla de never
  uses German typographic quotes in Keyed data.
- Gendered/case-inflecting languages need more than quoting: an injected label
  carries no gender, so any article or adjective agreeing with it must be
  removed rather than guessed. Every `Translate()` call in this mod passes a
  plain `string`, so vanilla's `{lookup: …; decline; N}` and
  `{N_gender ? … : … : …}` resolvers are unavailable — see the German glossary's
  landmine note for the concrete rewrites (drop the article; or move the head
  noun in front of the placeholder).
- Coined vanilla terms (ideoligion) may be a portmanteau in one language
  (RU идеолигия) and a plain word in another (JP 思想, zh-Hans 文化,
  de Ideologie) — always check, never extrapolate between languages. Relevant
  here for `PWU_RelicNameTooltip` (zh-Hans relic = 圣物, de = Reliquie).
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
