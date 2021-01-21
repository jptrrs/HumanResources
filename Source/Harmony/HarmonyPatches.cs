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

        public static bool
            ResearchPal = false,
            PrisonLabor = false,
            RunSpecialCases = false,
            VisibleBooksCategory = false;

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
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("fluffy.researchtree")))
            {
                Log.Message("[HumanResources] Deriving from ResearchTree.");
                ResearchTree_Patches.Execute(Instance, "FluffyResearchTree");
            }
            else if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("notfood.ResearchPal")))
            {
                Log.Message("[HumanResources] Deriving from ResearchPal.");
                ResearchTree_Patches.Execute(Instance, "ResearchPal");
                ResearchPal = true;
            }
            else if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("VinaLx.ResearchPalForked")))
            {
                Log.Message("[HumanResources] Deriving from ResearchPal - Forked.");
                ResearchTree_Patches.Execute(Instance, "ResearchPal", true);
                ResearchPal = true;
            }
            else
            {
                Log.Error("[HumanResources] Could not find ResearchTree nor ResearchPal. Human Resources will not work!");
            }

            //Go Explore! integration
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("Albion.GoExplore")))
            {
                Log.Message("[HumanResources] Go Explore detected! Integrating...");
                GoExplore_Patches.Execute(Instance);
            }

            //Material Filter patch - OBSOLETE?
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("KamiKatze.MaterialFilter")))
            {
                Log.Message("[HumanResources] Material Filter detected! Adapting...");
                MaterialFilter_Patch.Execute(Instance);
            }

            //Recipe icons patch - OBSOLETE?
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("automatic.recipeicons")))
            {
                Log.Message("[HumanResources] Recipe Icons detected! Adapting...");
                RecipeIcons_Patch.Execute(Instance);
            }

            //Simple Sidearms integration
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("PeteTimesSix.SimpleSidearms")))
            {
                Log.Message("[HumanResources] Simple Sidearms detected! Integrating...");
                SimpleSidearms_Patches.Execute(Instance);
            }

            //Prison Labor integration
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("avius.prisonlabor")))
            {
                Log.Message("[HumanResources] Prison Labor detected! Integrating...");
                PrisonLabor = true;
            }

            //Children, School and Learning integration
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("Dylan.CSL")))
            {
                Log.Message("[HumanResources] Children, School and Learning detected! Integrating...");
                ChildrenSchoolLearning_Patch.Execute(Instance);
            }

            //Dual Wield integration
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("Roolo.DualWield")))
            {
                Log.Message("[HumanResources] Dual Wield detected! Integrating...");
                DualWield_Patch.Execute(Instance);
            }

            //Hospitality integration
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("orion.hospitality")))
            {
                Log.Message("[HumanResources] Hospitality detected! Integrating...");
                Hospitality_Patches.Execute(Instance);
            }

            //Provisions for specific research projects
            if (LoadedModManager.RunningModsListForReading.Any(x => 
            x.PackageIdPlayerFacing.StartsWith("loconeko.roadsoftherim") || 
            x.PackageIdPlayerFacing.StartsWith("mlie.roadsoftherim") ||
            x.PackageIdPlayerFacing.StartsWith("fluffy.backuppower") || 
            x.PackageIdPlayerFacing.StartsWith("fluffy.fluffybreakdowns")))
            {
                RunSpecialCases = true;
            }
        }

        public static bool CheckKnownWeapons(Pawn pawn, Thing thing)
        {
            return CheckKnownWeapons(pawn, thing.def);
        }

        public static bool CheckKnownWeapons(Pawn pawn, ThingWithComps thing)
        {
            return CheckKnownWeapons(pawn, thing.def);
        }

        public static bool CheckKnownWeapons(Pawn pawn, ThingDef def)
        {

            var knownWeapons = pawn.TryGetComp<CompKnowledge>()?.knownWeapons;
            bool result = false;
            if (!knownWeapons.EnumerableNullOrEmpty()) result = knownWeapons.Contains(def);
            return result;
        }

        public static void InitNewGame_Prefix()
        {
            Find.FactionManager.OfPlayer.def.startingResearchTags.Clear();
            Log.Message("[HumanResources] Starting a new game, player faction has been stripped of all starting research.");
        }
    }
}
