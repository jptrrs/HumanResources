using HarmonyLib;
using System.Linq;
using Verse;

namespace HumanResources
{
    //Tweaks to ingredients visibility on knowledge recipes, 2/3
    [HarmonyPatch(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow))]
    public static class ThingFilterUI_DoThingFilterConfigWindow
    {
        public static void Prefix(ThingFilter parentFilter, int openMask)
        {
            if (parentFilter != null && parentFilter.AllowedDefCount > 0 && parentFilter.AllowedThingDefs.All(x => x.IsWithinCategory(DefDatabase<ThingCategoryDef>.GetNamed("Knowledge"))))
            {
                openMask = 4;
                HarmonyPatches.Ball = true;
            }
        }

        public static void Postfix()
        {
            HarmonyPatches.Ball = false;
        }
    }
}
