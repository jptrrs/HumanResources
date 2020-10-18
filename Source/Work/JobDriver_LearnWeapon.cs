using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using Verse.Sound;
using HarmonyLib;
using System.Reflection;
using System;
using UnityEngine;

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
		protected bool practice = false;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Pawn pawn = this.pawn;
			LocalTargetInfo target = this.job.GetTarget(TargetIndex.A);
			Job job = this.job;
			Bill bill = job.bill;
			if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed)) return false;
			practice = bill.recipe == TechDefOf.PracticeWeaponMelee || bill.recipe == TechDefOf.PracticeWeaponShooting;
			if (!practice) pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job, 1, -1, null);
			unknown = bill.recipe == TechDefOf.ExperimentWeaponShooting;
			Log.Warning("DEBUG LearWeapon Job: practice=" + practice + ", unknown=" + unknown + ", recipe is "+bill.recipe.label);
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
				if (pawn.equipment.Primary != null && !practice) pawn.equipment.TryDropEquipment(thingWithComps, out thingWithComps, pawn.Position, false);
			});
			Toil gotoBillGiver = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

			if (!practice)
			{
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
			}
			Thing currentWeapon = practice ? pawn.equipment.Primary : job.targetB.Thing;
			yield return gotoBillGiver;
			yield return Toils_Combat.TrySetJobToUseAttackVerb(TargetIndex.A);
			Toil train = new Toil();
			train.initAction = delegate ()
			{
				Pawn actor = train.actor;
				Job curJob = actor.jobs.curJob;
				ThingDef weapon = practice ? currentWeapon.def : job.targetB.Thing.def;//currentWeapon.def;//job.targetB.Thing.def;
				workLeft = curJob.bill.recipe.WorkAmountTotal(null);
				billStartTick = Find.TickManager.TicksGame;
				ticksSpentDoingRecipeWork = 0;
				curJob.bill.Notify_DoBillStarted(actor);
			};
			train.tickAction = delegate ()
			{
				Pawn actor = train.actor;
				Job curJob = actor.jobs.curJob;
				ThingDef weapon = practice ? currentWeapon.def : job.targetB.Thing.def; //currentWeapon.def;//job.targetB.Thing.def;
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
				Verb verbToUse = actor.TryGetAttackVerb(currentWeapon, true);//actor.jobs.curJob.verbToUse;
				LocalTargetInfo target = actor.jobs.curJob.GetTarget(TargetIndex.A);
				pawn.stances.SetStance(new Stance_Warmup(1, target, verbToUse));

				//sound:
				if (!unknown && verbToUse.verbProps != null && verbToUse.verbProps.warmupTime > 0)
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
					float xpDelta = practice ? 0.5f : 0.1f;
					float xp = xpDelta * job.RecipeDef.workSkillLearnFactor;
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
			yield return FinalizeTraining();

			//testing
			//yield return Toils_Reserve.Reserve(TargetIndex.B, 1, -1, null);
			//Toil findPlaceTarget = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
			//yield return findPlaceTarget;
			//yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, findPlaceTarget, true, true);

			yield break;
		}

		protected Toil FinalizeTraining()
        {
			Toil finalizeTraining = new Toil();
			finalizeTraining.initAction = delegate
			{
				Pawn actor = finalizeTraining.actor;
				if (!practice)
				{
					ThingDef weapon = job.targetB.Thing.def;
					CompKnowledge techComp = actor.TryGetComp<CompKnowledge>();
					bool safe = true;
					if (unknown) safe = !CheckExperimentFail(actor, TargetThingB);
					if (!techComp.proficientWeapons.Contains(weapon) && safe) LearnWeaponGroup(weapon, actor, techComp);
				}
				Log.Warning("DEBUG finalizing " + job.bill.Label);
				job.bill.Notify_IterationCompleted(actor, new List<Thing> { });
				actor.jobs.EndCurrentJob(JobCondition.Succeeded, false);
			};
			finalizeTraining.defaultCompleteMode = ToilCompleteMode.Instant;
			finalizeTraining.FailOnDespawnedOrNull(TargetIndex.A);
			return finalizeTraining;
		}

		private MethodInfo ImpactInfo(Type type) 
		{
			return AccessTools.Method(type, "Impact");
		}

		protected void LearnWeaponGroup(ThingDef weapon, Pawn pawn, CompKnowledge techComp)
		{
			bool ranged = weapon.IsRangedWeapon;
			bool melee = weapon.IsMeleeWeapon;
			if (ModBaseHumanResources.LearnAnyWeaponByGroup && Extension_Research.TechByWeapon.ContainsKey(weapon))
			{
				foreach (ThingDef sister in Extension_Research.WeaponsByTech[Extension_Research.TechByWeapon[weapon]])
                {
					if (ModBaseHumanResources.LearnRangedWeaponsByGroup && sister.IsRangedWeapon == ranged || ModBaseHumanResources.LearnMeleeWeaponsByGroup && sister.IsMeleeWeapon == melee)
					{
						techComp.LearnWeapon(sister);
						Messages.Message("MessageTrainingComplete".Translate(pawn, sister), MessageTypeDefOf.TaskCompletion);
					}
					//TEST group by: sister.projectile.damageDef !
                }
			}
			else
			{
				techComp.LearnWeapon(weapon);
				Messages.Message("MessageTrainingComplete".Translate(pawn, weapon), MessageTypeDefOf.TaskCompletion);
			}
		}

        protected bool CheckExperimentFail(Pawn tester, Thing weapon)
        {
			float num = 1f;
            float delta = 1f;
            if (tester.Faction?.def.techLevel != null) //Look for pawn's own actual techlevel?
            {
                delta = (int)tester.Faction.def.techLevel / (int)weapon.def.techLevel;
            }
            float test = WeaponExperimentChanceFactor.Evaluate(delta);
            Log.Message("DEBUG Experiment Weapon chance for " + tester + " vs. " + weapon.def + " is " + test);
            num *= test;
            num = Mathf.Min(num, 0.98f);
            Job job = this.job;
            RecipeDef recipe = job.bill.recipe;

            if (!Rand.Chance(num)) //Determined by the tech level difference according to curve.
            {
                if (Rand.Chance(0.5f)) //50% chance the failure is harmless;
                {
					tester.TryGetComp<CompKnowledge>()?.AddWeaponTrauma(weapon.def);
					if (Rand.Chance(0.5f)) //25% chance the weapon just takes some damage;
					{
						if (Rand.Chance(0.2f)) //5% chance the weapon is destroyed;
						{
							Find.LetterStack.ReceiveLetter("LetterLabelWeaponTestFailed".Translate(weapon.def.Named("WEAPON")), "MessageMedicalWeaponTestFailureRidiculous".Translate(tester.LabelShort, tester.LabelShort, weapon.def.Named("WEAPON"), tester.Named("TESTER"), recipe.Named("RECIPE")), LetterDefOf.NegativeEvent, tester, null, null, null, null);
							Backfire(tester, weapon);
							weapon.Destroy();
						}
						else //20% chance the weapon backfires.
						{
							Find.LetterStack.ReceiveLetter("LetterLabelWeaponTestFailed".Translate(weapon.def.Named("WEAPON")), "MessageWeaponTestFailureCatastrophic".Translate(tester.LabelShort, tester.LabelShort, weapon.def.Named("WEAPON"), tester.Named("TESTER"), recipe.Named("RECIPE")), LetterDefOf.NegativeEvent, tester, null, null, null, null);
							Backfire(tester, weapon);
						}
					}
                    else
                    {
						Find.LetterStack.ReceiveLetter("LetterLabelWeaponTestFailed".Translate(weapon.def.Named("WEAPON")), "MessageWeaponTestFailureMinor".Translate(tester.LabelShort, tester.LabelShort, weapon.def.Named("WEAPON"), tester.Named("TESTER"), recipe.Named("RECIPE")), LetterDefOf.NegativeEvent, tester, null, null, null, null);
						float damageFactor = 4 - delta;
						Scratch(weapon, damageFactor);
                    }
				}
                else
                {
					Messages.Message("WeaponTestFail".Translate(tester, weapon.def), MessageTypeDefOf.NegativeEvent);
				}
				return true;
            }
            return false;
        }

		private static FieldInfo launcherInfo = AccessTools.Field(typeof(Projectile), "launcher");
		private static FieldInfo equipmentDefInfo = AccessTools.Field(typeof(Projectile), "equipmentDef");
		//private FieldInfo targetCoverDefInfo = AccessTools.Field(typeof(Projectile), "targetCoverDef");

		private void Backfire(Pawn tester, Thing weapon)
        {
			Verb verb = tester.TryGetAttackVerb(weapon, true);
			if (verb != null)
			{
				ThingDef projectileDef = verb.GetProjectile();
				Type projectileType = projectileDef.thingClass;
				Projectile bullet = (Projectile)GenSpawn.Spawn(verb.GetProjectile(), tester.Position, tester.Map, WipeMode.Vanish);
				launcherInfo.SetValue(bullet, tester);
				bullet.intendedTarget = TargetA;
				equipmentDefInfo.SetValue(bullet, weapon.def);
				bullet.def = projectileDef;
				ImpactInfo(projectileType).Invoke(bullet, new object[] { tester });
			}
			else Log.Error("[HumanResources] No verb found for using " + weapon.def);
		}

		private void Scratch(Thing weapon, float factor)
        {
			float damage = Rand.Range(1f, 10f) * factor;
			weapon.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, damage, 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null));
		}

		private static readonly SimpleCurve WeaponExperimentChanceFactor = new SimpleCurve
        {
			{
				new CurvePoint(0f, 0f),
				true
			},
			{
				new CurvePoint(0.5f, 0.1f),
				true
			},
			{
				new CurvePoint(2f, 0.8f),
				true
			}


            //{
            //    new CurvePoint(0f, 0.7f),
            //    true
            //},
            //{
            //    new CurvePoint(1f, 1f),
            //    true
            //},
            //{
            //    new CurvePoint(2f, 1.3f),
            //    true
            //}
        };
    }
}
