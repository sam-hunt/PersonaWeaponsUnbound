using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace PersonaWeaponsUnbound
{
    /// <summary>
    /// One texture variant option for a <see cref="VpweTexturePart"/>, as
    /// plain data — no VEF <c>TextureVariant</c> type leaks out of
    /// <see cref="VPWEIntegration"/>. <see cref="Outline"/>/<see cref="Texture"/>
    /// are the raw texPath strings used to match/build texPaths lists (see
    /// <see cref="VPWETexPathMath"/>); <see cref="Label"/> is VEF's
    /// <c>texName</c>, shown to the player.
    /// </summary>
    internal sealed class VpweTextureVariantOption
    {
        public string Label;
        public string Outline;
        public string Texture;
    }

    /// <summary>
    /// One composable graphic part (grip, blade, …) on a VPWE/VEF persona
    /// weapon def, with its declared variant options. Returned by
    /// <see cref="VPWEIntegration.GetPartCatalog"/>; part count is dynamic
    /// (3–4 on VPWE weapons observed so far) — never assume a fixed count.
    /// </summary>
    internal sealed class VpweTexturePart
    {
        public string PartName;
        public List<VpweTextureVariantOption> Variants;
    }
    /// <summary>
    /// Optional integration with Vanilla Persona Weapons Expanded (VPWE) — really
    /// with its dependency Vanilla Expanded Framework (VEF), where the composable
    /// texture system actually lives (<c>VEF.Graphics.CompGraphicCustomization</c>).
    /// VPWE only attaches VEF's comp to the Royalty persona weapons.
    ///
    /// <para>The comp gives each persona weapon a per-instance random "skin": one
    /// weighted <c>TextureVariant</c> pick per graphic part (grip, blade, …),
    /// combined into a single texture. The selection is rolled lazily — VEF's
    /// <c>TryInit()</c> fires on first graphic access only when <c>texPaths</c> is
    /// empty — so any fresh <c>ThingMaker.MakeThing</c> of a persona def rolls a
    /// brand-new skin the first time it renders.</para>
    ///
    /// <para>PWU's customization re-makes persona Things constantly: the dialog
    /// preview builds a throwaway Thing per target def, and the real weapon is
    /// destroyed+respawned at the 0↔1 trait boundary (see
    /// <see cref="WeaponDefConversion"/>). Without intervention each of those
    /// re-rolls the skin, so the preview — and the finished weapon — look nothing
    /// like the weapon the player started customizing. This helper captures the
    /// original skin (<c>texPaths</c>) once and re-applies it onto every persona
    /// Thing PWU makes, so the skin is preserved rather than rerolled.</para>
    ///
    /// <para>All access is reflection so PWU compiles and runs without VPWE/VEF.
    /// The static ctor resolves the whole surface once and logs a single warning
    /// when VEF is loaded but that surface has drifted; when VEF is absent the
    /// integration is silently unavailable and every method no-ops. Availability
    /// requires <em>every</em> member needed for a self-consistent write —
    /// crucially both <c>texPaths</c> and <c>texVariants</c>: VEF's own customize
    /// dialog seeds from <c>texVariants</c> and would throw on an empty one
    /// (<c>GetCombinedTexture([])</c>), so we never set one without the other.</para>
    /// </summary>
    internal static class VPWEIntegration
    {
        // VPWE's packageId — kept for documentation; the comp itself is VEF's, so
        // the resolved type (not the packageId) is what proves VEF is present.
        private const string PackageId = "VanillaExpanded.VPersonaWeaponsE";

        private const string CompTypeName = "VEF.Graphics.CompGraphicCustomization";
        private const string PropsTypeName = "VEF.Graphics.CompProperties_GraphicCustomization";
        private const string GraphicPartTypeName = "VEF.Graphics.GraphicPart";
        private const string TextureVariantTypeName = "VEF.Graphics.TextureVariant";

        // The comp base type (subclassed by VPWE's psychic-weapon variant, so
        // membership is tested with IsInstanceOfType, not reference equality).
        private static readonly Type CompType;
        private static readonly Type TextureVariantType;

        // The load-bearing appearance state on the comp.
        private static readonly FieldInfo TexPathsField;      // public List<string> texPaths
        private static readonly FieldInfo TexVariantsField;   // public List<TextureVariant> texVariants

        // Lazy caches on the comp — nulled so the new skin composes on next render.
        // Not required for a correct write (a freshly-made Thing has them null), so
        // they are used null-safe and do NOT gate Available.
        private static readonly FieldInfo CompGraphicIntField;  // public Graphic graphicInt
        private static readonly FieldInfo CompTextureIntField;  // private Texture2D textureInt

        // Reconstruction surface: texVariants is rebuilt from texPaths by matching
        // each (outline, texture) pair back to the def's TextureVariant instances.
        private static readonly PropertyInfo PropsProperty;              // CompProperties_GraphicCustomization Props
        private static readonly FieldInfo PropsGraphicsField;            // List<GraphicPart> graphics
        private static readonly FieldInfo GraphicPartTexVariantsField;   // List<TextureVariant> texVariants
        private static readonly FieldInfo TexVariantOutlineField;        // string outline
        private static readonly FieldInfo TexVariantTextureField;        // string texture

        // Verse.Thing.graphicInt — the Thing's own cached graphic that VEF's
        // DefaultGraphic patch stuffs on first render. Core (always present), used
        // null-safe; does not gate Available.
        private static readonly FieldInfo ThingGraphicIntField;

        // --- Optional UI-surface reflection (gates UiSurfaceAvailable, not Available) ---
        //
        // Everything below is only needed by the opt-in unified texture tab
        // (Dialog_WeaponCustomization.Texture.cs) and the float-menu
        // suppression patch — never by the core skin-preservation path
        // above, which only needs texPaths/texVariants. Kept as a separate
        // gate so a drift here can't silently disable skin preservation too.

        // The props type itself, stored so GetPartCatalog can scan a
        // ThingDef's comps list for an instance of it (a def may not have a
        // live comp instance yet — e.g. a base weapon being previewed as its
        // persona conversion target).
        private static readonly Type PropsType;

        private static readonly FieldInfo GraphicPartNameField;      // public string name
        private static readonly FieldInfo TextureVariantTexNameField; // public string texName

        // VEF.Graphics.CompGraphicCustomization.CompFloatMenuOptions(Pawn) —
        // the method the float-menu-suppression patch postfixes. Resolved on
        // the comp's own declaring type (it's an override of ThingComp's
        // virtual method); VPWE's subclass doesn't re-override it, so this is
        // the method that actually runs for VPWE weapons too.
        internal static readonly MethodInfo CompFloatMenuOptionsMethod;

        /// <summary>
        /// True when the full surface needed for a self-consistent skin write
        /// resolved. Requires the reconstruction members too: setting texPaths
        /// without a matching texVariants would make VEF's own customize dialog
        /// throw, so if any piece is missing we no-op entirely and let VEF roll as
        /// it normally would (the pre-integration behaviour) rather than corrupt
        /// the comp.
        /// </summary>
        public static bool Available =>
            CompType != null
            && TextureVariantType != null
            && TexPathsField != null
            && TexVariantsField != null
            && PropsProperty != null
            && PropsGraphicsField != null
            && GraphicPartTexVariantsField != null
            && TexVariantOutlineField != null
            && TexVariantTextureField != null;

        /// <summary>
        /// True when, in addition to <see cref="Available"/>, the extra
        /// surface needed by the opt-in unified texture tab and the
        /// float-menu-suppression patch also resolved. Gated separately from
        /// <see cref="Available"/> so a drift in this (larger, more
        /// speculative) surface can never disable the core skin-preservation
        /// path — only the opt-in UI features are affected.
        /// </summary>
        public static bool UiSurfaceAvailable =>
            Available
            && PropsType != null
            && GraphicPartNameField != null
            && TextureVariantTexNameField != null
            && CompFloatMenuOptionsMethod != null;

        // Per-def catalog cache for GetPartCatalog. A def's declared
        // graphics/variants are static content (set once at load), so this
        // never needs invalidating. A cached null means "resolved, no
        // catalog" (e.g. a base weapon def with no graphic-customization
        // comp) — distinct from "not yet looked up" (absent key).
        private static readonly Dictionary<ThingDef, List<VpweTexturePart>> partCatalogCache =
            new Dictionary<ThingDef, List<VpweTexturePart>>();

        private static bool runtimeFailureLogged;

        static VPWEIntegration()
        {
            try
            {
                CompType = GenTypes.GetTypeInAnyAssembly(CompTypeName);
                TextureVariantType = GenTypes.GetTypeInAnyAssembly(TextureVariantTypeName);
                if (CompType != null)
                {
                    TexPathsField = CompType.GetField("texPaths", BindingFlags.Public | BindingFlags.Instance);
                    if (TexPathsField != null && !typeof(List<string>).IsAssignableFrom(TexPathsField.FieldType))
                        TexPathsField = null;

                    TexVariantsField = CompType.GetField("texVariants", BindingFlags.Public | BindingFlags.Instance);
                    CompGraphicIntField = CompType.GetField("graphicInt", BindingFlags.Public | BindingFlags.Instance);
                    CompTextureIntField = CompType.GetField("textureInt", BindingFlags.NonPublic | BindingFlags.Instance);
                    PropsProperty = CompType.GetProperty("Props", BindingFlags.Public | BindingFlags.Instance);
                }

                PropsType = GenTypes.GetTypeInAnyAssembly(PropsTypeName);
                PropsGraphicsField = PropsType?.GetField("graphics", BindingFlags.Public | BindingFlags.Instance);

                Type partType = GenTypes.GetTypeInAnyAssembly(GraphicPartTypeName);
                GraphicPartTexVariantsField = partType?.GetField("texVariants", BindingFlags.Public | BindingFlags.Instance);
                GraphicPartNameField = partType?.GetField("name", BindingFlags.Public | BindingFlags.Instance);

                if (TextureVariantType != null)
                {
                    TexVariantOutlineField = TextureVariantType.GetField("outline", BindingFlags.Public | BindingFlags.Instance);
                    TexVariantTextureField = TextureVariantType.GetField("texture", BindingFlags.Public | BindingFlags.Instance);
                    TextureVariantTexNameField = TextureVariantType.GetField("texName", BindingFlags.Public | BindingFlags.Instance);
                }

                if (CompType != null)
                {
                    MethodInfo floatMenuMethod = CompType.GetMethod(
                        "CompFloatMenuOptions", BindingFlags.Public | BindingFlags.Instance);
                    if (floatMenuMethod != null
                        && typeof(IEnumerable<FloatMenuOption>).IsAssignableFrom(floatMenuMethod.ReturnType))
                        CompFloatMenuOptionsMethod = floatMenuMethod;
                }

                ThingGraphicIntField = typeof(Thing).GetField("graphicInt", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            catch (Exception ex)
            {
                Log.Warning("[Persona Weapons Unbound] VPWE/VEF graphic-customization reflection failed "
                    + "(persona-weapon skins may reroll when a weapon crosses the base<->persona boundary "
                    + "during customization): " + ex);
                return;
            }

            // VEF is "loaded" iff its comp type resolved. Warn only when it's present
            // but the surface has drifted, so players without VPWE/VEF stay quiet.
            if (CompType != null && !Available)
            {
                Log.Warning("[Persona Weapons Unbound] VEF graphic customization present ("
                    + CompTypeName + ") but its skin surface could not be fully resolved "
                    + "(texPaths/texVariants + reconstruction fields); VPWE-style persona-weapon "
                    + "skins may reroll to a random appearance when a weapon crosses the "
                    + "base<->persona boundary during customization. RimWorld/VEF API may have changed.");
            }

            // Same pattern, one level up: warn only when the core skin surface
            // resolved (Available) but the additional UI surface didn't, so
            // this doesn't fire redundantly alongside the warning above.
            if (Available && !UiSurfaceAvailable)
            {
                Log.Warning("[Persona Weapons Unbound] VEF graphic customization present but its "
                    + "extended UI surface (GraphicPart.name/TextureVariant.texName/"
                    + "CompFloatMenuOptions) could not be fully resolved; the unified VPWE texture "
                    + "tab and ground float-menu integration will stay disabled. RimWorld/VEF API "
                    + "may have changed.");
            }
        }

        /// <summary>
        /// Returns a copy of the weapon's current composed-texture selection (VEF's
        /// <c>texPaths</c>), or null when VPWE/VEF is unavailable, the weapon carries
        /// no customization comp, or it has not rolled a skin yet. The copy is a
        /// plain <c>List&lt;string&gt;</c> so it scribes trivially and carries no VEF
        /// type dependency across the dialog→spec→job lifecycle.
        /// </summary>
        public static List<string> CaptureTexPaths(Thing weapon)
        {
            if (!Available || weapon == null)
                return null;

            ThingComp comp = FindComp(weapon);
            if (comp == null)
                return null;

            try
            {
                if (TexPathsField.GetValue(comp) is List<string> paths && paths.Count > 0)
                    return new List<string>(paths);
            }
            catch (Exception ex)
            {
                LogRuntimeFailureOnce(ex);
            }
            return null;
        }

        /// <summary>
        /// Writes <paramref name="paths"/> onto the weapon's customization comp so it
        /// renders that exact skin instead of rolling a random one, keeping
        /// <c>texVariants</c> consistent with <c>texPaths</c> (rebuilt from the def's
        /// variants) and clearing the cached graphic/texture so the skin recomposes
        /// on next render.
        ///
        /// <para>No-op when unavailable, the weapon has no comp, the paths are
        /// empty/odd-length, or texVariants can't be reconstructed consistently — in
        /// the last case we deliberately leave the comp untouched (VEF rolls as
        /// usual) rather than set a texPaths/texVariants pair VEF's own dialog would
        /// choke on. Call BEFORE the Thing is first rendered (i.e. before spawn) so
        /// the very first composition already uses these paths.</para>
        ///
        /// <para>When <paramref name="weapon"/> is already spawned, also dirties its
        /// map cell's mesh (mirroring VEF's own <c>Customize()</c>) so a live
        /// re-texture — e.g. from the Texture tab's <c>OpType.Restyle</c> — actually
        /// renders instead of waiting for some other mesh invalidation.</para>
        /// </summary>
        public static void ApplyTexPaths(Thing weapon, List<string> paths)
        {
            if (!Available || weapon == null || paths == null || paths.Count == 0 || (paths.Count % 2) != 0)
                return;

            ThingComp comp = FindComp(weapon);
            if (comp == null)
                return;

            try
            {
                IList rebuiltVariants = ReconstructVariants(comp, paths);
                if (rebuiltVariants == null)
                    return; // couldn't produce a consistent texVariants — leave VEF to roll

                TexPathsField.SetValue(comp, new List<string>(paths));
                TexVariantsField.SetValue(comp, rebuiltVariants);

                // Drop cached composites so the next render rebuilds from the new
                // paths. On a freshly-made Thing these are already null; clearing is
                // defensive and keeps this correct if ever applied to a live Thing.
                CompGraphicIntField?.SetValue(comp, null);
                CompTextureIntField?.SetValue(comp, null);
                ThingGraphicIntField?.SetValue(weapon, null);

                if (weapon.Spawned)
                    weapon.Map.mapDrawer.MapMeshDirty(weapon.Position, MapMeshFlagDefOf.Things);
            }
            catch (Exception ex)
            {
                LogRuntimeFailureOnce(ex);
            }
        }

        /// <summary>
        /// Rebuilds the comp's <c>texVariants</c> list from <paramref name="paths"/>
        /// by matching each (outline, texture) pair back to a <c>TextureVariant</c>
        /// instance declared on the comp's def. Returns a correctly-typed
        /// <c>List&lt;TextureVariant&gt;</c>, or null if any pair fails to resolve
        /// (so the caller can abort rather than write an inconsistent state).
        ///
        /// <para><c>texPaths</c> is laid out as all outlines followed by all textures
        /// (VEF's <c>GetTexPaths</c>), so pair <c>i</c> is
        /// <c>(paths[i], paths[i + n])</c> for <c>n = paths.Count / 2</c>.</para>
        /// </summary>
        private static IList ReconstructVariants(ThingComp comp, List<string> paths)
        {
            object props = PropsProperty.GetValue(comp);
            if (props == null)
                return null;
            if (!(PropsGraphicsField.GetValue(props) is IEnumerable graphics))
                return null;

            // Index every declared variant by its (outline, texture) pair. A weapon's
            // texture paths are unique across parts, so this key resolves the pick
            // regardless of part ordering.
            var byKey = new Dictionary<(string, string), object>();
            foreach (object part in graphics)
            {
                if (part == null)
                    continue;
                if (!(GraphicPartTexVariantsField.GetValue(part) is IEnumerable variants))
                    continue;
                foreach (object tv in variants)
                {
                    if (tv == null)
                        continue;
                    string outline = TexVariantOutlineField.GetValue(tv) as string;
                    string texture = TexVariantTextureField.GetValue(tv) as string;
                    byKey[(outline, texture)] = tv;
                }
            }

            int n = paths.Count / 2;
            var rebuilt = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(TextureVariantType));
            for (int i = 0; i < n; i++)
            {
                if (!byKey.TryGetValue((paths[i], paths[i + n]), out object tv))
                    return null; // an unknown pair (e.g. paths from another def) — bail
                rebuilt.Add(tv);
            }
            return rebuilt;
        }

        /// <summary>
        /// Returns the declared texture-customization catalog for a ThingDef — one
        /// <see cref="VpweTexturePart"/> per composable graphic part (grip, blade, …),
        /// each with its full list of variant options — or null when the UI surface
        /// is unavailable or <paramref name="def"/> carries no
        /// <c>CompProperties_GraphicCustomization</c>. Reads the def's declared comp
        /// <em>properties</em> rather than a live comp instance, so it works for a
        /// def that has no spawned Thing yet (e.g. previewing a base→persona
        /// conversion target in the dialog). Results are cached per def — the
        /// declared catalog is static content, fixed at load.
        /// </summary>
        public static List<VpweTexturePart> GetPartCatalog(ThingDef def)
        {
            if (!UiSurfaceAvailable || def == null)
                return null;

            if (partCatalogCache.TryGetValue(def, out List<VpweTexturePart> cached))
                return cached;

            List<VpweTexturePart> catalog = BuildPartCatalog(def);
            partCatalogCache[def] = catalog;
            return catalog;
        }

        private static List<VpweTexturePart> BuildPartCatalog(ThingDef def)
        {
            object props = null;
            if (def.comps != null)
            {
                foreach (CompProperties compProps in def.comps)
                {
                    if (compProps != null && PropsType.IsInstanceOfType(compProps))
                    {
                        props = compProps;
                        break;
                    }
                }
            }
            if (props == null)
                return null;

            try
            {
                if (!(PropsGraphicsField.GetValue(props) is IEnumerable graphics))
                    return null;

                var parts = new List<VpweTexturePart>();
                foreach (object part in graphics)
                {
                    if (part == null)
                        continue;
                    if (!(GraphicPartTexVariantsField.GetValue(part) is IEnumerable variants))
                        continue;

                    var options = new List<VpweTextureVariantOption>();
                    foreach (object tv in variants)
                    {
                        if (tv == null)
                            continue;
                        options.Add(new VpweTextureVariantOption
                        {
                            Label = TextureVariantTexNameField.GetValue(tv) as string,
                            Outline = TexVariantOutlineField.GetValue(tv) as string,
                            Texture = TexVariantTextureField.GetValue(tv) as string,
                        });
                    }

                    parts.Add(new VpweTexturePart
                    {
                        PartName = GraphicPartNameField.GetValue(part) as string,
                        Variants = options,
                    });
                }
                return parts;
            }
            catch (Exception ex)
            {
                LogRuntimeFailureOnce(ex);
                return null;
            }
        }

        /// <summary>
        /// The weapon's <c>CompGraphicCustomization</c> instance (or a subclass such
        /// as VPWE's psychic-weapon variant), or null if it carries none.
        /// </summary>
        private static ThingComp FindComp(Thing weapon)
        {
            if (!(weapon is ThingWithComps twc))
                return null;
            List<ThingComp> comps = twc.AllComps;
            for (int i = 0; i < comps.Count; i++)
            {
                if (CompType.IsInstanceOfType(comps[i]))
                    return comps[i];
            }
            return null;
        }

        private static void LogRuntimeFailureOnce(Exception ex)
        {
            if (runtimeFailureLogged)
                return;
            runtimeFailureLogged = true;
            Log.Error("[Persona Weapons Unbound] VPWE/VEF graphic-customization access failed at runtime "
                + "(persona-weapon skin may reroll on def conversion); further failures suppressed: " + ex);
        }
    }
}
