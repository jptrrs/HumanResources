using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    public class TechDatabase : WorldComponent
    {
        public Dictionary<ResearchProjectDef, BackupState> techsArchived = new Dictionary<ResearchProjectDef, BackupState>();

        public TechDatabase(World world) : base(world) { }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref techsArchived, "techsArchived", LookMode.Def, LookMode.Value);
        }
    }
}
