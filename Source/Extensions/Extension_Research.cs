using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection;
using System.Globalization;

namespace HumanResources
{
	public static class Extension_Research
	{
		public static SkillDef Bias = new SkillDef();

		public static Dictionary<ResearchProjectDef, List<SkillDef>> SkillsByTech = new Dictionary<ResearchProjectDef, List<SkillDef>>();

        private const float MarketValueOffset = 200f;
        private const float ResearchPointsPerWorkTick = 0.0075f; // aprox. 10% down down from vanilla 0.00825f, neutralized with half available techs in library;
        private const float StudyPointsPerWorkTick = 1f;

        private static bool animalsTag = false;
        private static bool artisticTag = false;
        private static bool constructionTag = false;
        private static bool cookingTag = false;
        private static bool craftingTag = false;
        private static bool intellectualTag = false;
        private static bool medicineTag = false;
        private static bool meleeTag = false;
        private static bool miningTag = false;
        private static bool plantsTag = false;
        private static bool shootingTag = false;
        private static bool socialTag = false;

        public static void CarefullyFinishProject(this ResearchProjectDef project, Thing place)
        {
            bool careful = !project.prerequisites.NullOrEmpty();
            List<ResearchProjectDef> prerequisitesCopy = new List<ResearchProjectDef>();
            if (careful)
            {
                prerequisitesCopy.AddRange(project.prerequisites);
                project.prerequisites.Clear();
            }
            Find.ResearchManager.FinishProject(project);
            if (careful) project.prerequisites.AddRange(prerequisitesCopy);
            Messages.Message("MessageFiledTech".Translate(project.label), place, MessageTypeDefOf.TaskCompletion, true);
        }

        public static void CreateStuff(this ResearchProjectDef tech, ThingFilter filter, UnlockManager unlocked)
        {
            string name = "Tech_" + tech.defName;
            ThingCategoryDef tCat = DefDatabase<ThingCategoryDef>.GetNamed(tech.techLevel.ToString());
            string label = "KnowledgeLabel".Translate(tech.label);
            ThingDef techStuff = new ThingDef
            {
                thingClass = typeof(ThingWithComps),
                defName = name,
                label = label,
                description = tech.description,
                category = ThingCategory.Item,
                thingCategories = new List<ThingCategoryDef>() { tCat },
                techLevel = tech.techLevel,
                menuHidden = true,
                stuffProps = new StuffProperties()
                {
                    categories = new List<StuffCategoryDef>() { DefDatabase<StuffCategoryDef>.GetNamed("Technic") },
                    color = ResearchTree_Assets.ColorCompleted[tech.techLevel],
                    stuffAdjective = tech.LabelCap,
                    statOffsets = new List<StatModifier>()
                    {
                        new StatModifier
                        {
                            stat = StatDefOf.MarketValue,
                            value = MarketValueOffset
                        }
                    },
                    statFactors = new List<StatModifier>()
                    {
                        new StatModifier
                        {
                            stat = StatDefOf.WorkToMake,
                            value = StuffCostFactor(tech)
                        },
                        new StatModifier
                        {
                            stat = StatDefOf.MarketValue,
                            value = StuffMarketValueFactor(tech)
                        }
                    }
                }
            };
            techStuff.ResolveReferences();
            MethodInfo GiveShortHashInfo = AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash");
            GiveShortHashInfo.Invoke(tech, new object[] { techStuff, typeof(ThingDef) });
            DefDatabase<ThingDef>.Add(techStuff);
            filter.SetAllow(techStuff, true);
            unlocked.stuffByTech.Add(tech, techStuff);
            unlocked.techByStuff.Add(techStuff, tech);
        }

        public static void EjectTech(this ResearchProjectDef tech, Thing place)
        {
            FieldInfo progressInfo = AccessTools.Field(typeof(ResearchManager), "progress");
            Dictionary<ResearchProjectDef, float> progress = (Dictionary<ResearchProjectDef, float>)progressInfo.GetValue(Find.ResearchManager);
            progress[tech] = 0f;
            Messages.Message("MessageEjectedTech".Translate(tech.label), place, MessageTypeDefOf.TaskCompletion, true);
        }

        public static float GetProgress(this ResearchProjectDef tech, Dictionary<ResearchProjectDef, float> expertise)
        {
            float result;
            if (expertise != null && expertise.TryGetValue(tech, out result))
            {
                return result;
            }
            expertise.Add(tech, 0f);
            return 0f;
        }

