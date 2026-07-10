using UnityEngine;
using Verse;

namespace PersonaWeaponsUnbound
{
    [StaticConstructorOnStartup]
    public static class PWU_Textures
    {
        public static readonly Texture2D Customize =
            ContentFinder<Texture2D>.Get("UI/PWU_Customize");
    }
}
