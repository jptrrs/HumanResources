using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using FluffyResearchTree;
using Verse;
using System.Reflection;
using HarmonyLib;
//using Harmony;

namespace HumanResources
{
	public static class Research_Extension
	{
		public static SkillDef Bias = new SkillDef();

		public static Dictionary<ResearchProjectDef, List<SkillDef>> SkillsByTech =
	new Dictionary<ResearchProjectDef, List<SkillDef>>();

		private static bool shootingTag = false;
		private static bool meleeTag = false;
		private static bool constructionTag = false;
		private static bool miningTag = false;
		private static bool cookingTag = false;
		private static bool plantsTag = false;
		private static bool animalsTag = false; //revert to secondary
		private static bool craftingTag = false;
		private static bool artisticTag = false;
		private static bool medicineTag = false;
		private static bool socialTag = false; //broader knowledge
		private static bool intellectualTag = false; //higher tech level

		//public static void InferSkillBias(this ResearchNode research)
		public static void InferSkillBias(this ResearchProjectDef tech)
		{
			//ResearchProjectDef tech = research.Research;
			ResearchNode techNode = new ResearchNode(tech);

			//Verse.Log.Message("InferSkillBias Starting for "+tech.LabelCap);

			//1. check what it unlocks
			List<Pair<Def, string>> unlocks = tech.GetUnlockDefsAndDescs();
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

			//IEnumerable<ThingDef> thingDefs = tech.GetThingsUnlocked();
			//IEnumerable<RecipeDef> recipeDefs = tech.GetRecipesUnlocked();
			//IEnumerable<TerrainDef> terrainDefs = tech.GetTerrainUnlocked();

			//2. look for skills based on unlocked stuff

			//a. checking by query on the research tree
			int matches = 0;
			if (techNode.Matches("scanner") > 0 | techNode.Matches("terraform") > 0) { miningTag = true; matches++; };

			if (techNode.Matches("sterile") > 0 | techNode.Matches("medical") > 0 | techNode.Matches("medicine") > 0 | techNode.Matches("cryptosleep") > 0 | techNode.Matches("prostheses") > 0 | techNode.Matches("implant") > 0 | techNode.Matches("organs") > 0 | techNode.Matches("surgery") > 0) { medicineTag = true; matches++; };

			if (techNode.Matches("irrigation") > 0 | techNode.Matches("soil") > 0 | techNode.Matches("hydroponic") > 0) { plantsTag = true; matches++; };

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
					foreach (ThingDef t in r.products.Select(x => x.thingDef))
					{
						InvestigateThingDef(t);
					}
					if (r.workSkill != null)
					{
						//FieldInfo workskillInfo = AccessTools.Field(typeof(ResearchNode_Extensions), r.workSkill.label + "Tag");
						//Verse.Log.Message(r + " worksill is " + r.workSkill);// +", field is "+ workskillInfo.GetValue(research));
						AccessTools.Field(typeof(Research_Extension), r.workSkill.label + "Tag").SetValue(techNode, true);
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
			//Verse.Log.Message("Skills for " + tech + ": " + scoreTableReport + " with " + matches + " matches, " + ThingDefCount + " thingDefs and " + RecipeDefCount + " recipes.");

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
			//if (ThingDefCount > 0)
			//{
			//Verse.Log.Message("Skills for " + tech + ": " + relevantSkills.ToStringSafeEnumerable() + " (with " + matches + " matches, " + ThingDefCount + " thingDefs, " + RecipeDefCount + " recipes and " + TerrainDefCount + " terrainDefs).");
			//}

			SkillsByTech.Add(tech, relevantSkills);
			//Bias = scoreTable.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
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
				if (medicineTag) try { medicineTag |= t.IsMedicine | t.IsDrug; } catch { };
			}
		}

		public static void CreateDummyThingDef(this ResearchProjectDef tech, ThingFilter filter)
		{
			string dummyName = "Tech_" + tech.defName;
			ThingCategoryDef tCat = DefDatabase<ThingCategoryDef>.GetNamed(tech.techLevel.ToString());
			ThingDef dummy = new ThingDef
			{
				defName = dummyName,
				label = tech.label,
				description = tech.description,
				category = ThingCategory.Item,
				thingCategories = new List<ThingCategoryDef>() { tCat },
				menuHidden = true
			};
			DefDatabase<ThingDef>.Add(dummy);
			filter.SetAllow(dummy, true);
		}

		public static void FillInThingfCategories(this ResearchProjectDef tech)
		{
			string techLevel = tech.techLevel.ToString();
			if (!DefDatabase<ThingCategoryDef>.AllDefs.Any(x => x.defName.Contains(techLevel)))
			{
				ThingCategoryDef newCat = new ThingCategoryDef
				{
					defName = techLevel,
					label = techLevel.ToLower(),
					parent = DefDatabase<ThingCategoryDef>.GetNamed("Knowledge")
				};

				//hard crash!
				//ThingCategoryDef newCat = DefDatabase<ThingCategoryDef>.GetNamed("Knowledge");
				//newCat.defName = techLevel;
				//newCat.label = techLevel.ToLower();
				//newCat.parent = DefDatabase<ThingCategoryDef>.GetNamed("Knowledge");

				DefDatabase<ThingCategoryDef>.Add(newCat);
				Verse.Log.Message("[HumanResources] " + techLevel + " included as a category.");
			}
		}

		public static List<ThingDef> UnlockedWeapons(this ResearchProjectDef tech)
		{
			//Verse.Log.Message("looking for unlocked weapons for "+tech+"...");
			List<ThingDef> result = new List<ThingDef>();
			foreach (RecipeDef r in tech.GetRecipesUnlocked().Where(x => !x.products.NullOrEmpty()))
			{
				//Verse.Log.Message("...checking recipe " + r.label);
				foreach (ThingDef weapon in r.products.Select(x => x.thingDef).Where(x => x.IsWeapon && (x.weaponTags.NullOrEmpty() || !x.weaponTags.Any(s => s.Contains("Basic")))))
				{
					result.Add(weapon);
				}
			}
			foreach (ThingDef weapon in tech.GetThingsUnlocked().Where(x => x.building.turretGunDef != null).Select(x => x.building.turretGunDef))
			{
				//Verse.Log.Message("...checking weapon " + weapon.label);
				result.Add(weapon);
			}
			return result;
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
	}
}
