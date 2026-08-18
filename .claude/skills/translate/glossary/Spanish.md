# Spanish — Persona Weapons Unbound glossary

Grounded in PWU's own 2026-07-29 es generation (Spanish had no preseed from
a sibling mod; no native review yet). This mod targets Castellano
(`Spanish`, not `SpanishLatin`). Family-shared engine mechanics, style
rules, and vanilla-grounded common vocabulary live in `l10n/languages/
Spanish.md` — this file holds only what is specific to Persona Weapons
Unbound. Note: PWU's own `PWU_RequiresWorkbench`-style bail-message pattern
(leading with an invariant feminine head noun so no injected value has to
agree) was promoted into that shared file's Engine mechanics section
verbatim as the family's worked example — see it there rather than here.

## Coined / mod-specific terms

**"Persona" is a false friend and vanilla es never uses it for the weapon
mind.** Spanish *persona* means "person", so "arma persona" reads as
"person weapon". Vanilla es systematically renders the onboard persona as
**la IA** and the weapon class as **arma vinculada** ("bonded weapon"):
`MeleeWeapon_MonoSwordBladelink.label` = mono-espada **vinculada**,
`WeaponsMeleeBladelink.label` = armas de filo vinculadas,
`BladelinkEquipWarning` = "la **IA** del arma", the bladelink ThingDef
descriptions = "una **IA** incorporada" / "el arma inteligente", and
`AIPersonaCore.label` = **núcleo de IA** (not "núcleo de persona"). PWU
follows this throughout: the gizmo is "Personalizar IA", not "Personalizar
persona". This is the single most important es rule — a literal "persona"
anywhere in a value is a bug. (It legitimately appears in EN comments,
which quote the English verbatim.)

**Spanish needs no coinage for the two terms ja and zh had to invent.** Both
halves of this mod's central phrase are vanilla es: `CustomizeIdeoligion` =
"**Personalizar** ideoligión" gives customize = personalizar /
personalización, and *vinculada* covers bladelink. So
`PWU_BladelinkCustomization.label` is composed, not coined:
**personalización de armas vinculadas**. A native reviewer might prefer a
terser "personalización de armas con IA"; flagged in the 2026-07-29 commit —
do not silently flip it either way.

## Vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| persona (the onboard mind) | la IA | la persona, el personaje | `BladelinkEquipWarning` = "la IA del arma"; *persona* means "person" in Spanish |
| persona weapon / bladelink weapon | arma vinculada (label suffix vinculada; category: armas de filo vinculadas) | arma persona, arma de enlace | Royalty `MeleeWeapon_*Bladelink.label`, Core `WeaponsMeleeBladelink.label` |
| trait (persona weapon) | característica | rasgo | Royalty `Stat_Thing_PersonaWeaponTrait_Label` **and** `BladelinkEquipWarningTraits`; *rasgo* is Odyssey's unique-weapon word AND Core's pawn-trait word |
| bond (noun / verb) | vínculo / vincularse, vinculada | enlace, unión | Core `BladelinkAlreadyBonded*`, `LetterBladelinkWeaponBonded` |
| AI persona core | núcleo de IA | núcleo de persona, núcleo de personalidad | Core `AIPersonaCore.label` |
| wielder / bearer | portador | usuario, empuñador | Royalty WeaponTraitDef descs, consistently |
| techprint | tecnoplano | plano técnico, anteproyecto | Core `TechprintLabel` = "tecnoplano ({PROJECT_label})" |
| monosword / plasmasword / zeushammer | mono-espada / espada de plasma / martillo de Zeus | monoespada, plasmaespada, zeusmartillo | Royalty weapon labels — es translates and hyphenates mono-espada |
| longsword / handle / edge / point | espada larga / empuñadura / filo / punta | mandoble | Core + Royalty `tools.*.label` |
| mechanite | mecanita(s) | nanomáquina, mecanito | Core `FibrousMechanites`, Royalty monosword desc. Vanilla es is gender-inconsistent here ("las mecanitas" but also "mecanitas fibrosos"); PWU uses feminine, matching the dominant Royalty desc |
| Crafting (the skill) | Fabricación | artesanía, manufactura | Core `Crafting.label` |
| bill (work bill) | proyecto (add-bill menu: Añadir proyecto) | pedido, factura, encargo | Core `TabBills` = Proyectos, `AddBill` = Añadir proyecto. **Collides with "research project" (proyecto de investigación)** — keep the qualifier whenever both could be meant |
| pawn | personaje | peón, muñeco | Core `Stat_Recipe_WorkSpeedStat_Desc` = "característica del personaje" |
| Empire (faction) | imperio destrozado | imperio alone | Royalty `Empire.label` |
| Empire traders | comerciantes imperiales | comerciantes del imperio | Royalty `Orbital_Empire.label` = comerciante imperial |
| freewielder (trait label) | liberal | libre, sin vínculo | Royalty `NeverBond.label` — quote it as `"liberal"` when naming it |
| stopping power / burst count / burst speed | Potencia de parada / Tiros por ráfaga / Cadencia de tiro | Poder de parada | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| armor penetration / damage / accuracy | Penetración de blindaje / Daño / Precisión | Penetración de armadura | Core `ArmorPenetration`, `Damage`, `Accuracy` |
| EMP / EMP stun | PEM / Aturdido por PEM | EMP | Core `StunnedByEMP`, Royalty zeushammer desc — es localizes the acronym |
| gizmo button | botón de comando | gizmo, artilugio | no vanilla es `Gizmo*` key exists; `Command*Desc` establishes "comando" |

