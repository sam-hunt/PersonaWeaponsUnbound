using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonaWeaponsUnbound
{
    public partial class Dialog_WeaponCustomization
    {
        // --- Memory tab (memory/polish spec §4) ---
        //
        // A one-time wipe of the persona's memory: its bond to a wielder or its
        // record of kills. Three-way radio (D17) — "Do not wipe" (default,
        // free) or exactly one of the two wipes. A bond wipe's UnCode() also
        // resets the kill tracker, so the bond wipe strictly subsumes the kill
        // wipe and "both" is not a distinct outcome the UI may express.

        private void DrawMemoryTab(Rect rect)
        {
            // Base-state gate (D19): memory ops require the persona form. When
            // the preview is reverted to base, show only the centered gray
            // message — the disabled-tab pattern shared with the traits tab's
            // empty states.
            if (IsRevertedToBase)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.Label(rect, "PWU_MemoryRequiresPersona".Translate());
                GUI.color = prevColor;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // One radio row per MemoryOpKind, in enum order (None first). A
            // future memory op is one more enum member and radio row.
            float curY = rect.y + 6f;
            foreach (MemoryOpKind kind in (MemoryOpKind[])Enum.GetValues(typeof(MemoryOpKind)))
                DrawMemoryOpRow(rect.x, ref curY, rect.width - 6f, kind);
        }

        // Draws one row of the memory-op radio group, mirroring
        // DrawTraitRow's geometry (row height/gap, disabled reason centered
        // in the middle gap, cost right-aligned, whole-row invisible button,
        // selected-row solid highlight) plus a Widgets.RadioButton glyph at
        // the left — the one deliberate deviation from trait rows: a radio
        // group has to communicate exactly-one-of, which a bare highlight
        // can't (D19).
        private void DrawMemoryOpRow(float x, ref float curY, float width, MemoryOpKind kind)
        {
            Rect rowRect = new Rect(x, curY, width, TraitRowHeight);
            string rejection = GetMemoryOpRejection(kind);
            bool isSelected = desiredMemoryOp == kind;
            bool isDisabled = rejection != null;

            // Highlight on hover for clickable rows
            if (!isDisabled && Mouse.IsOver(rowRect))
                Widgets.DrawHighlight(rowRect);

            // Selected highlight (consistency with trait rows)
            if (isSelected)
                Widgets.DrawBoxSolid(rowRect, new Color(0.35f, 0.35f, 0.35f, 0.4f));

            // Radio glyph at the left. Widgets.RadioButton reports clicks even
            // when disabled, so the disabled gate is applied at the handler.
            const float radioSize = Widgets.RadioButtonSize;
            bool glyphClicked = Widgets.RadioButton(
                rowRect.x + 4f,
                rowRect.y + (rowRect.height - radioSize) / 2f,
                isSelected, isDisabled);

            Color prevColor = GUI.color;
            if (isDisabled && !isSelected)
                GUI.color = new Color(0.5f, 0.5f, 0.5f);

            // Label beside the glyph
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = new Rect(rowRect.x + 4f + radioSize + 4f, rowRect.y,
                rowRect.width * 0.35f - radioSize - 8f, rowRect.height);
            Widgets.Label(labelRect, MemoryOpLabel(kind));

            // Cost icons (right-aligned). Selected op: its real staged cost
            // against the frame's committed resources; unselected: the
            // hypothetical cost of selecting it now. "Do not wipe" is free
            // (empty cost list — DrawCostIcons draws nothing).
            Rect costRect = new Rect(rowRect.x + rowRect.width * 0.7f, rowRect.y,
                rowRect.width * 0.3f - 4f, rowRect.height);
            List<ThingDefCountClass> cost = MemoryWipeCost(kind);
            if (isSelected)
                DrawCostIcons(costRect, cost, rightAlign: true,
                    insufficientResources: insufficientResources);
            else
                DrawCostIcons(costRect, cost, rightAlign: true,
                    insufficientResources: GetHypotheticalInsufficient(cost));

            // Rejection reason (centered in the middle zone between label and costs)
            if (isDisabled && !isSelected)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.7f, 0.4f, 0.4f);
                Rect rejRect = new Rect(labelRect.xMax, rowRect.y,
                    costRect.x - labelRect.xMax, rowRect.height);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rejRect, rejection);
                Text.Font = GameFont.Small;
            }

            GUI.color = prevColor;
            Text.Anchor = TextAnchor.UpperLeft;

            if (Mouse.IsOver(rowRect))
            {
                string tooltip = BuildMemoryOpTooltip(kind);
                if (!string.IsNullOrEmpty(tooltip))
                    TooltipHandler.TipRegion(rowRect, tooltip);
            }

            // Click to select — whole row or the glyph. Clicking a disabled
            // row does nothing.
            bool rowClicked = Widgets.ButtonInvisible(rowRect);
            if ((glyphClicked || rowClicked) && !isDisabled)
                desiredMemoryOp = kind;

            curY += TraitRowHeight + TraitRowGap;
        }

        // --- Gating (§4 table) ---

        // Returns the disabled reason for a memory-op row, or null when the
        // row is enabled. The tab gates on the preview def state
        // (IsRevertedToBase); the rows gate on the CURRENT weapon's comp
        // state — you can't wipe what isn't there yet:
        // - No CompBladelinkWeapon (base being upgraded this spec): both
        //   wipes disabled.
        // - Comp present, not bonded: bond wipe disabled; kill wipe enabled
        //   iff lastKillTick >= 0 (freewielders can kill without bonding).
        // - Bonded, no neverBond staged: both enabled (lastKillTick is
        //   always >= 0 once bonded — OnCodedFor stamps it).
        // - Bonded with a neverBond trait staged for addition: BOTH wipes
        //   disabled — the trait add's UnCode() severs the bond (D8) and
        //   clears the kill tracker, and additions run before the memory
        //   op, so either wipe would buy a no-op.
        // "Do not wipe" is always enabled; OnTraitsChanged snaps a selection
        // back to it when its row goes disabled.
        private string GetMemoryOpRejection(MemoryOpKind kind)
        {
            if (kind == MemoryOpKind.None)
                return null;

            CompBladelinkWeapon comp = weapon.TryGetComp<CompBladelinkWeapon>();
            bool bonded = comp?.Biocoded == true;
            bool neverBondStaged = TraitsToAdd.Any(t => t.neverBond);

            switch (kind)
            {
                case MemoryOpKind.WipeBonding:
                    if (!bonded)
                        return "PWU_MemoryNotBonded".Translate();
                    if (neverBondStaged)
                        return "PWU_MemoryBondSeveredByTrait".Translate();
                    return null;

                case MemoryOpKind.WipeKillTracker:
                    if (comp == null)
                        return "PWU_MemoryNoKillMemory".Translate();
                    if (bonded)
                        return neverBondStaged
                            ? (string)"PWU_MemoryKillWipedByTrait".Translate()
                            : null;
                    return CurrentLastKillTick >= 0
                        ? null
                        : (string)"PWU_MemoryNoKillMemory".Translate();

                default:
                    return null;
            }
        }

        // The current weapon's raw lastKillTick (-1 = no kill memory), read
        // through the startup-verified reflection handle. Returns -1 when
        // the comp or the handle is missing.
        private int CurrentLastKillTick
        {
            get
            {
                CompBladelinkWeapon comp = weapon.TryGetComp<CompBladelinkWeapon>();
                if (comp == null || WeaponModificationUtility.LastKillTickField == null)
                    return -1;
                return (int)WeaponModificationUtility.LastKillTickField.GetValue(comp);
            }
        }

        // --- Shared helpers (rows + LHS chip) ---

        private static string MemoryOpLabel(MemoryOpKind kind)
        {
            switch (kind)
            {
                case MemoryOpKind.WipeBonding:
                    return "PWU_MemoryWipeBonding".Translate();
                case MemoryOpKind.WipeKillTracker:
                    return "PWU_MemoryWipeKillTracker".Translate();
                default:
                    return "PWU_MemoryNoWipe".Translate();
            }
        }

        // Tooltip for a memory-op row (and the LHS chip): the op's desc key.
        // The kill-tracker tooltip appends the persona's current memory when
        // one exists ("Last kill: N days ago") — the kill tracker's only
        // player-facing surface.
        private string BuildMemoryOpTooltip(MemoryOpKind kind)
        {
            switch (kind)
            {
                case MemoryOpKind.WipeBonding:
                    return "PWU_MemoryWipeBondingDesc".Translate();
                case MemoryOpKind.WipeKillTracker:
                    string desc = "PWU_MemoryWipeKillTrackerDesc".Translate();
                    if (CurrentLastKillTick >= 0)
                    {
                        CompBladelinkWeapon comp = weapon.TryGetComp<CompBladelinkWeapon>();
                        float days = (float)comp.TicksSinceLastKill / GenDate.TicksPerDay;
                        desc += "\n\n" + "PWU_MemoryLastKill".Translate(days.ToString("0.#"));
                    }
                    return desc;
                default:
                    return null;
            }
        }

        // The flat component cost of a memory wipe: ComponentSpacer times the
        // op's settings slider (no quality scaling, unlike trait changes;
        // never refunded). Empty list when the slider is 0 — a free wipe
        // still runs. Mirrors TraitCostUtility.GetChangeCost's cost-list
        // shape.
        private static List<ThingDefCountClass> MemoryWipeCost(MemoryOpKind kind)
        {
            int componentCount;
            switch (kind)
            {
                case MemoryOpKind.WipeBonding:
                    componentCount = PWU_Mod.Settings.wipeBondingComponentCost;
                    break;
                case MemoryOpKind.WipeKillTracker:
                    componentCount = PWU_Mod.Settings.wipeKillTrackerComponentCost;
                    break;
                default:
                    componentCount = 0;
                    break;
            }
            if (componentCount <= 0)
                return new List<ThingDefCountClass>();
            return new List<ThingDefCountClass>
            {
                new ThingDefCountClass(ThingDefOf.ComponentSpacer, componentCount),
            };
        }
    }
}
