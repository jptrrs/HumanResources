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
			IEnumerable<ResearchProjectDef> advantage = pawn.TryGetComp<CompKnowledge>().expertise.Keys.Where(x => !x.IsFinished);
			bool flag = advantage.ToList().Count > 0;
			return !flag;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			//Log.Message(pawn + " is looking for a document job...");
			Building_WorkTable Desk = t as Building_WorkTable;
			if (Desk != null)
			{
				if (!CheckJobOnThing(pawn, t, forced)/* && RelevantBills(t).Any()*/)
				{
					//Log.Message("...no job on desk.");
					return false;
				}
				IEnumerable<ResearchProjectDef> advantage = pawn.TryGetComp<CompKnowledge>().expertise.Where(x => !x.Key.IsFinished && x.Value >= 1f).Select(x => x.Key);
				//Log.Message("... advantage is " + advantage.ToStringSafeEnumerable());
				foreach (Bill bill in RelevantBills(Desk, pawn))
				{
					if (advantage.Intersect(bill.SelectedTech()).Any()) return true;
				}
				JobFailReason.Is("NothingToAddToLibrary".Translate(pawn), null);
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
					foreach (Bill bill in RelevantBills(thing, pawn))
					{
						return StartOrResumeBillJob(pawn, billGiver, target);
					}
				}
			}
			return null;
		}

		private Job StartOrResumeBillJob(Pawn pawn, IBillGiver giver, LocalTargetInfo target)
		{
			for (int i = 0; i < giver.BillStack.Count; i++)
			{
				Bill bill = giver.BillStack[i];
				if (bill.ShouldDoNow() && bill.PawnAllowedToStartAnew(pawn))
				{
					SkillRequirement skillRequirement = bill.recipe.FirstSkillRequirementPawnDoesntSatisfy(pawn);
					if (skillRequirement != null)
					{
						JobFailReason.Is("UnderRequiredSkill".Translate(skillRequirement.minLevel), bill.Label);
					}
					else
					{
						Bill_ProductionWithUft bill_ProductionWithUft = bill as Bill_ProductionWithUft;
						if (bill_ProductionWithUft != null)
						{
							if (bill_ProductionWithUft.BoundUft != null)
							{

								if (bill_ProductionWithUft.BoundWorker == pawn && pawn.CanReserveAndReach(bill_ProductionWithUft.BoundUft, PathEndMode.Touch, Danger.Deadly, 1, -1, null, false) && !bill_ProductionWithUft.BoundUft.IsForbidden(pawn))
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
						Job result = new Job(DefDatabase<JobDef>.GetNamed(bill.recipe.defName), target)
						{
							bill = bill
						};
						return result;
					}
				}
				
			}
			return null;
		}

		private static Job FinishUftJob(Pawn pawn, UnfinishedThing uft, Bill_ProductionWithUft bill)
		{
			if (uft.Creator != pawn)
			{
				Log.Error(string.Concat(new object[]
				{
					"Tried to get FinishUftJob for ",
					pawn,
					" finishing ",
					uft,
					" but its creator is ",
					uft.Creator
				}), false);
				return null;
			}
			Job job = WorkGiverUtility.HaulStuffOffBillGiverJob(pawn, bill.billStack.billGiver, uft);
			if (job != null && job.targetA.Thing != uft)
			{
				return job;
			}
			Job job2 = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(bill.recipe.defName), (Thing)bill.billStack.billGiver);
			job2.bill = bill;
			job2.targetQueueB = new List<LocalTargetInfo> { uft };
			job2.countQueue = new List<int> { 1 };
			job2.haulMode = HaulMode.ToCellNonStorage;
			return job2;
		}
	}
}
