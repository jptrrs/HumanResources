using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection;
using System.Globalization;
using UnityEngine;
using System.Text;

namespace HumanResources
{
    public static class Extension_Research
    {
        public static SkillDef Bias = new SkillDef();
        public static Dictionary<ResearchProjectDef, List<SkillDef>> SkillsByTech = new Dictionary<ResearchProjectDef, List<SkillDef>>();
        public static Dictionary<SkillDef, List<ResearchProjectDef>> TechsBySkill = new Dictionary<SkillDef, List<ResearchProjectDef>>();
        public static Dictionary<ResearchProjectDef, List<ThingDef>> WeaponsByTech = new Dictionary<ResearchProjectDef, List<ThingDef>>();
        public static Dictionary<ThingDef, ResearchProjectDef> TechByWeapon = new Dictionary<ThingDef, ResearchProjectDef>();
        public static Dictionary<ResearchProjectDef, List<Pawn>> AssignedHomework = new Dictionary<ResearchProjectDef, List<Pawn>>();
        private const float MarketValueOffset = 200f;
        private static FieldInfo ResearchPointsPerWorkTickInfo = AccessTools.Field(typeof(ResearchManager), "ResearchPointsPerWorkTick");

        private static float DifficultyResearchSpeedFactor // 90% from vanilla on easy, 80% on medium, 75% on rough & 70% on hard.
        { 
            get
            {
                float delta = 1 - Find.Storyteller.difficulty.researchSpeedFactor;
                float adjusted = delta > 0 ? delta : delta / 2;
                return 1 - (adjusted + 0.2f);
            }
        }

        private static float ResearchPointsPerWorkTick // default is aprox. 10% down down from vanilla 0.00825f, neutralized with half available techs in library.
        {
            get
            {
                float baseValue = (float)ResearchPointsPerWorkTickInfo.GetValue(new ResearchManager());
                return ModBaseHumanResources.ResearchSpeedTiedToDifficulty ? baseValue * DifficultyResearchSpeedFactor : baseValue * 0.9f;
            }
        }

        private static float StudyPointsPerWorkTick // 100% on easy, 90% on medium, 85% on rough & 80% on hard.
        {
            get
            {
                float baseValue = 1.1f;
                return ModBaseHumanResources.StudySpeedTiedToDifficulty? baseValue * DifficultyResearchSpeedFactor : baseValue;
            }
        }

        private static List<string>
            AnimalHints = new List<string>() { "animal" },
            CraftingHints = new List<string>() { "tool", "armor", "armour", "cloth" },
            IntellectualHints = new List<string>() { "manage" },
            MiningHints = new List<string>() { "scanner", "terraform" },
            MedicineHints = new List<string>() { "sterile", "medical", "medicine", "cryptosleep", "prostheses", "implant", "organs", "surgery" },
            PlantsHints = new List<string>() { "irrigation", "soil", "hydroponic" };

        private static bool
            animalsTag = false,
            artisticTag = false,
            constructionTag = false,
            cookingTag = false,
            craftingTag = false,
            intellectualTag = false,
            medicineTag = false,
            meleeTag = false,
            miningTag = false,
            plantsTag = false,
            shootingTag = false,
            socialTag = false;

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
            using (IEnumerator<Pawn> enumerator = currentPawns.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    CompKnowledge techComp = enumerator.Current.TryGetComp<CompKnowledge>();
                    techComp.homework?.RemoveAll(x => x == project);
                }
            }
            currentPawnsCache.Clear();
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
                    categories = new List<StuffCategoryDef>() { TechDefOf.Technic },
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
            //0. variables for diagnostics
            int
            matchesCount = 0,
            thingsCount = 0,
            recipesCount = 0,
            recipeThingsCount = 0,
            recipeSkillsCount = 0,
            terrainsCount = 0;
            List<string> keywords = new List<string>();
            bool usedPreReq = false;

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
            foreach (string word in AnimalHints) if (tech.Matches(word)) { animalsTag = true; matchesCount++; keywords.Add(word); break; }
            foreach (string word in CraftingHints) if (tech.Matches(word)) { craftingTag = true; matchesCount++; keywords.Add(word); break; }
            foreach (string word in IntellectualHints) if (tech.Matches(word)) { intellectualTag = true; matchesCount++; keywords.Add(word); break; }
            foreach (string word in MiningHints) if (tech.Matches(word)) { miningTag = true; matchesCount++; keywords.Add(word); break; }
            foreach (string word in MedicineHints) if (tech.Matches(word)) { medicineTag = true; matchesCount++; keywords.Add(word); break; }
            foreach (string word in PlantsHints) if (tech.Matches(word)) { plantsTag = true; matchesCount++; keywords.Add(word); break; }

