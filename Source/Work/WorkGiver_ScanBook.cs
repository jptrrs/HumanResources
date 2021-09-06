using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace HumanResources
{
    using static ModBaseHumanResources;

    class WorkGiver_ScanBook : WorkGiver_Knowledge
	{
		public new List<ThingCount> chosenIngThings = new List<ThingCount>();
		protected static MethodInfo
            GetBillGiverRootCellInfo = AccessTools.Method(typeof(WorkGiver_DoBill), "GetBillGiverRootCell"),
            BestIngredientsInfo = AccessTools.Method(typeof(WorkGiver_DoBill), "TryFindBestBillIngredients");
		protected static FieldInfo rangeInfo = AccessTools.Field(typeof(WorkGiver_DoBill), "ReCheckFailedBillTicksRange");

        private static Func<Thing, Bill, List<Thing>> ContainsSelected = (thing, bill) =>
        {
            List<Thing> results = new List<Thing>();
            if (thing is Building_BookStore && thing is IThingHolder holder)
            {
                results.AddRange(ThingOwnerUtility.GetAllThingsRecursively(holder, false).Where(t => IsSelected(t, bill)));
            }
            return results;
        };

        private static Func<Thing, Bill, bool> IsSelected = (thing, bill) =>
        {
            if (thing.Stuff != null)
            {
                bool chosen = bill.ingredientFilter.AllowedThingDefs.Contains(thing.Stuff);
                ResearchProjectDef tech = thing.TryGetTech();
                bool relevant = !tech.IsOnline();
                return chosen && relevant;
            }
            return false;
        };

        private static new List<Thing>
            relevantThings = new List<Thing>(),
            newRelevantThings = new List<Thing>();

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            int tick = Find.TickManager.TicksGame;
            if (actualJob == null || lastVerifiedJobTick != tick || Find.TickManager.Paused)
            {
                actualJob = null;
                IBillGiver billGiver = thing as IBillGiver;
                if (billGiver != null && ThingIsUsableBillGiver(thing) && billGiver.BillStack.AnyShouldDoNow && billGiver.UsableForBillsAfterFueling())
                {
                    if (billGiver.Map.ServerAvailable()) //check servers
                    {
                        LocalTargetInfo target = thing;
                        if (pawn.CanReserve(target, 1, -1, null, forced) && !thing.IsBurning() && !thing.IsForbidden(pawn)) //basic desk availabilty
                        {
                            var progress = (Dictionary<ResearchProjectDef, float>)Extension_Research.progressInfo.GetValue(Find.ResearchManager);
                            if (unlocked.TechsArchived.Count < progress.Keys.Where(x => x.IsFinished).EnumerableCount()) //check database
                            {
                                billGiver.BillStack.RemoveIncompletableBills();
                                foreach (Bill bill in RelevantBills(thing, pawn))
                                {
                                    if (ValidateChosenTechs(bill, pawn, billGiver)) //check bill filter
                                    {
                                        actualJob = StartBillJob(pawn, billGiver, bill);
                                        lastVerifiedJobTick = tick;
                                        break;
                                    }
                                }
                            }
                            else if (!JobFailReason.HaveReason) JobFailReason.Is("NoBooksLeftToScan".Translate());
                        }
                    }
                    else if (!JobFailReason.HaveReason) JobFailReason.Is("NoAvailableServer".Translate());
                }
            }
            return actualJob;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
			var progress = (Dictionary<ResearchProjectDef, float>)Extension_Research.progressInfo.GetValue(Find.ResearchManager);
			return unlocked.TechsArchived.Count >= progress.Keys.Where(x => x.IsFinished).EnumerableCount();
		}

		protected Job StartBillJob(Pawn pawn, IBillGiver giver, Bill bill)
		{
			IntRange range = (IntRange)rangeInfo.GetValue(this);
			if (Find.TickManager.TicksGame >= bill.lastIngredientSearchFailTicks + range.RandomInRange || FloatMenuMakerMap.makingFor == pawn)
			{
				bill.lastIngredientSearchFailTicks = 0;
				if (bill.ShouldDoNow() && bill.PawnAllowedToStartAnew(pawn))
				{
					Job result = TryStartNewDoBillJob(pawn, bill, giver);
					chosenIngThings.Clear();
					return result;
				}
			}
			chosenIngThings.Clear();
			return null;
		}

		protected virtual Job TryStartNewDoBillJob(Pawn pawn, Bill bill, IBillGiver giver)
		{
			Job job = WorkGiverUtility.HaulStuffOffBillGiverJob(pawn, giver, null);
			if (job != null)
			{
				return job;
			}
			Job job2 = new Job(TechJobDefOf.ScanBook, (Thing)giver);
			job2.targetQueueB = new List<LocalTargetInfo>(chosenIngThings.Count);
			job2.countQueue = new List<int>(chosenIngThings.Count);
			for (int i = 0; i < chosenIngThings.Count; i++)
			{
				job2.targetQueueB.Add(chosenIngThings[i].Thing);
				job2.countQueue.Add(chosenIngThings[i].Count);
			}
			job2.haulMode = HaulMode.ToCellNonStorage;
			job2.bill = bill;
			return job2;
		}

        private static Thing FindNearest(List<Thing> availableThings, IntVec3 rootCell)
        {
            Comparison<Thing> comparison = delegate (Thing t1, Thing t2)
            {
                float num5 = (t1.Position - rootCell).LengthHorizontalSquared;
                float value = (t2.Position - rootCell).LengthHorizontalSquared;
                return num5.CompareTo(value);
            };
            availableThings.Sort(comparison);
            return availableThings.FirstOrDefault();
        }

        private static new bool TryFindBestBillIngredients(Bill bill, Pawn pawn, Thing billGiver, List<ThingCount> chosen)
        {
            chosen.Clear();
            newRelevantThings.Clear();
            IntVec3 rootCell = (IntVec3)GetBillGiverRootCellInfo.Invoke(bill, new object[] { billGiver, pawn });
            Region rootReg = rootCell.GetRegion(pawn.Map, RegionType.Set_Passable);
            if (rootReg == null)
            {
                return false;
            }
            relevantThings.Clear();
            bool foundAll = false;
            Predicate<Thing> baseValidator = (Thing t) => t.Spawned && !t.IsForbidden(pawn) && (t.Position - billGiver.Position).LengthHorizontalSquared < bill.ingredientSearchRadius * bill.ingredientSearchRadius && ((!t.def.tradeTags.NullOrEmpty() && t.def.tradeTags.Contains(TechStrings.bookTraderTag)) || t is Building_BookStore) && pawn.CanReserve(t, 1, -1, null, false);
            TraverseParms traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false);
            RegionEntryPredicate entryCondition = null;
            if (Math.Abs(999f - bill.ingredientSearchRadius) >= 1f)
            {
                float radiusSq = bill.ingredientSearchRadius * bill.ingredientSearchRadius;
                entryCondition = delegate (Region from, Region r)
                {
                    if (!r.Allows(traverseParams, false))
                    {
                        return false;
                    }
                    CellRect extentsClose = r.extentsClose;
                    int num = Math.Abs(billGiver.Position.x - Math.Max(extentsClose.minX, Math.Min(billGiver.Position.x, extentsClose.maxX)));
                    if (num > bill.ingredientSearchRadius)
                    {
                        return false;
                    }
                    int num2 = Math.Abs(billGiver.Position.z - Math.Max(extentsClose.minZ, Math.Min(billGiver.Position.z, extentsClose.maxZ)));
                    return num2 <= bill.ingredientSearchRadius && (num * num + num2 * num2) <= radiusSq;
                };
            }
            else
            {
                entryCondition = ((Region from, Region r) => r.Allows(traverseParams, false));
            }
            int adjacentRegionsAvailable = rootReg.Neighbors.Count((Region region) => entryCondition(rootReg, region));
            int regionsProcessed = 0;
            RegionProcessor regionProcessor = delegate (Region r)
            {
                List<Thing> items = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
                for (int i = 0; i < items.Count; i++)
                {
                    Thing thing = items[i];
                    if (ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, PathEndMode.ClosestTouch, pawn) && baseValidator(thing) && IsSelected(thing, bill))
                    {
                        newRelevantThings.Add(thing);
                    }
                  }
                List<Thing> buildings = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial));
                for (int i = 0; i < buildings.Count; i++)
                {
                    Thing thing = buildings[i];
                    if (ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, PathEndMode.ClosestTouch, pawn) && baseValidator(thing))
                    {
                        List<Thing> innerList = ContainsSelected(thing, bill);
                        if (!innerList.NullOrEmpty())
                        {
                            newRelevantThings.AddRange(innerList);
                        }
                    }
                }
                regionsProcessed++;
                if (newRelevantThings.Count > 0 && regionsProcessed > adjacentRegionsAvailable)
                {
                    relevantThings.AddRange(newRelevantThings);
                    newRelevantThings.Clear();
                    ThingCountUtility.AddToList(chosen, FindNearest(relevantThings, rootCell), 1);
                    foundAll = true;
                    return true;
                }
                return false;
            };
            RegionTraverser.BreadthFirstTraverse(rootReg, entryCondition, regionProcessor, 99999, RegionType.Set_Passable);
            relevantThings.Clear();
            newRelevantThings.Clear();
            return foundAll;
        }

        private bool ValidateChosenTechs(Bill bill, Pawn pawn, IBillGiver giver)
        {
            if (TryFindBestBillIngredients(bill, pawn, (Thing)giver, chosenIngThings))
            {
                return chosenIngThings.Any();
            }
			if (!JobFailReason.HaveReason) JobFailReason.Is("NoBooksToScan".Translate(pawn), null);
			if (FloatMenuMakerMap.makingFor != pawn) bill.lastIngredientSearchFailTicks = Find.TickManager.TicksGame;
			return false;
		}
	}
}
