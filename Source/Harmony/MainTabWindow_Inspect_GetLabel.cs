using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;

namespace HumanResources
{
    //Fix for the things inspect panel label truncating to the wrong size.
    [HarmonyPatch(typeof(MainTabWindow_Inspect), "GetLabel", new Type[] { typeof(Rect) })]
    public static class MainTabWindow_Inspect_GetLabel
    {
        public static void Prefix(MainTabWindow_Inspect __instance, ref Rect rect)
        {
            rect.width = InspectPaneUtility.PaneWidthFor(__instance) - 72f;
        }

    }
}