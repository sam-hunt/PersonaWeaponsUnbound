using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using RimWorld;
using Verse;
using Xunit;

namespace PersonaWeaponsUnbound.Tests
{
    // Covers the pure half of the skill prerequisite: settings defaults, the
    // skill/expertise pairing, the recipe index and tech-tier table,
    // requirement derivation under each kind, and the Vanilla Skills Expanded
    // fallback. Pawn evaluation needs a live skill
    // tracker (ModsConfig, Biotech aptitude paths) and stays in-game.
    //
    // VSE is never available under the headless harness (ModsConfig has no
    // data, so the integration's static ctor bails out), which is exactly the
    // state the fallback tests need.
    //
    // PWU_Mod.Settings is process-global, so every test that reads it runs
    // inside a SettingsScope, and every class that touches that static shares
    // the "PWU_Mod.Settings" xunit collection so they never run in parallel.
    [Collection("PWU_Mod.Settings")]
    public class SkillCheckRulesTests
    {
        private static readonly SkillDef Intellectual =
            new SkillDef { defName = "Intellectual", skillLabel = "intellectual" };
        private static readonly SkillDef Crafting =
            new SkillDef { defName = "Crafting", skillLabel = "crafting" };

        public SkillCheckRulesTests()
        {
            TestHelpers.BootstrapHeadlessGame();
            SkillDefOf.Intellectual = Intellectual;
            SkillDefOf.Crafting = Crafting;
        }

        // ThingDef via GetUninitializedObject: `new ThingDef()` runs BuildableDef's
        // ctor, which reaches Unity (see TestHelpers.MakeDef). RecipeDef and
        // ThingDefCountClass are plain Def/data classes and construct directly.
        private static ThingDef MakeWeapon(TechLevel techLevel)
        {
            var def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            def.defName = "TestWeapon_" + Guid.NewGuid().ToString("N");
            def.techLevel = techLevel;
            // ThingDef.IsWeapon is category Item + verbs or tools, minus apparel.
            def.category = ThingCategory.Item;
            def.tools = new List<Tool> { new Tool { label = "test", power = 1f, cooldownTime = 1f } };
            return def;
        }

        private static RecipeDef RecipeFor(ThingDef product, params SkillRequirement[] requirements)
        {
            return new RecipeDef
            {
                defName = "TestRecipe_" + Guid.NewGuid().ToString("N"),
                products = new List<ThingDefCountClass> { new ThingDefCountClass(product, 1) },
                skillRequirements = requirements.Length == 0 ? null : new List<SkillRequirement>(requirements),
            };
        }

        private static PWU_Settings SettingsFor(
            SkillCheckSubject subject,
            SkillCheckKind kind,
            SkillCheckSkill skill = SkillCheckSkill.Intellectual,
            int flatLevel = 10)
        {
            var settings = new PWU_Settings();
            settings.ResetToDefaults();
            settings.skillCheckSubject = subject;
            settings.skillCheckSkill = skill;
            settings.skillCheckKind = kind;
            settings.skillCheckMinimumLevel = flatLevel;
            return settings;
        }

        private sealed class SettingsScope : IDisposable
        {
            private readonly PWU_Settings previous;

            public SettingsScope(PWU_Settings settings)
            {
                previous = PWU_Mod.Settings;
                PWU_Mod.Settings = settings;
            }

            public void Dispose()
            {
                PWU_Mod.Settings = previous;
            }
        }

        [Fact]
        public void Defaults_AreOptOut_AndIntellectual()
        {
            var settings = new PWU_Settings();
            Assert.Equal(SkillCheckSubject.None, settings.skillCheckSubject);
            Assert.Equal(SkillCheckSkill.Intellectual, settings.skillCheckSkill);
            Assert.Equal(SkillCheckKind.RecipeOrTechTier, settings.skillCheckKind);
            Assert.Equal(10, settings.skillCheckMinimumLevel);

            settings.skillCheckSubject = SkillCheckSubject.BestAnywhere;
            settings.skillCheckSkill = SkillCheckSkill.Crafting;
            settings.skillCheckKind = SkillCheckKind.Expertise;
            settings.skillCheckMinimumLevel = 3;
            settings.ResetToDefaults();
            Assert.Equal(SkillCheckSubject.None, settings.skillCheckSubject);
            Assert.Equal(SkillCheckSkill.Intellectual, settings.skillCheckSkill);
            Assert.Equal(SkillCheckKind.RecipeOrTechTier, settings.skillCheckKind);
            Assert.Equal(10, settings.skillCheckMinimumLevel);
        }

