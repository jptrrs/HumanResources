using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HumanResources
{
    //Changed in RW 1.5
    public class JobDriver_LearnWeapon : JobDriver_DoBill
    {
        protected bool practice = false;
        protected bool unknown = false;

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
        };

        private static FieldInfo currentTargetInfo = AccessTools.Field(typeof(Verb), "currentTarget");

        private static FieldInfo equipmentDefInfo = AccessTools.Field(typeof(Projectile), "equipmentDef");

        private static FieldInfo launcherInfo = AccessTools.Field(typeof(Projectile), "launcher");

        private static MethodInfo TryCastShotInfo = AccessTools.Method(typeof(Verb_MeleeAttack), "TryCastShot");

        private FieldInfo equipmentInfo = AccessTools.Field(typeof(Pawn_EquipmentTracker), "equipment");

        protected CompKnowledge techComp
        {
            get
            {
                return pawn.TryGetComp<CompKnowledge>();
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref practice, "practice", false, false);
            Scribe_Values.Look<bool>(ref unknown, "unknown", false, false);
            base.ExposeData();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = this.pawn;
            LocalTargetInfo target = this.job.GetTarget(TargetIndex.A);
            Job job = this.job;
            Bill bill = job.bill;
            if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed)) return false;
            practice = bill.recipe == TechDefOf.PracticeWeaponMelee || bill.recipe == TechDefOf.PracticeWeaponShooting;
            if (!practice) pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job, 1, -1, null);
            unknown = bill.recipe == TechDefOf.ExperimentWeaponShooting || bill.recipe == TechDefOf.ExperimentWeaponMelee;
            return true;
        }

        protected bool CheckExperimentFail(Pawn tester, Thing weapon)
        {
            float num = 1f;
            float delta = 1f;
            CompKnowledge techComp = tester.TryGetComp<CompKnowledge>();
            if (techComp != null)
            {
                delta = (int)techComp.techLevel / (int)weapon.def.techLevel;
            }
            float factor = WeaponExperimentChanceFactor.Evaluate(delta);
            num *= factor;
            num = Mathf.Min(num, 0.98f);
            if (Prefs.LogVerbose) Log.Message($"[HumanResources] Experiment weapon calculation for {tester} vs. {weapon.def}: techLevel diff is {delta} -> chance factor is {num}");
            Job job = this.job;
            RecipeDef recipe = job.bill.recipe;
            if (!Rand.Chance(num)) //Determined by the tech level difference according to curve.
            {
                if (Rand.Chance(0.5f)) //50% chance the failure is harmless;
                {
                    techComp?.AddWeaponTrauma(weapon.def);
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

        protected void Equip(TargetIndex target, bool saveCurrent)
        {
            if (saveCurrent && pawn.equipment?.Primary != null)
            {
                pawn.CurJob.targetC = new LocalTargetInfo(pawn.equipment.Primary);
                pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment.Primary, pawn.inventory.innerContainer);
            }
            ThingWithComps targetThing = (ThingWithComps)job.GetTarget(target).Thing;
            ThingWithComps isolatedThing;
            if (targetThing.def.stackLimit > 1 && targetThing.stackCount > 1)
            {
                isolatedThing = (ThingWithComps)targetThing.SplitOff(1);
            }
            else
            {
                isolatedThing = targetThing;
                if (targetThing.Spawned) isolatedThing.DeSpawn(DestroyMode.Vanish);
            }
            pawn.equipment.MakeRoomFor(isolatedThing);
            ThingOwner equipment = (ThingOwner)equipmentInfo.GetValue(pawn.equipment);
            if (!equipment.TryAddOrTransfer(isolatedThing)) pawn.equipment.AddEquipment(isolatedThing);
            if (targetThing.def.soundInteract != null)
            {
                targetThing.def.soundInteract.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
            }
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
                job.bill.Notify_IterationCompleted(actor, new List<Thing> { });
                actor.jobs.EndCurrentJob(JobCondition.Succeeded, false);
            };
            finalizeTraining.defaultCompleteMode = ToilCompleteMode.Instant;
            finalizeTraining.FailOnDespawnedOrNull(TargetIndex.A);
            return finalizeTraining;
        }

        protected void LearnWeaponGroup(ThingDef weapon, Pawn pawn, CompKnowledge techComp)
        {
            bool groupRanged = HumanResourcesSettings.LearnRangedWeaponsByGroup && weapon.IsRangedWeapon;
            bool groupMelee = HumanResourcesSettings.LearnMeleeWeaponsByGroup && weapon.IsMeleeWeapon;
            if (TechTracker.FindTechs(weapon).Any() && (groupRanged || groupMelee))
            {
                foreach (ThingDef sister in TechTracker.FindTech(weapon).Weapons)
                {
                    if ((groupRanged && sister.IsRangedWeapon) || (groupMelee && sister.IsMeleeWeapon))
                    {
                        techComp.LearnWeapon(sister);
                        Messages.Message("MessageTrainingComplete".Translate(pawn, sister), MessageTypeDefOf.TaskCompletion);
                    }
                }
            }
            else
            {
                techComp.LearnWeapon(weapon);
                Messages.Message("MessageTrainingComplete".Translate(pawn, weapon), MessageTypeDefOf.TaskCompletion);
            }
        }

        public override IEnumerable<Toil> MakeNewToils()
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
                if (pawn.equipment.Primary != null && !practice)
                {
                    if (pawn.equipment.Primary == (ThingWithComps)job.targetB.Thing)
                    {
                        ThingWithComps thingWithComps = (ThingWithComps)job.targetB.Thing;
                        pawn.equipment.TryDropEquipment(thingWithComps, out thingWithComps, pawn.Position, false);
                    }
                    if (job.GetTarget(TargetIndex.C).IsValid) Equip(TargetIndex.C, false);
                }
            });
            Toil gotoBillGiver = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            if (!practice)
            {
                yield return Toils_Jump.JumpIf(gotoBillGiver, () => job.GetTargetQueue(TargetIndex.B).NullOrEmpty<LocalTargetInfo>());
                Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B, true);
                yield return extract;
                Toil getToHaulTarget = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
                yield return getToHaulTarget;
                yield return new Toil
                {
                    initAction = delegate ()
                    {
                        Equip(TargetIndex.B, true);
                    },
                    defaultCompleteMode = ToilCompleteMode.Instant
                };

                yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.B, extract);
            }
            Thing currentWeapon = practice ? pawn.equipment.Primary : job.targetB.Thing;
            yield return gotoBillGiver;
            yield return Toils_Combat.TrySetJobToUseAttackVerb(TargetIndex.A);
            yield return Train(currentWeapon).FailOnDespawnedNullOrForbiddenPlacedThings().FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            yield return FinalizeTraining();
            yield break;
        }

        protected Toil Train(Thing currentWeapon)
        {
            Toil train = new Toil();
            train.initAction = delegate ()
            {
                Pawn actor = train.actor;
                Job curJob = actor.jobs.curJob;
                ThingDef weapon = practice ? currentWeapon.def : job.targetB.Thing.def;
                workLeft = curJob.bill.recipe.WorkAmountTotal(null);
                billStartTick = Find.TickManager.TicksGame;
                ticksSpentDoingRecipeWork = 0;
                curJob.bill.Notify_DoBillStarted(actor);
                Verb verbToUse = actor.TryGetAttackVerb(currentWeapon, true);
                LocalTargetInfo target = actor.jobs.curJob.GetTarget(TargetIndex.A);
                pawn.stances.SetStance(new Stance_Cooldown(2, target, verbToUse));
            };
            train.tickAction = delegate ()
            {
                Pawn actor = train.actor;
                Job curJob = actor.jobs.curJob;
                ThingDef weapon = practice ? currentWeapon.def : job.targetB.Thing.def;
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
                LearningUtility.WeaponTrainingAnimation(pawn, pawn.jobs.curJob.GetTarget(TargetIndex.A), actor.TryGetAttackVerb(currentWeapon, true), ticksSpentDoingRecipeWork);
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
                return 1f - (workLeft / curJob.bill.recipe.WorkAmountTotal(null));
            }, false, -0.5f);
            train.FailOn(() => train.actor.CurJob.bill.suspended);
            train.activeSkill = () => train.actor.CurJob.bill.recipe.workSkill;
            return train;
        }

        private void Backfire(Pawn tester, Thing weapon)
        {
            Verb verb = tester.TryGetAttackVerb(weapon, true);
            if (verb != null)
            {
                if (verb.IsMeleeAttack)
                {
                    currentTargetInfo.SetValue(verb, new LocalTargetInfo(tester));
                    TryCastShotInfo.Invoke(verb, new object[] { });
                }
                else
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
            }
            else Log.Error("[HumanResources] No verb found for using " + weapon.def);
        }

        private MethodInfo ImpactInfo(Type type)
        {
            return AccessTools.Method(type, "Impact");
        }

        private void Scratch(Thing weapon, float factor)
        {
            float damage = Rand.Range(1f, 10f) * factor;
            weapon.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, damage, 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null));
        }
    }
}