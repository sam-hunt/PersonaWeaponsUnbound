# Traditional Chinese — Persona Weapons Unbound glossary

Grounded in PWU's own 2026-08-22 zh-Hant generation pass, mined fresh from the
official zh-Hant Core + Royalty + Ideology tars. **No native-speaker review
yet.** Family-shared engine mechanics, style rules, and vanilla-grounded
common vocabulary live in `l10n/languages/ChineseTraditional.md` — this file
holds only what is specific to Persona Weapons Unbound.

**Do not port anything from `ChineseSimplified.md`.** This pass re-grounded
every term against the zh-Hant tars and found the two Chinese localizations
inverted on five of PWU's own core terms (see the table below). Character
conversion of the zh-Hans tree would produce wrong zh-Hant in every one.

## Coined / mod-specific terms

**"Bladelink"/"persona weapon" needs no coinage in zh-Hant — vanilla has one.**
Unlike zh-Hans (which marks persona weapons with the Δ' label prefix and has no
prose term, forcing PWU's 智能人格定制 coinage), Royalty zh-Hant renders the
English label `persona monosword` as **單分子劍(羈絆武器)** — a parenthetical
suffix using a real standalone term, 羈絆武器. It is also the term in prose
(`LetterBladelinkWeaponBondedLabel`) and reusable attributively.

So `PWU_BladelinkCustomization.label` = **羈絆武器自訂**, which is grounded
rather than coined: 羈絆武器 is Royalty's own term and 自訂 is Core Keyed's
`Customize`. It matches vanilla's terse research-label style (造槍, 機械加工,
高級精密製作). This is a genuinely better-grounded answer than the zh-Hans
sibling's, so do not "harmonize" the two.

