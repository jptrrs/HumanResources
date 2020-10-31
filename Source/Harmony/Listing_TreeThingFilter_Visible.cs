using HarmonyLib;
using System;
using Verse;

namespace HumanResources
{
    //Tweaks visibility of technical books category, 2/2
    [HarmonyPatch(typeof(Listing_TreeThingFilter), "Visible", new Type[] { typeof(ThingDef) })]
    public static class Listing_TreeThingFilter_Visible
    {
        public static void Postfix(ThingDef td, ref bool __result)
        {
            __result = HarmonyPatches.VisibleBooksCategory && td.IsWithinCategory(TechDefOf.Knowledge);
            //{
            //    if (FutureTech) __result = !ModBaseHumanResources.unlocked.techByStuff[td].IsFinished;
            //    else if (CurrentTech) __result = ModBaseHumanResources.unlocked.techByStuff[td].IsFinished;
            //    else __result = true;
            //}
            //else if (HarmonyPatches.WeaponTrainingSelection)
            //{
            //    __result = !ModBaseHumanResources.UniversalWeapons.Contains(td);
            //}
        }
    }
}
