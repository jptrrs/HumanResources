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
            IEnumerable<ThingDef> knownWeapons = pawn.TryGetComp<CompKnowledge>()?.KnownWeaponsCached;
            if (knownWeapons != null)
            {
                IEnumerable<ThingDef> available = unlocked.weapons;
                IEnumerable<ThingDef> studyMaterial = available.Except(knownWeapons);
                return !studyMaterial.Any();
            }
            return true;
        }

        protected override IEnumerable<ThingDef> StudyWeapons(Bill bill, Pawn pawn)
        {
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            IEnumerable<ThingDef> known = techComp.KnownWeaponsCached;
            IEnumerable<ThingDef> craftable = techComp.craftableWeapons;
            IEnumerable<ThingDef> available = unlocked.weapons.Concat(craftable);
            IEnumerable<ThingDef> chosen = bill.ingredientFilter.AllowedThingDefs;
            IEnumerable<ThingDef> feared = techComp.fearedWeapons;
            IEnumerable<ThingDef> unavailable = chosen.Except(known).Where(x => !available.Contains(x));
            var result = feared.EnumerableNullOrEmpty() ? chosen.Intersect(unavailable) : chosen.Intersect(unavailable).Except(feared);
            return result;
        }

        protected override bool ValidateChosenWeapons(Bill bill, Pawn pawn, IBillGiver giver)
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