        [Fact]
        public void TechTierTable_IsMonotonicAndCoversEveryTier()
        {
            int previous = int.MinValue;
            foreach (TechLevel tier in Enum.GetValues(typeof(TechLevel)))
            {
                int level = SkillCheckRules.TechTierMinimumSkill(tier);
                Assert.InRange(level, SkillCheckRules.MinFlatLevel, SkillCheckRules.MaxFlatLevel);
                Assert.True(level >= previous, $"{tier} ({level}) is below the previous tier ({previous})");
                previous = level;
            }
            // Vanilla persona weapons are Ultra; Archotech lands on VSE's threshold.
            Assert.Equal(12, SkillCheckRules.TechTierMinimumSkill(TechLevel.Ultra));
            Assert.Equal(SkillCheckRules.ExpertiseFallbackLevel,
                SkillCheckRules.TechTierMinimumSkill(TechLevel.Archotech));

            TechLevel[] tiers = SkillCheckRules.TechTiers;
            Assert.Equal(TechLevel.Neolithic, tiers[0]);
            Assert.Equal(TechLevel.Archotech, tiers[tiers.Length - 1]);
            for (int i = 1; i < tiers.Length; i++)
            {
                Assert.True(tiers[i] > tiers[i - 1]);
                Assert.True(SkillCheckRules.TechTierMinimumSkill(tiers[i])
                    > SkillCheckRules.TechTierMinimumSkill(tiers[i - 1]));
            }
        }

        [Fact]
        public void WeaponTechLevel_FallsBackToThePairedDef()
        {
            ThingDef baseDef = MakeWeapon(TechLevel.Spacer);
            ThingDef personaDef = MakeWeapon(TechLevel.Undefined);
            Assert.Equal(TechLevel.Spacer, SkillCheckRules.GetWeaponTechLevel(baseDef, personaDef, personaDef));
            Assert.Equal(TechLevel.Spacer, SkillCheckRules.GetWeaponTechLevel(baseDef, personaDef, baseDef));
            Assert.Equal(TechLevel.Undefined, SkillCheckRules.GetWeaponTechLevel(null, personaDef, personaDef));
        }

        [Fact]
        public void RecipeIndex_TakesLowestLevelAcrossRecipes_IgnoresNonWeapons()
        {
            ThingDef weapon = MakeWeapon(TechLevel.Ultra);
            ThingDef easy = MakeWeapon(TechLevel.Industrial);
            var notWeapon = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            notWeapon.defName = "TestResource";
            notWeapon.category = ThingCategory.Item;

            WeaponRecipeIndex.Rebuild(new[]
            {
                RecipeFor(weapon, new SkillRequirement { skill = Crafting, minLevel = 12 }),
                RecipeFor(weapon, new SkillRequirement { skill = Crafting, minLevel = 8 },
                    new SkillRequirement { skill = Intellectual, minLevel = 9 }),
                RecipeFor(easy),
                RecipeFor(notWeapon, new SkillRequirement { skill = Crafting, minLevel = 3 }),
            });

            // Two recipes: a 12 and a (8, 9) = 9. "Whoever could craft it" is the 9.
            Assert.True(WeaponRecipeIndex.TryGetRequiredLevel(weapon, out int level));
            Assert.Equal(9, level);
            // Craftable with no requirement records 0 (present, but demands nothing).
            Assert.True(WeaponRecipeIndex.TryGetRequiredLevel(easy, out level));
            Assert.Equal(0, level);
            Assert.False(WeaponRecipeIndex.TryGetRequiredLevel(notWeapon, out _));
            Assert.False(WeaponRecipeIndex.TryGetRequiredLevel(MakeWeapon(TechLevel.Ultra), out _));
            Assert.False(WeaponRecipeIndex.TryGetRequiredLevel(null, out _));
        }