**Trait divergence by DLC — zh-Hant DOES diverge, unlike zh-Hans.** Royalty's
`Stat_Thing_PersonaWeaponTrait_Label` = **特性** (also Core Keyed `Traits` and
`BladelinkEquipWarningTraits`), while Odyssey's `Stat_ThingUniqueWeaponTrait_
Label` = 特質. PWU is Royalty's domain, so **特性 throughout** — 特質 belongs to
the UniqueWeaponsUnbound sibling. (zh-Hans has no such split; do not assume
from it.)

## Vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| persona weapon / bladelink weapon | 羈絆武器 | 人格武器, Δ'前綴 | Royalty `MeleeWeapon_*Bladelink.label` = 單分子劍(羈絆武器); the Δ' prefix is the zh-Hans convention only |
| persona weapon label form | suffix `(羈絆武器)`, ASCII parens set solid | （羈絆武器） | Royalty labels; full-width parens are zh-Hans's convention |
| persona (the onboard mind, prose) | 人格 (AI人格 in weapon descriptions, AI set solid) | 智能人格 | Royalty `NeverBond.description` 這把武器的人格; weapon descs 這件武器自身具備AI人格 |
| persona weapon trait (stat) | 特性 | 特質, 屬性 | Royalty `Stat_Thing_PersonaWeaponTrait_Label` — see divergence note |
| persona core | 人格核心 | | Core `AIPersonaCore.label` |
| bond (verb/state) / the bond (noun) | 綁定 / 羈絆 | | Royalty `BladelinkAlreadyBonded*`, `LetterBladelinkWeaponBonded` |
| techprint | 科技藍圖 | 科研藍圖 | Core `ResearchTechprintRequirement` — **inverted vs zh-Hans's 科研蓝图** |
| fabrication bench | 精密製作桌 | 精密裝配台 | Core `FabricationBench.label` |
| advanced component | 高級零件 | 高級元件 | Core `ComponentSpacer.label` |
| advanced fabrication (research) | 高級精密製作 | | Core `AdvancedFabrication.label` |
| monosword / plasmasword / zeushammer | 單分子劍 / 等離子劍 / 宙斯錘 | | Royalty base-weapon labels (prose plasma = 電漿) |
| customize (verb + UI command) | 自訂 | 客製化, 定制 | Core Keyed `Customize`; `CustomizeIdeoligion` 自訂理念 |
| appearance / texture tab | 外觀 | 材質 | Core Keyed `Appearance`; 紋理 is reserved for `TextureCompression` (graphics settings) |
| bill (workbench order) | 工作 (add-bill menu 新增工作) | 訂單, 帳單 | Core `AddBill` 新增工作; 訂單 is spent on `Quest_TradeRequest` 訂單任務 |
| freewielder (trait label) | 自由 | 自由持有 | Royalty `NeverBond.label` — quote it as 「自由」特性 when naming it |
| stopping power | 攔截力 | 抑止能力 | Core `StoppingPower` — **inverted vs zh-Hans's 抑止能力** |
| burst count / burst speed | 連發次數 / 射速 | 連射次數 | Core `BurstShotCount`, `BurstShotFireRate` |
| Crafting (skill) | 手工 | 製作 | Core `Crafting.skillLabel` — 製作 is the verb, never the skill name |
| Empire (faction) | 破碎帝國 | 帝國 | Royalty `Empire.label` |
| relic / ideoligion | 聖物 / 理念 | | Ideology `IdeoRelic`, `CustomizeIdeoligion` |
| colonist / quest / caravan | 殖民者 / 任務 / 旅隊 | | Core Keyed |
| quality tiers | 糟糕/劣質/普通/良好/傑出/大師/傳奇 | | Core `QualityCategory_*` |
| Cancel / Confirm / Randomize / Reset | 取消 / 確定 / 隨機生成 / 重置 | | Core Keyed |
| reset to defaults | 恢復為預設值 | 恢復為默認值 | Core `RestoreToDefaultSettings` |
| DLC brand names | 「皇權」/「漫遊」/「理念」 | English names | Core `SimulateNotOwning*` — zh-Hant localizes them, in corner brackets |
| bladelink customization (this mod's research) | 羈絆武器自訂 | 智能人格定制 | **Grounded, not coined** — see above |

## Slot rules this pass confirmed for PWU's surface

- **RecipeDef `jobString` carries no trailing 。**, exactly like JobDef
  `reportString`, even though the English source ends in a period — and vanilla
  makes `label` and `jobString` byte-identical (`Make_ComponentSpacer` = 製作高級零件
  for both). PWU's four `PWU_Make_*` recipes follow that shape. The `description`
  in the same file *does* end 。
- **Job report strings split on transitivity**: transitive ones take no 中
  (清理TargetA, 破解TargetA), intransitive ones take 中 (覓食中, 巡邏中). PWU's
  `PWU_CustomizingWeapon`/`PWU_AddingTrait`/etc. are transitive and bare;
  `PWU_AdmiringWork` and the `PWU_CustomizeWeapon.reportString` take 中.
- **Injected labels and clicked UI commands both take 「」** (not curly quotes),
  including where English writes `"{0}"` — so `PWU_Enable*RecipeDesc`'s bill name
  becomes 「{0}」. Royalty's own `CataphractArmor.description` cites 「全覆式裝甲」
  the same way.
- **Label:value templates take full-width ：** (最低武器品質：{0}), while the
  parenthetical suffixes take ASCII parens set solid ((預設值), (原版)).

## Landmine — ShipComputerCore research prerequisite

`PWU_UI.xml`'s translator comment calls `{2}` "machine persuasion (vanilla
research label)" in `PWU_EnablePersonaCoreRecipeDesc`. That is the correct
*English* label, but the def is `ShipComputerCore` and zh-Hant renders it
**機械核心** — nothing like the English, and also nothing like zh-Hans's
飛船電腦核心. Never translate that hint literally; resolve the defName through
the tar. (PWU's instance of the general resolve-a-hint-through-the-tar rule.)

## Workshop title (2026-08-22, machine-assisted, pending native review)

**羈絆武器解放** (= `PWU_SettingsCategory`, coupled to line 1 of
`.steamworkshop/Description/ChineseTraditional.txt`). 羈絆武器 is Royalty
zh-Hant's own term for persona weapons and the searchable anchor the
`.steamworkshop/README.md` title convention requires; unlike zh-Hans, no
prose substitute is needed because the term is not a label-only glyph prefix.
解放 renders "Unbound" without colliding with the mod's 自由 (freewielder) or
自訂 (customize) vocabulary — matching the zh-Hans sibling's choice of 解放.

Workshop-description-only notes: vanilla zh-Hant localizes the DLC brand names
in corner brackets (「皇權」/「漫遊」/「理念」), so the description uses those
rather than the English names the FR/DE/ES/JA/KO/RU descriptions keep. Mod
names that are not vanilla (Unique Weapons Unbound, VEF, More Persona Traits)
stay English, spaced from adjacent CJK.
