using System.Linq;
using UnityEngine;
using Verse;

namespace HumanResources
{
    public class PlaceWorker_ShootingRange : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            if (def.hasInteractionCell) GenDraw.DrawFieldEdges(ShootingRangeUtility.RangeArea(def, center, rot).ToList());
        }

        public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            return ShootingRangeUtility.AreaClear(ShootingRangeUtility.RangeArea(def as ThingDef, center, rot), map);
            //        foreach (IntVec3 c in ShootingRangeUtility.RangeArea(def as ThingDef,center,rot))
            //        {
            //if (!c.Standable(map)) return "ShootingRangeObstructed".Translate();
            //        }
            //        return true;
        }
    }
}
