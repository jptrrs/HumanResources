using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace HumanResources
{
    public class Building_NetworkServer : Building_BookStore, IStoreSettingsParent, IHaulDestination, IThingHolder
    {
        public new bool retrievable = false;

        public override int dynamicCapacity
        {
            get
            {
                if (dynamicCapacityInt == 0)
                {
                    dynamicCapacityInt = ModBaseHumanResources.unlocked.total;
                }
                return dynamicCapacityInt;
            }
        }
       
        public override bool Accepts(Thing thing)
        {
            return false;
        }

        public override void CheckTechIn(ResearchProjectDef tech)
        {
            if (!tech.IsFinished) tech.CarefullyFinishProject(this);
            CompStorageGraphic.UpdateGraphics();
            ModBaseHumanResources.unlocked.networkDatabase.AddDistinct(tech);
        }

        public override void DeSpawn(DestroyMode mode)
        {
            if (innerContainer.Count > 0)
            {
                foreach (Thing t in innerContainer)
                {
                    ModBaseHumanResources.unlocked.techByStuff[t.Stuff].EjectTech(this);
                }
            }
            base.DeSpawn(mode);
        }
    }
}
