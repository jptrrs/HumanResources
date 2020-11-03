using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;

namespace HumanResources
{
    //[HarmonyPatch(typeof(JobDriver_ApplyTechprint), "TryMakePreToilReservations")]
    public static class JobDriver_ApplyTechprint_TryMakePreToilReservations
    {
        private static PropertyInfo TechprintInfo = AccessTools.Property(typeof(JobDriver_ApplyTechprint), "Techprint");

        public static bool Prefix(JobDriver_ApplyTechprint __instance, bool errorOnFailed, ref bool __result)
        {
            __result = __instance.pawn.Reserve(__instance.job.GetTarget(TargetIndex.A).Thing, __instance.job, 1, -1, null, errorOnFailed) && __instance.pawn.Reserve((Thing)TechprintInfo.GetValue(__instance), __instance.job, 1, -1, null, errorOnFailed);
            return false;
        }
    }


}
