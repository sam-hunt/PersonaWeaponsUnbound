using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PersonaWeaponsUnbound
{
    public partial class Dialog_WeaponCustomization
    {
        // --- Texture tab (optional, VPWE/VEF unified appearance customization) ---
        //
        // A third tab, shown only when the player opted into
        // PWU_Settings.integrateVpweCustomization and VPWEIntegration's extended
        // UI surface resolved (UiSurfaceAvailable) and the resulting def actually
        // carries a texture catalog (VPWEIntegration.GetPartCatalog). A single
        // "< [Part] >" selector picks which composable graphic part (grip,
        // blade, …; count is dynamic — 3–4 on VPWE weapons) is being edited, and
        // a scrollable grid of square thumbnails below it shows every variant of
        // that part rendered as it will actually look — the full layer stack
        // drawn with plain GUI.DrawTexture, substituting the candidate variant
        // for the selected part (see DrawTextureCell). Edits recompute
        // vpweTexPaths via the pure VPWETexPathMath helper and always assign a
        // fresh list — see SelectPartVariant — so preview/reference-equality
        // checks elsewhere (DrawPreviewIcon's rebuild condition) see every
        // change.

        private const float TexturePartRowGap = 6f;
        private const float ThumbCellSize = 128f;
        private const float ThumbCellGap = 6f;

        // Selected part index (non-scribed UI state — the dialog is
        // per-session only). Clamped to catalog.Count every draw so a def/
        // catalog change mid-session (e.g. a trait staging that swaps
        // ResultingDef) can't leave it out of range.
        private int texturePartIndex;

        private Vector2 textureGridScroll;

        // Per-cell layer-path-list cache for the currently selected part's
        // thumbnail grid. Rebuilt only when the selected part changes or
        // vpweTexPaths is reassigned — vpweTexPaths is always replaced with a
        // FRESH list on any edit (see SelectPartVariant), so comparing the
        // cached reference is a correct, cheap invalidation key. Avoids
        // rebuilding catalog->path-list math (and reflection-free, but still
        // non-trivial) on every one of the ~60fps draw calls a scroll view
        // triggers.
        private List<List<string>> cachedCellPaths;
        private List<string> cachedCellPathsFor;
        private int cachedCellPathsPart = -1;

        // Memoized ContentFinder<Texture2D> lookups, shared across all dialog
        // instances and cells. Content is fixed at load time — including
        // misses, so a missing/renamed asset doesn't re-search every frame —
        // so this never needs invalidating.
        private static readonly Dictionary<string, Texture2D> TextureLookupCache =
            new Dictionary<string, Texture2D>();

        /// <summary>
        /// Gates both the tab's presence in <see cref="DrawTabs"/> and the
        /// dispatch guard in <see cref="DrawControlsPanel"/>. Recomputed every
        /// frame — cheap, since <see cref="VPWEIntegration.GetPartCatalog"/>
        /// caches per def — so it tracks live setting toggles and the
        /// resulting def changing as traits are staged (e.g. reverting to
        /// base hides the tab, since base defs carry no texture catalog).
        /// </summary>
        private bool TextureTabAvailable
        {
            get
            {
                if (!PWU_Mod.Settings.integrateVpweCustomization || !VPWEIntegration.UiSurfaceAvailable)
                    return false;
                List<VpweTexturePart> catalog = VPWEIntegration.GetPartCatalog(ResultingDef);
                return catalog != null && catalog.Count > 0;
            }
        }

        /// <summary>
        /// True once the weapon's VPWE/VEF skin has actually been edited from
        /// its captured baseline (<see cref="originalVpweTexPaths"/>). Both
        /// sides are established together (ctor, or the lazy preview
        /// capture in BuildPreviewGraphic) so a null/null pair (no VPWE/VEF,
        /// or nothing rolled yet) correctly reads as "unchanged".
        /// </summary>
        private bool TextureChanged =>
            originalVpweTexPaths != null && vpweTexPaths != null
            && !originalVpweTexPaths.SequenceEqual(vpweTexPaths);

        private void DrawTextureTab(Rect rect)
        {
            List<VpweTexturePart> catalog = VPWEIntegration.GetPartCatalog(ResultingDef);
            if (catalog == null || catalog.Count == 0)
            {
                // Defensive — DrawControlsPanel/DrawTabs already gate on
                // TextureTabAvailable, so this shouldn't normally be reached.
                Text.Anchor = TextAnchor.MiddleCenter;
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.Label(rect, "PWU_NoTextureOptions".Translate());
                GUI.color = prevColor;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // A def/catalog change can leave the selected index out of range
            // (e.g. staging traits swaps ResultingDef to a def with fewer
            // parts) — clamp rather than throw or silently index OOB below.
            if (texturePartIndex < 0 || texturePartIndex >= catalog.Count)
                texturePartIndex = 0;

            float curY = rect.y + 6f;

            // vpweTexPaths is captured lazily by the first preview build (see
            // BuildPreviewGraphic) — for a base weapon being upgraded, that
            // happens within the same frame's preview draw (which runs before
            // this tab), but show a placeholder for the rare case it hasn't
            // landed yet rather than draw a grid with a stale/-1 selection.
            if (vpweTexPaths == null)
            {
                Text.Font = GameFont.Tiny;
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(rect.x, curY, rect.width, 20f),
                    "PWU_TextureRollingPlaceholder".Translate());
                GUI.color = prevColor;
                Text.Font = GameFont.Small;
                curY += 24f;
            }

            DrawTexturePartSelectorRow(rect.x, ref curY, rect.width - 6f, catalog);
            curY += TexturePartRowGap;

            float bottomReserve = TextureChanged ? ControlRowHeight + 8f : 0f;
            Rect gridRect = new Rect(rect.x, curY, rect.width - 6f,
                Mathf.Max(0f, rect.yMax - curY - bottomReserve));
            DrawTextureThumbnailGrid(gridRect, catalog);

            if (TextureChanged)
            {
                Rect resetRect = new Rect(rect.x, rect.yMax - ControlRowHeight - 4f, 150f, ControlRowHeight);
                if (Widgets.ButtonText(resetRect, "PWU_ResetTexture".Translate()))
                    vpweTexPaths = new List<string>(originalVpweTexPaths);
            }
        }

        /// <summary>
        /// The single "&lt; [Part] &gt;" row picking which composable graphic part
        /// the thumbnail grid below edits — left/right arrows step through
        /// <paramref name="catalog"/> with wraparound, the center button opens
        /// a FloatMenu listing every part by name. Mirrors the old per-part
        /// variant row's control geometry, applied one level up (parts
        /// instead of variants).
        /// </summary>
        private void DrawTexturePartSelectorRow(
            float x, ref float curY, float width, List<VpweTexturePart> catalog)
        {
            Rect rowRect = new Rect(x, curY, width, ControlRowHeight);
            Widgets.DrawHighlight(rowRect);

            string label = catalog[texturePartIndex].PartName ?? "?";

            Rect leftRect = new Rect(rowRect.x, rowRect.y, ArrowButtonWidth, rowRect.height);
            Rect rightRect = new Rect(rowRect.xMax - ArrowButtonWidth, rowRect.y, ArrowButtonWidth, rowRect.height);
            Rect centerRect = new Rect(leftRect.xMax, rowRect.y,
                rowRect.width - ArrowButtonWidth * 2f, rowRect.height);

            if (Widgets.ButtonText(leftRect, "<"))
                texturePartIndex = StepVariantIndex(texturePartIndex, catalog.Count, -1);

            if (Widgets.ButtonText(centerRect, label))
            {
                var options = new List<FloatMenuOption>();
                for (int p = 0; p < catalog.Count; p++)
                {
                    int capturedIndex = p;
                    options.Add(new FloatMenuOption(
                        catalog[p].PartName ?? "?",
                        () => texturePartIndex = capturedIndex));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            if (Widgets.ButtonText(rightRect, ">"))
                texturePartIndex = StepVariantIndex(texturePartIndex, catalog.Count, 1);

            curY += rowRect.height;
        }

        /// <summary>
        /// Scrollable grid of square thumbnails, one cell per variant of the
        /// currently selected part (<see cref="texturePartIndex"/>). Column
        /// count fits the available width; rows scroll via
        /// <see cref="textureGridScroll"/>.
        /// </summary>
        private void DrawTextureThumbnailGrid(Rect rect, List<VpweTexturePart> catalog)
        {
            List<VpweTextureVariantOption> variants = catalog[texturePartIndex].Variants;
            if (variants == null || variants.Count == 0)
                return; // Malformed part (no declared variants) — nothing to show.

            EnsureCellPathsCache(catalog, variants.Count);

            int columns = Mathf.Max(1,
                Mathf.FloorToInt((rect.width + ThumbCellGap) / (ThumbCellSize + ThumbCellGap)));
            int rows = Mathf.CeilToInt(variants.Count / (float)columns);
            float contentHeight = rows * ThumbCellSize + Mathf.Max(0, rows - 1) * ThumbCellGap;
            bool needsScroll = contentHeight > rect.height;
            float innerWidth = needsScroll ? rect.width - 16f : rect.width;
            Rect innerRect = new Rect(0f, 0f, innerWidth, contentHeight);

            Widgets.BeginScrollView(rect, ref textureGridScroll, innerRect);

            int[] selected = VPWETexPathMath.SelectedIndices(vpweTexPaths, catalog);
            int selectedIndex = (selected != null && texturePartIndex < selected.Length)
                ? selected[texturePartIndex]
                : -1;

            for (int v = 0; v < variants.Count; v++)
            {
                int col = v % columns;
                int row = v / columns;
                Rect cellRect = new Rect(
                    col * (ThumbCellSize + ThumbCellGap),
                    row * (ThumbCellSize + ThumbCellGap),
                    ThumbCellSize, ThumbCellSize);
                DrawTextureCell(cellRect, catalog, v, variants[v], v == selectedIndex);
            }

            Widgets.EndScrollView();
        }

        /// <summary>
        /// Rebuilds <see cref="cachedCellPaths"/> — one full layer-stack path
        /// list per variant of the selected part — unless the cache already
        /// matches the current (part, vpweTexPaths-reference) pair. Each
        /// entry substitutes that variant into the current selection (via
        /// <see cref="VPWETexPathMath.SelectedIndices"/>, with any
        /// unresolved/-1 part coerced to variant 0, mirroring
        /// <see cref="SelectPartVariant"/>'s own fallback) and rebuilds the
        /// full path list with <see cref="VPWETexPathMath.BuildTexPaths"/>.
        /// </summary>
        private void EnsureCellPathsCache(List<VpweTexturePart> catalog, int variantCount)
        {
            if (cachedCellPaths != null
                && cachedCellPathsPart == texturePartIndex
                && cachedCellPathsFor == vpweTexPaths)
                return;

            int[] baseIndices = VPWETexPathMath.SelectedIndices(vpweTexPaths, catalog);
            for (int i = 0; i < baseIndices.Length; i++)
            {
                if (baseIndices[i] < 0)
                    baseIndices[i] = 0;
            }

            var cells = new List<List<string>>(variantCount);
            for (int v = 0; v < variantCount; v++)
            {
                int[] cellIndices = (int[])baseIndices.Clone();
                cellIndices[texturePartIndex] = v;
                cells.Add(VPWETexPathMath.BuildTexPaths(catalog, cellIndices));
            }

            cachedCellPaths = cells;
            cachedCellPathsFor = vpweTexPaths;
            cachedCellPathsPart = texturePartIndex;
        }

        /// <summary>
        /// One thumbnail cell: draws the full composed layer stack (all
        /// outlines then all textures, in the order
        /// <see cref="VPWETexPathMath.BuildTexPaths"/> returns them) with
        /// plain <c>GUI.DrawTexture</c> calls stacked on the same rect — the
        /// GPU's alpha blending reproduces VEF's own CPU
        /// <c>Color.Lerp(bg, overlay, overlay.a)</c> composition exactly, with
        /// no VEF calls or reflection in this hot path. Tinted with the
        /// weapon's own <see cref="Thing.DrawColor"/> to match VEF's own
        /// preview coloring.
        /// </summary>
        private void DrawTextureCell(
            Rect cellRect, List<VpweTexturePart> catalog, int variantIndex,
            VpweTextureVariantOption variant, bool isSelected)
        {
            Widgets.DrawHighlightIfMouseover(cellRect);

            List<string> paths = (cachedCellPaths != null && variantIndex < cachedCellPaths.Count)
                ? cachedCellPaths[variantIndex]
                : null;
            if (paths != null)
            {
                Color prevColor = GUI.color;
                GUI.color = weapon.DrawColor;
                for (int i = 0; i < paths.Count; i++)
                {
                    Texture2D tex = LookupTexture(paths[i]);
                    if (tex != null)
                        GUI.DrawTexture(cellRect, tex, ScaleMode.ScaleToFit);
                }
                GUI.color = prevColor;
            }

            if (isSelected)
            {
                Color prevColor = GUI.color;
                GUI.color = Color.white;
                Widgets.DrawBox(cellRect, 2);
                GUI.color = prevColor;
            }

            if (!string.IsNullOrEmpty(variant.Label))
                TooltipHandler.TipRegion(cellRect, variant.Label);

            if (Widgets.ButtonInvisible(cellRect))
                SelectPartVariant(catalog, texturePartIndex, variantIndex);
        }

        /// <summary>
        /// Memoized <see cref="ContentFinder{T}"/> lookup — misses are cached
        /// too (as null) so a missing/renamed asset doesn't re-search every
        /// frame the grid is visible.
        /// </summary>
        private static Texture2D LookupTexture(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            if (TextureLookupCache.TryGetValue(path, out Texture2D cached))
                return cached;
            Texture2D tex = ContentFinder<Texture2D>.Get(path, false);
            TextureLookupCache[path] = tex;
            return tex;
        }

        /// <summary>
        /// Steps <paramref name="current"/> by <paramref name="delta"/> (±1),
        /// wrapping around <paramref name="count"/>. An unset current index
        /// (-1, e.g. texPaths not yet rolled or unmatched) starts from the
        /// first/last variant depending on direction, so the very first click
        /// lands on a real selection instead of wrapping past it. Also reused
        /// for stepping <see cref="texturePartIndex"/> through the part
        /// catalog, where current is always in-range so it behaves as a
        /// plain wraparound step.
        /// </summary>
        private static int StepVariantIndex(int current, int count, int delta)
        {
            if (count <= 0)
                return 0;
            if (current < 0)
                return delta > 0 ? 0 : count - 1;
            int next = (current + delta) % count;
            if (next < 0)
                next += count;
            return next;
        }

        /// <summary>
        /// Applies one part's variant selection and rebuilds vpweTexPaths as a
        /// fresh list via the pure helper. Any other part with no current
        /// selection (-1 — texPaths not yet rolled, or a stale/unmatched
        /// pair) falls back to its own variant 0 rather than leaving a gap,
        /// mirroring VPWETexPathMath.BuildTexPaths's own fallback.
        /// </summary>
        private void SelectPartVariant(List<VpweTexturePart> catalog, int partIndex, int newIndex)
        {
            int[] indices = VPWETexPathMath.SelectedIndices(vpweTexPaths, catalog);
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] < 0)
                    indices[i] = 0;
            }

            // Belt-and-suspenders: in the ordinary case originalVpweTexPaths is
            // already set by the ctor/lazy preview capture well before the
            // player can click anything (see BuildPreviewGraphic). But if that
            // capture ever failed to land (e.g. a runtime reflection error)
            // while the tab is still showing (its gate is the def's catalog,
            // not whether a skin got captured), establish the pre-edit
            // baseline here — otherwise TextureChanged could never detect
            // this edit and it would be silently dropped at confirm.
            if (originalVpweTexPaths == null)
                originalVpweTexPaths = VPWETexPathMath.BuildTexPaths(catalog, indices);

            indices[partIndex] = newIndex;

            List<string> rebuilt = VPWETexPathMath.BuildTexPaths(catalog, indices);
            if (rebuilt != null)
                vpweTexPaths = rebuilt;
        }
    }
}
