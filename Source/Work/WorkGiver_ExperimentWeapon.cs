using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;

namespace HumanResources
{
    internal class WorkGiver_ExperimentWeapon : WorkGiver_LearnWeapon
    {
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            //Log.Message(pawn + " is looking for an experimenting job...");
            Building_WorkTable Target = t as Building_WorkTable;
            if (Target != null)
            {
                if (!CheckJobOnThing(pawn, t, forced) && RelevantBills(t, pawn).Any())
                {
                    //Log.Message("...no job on target.");
                    return false;
                }
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
                            return StartBillJob(pawn, billGiver, bill);
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

        protected override IEnumerable<ThingDef> StudyWeapons(Bill bill, Pawn pawn)
        {
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            IEnumerable<ThingDef> known = techComp.knownWeapons;
            IEnumerable<ThingDef> craftable = techComp.knownTechs.SelectMany(x => x.UnlockedWeapons());
            IEnumerable<ThingDef> available = ModBaseHumanResources.unlocked.weapons.Concat(craftable);
            IEnumerable<ThingDef> chosen = bill.ingredientFilter.AllowedThingDefs;
            IEnumerable<ThingDef> feared = techComp.fearedWeapons;
            IEnumerable<ThingDef> unavailable = chosen.Except(known).Where(x => !available.Contains(x));
            //if (!unavailable.EnumerableNullOrEmpty())
            //{
            //    string thoseWeapons = "ThoseWeapons".Translate();
            //    string listing = (unavailable.EnumerableCount() < 10) ? unavailable.Select(x => x.label).ToStringSafeEnumerable() : thoseWeapons;
            //    JobFailReason.Is("MissingRequirementToLearnWeapon".Translate(pawn, listing));
            //}
            var result = feared.EnumerableNullOrEmpty() ? chosen.Intersect(unavailable) : chosen.Intersect(unavailable).Except(feared);
            return result;
        }

        private bool ValidateChosenWeapons(Bill bill, Pawn pawn, IBillGiver giver)
        {
            if ((bool)BestIngredientsInfo.Invoke(this, new object[] { bill, pawn, giver, chosenIngThings }))
            {
                var studyWeapons = StudyWeapons(bill, pawn);
                chosenIngThings.RemoveAll(x => !studyWeapons.Contains(x.Thing.def));
                if (chosenIngThings.Any())
                {
                    if (!JobFailReason.HaveReason) JobFailReason.Is("NoWeaponToLearn".Translate(pawn), null);
                    return studyWeapons.Any();
                }
                var traumas = pawn.TryGetComp<CompKnowledge>().fearedWeapons;
                if (!traumas.NullOrEmpty() && chosenIngThings.All(x => traumas.Contains(x.Thing.def))) JobFailReason.Is("FearedWeapon".Translate(pawn));
            }
            if (!JobFailReason.HaveReason) JobFailReason.Is("NoWeaponsFoundToLearn".Translate(pawn), null);
            if (FloatMenuMakerMap.makingFor != pawn) bill.lastIngredientSearchFailTicks = Find.TickManager.TicksGame;
            return false;
        }
    }
}