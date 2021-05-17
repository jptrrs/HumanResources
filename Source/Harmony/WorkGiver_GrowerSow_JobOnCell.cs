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
            if (pawn.TechBound())
            {
                var requisites = ___wantedPlantDef.plant?.sowResearchPrerequisites;
                if (!requisites.NullOrEmpty())
                {
                    var knownPlants = pawn.TryGetComp<CompKnowledge>().knownPlants;
                    if (knownPlants == null)
                    {
                        Log.Error($"[HumanResources] {pawn} plant knowledge is null. Can't plant. This is critical.");
                        __result = null;
                        return;
                    }
                    if (Prefs.LogVerbose) 
                        Log.Message("[HumanResources] "+pawn + "'s plant knowledge: " + Diagnostic.ExpandEnumerableSafelyToString(knownPlants));
                    if (!knownPlants.Contains(___wantedPlantDef))
                    {
                        var missing = requisites.Where(x => !x.IsKnownBy(pawn));
                        string preReqText = (missing.Count() > 1) ? missing.Select(x => x.label).ToStringSafeEnumerable() : missing.FirstOrDefault().label;
                        JobFailReason.Is("DoesntKnowThisPlant".Translate(pawn, ___wantedPlantDef, preReqText));
                        __result = null;
                    }
                }
            }
        }
    }
}
