using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace HumanResources
{
    //Changed in RW 1.3
    class WorkGiver_Knowledge : WorkGiver_DoBill
    {
        protected Job actualJob = null;
        protected int lastVerifiedJobTick = 0;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (!pawn.IsGuest())
            {
                CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
                if (techComp != null)
                {
                    return techComp.expertise == null || techComp.homework.NullOrEmpty();
                }
            }
            return true;
        }

        protected bool CheckJobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (thing is IBillGiver billGiver && ThingIsUsableBillGiver(thing) && billGiver.CurrentlyUsableForBills() && billGiver.BillStack.AnyShouldDoNow && pawn.CanReserve(thing, 1, -1, null, forced) && !thing.IsBurning() && !thing.IsForbidden(pawn))
            {
                if (!pawn.CanReach(thing, PathEndMode.OnCell, Danger.Some, false, TraverseMode.ByPawn)) return false;
                billGiver.BillStack.RemoveIncompletableBills();
                return true;
            }
            return false;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_WorkTable desk = t as Building_WorkTable;
            if (desk != null)
            {
                var relevantBills = RelevantBills(desk, pawn);
                if (!CheckJobOnThing(pawn, t, forced) || relevantBills.EnumerableNullOrEmpty()) return false;
                return JobOnThing(pawn, t, forced) != null;
            }
            return false;
        }

        protected IEnumerable<Bill_Production> RelevantBills(Thing thing, Pawn pawn)
        {
            Building_WorkTable desk = thing as Building_WorkTable;
            if (desk != null)
            {
                return desk.BillStack.Bills.Cast<Bill_Production>().Where(x => x.recipe.defName.StartsWith(def.defName) && x.ShouldDoNow() && x.PawnAllowedToStartAnew(pawn));
            }
            return null;
        }
    }
}
