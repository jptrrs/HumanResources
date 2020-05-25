using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HumanResources
{
    public static class ResearchTree_Patches
    {
        private static string ModName = "";
        private static NotImplementedException stubMsg = new NotImplementedException("ResearchTree reverse patch");
        public static Type ResearchNodeType() => AccessTools.TypeByName(ModName + ".ResearchNode");
        public static Type AssetsType() => AccessTools.TypeByName(ModName + ".Assets");

        public static void Execute(Harmony instance, string modName)
        {
            ModName = modName;

            //ResearchProjectDef_Extensions
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetUnlockDefsAndDescs"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ResearchTree_Patches.GetUnlockDefsAndDescs)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetRecipesUnlocked"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ResearchTree_Patches.GetRecipesUnlocked)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetThingsUnlocked"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ResearchTree_Patches.GetThingsUnlocked)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetPlantsUnlocked"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ResearchTree_Patches.GetPlantsUnlocked)))).Patch();

            //ResearchNode
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchNode:BuildingPresent", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ResearchTree_Patches.BuildingPresent)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchNode:MissingFacilities", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ResearchTree_Patches.MissingFacilities)))).Patch();

            //Def_Extensions
            instance.CreateReversePatcher(AccessTools.Method(modName + ".Def_Extensions:DrawColouredIcon"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ResearchTree_Patches.DrawColouredIcon)))).Patch();
        }

        public static List<Pair<Def, string>> GetUnlockDefsAndDescs(ResearchProjectDef research, bool dedupe = true) { throw stubMsg; }
        public static bool BuildingPresent(ResearchProjectDef research) { throw stubMsg; }
        public static List<ThingDef> MissingFacilities(ResearchProjectDef research) { throw stubMsg; }
        public static void DrawColouredIcon(this Def def, Rect canvas) { throw stubMsg; }
        public static IEnumerable<RecipeDef> GetRecipesUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static IEnumerable<ThingDef> GetThingsUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static IEnumerable<ThingDef> GetPlantsUnlocked(this ResearchProjectDef research) { throw stubMsg; }
    }
}


