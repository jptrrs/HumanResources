using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace HumanResources
{
    //Checks if pawn knows how to cultivate a crop.
    [HarmonyPatch(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.JobOnCell), new Type[] { typeof(Pawn), typeof(IntVec3), typeof(bool) })]
    public static class WorkGiver_GrowerSow_JobOnCell
    {
        public static void Postfix(Pawn pawn, ThingDef ___wantedPlantDef, ref Job __result)
        {
            if (__result != null)
            {
                var requisites = ___wantedPlantDef.researchPrerequisites;
                if (!requisites.NullOrEmpty())
                {
                    var knownPlants = pawn.TryGetComp<CompKnowledge>().knownPlants;
                    if (Prefs.LogVerbose) Log.Warning(pawn + "'s plant knowledge: " + knownPlants);
                    bool flag = true;
                    if (!knownPlants.EnumerableNullOrEmpty()) flag = knownPlants.Contains(___wantedPlantDef);
                    else flag = false;
                    if (!flag)
                    {
                        string preReqText = requisites.Any() ? (string)"MultiplePrerequisites".Translate() : requisites.FirstOrDefault().label;
                        JobFailReason.Is("DoesntKnowThisPlant".Translate(pawn, ___wantedPlantDef, preReqText));
                        __result = null;
                    }
                }
            }
        }
    }
}
