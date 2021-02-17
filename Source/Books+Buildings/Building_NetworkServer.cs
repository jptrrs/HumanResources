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

        public override string GetInspectString()
        {
            int count = unlocked.TechsArchived.Count;
            StringBuilder s = new StringBuilder();
            string baseStr = base.GetInspectString();
            if (baseStr != "") s.AppendLine(baseStr);
            if (count == 0) s.AppendLine("BookStoreEmpty".Translate());
            else s.AppendLine("BookStoreCapacity".Translate(count, unlocked.discoveredCount ));
            return s.ToString().TrimEndNewlines();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            CompStorageGraphic?.UpdateGraphics();
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override void DeSpawn(DestroyMode mode)
        {
            base.DeSpawn(mode);
            bool backup = false;
            foreach (Map m in Find.Maps)
            {
                if (m.listerBuildings.ColonistsHaveBuilding(def))
                {
                    backup = true;
                    break;
                }
            }
            if (!backup) AuditArchive();
        }

        public void AuditArchive()
        {
            List<ResearchProjectDef> currentArchive = unlocked.TechsArchived.Keys.ToList();
            foreach (ResearchProjectDef tech in currentArchive)
            {
                tech.Ejected(this, false);
            }
        }
    }
}