            //b. checking by unlocked things
            if (thingDefs.Count() > 0)
            {
                foreach (ThingDef t in thingDefs)
                {
                    if (t != null && InvestigateThingDef(t, tech)) thingsCount++;
                }
            }

            //c. checking by unlocked recipes
            if (recipeDefs.Count() > 0)
            {
                foreach (RecipeDef r in recipeDefs)
                {
                    recipesCount++;
                    foreach (ThingDef t in r.products.Select(x => x.thingDef))
                    {
                        if (InvestigateThingDef(t, tech)) recipeThingsCount++;
                    }
                    if (r.workSkill != null)
                    {
                        AccessTools.Field(typeof(Extension_Research), r.workSkill.defName.ToLower() + "Tag").SetValue(tech, true);
                        recipeSkillsCount++;
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
                    terrainsCount++;
                }
            }

            //e. special cases
            if (HarmonyPatches.RunSpecialCases)
            {
                if (tech.defName.StartsWith("ResearchProject_RotR"))
                {
                    miningTag = true;
                    constructionTag = true;
                }
                if (tech.defName.StartsWith("BackupPower") || tech.defName.StartsWith("FluffyBreakdowns")) constructionTag = true;
                if (tech.defName.StartsWith("OG_"))
                {
                    if (tech.Matches("weapon"))
                    {
                        shootingTag = (meleeTag = (craftingTag = true));
                        keywords.Add("weapon");
                    }
                 }
            }

            //f. If necessary, consider the skills already assigned to its prerequisites 
            if (!(shootingTag || meleeTag || constructionTag || miningTag || cookingTag || plantsTag || animalsTag || craftingTag || artisticTag || medicineTag || socialTag || intellectualTag))
            {
                if (!tech.prerequisites.NullOrEmpty())
                {
                    foreach (ResearchProjectDef prereq in tech.prerequisites)
                    {
                        if (SkillsByTech.ContainsKey(prereq))
                        {
                            foreach (SkillDef skill in SkillsByTech[prereq])
                            {
                                AccessTools.Field(typeof(Extension_Research), skill.defName.ToLower() + "Tag").SetValue(tech, true);
                            }
                            usedPreReq = true;
                        }
                    }
                }
            }

            //3. Figure out Bias.
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

            if (!relevantSkills.NullOrEmpty())
            {
                SkillsByTech.Add(tech, relevantSkills);
                foreach (SkillDef skill in relevantSkills)
                {
                    if (!TechsBySkill.ContainsKey(skill)) TechsBySkill.Add(skill, new List<ResearchProjectDef>() { tech });
                    else TechsBySkill[skill].Add(tech);
                }

                //4. Telling humans what's going on, depending on settings
                if (ModBaseHumanResources.FullStartupReport)
                {
                    StringBuilder report = new StringBuilder();
                    if (matchesCount > 0) { report.Append("keyword" + (matchesCount > 1 ? "s" : "") + ": " + keywords.ToStringSafeEnumerable()); }
                    if (thingsCount > 0) { report.AppendWithComma(thingsCount + " thing" + (thingsCount > 1 ? "s" : "")); }
                    if (recipesCount > 0) 
                    {
                        report.AppendWithComma(recipesCount + " recipe" + (recipesCount > 1 ? "s" : "") + " ");
                        if (recipeThingsCount > 0 || recipeSkillsCount > 0)
                        {
                            StringBuilder recipeSub = new StringBuilder();
                            recipeSub.Append(recipeThingsCount > 0 ? recipeThingsCount + " thing" + (recipeThingsCount > 1 ? "s" : "") : "");
                            recipeSub.AppendWithComma(recipeSkillsCount > 0 ? recipeSkillsCount + " workskill" + (recipeSkillsCount > 1 ? "s" : "") : "");
                            report.Append("(" + recipeSub + ")");
                        }
                        else report.Append("(irrelevant)");
                    }
                    if (terrainsCount > 0) { report.AppendWithComma(terrainsCount + " terrain" + (terrainsCount > 1 ? "s" : "")); }
                    if (usedPreReq) { report.AppendWithComma("follows its pre-requisites"); }
                    string skills = relevantSkills.Count() > 1 ? "Skills are " : "Skill is ";
                    report.Append(". " + skills + relevantSkills.ToStringSafeEnumerable() + ".");
                    Log.Message("[HumanResources] " + tech + ": "+report.ToString(), true);
                }
                else if (usedPreReq) Log.Warning("[HumanResources] No relevant skills could be calculated for " + tech + " so it inherited the ones from its pre-requisites: "+ relevantSkills.ToStringSafeEnumerable() + ".");
            }
            else Log.Warning("[HumanResources] No relevant skills could be calculated for " + tech+". It won't be known by anyone.");
        }

