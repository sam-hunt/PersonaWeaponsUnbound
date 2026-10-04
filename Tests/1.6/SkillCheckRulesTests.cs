using System;
using RimWorld;
using Verse;
using Xunit;

namespace PersonaWeaponsUnbound.Tests
{
    // Covers the pure half of the skill prerequisite: settings defaults, the
    // skill/expertise pairing, requirement derivation under each kind, and the
    // Vanilla Skills Expanded fallback. Pawn evaluation needs a live skill
    // tracker (ModsConfig, Biotech aptitude paths) and stays in-game.
    //
    // VSE is never available under the headless harness (ModsConfig has no
    // data, so the integration's static ctor bails out), which is exactly the
    // state the fallback tests need.
    //
    // PWU_Mod.Settings is process-global, so every test that reads it runs
    // inside a SettingsScope; xunit runs the facts of one class serially.
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
            Assert.Equal(SkillCheckKind.FlatMinimum, settings.skillCheckKind);
            Assert.Equal(10, settings.skillCheckMinimumLevel);

            settings.skillCheckSubject = SkillCheckSubject.BestAnywhere;
            settings.skillCheckSkill = SkillCheckSkill.Crafting;
            settings.skillCheckKind = SkillCheckKind.Expertise;
            settings.skillCheckMinimumLevel = 3;
            settings.ResetToDefaults();
            Assert.Equal(SkillCheckSubject.None, settings.skillCheckSubject);
            Assert.Equal(SkillCheckSkill.Intellectual, settings.skillCheckSkill);
            Assert.Equal(SkillCheckKind.FlatMinimum, settings.skillCheckKind);
            Assert.Equal(10, settings.skillCheckMinimumLevel);
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
                var req = SkillCheckRules.GetRequirement();
                Assert.Null(req.ExpertiseDefName);
                Assert.Same(Intellectual, req.Skill.skill);
                Assert.Equal(12, req.Skill.minLevel);
            }
            using (new SettingsScope(SettingsFor(
                SkillCheckSubject.CustomizingPawn, SkillCheckKind.FlatMinimum,
                SkillCheckSkill.Crafting, flatLevel: 7)))
            {
                var req = SkillCheckRules.GetRequirement();
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
                Assert.Equal(SkillCheckRules.MaxFlatLevel, SkillCheckRules.GetRequirement().Skill.minLevel);
            }
            using (new SettingsScope(SettingsFor(
                SkillCheckSubject.CustomizingPawn, SkillCheckKind.FlatMinimum, flatLevel: 0)))
            {
                Assert.True(SkillCheckRules.GetRequirement().IsEmpty);
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

                var req = SkillCheckRules.GetRequirement();
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
