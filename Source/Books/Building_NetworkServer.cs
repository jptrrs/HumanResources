﻿using RimWorld;
using System;
using System.Text;
using Verse;

namespace HumanResources
{
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
            int count = ModBaseHumanResources.unlocked.networkDatabase.Count;
            StringBuilder s = new StringBuilder();
            string baseStr = base.GetInspectString();
            if (baseStr != "") s.AppendLine(baseStr);
            if (count == 0) s.AppendLine("BookStoreEmpty".Translate());
            else s.AppendLine("BookStoreCapacity".Translate(count, ModBaseHumanResources.unlocked.discoveredCount ));
            return s.ToString().TrimEndNewlines();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            CompStorageGraphic?.UpdateGraphics();
            base.SpawnSetup(map, respawningAfterLoad);
        }
    }
}
