using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HumanResources
{
    [StaticConstructorOnStartup]
    public static class ResearchTree_Assets
    {
        private static Type
            AssetsType = ResearchTree_Patches.AssetsType(),
            LinesType = ResearchTree_Patches.LinesType();
        public static object Assets;
        public static Dictionary<TechLevel, Color>
            ColorCompleted = (Dictionary<TechLevel, Color>)AccessTools.Field(AssetsType, "ColorCompleted").GetValue(Assets),
            ColorAvailable = (Dictionary<TechLevel, Color>)AccessTools.Field(AssetsType, "ColorAvailable").GetValue(Assets),
            ColorUnavailable = (Dictionary<TechLevel, Color>)AccessTools.Field(AssetsType, "ColorUnavailable").GetValue(Assets);
        public static Texture2D
            Button = (Texture2D)AccessTools.Field(AssetsType, "Button").GetValue(Assets),
            ButtonActive = (Texture2D)AccessTools.Field(AssetsType, "ButtonActive").GetValue(Assets),
            ResearchIcon = (Texture2D)AccessTools.Field(AssetsType, "ResearchIcon").GetValue(Assets),
            MoreIcon = (Texture2D)AccessTools.Field(AssetsType, "MoreIcon").GetValue(Assets),
            Lock = (Texture2D)AccessTools.Field(AssetsType, "Lock").GetValue(Assets),
            EW = (Texture2D)AccessTools.Field(LinesType, "EW").GetValue(Assets);
    }
}

