using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace HumanResources
{
    //Checks if pawn knows how to repair something.
    [HarmonyPatch(typeof(WorkGiver_Repair), nameof(WorkGiver_Repair.HasJobOnThing), new Type[] { typeof(Pawn), typeof(Thing), typeof(bool) })]
    public static class WorkGiver_Repair_HasJobOnThing
    {
        public static void PostFix(Pawn pawn, Thing t, ref bool __result)
        {
            if (__result && pawn.RaceProps.Humanlike && pawn.TryGetComp<CompKnowledge>() != null)
            {
                var requisites = t.def.researchPrerequisites;
                if (!requisites.NullOrEmpty())
                {
                    __result = pawn.TryGetComp<CompKnowledge>().expertise.Any(x => requisites.Contains(x.Key) && x.Value >= 1f);
                    if (!__result)
                    {
                        string preReqText = (requisites.Count() > 1) ? (string)"MultiplePrerequisites".Translate() : requisites.FirstOrDefault().label;
                        JobFailReason.Is("DoesntKnowHowToRepair".Translate(pawn, t.def.label, preReqText));
                    }
                }
            }
        }
    }
}