        public static void InferSkillBias(this ResearchProjectDef tech)
        {
            //Log.Warning("InferSkillBias Starting for "+tech.LabelCap);
            //1. check what it unlocks
            List<Pair<Def, string>> unlocks = ResearchTree_Patches.GetUnlockDefsAndDescs(tech);
            IEnumerable<Def> defs = unlocks.Select(x => x.First).AsEnumerable();
            IEnumerable<ThingDef> thingDefs = from d in defs
                                              where d is ThingDef
                                              select d as ThingDef;
            IEnumerable<RecipeDef> recipeDefs = from d in defs
                                                where d is RecipeDef
                                                select d as RecipeDef;
            IEnumerable<TerrainDef> terrainDefs = from d in defs
                                                  where d is TerrainDef
                                                  select d as TerrainDef;

            //2. look for skills based on unlocked stuff

            //a. checking by query on the research tree
            int matches = 0;
            if (tech.Matches("scanner") > 0 | tech.Matches("terraform") > 0) { miningTag = true; matches++; };

            if (tech.Matches("sterile") > 0 | tech.Matches("medical") > 0 | tech.Matches("medicine") > 0 | tech.Matches("cryptosleep") > 0 | tech.Matches("prostheses") > 0 | tech.Matches("implant") > 0 | tech.Matches("organs") > 0 | tech.Matches("surgery") > 0) { medicineTag = true; matches++; };

            if (tech.Matches("irrigation") > 0 | tech.Matches("soil") > 0 | tech.Matches("hydroponic") > 0) { plantsTag = true; matches++; };

            //b. checking by unlocked things
            if (thingDefs.Count() > 0)
            {
                foreach (ThingDef t in thingDefs)
                {
                    if (t != null) InvestigateThingDef(t);
                }
            }

            //c. checking by unlocked recipes
            if (recipeDefs.Count() > 0)
            {
                foreach (RecipeDef r in recipeDefs)
                {
                    //Log.Message("trying recipe " + r.label);
                    foreach (ThingDef t in r.products.Select(x => x.thingDef))
                    {
                        InvestigateThingDef(t);
                    }
                    if (r.workSkill != null)
                    {
                        AccessTools.Field(typeof(Extension_Research), r.workSkill.defName.ToLower() + "Tag").SetValue(tech, true);
                    }
                }
            }

            //d. checking by unlocked terrainDefs
            if (terrainDefs.Count() > 0)
            {
                foreach (TerrainDef t in terrainDefs)
                {
                    if (!constructionTag && t.designationCategory != null && t.designationCategory.label.Contains("floor")) constructionTag = true;
                    else if (!miningTag) miningTag = true;
                }
            }

            //3. Figure out Bias.
            int ThingDefCount = thingDefs.Count();
            int RecipeDefCount = recipeDefs.Count();
            int TerrainDefCount = terrainDefs.Count();

            List<SkillDef> relevantSkills = new List<SkillDef>();
            if (shootingTag)
            {
                relevantSkills.Add(SkillDefOf.Shooting);
                shootingTag = false;
            }
            if (meleeTag)
            {
                relevantSkills.Add(SkillDefOf.Melee);
                meleeTag = false;
            }
            if (constructionTag)
            {
                relevantSkills.Add(SkillDefOf.Construction);
                constructionTag = false;
            }
            if (miningTag)
            {
                relevantSkills.Add(SkillDefOf.Mining);
                miningTag = false;
            }
            if (cookingTag)
            {
                relevantSkills.Add(SkillDefOf.Cooking);
                cookingTag = false;
            }
            if (plantsTag)
            {
                relevantSkills.Add(SkillDefOf.Plants);
                plantsTag = false;
            }
            if (animalsTag)
            {
                relevantSkills.Add(SkillDefOf.Animals);
                animalsTag = false;
            }
            if (craftingTag)
            {
                relevantSkills.Add(SkillDefOf.Crafting);
                craftingTag = false;
            }
            if (artisticTag)
            {
                relevantSkills.Add(SkillDefOf.Artistic);
                artisticTag = false;
            }
            if (medicineTag)
            {
                relevantSkills.Add(SkillDefOf.Medicine);
                medicineTag = false;
            }
            if (socialTag)
            {
                relevantSkills.Add(SkillDefOf.Social);
                socialTag = false;
            }
            if (intellectualTag)
            {
                relevantSkills.Add(SkillDefOf.Intellectual);
                intellectualTag = false;
            }

            SkillsByTech.Add(tech, relevantSkills);
        }

