using HarmonyLib;
using HugsLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;
using Verse;

namespace HumanResources
{
    using static HumanResourcesSettings;

    public class ModBaseHumanResources : ModBase
    {
        public static FieldInfo ScenePartThingDefInfo = AccessTools.Field(typeof(ScenPart_ThingCount), "thingDef");
        public static List<ThingDef>
            SimpleWeapons = new List<ThingDef>(),
            MountedWeapons = new List<ThingDef>(),
            UniversalCrops = new List<ThingDef>(),
            UniversalWeapons = new List<ThingDef>();
        public static UnlockManager unlocked = new UnlockManager();
        private static bool GameJustLoaded = true;

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

            // 1. Adding Tech Tab to Pawns
            //ThingDef injection stolen from the work of notfood for Psychology
            var zombieThinkTree = DefDatabase<ThinkTreeDef>.GetNamedSilentFail("Zombie");
            IEnumerable<ThingDef> things = (from def in DefDatabase<ThingDef>.AllDefs
                                            where def.race?.intelligence == Intelligence.Humanlike && (zombieThinkTree == null || def.race.thinkTreeMain != zombieThinkTree)
                                            select def);
            //List<string> registered = new List<string>();
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
            }
            InspectPaneUtility.Reset();

            // 2. Preparing knowledge support infrastructure

            // A. Provisions to deal with turrets & artilleries
            List<ThingDef> turretGuns = new List<ThingDef>();
            foreach (var (t, foundGun) in from ThingDef t in DefDatabase<ThingDef>.AllDefs
                                          let foundGun = t.GetTurretGun()
                                          where foundGun != null
                                          select (t, foundGun))
            {
                if (t.IsMannable()) MountedWeapons.Add(foundGun);
                else turretGuns.Add(foundGun);
            }

            // B. Things everyone knows
            UniversalWeapons.AddRange(DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWeapon).Except(turretGuns));
            UniversalCrops.AddRange(DefDatabase<ThingDef>.AllDefs.Where(x => x.plant != null && x.plant.Sowable));

            // C. Minus things unlocked on research
            ThingFilter lateFilter = new ThingFilter();
            foreach (ResearchProjectDef tech in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                tech.InferSkillBias();
                tech.CreateStuff(lateFilter, unlocked);
                foreach (ThingDef weapon in tech.UnlockedWeapons()) UniversalWeapons.Remove(weapon);
                foreach (ThingDef plant in tech.UnlockedPlants()) UniversalCrops.Remove(plant);
            };

            // D. Also removing atypical weapons
            List<string> ForbiddenWeaponTags = TechDefOf.HardWeapons.weaponTags;
            UniversalWeapons.RemoveAll(x => SplitSimpleWeapons(x, ForbiddenWeaponTags));
            List<ThingDef> garbage = new List<ThingDef>();
            garbage.Add(TechDefOf.HardWeapons);

            // E. Classifying pawn backstories
            PawnBackgroundUtility.BuildCache();

            // F. Telling humans what's going on
            ThingCategoryDef knowledgeCat = TechDefOf.Knowledge;
            IEnumerable<ThingDef> codifiedTech = DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWithinCategory(knowledgeCat));
            if (Prefs.LogVerbose || FullStartupReport)
            {
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

            // 5. Filling gaps on the database

            // A. TechBook dirty trick, but only now this is possible!
            TechDefOf.TechBook.stuffCategories = TechDefOf.UnfinishedTechBook.stuffCategories = TechDefOf.LowTechCategories.stuffCategories;
            TechDefOf.TechDrive.stuffCategories = TechDefOf.HiTechCategories.stuffCategories;
            garbage.Add(TechDefOf.LowTechCategories);
            garbage.Add(TechDefOf.HiTechCategories);

            // B. Filling main tech category with subcategories
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

            // C. Populating knowledge recipes and book shelves
            foreach (RecipeDef r in DefDatabase<RecipeDef>.AllDefs.Where(x => x.ingredients.Count == 1 && x.fixedIngredientFilter.AnyAllowedDef == null))
            {
                r.fixedIngredientFilter.ResolveReferences();
                r.defaultIngredientFilter.ResolveReferences();
            }
            foreach (ThingDef t in DefDatabase<ThingDef>.AllDefs.Where(x => x.thingClass == typeof(Building_BookStore)))
            {
                t.building.fixedStorageSettings.filter.ResolveReferences();
                t.building.defaultStorageSettings.filter.ResolveReferences();
            }

            // D. Removing temporary defs from database.
            foreach (ThingDef def in garbage)
            {
                AccessTools.Method(typeof(DefDatabase<ThingDef>), "Remove").Invoke(this, new object[] { def });
            }
        }


        public override void SceneLoaded(Scene scene)
        {
            if (GenScene.InPlayScene) GameJustLoaded = true;
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