        [Fact]
        public void RecipeKind_UsesRecipeLevel_InTheCheckedSkill()
        {
            ThingDef baseDef = MakeWeapon(TechLevel.Ultra);
            ThingDef personaDef = MakeWeapon(TechLevel.Ultra);
            WeaponRecipeIndex.Rebuild(new[]
            {
                RecipeFor(baseDef, new SkillRequirement { skill = Crafting, minLevel = 10 }),
            });
            using (new SettingsScope(SettingsFor(
                SkillCheckSubject.CustomizingPawn, SkillCheckKind.RecipeOrTechTier, SkillCheckSkill.Intellectual)))
            {
                var req = SkillCheckRules.GetRequirement(baseDef, personaDef, TechLevel.Ultra);
                Assert.Same(Intellectual, req.Skill.skill);
                Assert.Equal(10, req.Skill.minLevel);
            }
            using (new SettingsScope(SettingsFor(
                SkillCheckSubject.CustomizingPawn, SkillCheckKind.RecipeOrTechTier, SkillCheckSkill.Crafting)))
            {
                var req = SkillCheckRules.GetRequirement(baseDef, personaDef, TechLevel.Ultra);
                Assert.Same(Crafting, req.Skill.skill);
                Assert.Equal(10, req.Skill.minLevel);
            }
        }

        [Fact]
        public void RecipeKind_PrefersBaseRecipe_ThenPersona_ThenTechTier()
        {
            ThingDef baseDef = MakeWeapon(TechLevel.Spacer);
            ThingDef personaDef = MakeWeapon(TechLevel.Spacer);
            ThingDef orphanPersona = MakeWeapon(TechLevel.Medieval);
            WeaponRecipeIndex.Rebuild(new[]
            {
                RecipeFor(baseDef, new SkillRequirement { skill = Crafting, minLevel = 4 }),
                RecipeFor(personaDef, new SkillRequirement { skill = Crafting, minLevel = 9 }),
            });
            using (new SettingsScope(SettingsFor(
                SkillCheckSubject.CustomizingPawn, SkillCheckKind.RecipeOrTechTier)))
            {
                Assert.Equal(4, SkillCheckRules.GetRequirement(baseDef, personaDef, TechLevel.Spacer).Skill.minLevel);
                Assert.Equal(9, SkillCheckRules.GetRequirement(null, personaDef, TechLevel.Spacer).Skill.minLevel);
                Assert.Equal(SkillCheckRules.TechTierMinimumSkill(TechLevel.Medieval),
                    SkillCheckRules.GetRequirement(null, orphanPersona, TechLevel.Medieval).Skill.minLevel);
            }
        }

        [Fact]
        public void RecipeKind_CraftableWithoutRequirement_IsEmpty()
        {
            ThingDef baseDef = MakeWeapon(TechLevel.Ultra);
            WeaponRecipeIndex.Rebuild(new[] { RecipeFor(baseDef) });
            using (new SettingsScope(SettingsFor(
                SkillCheckSubject.CustomizingPawn, SkillCheckKind.RecipeOrTechTier)))
            {
                Assert.True(SkillCheckRules.GetRequirement(baseDef, null, TechLevel.Ultra).IsEmpty);
            }
        }

        [Fact]
        public void TechTierKind_UsesTierTable_IgnoringRecipe()
        {
            ThingDef baseDef = MakeWeapon(TechLevel.Ultra);
            WeaponRecipeIndex.Rebuild(new[]
            {
                RecipeFor(baseDef, new SkillRequirement { skill = Crafting, minLevel = 3 }),
            });
            using (new SettingsScope(SettingsFor(
                SkillCheckSubject.CustomizingPawn, SkillCheckKind.TechTier, SkillCheckSkill.Crafting, flatLevel: 1)))
            {
                var req = SkillCheckRules.GetRequirement(baseDef, null, TechLevel.Ultra);
                Assert.Same(Crafting, req.Skill.skill);
                Assert.Equal(SkillCheckRules.TechTierMinimumSkill(TechLevel.Ultra), req.Skill.minLevel);
            }
        }

        [Fact]
        public void SkillPairing_MapsEachSkillToItsDefAndExpertise()
        {
            Assert.Same(Intellectual, SkillCheckRules.SkillDefFor(SkillCheckSkill.Intellectual));
            Assert.Same(Crafting, SkillCheckRules.SkillDefFor(SkillCheckSkill.Crafting));
            Assert.Equal(VanillaSkillsExpandedIntegration.HackerDefName,
                SkillCheckRules.ExpertiseDefNameFor(SkillCheckSkill.Intellectual));
            Assert.Equal(VanillaSkillsExpandedIntegration.WeaponsmithDefName,
                SkillCheckRules.ExpertiseDefNameFor(SkillCheckSkill.Crafting));
        }

