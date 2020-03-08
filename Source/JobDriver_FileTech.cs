using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace HumanResources
{
    public class JobDriver_FileTech: JobDriver
    {
        //Borrowed from Jecrell's RimWriter
        public JobDriver_FileTech()
        {
            rotateToFace = TargetIndex.B;
        }

        private Thing Book
        {
            get
            {
                return (Thing)job.GetTarget(TargetIndex.A).Thing;
            }
        }

        private ResearchProjectDef Project
        {
            get
            {
                string name = Book.Stuff.defName;
                return DefDatabase<ResearchProjectDef>.GetNamed(name.Substring(name.IndexOf(@"_") + 1));
            }
        }

        private Building_BookStore Storage
        {
            get
            {
                return (Building_BookStore)job.GetTarget(TargetIndex.B).Thing;
            }
        }

        public override bool TryMakePreToilReservations(bool b)
        {
            return pawn.Reserve(Book, job, 1, -1, null) && pawn.Reserve(Book, job, 1, -1, null);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
            this.FailOn(() => !Storage.Accepts(Book));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false);
            yield return Toils_Haul.CarryHauledThingToContainer();
            Toil prepare = Toils_General.Wait(250);
            prepare.WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
            yield return prepare;
            yield return new Toil
            {
                initAction = delegate
                {
                    if (pawn.carryTracker.CarriedThing == null)
					{
                        Log.Error(pawn + " tried to place book on shelf but is not hauling anything.");
                        return;
                    }
                    if (Storage.Accepts(Book))
					{
                        bool flag = false;
                        if (Book.holdingOwner != null)
                        {
                            Book.holdingOwner.TryTransferToContainer(Book, Storage.TryGetInnerInteractableThingOwner(), Book.stackCount, true);
                            flag = true;
                        }
                        else
                        {
                            flag = Storage.TryGetInnerInteractableThingOwner().TryAdd(Book, true);
                        }
                        Storage.CompStorageGraphic.UpdateGraphics();
                        pawn.carryTracker.innerContainer.Remove(Book);
                        CarefullyFinishProject();
                    }
                }
            };
            yield break;
        }

        protected void CarefullyFinishProject()
        {
            List<ResearchProjectDef> prerequisitesCopy = new List<ResearchProjectDef>();
            prerequisitesCopy.AddRange(Project.prerequisites);
            Project.prerequisites.Clear();
            Find.ResearchManager.FinishProject(Project);
            Project.prerequisites.AddRange(prerequisitesCopy);
            Messages.Message("MessageFiledTech".Translate(Project.label), Storage, MessageTypeDefOf.TaskCompletion, true);
        }

    }
}
