using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;
using HarmonyLib;

namespace HumanResources
{
    internal class WorkGiver_LearnWeapon : WorkGiver_Knowledge
    {
        public List<ThingCount> chosenIngThings = new List<ThingCount>();
        private MethodInfo BestIngredientsInfo = AccessTools.Method(typeof(WorkGiver_DoBill), "TryFindBestBillIngredients");

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            //Log.Message(pawn + " is looking for a training job...");
            Building_WorkTable Target = t as Building_WorkTable;
            if (Target != null)
            {
                if (!CheckJobOnThing(pawn, t, forced) && RelevantBills(t, pawn).Any())
                {
                    //Log.Message("...no job on target.");
                    return false;
                }
                IEnumerable<ThingDef> knownWeapons = pawn.TryGetComp<CompKnowledge>().knownWeapons;
                foreach (Bill bill in RelevantBills(Target, pawn))
                {
                    return ValidateChosenWeapons(bill, pawn, t as IBillGiver);
                }
                return false;
            }
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            //Log.Message(pawn + " looking for a job at " + thing);
            IBillGiver billGiver = thing as IBillGiver;
            if (billGiver != null && ThingIsUsableBillGiver(thing) && billGiver.BillStack.AnyShouldDoNow && billGiver.UsableForBillsAfterFueling())
            {
                LocalTargetInfo target = thing;
                if (pawn.CanReserve(target, 1, -1, null, forced) && !thing.IsBurning() && !thing.IsForbidden(pawn))
                {
                    billGiver.BillStack.RemoveIncompletableBills();
                    foreach (Bill bill in RelevantBills(thing, pawn))
                    {
                        if (ValidateChosenWeapons(bill, pawn, billGiver))
                        {
                            return StartBillJob(pawn, billGiver);
                        }
                    }
                }
            }
            return null;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            IEnumerable<ThingDef> knownWeapons = pawn.TryGetComp<CompKnowledge>()?.knownWeapons;
            if (knownWeapons != null)
            {
                IEnumerable<ThingDef> available = ModBaseHumanResources.unlocked.weapons;
                IEnumerable<ThingDef> studyMaterial = available.Except(knownWeapons);
                return !studyMaterial.Any();
            }
            return true;
        }

        private Job StartBillJob(Pawn pawn, IBillGiver giver)
        {
            //Log.Warning(pawn + " is trying to start a training job...");
            for (int i = 0; i < giver.BillStack.Count; i++)
            {
                Bill bill = giver.BillStack[i];
                if (bill.recipe.requiredGiverWorkType == null || bill.recipe.requiredGiverWorkType == def.workType)
                {
                    //reflection info
                    FieldInfo rangeInfo = AccessTools.Field(typeof(WorkGiver_DoBill), "ReCheckFailedBillTicksRange");
                    IntRange range = (IntRange)rangeInfo.GetValue(this);
                    MethodInfo BestIngredientsInfo = AccessTools.Method(typeof(WorkGiver_DoBill), "TryFindBestBillIngredients");
                    //
                    if (Find.TickManager.TicksGame >= bill.lastIngredientSearchFailTicks + range.RandomInRange || FloatMenuMakerMap.makingFor == pawn)
                    {
                        bill.lastIngredientSearchFailTicks = 0;
                        if (bill.ShouldDoNow() && bill.PawnAllowedToStartAnew(pawn))
                        {
                            //if ((bool)BestIngredientsInfo.Invoke(this, new object[] { bill, pawn, giver, chosenIngThings }))
                            //{
                                //Log.Message("...weapon found, chosen ingredients: " + chosenIngThings.Select(x => x.Thing).ToStringSafeEnumerable());
                                chosenIngThings.RemoveAll(x => !StudyWeapons(bill, pawn).Contains(x.Thing.def));
                                if (chosenIngThings.Any())
                                {
                                    Job result = TryStartNewDoBillJob(pawn, bill, giver);
                                    chosenIngThings.Clear();
                                    return result;
                                }
                                else if (!JobFailReason.HaveReason) JobFailReason.Is("NoWeaponToLearn".Translate(pawn));
                            //}
                            if (FloatMenuMakerMap.makingFor != pawn)
                            {
                                //Log.Message("...float menu maker case");
                                bill.lastIngredientSearchFailTicks = Find.TickManager.TicksGame;
                            }
                            else
                            {
                                //reflection info
                                FieldInfo MissingMaterialsTranslatedInfo = AccessTools.Field(typeof(WorkGiver_DoBill), "MissingMaterialsTranslated");
                                //
                                //Log.Message("...missing materials");
                                JobFailReason.Is((string)MissingMaterialsTranslatedInfo.GetValue(this), bill.Label);
                            }
                            chosenIngThings.Clear();
                        }
                    }
                }
            }
            //Log.Message("...job failed.");
            chosenIngThings.Clear();
            return null;
        }

