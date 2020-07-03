using HarmonyLib;
using HugsLib;
using HugsLib.Settings;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace HumanResources
{
    public class ModBaseHumanResources : ModBase
    {

        public static FieldInfo ScenPartThingDefInfo = AccessTools.Field(typeof(ScenPart_ThingCount), "thingDef");

        public static List<ThingDef> UniversalCrops = new List<ThingDef>();

        public static List<ThingDef> UniversalWeapons = new List<ThingDef>();

        public static List<ThingDef> SimpleWeapons = new List<ThingDef>();

        public static UnlockManager unlocked = new UnlockManager();

        public enum FactionTechPool { Both, TechLevel, Starting }

        public static FactionTechPool TechPoolMode;

        public static bool TechPoolIncludesTechLevel => TechPoolMode < FactionTechPool.Starting;

        public static bool TechPoolIncludesStarting => TechPoolMode != FactionTechPool.TechLevel;

        public static SettingHandle<bool> TechPoolIncludesScenario;

        public static SettingHandle<bool> ResearchSpeedTiedToDifficulty;

        public static SettingHandle<bool> StudySpeedTiedToDifficulty;

        public enum FactionWeaponPool { Both, TechLevel, Scenario }

        public static FactionWeaponPool WeaponPoolMode;

        public static bool WeaponPoolIncludesTechLevel => WeaponPoolMode < FactionWeaponPool.Scenario;

        public static bool WeaponPoolIncludesScenario => WeaponPoolMode != FactionWeaponPool.TechLevel;

        public static SettingHandle<bool> FreeScenarioWeapons;

        public ModBaseHumanResources()
        {
            Settings.EntryName = "Human Resources";
        }

        public override string ModIdentifier
        {
            get
            {
                return "JPT_HumanResources";
            }
        }

        public override void DefsLoaded()
        {
            //1. Adding Tech Tab to Pawns
            //ThingDef injection stolen from the work of notfood for Psychology
            var zombieThinkTree = DefDatabase<ThinkTreeDef>.GetNamedSilentFail("Zombie");
            IEnumerable<ThingDef> things = (from def in DefDatabase<ThingDef>.AllDefs
                                            where def.race?.intelligence == Intelligence.Humanlike && !def.defName.Contains("Android") && !def.defName.Contains("Robot") && (zombieThinkTree == null || def.race.thinkTreeMain != zombieThinkTree)
                                            select def);
            List<string> registered = new List<string>();
            foreach (ThingDef t in things)
            {
                if (t.inspectorTabsResolved == null)
                {
                    t.inspectorTabsResolved = new List<InspectTabBase>(1);
                }
                t.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_PawnKnowledge)));
                if (t.comps == null)
                {
                    t.comps = new List<CompProperties>(1);
                }
                t.comps.Add(new CompProperties_Knowledge());
                registered.Add(t.defName);
            }

            //2. Preparing knowledge support infrastructure

            //a. Things everyone knows
            UniversalWeapons.AddRange(DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWeapon));
            UniversalCrops.AddRange(DefDatabase<ThingDef>.AllDefs.Where(x => x.plant != null && x.plant.Sowable));

            //b. Minus things unlocked on research
            ThingFilter lateFilter = new ThingFilter();
            foreach (ResearchProjectDef tech in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                tech.InferSkillBias();
                tech.CreateStuff(lateFilter, unlocked);
                foreach (ThingDef weapon in tech.UnlockedWeapons()) UniversalWeapons.Remove(weapon);
                foreach (ThingDef plant in tech.UnlockedPlants()) UniversalCrops.Remove(plant);
            };

            //b. Also removing atipical weapons
            ThingDef WeaponsNotBasicDef = DefDatabase<ThingDef>.GetNamed("NotBasic");
            List<string> ForbiddenWeaponTags = WeaponsNotBasicDef.weaponTags;
            UniversalWeapons.RemoveAll(x => SplitSimpleWeapons(x, ForbiddenWeaponTags));
            AccessTools.Method(typeof(DefDatabase<ThingDef>), "Remove").Invoke(this, new object[] { WeaponsNotBasicDef });

            //c. Telling humans what's going on
            ThingCategoryDef knowledgeCat = TechDefOf.Knowledge;
            IEnumerable<ThingDef> codifiedTech = DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWithinCategory(knowledgeCat));
            if (Prefs.LogVerbose)
            {
                Log.Message("[HumanResources] Codified technologies:" + codifiedTech.Select(x => x.label).ToStringSafeEnumerable());
                Log.Message("[HumanResources] Basic crops: " + UniversalCrops.ToStringSafeEnumerable());
                Log.Message("[HumanResources] Basic weapons: " + UniversalWeapons.ToStringSafeEnumerable());
                Log.Message("[HumanResources] Basic weapons that require training: " + SimpleWeapons.ToStringSafeEnumerable());
                Log.Warning("[HumanResources] Basic weapons tags: " + SimpleWeapons.Where(x => !x.weaponTags.NullOrEmpty()).SelectMany(x => x.weaponTags).Distinct().ToStringSafeEnumerable());
            }
            else Log.Message("[HumanResources] This is what we know: " + codifiedTech.EnumerableCount() + " technologies processed, " + UniversalCrops.Count() + " basic crops, " + UniversalWeapons.Count() + " basic weapons + "+SimpleWeapons.Count()+ " that require training.");

            //3. Filling gaps on the database

            //a. TechBook dirty trick, but only now this is possible!
            foreach (ThingDef t in DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x == TechDefOf.TechBook || x == TechDefOf.UnfinishedTechBook))
            {
                t.stuffCategories.Add(TechDefOf.Technic);
            }

            //b. Filling main tech category with subcategories
            foreach (ThingDef t in lateFilter.AllowedThingDefs.Where(t => !t.thingCategories.NullOrEmpty()))
            {
                foreach (ThingCategoryDef c in t.thingCategories)
                {
                    c.childThingDefs.Add(t);
                    if (!knowledgeCat.childCategories.NullOrEmpty() && !knowledgeCat.childCategories.Contains(c))
                    {
                        knowledgeCat.childCategories.Add(c);
                    }
                }
            }

            //c. Populating knowledge recipes and book shelves
            foreach (RecipeDef r in DefDatabase<RecipeDef>.AllDefs.Where(x => x.fixedIngredientFilter.AnyAllowedDef == null))
            {
                r.fixedIngredientFilter.ResolveReferences();
            }
            foreach (ThingDef t in DefDatabase<ThingDef>.AllDefs.Where(x => x.thingClass == typeof(Building_BookStore)))
            {
                t.building.fixedStorageSettings.filter.ResolveReferences();
                t.building.defaultStorageSettings.filter.ResolveReferences();
            }

            //4. Finally, preparing settings
            TechPoolMode = Settings.GetHandle("TechPoolMode", "TechPoolModeTitle".Translate(), "TechPoolModeDesc".Translate(), FactionTechPool.Both, null, "TechPoolMode_");
            TechPoolIncludesScenario = Settings.GetHandle<bool>("TechPoolIncludesScenario", "TechPoolIncludesScenarioTitle".Translate(), "TechPoolIncludesScenarioDesc".Translate(), true);
            WeaponPoolMode = Settings.GetHandle("WeaponPoolMode", "WeaponPoolModeTitle".Translate(), "WeaponPoolModeDesc".Translate(), FactionWeaponPool.Scenario, null, "WeaponPoolMode_");
            FreeScenarioWeapons = Settings.GetHandle("FreeScenarioWeapons", "FreeScenarioWeaponsTitle".Translate(), "FreeScenarioWeaponsDesc".Translate(), false);
            ResearchSpeedTiedToDifficulty = Settings.GetHandle<bool>("ResearchSpeedTiedToDifficulty", "ResearchSpeedTiedToDifficultyTitle".Translate(), "ResearchSpeedTiedToDifficultyDesc".Translate(), true);
            StudySpeedTiedToDifficulty = Settings.GetHandle<bool>("StudySpeedTiedToDifficulty", "StudySpeedTiedToDifficultyTitle".Translate(), "StudySpeedTiedToDifficultyDesc".Translate(), true);
        }

        public override void MapComponentsInitializing(Map map)
        {
            if (GenScene.InPlayScene)
            {
                unlocked.RegisterStartingResources();
                unlocked.RecacheUnlockedWeapons();
            }
        }

        //public override void WorldLoaded()
        //{
        //}

        private static bool SplitSimpleWeapons (ThingDef t, List<string> forbiddenWeaponTags)
        {
            bool flag = false;
            //if (!t.weaponTags.NullOrEmpty() && t.weaponTags.Any(x => x.Contains("TurretGun")))
            foreach (string tag in forbiddenWeaponTags)
            {
                if (!t.weaponTags.NullOrEmpty() && t.weaponTags.Any(x => x.Contains(tag)))
                {
                    flag = true;
                    SimpleWeapons.Add(t);
                    break;
                }
            }
            if (!flag && t.IsRangedWeapon && t.defName.ToLower().Contains("gun"))
            {
                flag = true;
                SimpleWeapons.Add(t);
            }
            if (t.IsWithinCategory(DefDatabase<ThingCategoryDef>.GetNamed("WeaponsMeleeBladelink"))) 
            {
                flag = true;
                SimpleWeapons.Add(t);
            }
            return flag;
        }
    }
}