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
        private bool
            inShelf = false,
            bookOut = false;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Thing book = null;
            if (job.targetQueueB != null && job.targetQueueB.Count == 1 && job.targetQueueB[0].Thing != null)
            {
                book = job.targetQueueB[0].Thing;
                inShelf = ThingOwnerUtility.AnyParentIs<Building_BookStore>(book);
                if (inShelf) //insert bookstore in queue
                {
                    job.targetQueueB.Clear();
                    Thing shelf = ThingOwnerUtility.GetFirstSpawnedParentThing(book);
                    job.AddQueuedTarget(TargetIndex.B, shelf);
                    job.AddQueuedTarget(TargetIndex.B, book);
                    //job.AddQueuedTarget(TargetIndex.A, shelf);
                }
                ThingDef techStuff = book.Stuff;
                if (techStuff != null)
                {
                    project = ModBaseHumanResources.unlocked.techByStuff[techStuff];
                }
            }
            Log.Warning("starting Scanning Job: target A is " + TargetA.Thing + ", book is "+ book + ", targetQueueA count = " + job.targetQueueA.Count() + ", project is " + project + ", bill is " + job.bill.Label);
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
            AddFinishAction(delegate
            {
                if (inShelf && bookOut && !ModBaseHumanResources.unlocked.networkDatabase.Contains(project))
                {
                    project.EjectTech(job.GetTarget(TargetIndex.A).Thing);
                }
            });
            Toil gotoBillGiver = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_Jump.JumpIf(gotoBillGiver, () => job.GetTargetQueue(TargetIndex.B).NullOrEmpty<LocalTargetInfo>());
            Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B, true);
            yield return extract;
            Toil getToHaulTarget = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return getToHaulTarget;
            yield return inShelf ? TakeFromShelf(TargetIndex.B) : Toils_Haul.StartCarryThing(TargetIndex.B, true, false, true);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDestroyedOrNull(TargetIndex.B);
            Toil findPlaceTarget = Toils_JobTransforms.SetTargetToIngredientPlaceCell(TargetIndex.A, TargetIndex.B, TargetIndex.C);
            yield return findPlaceTarget;
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget, false, false);
            //yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.B, extract);
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
                if (!inShelf) actor.jobs.EndCurrentJob(JobCondition.Succeeded, true, true);
            };
            yield return upload;

            //Put it back!
            if (inShelf)
            {
                yield return Toils_Haul.StartCarryThing(TargetIndex.B, true, false, true);
                yield return extract;
                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDestroyedOrNull(TargetIndex.B);
                yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A, false);
                yield return Toils_Haul.DepositHauledThingInContainer(TargetIndex.B, TargetIndex.A);
            }
            //else
            //{
            //    yield return Toils_Haul.CarryHauledThingToContainer();
            //}
            yield return new Toil()
            {
                initAction = delegate ()
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Succeeded, true, true);
                }
            };


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

		public Toil TakeFromShelf(TargetIndex index)
		{
			Toil toil = new Toil();
			toil.initAction = delegate ()
			{
				Pawn actor = toil.actor;
				Job curJob = actor.jobs.curJob;
                List<LocalTargetInfo> targetQueue = curJob.GetTargetQueue(index);
                if (curJob.GetTarget(index).Thing is Building_BookStore shelf)
                {
                    Thing book = curJob.targetQueueB[0].Thing;
                    int availableSpace = actor.carryTracker.AvailableStackSpace(book.def);
                    if (availableSpace == 0)
                    {
                        throw new Exception(string.Concat(new object[]
                        {"StartCarryThing got availableStackSpace ", availableSpace, " for haulTarg ", book, ". Job: ", curJob }));
                    }
                    shelf.innerContainer.TryTransferToContainer(book, actor.carryTracker.innerContainer, false);
                    curJob.SetTarget(index, actor.carryTracker.CarriedThing);
                    targetQueue.RemoveAt(0);
                    targetQueue.Add(shelf);
                    if (!shelf.innerContainer.Contains(book)) bookOut = true;
                    actor.records.Increment(RecordDefOf.ThingsHauled);
                }
                else
                {
                    throw new Exception("Tried taking book from shelf, but shelf isn't on queue for the job.");
                }
			};
			return toil;
		}
	}
}