        public static void InvestigateThingDef(ThingDef t)
        {
            if (t != null)
            {
                if (!shootingTag) try { shootingTag |= t.IsRangedWeapon | t.designationCategory == DesignationCategoryDefOf.Security; } catch { };
                if (!meleeTag) try { meleeTag |= t.IsMeleeWeapon | t.designationCategory == DesignationCategoryDefOf.Security; } catch { };
                if (!constructionTag) try { constructionTag |= t.BuildableByPlayer; } catch { };
                if (!miningTag) try { miningTag |= t.IsShell; } catch { };
                if (!cookingTag) try { cookingTag |= t.ingestible.IsMeal | t.building.isMealSource; } catch { };
                if (!plantsTag) try { plantsTag |= t.plant != null; } catch { };
                if (!craftingTag) try { craftingTag |= t.IsApparel | t.IsWeapon; } catch { };
                if (!artisticTag) try { artisticTag |= t.IsArt | t.IsWithinCategory(ThingCategoryDefOf.BuildingsArt); } catch { };
                if (!medicineTag) try { medicineTag |= t.IsMedicine | t.IsDrug; } catch { };
            }
        }

        public static void Learned(this ResearchProjectDef tech, float amount, float recipeCost, Pawn researcher, bool research = false)
        {
            float total = research ? tech.baseCost : recipeCost * tech.StuffCostFactor();
            amount *= research ? ResearchPointsPerWorkTick : StudyPointsPerWorkTick;
            amount *= Find.Storyteller.difficulty.researchSpeedFactor;
            Dictionary<ResearchProjectDef, float> expertise = researcher.TryGetComp<CompKnowledge>().expertise;
            foreach (ResearchProjectDef ancestor in expertise.Keys)
            {
                if (!tech.prerequisites.NullOrEmpty() && tech.prerequisites.Contains(ancestor))
                {
                    amount *= 2;
                    break;
                }
            }
            if (researcher != null && researcher.Faction != null)
            {
                amount /= tech.CostFactor(researcher.Faction.def.techLevel);
            }
            if (DebugSettings.fastResearch)
            {
                amount *= 500f;
            }
            if (researcher != null && research)
            {
                researcher.records.AddTo(RecordDefOf.ResearchPointsResearched, amount);
            }
            float num = tech.GetProgress(expertise);
            num += amount / total;
            //Log.Warning(tech + " research performed by " + researcher + ": " + amount + "/" + total);
            expertise[tech] = num;
        }

        public static int Matches(this ResearchProjectDef tech, string query)
        {
            var culture = CultureInfo.CurrentUICulture;
            query = query.ToLower(culture);

            if (tech.LabelCap.RawText.ToLower(culture).Contains(query))
                return 1;
            if (ResearchTree_Patches.GetUnlockDefsAndDescs(tech).Any(unlock => unlock.First.LabelCap.RawText.ToLower(culture).Contains(query)))
                return 2;
            if (tech.description.ToLower(culture).Contains(query))
                return 3;
            return 0;
        }

        public static float StuffCostFactor(this ResearchProjectDef tech)
        {
            return (float)Math.Round(Math.Pow(tech.baseCost, (1.0 / 2.0)), 1);
        }

        public static List<ThingDef> UnlockedPlants(this ResearchProjectDef tech)
        {
            List<ThingDef> result = new List<ThingDef>();
            foreach (ThingDef plant in tech.GetPlantsUnlocked()) //Cacau shouldn't be a Tree!
            {
                result.Add(plant);
            }
            return result;
        }

        public static List<ThingDef> UnlockedWeapons(this ResearchProjectDef tech)
        {
            List<ThingDef> result = new List<ThingDef>();
            foreach (RecipeDef r in tech.GetRecipesUnlocked().Where(x => !x.products.NullOrEmpty()))
            {
                foreach (ThingDef weapon in r.products.Select(x => x.thingDef).Where(x => x.IsWeapon && (x.weaponTags.NullOrEmpty() || !x.weaponTags.Any(t => t.Contains("Basic"))) && !(x.defName.Contains("Tool") || x.defName.Contains("tool"))))
                {
                    result.Add(weapon);
                }
            }
            foreach (ThingDef weapon in tech.GetThingsUnlocked().Where(x => x.building != null && x.building.turretGunDef != null).Select(x => x.building.turretGunDef))
            {
                result.Add(weapon);
            }
            return result;
        }

        //higher tech level
        private static float StuffMarketValueFactor(this ResearchProjectDef tech)
		{
			return (float)Math.Round(Math.Pow(tech.baseCost, 1.0 / 3.0) / 10, 1);
		}
	}
}
