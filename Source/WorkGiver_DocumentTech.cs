using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace HumanResources
{
    class WorkGiver_DocumentTech : WorkGiver_Knowledge
	{
		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			IEnumerable<ResearchProjectDef> advantage = pawn.GetComp<CompKnowledge>().expertise.Where(x => !x.IsFinished);
			bool flag = advantage.ToList().Count > 0;
			return !flag;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Log.Message(pawn + " is looking for a document job...");
			Building_WorkTable Desk = t as Building_WorkTable;
			if (Desk != null)
			{
				if (!CheckJobOnThing(pawn, t, forced) && RelevantBills(t, RecipeName).Count() > 0)
				{
					//Log.Message("...no job on desk.");
					return false;
				}
				IEnumerable<ResearchProjectDef> advantage = pawn.GetComp<CompKnowledge>().expertise.Where(x => !x.IsFinished);
				Log.Message("... advantage is " + advantage.ToStringSafeEnumerable());
				foreach (Bill bill in RelevantBills(Desk, RecipeName))
				{
					if (advantage.Intersect(bill.SelectedTech()).Count() > 0) return true;
				}
				JobFailReason.Is("NothingToAddToLibrary".Translate(pawn), null);
				//Log.Message("case 3: " + RelevantBills(Desk));
				return false;
			}
			//Log.Message("case 4");
			return false;
		}

		public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			IBillGiver billGiver = thing as IBillGiver;
			if (billGiver != null && ThingIsUsableBillGiver(thing) && billGiver.BillStack.AnyShouldDoNow && billGiver.UsableForBillsAfterFueling())
			{
				LocalTargetInfo target = thing;
				if (pawn.CanReserve(target, 1, -1, null, forced) && !thing.IsBurning() && !thing.IsForbidden(pawn))
				{
					billGiver.BillStack.RemoveIncompletableBills();
					foreach (Bill bill in RelevantBills(thing, RecipeName))
					{
						return new Job(DefDatabase<JobDef>.GetNamed(RecipeName + "Tech"), target)
						{
							bill = bill
						};
					}
				}
			}
			return null;
		}

		protected new string RecipeName = "Document";
	}
}
