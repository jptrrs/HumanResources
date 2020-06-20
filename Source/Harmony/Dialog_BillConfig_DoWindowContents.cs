using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using UnityEngine;

namespace HumanResources
{
    //Tweaks to ingredients visibility on knowledge recipes, 1/3
    [HarmonyPatch(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.DoWindowContents), new Type[] { typeof(Rect) })]
    public static class Dialog_BillConfig_DoWindowContents
    {
        private static FieldInfo billInfo = AccessTools.Field(typeof(Dialog_BillConfig), "bill");

        public static void Prefix(Dialog_BillConfig __instance)
        {
            Bill_Production bill = billInfo.GetValue(__instance) as Bill_Production;
            if (bill.UsesKnowledge())
            {
                if (bill.IsResearch()) HarmonyPatches.FutureTech = true;
                else HarmonyPatches.CurrentTech = true;
            }
            //if (bill.IsWeaponsTraining()) HarmonyPatches.WeaponTrainingSelection = true;
        }

        public static void Postfix()
        {
            HarmonyPatches.CurrentTech = false;
            HarmonyPatches.FutureTech = false;
            //HarmonyPatches.WeaponTrainingSelection = false;
        }
    }
}
