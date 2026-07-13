using System.Collections.Generic;
using PersonaWeaponsUnbound.HaulPlanning;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonaWeaponsUnbound
{
    public partial class Dialog_WeaponCustomization
    {
        // --- Shared drawing helpers ---

        private void DrawCostIcons(
            Rect rect, List<ThingDefCountClass> costs, bool rightAlign = false,
            HashSet<ThingDef> insufficientResources = null, bool greenQuantities = false,
            int maxVisible = 0)
        {
            // TODO: Decide behavior for uncraftable weapons — TraitCostUtility returns
            // empty list. Currently shows nothing. May want "Free" label or a warning.
            if (costs == null || costs.Count == 0)
                return;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            // When the list exceeds maxVisible, show (maxVisible - 1) items + ellipsis
            // to signal that more is hidden and prompt the player to hover for the tooltip.
            bool truncated = maxVisible > 0 && costs.Count > maxVisible;
            int shownCount = truncated ? maxVisible - 1 : costs.Count;
            const string ellipsis = "...";

            float curX;
            if (rightAlign)
            {
                // Pre-calculate total width so we can start from the right edge
                float totalWidth = 0f;
                for (int i = 0; i < shownCount; i++)
                {
                    totalWidth += CostIconSize + 1f;
                    totalWidth += Text.CalcSize("x" + costs[i].count).x + 6f;
                }
                if (truncated)
                    totalWidth += Text.CalcSize(ellipsis).x + 6f;
                if (totalWidth > 0f)
                    totalWidth -= 6f; // Remove trailing gap
                curX = Mathf.Max(rect.x, rect.xMax - totalWidth);
            }
            else
            {
                curX = rect.x;
            }

            for (int i = 0; i < shownCount; i++)
            {
                ThingDefCountClass cost = costs[i];
                if (curX + CostIconSize > rect.xMax)
                    break;

                // Material icon
                Rect iconRect = new Rect(curX, rect.y + (rect.height - CostIconSize) / 2f,
                    CostIconSize, CostIconSize);
                Widgets.ThingIcon(iconRect, cost.thingDef);
                curX += CostIconSize + 1f;

                // Count label — red when insufficient, green for refunds
                string countText = "x" + cost.count;
                float textWidth = Text.CalcSize(countText).x;
                Rect textRect = new Rect(curX, rect.y, textWidth, rect.height);
                bool isShort = insufficientResources != null
                    && insufficientResources.Contains(cost.thingDef);
                if (isShort)
                {
                    Color prevCostColor = GUI.color;
                    GUI.color = new Color(0.9f, 0.2f, 0.2f);
                    Widgets.Label(textRect, countText);
                    GUI.color = prevCostColor;
                }
                else if (greenQuantities)
                {
                    Color prevCostColor = GUI.color;
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    Widgets.Label(textRect, countText);
                    GUI.color = prevCostColor;
                }
                else
                {
                    Widgets.Label(textRect, countText);
                }
                curX += textWidth + 6f;
            }

            if (truncated)
            {
                float ellipsisWidth = Text.CalcSize(ellipsis).x;
                Rect ellipsisRect = new Rect(curX, rect.y, ellipsisWidth, rect.height);
                Widgets.Label(ellipsisRect, ellipsis);
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        // --- Footer ---

        private void DrawFooter(Rect inRect)
        {
            float footerY = inRect.yMax - FooterHeight;
            float buttonY = footerY + (FooterHeight - ButtonSize.y) / 2f;

            // Cancel button (left-aligned)
            Rect cancelRect = new Rect(
                inRect.x,
                buttonY,
                ButtonSize.x,
                ButtonSize.y);
            if (Widgets.ButtonText(cancelRect, "PWU_Cancel".Translate()))
            {
                Close();
            }

            // Reset button (center-aligned)
            Rect resetRect = new Rect(
                inRect.x + (inRect.width - ButtonSize.x) / 2f,
                buttonY,
                ButtonSize.x,
                ButtonSize.y);
            if (HasChanges)
            {
                if (Widgets.ButtonText(resetRect, "PWU_Reset".Translate()))
                {
                    ResetToOriginal();
                }
            }
            else
            {
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.ButtonText(resetRect, "PWU_Reset".Translate());
                GUI.color = prevColor;
            }

            // Confirm button (right-aligned)
            Rect confirmRect = new Rect(
                inRect.xMax - ButtonSize.x,
                buttonY,
                ButtonSize.x,
                ButtonSize.y);

            // Determine if confirm should be enabled
            bool canConfirm = HasChanges;
            bool insufficientForConfirm = false;
            if (canConfirm)
            {
                if (insufficientResources != null && insufficientResources.Count > 0)
                {
                    canConfirm = false;
                    insufficientForConfirm = true;
                }
            }

            if (canConfirm)
            {
                if (Widgets.ButtonText(confirmRect, "PWU_Confirm".Translate()))
                {
                    // Reverting a BONDED persona weapon to base severs its bond
                    // (UnCode runs during conversion) — confirm first (spec D5/§9).
                    CompBladelinkWeapon bladelink = weapon.TryGetComp<CompBladelinkWeapon>();
                    if (IsRevertedToBase && bladelink != null
                        && bladelink.Biocoded && bladelink.CodedPawn != null)
                    {
                        string bondedLabel = !string.IsNullOrEmpty(bladelink.CodedPawnLabel)
                            ? bladelink.CodedPawnLabel
                            : bladelink.CodedPawn.LabelShortCap;
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                            "PWU_BondSeveredWarning".Translate(bondedLabel),
                            CommitCustomization,
                            destructive: true));
                    }
                    else
                    {
                        CommitCustomization();
                    }
                }
            }
            else
            {
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.ButtonText(confirmRect, "PWU_Confirm".Translate());
                GUI.color = prevColor;

                if (insufficientForConfirm)
                {
                    // Red error text immediately left of the confirm button
                    string errorText = "PWU_MissingResources".Translate();
                    float errorWidth = Text.CalcSize(errorText).x;
                    Rect errorRect = new Rect(
                        confirmRect.x - errorWidth - 8f,
                        confirmRect.y,
                        errorWidth,
                        confirmRect.height);
                    Color prevColor2 = GUI.color;
                    GUI.color = new Color(0.9f, 0.2f, 0.2f);
                    Text.Anchor = TextAnchor.MiddleRight;
                    Widgets.Label(errorRect, errorText);
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = prevColor2;

                    TaggedString tooltip = "PWU_NotEnoughMaterials".Translate();
                    TooltipHandler.TipRegion(errorRect, tooltip);
                    TooltipHandler.TipRegion(confirmRect, tooltip);
                }
            }
        }

        // Commits the staged customization: auto-generates a name if needed, builds
        // the spec, reserves ingredients synchronously (while forcePause holds the
        // game still — so what the player saw is what they get), hands the spec to
        // the running job driver, and closes. Invoked directly on confirm, or via a
        // confirmation dialog when the change severs a bond.
        private void CommitCustomization()
        {
            // Auto-generate name if result is a persona weapon but the name is empty
            if (ResultingDef == personaDef
                && string.IsNullOrEmpty(desiredName)
                && desiredTraits.Count > 0)
            {
                string regenerated = GenerateWeaponName();
                if (regenerated != null)
                    desiredName = regenerated;
            }

            // Build the ordered operations list and spec. Costs/refunds are
            // priced by the same sequential simulation as the live preview
            // (BuildOperations), so what the player saw is what they're charged.
            var spec = BuildCustomizationSpec();

            // Explicit CurJob null guard: in a single-player session forcePause
            // + absorbInputAroundWindow makes this impossible, but RimWorld
            // Multiplayer doesn't enforce pause across clients — a peer could
            // retarget our pawn between dialog open and confirm. The downstream
            // driver lookup inside TryReserveIngredientsForJob would also catch
            // this, but a paired check at the call site keeps the multiplayer-
            // readiness visible here next to the reservation call it protects.
            if (pawn.CurJob == null)
            {
                HandleReservationFailure(
                    IngredientReservation.ReservationResult.NoActiveDriver());
                return;
            }
            var result = IngredientReservation.TryReserveIngredientsForJob(
                pawn, spec.totalCost);
            if (!result.IsSuccess)
            {
                // Leave the dialog open (per-frame availability recompute
                // reveals new state on the next render) and surface both a
                // log line and a player-visible message via the helper, so
                // log + Messages text agree and the click isn't perceived
                // as a no-op. NoActiveDriver is a genuine invariant break
                // and logs at Error; the others at Warning.
                HandleReservationFailure(result);
                return;
            }

            // Set the spec directly on the driver field (not the static
            // pending-spec dict) so the field-scribe carries it across a
            // save/reload taken in the one-tick gap between Close() and
            // the consumeSpec toil — autosave landing in that gap would
            // otherwise orphan a confirmed customization.
            ((JobDriver_CustomizeWeapon)pawn.jobs.curDriver).SetSpec(spec);
            Close();
        }

        // --- Spec Building ---

        // Builds the confirmed spec from BuildOperations's sequential
        // simulation: totalCost is the net cost (aggregate op cost minus
        // aggregate op refund, positive remainder only) for pre-flight ingredient
        // reservation and hauling; totalRefund is the raw aggregate refund
        // that seeds the job driver's virtual refund ledger.
        private CustomizationSpec BuildCustomizationSpec()
        {
            List<CustomizationOp> ops = BuildOperations();

            List<ThingDefCountClass> totalCostAgg = SumOpCosts(ops, op => op.cost);
            List<ThingDefCountClass> totalRefundAgg = SumOpCosts(ops, op => op.refund);
            ComputeNetCostAndSurplus(totalCostAgg, totalRefundAgg,
                out List<ThingDefCountClass> netCost, out _);

            return new CustomizationSpec
            {
                operations = ops,
                resultingDef = ResultingDef,
                totalCost = netCost,
                totalRefund = totalRefundAgg,
                // Carry the VPWE/VEF skin so a base→persona conversion in the job
                // reproduces the previewed appearance instead of rolling a new one.
                vpweTexPaths = vpweTexPaths,
            };
        }

        public override void OnCancelKeyPressed()
        {
            Close();
        }

        // Maps a non-Success IngredientReservation.ReservationResult
        // to a paired log line + player-visible message describing what happened
        // at confirm time. Centralised so the two strings can't drift apart.
        // For IngredientReservation.ReservationOutcome.ReservationConflict
        // the result carries the specific def + count that failed to reserve,
        // so the message can name it concretely (e.g. "failed to reserve
        // plasteel x75") instead of saying "materials unavailable."
        private void HandleReservationFailure(IngredientReservation.ReservationResult result)
        {
            string logReason;
            string messageText;
            bool isInternalError = false;
            switch (result.Outcome)
            {
                case IngredientReservation.ReservationOutcome.NoActiveDriver:
                    logReason = "no active customize-weapon driver "
                        + "(invariant violation — dialog opened without our driver running)";
                    messageText = "PWU_CouldNotStartInternalError".Translate(weapon.LabelShortCap);
                    isInternalError = true;
                    break;
                case IngredientReservation.ReservationOutcome.PlanInfeasible:
                    logReason = "no haul planner could satisfy demand from the "
                        + "candidate pool (Sequential fallback also returned null)";
                    messageText = "PWU_CouldNotStartPlanInfeasible".Translate(weapon.LabelShortCap);
                    break;
                case IngredientReservation.ReservationOutcome.ReservationConflict:
                    string defLabel = result.ConflictDef?.label ?? "(unknown)";
                    string reserverInfo = result.ConflictReserver != null
                        ? " (held by " + result.ConflictReserver.LabelShortCap + ")"
                        : "";
                    logReason = "reservation conflict during commit "
                        + "(could not reserve " + defLabel + " x" + result.ConflictCount
                        + reserverInfo + ")";
                    // Pick the matching translation key — the held-by variant
                    // names the conflicting reserver so the player can find and
                    // investigate that pawn rather than retrying blindly.
                    messageText = result.ConflictReserver != null
                        ? "PWU_CouldNotStartReservationConflictHeldBy".Translate(
                            weapon.LabelShortCap, defLabel, result.ConflictCount,
                            result.ConflictReserver.LabelShortCap)
                        : "PWU_CouldNotStartReservationConflict".Translate(
                            weapon.LabelShortCap, defLabel, result.ConflictCount);
                    break;
                default:
                    return;
            }

            string logLine = "[Persona Weapons Unbound] Could not start customization of "
                + weapon.LabelShortCap + ": " + logReason + ". Dialog left open.";
            if (isInternalError)
                Log.Error(logLine);
            else
                Log.Warning(logLine);

            Messages.Message(
                messageText, weapon, MessageTypeDefOf.RejectInput, historical: false);
        }
    }
}
