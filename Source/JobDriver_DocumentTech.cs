using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse.AI;

namespace HumanResources
{
	public class JobDriver_DocumentTech : JobDriver_Knowledge
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Log.Warning("starting Document Job: target A is " + TargetA.Thing + ", target B is" + TargetB);
			
			project = job.bill.SelectedTech().Intersect(techComp.expertise).RandomElement();
			techStuff = ModBaseHumanResources.unlocked.stuffByTech.TryGetValue(project);
			//UpdateCost(project.baseCost);
			//job.bill.recipe.workAmount = VariableCost(project.baseCost);
			return base.TryMakePreToilReservations(errorOnFailed);
		}

		protected ThingDef techStuff;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			AddEndCondition(delegate
			{
				Thing thing = GetActor().jobs.curJob.GetTarget(TargetIndex.A).Thing;
				if (thing is Building && !thing.Spawned)
				{
					return JobCondition.Incompletable;
				}
				return JobCondition.Ongoing;
			});
			this.FailOnBurningImmobile(TargetIndex.A);
			this.FailOn(delegate ()
			{
				IBillGiver billGiver = job.GetTarget(TargetIndex.A).Thing as IBillGiver;
				if (billGiver != null)
				{
					if (job.bill.DeletedOrDereferenced)
					{
						return true;
					}
					if (!billGiver.CurrentlyUsableForBills())
					{
						return true;
					}
				}
				return false;
			});
			Toil gotoBillGiver = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
			yield return new Toil
			{
				initAction = delegate ()
				{
					if (job.targetQueueB != null && job.targetQueueB.Count == 1)
					{
						UnfinishedThing unfinishedThing = job.targetQueueB[0].Thing as UnfinishedThing;
						if (unfinishedThing != null)
						{
							unfinishedThing.BoundBill = (Bill_ProductionWithUft)job.bill;
						}
					}
				}
			};
			yield return Toils_Jump.JumpIf(gotoBillGiver, () => job.GetTargetQueue(TargetIndex.B).NullOrEmpty<LocalTargetInfo>());
			Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B, true);
			yield return extract;
			Toil getToHaulTarget = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
			yield return getToHaulTarget;
			yield return Toils_Haul.StartCarryThing(TargetIndex.B, true, false, true);
			//yield return JobDriver_DoBill.JumpToCollectNextIntoHandsForBill(getToHaulTarget, TargetIndex.B);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDestroyedOrNull(TargetIndex.B);
			Toil findPlaceTarget = Toils_JobTransforms.SetTargetToIngredientPlaceCell(TargetIndex.A, TargetIndex.B, TargetIndex.C);
			yield return findPlaceTarget;
			yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget, false, false);
			yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.B, extract);
			extract = null;
			getToHaulTarget = null;
			findPlaceTarget = null;
			yield return gotoBillGiver;
			yield return MakeUnfinishedThingIfNeeded();
			yield return Toils_Recipe.DoRecipeWork().FailOnDespawnedNullOrForbiddenPlacedThings().FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
			yield return Toils_Recipe.FinishRecipeAndStartStoringProduct();
			if (!job.RecipeDef.products.NullOrEmpty<ThingDefCountClass>() || !job.RecipeDef.specialProducts.NullOrEmpty<SpecialProductType>())
			{
				yield return Toils_Reserve.Reserve(TargetIndex.B, 1, -1, null);
				findPlaceTarget = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
				yield return findPlaceTarget;
				yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, findPlaceTarget, true, true);
			}
			yield break;
		}

		private Toil MakeUnfinishedThingIfNeeded()
		{
			Log.Message("Making unfinished thing if needed");
			Toil toil = new Toil();
			toil.initAction = delegate ()
			{
				Pawn actor = toil.actor;
				Job curJob = actor.jobs.curJob;
				if (!curJob.RecipeDef.UsesUnfinishedThing)
				{
					return;
				}
				if (curJob.GetTarget(TargetIndex.B).Thing != null && curJob.GetTarget(TargetIndex.B).Thing is UnfinishedThing)
				{
					return;
				}
				UnfinishedThing unfinishedThing = (UnfinishedThing)ThingMaker.MakeThing(curJob.RecipeDef.unfinishedThingDef, techStuff);
				unfinishedThing.Creator = actor;
				unfinishedThing.BoundBill = (Bill_ProductionWithUft)curJob.bill;
				unfinishedThing.ingredients = new List<Thing>
				{
					new Thing()
					{
						def = unfinishedThing.Stuff
					}
				};

				GenSpawn.Spawn(unfinishedThing, curJob.GetTarget(TargetIndex.A).Cell, actor.Map, WipeMode.Vanish);
				curJob.SetTarget(TargetIndex.B, unfinishedThing);
				actor.Reserve(unfinishedThing, curJob, 1, -1, null, true);
			};
			return toil;
		}
	}
}