        public static bool InvestigateThingDef(ThingDef thing, ResearchProjectDef tech)
        {
            bool found = false;
            if (thing != null)
            {
                if (!shootingTag) try { shootingTag |= thing.IsRangedWeapon | thing.designationCategory == DesignationCategoryDefOf.Security; found = true; } catch { };
                if (!meleeTag) try { meleeTag |= thing.IsMeleeWeapon | thing.designationCategory == DesignationCategoryDefOf.Security; found = true; } catch { };
                if (!constructionTag) try { constructionTag |= thing.BuildableByPlayer; found = true; } catch { };
                if (!miningTag) try { miningTag |= thing.IsShell; found = true; } catch { };
                if (!cookingTag) try { cookingTag |= thing.ingestible.IsMeal | thing.building.isMealSource; found = true; } catch { };
                if (!plantsTag) try { plantsTag |= thing.plant != null; found = true; } catch { };
                if (!craftingTag) try { craftingTag |= thing.IsApparel | thing.IsWeapon; found = true; } catch { };
                if (!artisticTag) try { artisticTag |= thing.IsArt | thing.IsWithinCategory(ThingCategoryDefOf.BuildingsArt); found = true; } catch { };
                if (!medicineTag) try { medicineTag |= thing.IsMedicine | thing.IsDrug; found = true; } catch { };
                //measures for weapons
                if (thing.IsWeapon && !TechByWeapon.ContainsKey(thing)) tech.RegisterWeapon(thing);
                if (thing.building != null && thing.building.turretGunDef != null && !TechByWeapon.ContainsKey(thing.building.turretGunDef)) tech.RegisterWeapon(thing);
            }
            return found;
        }

