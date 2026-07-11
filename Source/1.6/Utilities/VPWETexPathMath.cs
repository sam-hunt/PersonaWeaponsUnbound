using System.Collections.Generic;

namespace PersonaWeaponsUnbound
{
    /// <summary>
    /// Pure index/texPaths math for the VPWE/VEF unified texture tab, split out of
    /// <see cref="VPWEIntegration"/> so it has zero reflection dependency and no
    /// live RimWorld/VEF assembly requirement — it's exercised directly by the
    /// xUnit suite. Mirrors VEF's own texPaths layout
    /// (<c>CompGraphicCustomization.GetTexPaths</c>): all outlines in part order,
    /// followed by all textures in the same order.
    /// </summary>
    internal static class VPWETexPathMath
    {
        /// <summary>
        /// For each part in <paramref name="catalog"/>, finds the index of the
        /// variant currently selected by <paramref name="texPaths"/>, by matching
        /// its (outline, texture) pair against that part's declared variants.
        ///
        /// <para>Returns an array the same length as <paramref name="catalog"/>
        /// (or null if <paramref name="catalog"/> itself is null). Any part whose
        /// pair can't be resolved gets -1 — including every part when
        /// <paramref name="texPaths"/> is null, empty, or the wrong length for
        /// this catalog (e.g. not rolled yet, or paths from a different def) —
        /// callers should treat -1 as "no selection yet", not throw.</para>
        /// </summary>
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

        /// <summary>
        /// Rebuilds a texPaths list (VEF's layout: all outlines then all
        /// textures, both in part order) from one selected variant index per
        /// part in <paramref name="catalog"/>. Always returns a fresh list —
        /// never mutates any input — so reference-equality checks (e.g. the
        /// dialog's dirty-tracking) see every selection change.
        ///
        /// <para>An index that's negative, out of range, or simply absent
        /// (<paramref name="indices"/> shorter than <paramref name="catalog"/>)
        /// falls back to variant 0 for that part rather than throwing, so a
        /// stale/-1 selection can't corrupt the whole list. Returns null for a
        /// null/empty catalog, or if any part has no declared variants at all
        /// (nothing to build).</para>
        /// </summary>
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
