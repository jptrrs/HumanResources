using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;
using JPTools;

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
                    if (Prefs.LogVerbose) Log.Warning("[HumanResources] " + pawn + "'s plant knowledge: " + knownPlants);
                    bool flag = false;
                    if (!knownPlants.EnumerableNullOrEmpty()) flag = knownPlants.Contains(___wantedPlantDef);
                    if (!flag)
                    {
                        var missing = requisites.Where(x => !x.IsKnownBy(pawn));
                        string preReqText = (missing.Count() > 1) ? missing.Select(Utility.DefLabelFailSafe).ToStringSafeEnumerable() : Utility.DefLabelFailSafe(missing.FirstOrDefault());
                        JobFailReason.Is("DoesntKnowThisPlant".Translate(pawn, ___wantedPlantDef, preReqText));
                        __result = null;
                    }
                }
            }
        }
    }
}
