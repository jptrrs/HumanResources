using RimWorld;
using Verse;

namespace HumanResources
{
    public class ShootingRangeUtility
    {
        public static CellRect RangeArea(ThingDef def, IntVec3 center, Rot4 rot)
        {
            int range = -def.interactionCellOffset.z;
            IntVec2 area = new IntVec2(def.Size.x, range - def.size.z);
            return GenAdj.OccupiedRect(FindRangeCenter(center, rot, range), rot, area);
        }

        protected static IntVec3 FindRangeCenter(IntVec3 target, Rot4 rot, int range)
        {
            range /= 2;
            var face = rot.AsSpectateSide;
            IntVec3 adjust = new IntVec3(0, 0, 0);
            switch (face)
            {
                case SpectateRectSide.Up:
                    adjust = new IntVec3(0, 0, -range);
                    break;
                case SpectateRectSide.Down:
                    adjust = new IntVec3(0, 0, range);
                    break;
                case SpectateRectSide.Right:
                    adjust = new IntVec3(-range, 0, 0);
                    break;
                case SpectateRectSide.Left:
                    adjust = new IntVec3(range, 0, 0);
                    break;
                default:
                    break;
            }
            return target + adjust;
        }

        public static AcceptanceReport AreaClear(CellRect area, Map map)
        {
            foreach (IntVec3 c in area)
            {
                if (!c.Standable(map)) return "ShootingRangeObstructed".Translate();
            }
            return true;
        }
    }
}
