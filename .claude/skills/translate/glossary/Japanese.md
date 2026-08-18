# Japanese — Persona Weapons Unbound glossary

Grounded in PWU's own 2026-07-28 ja generation (preseeded from
UniqueMeleeWeapons' 2026-07 run and the Royalty JP tar; no native review
yet). Family-shared engine mechanics, style rules, and vanilla-grounded
common vocabulary live in `l10n/languages/Japanese.md` — this file holds
only what is specific to Persona Weapons Unbound.

## Coined / mod-specific terms

**"Bladelink" has no standalone ja rendering anywhere in vanilla.** Vanilla
ja prefixes the persona weapons with ペルソナ (ペルソナモノソード) and calls
the mind ペルソナ / 武器に宿るペルソナ in prose, while the
`MeleeWeapon_*Bladelink` ThingDef descriptions switch to 搭載されたAI and
生体認証 — the term itself is always sidestepped. So
`PWU_BladelinkCustomization.label` is a coinage: ペルソナ武器のカスタマイズ,
chosen to keep カスタマイズ single-valued across all 158 keys. It is longer
than vanilla's typical research label, and Royalty *does* transliterate the
parallel construction (`Gunlink.label` = ガンリンク), so a native reviewer
may prefer a terser ブレードリンク改変 or ペルソナ武器改変. Flagged for
native review in the 2026-07-28 commit; do not silently "correct" it either
way. Note 改変 is vanilla's own verb for altering a persona core
(`ShipComputerCore`'s `generalRules` ship `subject→AI人格コア改変`) and for
`ReformIdeoligion`, so it is the grounded alternative if brevity is wanted.

**Trait divergence by DLC.** Royalty's `Stat_Thing_PersonaWeaponTrait_Label`
is 特性・特徴, while Odyssey's `Stat_ThingUniqueWeaponTrait_Label` (UWU's
domain) is plain 特性 — use 特性・特徴 here, never the bare Odyssey form.

## Vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| trait (persona weapon) | 特性・特徴 | 特性 alone | Royalty `Stat_Thing_PersonaWeaponTrait_Label`, `BladelinkEquipWarningTraits`; plain 特性 is Odyssey's *unique*-weapon word |
| persona weapon / persona / bond | ペルソナ武器 / ペルソナ (人格 in the bond letter) / 絆, 絆を結ぶ | | Royalty `WeaponsMeleeBladelink`, `BladelinkEquipWarning*`, `LetterBladelinkWeaponBonded` |
| monosword / plasmasword / zeushammer | モノソード / プラズマソード / ゼウスハンマー (persona forms prefix ペルソナ) | | Royalty weapon labels |
| persona core / AI persona core | AI人格コア | ペルソナコア | Core `AIPersonaCore.label` — vanilla JP renders "persona core" with 人格, not ペルソナ, in *this* noun |
| techprint | テックプリント | 技術図面 | Core `TechprintLabel` |
| fabrication bench | コンポーネント工作台 | 製造台, 精密工作機械 (that's the `Machining` research) | Core `FabricationBench.label` |
| advanced components | 先進コンポーネント | 高度部品 | Core `ComponentSpacer.label`; plain components are コンポーネント |
| Crafting (the skill) | 工芸 | 製作, クラフト | Core `Crafting.label` |
| bill (work bill) | 加工 (add-bill menu: 新しい加工) | 請求, ビル | Core `TabBills`=加工, `AddBill`=新しい加工 |
| freewielder (trait label) | 自由支配者 | 自由 | Royalty `NeverBond.label` — quote it as 「自由支配者」 when naming it |
| stopping power / burst count / burst speed | 威力 / バースト時の弾数 / 連射速度 | 抑止力 | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| mechanite / mechanoid | メカナイト / メカノイド | | Royalty, Odyssey descs |
| wielder / bearer | 使用者 / 持ち主 | | Odyssey `EMPPulser` desc |
| stun / EMP / stagger | スタン / EMP / よろめき | | `StunnedByEMP`, `StaggerDurationFactor` |
| armor penetration / bleed rate / move speed | アーマー貫通力 / 出血量 / 移動速度 | | Core Stat labels |
| cut / stab (DamageDef) | 斬る / 刺す | 切創, 刺し傷 (those are the *hediff* labels) | Core DamageDefs vs HediffDefs differ |
| gizmo button | コマンドボタン | ギズモ | no vanilla ja `Gizmo*` key exists; `Command*Desc` calls them コマンド |
| Empire (faction) | 落日の帝国 | 帝国 alone | Royalty `Empire.label` |
| bladelink customization (this mod's research) | ペルソナ武器のカスタマイズ | | **Coined** — see above |

## Landmine — ShipComputerCore research prerequisite

`PWU_UI.xml`'s translator comment calls it "machine persuasion (vanilla
research label)", and that is the correct *English* label, but the def is
`ShipComputerCore` and ja renders it **AIコンピュータコア** — nothing like
the English. It reaches the player through `{2}` at runtime, so nothing is
translated there, but never resolve that hint literally when phrasing
around it; look the defName up in the tar.
