using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace HumanResources
{
    class WorkGiver_DocumentTech : WorkGiver_Knowledge
	{
		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			if (!base.ShouldSkip(pawn, forced))
			{
				IEnumerable<ResearchProjectDef> advantage = pawn.TryGetComp<CompKnowledge>().knownTechs.Where(x => !x.IsFinished);
				return !advantage.Any();
			}
			return true;
		}

        private bool CheckLibrarySpace(Thing Desk)
        {
			if (Desk.def == TechDefOf.NetworkTerminal)
			{
				return Desk.Map.listerBuildings.ColonistsHaveBuildingWithPowerOn(TechDefOf.NetworkServer);
			}
			else return ModBaseHumanResources.unlocked.libraryFreeSpace <= 0;
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			IBillGiver billGiver = thing as IBillGiver;
			if (billGiver != null && ThingIsUsableBillGiver(thing) && billGiver.BillStack.AnyShouldDoNow && billGiver.UsableForBillsAfterFueling())
			{
				if (CheckLibrarySpace(thing))
				{
					CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
					if (techComp.knownTechs.Where(x => !x.IsFinished).Intersect(techComp.homework).Any())
					{
						LocalTargetInfo target = thing;
						if (pawn.CanReserve(target, 1, -1, null, forced) && !thing.IsBurning() && !thing.IsForbidden(pawn))
						{
							billGiver.BillStack.RemoveIncompletableBills();
							foreach (Bill bill in RelevantBills(thing, pawn))
							{
								return StartOrResumeBillJob(pawn, billGiver, target);
							}
						}
					}
				}
				else if (!JobFailReason.HaveReason) JobFailReason.Is("NoSpaceInLibrary".Translate());
			}
			return null;
		}

		private Job StartOrResumeBillJob(Pawn pawn, IBillGiver giver, LocalTargetInfo target)
		{
			//Log.Message("...StartOrResumeBillJob for "+pawn);
			for (int i = 0; i < giver.BillStack.Count; i++)
			{
				Bill bill = giver.BillStack[i];
				if ((bill.recipe == TechDefOf.DocumentTech || bill.recipe == TechDefOf.DocumentTechDigital) && bill.ShouldDoNow() && bill.PawnAllowedToStartAnew(pawn))
				{
					SkillRequirement skillRequirement = bill.recipe.FirstSkillRequirementPawnDoesntSatisfy(pawn);
					if (skillRequirement != null)
					{
						JobFailReason.Is("UnderRequiredSkill".Translate(skillRequirement.minLevel), bill.Label);
					}
					else
					{
						if (bill.recipe.UsesUnfinishedThing)
						{
							Bill_ProductionWithUft bill_ProductionWithUft = bill as Bill_ProductionWithUft;
							if (bill_ProductionWithUft != null)
							{
								if (bill_ProductionWithUft.BoundUft != null)
								{
									bool BoundWorker = bill_ProductionWithUft.BoundWorker == pawn;
									bool canReach = pawn.CanReserveAndReach(bill_ProductionWithUft.BoundUft, PathEndMode.Touch, Danger.Deadly, 1, -1, null, false);
									bool isforbidden = bill_ProductionWithUft.BoundUft.IsForbidden(pawn);
									if (BoundWorker && canReach && isforbidden)
									{
										return FinishUftJob(pawn, bill_ProductionWithUft.BoundUft, bill_ProductionWithUft);
									}
									return null;
								}
								else
								{
									MethodInfo ClosestUftInfo = AccessTools.Method(typeof(WorkGiver_DoBill), "ClosestUnfinishedThingForBill");
									UnfinishedThing unfinishedThing = (UnfinishedThing)ClosestUftInfo.Invoke(this, new object[] { pawn, bill_ProductionWithUft });
									if (unfinishedThing != null)
									{
										return FinishUftJob(pawn, unfinishedThing, bill_ProductionWithUft);
									}
								}
							}
							return new Job(TechJobDefOf.DocumentTech, target)
							{
								bill = bill
							};
						}
						else
                        {
							return new Job(TechJobDefOf.DocumentTechDigital, target)
							{
								bill = bill
							};
						}
					}
				}

			}
			return null;
		}

		public static Job FinishUftJob(Pawn pawn, UnfinishedThing uft, Bill_ProductionWithUft bill)
		{
			if (pawn.TryGetComp<CompKnowledge>().expertise.Where(x => !x.Key.IsFinished && x.Value >= 1f && x.Key.LabelCap == uft.Stuff.stuffProps.stuffAdjective).Any() == false)
			{
				return null;
			}
			Job job = WorkGiverUtility.HaulStuffOffBillGiverJob(pawn, bill.billStack.billGiver, uft);
			if (job != null && job.targetA.Thing != uft)
			{
				return job;
			}
			Job job2 = JobMaker.MakeJob(TechJobDefOf.DocumentTech, (Thing)bill.billStack.billGiver);
			job2.bill = bill;
			job2.targetQueueB = new List<LocalTargetInfo> { uft };
			job2.countQueue = new List<int> { 1 };
			job2.haulMode = HaulMode.ToCellNonStorage;
			return job2;
		}
	}
}
