using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HumanResources
{
    public static class Extension_Building_WorkTable
    {
        public static bool HasFacility(this Building_WorkTable building, ThingDef facility)
        {
            var comp = building.GetComp<CompAffectedByFacilities>();
            if (comp == null)
                return false;

            if (comp.LinkedFacilitiesListForReading.Select(f => f.def).Contains(facility))
                return true;

            return false;
        }
    }
}
