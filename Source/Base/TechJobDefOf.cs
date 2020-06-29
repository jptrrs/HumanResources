using RimWorld;
using Verse;

namespace HumanResources
{
    [DefOf]
    public static class TechJobDefOf
    {
        static TechJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDef));
        }
        public static JobDef LearnTech;
        public static JobDef DocumentTech;
        public static JobDef ResearchTech;
        public static JobDef TrainWeapon;
    }
}
