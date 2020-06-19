using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace HumanResources
{
    class MaterialFilter_Patch
    {
        public static void Execute(Harmony instance)
        {
            Type MaterialFilterType = AccessTools.TypeByName("MaterialFilter");
            instance.Patch(AccessTools.Method(MaterialFilterType, "createSpecialThingFilterDef"),
                new HarmonyMethod(typeof(MaterialFilter_Patch), nameof(CreateSpecialThingFilterDef_Prefix)), null, null);
        }

        public static bool CreateSpecialThingFilterDef_Prefix(StuffCategoryDef stuffToFilter)
        {
            if (stuffToFilter.defName == "Technic")
            {
                Log.Message("[HumanResources] Skipped StuffCategoryDef: " + stuffToFilter.defName);
                return false;
            }
            return true;
        }
    }
}
