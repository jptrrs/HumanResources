using HarmonyLib;
using Verse;

namespace HumanResources
{
    //If a book is removed from a book shelf, forget corresponding tech.
    [HarmonyPatch(typeof(ThingOwner), "NotifyRemoved")]
    public static class ThingOwner_NotifyRemoved
    {
        private static Thing last;
        public static void Postfix(Thing item, IThingHolder ___owner)
        {
            if (item != last && ___owner is Building_BookStore bookStore)
            {
                bookStore.CheckBookOut(item);
                last = item;
            }
            else if (item == last) last = null;
        }
    }
}
