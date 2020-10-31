using HarmonyLib;
using System.Linq;
using Verse;

namespace HumanResources
{
    //Tweaks visibility of technical books category, 1/2
    [HarmonyPatch(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow))]
    public static class ThingFilterUI_DoThingFilterConfigWindow
    {
        public static void Prefix(ThingFilter parentFilter, int openMask)
        {
            if (parentFilter != null && parentFilter.AllowedDefCount > 0 && parentFilter.AllowedThingDefs.All(x => x.IsWithinCategory(TechDefOf.Knowledge)))
            {
                openMask = 2;
                HarmonyPatches.VisibleBooksCategory = true;
            }
        }

        public static void Postfix()
        {
            HarmonyPatches.VisibleBooksCategory = false;
        }
    }
}
