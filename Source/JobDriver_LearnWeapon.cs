using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using Verse.Sound;

namespace HumanResources
{
    public class JobDriver_LearnWeapon : JobDriver_DoBill
	{
		protected CompKnowledge techComp
		{
			get
			{
				return pawn.TryGetComp<CompKnowledge>();
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Pawn pawn = this.pawn;
			LocalTargetInfo target = this.job.GetTarget(TargetIndex.A);
			Job job = this.job;
			if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job, 1, -1, null);
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			Bill bill = job.bill;
			//Log.Message("Toil start:" + pawn + " is trying to learn how to use a " + TargetThingB + ", globalFailConditions count:" + globalFailConditions.Count);
			AddEndCondition(delegate
			{
				Thing thing = base.GetActor().jobs.curJob.GetTarget(TargetIndex.A).Thing;
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
			AddFinishAction(delegate ()
			{
				ThingWithComps thingWithComps = (ThingWithComps)job.targetB.Thing;
				pawn.equipment.TryDropEquipment(thingWithComps, out thingWithComps, pawn.Position, false);
			});

			Toil gotoBillGiver = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
			yield return Toils_Jump.JumpIf(gotoBillGiver, () => job.GetTargetQueue(TargetIndex.B).NullOrEmpty<LocalTargetInfo>());
			Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B, true);
			yield return extract;
			Toil getToHaulTarget = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
			yield return getToHaulTarget;

			//temporary equip
			yield return new Toil
			{
				initAction = delegate ()
				{
					ThingWithComps thingWithComps = (ThingWithComps)job.targetB.Thing;
					ThingWithComps thingWithComps2;
					if (thingWithComps.def.stackLimit > 1 && thingWithComps.stackCount > 1)
					{
						thingWithComps2 = (ThingWithComps)thingWithComps.SplitOff(1);
					}
					else
					{
						thingWithComps2 = thingWithComps;
						thingWithComps2.DeSpawn(DestroyMode.Vanish);
					}
					pawn.equipment.MakeRoomFor(thingWithComps2);
					pawn.equipment.AddEquipment(thingWithComps2);
					if (thingWithComps.def.soundInteract != null)
					{
						thingWithComps.def.soundInteract.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};

			yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.B, extract);
			yield return gotoBillGiver;
			yield return Toils_Combat.TrySetJobToUseAttackVerb(TargetIndex.A);
			Toil train = new Toil();
			train.initAction = delegate ()
			{
				Pawn actor = train.actor;
				Job curJob = actor.jobs.curJob;
				JobDriver_DoBill jobDriver_DoBill = (JobDriver_DoBill)actor.jobs.curDriver;
				UnfinishedThing unfinishedThing = curJob.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
				jobDriver_DoBill.workLeft = curJob.bill.recipe.WorkAmountTotal((unfinishedThing == null) ? null : unfinishedThing.Stuff);
				jobDriver_DoBill.billStartTick = Find.TickManager.TicksGame;
				jobDriver_DoBill.ticksSpentDoingRecipeWork = 0;
				curJob.bill.Notify_DoBillStarted(actor);
			};
			train.tickAction = delegate ()
			{
				Pawn actor = train.actor;
				Job curJob = actor.jobs.curJob;
				JobDriver_DoBill jobDriver_DoBill = (JobDriver_DoBill)actor.jobs.curDriver;
				jobDriver_DoBill.ticksSpentDoingRecipeWork++;
				curJob.bill.Notify_PawnDidWork(actor);
				IBillGiverWithTickAction billGiverWithTickAction = train.actor.CurJob.GetTarget(TargetIndex.A).Thing as IBillGiverWithTickAction;
				if (billGiverWithTickAction != null)
				{
					billGiverWithTickAction.UsedThisTick();
				}
				float num = (curJob.RecipeDef.workSpeedStat != null) ? actor.GetStatValue(curJob.RecipeDef.workSpeedStat, true) : 1f;
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
				jobDriver_DoBill.workLeft -= num;
				actor.GainComfortFromCellIfPossible();
				if (jobDriver_DoBill.workLeft <= 0f)
				{
					jobDriver_DoBill.ReadyForNextToil();
				}
				if (curJob.bill.recipe.UsesUnfinishedThing)
				{
					int num2 = Find.TickManager.TicksGame - jobDriver_DoBill.billStartTick;
					if (num2 >= 3000 && num2 % 1000 == 0)
					{
						actor.jobs.CheckForJobOverride();
					}
				}
				//pawn posture
				Verb verbToUse = actor.jobs.curJob.verbToUse;
				LocalTargetInfo target = actor.jobs.curJob.GetTarget(TargetIndex.A);
				pawn.stances.SetStance(new Stance_Warmup(1, target, verbToUse));
			};
			train.defaultCompleteMode = ToilCompleteMode.Never;
			train.WithEffect(() => train.actor.CurJob.bill.recipe.effectWorking, TargetIndex.A);
			train.PlaySustainerOrSound(() => train.actor.CurJob.bill.recipe.soundWorking);
			train.WithProgressBar(TargetIndex.A, delegate
			{
				Pawn actor = train.actor;
				Job curJob = actor.CurJob;
				return 1f - ((JobDriver_DoBill)actor.jobs.curDriver).workLeft / curJob.bill.recipe.WorkAmountTotal(null);
			}, false, -0.5f);
			train.FailOn(() => train.actor.CurJob.bill.suspended);
			train.activeSkill = (() => train.actor.CurJob.bill.recipe.workSkill);
			yield return train.FailOnDespawnedNullOrForbiddenPlacedThings().FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
			Toil acquireProficiency = new Toil();
			acquireProficiency.initAction = delegate
			{
				Pawn actor = acquireProficiency.actor;
				CompKnowledge techComp = actor.GetComp<CompKnowledge>();
				techComp.proficientWeapons.Add(TargetThingB.def);
				//Log.Message(actor + " can use the following weapons: " + techComp.proficientWeapons.ToStringSafeEnumerable());
				Messages.Message("MessageTrainingComplete".Translate(actor, TargetThingB.def.LabelCap), TargetThingA, MessageTypeDefOf.TaskCompletion, true);
				job.bill.Notify_IterationCompleted(actor, new List<Thing> { });
				//Notify_IterationCompleted(actor, bill as Bill_Production);
				if (job.RecipeDef.workSkill != null && !job.RecipeDef.UsesUnfinishedThing)
				{
					float xp = ticksSpentDoingRecipeWork * 0.1f * job.RecipeDef.workSkillLearnFactor;
					actor.skills.GetSkill(job.RecipeDef.workSkill).Learn(xp, false);
				}
				actor.jobs.EndCurrentJob(JobCondition.Succeeded, false);
			};
			acquireProficiency.defaultCompleteMode = ToilCompleteMode.Instant;
			acquireProficiency.FailOnDespawnedOrNull(TargetIndex.A);
			yield return acquireProficiency;
			yield break;
		}
	}
}
