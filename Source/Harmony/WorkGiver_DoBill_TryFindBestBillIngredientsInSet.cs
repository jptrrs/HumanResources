using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    //Tweaks train weapon recipes processing to considear all selected weapons.
    [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestBillIngredientsInSet")]
    public class WorkGiver_DoBill_TryFindBestBillIngredientsInSet
    {
        public static bool Prefix(List<Thing> availableThings, Bill bill, List<ThingCount> chosen, ref bool __result)
        {
            Pawn trainee = WorkGiver_DoBill_TryFindBestBillIngredients.Trainee;
            if (trainee != null)
            {
                chosen.Clear();
                availableThings.Sort((Thing t, Thing t2) => bill.recipe.IngredientValueGetter.ValuePerUnitOf(t2.def).CompareTo(bill.recipe.IngredientValueGetter.ValuePerUnitOf(t.def)));
                for (int i = 0; i < bill.recipe.ingredients.Count; i++)
                {
                    IngredientCount ingredientCount = bill.recipe.ingredients[i];
                    for (int j = 0; j < availableThings.Count; j++)
                    {
                        Thing thing = availableThings[j];
                        if (ingredientCount.filter.Allows(thing) && (ingredientCount.IsFixedIngredient || bill.ingredientFilter.Allows(thing)))
                        {
                            ThingCountUtility.AddToList(chosen, thing, 1);
                        }
                    }
                }
                __result = true;
                return false;
            }
            return true;
        }
    }
}
