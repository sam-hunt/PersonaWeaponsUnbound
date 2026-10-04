using UnityEngine;
using Verse;

namespace PersonaWeaponsUnbound
{
    // Keeps the attribute so the first resolve happens on the main thread
    // during CallAll (vanilla's canonical asset-loading slot). The texture is
    // re-resolved lazily rather than held in a readonly field because an
    // in-process play-data reload (main-menu language switch) destroys every
    // mod texture (ModContentHolder.ClearDestroy) while the type initializer
    // never runs again; a destroyed Texture2D compares equal to null through
    // Unity's == overload, which is exactly the check below.
    [StaticConstructorOnStartup]
    public static class PWU_Textures
    {
        private const string CustomizePath = "UI/PWU_Customize";

        private static Texture2D customize;

        static PWU_Textures()
        {
            customize = ContentFinder<Texture2D>.Get(CustomizePath);
        }

        public static Texture2D Customize
        {
            get
            {
                // Unity-overloaded ==, deliberately not ?? (see CLAUDE.md).
                if (customize == null)
                    customize = ContentFinder<Texture2D>.Get(CustomizePath);
                return customize;
            }
        }
    }
}
