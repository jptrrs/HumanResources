using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace HumanResources
{
    using static ModBaseHumanResources;
    public class Building_NetworkServer : Building
    {
        protected CompStorageGraphic compStorageGraphic = null;
        protected List<ResearchProjectDef> minifiedBackup;

        public CompStorageGraphic CompStorageGraphic
        {
            get
            {
                if (compStorageGraphic == null)
                {
                    compStorageGraphic = this.TryGetComp<CompStorageGraphic>();
                }
                return compStorageGraphic;
            }
        }

        public override Graphic Graphic
        {
            get
            {
                if (CompStorageGraphic?.CurStorageGraphic != null)
                {
                    return CompStorageGraphic.CurStorageGraphic;
                }
                return base.Graphic;
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref minifiedBackup, "minifiedBackup");
            base.ExposeData();
        }

        public override string GetInspectString()
        {
            int count = unlocked.TechsArchived.Count;
            StringBuilder s = new StringBuilder();
            string baseStr = base.GetInspectString();
            if (baseStr != "") s.AppendLine(baseStr);
            if (count == 0) s.AppendLine("BookStoreEmpty".Translate());
            else s.AppendLine("BookStoreCapacity".Translate(count, unlocked.DiscoveredCount));
            return s.ToString().TrimEndNewlines();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            CompStorageGraphic?.UpdateGraphics();
            if (!minifiedBackup.NullOrEmpty()) ReUpload();
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override void DeSpawn(DestroyMode mode)
        {
            base.DeSpawn(mode);
            minifiedBackup = unlocked.TechsArchived.Keys.ToList();
            if (!minifiedBackup.NullOrEmpty()) AuditArchive();
        }

        public void AuditArchive()
        {
            bool mapBackup = false;
            foreach (Map m in Find.Maps)
            {
                if (m.listerBuildings.ColonistsHaveBuilding(def))
                {
                    mapBackup = true;
                    break;
                }
            }
            if (!mapBackup)
            {
                foreach (ResearchProjectDef tech in minifiedBackup)
                {
                    tech.Ejected(this, false);
                }
            }
        }

        public void ReUpload()
        {
            foreach (ResearchProjectDef tech in minifiedBackup)
            {
                if (!tech.IsOnline())
                {
                    tech.Unlock(this, false);
                }
            }
            minifiedBackup.Clear();
        }
    }
}
