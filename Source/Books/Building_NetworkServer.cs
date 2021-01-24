using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace HumanResources
{
    //Borrowed from Jecrell's RimWriter
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

        public override void DeSpawn(DestroyMode mode)
        {
            ModBaseHumanResources.unlocked.libraryFreeSpace -= dynamicCapacity - innerContainer.Count;
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
