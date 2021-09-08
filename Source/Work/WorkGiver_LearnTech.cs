using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace HumanResources
{
    class WorkGiver_LearnTech : WorkGiver_Knowledge
    {
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (!base.ShouldSkip(pawn, forced))
            {
                IEnumerable<ResearchProjectDef> available = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(x => x.IsFinished);
                return !available.Any();
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            int tick = Find.TickManager.TicksGame;
            if (actualJob == null || lastVerifiedJobTick != tick || Find.TickManager.Paused)
            {
                actualJob = null;
                IBillGiver billGiver = thing as IBillGiver;
                if (billGiver != null && ThingIsUsableBillGiver(thing) && billGiver.BillStack.AnyShouldDoNow && billGiver.UsableForBillsAfterFueling())
                {
                    LocalTargetInfo target = thing;
                    if (pawn.CanReserve(target, 1, -1, null, forced) && !thing.IsBurning() && !thing.IsForbidden(pawn)) //basic desk availabilty
                    {
                        IEnumerable<ResearchProjectDef> homework = pawn.TryGetComp<CompKnowledge>().homework.Where(x => x.IsFinished && x.RequisitesKnownBy(pawn));
                        if (homework.Any()) //check homework
                        {
                            billGiver.BillStack.RemoveIncompletableBills();
                            foreach (Bill bill in RelevantBills(thing, pawn))
                            {
                                if (bill.Allows(homework)) //check bill filter vs. homework
                                {
                                    actualJob = new Job(TechJobDefOf.LearnTech, target) { bill = bill };
                                    lastVerifiedJobTick = tick;
                                    break;
                                }
                                else if (!JobFailReason.HaveReason) JobFailReason.Is("ForbiddenAssignment".Translate());
                            }
                        }
                        else if (!JobFailReason.HaveReason) JobFailReason.Is("NoAssignment".Translate(pawn));
                    }
                }
            }
            return actualJob;
        }
    }
}
