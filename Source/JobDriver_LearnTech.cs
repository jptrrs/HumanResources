using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace HumanResources
{
    public class JobDriver_LearnTech : JobDriver_Knowledge
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			project = techComp.HomeWork.RandomElement();
			UpdateCost(project.baseCost);
			//job.bill.recipe.workAmount = VariableCost(project.baseCost);
			return base.TryMakePreToilReservations(errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			Bill bill = job.bill;
			//Bill_Production probed = bill as Bill_Production;
			//Log.Warning("JobDriver_LearnTech:MakeNewToils: " + bill.Label);
			//Log.Message("Toil start:" + pawn + " is trying to learn " + project+ ", globalFailConditions count:" + globalFailConditions.Count);

			//from DoBill
			AddEndCondition(delegate
			{
				if (!desk.Spawned)
				{
					return JobCondition.Incompletable;
				}
				if (!techComp.HomeWork.Contains(project))
				{
					return JobCondition.Succeeded;
				}
				return JobCondition.Ongoing;
			});

			this.FailOnBurningImmobile(TargetIndex.A);
			this.FailOn(delegate ()
			{
				IBillGiver billGiver = desk as IBillGiver;
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
			yield return gotoBillGiver;
			yield return Toils_Recipe.DoRecipeWork().FailOnDespawnedNullOrForbiddenPlacedThings().FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);

			Toil acquireKnowledge = new Toil();
			acquireKnowledge.initAction = delegate
			{
				Pawn actor = acquireKnowledge.actor;
				CompKnowledge techComp = actor.GetComp<CompKnowledge>();
				techComp.expertise.Add(project);
				techComp.HomeWork.Clear();
				techComp.LearnCrops(project);
				Messages.Message("MessageStudyComplete".Translate(actor,project.LabelCap), (Thing)desk, MessageTypeDefOf.TaskCompletion, true);
				Notify_IterationCompleted(actor, bill as Bill_Production);
				if (job.RecipeDef.workSkill != null && !job.RecipeDef.UsesUnfinishedThing)
				{
					float xp = ticksSpentDoingRecipeWork * 0.1f * job.RecipeDef.workSkillLearnFactor;
					actor.skills.GetSkill(job.RecipeDef.workSkill).Learn(xp, false);
				}
				actor.jobs.EndCurrentJob(JobCondition.Succeeded, false);
			};
			acquireKnowledge.defaultCompleteMode = ToilCompleteMode.Instant;
			acquireKnowledge.FailOnDespawnedOrNull(TargetIndex.A);
			yield return acquireKnowledge;
			yield break;
		}

	}
}
