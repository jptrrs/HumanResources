using HarmonyLib;
using Verse;

namespace HumanResources
{
    //Prevents the player faction tech level from affecting displayed costs for techs (as if it was always Industrial, 100% cost).
    [HarmonyPatch(typeof(ResearchProjectDef), "CostApparent", MethodType.Getter)]
    public static class ResearchProjectDef_CostApparent
    {
        public static void Postfix(float ___baseCost, ref float __result)
        {
            __result = ___baseCost;
        }
    }
}
