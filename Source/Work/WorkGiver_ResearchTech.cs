using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace HumanResources
{
    class WorkGiver_ResearchTech : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.ResearchBench);
            }
        }

        public override bool Prioritized
        {
            get
            {
                return true;
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            if (techComp != null && !techComp.homework.NullOrEmpty())
            {
                bool result = !techComp.homework.Any(x => !x.IsFinished && !x.IsKnownBy(pawn));
                return result;
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_ResearchBench Desk = t as Building_ResearchBench;
            if (Desk != null && pawn.CanReserve(t, 1, -1, null, forced))
            {
                CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
                bool result = techComp.homework.Any(x => !x.IsFinished && x.CanBeResearchedAt(Desk, false) && !x.IsKnownBy(pawn) && x.RequisitesKnownBy(pawn));
                return result;
            }
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(TechJobDefOf.ResearchTech, t);
        }

        public override float GetPriority(Pawn pawn, TargetInfo t)
        {
            return t.Thing.GetStatValue(StatDefOf.ResearchSpeedFactor, true);
        }
    }
}
