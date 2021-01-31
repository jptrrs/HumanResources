using Verse;

namespace HumanResources
{
    //Borrowed from Jecrell's RimWriter
    public class CompProperties_StorageGraphic : CompProperties
    {
        public GraphicData graphicEmpty = null;
        public GraphicData graphicSparse = null;
        public GraphicData graphicFull = null;

        public int countSparseThreshold = 5;
        public int countFullCapacity = 10;

        public CompProperties_StorageGraphic()
        {
            compClass = typeof(CompStorageGraphic);
        }
    }
}
