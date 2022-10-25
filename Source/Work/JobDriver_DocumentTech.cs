using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;

namespace HumanResources
{
    public class JobDriver_DocumentTech : JobDriver_Knowledge
    {
        protected ThingDef techStuff;

        public override void ExposeData()
        {
            Scribe_Defs.Look<ThingDef>(ref techStuff, "techStuff");
            base.ExposeData();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            project = techComp.homework?.Where(x => job.bill.Allows(x)).Intersect(techComp.knownTechs).Reverse().FirstOrDefault();
            if (project == null) return false;
            techStuff = TechTracker.FindTech(project).Stuff;
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
                    if (project == null)
                    {
                        Log.Error("[HumanResources] Tried to document a null project.");
                        TryMakePreToilReservations(true);
                        return true;
                    }
                    if (!techComp.homework.Contains(project)) return true;
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
            yield return MakeUnfinishedThingIfNeeded();
            yield return Toils_Recipe.DoRecipeWork().FailOnDespawnedNullOrForbiddenPlacedThings().FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            yield return FinishRecipeAndStartStoringProduct();
            if (!job.RecipeDef.products.NullOrEmpty<ThingDefCountClass>() || !job.RecipeDef.specialProducts.NullOrEmpty<SpecialProductType>())
            {
                yield return Toils_Reserve.Reserve(TargetIndex.B);
                yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, true, false);
                findPlaceTarget = Toils_Haul.CarryHauledThingToContainer();
                yield return findPlaceTarget;
                //Toil prepare = Toils_General.Wait(250);
                //prepare.WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
                ////test
                //prepare.AddFinishAction(delegate { Log.Message("ready to deposit hauled book"); });
                //yield return prepare;
                //Building_BookStore shelf = (Building_BookStore)job.GetTarget(TargetIndex.B).Thing;
                //yield return shelf.DepositHauledBook();
            }
            yield break;
        }

        private Toil TransfertoBookStore()
        {
            return new Toil
            {
                initAction = delegate
                {
                    Building_BookStore shelf = (Building_BookStore)job.GetTarget(TargetIndex.B).Thing;
                    CurToil.FailOn(() => shelf == null);
                    Thing book = pawn.carryTracker.CarriedThing;
                    if (pawn.carryTracker.CarriedThing == null)
                    {
                        Log.Error($"[HumanResources] {pawn} tried to place a book on shelf but is not hauling anything.");
                        return;
                    }
                    if (shelf.Accepts(book))
                    {
                        bool flag = false;
                        if (book.holdingOwner != null)
                        {
                            book.holdingOwner.TryTransferToContainer(book, shelf.TryGetInnerInteractableThingOwner(), book.stackCount, true);
                            flag = true;
                        }
                        else
                        {
                            flag = shelf.TryGetInnerInteractableThingOwner().TryAdd(book, true);
                        }
                        pawn.carryTracker.innerContainer.Remove(book);
                    }
                    else
                    {
                        Log.Error($"[HumanResources] {pawn} tried to place a book in {shelf}, but it won't accept it.");
                        return;
                    }
                    pawn.jobs.EndCurrentJob(JobCondition.Succeeded, true, true);
                }
            };
        }

