using Verse;

namespace HumanResources
{
    public class CompShootingArea : ThingComp
    {
        private bool pending = true;
        private CellRect _rangeArea;
        public CellRect RangeArea
        {
            get
            {
                if (pending)
                {
                    _rangeArea = ShootingRangeUtility.RangeArea(parent.def, parent.Position, parent.Rotation);
                    pending = false;
                }
                return _rangeArea;
            }
        }
    }
}
