using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;

namespace HumanResources
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static Type patchType = typeof(HarmonyPatches);
        public static Harmony instance = null;
        public static bool CurrentTech;
        public static bool FutureTech;
        public static bool WeaponTrainingSelection;
        public static bool Ball;
        public static Type ResearchProjectDef_Extensions_Type = AccessTools.TypeByName("FluffyResearchTree.ResearchProjectDef_Extensions");

        public static Harmony Instance
        {
            get
            {
                if (instance == null)
                    instance = new Harmony("JPT.HumanResources");
                return instance;
            }
        }

        static HarmonyPatches()
        {
            //Harmony.DEBUG = true;
            Instance.PatchAll();

            //ResearchTree/ResearchPal integration
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing == "fluffy.researchtree"))
            {
                Log.Message("[HumanResources] Deriving from ResearchTree.");
                ResearchTree_Patches.Execute(Instance, "FluffyResearchTree");
            }
            else if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing == "notfood.ResearchPal"))
            {
                Log.Message("[HumanResources] Deriving from ResearchPal.");
                ResearchTree_Patches.Execute(Instance, "ResearchPal");
            }
            else
            {
                Log.Error("[HumanResources] Could not find ResearchTree nor ResearchPal. Human Resources will not work!");
            }

            //Go Explore! integration
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing == "Albion.GoExplore"))
            {
                Log.Message("[HumanResources] Go Explore detected! Integrating...");
                GoExplore_Patches.Execute(Instance);
            }
        }

        public static void TryFindBestBetterNonSlotGroupStorageFor_Postfix(Thing t, IHaulDestination haulDestination, ref bool __result)
        {
            if (__result && t.def.defName == "TechBook") Log.Warning("TryFindBestBetterNonSlotGroupStorageFor found " + haulDestination);
        }

        public static void TryFindBestBetterStoreCellFor_Postfix(Thing t, Pawn carrier, Map map, StoragePriority currentPriority, Faction faction, ref IntVec3 foundCell, ref bool __result, bool needAccurateResult = true)
        {
            if (!__result && carrier.CurJobDef != null)
            {
                Log.Message(carrier.CurJobDef.label);
            }
            
        }

        public static bool CheckKnownWeapons(Pawn pawn, Thing thing)
        {
            var knownWeapons = pawn.TryGetComp<CompKnowledge>()?.knownWeapons;
            bool result = true;
            if (!knownWeapons.EnumerableNullOrEmpty()) result = knownWeapons.Contains(thing.def);
            else result = false;
            return result;
        }

        public static void InitNewGame_Prefix()
        {
            Find.FactionManager.OfPlayer.def.startingResearchTags.Clear();
            Log.Message("[HumanResources] Starting a new game, player faction has been stripped of all starting research.");
        }
    }
}
