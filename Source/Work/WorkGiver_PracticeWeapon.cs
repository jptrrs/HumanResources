using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace HumanResources
{
    internal class WorkGiver_PracticeWeapon : WorkGiver_LearnWeapon
    {
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            //Log.Message(pawn + " is looking for a practice job...");
            Building_WorkTable Target = t as Building_WorkTable;
            if (Target != null)
            {
                if (!CheckJobOnThing(pawn, t, forced))
                {
                    return false;
                }
                foreach (Bill bill in RelevantBills(Target, pawn))
                {
                    return ValidateChosenWeapons(bill, pawn);
                }
                return false;
            }
            //Log.Message("case 4");
            return false;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            IEnumerable<ThingDef> knownWeapons = pawn.TryGetComp<CompKnowledge>()?.knownWeapons;
            return knownWeapons == null || !knownWeapons.Any();
        }

        protected override IEnumerable<ThingDef> StudyWeapons(Bill bill, Pawn pawn)
        {
            IEnumerable<ThingDef> knownWeapons = pawn.TryGetComp<CompKnowledge>().knownWeapons;
            IEnumerable<ThingDef> chosen = bill.ingredientFilter.AllowedThingDefs;
            return chosen.Intersect(knownWeapons);
        }

        private bool ValidateChosenWeapons(Bill bill, Pawn pawn)
        {
            IEnumerable<ThingDef> knownWeapons = pawn.TryGetComp<CompKnowledge>().knownWeapons;
            var studyWeapons = bill.ingredientFilter.AllowedThingDefs.Intersect(knownWeapons);
            bool result = studyWeapons.Any();
            if (!JobFailReason.HaveReason && !result) JobFailReason.Is("NoWeaponToLearn".Translate(pawn), null);
            return result;
        }
    }
}