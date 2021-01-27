using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace HumanResources
{
    internal class WorkGiver_PracticeWeapon : WorkGiver_LearnWeapon
    {
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

        protected override bool ValidateChosenWeapons(Bill bill, Pawn pawn, IBillGiver giver)
        {
            bool result = pawn.equipment.Primary != null && bill.ingredientFilter.AllowedThingDefs.Contains(pawn.equipment.Primary.def);
            if (!JobFailReason.HaveReason && !result) JobFailReason.Is("NoWeaponEquipped".Translate(pawn), null);
            return result;
        }
    }
}