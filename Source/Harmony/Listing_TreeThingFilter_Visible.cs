using HarmonyLib;
using System;
using Verse;

namespace HumanResources
{
    //Tweaks to ingredients visibility on knowledge recipes, 3/3
    [HarmonyPatch(typeof(Listing_TreeThingFilter), "Visible", new Type[] { typeof(ThingDef) })]
    public static class Listing_TreeThingFilter_Visible
    {
        public static void Postfix(ThingDef td, ref bool __result)
        {
            if (HarmonyPatches.Ball && td.IsWithinCategory(DefDatabase<ThingCategoryDef>.GetNamed("Knowledge")))
            {
                if (HarmonyPatches.FutureTech) __result = !ModBaseHumanResources.unlocked.techByStuff[td].IsFinished;
                else if (HarmonyPatches.CurrentTech) __result = ModBaseHumanResources.unlocked.techByStuff[td].IsFinished;
                else __result = true;
            }
            //else if (HarmonyPatches.WeaponTrainingSelection)
            //{
            //    __result = ModBaseHumanResources.unlocked.weapons.Contains(td);
            //}
        }
    }
}
