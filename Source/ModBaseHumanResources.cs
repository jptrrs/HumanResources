using HugsLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using Verse;

namespace HumanResources
{
    public class ModBaseHumanResources : ModBase
    {
        
        public override string ModIdentifier
        {
            get
            {
                return "JPT_HumanResources";
            }
        }

        // ThingDef injection stolen from the work of notfood for Psychology
        public override void DefsLoaded()
        {
            //Log.Warning("DefsLoaded start...");

            //Adding Tech Tab to Pawns
            var zombieThinkTree = DefDatabase<ThinkTreeDef>.GetNamedSilentFail("Zombie");
            IEnumerable<ThingDef> things = (from def in DefDatabase<ThingDef>.AllDefs
                                            where def.race?.intelligence == Intelligence.Humanlike && !def.defName.Contains("Android") && !def.defName.Contains("Robot")&& (zombieThinkTree == null || def.race.thinkTreeMain != zombieThinkTree)
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

            //Preparing knowledge support things
            UniversalWeapons.AddRange(DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWeapon));
            UniversalCrops.AddRange(DefDatabase<ThingDef>.AllDefs.Where(x => x.plant != null && x.plant.Sowable));
            ThingFilter lateFilter = new ThingFilter();
            foreach (ResearchProjectDef tech in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                tech.InferSkillBias();
                tech.CreateDummyThingDef(lateFilter);
                foreach (ThingDef weapon in tech.UnlockedWeapons()) UniversalWeapons.Remove(weapon);
                foreach (ThingDef plant in tech.UnlockedPlants()) UniversalCrops.Remove(plant);
            };
            //Verse.Log.Message("[HumanResources] Created tech dummy defs:" + DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWithinCategory(DefDatabase<ThingCategoryDef>.GetNamed("Knowledge"))).ToStringSafeEnumerable());
            ThingCategoryDef KnowledgeDef = DefDatabase<ThingCategoryDef>.GetNamed("Knowledge");
            foreach (ThingDef t in lateFilter.AllowedThingDefs.Where(t => !t.thingCategories.NullOrEmpty()))
            {
                foreach (ThingCategoryDef c in t.thingCategories)
                {
                    c.childThingDefs.Add(t);
                    if (!KnowledgeDef.childCategories.Contains(c))
                    {
                        KnowledgeDef.childCategories.Add(c);
                    }
                }
            }

            //Populating knowledge recipes
            foreach (RecipeDef r in DefDatabase<RecipeDef>.AllDefs.Where(x => x.defName.StartsWith("Tech_")))
            {
                r.fixedIngredientFilter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamed("Knowledge"), true);
                r.defaultIngredientFilter.CopyAllowancesFrom(r.fixedIngredientFilter);
            }
            Verse.Log.Message("[HumanResources] Universal Weapons: " + UniversalWeapons.ToStringSafeEnumerable());
        }

        public static List<ThingDef> UniversalWeapons = new List<ThingDef>();
        public static List<ThingDef> UniversalCrops = new List<ThingDef>();
        public static UnlockManager unlocked;

        public override void WorldLoaded()
        {
            unlocked = new UnlockManager();
            unlocked.RecacheUnlockedWeapons();
            Log.Message("[HumanResources] Unlocked weapons: " + unlocked.weapons.ToStringSafeEnumerable());
        }
    }   
}