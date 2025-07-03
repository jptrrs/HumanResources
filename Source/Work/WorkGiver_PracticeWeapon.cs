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

        protected override bool TryFindBestBillIngredients(Bill bill, Pawn pawn, Thing billGiver, List<ThingCount> chosen, List<IngredientCount> missingIngredients)
        {
            bool result = ValidateChosenWeapons(pawn, bill);
            if (!JobFailReason.HaveReason)
            {
                JobFailReason.Is("NoWeaponEquipped".Translate(), null); // Do we need a more detailed feedback message here?
            }
            return result;
        }

        private bool ValidateChosenWeapons(Pawn pawn, Bill bill)
        {
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            Thing equipped = pawn.equipment.Primary;
            return equipped != null && WorkGiver_DoBill.IsUsableIngredient(equipped, bill) && techComp.knownWeapons.Contains(equipped.def);
        }
    }
}