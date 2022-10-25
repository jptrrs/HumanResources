using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace HumanResources
{
    public class JobDriver_DocumentTechDigital : JobDriver_DocumentTech
    {
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
                        Log.Warning("[HumanResources] " + pawn + " tried to document a null project.");
                        TryMakePreToilReservations(true);
                        return true;
                    }
                    if (!techComp.homework.Contains(project)) return true;
                }
                return false;
            });
            Toil gotoBillGiver = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return gotoBillGiver;
            Toil document = new Toil();
            document.initAction = delegate ()
            {
                Pawn actor = document.actor;
                Job curJob = actor.jobs.curJob;
                JobDriver_DoBill jobDriver_DoBill = (JobDriver_DoBill)actor.jobs.curDriver;
                jobDriver_DoBill.billStartTick = Find.TickManager.TicksGame;
                jobDriver_DoBill.ticksSpentDoingRecipeWork = 0;
                curJob.bill.Notify_DoBillStarted(actor);
            };
            document.tickAction = delegate ()
            {
                Pawn actor = document.actor;
                Job curJob = actor.jobs.curJob;
                JobDriver_DoBill jobDriver_DoBill = (JobDriver_DoBill)actor.jobs.curDriver;
                jobDriver_DoBill.ticksSpentDoingRecipeWork++;
                curJob.bill.Notify_PawnDidWork(actor);
                IBillGiverWithTickAction billGiverWithTickAction = document.actor.CurJob.GetTarget(TargetIndex.A).Thing as IBillGiverWithTickAction;
                if (billGiverWithTickAction != null)
                {
                    billGiverWithTickAction.UsedThisTick();
                }
                SkillDef skill = curJob.RecipeDef.workSkill != null ? curJob.RecipeDef.workSkill : SkillDefOf.Intellectual;
                actor.skills.Learn(skill, 0.1f * curJob.RecipeDef.workSkillLearnFactor, false);
                float num = (curJob.RecipeDef.workSpeedStat == null) ? 1f : actor.GetStatValue(curJob.RecipeDef.workSpeedStat, true);
                if (curJob.RecipeDef.workTableSpeedStat != null)
                {
                    Building_WorkTable building_WorkTable = jobDriver_DoBill.BillGiver as Building_WorkTable;
                    if (building_WorkTable != null)
                    {
                        num *= building_WorkTable.GetStatValue(curJob.RecipeDef.workTableSpeedStat, true);
                    }
                }
                if (DebugSettings.fastCrafting)
                {
                    num *= 30f;
                }
                project.Uploaded(num, TargetThingA);
                actor.GainComfortFromCellIfPossible(true);
                if (project.IsFinished)
                {
                    curJob.bill.Notify_IterationCompleted(actor, new List<Thing>() { });
                    project.Unlock(jobDriver_DoBill.BillGiver as Thing, false);
                    techComp.homework.Remove(project);
                    jobDriver_DoBill.ReadyForNextToil();
                    return;
                }
            };
            document.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            document.WithEffect(EffecterDefOf.Research, TargetIndex.A);
            document.WithProgressBar(TargetIndex.A, delegate
            {
                if (project == null)
                {
                    return 0f;
                }
                return project.ProgressPercent;
            }, false, -0.5f);
            document.defaultCompleteMode = ToilCompleteMode.Delay;
            document.defaultDuration = 4000;
            document.activeSkill = (() => SkillDefOf.Intellectual);
            yield return document;
            yield return Toils_General.Wait(2, TargetIndex.None);
            yield break;
        }
    }
}
