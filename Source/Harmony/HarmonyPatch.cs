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
        public static Harmony instance = null;
        public static bool CurrentTech;
        public static bool FutureTech;
        public static bool WeaponTrainingSelection;
        public static bool Ball;
        public static Type ResearchProjectDef_Extensions_Type = AccessTools.TypeByName("FluffyResearchTree.ResearchProjectDef_Extensions");
        public static bool ResearchPal = false;

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
                ResearchPal = true;
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

            //Material Filter patch
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing == "KamiKatze.MaterialFilter"))
            {
                Log.Message("[HumanResources] Material Filter detected! Patching...");
                MaterialFilter_Patch.Execute(Instance);
            }

            //Recipe icons patch
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing == "automatic.recipeicons"))
            {
                Log.Message("[HumanResources] Recipe Icons detected! Patching...");
                RecipeIcons_Patch.Execute(Instance);
            }
        }
        public static bool CheckKnownWeapons(Pawn pawn, Thing thing)
        {
            var knownWeapons = pawn.TryGetComp<CompKnowledge>()?.knownWeapons;
            bool result = false;
            if (!knownWeapons.EnumerableNullOrEmpty()) result = knownWeapons.Contains(thing.def);
            return result;
        }

        public static void InitNewGame_Prefix()
        {
            Find.FactionManager.OfPlayer.def.startingResearchTags.Clear();
            Log.Message("[HumanResources] Starting a new game, player faction has been stripped of all starting research.");
        }
    }
}
