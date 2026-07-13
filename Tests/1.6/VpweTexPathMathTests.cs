using System.Collections.Generic;
using Xunit;

namespace PersonaWeaponsUnbound.Tests
{
    // Unit tests for VPWETexPathMath — pure index/texPaths math for
    // the VPWE/VEF unified texture tab. No live RimWorld/VEF assembly needed:
    // VpweTexturePart/VpweTextureVariantOption are
    // plain data, reachable here via InternalsVisibleTo (see
    // Source/1.6/Properties/AssemblyInfo.cs).
    public class VpweTexPathMathTests
    {
        private static VpweTexturePart MakePart(string name, params (string label, string outline, string texture)[] variants)
        {
            var options = new List<VpweTextureVariantOption>();
            foreach ((string label, string outline, string texture) v in variants)
            {
                options.Add(new VpweTextureVariantOption
                {
                    Label = v.label,
                    Outline = v.outline,
                    Texture = v.texture,
                });
            }
            return new VpweTexturePart { PartName = name, Variants = options };
        }

        // Three parts, two variants each — mirrors a typical dynamic-count
        // VPWE catalog without hardcoding the fixed 4-part shape.
        private static List<VpweTexturePart> ThreePartCatalog() => new List<VpweTexturePart>
        {
            MakePart("Grip", ("Plain", "grip_plain_outline", "grip_plain_tex"),
                             ("Ornate", "grip_ornate_outline", "grip_ornate_tex")),
            MakePart("Blade", ("Steel", "blade_steel_outline", "blade_steel_tex"),
                              ("Gold", "blade_gold_outline", "blade_gold_tex")),
            MakePart("Pommel", ("Round", "pommel_round_outline", "pommel_round_tex"),
                               ("Spiked", "pommel_spiked_outline", "pommel_spiked_tex")),
        };

        [Fact]
        public void BuildThenSelect_RoundTrips()
        {
            List<VpweTexturePart> catalog = ThreePartCatalog();
            int[] indices = { 1, 0, 1 };

            List<string> texPaths = VPWETexPathMath.BuildTexPaths(catalog, indices);
            Assert.NotNull(texPaths);
            Assert.Equal(6, texPaths.Count);

            int[] roundTripped = VPWETexPathMath.SelectedIndices(texPaths, catalog);
            Assert.Equal(indices, roundTripped);
        }

        [Fact]
        public void BuildTexPaths_LayoutIsAllOutlinesThenAllTextures()
        {
            List<VpweTexturePart> catalog = ThreePartCatalog();
            List<string> texPaths = VPWETexPathMath.BuildTexPaths(catalog, new[] { 0, 1, 0 });

            Assert.Equal(new List<string>
            {
                "grip_plain_outline", "blade_gold_outline", "pommel_round_outline",
                "grip_plain_tex", "blade_gold_tex", "pommel_round_tex",
            }, texPaths);
        }

        [Fact]
        public void SelectedIndices_UnmatchedPair_ReturnsNegativeOne()
        {
            List<VpweTexturePart> catalog = ThreePartCatalog();
            // A texPaths list that matches parts 0 and 2 but has a garbage
            // pair for part 1 (not any declared variant).
            var texPaths = new List<string>
            {
                "grip_plain_outline", "not_a_real_outline", "pommel_spiked_outline",
                "grip_plain_tex", "not_a_real_tex", "pommel_spiked_tex",
            };

            int[] indices = VPWETexPathMath.SelectedIndices(texPaths, catalog);
            Assert.Equal(new[] { 0, -1, 1 }, indices);
        }

        [Fact]
        public void SelectedIndices_NullTexPaths_ReturnsAllNegativeOne()
        {
            List<VpweTexturePart> catalog = ThreePartCatalog();
            int[] indices = VPWETexPathMath.SelectedIndices(null, catalog);
            Assert.Equal(new[] { -1, -1, -1 }, indices);
        }

