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
            //if (pawn.Faction != null && pawn.Faction.IsPlayer && pawn.RaceProps.Humanlike && pawn.TryGetComp<CompKnowledge>() != null)
            if (pawn.TechBound())
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
                    bool result = requisites.All(x => x.IsKnownBy(pawn));
                    if (!result)
                    {
                        var missing = requisites.Where(x => !x.IsKnownBy(pawn));
                        string preReqText = (missing.Count() > 1) ? missing.Select(x => x.label).ToStringSafeEnumerable() : missing.FirstOrDefault().label;
                        JobFailReason.Is("DoesntKnowHowToBuild".Translate(pawn, t.def.entityDefToBuild.label, preReqText));
                    }
                    return result;
                }
            }
            return true;
        }
    }
}
