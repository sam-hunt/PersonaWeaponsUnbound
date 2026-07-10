using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonaWeaponsUnbound
{
    public partial class Dialog_WeaponCustomization
    {
        private static string RelicNameTooltip => "PWU_RelicNameTooltip".Translate();

        // --- Right pane: controls ---

        private void DrawControlsPanel(Rect rect)
        {
            float curY = rect.y + 8f;
            float innerWidth = rect.width - 16f;
            float innerX = rect.x + 8f;

            // Tabbed section: tabs sit above the content area
            float menuTop = curY + TabBarHeight;
            float menuHeight = rect.yMax - menuTop - 4f;
            Rect menuRect = new Rect(innerX, menuTop, innerWidth, menuHeight);
            Widgets.DrawMenuSection(menuRect);
            DrawTabs(menuRect);

            Rect tabContentRect = menuRect.ContractedBy(4f);
            tabContentRect.x += 6f;
            tabContentRect.width -= 6f;
            tabContentRect.y += 6f;
            tabContentRect.height -= 6f;
            if (activeTab == 0)
                DrawTraitsTab(tabContentRect);
            else
            {
                // Memory tab (§4 of the memory/polish spec) takes this slot
                // in a later pass.
            }
        }

        private void DrawNameRow(float x, ref float curY, float width)
        {
            bool disabled = IsRevertedToBase;
            bool nameDisabled = disabled || isRelic;

            // Name field row: [field] [Randomize]
            float fieldWidth = width - RandomButtonWidth - 4f;
            Rect fieldRect = new Rect(x, curY, fieldWidth, NameFieldHeight);

            if (nameDisabled)
            {
                // Show read-only name: desiredName for relics (set to relic precept name
                // in constructor), base def name when reverted to base.
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.DrawBox(fieldRect);
                Text.Anchor = TextAnchor.MiddleLeft;
                Rect textInsetRect = fieldRect.ContractedBy(4f, 0f);
                string displayName = isRelic
                    ? desiredName ?? ""
                    : disabled
                        ? baseDef?.LabelCap ?? ""
                        : desiredName ?? "";
                Widgets.Label(textInsetRect, displayName);
                GUI.color = prevColor;

                if (isRelic && !disabled)
                    TooltipHandler.TipRegion(fieldRect, RelicNameTooltip);
            }
            else
            {
                string before = desiredName;
                desiredName = Widgets.TextField(fieldRect, desiredName ?? "", 60);
                // Detect manual edits — lock the name if the player typed something
                if (desiredName != before && desiredName != lastAutoName)
                    nameLocked = true;
            }

            // [Randomize] button — same height as the text field
            Rect randomRect = new Rect(
                fieldRect.xMax + 4f,
                curY,
                RandomButtonWidth,
                NameFieldHeight);

            bool randomDisabled = nameDisabled || desiredTraits.Count == 0;
            if (randomDisabled)
            {
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.ButtonText(randomRect, "PWU_Randomize".Translate());
                GUI.color = prevColor;
                if (isRelic && !disabled)
                    TooltipHandler.TipRegion(randomRect, RelicNameTooltip);
                else if (desiredTraits.Count == 0 && !nameDisabled)
                    TooltipHandler.TipRegion(randomRect,
                        "PWU_SelectTraitFirst".Translate());
            }
            else if (Widgets.ButtonText(randomRect, "PWU_Randomize".Translate()))
            {
                string regenerated = GenerateWeaponName();
                if (regenerated != null)
                {
                    desiredName = regenerated;
                    lastAutoName = desiredName;
                    nameLocked = false;
                }
            }

            Text.Anchor = TextAnchor.UpperLeft;
            curY += NameFieldHeight + 10f;

            // Auto-regenerate checkbox row: [checkbox] [label]
            float checkboxRowHeight = 24f;
            float checkboxSize = 24f;
            float checkboxGap = 4f;
            Rect checkboxIconRect = new Rect(x, curY, checkboxSize, checkboxRowHeight);
            Rect checkboxLabelRect = new Rect(
                x + checkboxSize + checkboxGap, curY,
                width - checkboxSize - checkboxGap, checkboxRowHeight);
            bool autoRegen = !nameLocked;

            Rect checkboxFullRect = new Rect(x, curY, width, checkboxRowHeight);
            if (nameDisabled)
            {
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.Checkbox(checkboxIconRect.x, checkboxIconRect.y, ref autoRegen,
                    checkboxSize, disabled: true);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(checkboxLabelRect,
                    "PWU_AutoRegenName".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = prevColor;

                if (isRelic && !disabled)
                    TooltipHandler.TipRegion(checkboxFullRect, RelicNameTooltip);
            }
            else
            {
                Widgets.Checkbox(checkboxIconRect.x, checkboxIconRect.y, ref autoRegen,
                    checkboxSize);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(checkboxLabelRect,
                    "PWU_AutoRegenName".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                nameLocked = !autoRegen;

                // Make the label clickable too
                if (Widgets.ButtonInvisible(checkboxLabelRect))
                {
                    autoRegen = !autoRegen;
                    nameLocked = !autoRegen;
                }
            }

            curY += checkboxRowHeight;
        }

        private void DrawTabs(Rect menuRect)
        {
            var tabs = new List<TabRecord>();
            tabs.Add(new TabRecord("PWU_TabTraits".Translate(), () => activeTab = 0, activeTab == 0));

            TabDrawer.DrawTabs(menuRect, tabs);
        }
    }
}
