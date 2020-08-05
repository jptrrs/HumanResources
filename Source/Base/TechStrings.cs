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
            headerResearch = DefDatabase<WorkGiverDef>.GetNamed("Research").verb.CapitalizeFirst();
    }
}
