using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HumanResources
{
    public static class Extensions
	{
        //Thing
        public static ResearchProjectDef TryGetTech(this Thing book)
        {
            return (book.Stuff != null && book.Stuff.IsWithinCategory(TechDefOf.Knowledge)) ? ModBaseHumanResources.unlocked.techByStuff[book.Stuff] : null;
        }
	}
} 
