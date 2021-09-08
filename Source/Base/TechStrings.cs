using RimWorld;
using Verse;

namespace HumanResources
{
    [StaticConstructorOnStartup]
    public static class TechStrings
    {
        public static string
            headerWrite = TechWorkDefOf.DocumentTech.verb.CapitalizeFirst(),
            headerRead = TechWorkDefOf.LearnTech.verb.CapitalizeFirst(),
            headerResearch = WorkTypeDefOf.Research.labelShort.CapitalizeFirst(),
            gerundResearch = WorkTypeDefOf.Research.gerundLabel,
            bookTraderTag = "TechBook";

        public static string GetTask(Pawn pawn, ResearchProjectDef tech)
        {
            bool known = tech.IsKnownBy(pawn);
            bool completed = tech.IsFinished;
            return known ? headerWrite : completed ? headerRead : headerResearch;
        }
    }
}
