using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using RimWorld;
using Verse;

namespace HumanResources
{
    public static class ResearchTree_Tree
    {
        private static Type AssetsType = ResearchTree_Patches.TreeType();
        public static object Assets;
        public static List<TechLevel> RelevantTechLevels = (List<TechLevel>)AccessTools.Property(AssetsType, "RelevantTechLevels").GetValue(Assets);
    }
}
        
