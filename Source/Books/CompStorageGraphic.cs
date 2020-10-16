using Verse;

namespace HumanResources
{
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
                    if (parent.TryGetInnerInteractableThingOwner() is ThingOwner thingOwner &&
                        thingOwner.Count is int count)
                    {
                        Building_BookStore shelf = parent as Building_BookStore;
                        if (count >= shelf.dynamicCapacity)
                        {
                            cachedGraphic = Props.graphicFull.GraphicColoredFor(parent);
                        }
                        else if (count >= Props.countSparseThreshhold)
                        {
                            cachedGraphic = Props.graphicSparse.GraphicColoredFor(parent);
                        }
                        else
                        {
                            cachedGraphic = Props.graphicEmpty.GraphicColoredFor(parent);
                        }
                    }
                }
                return cachedGraphic;
            }
        }
    }
}
