using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace HumanResources
{
	class WorkGiver_Knowledge : WorkGiver_DoBill
	{

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

		protected List<Bill_Production> RelevantBills(Thing thing, string recipeName)
		{
			Building_WorkTable desk = thing as Building_WorkTable;
			if (desk != null)
			{
				return desk.BillStack.Bills.Cast<Bill_Production>().Where(x => x.recipe.defName.Contains(recipeName) && x.repeatCount > 0).ToList();
			}
			return null;
		}

		protected string RecipeName;

	}
}
