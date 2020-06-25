using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace HumanResources
{
    //Tweaks train weapon recipes to ignore weapons the pawn is already proficient, 1/2
    [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestBillIngredients")]
    public class WorkGiver_DoBill_TryFindBestBillIngredients
    {
        public static Pawn Trainee;

        public static void Prefix(Bill bill, Pawn pawn)
        {
            if (bill.recipe.defName.StartsWith("TrainWeapon") && bill.recipe.requiredGiverWorkType.defName == "HR_Learn" && pawn.TryGetComp<CompKnowledge>()?.knownWeapons != null)
            {
                Trainee = pawn;
            }
        }

        public static void Postfix()
        {
            Trainee = null;
        }
    }
}