        public static void Learned(this ResearchProjectDef tech, float amount, float recipeCost, Pawn researcher, bool research = false)
        {
            float total = research ? tech.baseCost : recipeCost * tech.StuffCostFactor();
            amount *= research ? ResearchPointsPerWorkTick : StudyPointsPerWorkTick;
            CompKnowledge techComp = researcher.TryGetComp<CompKnowledge>();
            Dictionary<ResearchProjectDef, float> expertise = techComp.expertise;
            foreach (ResearchProjectDef sucessor in expertise.Keys.Where(x => x.IsKnownBy(researcher)))
            {
                if (!sucessor.prerequisites.NullOrEmpty() && sucessor.prerequisites.Contains(tech))
                {
                    amount *= 2;
                    break;
                }
            }
            if (researcher != null && researcher.Faction != null)
            {
                amount /= tech.CostFactor(techComp.techLevel);
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
            expertise[tech] = num;
        }

        public static bool Matches(this ResearchProjectDef tech, string query)
        {
            var culture = CultureInfo.CurrentUICulture;
            query = query.ToLower(culture);

            return 
                tech.LabelCap.RawText.ToLower(culture).Contains(query) ||
                ResearchTree_Patches.GetUnlockDefsAndDescs(tech).Any(unlock => unlock.First.LabelCap.RawText.ToLower(culture).Contains(query)) ||
                tech.description.ToLower(culture).Contains(query);
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

        private static void RegisterWeapon(this ResearchProjectDef tech, ThingDef weapon)
        {
            if (ShouldLockWeapon(weapon))
            {
                if (!TechByWeapon.ContainsKey(weapon)) TechByWeapon.Add(weapon, tech);
                if (WeaponsByTech.ContainsKey(tech)) WeaponsByTech[tech].Add(weapon);
                else WeaponsByTech.Add(tech, new List<ThingDef>() { weapon });
            }
        }

        public static List<ThingDef> UnlockedWeapons(this ResearchProjectDef tech)
        {
            return WeaponsByTech.ContainsKey(tech) ? WeaponsByTech[tech] : new List<ThingDef>();
        }

        private static Func<ThingDef, bool> ShouldLockWeapon = (x) =>
        {
            bool basic = x.weaponTags.NullOrEmpty() || x.weaponTags.Any(t => t.Contains("Basic")) || x.weaponTags.Any(tag => TechDefOf.WeaponsAlwaysBasic.weaponTags.Contains(tag));
            bool tool = x.defName.Contains("Tool") || x.defName.Contains("tool");
            bool exempted = x.IsExempted();
            return !basic && !tool && !exempted;
        };

        private static float StuffMarketValueFactor(this ResearchProjectDef tech)
		{
			return (float)Math.Round(Math.Pow(tech.baseCost, 1.0 / 3.0) / 10, 1);
		}

        public static List<Pawn> currentPawnsCache;

        private static IEnumerable<Pawn> currentPawns
        {
            get
            {
                if (currentPawnsCache.NullOrEmpty()) currentPawnsCache = HarmonyPatches.PrisonLabor? PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive.Where(x => x.TechBound()).ToList() : PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists.Where(x => x.TryGetComp<CompKnowledge>() != null).ToList();
                return currentPawnsCache;
            }  
        }            

        public static bool IsKnownBy(this ResearchProjectDef tech, Pawn pawn)
        {
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            var expertise = techComp.expertise;
            if (expertise != null) return expertise.ContainsKey(tech) && techComp.expertise[tech] >= 1f;
            return false;
        }

        public static bool RequisitesKnownBy(this ResearchProjectDef tech, Pawn pawn)
        {
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            var expertise = techComp.expertise;
            if (expertise != null)
            {
                //1. test if any descendent is known
                if (expertise.Where(x => x.Value >= 1 && !x.Key.prerequisites.NullOrEmpty() && x.Key.prerequisites.Contains(tech)).Any()) return true;
                //2. test if all ancestors are known
                if (!tech.prerequisites.NullOrEmpty()) return tech.prerequisites.All(x => x.IsKnownBy(pawn));
            }
            //3. test if there are any ancestors
            return tech.prerequisites.NullOrEmpty();
        }

        private static IEnumerable<Pawn> SortedPawns(this ResearchProjectDef tech)
        {
            if (tech.IsFinished) return currentPawns.Where(x => !tech.IsKnownBy(x)).OrderBy(x => x.workSettings.WorkIsActive(TechDefOf.HR_Learn)).ThenByDescending(x => x.skills.GetSkill(SkillDefOf.Intellectual).Level);
            else return currentPawns.OrderBy(x => tech.IsKnownBy(x))/*.ThenBy(x => x.workSettings.WorkIsActive(WorkTypeDefOf.Research)).ThenByDescending(x => x.skills.GetSkill(SkillDefOf.Intellectual).Level)*/;
        }

        private static IEnumerable<Widgets.DropdownMenuElement<Pawn>> GeneratePawnRestrictionOptions(this ResearchProjectDef tech, bool completed)
        {
            SkillDef skill = SkillDefOf.Intellectual;
            using (IEnumerator<Pawn> enumerator = tech.SortedPawns().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Pawn pawn = enumerator.Current;
                    CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
                    bool known = tech.IsKnownBy(pawn);
                    WorkTypeDef workGiver = (completed || known) ? TechDefOf.HR_Learn : WorkTypeDefOf.Research;
                    string header = TechStrings.GetTask(pawn, tech);
                    if (techComp != null && (techComp.homework.NullOrEmpty() || !techComp.homework.Contains(tech))) 
                    {
                        if (pawn.WorkTypeIsDisabled(workGiver))
                        {
                            yield return new Widgets.DropdownMenuElement<Pawn>
                            {
                                option = new FloatMenuOption(string.Format("{0}: {1} ({2})", header, pawn.LabelShortCap, "WillNever".Translate(workGiver.verb)), null, MenuOptionPriority.DisabledOption, null, null, 0f, null, null),
                                payload = pawn
                            };
                        }
                        else if (!pawn.workSettings.WorkIsActive(workGiver))
                        {
                            yield return new Widgets.DropdownMenuElement<Pawn>
                            {
                                option = new FloatMenuOption(string.Format("{0}: {1} ({2})", header, pawn.LabelShortCap, "NotAssigned".Translate()), delegate ()
                                {
                                    techComp.AssignBranch(tech);
                                }, MenuOptionPriority.VeryLow, null, null, 0f, null, null),
                                payload = pawn
                            };
                        }
                        else
                        {
                            yield return new Widgets.DropdownMenuElement<Pawn>
                            {
                                option = new FloatMenuOption(string.Format("{0}: {1} ({2} {3})", new object[]
                                {
                                    header,
                                    pawn.LabelShortCap,
                                    pawn.skills.GetSkill(skill).Level,
                                    skill.label
                                }),
                                delegate () { techComp.AssignBranch(tech); },
                                MenuOptionPriority.Default, null, null, 0f, null, null),
                                payload = pawn
                            };
                        }
                    }
                }
            }
            yield break;
        }

        public static void SelectMenu(this ResearchProjectDef tech, bool completed)
        {
            Find.WindowStack.FloatMenu?.Close(false);
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            if (tech.TechprintRequirementMet)
            {
                options = (from opt in GeneratePawnRestrictionOptions(tech, completed)
                           select opt.option).ToList<FloatMenuOption>();
            }
            else
            {
                options.Add(new FloatMenuOption("InsufficientTechprintsApplied".Translate(tech.TechprintsApplied, tech.TechprintCount), null)
                {
                    Disabled = true,
                });
            }
            if (!options.Any()) options.Add(new FloatMenuOption("NoOptionsFound".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        public static void DrawAssignments(this ResearchProjectDef tech, Rect rect)
        {
            float height = rect.height;
            float frameOffset = height / 4;
            float startPos = rect.x - frameOffset; //rect.xMax - height/2;
            Vector2 size = new Vector2(height, height);
            using (IEnumerator<Pawn> enumerator = currentPawns.Where(p => HasBeenAssigned(p,tech)).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Vector2 position = new Vector2(startPos, rect.y + (height / 3));
                    Rect box = new Rect(position, size);
                    Rect clickBox = new Rect(position.x + frameOffset, position.y, size.x - (2 * frameOffset), size.y);
                    Pawn pawn = enumerator.Current;
                    GUI.DrawTexture(box, PortraitsCache.Get(pawn, size, default, 1.2f));
                    if (Widgets.ButtonInvisible(clickBox)) pawn.TryGetComp<CompKnowledge>().CancelBranch(tech);
                    TooltipHandler.TipRegion(clickBox, new Func<string>(() => AssignmentStatus(pawn, tech)), tech.GetHashCode());
                    startPos += height / 2;
                }
            }
        }

        private static Func<Pawn, ResearchProjectDef, string> AssignmentStatus = (pawn, tech) =>
        {
            string status = "error";
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            if (techComp != null && !techComp.homework.NullOrEmpty())
            {
                if (tech.IsKnownBy(pawn)) status = "AssignedToDocument".Translate(pawn);
                else status = tech.IsFinished ? "AssignedToStudy".Translate(pawn) : "AssignedToResearch".Translate(pawn);
            }
            return status + " (" + "ClickToRemove".Translate() + ")";
        };

        private static Func<Pawn, ResearchProjectDef, bool> HasBeenAssigned = (pawn, tech) =>
        {
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            if (techComp != null && !techComp.homework.NullOrEmpty())
            {
                return techComp.homework.Contains(tech);
            }
            return false;
        };
    }
}
