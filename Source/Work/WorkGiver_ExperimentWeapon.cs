using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace HumanResources
{
    using static ModBaseHumanResources;

    internal class WorkGiver_ExperimentWeapon : WorkGiver_LearnWeapon
    {
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

        protected override bool ValidateChosenWeapons(Pawn pawn, Thing t, Bill bill, ref byte failReason, ref List<ThingDef> unavailable)
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
            if (unlocked.hardWeapons.Concat(techComp.craftableWeapons).Contains(t.def))
            {
                if (failReason < 3) failReason = 3;
                return false; // weapon found, not proficient, but corresponding tech is available => NoWeaponsFoundToLearn 
            }
            if (techComp.fearedWeapons != null && techComp.fearedWeapons.Contains(t.def))
            {
                if (failReason < 4) failReason = 4;
                return false; // weapon found, not proficient, unknown tech, but pawn traumatized by it => FearedWeapon
            }
            failReason = 0;
            return true; //found relevant weapon, allowed to proceed.
        }

        protected override void Feedback(Pawn pawn, byte reason, List<ThingDef> unavailable)
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
                    JobFailReason.Is("NoWeaponsFoundToLearn".Translate(pawn), null); //(TO DO: new msg, opposite of "MissingRequirementToLearnWeapon")
                    break;
                case 4:
                    JobFailReason.Is("FearedWeapon".Translate(pawn));
                    break;
            }
        }
    }
}