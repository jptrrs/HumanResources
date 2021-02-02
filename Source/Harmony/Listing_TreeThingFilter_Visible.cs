using HarmonyLib;
using System;
using Verse;

namespace HumanResources
{
    //Tweaks visibility of technical books category, 2/2
    [HarmonyPatch(typeof(Listing_TreeThingFilter), "Visible", new Type[] { typeof(ThingDef) })]
    public static class Listing_TreeThingFilter_Visible
    {
        public static void Postfix(ThingDef td, ThingFilter ___parentFilter, ref bool __result)
        {
            if (HarmonyPatches.VisibleBooksCategory)
            {
                __result = td.IsWithinCategory(TechDefOf.Knowledge) && ___parentFilter.Allows(td);
            }
        }
    }
}
