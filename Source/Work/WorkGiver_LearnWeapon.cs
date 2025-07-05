using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;
using JPTools;

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
                    if (pawn.CanReserve(target, 1, -1, null, forced) && !thing.IsBurning() && !thing.IsForbidden(pawn))
                    {
                        if (IsRangeClear(thing))
                        {
                            billGiver.BillStack.RemoveIncompletableBills();
                            foreach (Bill bill in RelevantBills(thing, pawn))
                            {
                                if (TryFindBestBillIngredients(bill, pawn, billGiver as Thing, chosenIngThings, missingIngredients))
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
            Log.Message($"[HumanResources] {pawn.Name} is skipping learning weapons.");
            return true;
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

        protected virtual new bool TryFindBestBillIngredients(Bill bill, Pawn pawn, Thing billGiver, List<ThingCount> chosen, List<IngredientCount> missingIngredients)
        {
            byte reason = 0;
            List<ThingDef> unavailable = new List<ThingDef>();
            bool result = WorkGiver_DoBill.TryFindBestIngredientsHelper((Thing t) => ValidateChosenWeapons(pawn, t, bill, ref reason, ref unavailable), (List<Thing> foundThings) => WorkGiver_DoBill.TryFindBestBillIngredientsInSet(foundThings, bill, chosen, WorkGiver_DoBill.GetBillGiverRootCell(billGiver, pawn), billGiver is Pawn, missingIngredients), bill.recipe.ingredients, pawn, billGiver, chosen, bill.ingredientSearchRadius);
            if (!JobFailReason.HaveReason && reason > 0) Feedback(pawn, reason, unavailable);
            return result;
        }

        protected virtual bool ValidateChosenWeapons(Pawn pawn, Thing t, Bill bill, ref byte failReason, ref List<ThingDef> unavailable)
        {
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            if (!WorkGiver_DoBill.IsUsableIngredient(t, bill) || CompBiocodable.IsBiocoded(t))
            {
                if (failReason < 1) failReason = 1;
                return false; // no weapon found => NoWeaponsFoundToLearn
            }
            if (techComp.knownWeapons.Contains(t.def))
            {
                if (failReason < 2) failReason = 2;
                return false; // weapon found, but already proficient => NoWeaponToLearn
            }
            if (!unlocked.hardWeapons.Concat(techComp.craftableWeapons).Contains(t.def))
            {
                if (failReason < 3) failReason = 3;
                unavailable.Add(t.def);
                return false; // weapon found, not proficient, but corresponding tech unavailable => MissingRequirementToLearnWeapon
            }
            failReason = 0;
            return true; //found relevant weapon, allowed to proceed.
        }

        protected virtual void Feedback(Pawn pawn, byte reason, List<ThingDef> unavailable)
        {
            switch (reason)
            {
                case 1:
                    JobFailReason.Is("NoWeaponsFoundToLearn".Translate(pawn), null);
                    break;
                case 2:
                    JobFailReason.Is("NoWeaponToLearn".Translate(pawn), null);
                    break;
                case 3:
                    string listing = (unavailable.Count() < 10) ? unavailable.Select(Utility.DefLabelFailSafe).ToStringSafeEnumerable() : (string)"ThoseWeapons".Translate();
                    JobFailReason.Is("MissingRequirementToLearnWeapon".Translate(pawn, listing), null);
                    break;
            }
        }
    }
}