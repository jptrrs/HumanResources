using HarmonyLib;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.SceneManagement;
using Verse;

namespace HumanResources
{
    //Changed in RW 1.5
    public class ModBaseHumanResources : ModBase
    {
        public static SettingHandle<bool>
            TechPoolTitle,
            TechPoolIncludesStarting,
            TechPoolIncludesTechLevel,
            TechPoolIncludesBackground,
            TechPoolIncludesScenario,
            FreeScenarioWeapons,
            LearnMeleeWeaponsByGroup,
            LearnRangedWeaponsByGroup,
            RequireTrainingForSingleUseWeapons,
            EnableJoyGiver,
            ResearchSpeedTiedToDifficulty,
            StudySpeedTiedToDifficulty,
            FullStartupReport,
            IndividualTechsReport;
        public static FieldInfo ScenPartThingDefInfo = AccessTools.Field(typeof(ScenPart_ThingCount), "thingDef");
        public static List<ThingDef>
            SimpleWeapons = new List<ThingDef>(),
            MountedWeapons = new List<ThingDef>(),
            UniversalCrops = new List<ThingDef>(),
            UniversalWeapons = new List<ThingDef>();
        public static UnlockManager unlocked = new UnlockManager();
        public static FactionWeaponPool WeaponPoolMode;
        private static bool GameJustLoaded = true;

        public ModBaseHumanResources()
        {
            Settings.EntryName = "Human Resources";
        }

        public enum FactionWeaponPool { Both, TechLevel, Scenario }

        public static bool WeaponPoolIncludesScenario => WeaponPoolMode != FactionWeaponPool.TechLevel;
        public static bool WeaponPoolIncludesTechLevel => WeaponPoolMode < FactionWeaponPool.Scenario;

        public override string ModIdentifier
        {
            get
            {
                return "JPT_HumanResources";
            }
        }

