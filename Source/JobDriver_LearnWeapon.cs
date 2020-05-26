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

		protected bool unknown = false;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Pawn pawn = this.pawn;
			LocalTargetInfo target = this.job.GetTarget(TargetIndex.A);
			Job job = this.job;
			Bill bill = job.bill;
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
			AddFinishAction(delegate ()
			{
				//Log.Message("LearnWeapon: finishing");
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
				ThingDef weapon = job.targetB.Thing.def;
				workLeft = curJob.bill.recipe.WorkAmountTotal(null);
				billStartTick = Find.TickManager.TicksGame;
				ticksSpentDoingRecipeWork = 0;
				curJob.bill.Notify_DoBillStarted(actor);
				//sound:
				if (weapon.soundInteract != null) weapon.soundInteract.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
			};
			train.tickAction = delegate ()
			{
				Pawn actor = train.actor;
				Job curJob = actor.jobs.curJob;
				ThingDef weapon = job.targetB.Thing.def;
				ticksSpentDoingRecipeWork++;
				curJob.bill.Notify_PawnDidWork(actor);
				IBillGiverWithTickAction billGiverWithTickAction = train.actor.CurJob.GetTarget(TargetIndex.A).Thing as IBillGiverWithTickAction;
				if (billGiverWithTickAction != null)
				{
					billGiverWithTickAction.UsedThisTick();
				}
				float num = (curJob.RecipeDef.workSpeedStat != null) ? actor.GetStatValue(curJob.RecipeDef.workSpeedStat, true) : 1f;
				if (curJob.RecipeDef.workTableSpeedStat != null)
				{
					Building_WorkTable building_WorkTable = BillGiver as Building_WorkTable;
					if (building_WorkTable != null)
					{
						num *= building_WorkTable.GetStatValue(curJob.RecipeDef.workTableSpeedStat, true);
					}
				}
				if (DebugSettings.fastCrafting)
				{
					num *= 30f;
				}
				workLeft -= num;
				actor.GainComfortFromCellIfPossible();
				if (workLeft <= 0f)
				{
					ReadyForNextToil();
				}

				//pawn posture
				Verb verbToUse = actor.jobs.curJob.verbToUse;
				LocalTargetInfo target = actor.jobs.curJob.GetTarget(TargetIndex.A);
				pawn.stances.SetStance(new Stance_Warmup(1, target, verbToUse));

				//sound:
				if (verbToUse.verbProps != null && verbToUse.verbProps.warmupTime > 0)
				{
					if ((ticksSpentDoingRecipeWork % verbToUse.verbProps.AdjustedFullCycleTime(verbToUse, actor).SecondsToTicks()) == 0)
					{
						if (verbToUse.verbProps.soundCast != null)
						{
							verbToUse.verbProps.soundCast.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
						}
						if (verbToUse.verbProps.soundCastTail != null)
						{
							verbToUse.verbProps.soundCastTail.PlayOneShotOnCamera(pawn.Map);
						}
					}
				}
				if (job.RecipeDef.workSkill != null)
				{
					//float xpDelta = techComp.proficientWeapons.Contains(job.targetB.Thing.def) ? 1f : 0.1f;
					float xp = 0.1f * job.RecipeDef.workSkillLearnFactor;
					actor.skills.GetSkill(job.RecipeDef.workSkill).Learn(xp, false);
				}
			};
			train.defaultCompleteMode = ToilCompleteMode.Never;
			train.WithEffect(() => train.actor.CurJob.bill.recipe.effectWorking, TargetIndex.A);
			train.PlaySustainerOrSound(() => train.actor.CurJob.bill.recipe.soundWorking);
			train.WithProgressBar(TargetIndex.A, delegate
			{
				Pawn actor = train.actor;
				Job curJob = actor.CurJob;
				//return 1f - ((JobDriver_DoBill)actor.jobs.curDriver).workLeft / curJob.bill.recipe.WorkAmountTotal(null);
				return 1f - (workLeft / curJob.bill.recipe.WorkAmountTotal(null));
			}, false, -0.5f);
			train.FailOn(() => train.actor.CurJob.bill.suspended);
			train.activeSkill = () => train.actor.CurJob.bill.recipe.workSkill;
			yield return train.FailOnDespawnedNullOrForbiddenPlacedThings().FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
			Toil finalizeTraining = new Toil();
			finalizeTraining.initAction = delegate
			{
				Pawn actor = finalizeTraining.actor;
				CompKnowledge techComp = actor.TryGetComp<CompKnowledge>();
				if (!techComp.proficientWeapons.Contains(job.targetB.Thing.def)) 
				{
					techComp.proficientWeapons.Add(TargetThingB.def);
				}
				job.bill.Notify_IterationCompleted(actor, new List<Thing> { });
				actor.jobs.EndCurrentJob(JobCondition.Succeeded, false);
			};
			finalizeTraining.defaultCompleteMode = ToilCompleteMode.Instant;
			finalizeTraining.FailOnDespawnedOrNull(TargetIndex.A);
			yield return finalizeTraining;

			//testing
			yield return Toils_Reserve.Reserve(TargetIndex.B, 1, -1, null);
			Toil findPlaceTarget = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
			yield return findPlaceTarget;
			yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, findPlaceTarget, true, true);

			yield break;
		}
	}
}
