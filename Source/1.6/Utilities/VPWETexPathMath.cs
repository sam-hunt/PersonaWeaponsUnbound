using System.Collections.Generic;

namespace PersonaWeaponsUnbound
{
    // Pure index/texPaths math for the VPWE/VEF unified texture tab, split out of
    // VPWEIntegration so it has zero reflection dependency and no
    // live RimWorld/VEF assembly requirement — it's exercised directly by the
    // xUnit suite. Mirrors VEF's own texPaths layout
    // (CompGraphicCustomization.GetTexPaths): all outlines in part order,
    // followed by all textures in the same order.
    internal static class VPWETexPathMath
    {
        // For each part in catalog, finds the index of the
        // variant currently selected by texPaths, by matching
        // its (outline, texture) pair against that part's declared variants.
        //
        // Returns an array the same length as catalog
        // (or null if catalog itself is null). Any part whose
        // pair can't be resolved gets -1 — including every part when
        // texPaths is null, empty, or the wrong length for
        // this catalog (e.g. not rolled yet, or paths from a different def) —
        // callers should treat -1 as "no selection yet", not throw.
        public static int[] SelectedIndices(List<string> texPaths, List<VpweTexturePart> catalog)
        {
            if (catalog == null)
                return null;

            int n = catalog.Count;
            var result = new int[n];
            bool havePaths = texPaths != null && texPaths.Count == n * 2;

            for (int i = 0; i < n; i++)
            {
                result[i] = -1;
                if (!havePaths)
                    continue;

                string outline = texPaths[i];
                string texture = texPaths[i + n];
                List<VpweTextureVariantOption> variants = catalog[i]?.Variants;
                if (variants == null)
                    continue;

                for (int v = 0; v < variants.Count; v++)
                {
                    if (variants[v].Outline == outline && variants[v].Texture == texture)
                    {
                        result[i] = v;
                        break;
                    }
                }
            }
            return result;
        }

        // Rebuilds a texPaths list (VEF's layout: all outlines then all
        // textures, both in part order) from one selected variant index per
        // part in catalog. Always returns a fresh list —
        // never mutates any input — so reference-equality checks (e.g. the
        // dialog's dirty-tracking) see every selection change.
        //
        // An index that's negative, out of range, or simply absent
        // (indices shorter than catalog)
        // falls back to variant 0 for that part rather than throwing, so a
        // stale/-1 selection can't corrupt the whole list. Returns null for a
        // null/empty catalog, or if any part has no declared variants at all
        // (nothing to build).
        public static List<string> BuildTexPaths(List<VpweTexturePart> catalog, int[] indices)
        {
            if (catalog == null || catalog.Count == 0)
                return null;

            int n = catalog.Count;
            var outlines = new List<string>(n);
            var textures = new List<string>(n);

            for (int i = 0; i < n; i++)
            {
                List<VpweTextureVariantOption> variants = catalog[i]?.Variants;
                if (variants == null || variants.Count == 0)
                    return null; // malformed part — can't build a consistent list

                int idx = (indices != null && i < indices.Length) ? indices[i] : 0;
                if (idx < 0 || idx >= variants.Count)
                    idx = 0;

                outlines.Add(variants[idx].Outline);
                textures.Add(variants[idx].Texture);
            }

            var result = new List<string>(n * 2);
            result.AddRange(outlines);
            result.AddRange(textures);
            return result;
        }
    }
}
