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
        //protected string RecipeName = "TrainWeapon";

        private List<ThingCount> chosenIngThings = new List<ThingCount>();
        //private string RangedSuffix = "Shooting";
        //private string MeleeSuffix = "Melee";

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            //Log.Message(pawn + " is looking for a training job...");
            Building_WorkTable Target = t as Building_WorkTable;
            if (Target != null)
            {
                if (!CheckJobOnThing(pawn, t, forced) && RelevantBills(t/*, RecipeName*/).Count() > 0)
                {
                    //Log.Message("...no job on target.");
                    return false;
                }
                IEnumerable<ThingDef> knownWeapons = pawn.GetComp<CompKnowledge>().knownWeapons;
                foreach (Bill bill in RelevantBills(Target/*, RecipeName*/))
                {
                    if (knownWeapons.Intersect(bill.ingredientFilter.AllowedThingDefs).Count() > 0) return base.HasJobOnThing(pawn, t, forced);//true;
                }
                JobFailReason.Is("NoWeaponToLearn".Translate(pawn), null);
                return false;
            }
            //Log.Message("case 4");
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
                    foreach (Bill bill in RelevantBills(thing/*, RecipeName*/))
                    {
                        return StartBillJob(pawn, billGiver);
                    }
                }
            }
            return null;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            IEnumerable<ThingDef> knownWeapons = pawn.GetComp<CompKnowledge>().knownWeapons;
            IEnumerable<ThingDef> available = ModBaseHumanResources.unlocked.weapons;
            IEnumerable<ThingDef> studyMaterial = available.Except(knownWeapons);
            bool flag = studyMaterial.Count() > 0;
            //if (!flag) Log.Message(pawn + " skipped training. Available: " + available.ToList().Count() + ", studyMaterial: " + studyMaterial.Count());
            return !flag;
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
                    //
                    if (Find.TickManager.TicksGame >= bill.lastIngredientSearchFailTicks + range.RandomInRange || FloatMenuMakerMap.makingFor == pawn)
                    {
                        bill.lastIngredientSearchFailTicks = 0;
                        if (bill.ShouldDoNow())
                        {
                            if (bill.PawnAllowedToStartAnew(pawn))
                            {
                                //reflection info
                                MethodInfo BestIngredientsInfo = AccessTools.Method(typeof(WorkGiver_DoBill), "TryFindBestBillIngredients");
                                FieldInfo MissingMaterialsTranslatedInfo = AccessTools.Field(typeof(WorkGiver_DoBill), "MissingMaterialsTranslated");
                                //
                                chosenIngThings.RemoveAll(x => pawn.GetComp<CompKnowledge>().knownWeapons.Contains(x.Thing.def));
                                if ((bool)BestIngredientsInfo.Invoke(this, new object[] { bill, pawn, giver, chosenIngThings }))
                                {
                                    //Log.Message("...weapon found, chosen ingredients: " + chosenIngThings.Select(x=>x.Thing).ToStringSafeEnumerable());
                                    Job result = TryStartNewDoBillJob(pawn, bill, giver);
                                    chosenIngThings.Clear();
                                    return result;
                                }
                                if (FloatMenuMakerMap.makingFor != pawn)
                                {
                                    //Log.Message("...float menu maker case");
                                    bill.lastIngredientSearchFailTicks = Find.TickManager.TicksGame;
                                }
                                else
                                {
                                    //Log.Message("...missing materials");
                                    JobFailReason.Is((string)MissingMaterialsTranslatedInfo.GetValue(this), bill.Label);
                                }
                                chosenIngThings.Clear();
                            }
                        }
                    }
                }
            }
            Log.Message("...job failed.");
            chosenIngThings.Clear();
            return null;
        }

        private Job TryStartNewDoBillJob(Pawn pawn, Bill bill, IBillGiver giver)
        {
            //string suffix = chosenIngThings.Any(x => x.Thing.def.IsRangedWeapon) ? RangedSuffix : MeleeSuffix;
            //string jobName = RecipeName + suffix;
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
    }
}