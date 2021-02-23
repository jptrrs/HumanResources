using RimWorld.Planet;
using Verse;

namespace HumanResources
{
    public class TechDatabase : WorldComponent
    {
        public TechDatabase(World world) : base(world)
        {
        }
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref ModBaseHumanResources.unlocked._techsArchived, "techsArchived", LookMode.Def, LookMode.Value);
        }
    }
}
