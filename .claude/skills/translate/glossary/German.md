# German — Persona Weapons Unbound glossary

Grounded in PWU's own 2026-07-28 de generation (German had no preseed from a
sibling mod; no native review yet). Family-shared engine mechanics
(including the case/lookup mechanism), style rules, and vanilla-grounded
common vocabulary live in `l10n/languages/German.md` — this file holds only
what is specific to Persona Weapons Unbound.

**Note on the worked rewrites below:** they were written against the
2026-07-28 belief that `GrammarResolverSimple` implements no `lookup`
function, so restructuring to avoid any article/adjective agreement on an
injected value was the *only* option. `l10n/languages/German.md`'s Engine
mechanics section records a 2026-08-10 correction (re-verified against the
1.6 assembly): `lookup`/`decline` **is** reachable from a plain Keyed string
via `{lookup: {0}; decline; N}`, backed by Core's `WordInfo/decline.txt` /
`plural_decline.txt`. This does not invalidate the rewrites below — every
value they restructure around is a **mod-coined or unpinned** label (a
workbench name from `ResolveWorkbenchLabel`, a quality tier), and those are
never in the vanilla `decline.txt` table regardless of which mechanism is
used — but a future pass could reconsider a *fixed vanilla noun* in an
oblique slot using `lookup`/`decline` instead of restructuring, if desired.

## Coined / mod-specific terms

