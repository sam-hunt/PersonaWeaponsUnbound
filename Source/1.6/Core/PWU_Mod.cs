using PersonaWeaponsUnbound.HaulPlanning;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonaWeaponsUnbound
{
    public class PWU_Mod : Mod
    {
        // Setter is internal so the headless test suite can install a settings instance.
        public static PWU_Settings Settings { get; internal set; }

        private Vector2 settingsScroll;
        private float settingsHeight;

        // Whether the skill-check subsection is expanded. Purely a dialog-level
        // view state (never scribed): derived from the stored subject the first
        // time the dialog draws after opening, then driven only by the
        // player's own clicks on the "Require minimum skill" checkbox. Null
        // means "derive on next draw"; WriteSettings (fired when the dialog
        // closes) and Reset to defaults clear it so the next open re-derives.
        private static bool? skillCheckExpanded;

        public PWU_Mod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<PWU_Settings>();
        }

        public override string SettingsCategory() => "PWU_SettingsCategory".Translate();

        public override void WriteSettings()
        {
            base.WriteSettings();
            // Live-apply: the techprint slider takes effect immediately, no restart
            // required (fork spec §7, D4). Same for the persona-core recipe's
            // ingredient count and skill requirement.
            PWU_ResearchDefOf.ApplyTechprintCount();
            PWU_RecipeDefOf.ApplyPersonaCoreRecipeSettings();
            // The persona-core recipe toggle itself is not live: it's an XML
            // patch gate (PatchOperation_UnlessPersonaCoreRecipeEnabled) that
            // only runs at load, so it takes effect on restart.
            skillCheckExpanded = null;
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

            listing.CheckboxLabeled(
                "PWU_FirstTraitCostsPersonaCore".Translate(),
                ref Settings.firstTraitCostsPersonaCore,
                "PWU_FirstTraitCostsPersonaCoreDesc".Translate());

            listing.Gap();

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
            listing.Label("PWU_SettingsMemoryCosts".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            string wipeBondingLabel = "PWU_WipeBondingComponentCost".Translate(
                Settings.wipeBondingComponentCost);
            if (Settings.wipeBondingComponentCost == 3)
                wipeBondingLabel += "PWU_DefaultSuffix".Translate();
            listing.Label(wipeBondingLabel, tooltip: "PWU_WipeBondingComponentCostDesc".Translate());
            Settings.wipeBondingComponentCost =
                Mathf.RoundToInt(listing.Slider(Settings.wipeBondingComponentCost, 0f, 5f));

            listing.Gap();

            string wipeKillTrackerLabel = "PWU_WipeKillTrackerComponentCost".Translate(
                Settings.wipeKillTrackerComponentCost);
            if (Settings.wipeKillTrackerComponentCost == 1)
                wipeKillTrackerLabel += "PWU_DefaultSuffix".Translate();
            listing.Label(wipeKillTrackerLabel, tooltip: "PWU_WipeKillTrackerComponentCostDesc".Translate());
            Settings.wipeKillTrackerComponentCost =
                Mathf.RoundToInt(listing.Slider(Settings.wipeKillTrackerComponentCost, 0f, 5f));

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
                "PWU_RequireCustomizationResearchDesc".Translate(
                    PWU_ResearchDefOf.PWU_BladelinkCustomization.label));

            listing.Gap();

            string techprintLabel = "PWU_TechprintCount".Translate(Settings.techprintCount);
            if (Settings.techprintCount == 1)
                techprintLabel += "PWU_DefaultSuffix".Translate();
            listing.Label(techprintLabel, tooltip: "PWU_TechprintCountDesc".Translate(
                PWU_ResearchDefOf.PWU_BladelinkCustomization.label));
            Settings.techprintCount = Mathf.RoundToInt(listing.Slider(Settings.techprintCount, 0f, 3f));

            listing.Gap();

            DrawSkillCheckSettings(listing);

            listing.Gap(24.0f);

            Text.Font = GameFont.Medium;
            listing.Label("PWU_SettingsCraftingRecipes".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            listing.CheckboxLabeled(
                "PWU_EnableMonoswordRecipe".Translate(),
                ref Settings.enableMonoswordRecipe,
                "PWU_EnableMonoswordRecipeDesc".Translate(
                    PWU_RecipeDefOf.PWU_Make_MonoSword.label,
                    PWU_ThingDefOf.FabricationBench.label,
                    PWU_ResearchDefOf.PWU_BladelinkCustomization.label));

            listing.Gap();

            listing.CheckboxLabeled(
                "PWU_EnablePlasmaswordRecipe".Translate(),
                ref Settings.enablePlasmaswordRecipe,
                "PWU_EnablePlasmaswordRecipeDesc".Translate(
                    PWU_RecipeDefOf.PWU_Make_PlasmaSword.label,
                    PWU_ThingDefOf.FabricationBench.label,
                    PWU_ResearchDefOf.PWU_BladelinkCustomization.label));

            listing.Gap();

            listing.CheckboxLabeled(
                "PWU_EnableZeushammerRecipe".Translate(),
                ref Settings.enableZeushammerRecipe,
                "PWU_EnableZeushammerRecipeDesc".Translate(
                    PWU_RecipeDefOf.PWU_Make_Zeushammer.label,
                    PWU_ThingDefOf.FabricationBench.label,
                    PWU_ResearchDefOf.PWU_BladelinkCustomization.label));

            listing.Gap();

            // The toggle is applied by an XML patch at load, so unlike every
            // other setting here it only takes effect on restart.
            listing.CheckboxLabeled(
                "PWU_EnablePersonaCoreRecipe".Translate() + "PWU_RestartRequiredSuffix".Translate(),
                ref Settings.enablePersonaCoreRecipe,
                "PWU_EnablePersonaCoreRecipeDesc".Translate(
                    PWU_RecipeDefOf.PWU_Make_AIPersonaCore.label,
                    PWU_ThingDefOf.FabricationBench.label,
                    PWU_ResearchDefOf.ShipComputerCore.label));

            // The two sliders below only configure the recipe above, so they
            // stay hidden while it's off (the default) rather than sitting
            // there dead.
            if (Settings.enablePersonaCoreRecipe)
            {
                listing.Gap();

                string coreComponentLabel = "PWU_PersonaCoreRecipeComponentCost".Translate(
                    Settings.personaCoreRecipeComponentCost);
                if (Settings.personaCoreRecipeComponentCost == 20)
                    coreComponentLabel += "PWU_DefaultSuffix".Translate();
                listing.Label(coreComponentLabel, tooltip: "PWU_PersonaCoreRecipeComponentCostDesc".Translate());
                Settings.personaCoreRecipeComponentCost =
                    Mathf.RoundToInt(listing.Slider(Settings.personaCoreRecipeComponentCost, 5f, 30f));

                listing.Gap();

                string coreSkillLabel = "PWU_PersonaCoreRecipeMinSkill".Translate(
                    Settings.personaCoreRecipeMinSkill);
                if (Settings.personaCoreRecipeMinSkill == 18)
                    coreSkillLabel += "PWU_DefaultSuffix".Translate();
                listing.Label(coreSkillLabel, tooltip: "PWU_PersonaCoreRecipeMinSkillDesc".Translate());
                Settings.personaCoreRecipeMinSkill =
                    Mathf.RoundToInt(listing.Slider(Settings.personaCoreRecipeMinSkill, 0f, 20f));
            }

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

            // VPWE/VEF integration — only shown when the extended reflection
            // surface it depends on actually resolved (see VPWEIntegration.
            // UiSurfaceAvailable); players without VPWE/VEF, or on a version
            // where the surface has drifted, never see a dead toggle.
            if (VPWEIntegration.UiSurfaceAvailable)
            {
                listing.Gap(24.0f);

                Text.Font = GameFont.Medium;
                listing.Label("PWU_SettingsVpweIntegration".Translate());
                Text.Font = GameFont.Small;
                listing.Gap(6f);

                listing.CheckboxLabeled(
                    "PWU_IntegrateVpweCustomization".Translate(),
                    ref Settings.integrateVpweCustomization,
                    "PWU_IntegrateVpweCustomizationDesc".Translate());
            }

            listing.Gap(60f);

            Text.Font = prev;
            settingsHeight = listing.CurHeight;
            listing.End();
            Widgets.EndScrollView();

            if (Widgets.ButtonText(buttonRect, "PWU_ResetToDefaults".Translate()))
            {
                Settings.ResetToDefaults();
                skillCheckExpanded = null;
            }
        }

        // The optional skill prerequisite, behind a "Require minimum skill"
        // checkbox that expands three radio groups (who is checked, which
        // skill, what is demanded: recipe, tech tier, expertise, or a flat
        // minimum) and the flat-minimum slider. Unticking the
        // checkbox resets the subject to "no one" and collapses the section;
        // ticking it only expands (the subject stays "no one" until the player
        // picks one). Picking "no one" from the radio group does not collapse
        // the section; only reopening the dialog re-derives the expanded state.
        //
        // Within the section the skill and requirement groups and the slider
        // render inert while the subject is "no one"; the slider is also inert
        // unless the flat kind is in force. The expertise row is hidden when
        // the selected skill's expertise is unavailable (no Vanilla Skills
        // Expanded, or VSE without the DLC it gates that expertise on), and
        // when it is nonetheless the stored selection the flat row simply
        // renders as active at the fallback level, mirroring what
        // SkillCheckRules.EffectiveKind enforces, without touching the stored
        // value, so installing VSE later restores the player's intent.
        private const float SkillCheckLabelIndent = 16f;
        private const float SkillCheckOptionIndent = 32f;

        private static void DrawSkillCheckSettings(Listing_Standard listing)
        {
            if (skillCheckExpanded == null)
                skillCheckExpanded = SkillCheckRules.Enabled;

            bool expanded = skillCheckExpanded.Value;
            listing.CheckboxLabeled("PWU_SkillCheckEnable".Translate(), ref expanded,
                "PWU_SkillCheckEnableDesc".Translate());
            if (expanded != skillCheckExpanded.Value)
            {
                skillCheckExpanded = expanded;
                if (!expanded)
                    Settings.skillCheckSubject = SkillCheckSubject.None;
            }
            if (!expanded)
                return;

            listing.Gap(8f);
            listing.Indent(SkillCheckLabelIndent);
            listing.ColumnWidth -= SkillCheckLabelIndent;
            float optionTab = SkillCheckOptionIndent - SkillCheckLabelIndent;

            listing.Label("PWU_SkillCheckSubject".Translate(),
                tooltip: "PWU_SkillCheckSubjectDesc".Translate());
            listing.Gap(4f);

            DrawSubjectOption(listing, SkillCheckSubject.None,
                "PWU_SkillCheckSubjectNone".Translate() + "PWU_DefaultSuffix".Translate(),
                "PWU_SkillCheckSubjectNoneDesc".Translate(), optionTab);
            DrawSubjectOption(listing, SkillCheckSubject.CustomizingPawn,
                "PWU_SkillCheckSubjectPawn".Translate(),
                "PWU_SkillCheckSubjectPawnDesc".Translate(), optionTab);
            DrawSubjectOption(listing, SkillCheckSubject.BestOnMap,
                "PWU_SkillCheckSubjectMap".Translate(),
                "PWU_SkillCheckSubjectMapDesc".Translate(), optionTab);
            DrawSubjectOption(listing, SkillCheckSubject.BestAnywhere,
                "PWU_SkillCheckSubjectWorld".Translate(),
                "PWU_SkillCheckSubjectWorldDesc".Translate(), optionTab);

            listing.Gap(8f);

            bool enabled = SkillCheckRules.Enabled;
            string inertTip = "PWU_SkillCheckNoEffect".Translate();
            SkillCheckSkill skill = Settings.skillCheckSkill;
            Color prevColor = GUI.color;

            // Which skill. Radio labels are vanilla's own skill labels, taken
            // from skillLabel exactly as the skills tab (SkillUI) does, so the
            // rows match it in every language and agree with the lowercase
            // {0} the requirement row and rejection messages inject.
            DrawGroupLabel(listing, "PWU_SkillCheckSkill", "PWU_SkillCheckSkillDesc", enabled, inertTip);

            if (DrawRadioOption(listing,
                SkillCheckRules.SkillDefFor(SkillCheckSkill.Intellectual).skillLabel.CapitalizeFirst()
                    + "PWU_DefaultSuffix".Translate(),
                enabled ? "PWU_SkillCheckSkillIntellectualDesc".Translate() : inertTip,
                active: skill == SkillCheckSkill.Intellectual,
                enabled: enabled, tabIn: optionTab))
            {
                Settings.skillCheckSkill = skill = SkillCheckSkill.Intellectual;
            }

            if (DrawRadioOption(listing,
                SkillCheckRules.SkillDefFor(SkillCheckSkill.Crafting).skillLabel.CapitalizeFirst(),
                enabled ? "PWU_SkillCheckSkillCraftingDesc".Translate() : inertTip,
                active: skill == SkillCheckSkill.Crafting,
                enabled: enabled, tabIn: optionTab))
            {
                Settings.skillCheckSkill = skill = SkillCheckSkill.Crafting;
            }

            listing.Gap(8f);

            // What is demanded. Evaluated after the skill group so a click
            // there is reflected in the same frame.
            SkillCheckKind effective = SkillCheckRules.EffectiveKind(out int flatLevel);
            string skillLabel = SkillCheckRules.SkillDefFor(skill).skillLabel;
            string tierSummary = enabled ? TechTierSummary() : null;

            DrawGroupLabel(listing, "PWU_SkillCheckKind", "PWU_SkillCheckKindDesc", enabled, inertTip);

            if (DrawRadioOption(listing,
                "PWU_SkillCheckKindRecipe".Translate() + "PWU_DefaultSuffix".Translate(),
                enabled ? "PWU_SkillCheckKindRecipeDesc".Translate(tierSummary) : inertTip,
                active: effective == SkillCheckKind.RecipeOrTechTier,
                enabled: enabled, tabIn: optionTab))
            {
                Settings.skillCheckKind = SkillCheckKind.RecipeOrTechTier;
            }

            if (DrawRadioOption(listing,
                "PWU_SkillCheckKindTechTier".Translate(TechTierLevels()),
                enabled ? "PWU_SkillCheckKindTechTierDesc".Translate(tierSummary) : inertTip,
                active: effective == SkillCheckKind.TechTier,
                enabled: enabled, tabIn: optionTab))
            {
                Settings.skillCheckKind = SkillCheckKind.TechTier;
            }

            // Hidden when the paired expertise can't be checked; a stored
            // expertise selection then shows as the flat row active at the
            // fallback level (see the summary comment above) and is kept for
            // when VSE returns.
            if (SkillCheckRules.ExpertiseAvailableFor(skill))
            {
                string expertiseLabel = SkillCheckRules.ExpertiseLabelFor(skill);
                if (DrawRadioOption(listing,
                    "PWU_SkillCheckKindExpertise".Translate(expertiseLabel).CapitalizeFirst(),
                    enabled ? "PWU_SkillCheckKindExpertiseDesc".Translate(expertiseLabel) : inertTip,
                    active: effective == SkillCheckKind.Expertise,
                    enabled: enabled, tabIn: optionTab))
                {
                    Settings.skillCheckKind = SkillCheckKind.Expertise;
                }
            }

            // The flat row sits last so its slider closes the group instead of
            // splitting it. The radio label doubles as the slider's value label
            // (no "(default)" suffix here: on a radio row it would read as the
            // default option rather than the default level).
            string flatLabel = "PWU_SkillCheckKindFlat".Translate(skillLabel, flatLevel).CapitalizeFirst();
            string flatTip = enabled ? "PWU_SkillCheckKindFlatDesc".Translate(skillLabel) : inertTip;
            // Clicking the row also writes the displayed level: under the
            // expertise fallback that is the fallback level, not the slider's,
            // and silently dropping to the slider value on "confirm" would
            // change what the check enforces.
            if (DrawRadioOption(listing, flatLabel, flatTip,
                active: effective == SkillCheckKind.FlatMinimum,
                enabled: enabled, tabIn: optionTab))
            {
                Settings.skillCheckMinimumLevel = flatLevel;
                Settings.skillCheckKind = SkillCheckKind.FlatMinimum;
            }

            // Slider indented under its radio row's label. Live whenever the
            // group is, and touching it (a press over it or a value change)
            // also selects the flat kind, so the player needn't click the radio
            // first (under the expertise fallback that makes the displayed
            // fallback level the permanent choice). With the group inert
            // (subject "no one") the slider is grey and non-interactive, and
            // never re-enables anything.
            Rect sliderRect = listing.GetRect(22f);
            sliderRect.xMin += optionTab + 12f;
            if (enabled)
            {
                // Read before the slider consumes the event.
                bool pressed = Event.current.type == EventType.MouseDown
                    && Event.current.button == 0 && Mouse.IsOver(sliderRect);
                int chosen = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect, flatLevel,
                    SkillCheckRules.MinFlatLevel, SkillCheckRules.MaxFlatLevel));
                if (pressed || chosen != flatLevel)
                {
                    Settings.skillCheckMinimumLevel = chosen;
                    Settings.skillCheckKind = SkillCheckKind.FlatMinimum;
                }
                if (effective != SkillCheckKind.FlatMinimum)
                    TooltipHandler.TipRegion(sliderRect, "PWU_SkillCheckFlatSliderSelects".Translate());
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.HorizontalSlider(sliderRect, flatLevel,
                    SkillCheckRules.MinFlatLevel, SkillCheckRules.MaxFlatLevel);
                GUI.color = prevColor;
                TooltipHandler.TipRegion(sliderRect, inertTip);
            }

            listing.ColumnWidth += SkillCheckLabelIndent;
            listing.Outdent(SkillCheckLabelIndent);
        }

        // The per-tier minimums for the radio tooltips ("neolithic 4, medieval
        // 5, ..."), read from the rule table so the text can't drift from the
        // behaviour.
        private static string TechTierSummary()
        {
            TechLevel[] tiers = SkillCheckRules.TechTiers;
            var parts = new string[tiers.Length];
            for (int i = 0; i < tiers.Length; i++)
            {
                parts[i] = "PWU_SkillCheckTierEntry".Translate(
                    tiers[i].ToStringHuman(),
                    SkillCheckRules.TechTierMinimumSkill(tiers[i]));
            }
            return string.Join(", ", parts);
        }

        // The same table compressed for the tech-level radio label
        // ("4/5/7/9/12/15"), lowest tier first.
        private static string TechTierLevels()
        {
            TechLevel[] tiers = SkillCheckRules.TechTiers;
            var parts = new string[tiers.Length];
            for (int i = 0; i < tiers.Length; i++)
                parts[i] = SkillCheckRules.TechTierMinimumSkill(tiers[i]).ToString();
            return string.Join("/", parts);
        }

        // Heading of a radio group that renders inert (grey, with the inert
        // tip) while the skill check applies to no one.
        private static void DrawGroupLabel(
            Listing_Standard listing, string labelKey, string descKey, bool enabled, string inertTip)
        {
            Color prevColor = GUI.color;
            if (!enabled)
                GUI.color = Color.gray;
            listing.Label(labelKey.Translate(), tooltip: enabled ? descKey.Translate() : inertTip);
            GUI.color = prevColor;
            listing.Gap(4f);
        }

        private static void DrawSubjectOption(
            Listing_Standard listing, SkillCheckSubject subject, string label, string tooltip,
            float tabIn)
        {
            if (DrawRadioOption(listing, label, tooltip,
                active: Settings.skillCheckSubject == subject, enabled: true, tabIn: tabIn))
            {
                Settings.skillCheckSubject = subject;
            }
        }

        // One radio row. Disabled rows render in vanilla's subtle grey and
        // ignore clicks (Widgets.RadioButtonLabeled still reports the click, so
        // the enabled check lives here). Returns true when an enabled row was
        // clicked.
        private static bool DrawRadioOption(
            Listing_Standard listing, string label, string tooltip, bool active, bool enabled,
            float tabIn = 0f)
        {
            bool clicked = listing.RadioButton(label, active, tabIn, tooltip, null, disabled: !enabled);
            listing.Gap(4f);
            return clicked && enabled;
        }

        // Renders one row of the haul-planner radio group. Selecting an option
        // flips Settings.haulPlannerKind to that value. The label is passed
        // in fully composed (including any "(default)" / "(vanilla)" suffix).
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

        // Renders the live per-quality component-cost table below the three
        // cost sliders: one row per QualityCategory (Awful through
        // Legendary), recomputed every frame from the current (possibly unsaved)
        // slider values via TraitCostUtility.ComponentCostForQuality
        // so it never drifts out of sync with the sliders above it.
        // Rows below the minimum-quality prerequisite show a dash instead of a
        // number, since those weapons can't be customized at any price.
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

                // Awful means "no restriction", matching CustomizationRules.
                bool belowMinimum = Settings.minimumQuality > QualityCategory.Awful
                    && quality < Settings.minimumQuality;

                string countLabel;
                if (belowMinimum)
                {
                    countLabel = "PWU_CostTableNotApplicable".Translate();
                    TooltipHandler.TipRegion(
                        rowRect,
                        "PWU_CostTableNotApplicableDesc".Translate(Settings.minimumQuality.GetLabel()));
                }
                else
                {
                    countLabel = TraitCostUtility.ComponentCostForQuality(
                        quality,
                        Settings.traitChangeBaseComponentCost,
                        Settings.traitChangeQualitySurchargeThreshold,
                        Settings.traitChangeQualitySurchargePerLevel).ToString();
                }

                Color prevColor = GUI.color;
                if (belowMinimum)
                    GUI.color = Color.gray;
                Widgets.Label(qualityRect, quality.GetLabel().CapitalizeFirst());
                Widgets.Label(countRect, countLabel);
                GUI.color = prevColor;
            }
        }
    }
}
