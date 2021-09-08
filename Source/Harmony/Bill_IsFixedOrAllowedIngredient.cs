using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace HumanResources
{
    //Tweaks train weapon recipes to ignore weapons the pawn is already proficient, 2/2
    [HarmonyPatch(typeof(Bill), "IsFixedOrAllowedIngredient", new Type[] { typeof(Thing) })]
    public static class Bill_IsFixedOrAllowedIngredient
    {
        public static bool Prefix(Thing thing, ref bool __result)
        {
            Pawn trainee = WorkGiver_DoBill_TryFindBestBillIngredients.Trainee;
            if (trainee != null)
            {
                return !trainee.TryGetComp<CompKnowledge>().knownWeapons.Contains(thing.def);
            }
            return true;
        }
    }
}
