using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using System.Linq;

namespace HumanResources
{
    public class WorkGiver_FileTech : WorkGiver_Scanner
    {
        //Borrowed from Jecrell's RimWriter
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerThings.AllThings.FindAll(x => x.def.defName == "TechBook");
        }

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.ClosestTouch;
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !pawn.Map.listerThings.AllThings.Any(x => x.def.defName == "TechBook");
        }

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!HaulAIUtility.PawnCanAutomaticallyHaul(pawn, t, forced))
            {
                return null;
            }
            Building_BookStore target = FindBestStorage(pawn, t);
            if (target == null)
            {
                JobFailReason.Is("RimWriter_NoInternalStorage".Translate());
                return null;
            }
            return new Job(DefDatabase<JobDef>.GetNamed("FileTech"), t, target)
            {
                count = t.stackCount
            };
        }

        private Building_BookStore FindBestStorage(Pawn p, Thing book)
        {
            Predicate<Thing> predicate = (Thing m) => !m.IsForbidden(p) && p.CanReserveNew(m) && ((Building_BookStore)m).Accepts(book);
            Func<Thing, float> priorityGetter = delegate (Thing t)
            {
                float result = 0f;
                result += (float)((IStoreSettingsParent)t).GetStoreSettings().Priority;
                if (t is Building_BookStore bS && bS.TryGetInnerInteractableThingOwner()?.Count > 0)
                    result -= bS.TryGetInnerInteractableThingOwner().Count;
                return result;
            };
            IntVec3 position = book.Position;
            Map map = book.Map;
            List<Thing> searchSet = book.Map.listerThings.AllThings.FindAll(x => x is Building_BookStore);
            PathEndMode peMode = PathEndMode.ClosestTouch;
            TraverseParms traverseParams = TraverseParms.For(p, Danger.Deadly, TraverseMode.ByPawn, false);
            Predicate<Thing> validator = predicate;
            return (Building_BookStore)GenClosest.ClosestThing_Global_Reachable(position, map, searchSet, peMode, traverseParams, 9999f, validator, priorityGetter);
        }
    }
}
