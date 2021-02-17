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
            base.DeSpawn(mode);
        }

        public void AuditArchive()
        {
            foreach (ResearchProjectDef tech in unlocked.TechsArchived.Keys)
            {
                Log.Message($"Auditing {tech}");
                tech.Ejected(this, false);
            }
        }
    }
}
