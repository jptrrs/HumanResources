using HarmonyLib;
using Verse;

namespace HumanResources
{
    //If a book is added to a book shelf, discover corresponding tech.
    [HarmonyPatch(typeof(ThingOwner), "NotifyAdded")]
    public static class ThingOwner_NotifyAdded
    {
        private static bool Act = false; //Only way I found to prevent the game from notifying twice for the same event.
        public static void Postfix(Thing item, IThingHolder ___owner)
        {
            if (Act && ___owner is Building_BookStore bookStore)
            {
                bookStore.CheckBookIn(item);
                Act = false;
            }
        }
        public static void Prefix(IThingHolder ___owner)
        {
            Act = ___owner is Building_BookStore;
        }
    }
}