        public override void DefsLoaded()
        {
            //1. Preparing settings
            UpdateSettings();

            //2. Adding Tech Tab to Pawns
            foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs.Where(x => x.race?.intelligence == Intelligence.Humanlike))
            {
                if (thing.inspectorTabsResolved == null)
                {
                    thing.inspectorTabsResolved = new List<InspectTabBase>(1);
                }
                thing.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_PawnKnowledge)));
                if (thing.comps == null)
                {
                    thing.comps = new List<CompProperties>(1);
                }
                thing.comps.Add(new CompProperties_Knowledge());
            }
            InspectPaneUtility.Reset();

            //3. Preparing knowledge support infrastructure

            //a. Populating dynamic categories
            Dictionary<ThingCategoryDef, StuffCategoryDef> LowTechCategories = new Dictionary<ThingCategoryDef, StuffCategoryDef>();
            Dictionary<ThingCategoryDef, StuffCategoryDef> HiTechCategories = new Dictionary<ThingCategoryDef, StuffCategoryDef>();
            TechLevel cutoff = TechDefOf.TechDrive.techLevel;
            ThingCategoryDef knowledgeCat = TechDefOf.Knowledge;
            foreach (TechLevel level in Enum.GetValues(typeof(TechLevel)))
            {
                string tag = null;
                if (!IsTechLevelRelevant(level, out tag)) continue;
                ThingCategoryDef thingCat = new ThingCategoryDef
                {
                    defName = tag,
                    label = tag.ToLower(),
                    parent = TechDefOf.Knowledge
                };
                InjectedDefHasher.GiveShortHashToDef(thingCat, typeof(ThingCategoryDef));
                DefDatabase<ThingCategoryDef>.Add(thingCat);
                knowledgeCat.childCategories.Add(thingCat);
                thingCat.ResolveReferences();

                StuffCategoryDef stuffCat = new StuffCategoryDef
                {
                    defName = tag,
                    label = tag.ToLower()
                };
                InjectedDefHasher.GiveShortHashToDef(stuffCat, typeof(StuffCategoryDef));
                DefDatabase<StuffCategoryDef>.Add(stuffCat);

                if (level < cutoff)
                {
                    LowTechCategories.Add(thingCat, stuffCat);
                }
                else HiTechCategories.Add(thingCat, stuffCat);
            }

            //b. Provisions to deal with turrets & artilleries
            List<ThingDef> turretGuns = new List<ThingDef>();
            foreach (var (t, foundGun) in from ThingDef t in DefDatabase<ThingDef>.AllDefs
                                          let foundGun = t.GetTurretGun()
                                          where foundGun != null
                                          select (t, foundGun))
            {
                if (t.IsMannable()) MountedWeapons.Add(foundGun);
                else turretGuns.Add(foundGun);
            }

            //c. Things everyone knows
            UniversalWeapons.AddRange(DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWeapon).Except(turretGuns));
            UniversalCrops.AddRange(DefDatabase<ThingDef>.AllDefs.Where(x => x.plant != null && x.plant.Sowable));

            //d. Minus things unlocked on research
            ThingFilter lateFilter = new ThingFilter();
            ThingDef pending = TechDefOf.TechBook;
            bool stage = true;
            foreach (ResearchProjectDef tech in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                tech.InferSkillBias();
                if (tech.CreateStuff(lateFilter, pending, cutoff) && stage) //on first positive, load the next thing to set defaultStuff for.
                {
                    pending = TechDefOf.TechDrive;
                    stage = false;
                }
                foreach (ThingDef weapon in tech.UnlockedWeapons()) UniversalWeapons.Remove(weapon);
                foreach (ThingDef plant in tech.UnlockedPlants()) UniversalCrops.Remove(plant);
            };

            //e. Also removing atipical weapons"
            List<string> ForbiddenWeaponTags = TechDefOf.HardWeapons.weaponTags;
            UniversalWeapons.RemoveAll(x => SplitSimpleWeapons(x, ForbiddenWeaponTags));
            List<ThingDef> garbage = new List<ThingDef>();
            garbage.Add(TechDefOf.HardWeapons);

            //f. Classifying pawn backstories
            PawnBackgroundUtility.BuildCache();

            //g. Telling humans what's going on
            IEnumerable<ThingDef> codifiedTech = DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWithinCategory(knowledgeCat));
            if (Prefs.LogVerbose || FullStartupReport)
            {
                Log.Message($"[HumanResources] Identified tech levels: (Lo-tech) {LowTechCategories.Keys.Select(x => x.label).ToStringSafeEnumerable()}, (Hi-tech) {HiTechCategories.Keys.Select(x => x.label).ToStringSafeEnumerable()}.");
                Log.Message($"[HumanResources] Codified technologies: {codifiedTech.Select(x => x.label).ToStringSafeEnumerable()}");
                Log.Message($"[HumanResources] Basic crops: {UniversalCrops.ToStringSafeEnumerable()}");
                Log.Message($"[HumanResources] Basic weapons: {UniversalWeapons.ToStringSafeEnumerable()}");
                Log.Message($"[HumanResources] Basic weapons that require training: {SimpleWeapons.ToStringSafeEnumerable()}");
                Log.Warning($"[HumanResources] Basic weapons tags: {SimpleWeapons.Where(x => !x.weaponTags.NullOrEmpty()).SelectMany(x => x.weaponTags).Distinct().ToStringSafeEnumerable()}");
                Log.Message($"[HumanResources] Mounted weapons: {MountedWeapons.ToStringSafeEnumerable()}");
                if (FullStartupReport)
                {
                    Log.Warning("[HumanResources] Backstories classified by TechLevel:");
                    for (int i = 0; i < 8; i++)
                    {
                        TechLevel level = (TechLevel)i;
                        IEnumerable<string> found = PawnBackgroundUtility.TechLevelByBackstory.Where(e => e.Value == level).Select(e => e.Key);
                        if (!found.EnumerableNullOrEmpty())
                        {
                            Log.Message($"- {level.ToString().CapitalizeFirst()} ({found.EnumerableCount()}): {found.ToStringSafeEnumerable()}");
                        }
                    }
                    Log.Warning("[HumanResources] Techs classified by associated skill:");
                    var skills = DefDatabase<SkillDef>.AllDefsListForReading.GetEnumerator();
                    while (skills.MoveNext())
                    {
                        SkillDef skill = skills.Current;
                        IEnumerable<string> found = TechTracker.FindTechs(skill).Select(x => x.Tech.label);
                        Log.Message($"- {skill.LabelCap} ({found.EnumerableCount()}): {found.ToStringSafeEnumerable()}");
                    }
                }
            }
            else Log.Message($"[HumanResources] This is what we know: {codifiedTech.EnumerableCount()} technologies processed, {UniversalCrops.Count()} basic crops, {UniversalWeapons.Count()} basic weapons + {SimpleWeapons.Count()} that require training.");

            //4. Filling gaps on the database

            //a. TechBook dirty trick, but only now this is possible!
            TechDefOf.TechBook.stuffCategories = TechDefOf.UnfinishedTechBook.stuffCategories = LowTechCategories.Values.ToList();
            TechDefOf.TechDrive.stuffCategories = HiTechCategories.Values.ToList();

            //b. Filling main tech category with subcategories
            foreach (ThingDef stuff in lateFilter.AllowedThingDefs.Where(t => !t.thingCategories.NullOrEmpty()))
            {
                foreach (ThingCategoryDef category in stuff.thingCategories)
                {
                    category.childThingDefs.Add(stuff);
                }
            }
            foreach(ThingCategoryDef stuffCat in knowledgeCat.childCategories)
            {
                stuffCat.PostLoad();
            }

            //c. Populating knowledge recipes and book shelves
            List<RecipeDef> LoTechRecipes = new List<RecipeDef>() { TechDefOf.LearnTech, TechDefOf.DocumentTech};
            List<RecipeDef> HiTechRecipes = new List<RecipeDef>() { TechDefOf.LearnTechDigital, TechDefOf.DocumentTechDigital, TechDefOf.ScanBook };
            List<string> LoTechBlackList = HiTechCategories.Keys.Select(x => x.defName).ToList();
            foreach(RecipeDef recipe in LoTechRecipes)
            {
                //if (recipe.fixedIngredientFilter == null) recipe.fixedIngredientFilter = new ThingFilter();
                //if (recipe.defaultIngredientFilter == null) recipe.defaultIngredientFilter = new ThingFilter();
                //recipe.fixedIngredientFilter.categories = LowTechCategories.Values.Select(x => x.defName).ToList();
                recipe.fixedIngredientFilter.disallowedCategories = recipe.defaultIngredientFilter.disallowedCategories = LoTechBlackList;
            }
            foreach (RecipeDef r in LoTechRecipes.Concat(HiTechRecipes))
            {
                r.fixedIngredientFilter.ResolveReferences();
                r.defaultIngredientFilter.ResolveReferences();
            }
            foreach (ThingDef t in DefDatabase<ThingDef>.AllDefs.Where(x => x.thingClass == typeof(Building_BookStore)))
            {
                t.building.fixedStorageSettings.filter.disallowedCategories = LoTechBlackList;
                t.building.fixedStorageSettings.filter.ResolveReferences();
                t.building.defaultStorageSettings.filter.ResolveReferences();
            }

            //d. Removing temporary defs from database.
            foreach (ThingDef def in garbage)
            {
                AccessTools.Method(typeof(DefDatabase<ThingDef>), "Remove").Invoke(this, new object[] { def });
            }
        }

        public static bool IsTechLevelRelevant(TechLevel level, out string tag)
        {
            if (level > 0)
            {
                tag = level.ToString();
                return true;
            }
            //if (ModsConfig.AnomalyActive)
            //{
            //    tag = "Anomaly";
            //    return true;
            //}
            tag = null;
            return false; 
        }

        public override void SceneLoaded(Scene scene)
        {
            GameJustLoaded = GenScene.InPlayScene;
        }

        public override void MapComponentsInitializing(Map map)
        {
            if (GameJustLoaded)
            {
                if (Prefs.LogVerbose) Log.Message("[HumanResources] Game started, resetting and caching resources...");
                unlocked.NewGameStarted();
                Extension_Research.currentPawnsCache = null;
                GameJustLoaded = false;
            }
        }

        //Dealing with older versions
        public override void MapLoaded(Map map)
        {
            ThingDef simpleBench = DefDatabase<ThingDef>.GetNamed("SimpleResearchBench");
            ThingDef hiTechBench = DefDatabase<ThingDef>.GetNamed("HiTechResearchBench");
            var obsolete = new List<Building_WorkTable>();
            obsolete.AddRange(map.listerBuildings.AllBuildingsColonistOfClass<Building_WorkTable>().Where(x => x.def == simpleBench || x.def == hiTechBench));
            if (obsolete.Any())
            {
                Log.Warning("[HumanResources] Replacing " + obsolete.Count() + " outdated research benches.");
                foreach (Building_WorkTable oldBench in obsolete)
                {
                    Building_ResearchBench newBench;
                    newBench = (Building_ResearchBench)ThingMaker.MakeThing(oldBench.def, oldBench.Stuff);
                    newBench.SetFactionDirect(oldBench.Faction);
                    var spawnedBench = (Building_ResearchBench)GenSpawn.Spawn(newBench, oldBench.Position, oldBench.Map, oldBench.Rotation);
                    spawnedBench.HitPoints = oldBench.HitPoints;
                }
            }
        }

        public override void SettingsChanged()
        {
            base.SettingsChanged();
            UpdateSettings();
        }

        public void UpdateSettings()
        {
            TechPoolTitle = Settings.GetHandle("TechPoolTitle", "TechPoolModeTitle".Translate(), "TechPoolModeDesc".Translate(), false);
            TechPoolTitle.CustomDrawer = rect => false;
            TechPoolTitle.CanBeReset = false;
            TechPoolIncludesStarting = Settings.GetHandle<bool>("TechPoolIncludesStarting", "TechPoolIncludesStartingTitle".Translate(), "TechPoolIncludesStartingDesc".Translate(), true);
            TechPoolIncludesStarting.OnValueChanged = x => { ValidateTechPoolSettings(x); };
            TechPoolIncludesTechLevel = Settings.GetHandle<bool>("TechPoolIncludesTechLevel", "TechPoolIncludesTechLevelTitle".Translate(), "TechPoolIncludesTechLevelDesc".Translate(), true);
            TechPoolIncludesTechLevel.OnValueChanged = x => { ValidateTechPoolSettings(x); };
            TechPoolIncludesBackground = Settings.GetHandle<bool>("TechPoolIncludesBackground", "TechPoolIncludesBackgroundTitle".Translate(), "TechPoolIncludesBackgroundDesc".Translate(), false);
            TechPoolIncludesBackground.OnValueChanged = x => { ValidateTechPoolSettings(x); };
            TechPoolIncludesScenario = Settings.GetHandle<bool>("TechPoolIncludesScenario", "TechPoolIncludesScenarioTitle".Translate(), "TechPoolIncludesScenarioDesc".Translate(), true);
            TechPoolIncludesScenario.OnValueChanged = x => { ValidateTechPoolSettings(x); };
            WeaponPoolMode = Settings.GetHandle("WeaponPoolMode", "WeaponPoolModeTitle".Translate(), "WeaponPoolModeDesc".Translate(), FactionWeaponPool.Scenario, null, "WeaponPoolMode_");
            FreeScenarioWeapons = Settings.GetHandle("FreeScenarioWeapons", "FreeScenarioWeaponsTitle".Translate(), "FreeScenarioWeaponsDesc".Translate(), false);
            LearnMeleeWeaponsByGroup = Settings.GetHandle<bool>("LearnMeleeWeaponsByGroup", "LearnMeleeWeaponsByGroupTitle".Translate(), "LearnMeleeWeaponsByGroupDesc".Translate(), false);
            LearnRangedWeaponsByGroup = Settings.GetHandle<bool>("LearnRangedWeaponsByGroup", "LearnRangedWeaponsByGroupTitle".Translate(), "LearnRangedWeaponsByGroupDesc".Translate(), true);
            RequireTrainingForSingleUseWeapons = Settings.GetHandle<bool>("RequireTrainingForSingleUseWeapons", "RequireTrainingForSingleUseWeaponsTitle".Translate(), "RequireTrainingForSingleUseWeaponsDesc".Translate(), false);
            EnableJoyGiver = Settings.GetHandle<bool>("EnableJoyGiver", "EnableJoyGiverTitle".Translate(), "EnableJoyGiverDesc".Translate(), true);
            ResearchSpeedTiedToDifficulty = Settings.GetHandle<bool>("ResearchSpeedTiedToDifficulty", "ResearchSpeedTiedToDifficultyTitle".Translate(), "ResearchSpeedTiedToDifficultyDesc".Translate(), true);
            StudySpeedTiedToDifficulty = Settings.GetHandle<bool>("StudySpeedTiedToDifficulty", "StudySpeedTiedToDifficultyTitle".Translate(), "StudySpeedTiedToDifficultyDesc".Translate(), true);
            FullStartupReport = Settings.GetHandle<bool>("FullStartupReport", "DEV: Print full startup report", null, false);
            FullStartupReport.NeverVisible = !Prefs.DevMode;
        }

        public void ValidateTechPoolSettings(bool value)
        {
            if (!value && !TechPoolIncludesStarting.Value && !TechPoolIncludesTechLevel.Value && !TechPoolIncludesBackground && !TechPoolIncludesScenario)
            {
                Messages.Message("TechPoolMinimumDefaultMsg".Translate(), MessageTypeDefOf.CautionInput);
                TechPoolIncludesStarting.ResetToDefault();
                ResetControl(TechPoolIncludesStarting);
            }
        }

        public void ResetControl(SettingHandle hanlde)
        {
            MethodInfo ResetHandleControlInfo = AccessTools.Method("HugsLib.Settings.Dialog_ModSettings:ResetHandleControlInfo");
            ResetHandleControlInfo.Invoke(Find.WindowStack.currentlyDrawnWindow, new object[] { hanlde });
        }

        private static bool SplitSimpleWeapons(ThingDef t, List<string> forbiddenWeaponTags)
        {
            if (t.NotThatHard()) return false;
            bool tagged = !t.weaponTags.NullOrEmpty();
            bool flag = false;
            foreach (string tag in forbiddenWeaponTags)
            {
                if (tagged && t.weaponTags.Any(x => x.Contains(tag)))
                {
                    flag = true;
                    SimpleWeapons.Add(t);
                    break;
                }
            }
            if (flag) return true;
            if (t.ExemptIfSingleUse() || (t.IsRangedWeapon && t.defName.ToLower().Contains("gun")))
            {
                SimpleWeapons.Add(t);
                return true;
            }
            return false;
        }
    }
}