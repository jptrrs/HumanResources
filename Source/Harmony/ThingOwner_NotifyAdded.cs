using HarmonyLib;
using Verse;

namespace HumanResources
{
    //If a book is added to a book shelf, discover corresponding tech.
    [HarmonyPatch(typeof(ThingOwner), "NotifyAdded")]
    public static class ThingOwner_NotifyAdded
    {
        private static Thing last;
        public static void Postfix(Thing item, IThingHolder ___owner)
        {
            if (item != last && item.IsValidBook() && ___owner is Building_BookStore bookStore && !bookStore.borrowed.Contains(item))
            {
                bookStore.CheckBookIn(item);
                last = item;
            }
            else if (item == last) last = null;
        }
    }
}
