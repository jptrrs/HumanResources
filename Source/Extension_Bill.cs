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
            IEnumerable<ThingDef> availableThings = bill.ingredientFilter.AllowedThingDefs;
            //IEnumerable<string> availableNames = availableThings.Select(x => x.defName.Substring(x.defName.IndexOf(@"_") + 1));
            //IEnumerable<ResearchProjectDef> availableTech = DefDatabase<ResearchProjectDef>.AllDefs.Where(x => availableNames.Contains(x.defName));
            IEnumerable<ResearchProjectDef> availableTech = ModBaseHumanResources.unlocked.techByStuff.Where(x => availableThings.Contains(x.Key)).Select(x => x.Value);
            return availableTech; 
        }

    }
}
