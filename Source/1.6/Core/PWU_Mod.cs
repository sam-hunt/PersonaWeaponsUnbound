using RimWorld;
using UnityEngine;
using PersonaWeaponsUnbound.HaulPlanning;
using Verse;

namespace PersonaWeaponsUnbound
{
    public class PWU_Mod : Mod
    {
        public static PWU_Settings Settings { get; private set; }

        private Vector2 settingsScroll;
        private float settingsHeight;

        public PWU_Mod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<PWU_Settings>();
        }

        public override string SettingsCategory() => "PWU_SettingsCategory".Translate();

        public override void WriteSettings()
        {
            base.WriteSettings();
            // Live-apply: the techprint slider takes effect immediately, no restart
            // required (fork spec §7, D4).
            PWU_ResearchDefOf.ApplyTechprintCount();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            float buttonHeight = 30f;
            float buttonGap = 10f;
            Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - buttonHeight - buttonGap);
            Rect buttonRect = new Rect(inRect.x, inRect.yMax - buttonHeight, 200f, buttonHeight);

            float innerWidth = viewRect.width - 16f;
            Rect innerRect = new Rect(0f, 0f, innerWidth, Mathf.Max(settingsHeight, viewRect.height));
            Widgets.BeginScrollView(viewRect, ref settingsScroll, innerRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(new Rect(0f, 0f, innerWidth - 8f, 99999f));
            GameFont prev = Text.Font;

            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("PWU_SettingsProgression".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(12.0f);

            listing.CheckboxLabeled(
                "PWU_RestrictTraitsToDiscovered".Translate(),
                ref Settings.restrictTraitsToDiscovered,
                "PWU_RestrictTraitsToDiscoveredDesc".Translate());

            listing.Gap(24.0f);

            Text.Font = GameFont.Medium;
            listing.Label("PWU_SettingsTraitCosts".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            string baseCostLabel = "PWU_TraitChangeBaseComponentCost".Translate(Settings.traitChangeBaseComponentCost);
            if (Settings.traitChangeBaseComponentCost == 2)
                baseCostLabel += "PWU_DefaultSuffix".Translate();
            listing.Label(baseCostLabel, tooltip: "PWU_TraitChangeBaseComponentCostDesc".Translate());
            Settings.traitChangeBaseComponentCost =
                Mathf.RoundToInt(listing.Slider(Settings.traitChangeBaseComponentCost, 0f, 10f));

            listing.Gap();

            string surchargeThresholdLabel = "PWU_TraitChangeQualitySurchargeThreshold".Translate(
                Settings.traitChangeQualitySurchargeThreshold.GetLabel());
            if (Settings.traitChangeQualitySurchargeThreshold == QualityCategory.Normal)
                surchargeThresholdLabel += "PWU_DefaultSuffix".Translate();
            listing.Label(surchargeThresholdLabel, tooltip: "PWU_TraitChangeQualitySurchargeThresholdDesc".Translate());
            float surchargeThresholdValue = (int)Settings.traitChangeQualitySurchargeThreshold;
            surchargeThresholdValue = listing.Slider(surchargeThresholdValue, 0f, (int)QualityCategory.Legendary);
            Settings.traitChangeQualitySurchargeThreshold = (QualityCategory)Mathf.RoundToInt(surchargeThresholdValue);

            listing.Gap();

            string surchargePerLevelLabel = "PWU_TraitChangeQualitySurchargePerLevel".Translate(
                Settings.traitChangeQualitySurchargePerLevel);
            if (Settings.traitChangeQualitySurchargePerLevel == 1)
                surchargePerLevelLabel += "PWU_DefaultSuffix".Translate();
            listing.Label(surchargePerLevelLabel, tooltip: "PWU_TraitChangeQualitySurchargePerLevelDesc".Translate());
            Settings.traitChangeQualitySurchargePerLevel =
                Mathf.RoundToInt(listing.Slider(Settings.traitChangeQualitySurchargePerLevel, 0f, 5f));

            listing.Gap(12f);

            listing.Label("PWU_CostTableHeader".Translate());
            listing.Gap(4f);
            DrawCostTable(listing);

            listing.Gap(24.0f);

            Text.Font = GameFont.Medium;
            listing.Label("PWU_SettingsPrerequisites".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            string qualityLabel = "PWU_MinimumQuality".Translate(Settings.minimumQuality.GetLabel());
            if (Settings.minimumQuality == QualityCategory.Awful)
                qualityLabel += "PWU_DefaultSuffix".Translate();
            else if (Settings.minimumQuality == QualityCategory.Normal)
                qualityLabel += "PWU_RecommendedSuffix".Translate();
            listing.Label(qualityLabel, tooltip: "PWU_MinimumQualityDesc".Translate());
            float qualityValue = (int)Settings.minimumQuality;
            qualityValue = listing.Slider(qualityValue, 0f, (int)QualityCategory.Legendary);
            Settings.minimumQuality = (QualityCategory)Mathf.RoundToInt(qualityValue);

            listing.Gap();

            listing.CheckboxLabeled(
                "PWU_AllowDefConversion".Translate(),
                ref Settings.allowDefConversion,
                "PWU_AllowDefConversionDesc".Translate());

            listing.Gap();

            listing.CheckboxLabeled(
                "PWU_RequireCustomizationResearch".Translate(),
                ref Settings.requireCustomizationResearch,
                "PWU_RequireCustomizationResearchDesc".Translate());

            listing.Gap();

            string techprintLabel = "PWU_TechprintCount".Translate(Settings.techprintCount);
            if (Settings.techprintCount == 1)
                techprintLabel += "PWU_DefaultSuffix".Translate();
            listing.Label(techprintLabel, tooltip: "PWU_TechprintCountDesc".Translate());
            Settings.techprintCount = Mathf.RoundToInt(listing.Slider(Settings.techprintCount, 0f, 3f));

            listing.Gap(24.0f);

            Text.Font = GameFont.Medium;
            listing.Label("PWU_SettingsCraftingRecipes".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            listing.CheckboxLabeled(
                "PWU_EnableMonoswordRecipe".Translate(),
                ref Settings.enableMonoswordRecipe,
                "PWU_EnableMonoswordRecipeDesc".Translate());

            listing.Gap();

            listing.CheckboxLabeled(
                "PWU_EnablePlasmaswordRecipe".Translate(),
                ref Settings.enablePlasmaswordRecipe,
                "PWU_EnablePlasmaswordRecipeDesc".Translate());

            listing.Gap();

            listing.CheckboxLabeled(
                "PWU_EnableZeushammerRecipe".Translate(),
                ref Settings.enableZeushammerRecipe,
                "PWU_EnableZeushammerRecipeDesc".Translate());

            listing.Gap(24.0f);

            Text.Font = GameFont.Medium;
            listing.Label("PWU_SettingsHaulPlanner".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            DrawHaulPlannerOption(listing,
                HaulPlannerKind.Sequential,
                "PWU_HaulPlannerSequential".Translate() + "PWU_VanillaSuffix".Translate(),
                "PWU_HaulPlannerSequentialDesc".Translate());

            DrawHaulPlannerOption(listing,
                HaulPlannerKind.Sweep,
                "PWU_HaulPlannerSweep".Translate() + "PWU_DefaultSuffix".Translate(),
                "PWU_HaulPlannerSweepDesc".Translate());

            DrawHaulPlannerOption(listing,
                HaulPlannerKind.Thorough,
                "PWU_HaulPlannerThorough".Translate() + "PWU_ExperimentalSuffix".Translate(),
                "PWU_HaulPlannerThoroughDesc".Translate());

            listing.Gap(24.0f);

            Text.Font = GameFont.Medium;
            listing.Label("PWU_SettingsMiscellaneous".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            listing.CheckboxLabeled(
                "PWU_EnableGroundCustomization".Translate(),
                ref Settings.enableGroundCustomization,
                "PWU_EnableGroundCustomizationDesc".Translate());

            listing.Gap();

            listing.CheckboxLabeled(
                "PWU_EnforceMaxTraitLimit".Translate(),
                ref Settings.enforceMaxTraitLimit,
                "PWU_EnforceMaxTraitLimitDesc".Translate());

            listing.Gap();

            listing.CheckboxLabeled(
                "PWU_EnforceSoleTrait".Translate(),
                ref Settings.enforceCanGenerateAlone,
                "PWU_EnforceSoleTraitDesc".Translate());

            listing.Gap(60f);

            Text.Font = prev;
            settingsHeight = listing.CurHeight;
            listing.End();
            Widgets.EndScrollView();

            if (Widgets.ButtonText(buttonRect, "PWU_ResetToDefaults".Translate()))
            {
                Settings.ResetToDefaults();
            }
        }

        /// <summary>
        /// Renders one row of the haul-planner radio group. Selecting an option
        /// flips Settings.haulPlannerKind to that value. The label is passed
        /// in fully composed (including any "(default)" / "(vanilla)" suffix).
        /// </summary>
        private static void DrawHaulPlannerOption(
            Listing_Standard listing,
            HaulPlannerKind kind,
            string label,
            string tooltip)
        {
            bool active = Settings.haulPlannerKind == kind;
            if (listing.RadioButton(label, active, tabIn: 0f, tooltip: tooltip))
            {
                Settings.haulPlannerKind = kind;
            }
            listing.Gap(8f);
        }

        /// <summary>
        /// Renders the live per-quality component-cost table below the three
        /// cost sliders: one row per <see cref="QualityCategory"/> (Awful through
        /// Legendary), recomputed every frame from the current (possibly unsaved)
        /// slider values via <see cref="TraitCostUtility.ComponentCostForQuality"/>
        /// so it never drifts out of sync with the sliders above it.
        /// </summary>
        private static void DrawCostTable(Listing_Standard listing)
        {
            const float rowHeight = 24f;
            const float qualityColumnWidth = 160f;

            var qualities = (QualityCategory[])System.Enum.GetValues(typeof(QualityCategory));
            for (int i = 0; i < qualities.Length; i++)
            {
                QualityCategory quality = qualities[i];
                Rect rowRect = listing.GetRect(rowHeight);

                // Subtle alternating shading — cheap (one extra draw call per
                // row) and helps the eye track seven rows of numbers.
                if (i % 2 == 1)
                    Widgets.DrawLightHighlight(rowRect);

                Rect qualityRect = new Rect(rowRect.x, rowRect.y, qualityColumnWidth, rowRect.height);
                Rect countRect = new Rect(
                    rowRect.x + qualityColumnWidth, rowRect.y,
                    rowRect.width - qualityColumnWidth, rowRect.height);

                int componentCount = TraitCostUtility.ComponentCostForQuality(
                    quality,
                    Settings.traitChangeBaseComponentCost,
                    Settings.traitChangeQualitySurchargeThreshold,
                    Settings.traitChangeQualitySurchargePerLevel);

                Widgets.Label(qualityRect, quality.GetLabel().CapitalizeFirst());
                Widgets.Label(countRect, componentCount.ToString());
            }
        }
    }
}
