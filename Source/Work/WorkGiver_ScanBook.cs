using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace HumanResources
{
    class WorkGiver_ScanBook : WorkGiver_Knowledge
	{
		public List<ThingCount> chosenIngThings = new List<ThingCount>();
		protected MethodInfo BestIngredientsInfo = AccessTools.Method(typeof(WorkGiver_DoBill), "TryFindBestBillIngredients");
		protected FieldInfo rangeInfo = AccessTools.Field(typeof(WorkGiver_DoBill), "ReCheckFailedBillTicksRange");

		public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
			var progress = (Dictionary<ResearchProjectDef, float>)Extension_Research.progressInfo.GetValue(Find.ResearchManager);
			return ModBaseHumanResources.unlocked.networkDatabase.Count >= progress.Keys.Where(x => x.IsFinished).EnumerableCount();
		}

		public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			IBillGiver billGiver = thing as IBillGiver;
			if (billGiver != null && ThingIsUsableBillGiver(thing) && billGiver.BillStack.AnyShouldDoNow && billGiver.UsableForBillsAfterFueling())
			{
				if (billGiver.Map.ServerAvailable())
				{
					LocalTargetInfo target = thing;
					if (pawn.CanReserve(target, 1, -1, null, forced) && !thing.IsBurning() && !thing.IsForbidden(pawn))
					{
						var progress = (Dictionary<ResearchProjectDef, float>)Extension_Research.progressInfo.GetValue(Find.ResearchManager);
						if (ModBaseHumanResources.unlocked.networkDatabase.Count < progress.Keys.Where(x => x.IsFinished).EnumerableCount())
						{
							billGiver.BillStack.RemoveIncompletableBills();
							foreach (Bill bill in RelevantBills(thing, pawn))
							{
								if (ValidateChosenTechs(bill, pawn, billGiver))
								{
									return StartBillJob(pawn, billGiver, bill);
								}
							}
						}
						else if (!JobFailReason.HaveReason) JobFailReason.Is("NoBooksLeftToScan".Translate());
					}
				}
				else if (!JobFailReason.HaveReason) JobFailReason.Is("NoAvailableServer".Translate());
			}
			return null;
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

		private bool ValidateChosenTechs(Bill bill, Pawn pawn, IBillGiver giver)
		{
			if ((bool)BestIngredientsInfo.Invoke(this, new object[] { bill, pawn, giver, chosenIngThings }))
			{
				//Log.Warning("DEBUG chosenInThings: " + chosenIngThings.ToStringSafeEnumerable());
                chosenIngThings.RemoveAll(x => x.Thing.Stuff == null || ModBaseHumanResources.unlocked.networkDatabase.Contains(ModBaseHumanResources.unlocked.techByStuff[x.Thing.Stuff]));
				if (!JobFailReason.HaveReason) JobFailReason.Is("NoBooksLeftToScan".Translate(pawn), null);
				return chosenIngThings.Any();
			}
			if (!JobFailReason.HaveReason) JobFailReason.Is("NoBooksFoundToScan".Translate(pawn), null);
			if (FloatMenuMakerMap.makingFor != pawn) bill.lastIngredientSearchFailTicks = Find.TickManager.TicksGame;
			return false;
		}

	}
}
