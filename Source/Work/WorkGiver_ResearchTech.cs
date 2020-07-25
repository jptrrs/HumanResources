using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace HumanResources
{
    class WorkGiver_ResearchTech : WorkGiver_Knowledge
    {
		public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
			if (!base.ShouldSkip(pawn, forced))
			{
				IEnumerable<ResearchProjectDef> available = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(x => !x.IsFinished && !x.IsKnownBy(pawn) && x.RequisitesKnownBy(pawn));
				return !available.Any();
			}
			return true;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Log.Message(pawn + " is looking for a research job...");
			Building_ResearchBench Desk = t as Building_ResearchBench;
			if (Desk != null && pawn.CanReserve(t, 1, -1, null, forced))
			{
				CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
				return techComp.homework.Where(x => !x.IsFinished && x.CanBeResearchedAt(Desk, false) && x.RequisitesKnownBy(pawn)).Any();
			}
			return false;

		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return JobMaker.MakeJob(TechJobDefOf.ResearchTech, t);
		}

		//public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
		//{
		//	IBillGiver billGiver = thing as IBillGiver;
		//	if (billGiver != null && ThingIsUsableBillGiver(thing) && billGiver.BillStack.AnyShouldDoNow && billGiver.UsableForBillsAfterFueling())
		//	{
		//		LocalTargetInfo target = thing;
		//		if (pawn.CanReserve(target, 1, -1, null, forced) && !thing.IsBurning() && !thing.IsForbidden(pawn))
		//		{
		//			billGiver.BillStack.RemoveIncompletableBills();
		//			foreach (Bill bill in RelevantBills(thing, pawn))
		//			{
		//				if (bill.ShouldDoNow() && bill.PawnAllowedToStartAnew(pawn)/* && bill.SelectedTech().Intersect(availableTechs).Any()*/)
		//				{
		//					//Log.Message("probing bill: pawn allowed is " + bill.PawnAllowedToStartAnew(pawn) + " for " + pawn);
		//					return new Job(TechJobDefOf.ResearchTech, target)
		//					{
		//						bill = bill
		//					};
		//				}
		//			}
		//		}
		//	}
		//	return null;
		//}
	}
}