**German needs no coinage for either of the two terms ja and zh had to
invent.** Royalty de calls the weapon class **Personawaffe**
(`WeaponsMeleeBladelink.label` = Personawaffen, and
`BladelinkAlreadyBondedDialog` says "sich immer nur mit einer Personawaffe
verbinden"), and Core de renders "customize" as **anpassen**
(`CustomizeIdeoligion` = Ideologie anpassen). Note the label prefix is
hyphenated (Persona-Monoschwert) while the class noun is a solid compound
(Personawaffe).

**German has NO trait divergence.** Royalty's
`Stat_Thing_PersonaWeaponTrait_Label`, Odyssey's
`Stat_ThingUniqueWeaponTrait_Label`, *and* Core's pawn-trait `<Traits>` are
all **Merkmale**. The usual divergence check still has to be run, it just
comes back the same three times here. A disambiguating "Waffenmerkmal" is
only needed if surrounding context is absent (vanilla itself ships
`StatsReport_WeaponTraits` = Waffenmerkmale for that case).

## Vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| persona weapon / bladelink weapon | Personawaffe (label prefix Persona-) | Klingenverbindung, Bladelink-Waffe | Royalty `WeaponsMeleeBladelink.label`, `MeleeWeapon_*Bladelink.label` |
| persona (the onboard mind) | die Persona | Persönlichkeit, KI | Royalty bladelink descs, `LetterBladelinkWeaponBonded` |
| customize / customization | anpassen / Anpassung | anwenden, individualisieren, konfigurieren | Core `CustomizeIdeoligion` = Ideologie anpassen |
| trait (weapon and pawn alike) | Merkmal / Merkmale | Eigenschaft, Attribut | Royalty `Stat_Thing_PersonaWeaponTrait_Label`, `BladelinkEquipWarningTraits`, Core `<Traits>` — all three agree, see above |
| bond (noun / verb) | Bindung / binden, gebunden | Verbindung, Bund | Royalty `BladelinkAlreadyBonded*`, `LetterBladelinkWeaponBonded` |
| wielder / bearer | Träger | Anwender, Nutzer | Royalty weapon-trait descs |
| persona core / AI persona core | Personakern | Persona-Kern, KI-Kern | Core `AIPersonaCore.label` |
| techprint | Techplan / Techpläne | Techdruck, Blaupause | Core `TechprintLabel` = Techplan ({PROJECT_label}) |
| fabrication bench | Fabrikationstisch | Fertigungstisch, Werkbank alone | Core `FabricationBench.label` |
| advanced components | Hightech-Bauteile | fortschrittliche Komponenten | Core `ComponentSpacer.label`; plain components are Bauteile |
| monosword / plasmasword / zeushammer | Monoschwert / Plasmaschwert / Zeushammer | | Royalty weapon labels (persona forms prefix Persona-) |
| longsword / mace / knife / spear | Langschwert / Streitkolben / Messer / Speer | | Core labels |
| mechanite | Mechaniten | Mechanite | Royalty monosword desc |
| Crafting (the skill) | Handwerk | Herstellung, Basteln | Core `Crafting.label` |
| bill / recipe (both) | Auftrag (add-bill menu: Auftrag hinzufügen) | Rezept, Rechnung | Core `TabBills`, `AddBill`, and every `Stat_Recipe_*_Desc` says Auftrag — de collapses bill and recipe into one word |
| ingredients / hauling | Zutaten / Transport | Bestandteile, Schleppen | Core `Ingredients`, `WorkTagHauling` |
| Empire (faction) | zerrüttetes Imperium | Imperium alone | Royalty `Empire.label` |
| freewielder (trait label) | frei schwingend | Freiträger, frei führbar | Royalty `NeverBond.label` — quote it as `'frei schwingend'`; de weapon-trait labels are all lowercase adjectives/participles |
| stopping power / burst count / burst speed | Mannstoppwirkung / Schüsse pro Feuerstoß / Feuerrate | Durchschlagskraft | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| armor penetration / damage / accuracy | Rüstungsdurchdringung / Schaden / Genauigkeit | Panzerdurchdringung, Treffsicherheit | Core `ArmorPenetration`, `Damage`, `Accuracy` |
| cut / stab (DamageDef) | Schnitt / Stich | Schnittwunde, Stichwunde (those are hediffs) | Core DamageDefs |
| EMP stun | Betäubt durch EMP | | Core `StunnedByEMP` |
| gizmo button | Befehlsknopf | Gizmo | no vanilla de `Gizmo*` key exists; Befehlsknopf is the descriptive form |
| bladelink customization (this mod's research) | Personawaffen anpassen | | composed from two grounded terms — see below |

## Worked rewrites tied to PWU's own strings

- `PWU_RequiresWorkbench` ("requires a {0}") drops the article: `erfordert
  {0}`. `{0}` comes from `ResolveWorkbenchLabel`, an arbitrary member of a
  VEF-expandable bench set, so the noun is genuinely unknown at write time —
  never in the vanilla declension tables regardless of mechanism.
- `PWU_RequiresMinimumQuality` becomes `erfordert Qualität {0} oder besser`,
  putting the noun *before* the injected label, because German quality
  labels are adjectives that would have to inflect (vanilla's own
  `NormalQualityOrBetter` = "normale Qualität oder besser" is pre-inflected
  and cannot be templated).
- Bail/error messages use `Anpassung von '{0}' unterbrochen: …` — the `von`
  +quoted-label frame is case-proof.
- The one place an inflected article IS used is `am {1}` in the four
  `PWU_Enable*RecipeDesc` strings. Deliberate and safe: `{1}` is hard-bound
  to `PWU_ThingDefOf.FabricationBench.label` in `PWU_Mod.cs`, which de
  renders **Fabrikationstisch** (masculine), so the dative contraction is
  always correct. Do not "generalize" it, and do not copy the pattern to a
  placeholder whose def isn't pinned.
- Fixed German nouns inflect normally (eine meisterliche Waffe) — only
  *injected* values need the workaround.

## German-specific notes

**`PWU_BladelinkCustomization.label` is composed, not coined.** Personawaffen
+ anpassen are both vanilla de, and the verb-final shape mirrors
`ShipComputerCore.label` = "Maschinenpersona überreden" — the one vanilla
research project also about reprogramming a persona, and therefore the
closest available style anchor. It is longer than the median de research
label, so a native reviewer may prefer a nominal
"Personawaffen-Anpassung"; flagged in the 2026-07-28 commit, do not silently
flip it either way. Because the label is a verb phrase, every string that
injects it quotes it (`Forschung '{0}'`).

**Landmine — the persona-core recipe's research prerequisite.**
`PWU_UI.xml`'s translator comment calls it "machine persuasion (vanilla
research label)". The def is `ShipComputerCore`, and de renders it
**Maschinenpersona überreden** — close enough to the English hint to be
seductive, but still not a literal match ("Maschinenpersona", not
"Maschine"). Resolve the defName through the tar every time.

**Kill tracker vs kill memory.** English uses "kill tracker" as the toggle
label and "kill memory" in prose; de collapses both to
**Tötungsgedächtnis** so the Memory tab reads consistently against
`PWU_TabMemory` = Gedächtnis. The haul planner modes are mod-coined with no
vanilla anchor: **Sequenziell / Sammelgang / Gründlich**.
