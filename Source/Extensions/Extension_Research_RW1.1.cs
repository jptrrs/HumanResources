using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection;
using System.Globalization;
using UnityEngine;

namespace HumanResources
{
    public static class Extension_Research
    {
        public static SkillDef Bias = new SkillDef();
        public static Dictionary<ResearchProjectDef, List<SkillDef>> SkillsByTech = new Dictionary<ResearchProjectDef, List<SkillDef>>();
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
            //int matches = 0;
            if (tech.Matches("scanner") > 0 | tech.Matches("terraform") > 0) { miningTag = true; /*matches++;*/ };

            if (tech.Matches("sterile") > 0 | tech.Matches("medical") > 0 | tech.Matches("medicine") > 0 | tech.Matches("cryptosleep") > 0 | tech.Matches("prostheses") > 0 | tech.Matches("implant") > 0 | tech.Matches("organs") > 0 | tech.Matches("surgery") > 0) { medicineTag = true; /*matches++;*/ };

            if (tech.Matches("irrigation") > 0 | tech.Matches("soil") > 0 | tech.Matches("hydroponic") > 0) { plantsTag = true; /*matches++;*/ };

            if (tech.Matches("tool") > 0) { craftingTag = true; }

            if (tech.Matches("manage") > 0) { intellectualTag = true; }

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

            //e. special cases
            if (HarmonyPatches.RunSpecialCases)
            {
                if (tech.defName.StartsWith("ResearchProject_RotR"))
                {
                    miningTag = true;
                    constructionTag = true;
                }
                if (tech.defName.StartsWith("BackupPower") || tech.defName.StartsWith("FluffyBreakdowns")) constructionTag = true;
            }

            //3. Figure out Bias.
            //int ThingDefCount = thingDefs.Count();
            //int RecipeDefCount = recipeDefs.Count();
            //int TerrainDefCount = terrainDefs.Count();

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
                if (Prefs.LogVerbose) Log.Message("[HumanResources] " + tech + " associated to "+relevantSkills.ToStringSafeEnumerable()+".");
            }
            else
            {
                Log.Warning("[HumanResources] No relevant skills could be calculated for " + tech+". It won't be known by anyone.");
            }
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

        public static void LearnInstantly(this ResearchProjectDef tech, Pawn researcher)
        {
            Dictionary<ResearchProjectDef, float> expertise = researcher.TryGetComp<CompKnowledge>().expertise;
            if (expertise != null)
            {
                if (!expertise.ContainsKey(tech)) expertise.Add(tech, 1f);
                else expertise[tech] = 1f;
                Messages.Message("MessageStudyComplete".Translate(researcher, tech.LabelCap), researcher, MessageTypeDefOf.TaskCompletion, true);
            }
            else Log.Warning("[HumanResources] " + researcher + " tried to learn a technology without being able to.");
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
                foreach (ThingDef weapon in r.products.Select(x => x.thingDef).Where(x => ShouldLockWeapon(x)))
                {
                    result.Add(weapon);
                    if (!TechByWeapon.ContainsKey(weapon)) TechByWeapon.Add(weapon, tech);
                }
            }
            foreach (ThingDef weapon in tech.GetThingsUnlocked().Where(x => x.building != null && x.building.turretGunDef != null).Select(x => x.building.turretGunDef))
            {
                result.Add(weapon);
                if (!TechByWeapon.ContainsKey(weapon)) TechByWeapon.Add(weapon, tech);
            }
            if (!result.NullOrEmpty() && (!WeaponsByTech.ContainsKey(tech) || WeaponsByTech[tech].EnumerableNullOrEmpty())) WeaponsByTech.Add(tech, result);
            return result;
        }

        private static Func<ThingDef, bool> ShouldLockWeapon = (x) =>
        {
            bool basic = x.weaponTags.NullOrEmpty() || x.weaponTags.Any(t => t.Contains("Basic")) || x.weaponTags.Any(tag => TechDefOf.WeaponsAlwaysBasic.weaponTags.Contains(tag));
            bool tool = x.defName.Contains("Tool") || x.defName.Contains("tool");
            bool exempted = x.IsExempted();
            return !basic && !tool && !exempted;
        };

        //higher tech level
        private static float StuffMarketValueFactor(this ResearchProjectDef tech)
		{
			return (float)Math.Round(Math.Pow(tech.baseCost, 1.0 / 3.0) / 10, 1);
		}

        public static List<Pawn> currentPawnsCache;

        private static IEnumerable<Pawn> currentPawns //=> HarmonyPatches.PrisonLabor ? PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive.Where(x => x.CanContribute() && x.TryGetComp<CompKnowledge>() != null) : PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists.Where(x => x.TryGetComp<CompKnowledge>() != null);
        {
            get
            {
                if (currentPawnsCache.NullOrEmpty()) currentPawnsCache = HarmonyPatches.PrisonLabor? PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive.Where(x => x.CanContribute() && x.TryGetComp<CompKnowledge>() != null).ToList() : PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists.Where(x => x.TryGetComp<CompKnowledge>() != null).ToList();
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
                    string header = known ? TechStrings.headerWrite : completed ? TechStrings.headerRead : TechStrings.headerResearch;
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
                options.Add(new FloatMenuOption("InsufficientTechprintsApplied".Translate(tech.TechprintsApplied, tech.techprintCount), null)
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
                    Pawn pawn = enumerator.Current;
                    GUI.DrawTexture(box, PortraitsCache.Get(pawn, size, default, 1.2f));
                    Rect clickBox = new Rect(position.x + frameOffset, position.y, size.x - (2 * frameOffset), size.y);
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