        private Toil FinishRecipeAndStartStoringProduct()
        {
            //Reflection info
            MethodInfo CalculateIngredientsInfo = AccessTools.Method(typeof(Toils_Recipe), "CalculateIngredients", new Type[] { typeof(Job), typeof(Pawn) });
            MethodInfo CalculateDominantIngredientInfo = AccessTools.Method(typeof(Toils_Recipe), "CalculateDominantIngredient", new Type[] { typeof(Job), typeof(List<Thing>) });
            MethodInfo ConsumeIngredientsInfo = AccessTools.Method(typeof(Toils_Recipe), "ConsumeIngredients", new Type[] { typeof(List<Thing>), typeof(RecipeDef), typeof(Map) });
            //

            Toil toil = new Toil();
            toil.initAction = delegate ()
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                JobDriver_DoBill jobDriver_DoBill = (JobDriver_DoBill)actor.jobs.curDriver;
                if (curJob.RecipeDef.workSkill != null && !curJob.RecipeDef.UsesUnfinishedThing)
                {
                    float xp = (float)jobDriver_DoBill.ticksSpentDoingRecipeWork * 0.1f * curJob.RecipeDef.workSkillLearnFactor;
                    actor.skills.GetSkill(curJob.RecipeDef.workSkill).Learn(xp, false);
                }
                List<Thing> ingredients = (List<Thing>)CalculateIngredientsInfo.Invoke(this, new object[] { curJob, actor });
                Thing dominantIngredient = (Thing)CalculateDominantIngredientInfo.Invoke(this, new object[] { curJob, ingredients });
                List<Thing> list = GenRecipe.MakeRecipeProducts(curJob.RecipeDef, actor, ingredients, dominantIngredient, jobDriver_DoBill.BillGiver).ToList<Thing>();
                ConsumeIngredientsInfo.Invoke(this, new object[] { ingredients, curJob.RecipeDef, actor.Map });
                curJob.bill.Notify_IterationCompleted(actor, ingredients);
                RecordsUtility.Notify_BillDone(actor, list);
                UnfinishedThing unfinishedThing = curJob.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                if (curJob.bill.recipe.WorkAmountTotal((unfinishedThing != null) ? unfinishedThing.Stuff : null) >= 10000f && list.Count > 0)
                {
                    TaleRecorder.RecordTale(TaleDefOf.CompletedLongCraftingProject, new object[]
                    {
                        actor,
                        list[0].GetInnerIfMinified().def
                    });
                }
                if (list.Any())
                {
                    Find.QuestManager.Notify_ThingsProduced(actor, list);
                    techComp.homework.Remove(project);
                }
                if (list.Count == 0)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded, true, true);
                    return;
                }
                if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.DropOnFloor)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (!GenPlace.TryPlaceThing(list[i], actor.Position, actor.Map, ThingPlaceMode.Near, null, null, default(Rot4)))
                        {
                            Log.Error($"[HumanResources] {actor} could not drop recipe product {list[i]} near {actor.Position}", false);
                        }
                    }
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded, true, true);
                    return;
                }
                if (list.Count > 1)
                {
                    for (int j = 1; j < list.Count; j++)
                    {
                        if (!GenPlace.TryPlaceThing(list[j], actor.Position, actor.Map, ThingPlaceMode.Near, null, null, default(Rot4)))
                        {
                            Log.Error($"[HumanResources] {actor} could not drop recipe product {list[j]} near {actor.Position}", false);
                        }
                    }
                }
                IHaulDestination destination;
                if (curJob.bill.GetStoreMode() == BillStoreModeDefOf.BestStockpile)
                {
                    StoreUtility.TryFindBestBetterNonSlotGroupStorageFor(list[0], actor, actor.Map, StoragePriority.Unstored, actor.Faction, out destination);
                    curJob.targetB = destination as Thing;
                    curJob.targetA = list[0];
                    curJob.count = 1;
                    //test
                    Log.Message($"targets set for storage: A is {curJob.targetA}, B is {curJob.targetB}.");
                }
                else
                {
                    Log.ErrorOnce("[HumanResources] Unknown store mode", 9158246, false);
                }
                if (!GenPlace.TryPlaceThing(list[0], actor.Position, actor.Map, ThingPlaceMode.Near, null, null, default(Rot4)))
                {
                    Log.Error($"[HumanResources] Bill doer could not drop product {list[0]} near {actor.Position}", false);
                }
            };
            return toil;
        }

        private Toil MakeUnfinishedThingIfNeeded()
        {
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
                unfinishedThing.BoundBill = (Bill_ProductionWithUft)curJob.bill; // <- when pawn is a prisoner, the recipe doesn't seem to register this.
                unfinishedThing.ingredients = new List<Thing>
                {
                    new Thing()
                    {
                        def = unfinishedThing.Stuff,
                        stackCount = 0
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
