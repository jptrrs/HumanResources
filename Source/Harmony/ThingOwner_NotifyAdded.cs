using HarmonyLib;
using System;
using Verse;

namespace HumanResources
{
    //If a book is added to book shelf, discover corresponding tech.
    [HarmonyPatch(typeof(ThingOwner), "NotifyAdded")]
    public static class ThingOwner_NotifyAdded
    {
        private static bool Act = false;

        public static void Postfix(Thing item, IThingHolder ___owner)
        {
            if (Act && ___owner is Building_BookStore bookStore && item.Stuff != null && item.Stuff.IsWithinCategory(TechDefOf.Knowledge))
            {
                ResearchProjectDef project = ModBaseHumanResources.unlocked.techByStuff[item.Stuff];
                project.CarefullyFinishProject(bookStore);
                bookStore.CompStorageGraphic.UpdateGraphics();
                ModBaseHumanResources.unlocked.libraryFreeSpace--;
                Act = false;
            }
        }
        public static void Prefix(Thing item, IThingHolder ___owner)
        {
            if (___owner is Building_BookStore) Act = true;
        }
    }
}