        [Fact]
        public void FlatKind_UsesSliderLevel_InTheSelectedSkill()
        {
            using (new SettingsScope(SettingsFor(
                SkillCheckSubject.CustomizingPawn, SkillCheckKind.FlatMinimum,
                SkillCheckSkill.Intellectual, flatLevel: 12)))
            {
                var req = SkillCheckRules.GetRequirement(null, null, TechLevel.Ultra);
                Assert.Null(req.ExpertiseDefName);
                Assert.Same(Intellectual, req.Skill.skill);
                Assert.Equal(12, req.Skill.minLevel);
            }
            using (new SettingsScope(SettingsFor(
                SkillCheckSubject.CustomizingPawn, SkillCheckKind.FlatMinimum,
                SkillCheckSkill.Crafting, flatLevel: 7)))
            {
                var req = SkillCheckRules.GetRequirement(null, null, TechLevel.Ultra);
                Assert.Same(Crafting, req.Skill.skill);
                Assert.Equal(7, req.Skill.minLevel);
            }
        }

        [Fact]
        public void FlatKind_ClampsOutOfRangeLevel_AndZeroIsEmpty()
        {
            using (new SettingsScope(SettingsFor(
                SkillCheckSubject.CustomizingPawn, SkillCheckKind.FlatMinimum, flatLevel: 99)))
            {
                Assert.Equal(SkillCheckRules.MaxFlatLevel,
                    SkillCheckRules.GetRequirement(null, null, TechLevel.Ultra).Skill.minLevel);
            }
            using (new SettingsScope(SettingsFor(
                SkillCheckSubject.CustomizingPawn, SkillCheckKind.FlatMinimum, flatLevel: 0)))
            {
                Assert.True(SkillCheckRules.GetRequirement(null, null, TechLevel.Ultra).IsEmpty);
            }
        }

        [Theory]
        [InlineData(SkillCheckSkill.Intellectual)]
        [InlineData(SkillCheckSkill.Crafting)]
        public void ExpertiseKind_WithoutVse_FallsBackToFlatFifteen_InTheSelectedSkill(SkillCheckSkill skill)
        {
            Assert.False(VanillaSkillsExpandedIntegration.Available);
            Assert.False(SkillCheckRules.ExpertiseAvailableFor(skill));
            using (new SettingsScope(SettingsFor(
                SkillCheckSubject.BestAnywhere, SkillCheckKind.Expertise, skill, flatLevel: 4)))
            {
                Assert.True(SkillCheckRules.ExpertiseFallbackActive);
                Assert.Equal(SkillCheckKind.FlatMinimum, SkillCheckRules.EffectiveKind(out int level));
                Assert.Equal(SkillCheckRules.ExpertiseFallbackLevel, level);

                var req = SkillCheckRules.GetRequirement(null, null, TechLevel.Ultra);
                Assert.Null(req.ExpertiseDefName);
                Assert.Same(SkillCheckRules.SkillDefFor(skill), req.Skill.skill);
                Assert.Equal(SkillCheckRules.ExpertiseFallbackLevel, req.Skill.minLevel);

                // The stored selection is left alone so installing VSE restores it.
                Assert.Equal(SkillCheckKind.Expertise, PWU_Mod.Settings.skillCheckKind);
                Assert.Equal(4, PWU_Mod.Settings.skillCheckMinimumLevel);
            }
        }

        [Fact]
        public void EffectiveKind_PassesThroughWhenNoFallback()
        {
            using (new SettingsScope(SettingsFor(
                SkillCheckSubject.BestOnMap, SkillCheckKind.FlatMinimum, flatLevel: 7)))
            {
                Assert.False(SkillCheckRules.ExpertiseFallbackActive);
                Assert.Equal(SkillCheckKind.FlatMinimum, SkillCheckRules.EffectiveKind(out int level));
                Assert.Equal(7, level);
                Assert.True(SkillCheckRules.Enabled);
            }
            using (new SettingsScope(SettingsFor(SkillCheckSubject.None, SkillCheckKind.FlatMinimum)))
            {
                Assert.False(SkillCheckRules.Enabled);
            }
        }

        [Fact]
        public void PawnSatisfies_NullPawn_IsFalse()
        {
            var req = new SkillCheckRules.Requirement
            {
                Skill = new SkillRequirement { skill = Intellectual, minLevel = 1 },
            };
            Assert.False(SkillCheckRules.PawnSatisfies(null, req));
        }
    }
}
