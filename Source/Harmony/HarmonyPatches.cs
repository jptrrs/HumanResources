using HarmonyLib;
using System.Linq;
using Verse;

namespace HumanResources
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        public static Harmony instance = null;

        public static bool
            PrisonLabor = false,
            VFEM = false,
            RunSpecialCases = false,
            SemiRandom = false,
            VisibleBooksCategory = false;

        public static ResearchTreeVersion ResearchTreeBase = ResearchTreeVersion.Fluffy;

        public static Harmony Instance
        {
            get
            {
                if (instance == null)
                    instance = new Harmony("JPT.HumanResources");
                return instance;
            }
        }

        public static string ResearchTreeNamespaceRoot
        {
            get
            {
                switch (ResearchTreeBase)
                {
                    case ResearchTreeVersion.Fluffy:
                    case ResearchTreeVersion.Mlie:
                        return "Fluffy.ResearchTree";
                    case ResearchTreeVersion.NotFood:
                    case ResearchTreeVersion.VinaLx:
                    case ResearchTreeVersion.Owlchemist:
                        return "ResearchPal";
                    default: return string.Empty;
                }
            }
        }

        static HarmonyPatches()
        {
            //Harmony.DEBUG = true;
            Instance.PatchAll();

            // integration with ResearchTree / ResearchPal / whatever kids use these days
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("fluffy.researchtree")))
            {
                Log.Message("[HumanResources] Deriving from ResearchTree.");
                ResearchTree_Patches.Execute(Instance, "FluffyResearchTree");
            }
            else if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("Mlie.ResearchTree")))
            {
                Log.Message("[HumanResources] Deriving from ResearchTree (Mlie version).");
                ResearchTree_Patches.Execute(Instance, "FluffyResearchTree"); //Hypotesis
            }
            else if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("notfood.ResearchPal")))
            {
                Log.Message("[HumanResources] Deriving from ResearchPal.");
                ResearchTreeBase = ResearchTreeVersion.NotFood;
                ResearchTree_Patches.Execute(Instance, "ResearchPal");
            }
            else if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("VinaLx.ResearchPalForked")))
            {
                Log.Message("[HumanResources] Deriving from ResearchPal - Forked (VinaLx version).");
                ResearchTreeBase = ResearchTreeVersion.VinaLx;
                ResearchTree_Patches.Execute(Instance, "ResearchPal");
            }
            else if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("Owlchemist.ResearchPowl"))) // Note for the next sucker who comes in here - CASE SENSITIVE. -VS7
            {
                Log.Message("[HumanResources] Deriving from ResearchPowl (Owlchemist version).");
                ResearchTreeBase = ResearchTreeVersion.Owlchemist;
                ResearchTree_Patches.Execute(Instance, "ResearchPowl");
            }
            else if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("Maruf61.ResearchPalForkd")))
            {
                Log.Message("[HumanResources] Deriving from ResearchPal - Forkd (Maruf61 version).");
                ResearchTreeBase = ResearchTreeVersion.VinaLx;
                ResearchTree_Patches.Execute(Instance, "ResearchPal"); //Hypotesis
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

            //Material Filter patch
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("KamiKatze.MaterialFilter")))
            {
                Log.Message("[HumanResources] Material Filter detected! Adapting...");
                MaterialFilter_Patch.Execute(Instance);
            }

            //Recipe icons patch
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

            //VFEM integration
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("OskarPotocki.VFE.Mechanoid")))
            {
                Log.Message("[HumanResources] Vanilla Factions Expanded - Mechanoids detected! Integrating...");
                VFEM_Patches.Execute(Instance);
                VFEM = true;
            }

            //Pick Up and Haul integration
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("Mehni.PickUpAndHaul")))
            {
                Log.Message("[HumanResources] Pick Up And Haul detected! Adapting...");
                PUAH_Patch.Execute(Instance);
            }

            //Semi-Random integration
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("CaptainMuscles.SemiRandomResearch")))
            {
                Log.Message("[HumanResources] Semi-Random Research detected! Integrating...");
                SemiRandom_Patch.Execute(Instance);
                SemiRandom = true;
            }

            //Fluffy Breakdowns integration
            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("Fluffy.FluffyBreakdowns") || x.PackageIdPlayerFacing.StartsWith("theeyeofbrows.fluffybreakdowns")))
            {
                Log.Message("[HumanResources] Fluffy Breakdowns detected! Integrating...");
                FluffyBreakdowns_Patches.Execute(Instance);
            }

            //Provisions for specific research projects
            if (LoadedModManager.RunningModsListForReading.Any(x =>
            x.PackageIdPlayerFacing.StartsWith("loconeko.roadsoftherim") ||
            x.PackageIdPlayerFacing.StartsWith("Mlie.RoadsOfTheRim") ||
            x.PackageIdPlayerFacing.StartsWith("fluffy.backuppower") ||
            x.PackageIdPlayerFacing.StartsWith("Fluffy.FluffyBreakdowns") ||
            x.PackageIdPlayerFacing.StartsWith("Ogliss.AdMech.Armoury") ||
            x.PackageIdPlayerFacing.StartsWith("VanillaExpanded.VFEArt")))
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
            return !knownWeapons.EnumerableNullOrEmpty() && knownWeapons.Contains(def);
        }

        public static void InitNewGame_Prefix()
        {
            Find.FactionManager.OfPlayer.def.startingResearchTags.Clear();
            Log.Message("[HumanResources] Starting a new game, player faction has been stripped of all starting research.");
        }
    }
}
