using HarmonyLib;
using System;
using Verse;

namespace HumanResources
{
    //Tweaks visibility of technical books category, 2/2
    [HarmonyPatch(typeof(Listing_TreeThingFilter))]
    public static class Listing_TreeThingFilter_Visible
    {
        static bool flag = false;

        //[HarmonyPatch("Visible", new Type[] { typeof(ThingDef) })]
        //public static void Postfix(ThingDef td, ThingFilter ___parentFilter, ref bool __result)
        //{
        //    if (flag)
        //    { 
        //        __result = td.IsWithinCategory(TechDefOf.Knowledge) && ___parentFilter.Allows(td);
        //        Log.Message($"HR: {td} visibility: {__result}");
        //    };
        //}

        [HarmonyPatch("Visible", new Type[] { typeof(TreeNode_ThingCategory) })]
        public static void Postfix(TreeNode_ThingCategory node, ThingFilter ___parentFilter, ref bool __result)
        {
            if (node.catDef == TechDefOf.Knowledge || node.catDef.parent == TechDefOf.Knowledge)
            {
                __result = false;
                string test = node.catDef != null ? "ok" : "bad";
                Log.Message($"HR: node {node.Label}, catDef is {test}, descendents: {node.catDef.DescendantThingDefs.ToStringSafeEnumerable()}");
            }

        }

        [HarmonyPatch("DoCategoryChildren")]//, new Type[] { typeof(TreeNode_ThingCategory), typeof(int), typeof(int), typeof(Map), typeof(bool)})]
        public static void Prefix(TreeNode_ThingCategory node, ThingFilter ___parentFilter)
        {
            flag = node.catDef == TechDefOf.Knowledge;
        }
    }
}
d