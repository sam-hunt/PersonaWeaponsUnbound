using UnityEngine;
using Verse;

namespace PersonaWeaponsUnbound
{
    [StaticConstructorOnStartup]
    public static class PWU_Textures
    {
        public static readonly Texture2D Customize =
            ContentFinder<Texture2D>.Get("UI/PWU_Customize");

        // Vanilla overlay icons for color swatches (Ideology DLC)
        public static readonly Texture2D FavoriteColor =
            ContentFinder<Texture2D>.Get("UI/Icons/ColorSelector/ColorFavourite", false);
        public static readonly Texture2D IdeoColor =
            ContentFinder<Texture2D>.Get("UI/Icons/ColorSelector/ColorIdeology", false);
    }
}
