using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonaWeaponsUnbound
{
    public partial class Dialog_WeaponCustomization
    {
        private const int PreviewRTSize = 256;

        // Cached preview render — rebuilt only when preview state changes.
        // The trait snapshot is part of the key because appearance is now
        // trait-dependent: a trait can drive color two (or any override reachable
        // through the thing's graphic), so toggling one must rebuild even when the
        // def is unchanged.
        private RenderTexture previewRT;
        private ThingDef cachedPreviewDef;
        private List<WeaponTraitDef> cachedPreviewTraits;

        // VPWE/VEF texPaths snapshot the preview render was last built from —
        // included in the rebuild key alongside def/traits so a Texture-tab
        // variant edit (which reassigns vpweTexPaths to a fresh list) refreshes
        // the preview icon, not just the confirmed job's appearance.
        private List<string> cachedPreviewTexPaths;

        // One prospective Thing reused across rebuilds (re-made only on def change).
        // ThingMaker.MakeThing mutates global sim state — Thing.PostMake draws a
        // UniqueIDsManager id and PostPostMake rolls off the global Rand — so caching
        // it bounds those draws to def changes instead of firing on every rebuild.
        private Thing previewThing;

        // What vpweTexPaths previewThing's comp was last stamped with (see
        // BuildPreviewGraphic) — null when never stamped (fresh Thing, or a
        // def with no VPWE/VEF comp). Distinct from cachedPreviewTexPaths:
        // this tracks the reused previewThing's own state, so a Texture-tab
        // edit re-stamps the same Thing rather than only affecting a future
        // def-change remake.
        private List<string> previewThingStampedTexPaths;

        // --- Left pane: weapon preview ---

        private void DrawWeaponPreview(Rect rect)
        {
            float curY = rect.y + 10f;

            // Weapon icon — reflects desired texture variant and effective color
            float iconSize = Mathf.Min(rect.width - 20f, rect.height * 0.4f);
            Rect iconRect = new Rect(
                rect.x + (rect.width - iconSize) / 2f,
                curY,
                iconSize,
                iconSize);
            DrawPreviewIcon(iconRect);

            // Vanilla "i" stats button — opens the info card for the prospective
            // weapon, so the player reads the final trait roster and stats rather
            // than summing modifiers by hand. Most of the thing's identity state is
            // stamped when it's (re)built (see BuildPreviewGraphic); name and bond
            // are stamped here instead because neither triggers a rebuild — a name
            // edit doesn't affect appearance, and the bond turns partly on the
            // memory-op radio, which isn't in the rebuild key.
            //
            // Placement: right edge flush with the pane itself, bottom flush with
            // the right pane's tab headers — both panes share contentRect.y, and
            // the tabs DrawControlsPanel hangs above its menu section bottom out at
            // rect.y + 8f + TabBarHeight.
            if (previewThing != null)
            {
                WeaponModificationUtility.SetName(previewThing, desiredName);
                WeaponModificationUtility.SetBiocodeDisplayState(
                    previewThing, PreviewKeepsBond, weapon.TryGetComp<CompBladelinkWeapon>()?.CodedPawnLabel);
                Widgets.InfoCardButton(
                    rect.xMax - InfoCardButtonSize,
                    rect.y + 8f + TabBarHeight - InfoCardButtonSize,
                    previewThing);
            }

            curY = iconRect.yMax + 8f;

            // Name input field
            DrawNameRow(rect.x + 8f, ref curY, rect.width - 16f);

            Text.Anchor = TextAnchor.UpperLeft;
            curY += 20f;

            // Bottom-aligned cost and refund summary (always visible)
            {
                float costRowHeight = CostIconSize + 8f;
                float bottomPadding = 6f;

                // Reserve space for the two summary rows so chips can scroll above them
                float summaryHeight = costRowHeight * 2f + bottomPadding;
                float chipsAreaHeight = Mathf.Max(0f, rect.yMax - curY - summaryHeight - 4f);

                if ((desiredTraits.Count > 0 || desiredMemoryOp != MemoryOpKind.None)
                    && chipsAreaHeight > 0f)
                {
                    Rect chipsOuterRect = new Rect(
                        rect.x + 8f, curY, rect.width - 16f, chipsAreaHeight);
                    float chipStride = TraitRowHeight + 2f;
                    float chipsContentHeight =
                        (desiredTraits.Count + (desiredMemoryOp != MemoryOpKind.None ? 1 : 0))
                        * chipStride;
                    bool needsScroll = chipsContentHeight > chipsAreaHeight;
                    float innerWidth = needsScroll
                        ? chipsOuterRect.width - 16f
                        : chipsOuterRect.width;
                    Rect chipsInnerRect = new Rect(0f, 0f, innerWidth, chipsContentHeight);

                    Widgets.BeginScrollView(chipsOuterRect, ref desiredTraitsScroll, chipsInnerRect);

                    float chipY = 0f;
                    foreach (WeaponTraitDef trait in desiredTraits)
                    {
                        Rect chipRect = new Rect(0f, chipY, innerWidth, TraitRowHeight);

                        // Chip background with hover highlight
                        bool hovered = Mouse.IsOver(chipRect);
                        Widgets.DrawBoxSolid(chipRect, hovered
                            ? new Color(0.3f, 0.3f, 0.3f, 0.5f)
                            : new Color(0.2f, 0.2f, 0.2f, 0.4f));

                        // Label — yellow when removing this trait would empty the
                        // player's pool of available sources for it (progression mode).
                        bool isLastSource = progressionPool?.IsLastNonHostileSource(trait, originalTraits) == true;
                        Text.Anchor = TextAnchor.MiddleLeft;
                        Rect labelRect = new Rect(
                            chipRect.x + 4f, chipRect.y,
                            chipRect.width * 0.7f, chipRect.height);
                        Color prevLabelColor = GUI.color;
                        if (isLastSource)
                            GUI.color = ColorLibrary.Yellow;
                        Widgets.Label(labelRect, trait.LabelCap);
                        GUI.color = prevLabelColor;

                        // Cost icons (right-aligned) — only for newly added traits.
                        // Every chip here is already staged, so use its real
                        // sequenced cost rather than an isolated hypothetical.
                        if (!originalTraits.Contains(trait))
                        {
                            List<ThingDefCountClass> chipCosts = StagedAdditionCost(trait);
                            Rect chipCostRect = new Rect(
                                labelRect.xMax, chipRect.y,
                                chipRect.xMax - labelRect.xMax - 4f, chipRect.height);
                            DrawCostIcons(chipCostRect, chipCosts, rightAlign: true,
                                insufficientResources: insufficientResources);
                        }

                        Text.Anchor = TextAnchor.UpperLeft;

                        // Tooltip (same as traits tab). The "last source" warning
                        // gets its own tooltip box stacked alongside, so it reads as
                        // a distinct alert rather than being lost at the bottom of
                        // a long stat block.
                        string tooltip = BuildTraitTooltip(trait);
                        if (!string.IsNullOrEmpty(tooltip))
                            TooltipHandler.TipRegion(chipRect, tooltip);
                        if (isLastSource)
                        {
                            // Color the tooltip body to match the chip's yellow label,
                            // so the visual cue and the explanatory tip share an identity.
                            // Hex matches ColorLibrary.Yellow (#ffff14).
                            TooltipHandler.TipRegion(chipRect,
                                "<color=#ffff14>" + "PWU_LastTraitSourceWarning".Translate() + "</color>");
                        }

                        // Click: switch to traits tab and scroll trait into view
                        if (Widgets.ButtonInvisible(chipRect))
                        {
                            activeTab = 0;
                            int traitIndex = compatibleTraits.IndexOf(trait);
                            if (traitIndex >= 0)
                                traitListScroll.y = traitIndex * (TraitRowHeight + TraitRowGap);
                        }

                        chipY += chipStride;
                    }

                    // Memory-op chip (§4): the selected wipe (at most one, by
                    // the radio model) renders as one chip after the trait
                    // chips, styled identically. It's a one-time operation —
                    // never an "original" — so it always shows its cost.
                    if (desiredMemoryOp != MemoryOpKind.None)
                    {
                        Rect chipRect = new Rect(0f, chipY, innerWidth, TraitRowHeight);

                        bool hovered = Mouse.IsOver(chipRect);
                        Widgets.DrawBoxSolid(chipRect, hovered
                            ? new Color(0.3f, 0.3f, 0.3f, 0.5f)
                            : new Color(0.2f, 0.2f, 0.2f, 0.4f));

                        Text.Anchor = TextAnchor.MiddleLeft;
                        Rect labelRect = new Rect(
                            chipRect.x + 4f, chipRect.y,
                            chipRect.width * 0.7f, chipRect.height);
                        Widgets.Label(labelRect, MemoryOpLabel(desiredMemoryOp));

                        List<ThingDefCountClass> chipCosts = MemoryWipeCost(desiredMemoryOp);
                        Rect chipCostRect = new Rect(
                            labelRect.xMax, chipRect.y,
                            chipRect.xMax - labelRect.xMax - 4f, chipRect.height);
                        DrawCostIcons(chipCostRect, chipCosts, rightAlign: true,
                            insufficientResources: insufficientResources);

                        Text.Anchor = TextAnchor.UpperLeft;

                        string opTooltip = BuildMemoryOpTooltip(desiredMemoryOp);
                        if (!string.IsNullOrEmpty(opTooltip))
                            TooltipHandler.TipRegion(chipRect, opTooltip);

                        // Click: jump to the Memory tab (mirrors trait chips
                        // jumping to the Traits tab; no scroll-to needed — the
                        // tab holds three rows).
                        if (Widgets.ButtonInvisible(chipRect))
                            activeTab = 1;
                    }
                    // A pending texture restyle (VPWE/VEF texture tab) deliberately
                    // gets NO chip: the chip list exists to itemize cost-bearing
                    // ops, and OpType.Restyle never carries a cost. The Texture
                    // tab's own controls are the only staging surface it needs.

                    Widgets.EndScrollView();
                }

                bool hasSurplus = currentSurplus?.Count > 0;
                bool hasNetCost = currentNetCost?.Count > 0;

                // Stack from bottom: refund row, net cost row
                float bottomY = rect.yMax - bottomPadding;

                // Net refund row
                Rect refundArea = new Rect(
                    rect.x + 8f, bottomY - costRowHeight,
                    rect.width - 16f, costRowHeight);

                Text.Anchor = TextAnchor.MiddleLeft;
                if (!hasSurplus)
                    GUI.color = Color.gray;
                string refundLabel = "PWU_NetRefund".Translate();
                float refundLabelWidth = Text.CalcSize(refundLabel).x;
                Widgets.Label(
                    new Rect(refundArea.x, refundArea.y,
                        refundLabelWidth, refundArea.height),
                    refundLabel);

                if (hasSurplus)
                {
                    DrawCostIcons(
                        new Rect(refundArea.x + refundLabelWidth, refundArea.y,
                            refundArea.width - refundLabelWidth, refundArea.height),
                        currentSurplus,
                        greenQuantities: true,
                        maxVisible: 5);
                    TooltipHandler.TipRegion(refundArea,
                        refundLabel + FormatCostList(currentSurplus));
                }
                else
                {
                    Widgets.Label(
                        new Rect(refundArea.x + refundLabelWidth, refundArea.y,
                            refundArea.width - refundLabelWidth, refundArea.height),
                        "PWU_RefundNone".Translate());
                    GUI.color = Color.white;
                }
                Text.Anchor = TextAnchor.UpperLeft;

                // Net cost row above refund
                Rect netCostArea = new Rect(
                    rect.x + 8f, refundArea.y - costRowHeight,
                    rect.width - 16f, costRowHeight);

                Text.Anchor = TextAnchor.MiddleLeft;
                if (hasNetCost)
                {
                    string costLabel = "PWU_NetCost".Translate();
                    float labelWidth = Text.CalcSize(costLabel).x;
                    Widgets.Label(
                        new Rect(netCostArea.x, netCostArea.y, labelWidth, netCostArea.height),
                        costLabel);

                    DrawCostIcons(
                        new Rect(netCostArea.x + labelWidth, netCostArea.y,
                            netCostArea.width - labelWidth, netCostArea.height),
                        currentNetCost,
                        insufficientResources: insufficientResources,
                        maxVisible: 5);
                    TooltipHandler.TipRegion(netCostArea,
                        costLabel + FormatCostList(currentNetCost));
                }
                else
                {
                    string costPrefix = "PWU_NetCost".Translate();
                    float prefixWidth = Text.CalcSize(costPrefix).x;
                    Widgets.Label(
                        new Rect(netCostArea.x, netCostArea.y, prefixWidth, netCostArea.height),
                        costPrefix);
                    Color prevFreeColor = GUI.color;
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    Widgets.Label(
                        new Rect(netCostArea.x + prefixWidth, netCostArea.y,
                            netCostArea.width - prefixWidth, netCostArea.height),
                        "PWU_CostFree".Translate());
                    GUI.color = prevFreeColor;
                }
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private static string FormatCostList(List<ThingDefCountClass> costs)
        {
            var sb = new System.Text.StringBuilder();
            foreach (ThingDefCountClass cost in costs)
                sb.Append("\n  ").Append(cost.thingDef.LabelCap).Append(" x").Append(cost.count);
            return sb.ToString();
        }

        private void DrawPreviewIcon(Rect rect)
        {
            ThingDef resultDef = ResultingDef;

            bool needsRebuild = previewRT == null
                || cachedPreviewDef != resultDef
                || !SameTraits(cachedPreviewTraits, desiredTraits)
                || !SameTexPaths(cachedPreviewTexPaths, vpweTexPaths);

            // Rebuild during Layout to avoid disrupting Repaint's active rendering.
            // Graphics.Blit changes RenderTexture.active, which during Repaint would
            // redirect subsequent UI draws into our texture instead of the screen.
            if (needsRebuild && Event.current.type == EventType.Layout)
            {
                RebuildPreviewRT(resultDef);
                cachedPreviewDef = resultDef;
                cachedPreviewTraits = new List<WeaponTraitDef>(desiredTraits);
                cachedPreviewTexPaths = vpweTexPaths != null ? new List<string>(vpweTexPaths) : null;
            }

            if (previewRT != null)
                GUI.DrawTexture(rect, previewRT, ScaleMode.ScaleToFit, true);
            else
                Widgets.ThingIcon(rect, resultDef);
        }

        private void RebuildPreviewRT(ThingDef resultDef)
        {
            DestroyPreviewRT();
            Graphic topLevel = BuildPreviewGraphic(resultDef);
            // No player-facing variant-index concept (persona weapons render via
            // Graphic_Single) — fall back to the weapon's own override, if any,
            // as harmless preservation for modded weapons that still vary by index.
            previewRT = BuildVariantPreview(topLevel, weapon.overrideGraphicIndex ?? 0, PreviewRTSize);
        }

        // Resolves the weapon's top-level (collection-level) graphic for a
        // prospective customization state — the desired def and trait set —
        // by building a Thing in that state and asking it, rather than
        // predicting the appearance by hand.
        //
        // We let the actual object describe itself: Thing.Graphic resolves
        // through GraphicData.GraphicColoredFor using the thing's own
        // DrawColor/DrawColorTwo, so the weapon's own Thing/Comp graphic
        // overrides run against the prospective trait list. That keeps the
        // preview decoupled from how a given weapon (vanilla or a downstream
        // mod) maps state to appearance — any override reachable through the
        // thing's graphic comes through for free, with no knowledge of its
        // mechanism here. (The one ceiling: an override that lives purely in
        // a draw-time patch and never changes the thing's graphic can't be
        // reconstructed by anything short of invoking that draw path.)
        //
        // For appearance, only the trait list needs setting: color two is
        // derived from the trait list (+ stuff) by the weapon's own
        // DrawColorTwo. Bonding/hediff wiring doesn't affect appearance, so
        // the heavier AddTrait side effects are skipped — the trait list is
        // replaced directly. Beyond appearance, the thing also backs the
        // preview's info card, so when it is (re)made the original weapon's
        // identity state (quality, hitpoints, art, relic status) is stamped on
        // via WeaponDefConversion's copy helpers — copy semantics, not the
        // conversion pipeline's ownership transfers, so the live weapon's
        // state is never disturbed.
        //
        // Building a Thing mutates global sim state, which the old
        // graphic-only path never touched: Thing.PostMake pulls a
        // UniqueIDsManager id and CompBladelinkWeapon.PostPostMake rolls
        // random traits/name off the global Rand. Two guards keep that from
        // leaking (a multiplayer desync risk, since rebuilds run during GUI
        // layout, off the synchronized tick): the make is wrapped in
        // Rand.Push/PopState so the throwaway rolls don't perturb the shared
        // Rand stream, and the Thing is cached on previewThing and re-made
        // only when the result def changes — so the id draw fires per def,
        // not per rebuild. Re-stamping traits below touches no global state.
        // The cached thing is never spawned, never scribed, and never
        // destroyed — simply dropped with the dialog. That lifecycle is also
        // what makes the identity stamp's shared references (art TaleReference,
        // relic precept) safe: the destroy and save paths, the only places a
        // shared reference could tear down or fork state the real weapon still
        // owns, never run. Destroy() must NOT be added here — it would fire
        // CompArt.PostDestroy against the live weapon's tale,
        // Precept_Relic.Notify_ThingLost against its precept, and
        // CompBladelinkWeapon.PostDestroy's UnCode against its bond.
        private Graphic BuildPreviewGraphic(ThingDef resultDef)
        {
            if (resultDef?.graphicData == null)
                return null;

            bool madeThisCall = false;
            if (previewThing == null || previewThing.def != resultDef)
            {
                // Mirror WeaponDefConversion: carry the live weapon's material across
                // (color two's stuff tint depends on it), falling back to the default
                // so a stuffable target is never handed a null stuff.
                ThingDef stuff = resultDef.MadeFromStuff
                    ? (weapon.Stuff ?? GenStuff.DefaultStuffFor(resultDef))
                    : null;

                // Contain PostMake/PostPostMake's Rand draws — we overwrite the rolled
                // state below, so the values don't matter, but the global stream must
                // not advance (see method remarks).
                Rand.PushState();
                try
                {
                    previewThing = ThingMaker.MakeThing(resultDef, stuff);
                }
                finally
                {
                    Rand.PopState();
                }
                madeThisCall = true;
                // A fresh Thing carries no stamp yet — reset so the skin
                // logic below re-stamps it rather than assuming the previous
                // (now-replaced) previewThing's stamp still applies.
                previewThingStampedTexPaths = null;

                // Mirror ConvertWeaponDef's identity handling so the info card
                // opened from the preview reads as the customized weapon will:
                // scrub PostPostMake's rolled traits and the rolled persona
                // name, then stamp the original's quality (null art source — no
                // InitializeArt roll), hitpoint percentage, art, and relic
                // status. These are the copy-semantics halves of the conversion
                // transfers — the original keeps ownership of the shared
                // references (art TaleReference, relic precept), which the
                // preview thing's lifecycle makes safe (see method remarks).
                // Biocode is deliberately absent: it isn't part of conversion at
                // all (fork spec §4), and the preview's display-only bond stamp
                // depends on staged state that changes without a remake, so it
                // lives in DrawWeaponPreview instead. None of this touches
                // global state, so no Rand guard is needed. Once per make:
                // everything stamped here is immutable while the dialog is open.
                WeaponModificationUtility.ClearAutoGeneratedPersonaState(previewThing);
                WeaponDefConversion.CopyQuality(weapon, previewThing);
                WeaponDefConversion.CopyHitPointsPercent(weapon, previewThing);
                WeaponDefConversion.CopyArt(weapon, previewThing);
                WeaponDefConversion.CopyRelicStatus(weapon, previewThing);

                // Same harmless preservation ConvertWeaponDef does for modded
                // weapons that still vary by index — it also fixes the info
                // card's icon, which resolves through the thing
                // (Widgets.ThingIcon → ExtractInnerGraphicFor →
                // Graphic_Random.SubGraphicFor) and without an override falls
                // back to hashing the throwaway thingIDNumber.
                previewThing.overrideGraphicIndex = weapon.overrideGraphicIndex;
            }

            CompBladelinkWeapon comp = previewThing.TryGetComp<CompBladelinkWeapon>();
            if (comp != null)
            {
                // Replace PostPostMake's random roll with the prospective trait set.
                List<WeaponTraitDef> traits = comp.TraitsListForReading;
                traits.Clear();
                traits.AddRange(desiredTraits);
            }

            // VPWE/VEF skin preservation AND live Texture-tab edits. Stamp
            // whenever the desired paths differ from what's currently on the
            // preview Thing — not only on a fresh make — so picking a new
            // variant in the Texture tab recomposes the reused previewThing
            // immediately instead of only taking effect on a future def
            // change. ApplyTexPaths clears the cached graphic/texture, so a
            // reused Thing recomposes correctly from the new paths.
            if (vpweTexPaths != null && !SameTexPaths(previewThingStampedTexPaths, vpweTexPaths))
            {
                VPWEIntegration.ApplyTexPaths(previewThing, vpweTexPaths);
                previewThingStampedTexPaths = new List<string>(vpweTexPaths);
            }

            // Resolving .Graphic can trigger VEF's lazy skin roll — when customizing a
            // base weapon there's no captured skin to stamp, so VEF rolls off the
            // global Rand on this first access. Contain it in Rand.Push/PopState like
            // the MakeThing rolls above, so a rebuild during GUI layout can't perturb
            // the synchronized Rand stream (MP-desync guard). Then capture that roll
            // so every later preview and the confirmed job reproduce the same skin
            // instead of rerolling. With a captured skin already stamped, texPaths is
            // non-empty and VEF doesn't roll at all here.
            Graphic graphic;
            Rand.PushState();
            try
            {
                graphic = previewThing.Graphic;
                if (madeThisCall && vpweTexPaths == null)
                {
                    vpweTexPaths = VPWEIntegration.CaptureTexPaths(previewThing);
                    if (vpweTexPaths != null)
                    {
                        previewThingStampedTexPaths = new List<string>(vpweTexPaths);
                        // First time a skin is ever established for this dialog
                        // (base weapon rolling its first persona skin) — this
                        // roll becomes the Texture tab's "original" baseline too.
                        if (originalVpweTexPaths == null)
                            originalVpweTexPaths = new List<string>(vpweTexPaths);
                    }
                }
            }
            finally
            {
                Rand.PopState();
            }

            return graphic;
        }

        // Ordered equality for the two preview caches' texPaths snapshots
        // (and the reused previewThing's stamp check). Unlike SameTraits, a
        // null/null pair matches — no VPWE/VEF, or nothing rolled yet, is a
        // stable "unchanged" state that must not force a rebuild every frame.
        private static bool SameTexPaths(List<string> cached, List<string> current)
        {
            if (cached == null && current == null)
                return true;
            if (cached == null || current == null)
                return false;
            if (cached.Count != current.Count)
                return false;
            for (int i = 0; i < cached.Count; i++)
            {
                if (cached[i] != current[i])
                    return false;
            }
            return true;
        }

        // Blits one texture variant of a prebuilt, already-colored top-level
        // graphic into a fresh RenderTexture. textureIndex only matters for
        // modded weapons whose graphic is still a Graphic_Random — persona
        // weapons render via Graphic_Single, with no variant concept.
        private RenderTexture BuildVariantPreview(Graphic topLevel, int textureIndex, int rtSize)
        {
            if (topLevel == null)
                return null;

            // Select the texture variant, mirroring Graphic_Random.SubGraphicFor at
            // draw time. The coloring preserves the wrapper types, so unwrap rotation
            // then index into the variants.
            Graphic graphic = topLevel;
            if (graphic is Graphic_RandomRotated rotated)
                graphic = rotated.SubGraphic;
            if (graphic is Graphic_Random random)
                graphic = random.SubGraphicAtIndex(textureIndex);

            // Unity-overloaded == (not ?.) so a destroyed Material also bails out
            // instead of throwing MissingReferenceException on .mainTexture (UNT0008).
            Material mat = graphic.MatSingle;
            if (mat == null)
                return null;
            Texture mainTex = mat.mainTexture;
            if (mainTex == null)
                return null;

            RenderTexture rt = new RenderTexture(rtSize, rtSize, 0, RenderTextureFormat.ARGB32);

            // Save and restore RenderTexture.active around the entire operation.
            // Graphics.Blit sets it to the destination and does NOT restore it —
            // leaving it set would redirect all subsequent UI rendering into our texture.
            RenderTexture prev = RenderTexture.active;

            // Clear to transparent so clipped pixels (alpha < cutoff) stay transparent
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);

            // Blit through the material's shader — CutoutComplex reads the mask
            // texture to selectively apply the color, matching in-game rendering
            Graphics.Blit(mainTex, rt, mat);

            RenderTexture.active = prev;
            return rt;
        }

        // Ordered equality for the two preview caches' trait snapshots. Order
        // matters — color resolution is order-sensitive (e.g. "last forced color
        // wins" / "first body-color trait wins"). A null cached snapshot (first
        // build) never matches, forcing the initial rebuild.
        private static bool SameTraits(List<WeaponTraitDef> cached, List<WeaponTraitDef> current)
        {
            if (cached == null)
                return false;
            if (cached.Count != current.Count)
                return false;
            for (int i = 0; i < cached.Count; i++)
            {
                if (cached[i] != current[i])
                    return false;
            }
            return true;
        }

        private void DestroyPreviewRT()
        {
            if (previewRT != null)
            {
                previewRT.Release();
                UnityEngine.Object.Destroy(previewRT);
                previewRT = null;
            }
        }

        public override void PreClose()
        {
            base.PreClose();
            DestroyPreviewRT();
        }
    }
}
