using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    [StaticConstructorOnStartup]
    public static class TechStrings
    {
        public static string
            headerWrite = TechWorkDefOf.DocumentTech.verb.CapitalizeFirst(),
            headerRead = TechWorkDefOf.LearnTech.verb.CapitalizeFirst(),
            headerResearch = DefDatabase<WorkGiverDef>.GetNamed("Research").verb.CapitalizeFirst();

        public static string GetTask(Pawn pawn, ResearchProjectDef tech)
        {
            bool known = tech.IsKnownBy(pawn);
            bool completed = tech.IsFinished;
            return known ? headerWrite : completed ? headerRead : headerResearch;
        }
    }
}
