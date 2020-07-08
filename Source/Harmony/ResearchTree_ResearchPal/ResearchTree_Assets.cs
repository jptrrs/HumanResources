using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using RimWorld;
using Verse;

namespace HumanResources
{
    [StaticConstructorOnStartup]
    public static class ResearchTree_Assets
    {
        private static Type AssetsType = ResearchTree_Patches.AssetsType();
        public static object Assets; 
        public static Dictionary<TechLevel, Color> ColorCompleted = (Dictionary<TechLevel, Color>) AccessTools.Field(AssetsType, "ColorCompleted").GetValue(Assets);
        public static Dictionary<TechLevel, Color> ColorAvailable = (Dictionary<TechLevel, Color>)AccessTools.Field(AssetsType, "ColorAvailable").GetValue(Assets);
        public static Dictionary<TechLevel, Color> ColorUnavailable = (Dictionary<TechLevel, Color>) AccessTools.Field(AssetsType, "ColorUnavailable").GetValue(Assets);
        public static Texture2D Button = (Texture2D) AccessTools.Field(AssetsType, "Button").GetValue(Assets);
        public static Texture2D ButtonActive = (Texture2D) AccessTools.Field(AssetsType, "ButtonActive").GetValue(Assets);
        public static Texture2D ResearchIcon = (Texture2D) AccessTools.Field(AssetsType, "ResearchIcon").GetValue(Assets);
        public static Texture2D MoreIcon = (Texture2D) AccessTools.Field(AssetsType, "MoreIcon").GetValue(Assets);
        public static Texture2D Lock = (Texture2D) AccessTools.Field(AssetsType, "Lock").GetValue(Assets);
    }
}
        
