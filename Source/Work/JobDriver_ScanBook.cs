using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;
using Verse.AI;
using System.Reflection;
using HarmonyLib;
using System;

namespace HumanResources
{
	public class JobDriver_ScanBook : JobDriver_Knowledge
	{
        protected Building_NetworkServer server;
        private bool inShelf = false;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (job.targetQueueB != null && job.targetQueueB.Count == 1 && job.targetQueueB[0].Thing != null)
            {
                Thing book = job.targetQueueB[0].Thing;
                inShelf = ThingOwnerUtility.AnyParentIs<Building_BookStore>(book);
                ThingDef techStuff = book.Stuff;
                if (techStuff != null)
                {
                    project = ModBaseHumanResources.unlocked.techByStuff[techStuff];
                }
            }
            Log.Warning("starting Scanning Job: target A is " + TargetA.Thing + ", project is " + project + ", bill is " + job.bill.Label);
            return base.TryMakePreToilReservations(errorOnFailed);
		}

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
					if (job.bill.DeletedOrDereferenced) return true;
					if (!billGiver.CurrentlyUsableForBills()) return true;
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
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDestroyedOrNull(TargetIndex.B);
            Toil findPlaceTarget = Toils_JobTransforms.SetTargetToIngredientPlaceCell(TargetIndex.A, TargetIndex.B, TargetIndex.C);
			yield return findPlaceTarget;
			yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget, false, false);
			yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.B, extract);
			extract = null;
			getToHaulTarget = null;
			findPlaceTarget = null;
			yield return gotoBillGiver;
            yield return Toils_Recipe.DoRecipeWork().FailOnDespawnedNullOrForbiddenPlacedThings().FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            Toil upload = new Toil();
            upload.initAction = delegate ()
            {
                Pawn actor = upload.actor;
                Job curJob = actor.jobs.curJob;
                JobDriver_DoBill jobDriver_DoBill = (JobDriver_DoBill)actor.jobs.curDriver;
                if (curJob.RecipeDef.workSkill != null)
                {
                    float xp = (float)jobDriver_DoBill.ticksSpentDoingRecipeWork * 0.1f * curJob.RecipeDef.workSkillLearnFactor;
                    actor.skills.GetSkill(curJob.RecipeDef.workSkill).Learn(xp, false);
                }
                project.CompleteUpload(TargetThingA);
                curJob.bill.Notify_IterationCompleted(actor, new List<Thing>());
                actor.jobs.EndCurrentJob(JobCondition.Succeeded, true, true);
            };
            yield return upload;

            //yield return FinishRecipeAndStartStoringProduct();
            //if (!job.RecipeDef.products.NullOrEmpty<ThingDefCountClass>() || !job.RecipeDef.specialProducts.NullOrEmpty<SpecialProductType>())
            //{
            //	yield return Toils_Reserve.Reserve(TargetIndex.B);
            //	yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, true, false);
            //	findPlaceTarget = Toils_Haul.CarryHauledThingToContainer();
            //	yield return findPlaceTarget; 
            //	Toil prepare = Toils_General.Wait(250);
            //	prepare.WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
            //	yield return prepare;
            //	yield return new Toil
            //	{
            //		initAction = delegate
            //		{
            //			Building_BookStore shelf = (Building_BookStore)job.GetTarget(TargetIndex.B).Thing;
            //			CurToil.FailOn(() => shelf == null);
            //			Thing book = pawn.carryTracker.CarriedThing;
            //			if (pawn.carryTracker.CarriedThing == null)
            //			{
            //				Log.Error(pawn + " tried to place a book on shelf but is not hauling anything.");
            //				return;
            //			}
            //			if (shelf.Accepts(book))
            //			{
            //				bool flag = false;
            //				if (book.holdingOwner != null)
            //				{
            //					book.holdingOwner.TryTransferToContainer(book, shelf.TryGetInnerInteractableThingOwner(), book.stackCount, true);
            //					flag = true;
            //				}
            //				else
            //				{
            //					flag = shelf.TryGetInnerInteractableThingOwner().TryAdd(book, true);
            //				}
            //				pawn.carryTracker.innerContainer.Remove(book);
            //			}
            //			else
            //			{
            //				Log.Error(pawn + " tried to place a book in " + shelf + ", but won't accept it.");
            //				return;
            //			}
            //			pawn.jobs.EndCurrentJob(JobCondition.Succeeded, true, true);
            //		}
            //	};
            //}
            yield break;
        }
	}
}
