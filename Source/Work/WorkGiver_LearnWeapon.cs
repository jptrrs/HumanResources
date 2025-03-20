﻿using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;

namespace HumanResources
{
    using static ModBaseHumanResources;
    internal class WorkGiver_LearnWeapon : WorkGiver_Knowledge
    {
        public new List<ThingCount> chosenIngThings = new List<ThingCount>();
        protected FieldInfo rangeInfo = AccessTools.Field(typeof(WorkGiver_DoBill), "ReCheckFailedBillTicksRange");

        public static bool ShouldReserve(Pawn p, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
        {
            if (p.TryGetComp<CompKnowledge>().knownWeapons.Contains(target.Thing.def))
            {
                return false;
            }
            return p.CanReserve(target, maxPawns, stackCount, layer, ignoreOtherReservations);
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            int tick = Find.TickManager.TicksGame;
            if (actualJob == null || lastVerifiedJobTick != tick || Find.TickManager.Paused)
            {
                actualJob = null;
                IBillGiver billGiver = thing as IBillGiver;
                if (billGiver != null && ThingIsUsableBillGiver(thing) && billGiver.BillStack.AnyShouldDoNow && billGiver.UsableForBillsAfterFueling())
                {
                    LocalTargetInfo target = thing;
                    if (pawn.CanReserve(target, 1, -1, null, forced) && !thing.IsBurning() && !thing.IsForbidden(pawn)) //basic desk availabilty
                    {
                        if (IsRangeClear(thing)) //check is shooting area is clear if it exists.
                        {
                            billGiver.BillStack.RemoveIncompletableBills();
                            foreach (Bill bill in RelevantBills(thing, pawn))
                            {
                                if (ValidateChosenWeapons(bill, pawn, billGiver)) //check bill filter
                                {
                                    actualJob = StartBillJob(pawn, billGiver, bill);
                                    lastVerifiedJobTick = tick;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return actualJob;
        }

        protected virtual bool IsRangeClear(Thing target)
        {
            CompShootingArea comp = target.TryGetComp<CompShootingArea>();
            if (comp == null) return true;
            var check = ShootingRangeUtility.AreaClear(comp.RangeArea, target.Map);
            if (check.Accepted) return true;
            if (!JobFailReason.HaveReason) JobFailReason.Is(check.Reason);
            return false;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            IEnumerable<ThingDef> knownWeapons = pawn.TryGetComp<CompKnowledge>()?.knownWeapons;
            if (knownWeapons != null)
            {
                IEnumerable<ThingDef> available = unlocked.hardWeapons;
                IEnumerable<ThingDef> studyMaterial = available.Except(knownWeapons);
                return !studyMaterial.Any();
            }
            return true;
        }

        protected virtual IEnumerable<ThingDef> StudyWeapons(Bill bill, Pawn pawn)
        {
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            IEnumerable<ThingDef> known = techComp.knownWeapons;
            IEnumerable<ThingDef> craftable = techComp.craftableWeapons;
            IEnumerable<ThingDef> allowed = unlocked.hardWeapons.Concat(craftable);
            IEnumerable<ThingDef> chosen = bill.ingredientFilter.AllowedThingDefs;
            IEnumerable<ThingDef> viable = chosen.Intersect(allowed).Except(known);
            IEnumerable<ThingDef> unavailable = chosen.Except(viable);
            if (viable.EnumerableNullOrEmpty() && !unavailable.EnumerableNullOrEmpty())
            {
                string thoseWeapons = "ThoseWeapons".Translate();
                string listing = (unavailable.EnumerableCount() < 10) ? unavailable.Select(x => x.label).ToStringSafeEnumerable() : thoseWeapons;
                JobFailReason.Is("MissingRequirementToLearnWeapon".Translate(pawn, listing));
            }
            return viable;
        }

        protected Job StartBillJob(Pawn pawn, IBillGiver giver, Bill bill)
        {
            IntRange range = (IntRange)rangeInfo.GetValue(this);
            if (Find.TickManager.TicksGame >= bill.nextTickToSearchForIngredients + range.RandomInRange || FloatMenuMakerMap.makingFor == pawn)
            {
                bill.nextTickToSearchForIngredients = 0;
                if (bill.ShouldDoNow() && bill.PawnAllowedToStartAnew(pawn))
                {
                    Job result = TryStartNewDoBillJob(pawn, bill, giver);
                    chosenIngThings.Clear();
                    return result;
                }
            }
            chosenIngThings.Clear();
            return null;
        }

        protected virtual Job TryStartNewDoBillJob(Pawn pawn, Bill bill, IBillGiver giver)
        {
            Job job = WorkGiverUtility.HaulStuffOffBillGiverJob(pawn, giver, null);
            if (job != null)
            {
                return job;
            }
            Job job2 = new Job(TechJobDefOf.TrainWeapon, (Thing)giver);
            if (chosenIngThings.Any()) //to accommodate PractiseWeapon, which uses no ingredient.
            {
                job2.targetQueueB = new List<LocalTargetInfo>(chosenIngThings.Count);
                job2.countQueue = new List<int>(chosenIngThings.Count);
                job2.targetQueueB.Add(chosenIngThings[0].Thing);
                job2.countQueue.Add(chosenIngThings[0].Count);
            }
            job2.haulMode = HaulMode.ToCellNonStorage;
            job2.bill = bill;
            return job2;
        }

        protected virtual bool ValidateChosenWeapons(Bill bill, Pawn pawn, IBillGiver giver)
        {
            if (TryFindBestBillIngredients(bill, pawn, giver as Thing, chosenIngThings, missingIngredients))
            {
                var studyWeapons = StudyWeapons(bill, pawn);
                chosenIngThings.RemoveAll(x => !studyWeapons.Contains(x.Thing.def));
                if (chosenIngThings.Any())
                {
                    if (!JobFailReason.HaveReason) JobFailReason.Is("NoWeaponToLearn".Translate(pawn), null);
                    return studyWeapons.Any();
                }
            }
            if (!JobFailReason.HaveReason) JobFailReason.Is("NoWeaponsFoundToLearn".Translate(pawn), null);
            if (FloatMenuMakerMap.makingFor != pawn) bill.nextTickToSearchForIngredients = Find.TickManager.TicksGame;
            return false;
        }
    }
}