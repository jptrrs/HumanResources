using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace HumanResources
{
    public static class Bill_Extension
    {
        public static IEnumerable<ResearchProjectDef> SelectedTech(this Bill bill)
        {
            IEnumerable<ThingDef> availableThings = bill.ingredientFilter.AllowedThingDefs;
            IEnumerable<string> availableNames = availableThings.Select(x => x.defName.Substring(x.defName.IndexOf(@"_") + 1));
            IEnumerable<ResearchProjectDef> availableTech = DefDatabase<ResearchProjectDef>.AllDefs.Where(x => availableNames.Contains(x.defName));
            return availableTech; 
        }

    }
}
