using HarmonyLib;
using System;
using Verse;

namespace HumanResources
{
    //Cover for ResearchBench modifications
    //[HarmonyPatch(typeof(ThingListGroupHelper), "Includes", new Type[] { typeof(ThingRequestGroup), typeof(ThingDef) })]
    public static class ThingListGroupHelper_Includes
    {
        public static bool Prefix(ThingRequestGroup group, ThingDef def, ref bool __result)
        {
            if (group == ThingRequestGroup.ResearchBench)
            {
                __result = def.defName.EndsWith("ResearchBench");
                return false;
            }
            return true;
        }
    }
}
