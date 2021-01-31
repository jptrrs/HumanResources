using HarmonyLib;
using Verse;

namespace HumanResources
{
    //If a book is removed from a book shelf, discover corresponding tech.
    [HarmonyPatch(typeof(ThingOwner), "NotifyRemoved")]
    public static class ThingOwner_NotifyRemoved
    {
        public static void Postfix(Thing item, IThingHolder ___owner)
        {
            if (___owner is Building_BookStore bookStore)
            {
                bookStore.CheckBookOut(item);
            }
        }
    }
}