## Worked rewrites tied to PWU's own strings

- `PWU_RequiresWorkbench` ("requires a {0}") drops the article: `requiere
  {0}`. Necessary, not stylistic: `{0}` comes from `ResolveWorkbenchLabel`,
  which returns an arbitrary member of a VEF-expandable bench set, so the
  noun is genuinely unknown at write time.
- `PWU_RequiresMinimumQuality` quotes the label: `requiere calidad "{0}" o
  mejor`. Quality labels are **adjectives**, and vanilla es is itself
  gender-inconsistent about them (`QualityCategory_Good` = bueno
  *masculine*, `QualityCategory_Legendary` = legendaria *feminine*), so no
  unquoted form agrees with feminine *calidad* for all seven tiers. Same
  treatment in `PWU_CostTableNotApplicableDesc` (`fijado en "{0}"`).
- The one place an inflected article IS used is `en la {1}` in the four
  `PWU_Enable*RecipeDesc` strings. Deliberate and safe: `{1}` is hard-bound
  to `PWU_ThingDefOf.FabricationBench.label` in `PWU_Mod.cs`, which es
  renders **mesa de ensamblaje** (feminine, and the one injected noun
  actually present in the Female table). Do not generalize it to an unpinned
  placeholder.

## Further Spanish notes

**Landmine — the persona-core recipe's research prerequisite.**
`PWU_UI.xml`'s translator comment calls it "machine persuasion (vanilla
research label)". The def is `ShipComputerCore`, and es renders it
**persuasión de IA** — "AI persuasion", not "machine persuasion". Close
enough to the English hint to be seductive, still not a literal match, and
the noun is *IA* rather than *máquina*, which matters because es reuses IA
for the weapon persona. Resolve the defName through the tar every time.

**Kill tracker vs kill memory, and the haul planner.** English's "kill
tracker" (toggle label) and "kill memory" (prose) split into **registro de
muertes** and **memoria de muertes** respectively — es keeps the
distinction rather than collapsing it the way German does, because both
read naturally against `PWU_TabMemory` = Memoria. Haul planner modes are
mod-coined with no vanilla anchor: **Secuencial / Barrido / Exhaustivo**.

## Workshop title (2026-08-18, machine-assisted, pending native review)

**Armas vinculadas sin ataduras** (= `PWU_SettingsCategory`, coupled to line 1
of `.steamworkshop/Description/Spanish.txt`). Vanilla es renders persona
weapons through the adjective *vinculada* (mono-espada vinculada; category
"armas de filo vinculadas"), so "armas vinculadas" is the searchable anchor.
"sin ataduras" renders "Unbound" as deliberate wordplay against
vincular = to bond. Sentence case per Spanish title norms.
