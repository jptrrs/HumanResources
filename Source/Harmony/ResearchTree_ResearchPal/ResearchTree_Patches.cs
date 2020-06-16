using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(GetUnlockDefsAndDescs)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetRecipesUnlocked"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(GetRecipesUnlocked)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetThingsUnlocked"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(GetThingsUnlocked)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetPlantsUnlocked"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(GetPlantsUnlocked)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:Ancestors"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Ancestors)))).Patch();

            //ResearchNode
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchNode:BuildingPresent", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(BuildingPresent)))).Patch();
            instance.Patch(AccessTools.Method(modName + ".ResearchNode:BuildingPresent", new Type[] { typeof(ResearchProjectDef) }),
                null, new HarmonyMethod(typeof(ResearchTree_Patches), nameof(BuildingPresent_Postfix)), null);
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchNode:MissingFacilities", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(MissingFacilities)))).Patch();

            //Def_Extensions
            instance.CreateReversePatcher(AccessTools.Method(modName + ".Def_Extensions:DrawColouredIcon"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(DrawColouredIcon)))).Patch();
        }

        public static List<Pair<Def, string>> GetUnlockDefsAndDescs(ResearchProjectDef research, bool dedupe = true) { throw stubMsg; }
        public static bool BuildingPresent(ResearchProjectDef research) { throw stubMsg; }
        public static List<ThingDef> MissingFacilities(ResearchProjectDef research) { throw stubMsg; }
        public static void DrawColouredIcon(this Def def, Rect canvas) { throw stubMsg; }
        public static IEnumerable<RecipeDef> GetRecipesUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static IEnumerable<ThingDef> GetThingsUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static IEnumerable<ThingDef> GetPlantsUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static List<ResearchProjectDef> Ancestors(this ResearchProjectDef research) { throw stubMsg; }

        private static FieldInfo buildingPresentCacheInfo = AccessTools.Field(ResearchNodeType(), "_buildingPresentCache");
        //private static MethodInfo buildingPresentCacheSetMethod => buildingPresentCacheInfo.FieldType.GetMethod("set_Item", new Type[] { typeof(ResearchProjectDef), typeof(bool) });

        public static void BuildingPresent_Postfix(ResearchProjectDef research, ref bool __result)
        {
            bool flag = false;
            if (!__result && research.requiredResearchBuilding != null)
            {
                flag = Find.Maps.SelectMany((Map map) => map.listerBuildings.allBuildingsColonist).OfType<Building_WorkTable>().Any((Building_WorkTable b) => research.Alt_CanBeResearchedAt(b));
            }
            if (flag)
            {
                flag = research.Ancestors().All(new Func<ResearchProjectDef, bool>(BuildingPresent));
            }
            string test = buildingPresentCacheInfo != null ? "ok" : "bad";
            Log.Warning("DEBUG buildingPresentCacheInfo is " + test);
            //FieldInfo buildingPresentCacheInfo = AccessTools.Field(ResearchNodeType(), "_buildingPresentCache");
            MethodInfo buildingPresentCacheSetMethod = buildingPresentCacheInfo.FieldType.GetMethod("set_Item");
            //object dict = buildingPresentCacheInfo.GetValue(ResearchNodeType());
            //buildingPresentCacheSetMethod.Invoke(dict, new object[] { research, flag });
            
            string test2 = buildingPresentCacheSetMethod != null ? "ok" : "bad";
            Log.Warning("DEBUG buildingPresentCacheSetMethod is " + test2);


            __result = flag;
        }
    }
}


