using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;

namespace HumanResources
{
    //Checks if pawn knows how to cultivate a crop.
    [HarmonyPatch(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.JobOnCell), new Type[] { typeof(Pawn), typeof(IntVec3), typeof(bool) })]
    public static class WorkGiver_GrowerSow_JobOnCell
    {
        public static void Postfix(object __instance, Pawn pawn, ThingDef ___wantedPlantDef, ref Job __result)
        {
            if (pawn.Faction != null && pawn.Faction.IsPlayer && __result != null && pawn.RaceProps.Humanlike && pawn.TryGetComp<CompKnowledge>() != null)
            {
                var requisites = ___wantedPlantDef.plant?.sowResearchPrerequisites;
                if (!requisites.NullOrEmpty())
                {
                    var knownPlants = pawn.TryGetComp<CompKnowledge>().knownPlants;
                    if (Prefs.LogVerbose) Log.Warning("[HumanResources] "+pawn + "'s plant knowledge: " + knownPlants);
                    bool flag = false;
                    if (!knownPlants.EnumerableNullOrEmpty()) flag = knownPlants.Contains(___wantedPlantDef);
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