        protected virtual IEnumerable<ThingDef> StudyWeapons(Bill bill, Pawn pawn)
        {
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            IEnumerable<ThingDef> known = techComp.knownWeapons;
            IEnumerable<ThingDef> craftable = techComp.knownTechs.SelectMany(x => x.UnlockedWeapons());
            IEnumerable<ThingDef> available = ModBaseHumanResources.unlocked.weapons.Concat(craftable);
            IEnumerable<ThingDef> chosen = bill.ingredientFilter.AllowedThingDefs;
            IEnumerable<ThingDef> unavailable = chosen.Except(known).Where(x => !available.Contains(x));
            if (!unavailable.EnumerableNullOrEmpty())
            {
                string thoseWeapons = "ThoseWeapons".Translate();
                string listing = (unavailable.EnumerableCount() < 10) ? unavailable.Select(x => x.label).ToStringSafeEnumerable() : thoseWeapons;
                JobFailReason.Is("MissingRequirementToLearnWeapon".Translate(pawn, listing));
            }
            var result = chosen.Intersect(available).Except(known);
            return result;
        }

        private Job TryStartNewDoBillJob(Pawn pawn, Bill bill, IBillGiver giver)
        {
            Job job = WorkGiverUtility.HaulStuffOffBillGiverJob(pawn, giver, null);
            if (job != null)
            {
                return job;
            }
            Job job2 = new Job(DefDatabase<JobDef>.GetNamed("TrainWeapon"), (Thing)giver);
            job2.targetQueueB = new List<LocalTargetInfo>(chosenIngThings.Count);
            job2.countQueue = new List<int>(chosenIngThings.Count);
            for (int i = 0; i < chosenIngThings.Count; i++)
            {
                job2.targetQueueB.Add(chosenIngThings[i].Thing);
                job2.countQueue.Add(chosenIngThings[i].Count);
            }
            job2.haulMode = HaulMode.ToCellNonStorage;
            job2.bill = bill;
            return job2;
        }

        private bool ValidateChosenWeapons(Bill bill, Pawn pawn, IBillGiver giver)
        {
            if ((bool)BestIngredientsInfo.Invoke(this, new object[] { bill, pawn, giver, chosenIngThings }))
            {
                var studyWeapons = StudyWeapons(bill, pawn);
                //Log.Warning("ValidateChosenWeapons for " + pawn + "...");
                //Log.Warning("..." + chosenIngThings.Count() + " chosen ingredients:  " + chosenIngThings.Select(x => x.Thing).ToStringSafeEnumerable());
                //Log.Warning("..." + studyWeapons.EnumerableCount() + " weapons to study: " + studyWeapons.Select(x => x.defName).ToStringSafeEnumerable());
                chosenIngThings.RemoveAll(x => !studyWeapons.Contains(x.Thing.def));
                if (chosenIngThings.Any())
                {
                    //Log.Message("ValidateChosenWeapons for " + pawn + ": proceeed!");
                    if (!JobFailReason.HaveReason) JobFailReason.Is("NoWeaponToLearn".Translate(pawn), null);
                    return studyWeapons.Any();
                }
            }
            if (!JobFailReason.HaveReason) JobFailReason.Is("NoWeaponsFoundToLearn".Translate(pawn), null);
            return false;
        }
    }
}