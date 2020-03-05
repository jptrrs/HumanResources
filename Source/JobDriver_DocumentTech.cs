using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using System.Reflection.Emit;

namespace HumanResources
{
	public class JobDriver_DocumentTech : JobDriver_Knowledge
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			project = job.bill.SelectedTech().Intersect(techComp.expertise).RandomElement();
			UpdateCost(project.baseCost);
			//job.bill.recipe.workAmount = VariableCost(project.baseCost);
			return base.TryMakePreToilReservations(errorOnFailed);
		}

		//private float VariableCost (float baseCost)
		//{
		//	return baseCost * (float)Math.Pow(baseCost, (1.0 / 3.0)) * 2;
		//}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			Bill bill = job.bill;
			//Log.Warning("JobDriver_DocumentTech:MakeNewToils: " + bill.Label);
			//Log.Message("Toil start:" + pawn + " is trying to document " + project + ", globalFailConditions count:" + globalFailConditions.Count);

			//from DoBill
			AddEndCondition(delegate
			{
				if (!desk.Spawned)
				{
					return JobCondition.Incompletable;
				}
				if (project.IsFinished)
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
			
			//yield return Toils_Recipe.DoRecipeWork().FailOnDespawnedNullOrForbiddenPlacedThings().FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);

			//Toil transferKnowledge = new Toil();
			//transferKnowledge.initAction = delegate
			//{
			//	Pawn actor = transferKnowledge.actor;
			//	Find.ResearchManager.FinishProject(project, true, actor);
			//	Messages.Message("MessageDocumentComplete".Translate(actor,project.LabelCap), desk, MessageTypeDefOf.TaskCompletion, true);
			//	//because vanillla FinishRecipeAndStartStoringProduct() wasn't working
			//	//bill.Notify_IterationCompleted(actor, new List<Thing> { });
			//	Notify_IterationCompleted(actor, bill as Bill_Production);
			//	if (job.RecipeDef.workSkill != null && !job.RecipeDef.UsesUnfinishedThing)
			//	{
			//		float xp = ticksSpentDoingRecipeWork * 0.1f * job.RecipeDef.workSkillLearnFactor;
			//		actor.skills.GetSkill(job.RecipeDef.workSkill).Learn(xp, false);
			//	}
			//	actor.jobs.EndCurrentJob(JobCondition.Succeeded, false);	
			//};
			//transferKnowledge.defaultCompleteMode = ToilCompleteMode.Instant;
			//transferKnowledge.FailOnDespawnedOrNull(TargetIndex.A);
			//yield return transferKnowledge;

			Toil research = new Toil();
			research.tickAction = delegate ()
			{
				Pawn actor = research.actor;
				float num = actor.GetStatValue(StatDefOf.ResearchSpeed, true);
				num *= TargetThingA.GetStatValue(StatDefOf.ResearchSpeedFactor, true); //testing
				ResearchPerformed(num, actor);
				actor.skills.Learn(SkillDefOf.Intellectual, 0.1f, false);
				actor.GainComfortFromCellIfPossible(true);
			};
			research.FailOn(() => project == null);
			//research.FailOn(() => !project.CanBeResearchedAt(this.ResearchBench, false));
			research.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
			research.WithEffect(EffecterDefOf.Research, TargetIndex.A);
			research.WithProgressBar(TargetIndex.A, delegate
			{
				if (project == null)
				{
					return 0f;
				}
				return project.ProgressPercent;
			}, false, -0.5f);
			research.defaultCompleteMode = ToilCompleteMode.Delay;
			research.defaultDuration = 4000;
			research.activeSkill = (() => SkillDefOf.Intellectual);
			yield return research;
			yield return Toils_General.Wait(2, TargetIndex.None);

			yield break;
		}

		public void ResearchPerformed(float amount, Pawn researcher)
		{
			if (project == null)
			{
				Log.Error("Researched without having an active project.", false);
				return;
			}
			amount *= ResearchPointsPerWorkTick;
			amount *= Find.Storyteller.difficulty.researchSpeedFactor;
			if (researcher != null && researcher.Faction != null)
			{
				amount /= project.CostFactor(researcher.Faction.def.techLevel);
			}
			if (DebugSettings.fastResearch)
			{
				amount *= 500f;
			}
			if (researcher != null)
			{
				researcher.records.AddTo(RecordDefOf.ResearchPointsResearched, amount);
			}
			float num = Find.ResearchManager.GetProgress(project);
			num += amount;

			FieldInfo progressInfo = AccessTools.Field(typeof(ResearchManager), "progress");
			object progressObj = progressInfo.GetValue(Find.ResearchManager);
			((Dictionary<ResearchProjectDef, float>)progressObj)[project] = num;

			if (project.IsFinished)
			{
				Find.ResearchManager.FinishProject(project, true, researcher);
			}
		}

		private float ResearchPointsPerWorkTick
		{
			get
			{
				FieldInfo researchPerWorktickInfo = AccessTools.Field(typeof(ResearchManager), "ResearchPointsPerWorkTick");
				return (float)researchPerWorktickInfo.GetValue(Find.ResearchManager) * DocumentedResearchFactor;
			}
		}

		private const int DocumentedResearchFactor = 5;
	}
}
