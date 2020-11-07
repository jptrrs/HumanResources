using HarmonyLib;
using RimWorld;

namespace HumanResources
{
    //Supress the "Need research project" alert
    [HarmonyPatch(typeof(Alert_NeedResearchProject), "GetReport")]
    public static class Alert_NeedResearchProject_GetReport
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}
