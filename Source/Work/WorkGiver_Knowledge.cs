using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace HumanResources
{
	class WorkGiver_Knowledge : WorkGiver_DoBill
	{
		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			return pawn.TryGetComp<CompKnowledge>()?.expertise == null;
		}

		protected bool CheckJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			IBillGiver billGiver = t as IBillGiver;
			if (billGiver != null && ThingIsUsableBillGiver(t) && billGiver.CurrentlyUsableForBills() && billGiver.BillStack.AnyShouldDoNow && pawn.CanReserve(t, 1, -1, null, forced) && !t.IsBurning() && !t.IsForbidden(pawn))
			{
				if (!pawn.CanReach(t, PathEndMode.OnCell, Danger.Some, false, TraverseMode.ByPawn))
				{
					return false;
				}
				billGiver.BillStack.RemoveIncompletableBills();
				return true;
			}
			return false;
		}

		protected IEnumerable<Bill_Production> RelevantBills(Thing thing, Pawn pawn)
		{
			Building_WorkTable desk = thing as Building_WorkTable;	
			if (desk != null)
			{
				return desk.BillStack.Bills.Cast<Bill_Production>().Where(x => x.recipe.defName.StartsWith(def.defName) && x.ShouldDoNow()/*x.repeatCount > 0*/ && x.PawnAllowedToStartAnew(pawn));
			}
			return null;
		}

		protected IEnumerable<ResearchProjectDef> availableTechs;
	}
}