        [Fact]
        public void SelectedIndices_WrongLengthTexPaths_ReturnsAllNegativeOne()
        {
            List<VpweTexturePart> catalog = ThreePartCatalog();
            // Only 4 entries — wrong for a 3-part catalog (needs 6).
            var texPaths = new List<string> { "a", "b", "c", "d" };

            int[] indices = VPWETexPathMath.SelectedIndices(texPaths, catalog);
            Assert.Equal(new[] { -1, -1, -1 }, indices);
        }

        [Fact]
        public void SelectedIndices_NullCatalog_ReturnsNull()
        {
            Assert.Null(VPWETexPathMath.SelectedIndices(new List<string> { "a", "b" }, null));
        }

        [Fact]
        public void BuildTexPaths_NullOrEmptyCatalog_ReturnsNull()
        {
            Assert.Null(VPWETexPathMath.BuildTexPaths(null, new[] { 0 }));
            Assert.Null(VPWETexPathMath.BuildTexPaths(new List<VpweTexturePart>(), new[] { 0 }));
        }

        [Fact]
        public void BuildTexPaths_NullIndices_FallsBackToVariantZeroForEveryPart()
        {
            List<VpweTexturePart> catalog = ThreePartCatalog();
            List<string> texPaths = VPWETexPathMath.BuildTexPaths(catalog, null);

            Assert.Equal(new List<string>
            {
                "grip_plain_outline", "blade_steel_outline", "pommel_round_outline",
                "grip_plain_tex", "blade_steel_tex", "pommel_round_tex",
            }, texPaths);
        }

        [Fact]
        public void BuildTexPaths_OutOfRangeOrNegativeIndex_FallsBackToVariantZero()
        {
            List<VpweTexturePart> catalog = ThreePartCatalog();
            // part 0: -1 (unset), part 1: 99 (out of range), part 2: valid (1)
            List<string> texPaths = VPWETexPathMath.BuildTexPaths(catalog, new[] { -1, 99, 1 });

            Assert.Equal(new List<string>
            {
                "grip_plain_outline", "blade_steel_outline", "pommel_spiked_outline",
                "grip_plain_tex", "blade_steel_tex", "pommel_spiked_tex",
            }, texPaths);
        }

        [Fact]
        public void BuildTexPaths_ShorterIndicesArray_FallsBackForMissingParts()
        {
            List<VpweTexturePart> catalog = ThreePartCatalog();
            // Only one index supplied for a 3-part catalog.
            List<string> texPaths = VPWETexPathMath.BuildTexPaths(catalog, new[] { 1 });

            Assert.Equal(new List<string>
            {
                "grip_ornate_outline", "blade_steel_outline", "pommel_round_outline",
                "grip_ornate_tex", "blade_steel_tex", "pommel_round_tex",
            }, texPaths);
        }

        [Fact]
        public void BuildTexPaths_PartWithNoVariants_ReturnsNull()
        {
            var catalog = new List<VpweTexturePart>
            {
                MakePart("Grip", ("Plain", "grip_plain_outline", "grip_plain_tex")),
                new VpweTexturePart { PartName = "Empty", Variants = new List<VpweTextureVariantOption>() },
            };

            Assert.Null(VPWETexPathMath.BuildTexPaths(catalog, new[] { 0, 0 }));
        }

        [Fact]
        public void DynamicPartCount_FourParts_RoundTrips()
        {
            var catalog = new List<VpweTexturePart>
            {
                MakePart("Grip", ("A", "o0", "t0"), ("B", "o1", "t1")),
                MakePart("Blade", ("A", "o2", "t2"), ("B", "o3", "t3")),
                MakePart("Guard", ("A", "o4", "t4"), ("B", "o5", "t5")),
                MakePart("Pommel", ("A", "o6", "t6"), ("B", "o7", "t7")),
            };
            int[] indices = { 1, 1, 0, 1 };

            List<string> texPaths = VPWETexPathMath.BuildTexPaths(catalog, indices);
            Assert.Equal(8, texPaths.Count);
            Assert.Equal(indices, VPWETexPathMath.SelectedIndices(texPaths, catalog));
        }
    }
}
