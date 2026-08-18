# French — Persona Weapons Unbound glossary

Grounded in PWU's own 2026-07-29 fr generation (French had no preseed from a
sibling mod; no native review yet). Family-shared engine mechanics, style
rules, and vanilla-grounded common vocabulary live in `l10n/languages/
French.md` — this file holds only what is specific to Persona Weapons
Unbound.

**Correction flag:** the 2026-07-29 pass (this repo and UWU) believed
`LanguageWorker_French.PostProcessed` elides/contracts *after* argument
substitution, so writing `de {0}` "self-repairs" to `d'{0}` at runtime for a
vowel-initial injected value. BTG's 2026-08-10 pass, re-verified directly
against the 1.6 assembly, **reverses this**: `PostProcessed` runs at load,
*before* substitution, so elision never fires across a `{0}`/`[symbol]` —
see `l10n/languages/French.md`'s Engine mechanics section. This does not
break any of PWU's shipped strings — none of the worked rewrites below
actually relied on the self-repair claim, they all restructure to avoid an
elidable particle sitting directly before a placeholder regardless — but do
not reintroduce the "self-repairing" framing in future generation passes.

## Coined / mod-specific terms

**French needs no coinage for either central term, but it does need a
choice.** Vanilla fr renders "persona weapon" four different ways:
`WeaponsMeleeBladelink.label` = **armes intelligentes** (the official
*category* name), the individual labels use the adjective
**conscient(e)** (`MeleeWeapon_MonoSwordBladelink.label` = "épée
mono-moléculaire consciente"), `BladelinkAlreadyBondedDialog` says "arme
intelligente", and `LetterBladelinkWeaponBondedLabel` keeps the English —
"Lien **Bladelink** : {PAWN_labelShort}" (the only value in the whole
corpus that does). PWU uses **arme intelligente** for the class, because it
is the ThingCategoryDef label and therefore the one term the game itself
presents as the category's name. For the onboard mind, **la conscience** is
dominant (every bladelink ThingDef description says "Cette arme a une
conscience qui ne peut se lier qu'à une seule et unique personne", and
`BladelinkEquipWarning` says "la conscience de l'arme"). Note
`AIPersonaCore.label` = "noyau IA de **personnalité**" and
`NeverBond.description` says "La **personnalité** de cette arme" — so
*personnalité* is vanilla too, and a native reviewer may prefer it. PWU
keeps conscience for the mind and reserves personnalité for the core's
fixed label; flagged in the 2026-07-29 commit, do not silently flip it
either way. "Customize" is **personnaliser / personnalisation** (Ideology
`CustomizeIdeoligion` = "Personnalisez votre idéoligion").

**French diverges on trait — but in the opposite direction from every other
language checked.** Royalty's `Stat_Thing_PersonaWeaponTrait_Label`,
Odyssey's `Stat_ThingUniqueWeaponTrait_Label` and Core's `WeaponTraits` all
say plain **Traits** (`WeaponTraits` = "Traits d'arme"), while Core's
*pawn* trait header `<Traits>` is **"Éléments marquants :"**. So the bare
word "trait" is the weapon word here and the pawn word is the special one.
Royalty ships one outlier: `BladelinkEquipWarningTraits` = "L'arme possède
les **caractéristiques** suivantes"; it is a paraphrase in running prose,
not the term. Use **trait**.

## Vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| persona weapon / bladelink weapon | arme intelligente (label adjective conscient/consciente) | arme persona, arme à lien de lame | Core `WeaponsMeleeBladelink.label` = armes intelligentes; Royalty `MeleeWeapon_*Bladelink.label` |
| persona (the onboard mind) | la conscience | l'âme, l'esprit | Royalty bladelink descs, `BladelinkEquipWarning` = "la conscience de l'arme" |
| trait (weapon) | trait | caractéristique, éléments marquants (that's the *pawn* word) | see the divergence note above |
| bond (noun / verb) | lien / se lier, lié | attache, liaison | Royalty `LetterBladelinkWeaponBonded`, `BladelinkAlreadyBonded*`, Core `BondedTo` = "Lié à" |
| AI persona core | noyau IA de personnalité | noyau de conscience, cœur IA | Core `AIPersonaCore.label` |
| wielder / bearer | porteur | manieur, utilisateur | Royalty bladelink descs ("lié à un porteur") |
| techprint | schéma technique | plan technique, tirage tech | Core `TechprintLabel` = "schéma technique ({PROJECT_label})" |
| monosword / plasmasword / zeushammer | épée mono-moléculaire / épée plasmique / marteau de Zeus | monoépée, épée à plasma | Royalty weapon labels (persona forms append conscient/consciente) |
| longsword / warhammer / mace | épée longue / marteau de guerre / masse | | Core labels |
| handle / hilt / edge / point | poignée (marteau: manche) / tranchant / pointe | manche for a sword | Royalty `MeleeWeapon_*Bladelink.tools.*.label` |
| mechanite | mécanites | nanomachines, mécanites (sg. in prose) | Core + Royalty |
| Crafting (the skill) | artisanat | fabrication, façonnage | Core `Crafting.label` |
| bill (work bill) | tâche (add-bill menu: Ajouter une tâche) | facture, commande, ordre | Core `TabBills` = Tâches, `AddBill` = Ajouter une tâche |
| ingredients / hauling | ingrédients / transport | composants, portage | Core `Ingredients`, `WorkTagHauling` |
| pawn / colonist | personnage / colon | pion, colonisateur | Core `StatsReport_CharacterQuality` = "Qualité du personnage", `Colonist` = colon |
| Empire (faction) | empire brisé | empire alone | Royalty `Empire.label` |
| freewielder (trait label) | porteur libre | libre porteur, sans lien | Royalty `NeverBond.label` — quote it as `« porteur libre »` when naming it |
| stopping power / burst count / burst speed | Puissance d'arrêt / Nombre de tirs par rafale / Cadence de tir | Force d'arrêt | Core `StoppingPower`, `BurstShotCount`, `BurstShotFireRate` |
| armor penetration / damage / accuracy | Pénétration d'armure / Dégâts / Précision | Perforation, Justesse | Core `ArmorPenetration`, `Damage`, `Accuracy` |
| cut / stab (DamageDef) | taillade / blessure par lame | coupure, estocade | Core DamageDefs |
| EMP / EMP stun | IEM / Étourdi par une IEM | EMP | Core `StunnedByEMP`, Royalty zeushammer desc — fr localizes the acronym |
| gizmo button | bouton de commande | gadget, gizmo | no vanilla fr `Gizmo*` key exists; `Command*Desc` establishes "commande" |
| bladelink customization (this mod's research) | personnalisation des armes intelligentes | | composed from two grounded terms — see below |

## Worked rewrites tied to PWU's own strings

- `PWU_RequiresWorkbench` ("requires a {0}") drops the article: `nécessite
  {0}`. `{0}` comes from `ResolveWorkbenchLabel` over a VEF-expandable bench
  set, so the noun is genuinely unknown. Vanilla sanctions the bare form —
  `NeedResearchBenchDesc` = "Ce projet nécessite que vous construisiez
  {1}."
- `PWU_RequiresMinimumQuality` quotes the label: `nécessite la qualité "{0}"
  ou mieux`. Quality tiers are adjectives (horrible, médiocre, normal, bon,
  excellent, **merveille** — a feminine *noun* — légendaire), so no
  unquoted form agrees with feminine *qualité* across all seven. Same
  treatment in `PWU_CostTableNotApplicableDesc` (`réglée sur "{0}"`).
- Bail/error messages use `Personnalisation de "{0}" interrompue : …` —
  quoting blocks elision *and* removes the need for an article.
- After a **colon** no quotes are needed (`Personnaliser la conscience :
  {0}`, `Qualité minimale de l'arme : {0}`) — matches vanilla
  `ResearchFinished` = "Recherche terminée : {0}".
- The one place an elided article IS hard-coded is `à l'{1}` in the four
  `PWU_Enable*RecipeDesc` strings. Deliberate and safe: `{1}` is bound to
  `PWU_ThingDefOf.FabricationBench.label` in `PWU_Mod.cs`, which fr renders
  **atelier de fabrication** — vowel-initial, so `l'` is always right. Do
  not generalize it to an unpinned placeholder (a consonant-initial value
  would give "l'plastacier"), and note you cannot write `à le {1}` instead
  because `ALe` would turn it into "au atelier".
- PWU's three weapon recipes originally said "Convertissez-la ensuite…" and
  were rewritten to "**Il faut ensuite la convertir…**" because the
  enclitic `-la` immediately before a vowel-initial word ("ensuite") would
  have elided into "Convertissez-l'ensuite" (`la c` is safe, and it avoids
  `à le` too).
- Fixed French nouns inflect normally (une épée longue, une arme de
  qualité merveille) — only *injected* values need the workaround.

## Further French notes

**`PWU_BladelinkCustomization.label` is composed, not coined.**
personnalisation and armes intelligentes are both vanilla fr, so no
invention was needed. It is longer than the median fr research label
(usinage, longues lames), so a native reviewer may prefer a terser
"personnalisation des consciences"; flagged in the 2026-07-29 commit, do
not silently flip it either way.

**Landmine — the persona-core recipe's research prerequisite.**
`PWU_UI.xml`'s translator comment calls it "machine persuasion (vanilla
research label)". The def is `ShipComputerCore`, and fr renders it **noyau
central de l'ordinateur de bord** — nothing like the English. Its
description does give the sense ("Vous apprend à brider une IA
existante…"), but the label does not. Resolve the defName through the tar
every time.

**Kill tracker vs kill memory, and the haul planner.** English's "kill
tracker" (toggle label) and "kill memory" (prose) split into **registre des
victimes** and **mémoire des victimes** — fr keeps the distinction like
Spanish rather than collapsing it like German, because both read naturally
against `PWU_TabMemory` = Mémoire. Haul planner modes are mod-coined with
no vanilla anchor: **Séquentiel / Balayage / Exhaustif**.

## Workshop title (2026-08-18, machine-assisted, pending native review)

**Armes intelligentes libérées** (= `PWU_SettingsCategory`, coupled to line 1
of `.steamworkshop/Description/French.txt`). "armes intelligentes" is Royalty
fr's own `WeaponsMeleeBladelink.label` term; "libérées" renders "Unbound".
The description quotes the research name « personnalisation des armes
intelligentes » lowercase, matching the DefInjected label verbatim.
