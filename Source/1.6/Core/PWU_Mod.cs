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

            listing.Gap(18.0f);

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

            listing.Gap(24.0f);

            Text.Font = GameFont.Medium;
            listing.Label("PWU_SettingsHaulPlanner".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            DrawHaulPlannerOption(listing,
                HaulPlannerKind.Sequential,
                "PWU_HaulPlannerSequential".Translate() + "PWU_VanillaSuffix".Translate(),
                "PWU_HaulPlannerSequentialDesc".Translate(),
                enabled: true);

            DrawHaulPlannerOption(listing,
                HaulPlannerKind.Sweep,
                "PWU_HaulPlannerSweep".Translate() + "PWU_DefaultSuffix".Translate(),
                "PWU_HaulPlannerSweepDesc".Translate(),
                enabled: true);

            DrawHaulPlannerOption(listing,
                HaulPlannerKind.Thorough,
                "PWU_HaulPlannerThorough".Translate() + "PWU_ExperimentalSuffix".Translate(),
                "PWU_HaulPlannerThoroughDesc".Translate(),
                enabled: true);

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

            if (ModsConfig.IdeologyActive)
            {
                listing.CheckboxLabeled(
                    "PWU_EnableIdeoColors".Translate(),
                    ref Settings.enableIdeologyColors,
                    "PWU_EnableIdeoColorsDesc".Translate());

                listing.Gap();
            }

            listing.CheckboxLabeled(
                "PWU_EnableStructureColors".Translate(),
                ref Settings.enableStructureColors,
                "PWU_EnableStructureColorsDesc".Translate());

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
        /// Renders one row of the haul-planner radio group. Disabled options
        /// render darkened and ignore clicks. Selecting an enabled option
        /// flips Settings.haulPlannerKind to that value. The label is passed
        /// in fully composed (including any "(default)" / "(vanilla)" suffix).
        /// </summary>
        private static void DrawHaulPlannerOption(
            Listing_Standard listing,
            HaulPlannerKind kind,
            string label,
            string tooltip,
            bool enabled)
        {
            bool active = Settings.haulPlannerKind == kind;

            if (enabled)
            {
                if (listing.RadioButton(label, active, tabIn: 0f, tooltip: tooltip))
                {
                    Settings.haulPlannerKind = kind;
                }
            }
            else
            {
                Color prevColor = GUI.color;
                Color prevContent = GUI.contentColor;
                // Compound the tint: GUI.color attenuates the whole control,
                // GUI.contentColor specifically attenuates text/icon glyphs.
                // Together they multiply (~0.4 * 0.5 = 0.2 effective), which
                // reads as visibly darker than plain Color.gray would.
                GUI.color = new Color(0.4f, 0.4f, 0.4f);
                GUI.contentColor = new Color(0.5f, 0.5f, 0.5f);
                // Force-render as inactive even if Settings somehow points
                // here (e.g. via a save from a future build); the runtime
                // factory falls back to Sequential for unrecognized values
                // anyway, so showing it inactive here matches behavior.
                listing.RadioButton(label, active: false, tabIn: 0f, tooltip: tooltip);
                GUI.contentColor = prevContent;
                GUI.color = prevColor;
            }
            listing.Gap(8f);
        }
    }
}
