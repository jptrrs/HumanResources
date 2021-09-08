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
            if (stuffToFilter == TechDefOf.Neolithic ||
                stuffToFilter == TechDefOf.Medieval ||
                stuffToFilter == TechDefOf.Industrial ||
                stuffToFilter == TechDefOf.Spacer ||
                stuffToFilter == TechDefOf.Ultra ||
                stuffToFilter == TechDefOf.Archotech
                )
            {
                if (Prefs.LogVerbose) Log.Message("[HumanResources] Skipped StuffCategoryDef: " + stuffToFilter.defName);
                return false;
            }
            return true;
        }
    }
}
