using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HumanResources
{
    public static class Extension_Bill
    {
        public static IEnumerable<ResearchProjectDef> SelectedTech(this Bill bill)
        {
            IEnumerable<ThingDef> allowedThings = bill.ingredientFilter.AllowedThingDefs;
            IEnumerable<ResearchProjectDef> allowedTechs = allowedThings.Where(x => ModBaseHumanResources.unlocked.techByStuff[x] != null).Select(x => ModBaseHumanResources.unlocked.techByStuff[x]);
            return allowedTechs;
        }

        public static bool IsResearch(this Bill bill)
        {
            return bill.recipe.requiredGiverWorkType == WorkTypeDefOf.Research;
        }

        public static bool UsesKnowledge(this Bill bill)
        {
            if (bill.recipe.fixedIngredientFilter.AnyAllowedDef != null)
            {
                return bill.recipe.fixedIngredientFilter.AnyAllowedDef.IsWithinCategory(DefDatabase<ThingCategoryDef>.GetNamed("Knowledge"));
            }
            return false;
        }

        public static bool IsWeaponsTraining(this Bill bill)
        {
            return bill.recipe.defName.StartsWith("TrainWeapon");
        }
    }
}
