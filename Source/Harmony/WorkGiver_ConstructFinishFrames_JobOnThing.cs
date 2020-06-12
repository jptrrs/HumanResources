using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace HumanResources
{
    //Checks if pawn knows how to build something.
    [HarmonyPatch(typeof(WorkGiver_ConstructFinishFrames), nameof(WorkGiver_ConstructFinishFrames.JobOnThing), new Type[] { typeof(Pawn), typeof(Thing), typeof(bool) })]
    public static class WorkGiver_ConstructFinishFrames_JobOnThing
    {
        public static bool Prefix(Pawn pawn, Thing t)
        {
            if (pawn.RaceProps.Humanlike)
            {
                if (t.Faction != pawn.Faction)
                {
                    return true;
                }
                Frame frame = t as Frame;
                if (frame == null)
                {
                    return true;
                }
                if (frame.MaterialsNeeded().Count > 0)
                {
                    return true;
                }
                var requisites = t.def.entityDefToBuild.researchPrerequisites;
                if (!requisites.NullOrEmpty())
                {
                    string preReqText = (requisites.Count() > 1) ? (string)"MultiplePrerequisites".Translate() : requisites.FirstOrDefault().label;
                    JobFailReason.Is("DoesntKnowHowToBuild".Translate(pawn, t.def.entityDefToBuild.label, preReqText));
                    return pawn.TryGetComp<CompKnowledge>().expertise.Any(x => requisites.Contains(x.Key) && x.Value >= 1f);
                }
            }
            return true;
        }
    }
}
