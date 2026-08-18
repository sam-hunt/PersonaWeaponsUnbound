# Brazilian Portuguese — Persona Weapons Unbound glossary

Grounded in PWU's own 2026-07-29 pt-BR generation (pt-BR had no preseed from
a sibling mod; no native review yet). This mod targets `PortugueseBrazilian`
(not the separate European `Portuguese`). Family-shared engine mechanics
(including the dead-gender-resolution and missing-contraction findings),
style rules, and vanilla-grounded common vocabulary live in `l10n/languages/
PortugueseBrazilian.md` — this file holds only what is specific to Persona
Weapons Unbound.

## Coined / mod-specific terms

**pt-BR is the one language so far where "persona" is vanilla vocabulary for
the weapon.** This is the exact opposite of Spanish, where *persona* means
"person" and is a bug. Portuguese has *pessoa* for "person", leaving
*persona* free as a loanword, and vanilla uses it:
`WeaponsMeleeBladelink.label` = **armas persona**,
`MeleeWeapon_MonoSwordBladelink.label` = "espada monomolecular **persona**"
(suffix, not prefix), `LetterBladelinkWeaponBondedLabel` = "**Vínculo
persona**". So no coinage is needed for the class noun. For the onboard mind
vanilla is split: **persona** in `BladelinkEquipWarning` ("a persona da
arma") and `NeverBond.description` ("A persona desta arma"), but
**personalidade** in the bladelink ThingDef descriptions and
`LetterBladelinkWeaponBonded`. PWU uses **persona** for the mind, for three
reasons: it keeps one word across all 158 keys, it matches "arma persona",
and *personalidade* is already spoken for twice over — by
`AIPersonaCore.label` ("núcleo de personalidade IA") and by the pawn-trait
phrase "traço de personalidade", which would collide badly with this mod's
constant talk of weapon traços. Flagged for native review in the 2026-07-29
commit; do not silently flip it either way.

**pt-BR is a THIRD trait pattern — the mirror image of Spanish.** Royalty's
`Stat_Thing_PersonaWeaponTrait_Label` = **Traços** and Royalty's
`BladelinkEquipWarningTraits` = "os seguintes **traços**", while Odyssey's
`Stat_ThingUniqueWeaponTrait_Label`, Core's `WeaponTraits` and
`StatsReport_WeaponTraits` all say **Características**. Core's *pawn* trait
header `<Traits>` is also **Traços**. So in pt-BR the Royalty word coincides
with the pawn word and Odyssey is the odd one out — whereas in Spanish,
Royalty (características) was the odd one out against Odyssey+pawn
(rasgos). Use **traço/traços**; *características* would be importing UWU's
domain word. The pawn/weapon collision is harmless because the disambiguator
is the trailing qualifier: pawn traits are "traço **de personalidade**"
(Core `BrawlerHasRangedWeaponDesc`), so a bare "traço" in weapon context
reads correctly.

**Participle agreement on injected TRAIT labels is a real trap here, not a
theoretical one.** Royalty pt-BR trait labels are mixed gender — `Jealous`
= **Ciumenta** (fem), `Ugly` = **feia** (fem), `SpeedBoost` = Movimentação
ágil (fem), against `NeverBond` = Vínculo livre (masc), `NoPain` = Indolor.
So any participle agreeing with an injected trait is a coin flip. Three PWU
strings were rewritten for exactly this during the 2026-07-29 run:

- `PWU_IngredientShortfall` — "antes que {1} pudesse ser **aplicado**"
  became "antes de **aplicar** {1}" (infinitive, no agreement). `{1}` here
  is either a trait label or `PWU_MemoryWipeLabel`, so its gender is doubly
  unknowable.
- `PWU_AlreadyApplied` — "Já **aplicado**" became **"Já está na arma"**.
  The string has no placeholder, but its implicit subject is a trait of
  varying gender.
- `PWU_OnlyOnHostiles` — "Precisa ser **desarmado** de um hostil" became
  **"Somente ao desarmar um hostil"**.

## Vocabulary

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
| Crafting (the skill) | Fabricação | artesanato, manufatura | Core `Crafting.label`; `Crafting.labelShort` = fabricação |
| bill (work bill) | tarefa (add-bill menu: Adicionar Tarefa) | pedido, fatura, ordem | Core `TabBills` = Tarefas, `AddBill` = Adicionar Tarefa |
| ingredients / hauling | ingredientes / transporte | acarreto | Core `Ingredients`, `WorkTagHauling` = transportar |
| pawn / colonist | personagem / colono | peão, boneco | Core Keyed uses personagem (9×); `Colonist` = colono |
| Empire (faction) | Império Fragmentado | Império alone | Royalty `Empire.label` |
| Empire traders | comerciantes imperiais | comerciantes do Império | Royalty `Orbital_Empire.label` = comerciante imperial |
| freewielder (trait label) | Vínculo livre | portador livre, sem vínculo | Royalty `NeverBond.label` — note it is **capitalized** in vanilla, unlike de/fr where trait labels are lowercase |
| stopping power / burst count / burst speed | Poder de parada / Contagem de tiros por disparo / Taxa de disparo | | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| armor penetration / damage / accuracy | Penetração de Armadura / Dano / Precisão | | Core `ArmorPenetration`, `Damage`, `Accuracy` |
| gizmo button | botão de comando | gizmo | no vanilla pt-BR `Gizmo*` key exists; `GameplayTips.RightClickGizmos` establishes "botões no menu de ordens" |
| bladelink customization (this mod's research) | personalização de armas persona | | composed from two grounded terms — see below |

## Worked rewrites tied to PWU's own strings

- `PWU_RequiresWorkbench` ("requires a {0}") drops the article: **`requer
  {0}`**. `{0}` comes from `ResolveWorkbenchLabel` over a VEF-expandable
  bench set, so the noun is genuinely unknown. Vanilla sanctions the bare
  form — `NeedResearchBenchDesc` = "requer que você construa {1}". Note this
  makes `PWU_RequiresWorkbench` and `PWU_RequiresResearch` identical strings
  in pt-BR ("requer {0}"), since English differs only by the article. That
  is correct, not a copy-paste slip.
- `PWU_RequiresMinimumQuality` quotes the label: **`requer qualidade "{0}"
  ou melhor`**, head noun before the placeholder. Quality tiers are
  adjectives and vanilla pt-BR is itself gender-inconsistent about them
  (`QualityCategory_Good` = bom *masc*, `QualityCategory_Masterwork` =
  obra-prima *fem noun*, `QualityCategory_Legendary` = lendário *masc*).
  Same treatment in `PWU_CostTableNotApplicableDesc` (`definido como
  "{0}"`).
- Bail/error messages use **`Personalização de {0} interrompida: …`** — `de`
  + no article is gender-proof, and *interrompida* agrees with the fixed
  feminine *Personalização*, never with `{0}`. Each trailing clause carries
  its own fixed subject ("a bancada ficou inalcançável", "a arma foi
  perdida").
- Job strings keep injected targets **bare** (`Adicionando {0} a {1}`,
  `Removendo {0} de {1}`, `Colocando {0} em {1}`) rather than risking a
  contraction.
- The one place a contracted article IS hard-coded is **`na {1}`** in the
  four `PWU_Enable*RecipeDesc` strings. Deliberate and safe: `{1}` is bound
  to `PWU_ThingDefOf.FabricationBench.label` in `PWU_Mod.cs`, which pt-BR
  renders **bancada de fabricação** (feminine), so em+a → *na* is always
  right. Do not generalize it to an unpinned placeholder.
- Fixed Portuguese nouns contract and inflect normally —
  `PWU_BondSeveredWarning` writes `à sua forma base` (a+a on the fixed
  feminine *forma*). Only *injected* values need the workaround.

## Further pt-BR notes

**`PWU_BladelinkCustomization.label` is composed, not coined.** Both halves
are vanilla pt-BR (personalização from `CustomizeIdeoligion`, armas persona
from `WeaponsMeleeBladelink.label`), so unlike ja/zh no invention was
needed. It is longer than the median pt-BR research label (usinagem, lâminas
longas) though in line with the longest (substituições biônicas, sarcófago
de criptosono), so a native reviewer may prefer a terser "personalização de
personas"; flagged in the 2026-07-29 commit — do not silently flip it either
way.

**Landmine — the persona-core recipe's research prerequisite.**
`PWU_UI.xml`'s translator comment calls it "machine persuasion (vanilla
research label)". The def is `ShipComputerCore`, and pt-BR renders it
**persuasão mecânica** — a literal match for the English hint. Still resolve
the defName through the tar every time — the coincidence is
language-specific, not a general licence.

**Kill tracker vs kill memory, and the haul planner.** English's "kill
tracker" (toggle label) and "kill memory" (prose) split into **registro de
mortes** and **memória de mortes** — pt-BR keeps the distinction like
Spanish and French rather than collapsing it like German, because both read
naturally against `PWU_TabMemory` = Memória. Haul planner modes are
mod-coined with no vanilla anchor: **Sequencial / Varredura / Minucioso**.

## Workshop title (2026-08-18, machine-assisted, pending native review)

**Armas persona liberadas** (= `PWU_SettingsCategory`, coupled to line 1 of
`.steamworkshop/Description/PortugueseBrazilian.txt`). "armas persona" is
Royalty pt-BR's own `WeaponsMeleeBladelink.label` verbatim. "liberadas"
renders "Unbound" without touching the vinculo/Vínculo livre bond vocabulary
already reserved for the bladelink-bond and freewielder mechanics.
Sentence case per Portuguese title norms.
