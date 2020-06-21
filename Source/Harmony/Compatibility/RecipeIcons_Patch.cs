using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace HumanResources
{
    class RecipeIcons_Patch
    {
        private static Type patchType = AccessTools.TypeByName("RecipeTooltip");
        private static MethodInfo FindRecipeInfo = AccessTools.Method(patchType, "FindRecipe", new Type[] { typeof(FloatMenuOption) });

        public static void Execute(Harmony instance)
        {
            instance.Patch(AccessTools.Method(patchType, "ShowAt"),
                new HarmonyMethod(typeof(RecipeIcons_Patch), nameof(ShowAt_Prefix)), null, null);
        }

        public static bool ShowAt_Prefix(object __instance, FloatMenuOption option)
        {
            RecipeDef recipe = (RecipeDef)FindRecipeInfo.Invoke(__instance, new object[] { option });
            if (recipe != null && recipe.defName.EndsWith("Tech"))
            {
                return false;
            }
            return true;
        }
    }
}
