using Verse;

namespace HumanResources
{
    using static ModBaseHumanResources;

    //Borrowed from Jecrell's RimWriter
    public class CompStorageGraphic : ThingComp
    {
        private Graphic cachedGraphic = null;
        public CompProperties_StorageGraphic Props
        {
            get
            {
                return (CompProperties_StorageGraphic)props;
            }
        }

        public void UpdateGraphics()
        {
            cachedGraphic = null;
        }

        public Graphic CurStorageGraphic
        {
            get
            {
                if (cachedGraphic == null)
                {
                    int capacity = 0;
                    int sparseThreshold = 0;
                    int count = 0;

                    if (parent.TryGetInnerInteractableThingOwner() is ThingOwner thingOwner && parent is Building_BookStore shelf)
                    {
                        count = thingOwner.Count;
                        capacity = shelf.dynamicCapacity;
                        sparseThreshold = Props.countSparseThreshold;
                    }
                    else if (parent.def == TechDefOf.NetworkServer)
                    {
                        count = unlocked.TechsArchived.Count;
                        capacity = (int)(TechTracker.totalBooks * 0.80f); //discoveredCount;
                        sparseThreshold = capacity / 4;
                    }
                    if (count >= capacity)
                    {
                        cachedGraphic = Props.graphicFull.GraphicColoredFor(parent);
                    }
                    else if (count >= sparseThreshold)
                    {
                        cachedGraphic = Props.graphicSparse.GraphicColoredFor(parent);
                    }
                    else
                    {
                        cachedGraphic = Props.graphicEmpty.GraphicColoredFor(parent);
                    }
                }
                return cachedGraphic;
            }
        }
    }
}
