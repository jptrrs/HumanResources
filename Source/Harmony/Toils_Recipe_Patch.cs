using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace HumanResources
{
    //Access to the ConsumeIngredients method
    [HarmonyPatch]
    public static class Toils_Recipe_Patch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Toils_Recipe), "ConsumeIngredients")]
        public static void ConsumeIngredients(List<Thing> ingredients, RecipeDef recipe, Map map)
        {
            { throw new NotImplementedException("ConsumeIngredients reverse patch"); }
        }
    }
}
