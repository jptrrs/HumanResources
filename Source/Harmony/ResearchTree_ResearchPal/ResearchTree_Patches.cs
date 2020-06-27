using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
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
        public static Type TreeType() => AccessTools.TypeByName(ModName + ".Tree");
        public static Type NodeType() => AccessTools.TypeByName(ModName + ".Node");
        public static Type MainTabType() => AccessTools.TypeByName(ModName + ".MainTabWindow_ResearchTree");

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
            instance.CreateReversePatcher(AccessTools.Method(ResearchNodeType(), "BuildingPresent", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(BuildingPresent)))).Patch();
            instance.Patch(AccessTools.Method(ResearchNodeType(), "BuildingPresent", new Type[] { typeof(ResearchProjectDef) }),
                null, new HarmonyMethod(typeof(ResearchTree_Patches), nameof(BuildingPresent_Postfix)));
            instance.CreateReversePatcher(AccessTools.Method(ResearchNodeType(), "MissingFacilities", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(MissingFacilities)))).Patch();
            instance.Patch(AccessTools.Method(ResearchNodeType(), "MissingFacilities", new Type[] { typeof(ResearchProjectDef) }),
                null, new HarmonyMethod(typeof(ResearchTree_Patches), nameof(MissingFacilities_Postfix)));
            instance.Patch(AccessTools.Constructor(ResearchNodeType(), new Type[] { typeof(ResearchProjectDef) }),
                null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ResearchNode_Postfix))));

            _buildingPresentCache = AccessTools.Field(ResearchNodeType(), "_buildingPresentCache").GetValue(ResearchNodeType()) as Dictionary<ResearchProjectDef, bool>;
            _missingFacilitiesCache = AccessTools.Field(ResearchNodeType(), "_missingFacilitiesCache").GetValue(ResearchNodeType()) as Dictionary<ResearchProjectDef, List<ThingDef>>;

            //Def_Extensions
            instance.CreateReversePatcher(AccessTools.Method(modName + ".Def_Extensions:DrawColouredIcon"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(DrawColouredIcon)))).Patch();

            //MainTabWindow_ResearchTree
            instance.Patch(AccessTools.Method(MainTabType(), "DoWindowContents"),
                null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(DoWindowContents_Postfix))));
            if (modName != "ResearchPal")
            {
                instance.Patch(AccessTools.Method(MainTabType(), "Notify_TreeInitialized"),
                    null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TreeInitialized_Postfix))));
            }

            //Tree
            instance.Patch(AccessTools.Method(TreeType(), "PopulateNodes"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(PopulateNodes_Prefix))),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(PopulateNodes_Postfix))));
            if (modName == "ResearchPal")
            {
                instance.Patch(AccessTools.Method(TreeType(), "Initialize"),
                    null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TreeInitialized_Postfix))));
            }
        }

        public static List<Pair<Def, string>> GetUnlockDefsAndDescs(ResearchProjectDef research, bool dedupe = true) { throw stubMsg; }
        public static bool BuildingPresent(ResearchProjectDef research) { throw stubMsg; }
        public static List<ThingDef> MissingFacilities(ResearchProjectDef research) { throw stubMsg; }
        public static void DrawColouredIcon(this Def def, Rect canvas) { throw stubMsg; }
        public static IEnumerable<RecipeDef> GetRecipesUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static IEnumerable<ThingDef> GetThingsUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static IEnumerable<ThingDef> GetPlantsUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static List<ResearchProjectDef> Ancestors(this ResearchProjectDef research) { throw stubMsg; }

        private static Dictionary<ResearchProjectDef, bool> _buildingPresentCache;
        private static Dictionary<ResearchProjectDef, List<ThingDef>> _missingFacilitiesCache;

        public static void BuildingPresent_Postfix(ResearchProjectDef research, ref bool __result)
        {
            if (__result == true)
                return;

            bool flag = false;
            if (research.requiredResearchBuilding != null)
            {
                flag = Find.Maps.SelectMany((Map map) => map.listerBuildings.allBuildingsColonist).OfType<Building_WorkTable>().Any((Building_WorkTable b) => research.Alt_CanBeResearchedAt(b));
            }
            if (flag)
            {
                flag = research.Ancestors().All(new Func<ResearchProjectDef, bool>(BuildingPresent));
            }

            if (flag)
            {
                _buildingPresentCache.Remove(research);
                _buildingPresentCache.Add(research, flag);
            }

            __result = flag;
        }

        public static void MissingFacilities_Postfix(ref ResearchProjectDef research, ref List<ThingDef> __result)
        {
            if (__result.NullOrEmpty()) return;
            List<ThingDef> missing;

            // get list of all researches required before this
            var thisAndPrerequisites = research.Ancestors().Where(rpd => !rpd.IsFinished).ToList();
            thisAndPrerequisites.Add(research);

            // get list of all available research benches
            var availableBenches = Find.Maps.SelectMany(map => map.listerBuildings.allBuildingsColonist).OfType<Building_WorkTable>();
            var availableBenchDefs = availableBenches.Select(b => b.def).Distinct();
            missing = new List<ThingDef>();

            // check each for prerequisites
            foreach (var rpd in thisAndPrerequisites)
            {
                if (rpd.requiredResearchBuilding == null) continue;
                if (!availableBenchDefs.Contains(rpd.requiredResearchBuilding)) missing.Add(rpd.requiredResearchBuilding);
                if (rpd.requiredResearchFacilities.NullOrEmpty()) continue;
                foreach (var facility in rpd.requiredResearchFacilities)
                {
                    if (!availableBenches.Any(b => b.HasFacility(facility))) missing.Add(facility);
                }
            }

            // add to cache
            missing = missing.Distinct().ToList();
            if (missing != _missingFacilitiesCache[research])
            {
                _missingFacilitiesCache.Remove(research);
                _missingFacilitiesCache.Add(research, missing);
            }

            __result = missing;
        }

        public static ResearchProjectDef subjectToShow;
        private static MethodInfo MainTabCenterOnInfo => AccessTools.Method(MainTabType(), "CenterOn", new Type[] { NodeType() });
        private static PropertyInfo TreeNodesListInfo => AccessTools.Property(TreeType(), "Nodes");

        public static void DoWindowContents_Postfix(object __instance)
        {
            if (subjectToShow != null && treeReady)
            {
                int idx = treeNodesResearchCache.IndexOf(subjectToShow);
                MainTabCenterOnInfo.Invoke(__instance, new object[] { treeNodesList[idx] });
                subjectToShow = null;
            }
        }

        private static IList treeNodesList;

        private static bool treeReady = false;

        private static List<ResearchProjectDef> treeNodesResearchCache = new List<ResearchProjectDef>();

        public static void TreeInitialized_Postfix(object __instance)
        {
            treeNodesList = (IList)TreeNodesListInfo.GetValue(__instance);
            treeReady = !treeNodesResearchCache.NullOrEmpty();
        }

        private static bool populating = false;

        private static void PopulateNodes_Prefix()
        {
            populating = true;
        }

        private static void PopulateNodes_Postfix()
        {
            populating = false;
        }

        private static void ResearchNode_Postfix(ResearchProjectDef research)
        {
            if (populating)
            {
                treeNodesResearchCache.Add(research);
            }
        }
    }
}


