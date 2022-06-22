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
    //Changed in RW 1.3

    using static ModBaseHumanResources;
    using static TechTracker;
    using static ResearchTreeHelper;
    using static ResearchTree_Patches;

    public static class Extension_Research
    {
        #region variables
        public static List<Pawn> currentPawnsCache;
        public static FieldInfo progressInfo = AccessTools.Field(typeof(ResearchManager), "progress");
        private const float MarketValueOffset = 200f;
        private static FieldInfo ResearchPointsPerWorkTickInfo = AccessTools.Field(typeof(ResearchManager), "ResearchPointsPerWorkTick");
        #endregion

        #region functions
        private static Func<Pawn, ResearchProjectDef, string> AssignmentStatus = (pawn, tech) =>
        {
            string status = "error";
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            if (techComp != null && !techComp.homework.NullOrEmpty())
            {
                if (tech.IsKnownBy(pawn)) status = "AssignedToDocument".Translate(pawn);
                else status = tech.IsFinished ? "AssignedToStudy".Translate(pawn) : "AssignedToResearch".Translate(pawn);
            }
            return $"{status} ({"ClickToRemove".Translate()})";
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

        private static Func<ThingDef, bool> ShouldLockWeapon = (x) =>
        {
            bool basic = x.weaponTags.NullOrEmpty() || x.weaponTags.Any(t => t.Contains("Basic"));
            bool tool = x.defName.ToLower().Contains("tool");
            return !basic && !tool;
        };
        #endregion

        #region properties
        private static IEnumerable<Pawn> currentPawns
        {
            get
            {
                if (currentPawnsCache.NullOrEmpty()) currentPawnsCache = HarmonyPatches.PrisonLabor ? PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive.Where(x => x.TechBound()).ToList() : PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists.Where(x => x.TryGetComp<CompKnowledge>() != null).ToList();
                return currentPawnsCache;
            }
        }
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
                return ResearchSpeedTiedToDifficulty ? baseValue * DifficultyResearchSpeedFactor : baseValue * 0.9f;
            }
        }
        private static float StudyPointsPerWorkTick // 100% on easy, 90% on medium, 85% on rough & 80% on hard.
        {
            get
            {
                float baseValue = 1.1f;
                return StudySpeedTiedToDifficulty? baseValue * DifficultyResearchSpeedFactor : baseValue;
            }
        }

        #endregion

        #region startup
        public static void CreateStuff(this ResearchProjectDef tech, ThingFilter filter, UnlockManager unlocked)
        {
            string name = "Tech_" + tech.defName;
            ThingCategoryDef tCat = DefDatabase<ThingCategoryDef>.GetNamed(tech.techLevel.ToString());
            StuffCategoryDef sCat = DefDatabase<StuffCategoryDef>.GetNamed(tech.techLevel.ToString());
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
                    categories = new List<StuffCategoryDef>() { sCat },
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
            FindTech(tech).Stuff = techStuff;
            totalBooks++;
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
            List<Pair<Def, string>> unlocks = GetUnlockDefsAndDescs(tech);
            IEnumerable<Def> defs = unlocks.Select(x => x.First).AsEnumerable();
            IEnumerable<ThingDef> thingDefs = defs.Where(d => d is ThingDef).Select(d => d as ThingDef);
            IEnumerable<RecipeDef> recipeDefs = defs.Where(d => d is RecipeDef).Select(d => d as RecipeDef);
            IEnumerable<TerrainDef> terrainDefs = defs.Where(d => d is TerrainDef).Select(d => d as TerrainDef);

            //2. look for skills based on unlocked stuff

            //a. checking by query on the research tree
            matchesCount = tech.CheckKeywordsFor(keywords);

            //b. checking by unlocked things
            if (thingDefs.Count() > 0)
            {
                thingsCount = tech.CheckUnlockedThings(thingDefs);
            }

            //c. checking by unlocked recipes
            if (recipeDefs.Count() > 0)
            {
                recipesCount = tech.CheckUnlockedRecipes(ref recipeThingsCount, ref recipeSkillsCount, recipeDefs);
            }

            //d. checking by unlocked terrainDefs
            if (terrainDefs.Count() > 0)
            {
                terrainsCount = CheckUnlockedTerrains(terrainDefs);
            }

            //e. special cases
            if (HarmonyPatches.RunSpecialCases)
            {
                tech.CheckSpecialCases(keywords);
            }

            //f. If necessary, consider the skills already assigned to its prerequisites 
            if (!FindSkills(x => x.relevant).Any())
            {
                usedPreReq = tech.CopyFromPreReq();
            }

            //3. Figure out Bias.
            List<SkillDef> relevantSkills = GetSkillBiasAndReset().ToList();
            bool success = !relevantSkills.NullOrEmpty();
            if (success)
            {
                LinkTechAndSkills(tech, relevantSkills);
            }

            //4. Telling humans what's going on, depending on the settings
            StringBuilder report = new StringBuilder();
            if (Prefs.LogVerbose)
            {
                if (!success || usedPreReq) GatherNoMatchesDetails(thingDefs.EnumerableCount(), recipeDefs.EnumerableCount(), terrainDefs.EnumerableCount(), report);
                if (success && FullStartupReport)
                {
                    GatherSuccessDetails(tech, matchesCount, thingsCount, recipesCount, recipeThingsCount, recipeSkillsCount, terrainsCount, keywords, usedPreReq, relevantSkills, report);
                    Log.Message($"{tech.LabelCap}: {report}", true);
                }
            }
            NoMatchesWarning(tech, usedPreReq, relevantSkills, success, report);
        }

        public static bool InvestigateThingDef(ThingDef thing, ResearchProjectDef tech)
        {
            bool found = false;
            if (thing != null)
            {
                foreach (var skill in FindSkills(x => !x && x.Criteria != null))
                {
                    bool check = false;
                    try { check = skill.Criteria(thing); } catch { }
                    SetSkillRelevance(skill, check);
                    found |= check; 
                }
                WeaponsRegistration(thing, tech);
            }
            return found;
        }

        private static void WeaponsRegistration(ThingDef thing, ResearchProjectDef tech)
        {
            if (thing.IsWeapon) tech.RegisterWeapon(thing);
            ThingDef turretGun = thing.GetTurretGun();
            if (turretGun != null && MountedWeapons.Contains(turretGun)) tech.RegisterWeapon(turretGun);
        }

        public static bool Matches(this ResearchProjectDef tech, string query)
        {
            var culture = CultureInfo.CurrentUICulture;
            if (culture == null) return false;
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

        private static int CheckKeywordsFor(this ResearchProjectDef tech, List<string> keywords)
        {
            var matches = 0;
            foreach (var skill in FindSkills(x => !x.Hints.NullOrEmpty()))
            {
                foreach (var word in skill.Hints)
                {
                    if (tech.Matches(word))
                    {
                        SetSkillRelevance(skill, true);
                        matches++;
                        keywords.Add(word);
                        break;
                    }
                }
            }
            return matches;
        }

        private static void CheckSpecialCases(this ResearchProjectDef tech, List<string> keywords)
        {
            if (tech.defName.StartsWith("ResearchProject_RotR"))
            {
                SetSkillRelevance(SkillDefOf.Mining, true);
                SetSkillRelevance(SkillDefOf.Construction, true);
            }
            if (tech.defName.StartsWith("BackupPower") || tech.defName.StartsWith("FluffyBreakdowns")) SetSkillRelevance(SkillDefOf.Construction, true);
            if (tech.defName.StartsWith("OG_"))
            {
                if (tech.Matches("weapon"))
                {
                    SetSkillRelevance(SkillDefOf.Shooting, true);
                    SetSkillRelevance(SkillDefOf.Melee, true);
                    SetSkillRelevance(SkillDefOf.Crafting, true);
                    keywords.Add("weapon");
                }
            }
            var VFE = LoadedModManager.RunningMods.Where(x => x.PackageIdPlayerFacing.StartsWith("VanillaExpanded.VFEArt"));
            if (!VFE.EnumerableNullOrEmpty() && VFE.First().AllDefs.Contains(tech))
            {
                SetSkillRelevance(SkillDefOf.Artistic, true);
            }
        }

        private static int CheckUnlockedRecipes(this ResearchProjectDef tech, ref int recipeThingsCount, ref int recipeSkillsCount, IEnumerable<RecipeDef> recipeDefs)
        {
            int count = 0;
            foreach (RecipeDef r in recipeDefs)
            {
                count++;
                foreach (ThingDef t in r.products.Select(x => x.thingDef))
                {
                    if (InvestigateThingDef(t, tech)) recipeThingsCount++;
                }
                if (r.workSkill != null)
                {
                    SetSkillRelevance(r.workSkill, true);
                    recipeSkillsCount++;
                }
            }
            return count;
        }

        private static int CheckUnlockedTerrains(IEnumerable<TerrainDef> terrainDefs)
        {
            int count = 0;
            foreach (TerrainDef t in terrainDefs)
            {
                if (!GetSkillRelevance(SkillDefOf.Construction) && t.designationCategory != null && t.designationCategory.label.Contains("floor")) SetSkillRelevance(SkillDefOf.Construction, true);
                else if (!GetSkillRelevance(SkillDefOf.Mining)) SetSkillRelevance(SkillDefOf.Mining, true);
                count++;
            }
            return count;
        }

        private static int CheckUnlockedThings(this ResearchProjectDef tech, IEnumerable<ThingDef> thingDefs)
        {
            int count = 0;
            foreach (ThingDef t in thingDefs)
            {
                if (t != null && InvestigateThingDef(t, tech)) count++;
            }
            return count;
        }

        private static bool CopyFromPreReq(this ResearchProjectDef tech)
        {
            if (tech.prerequisites.NullOrEmpty()) return false;
            bool applied = false;
            foreach (ResearchProjectDef prereq in tech.prerequisites)
            {
                foreach (var skill in FindTech(prereq).Skills)
                {
                    SetSkillRelevance(skill, true);
                }
                applied = true;
            }
            return applied;
        }

        private static void GatherNoMatchesDetails(int things, int recipes, int terrains, StringBuilder report)
        {
            if (things + recipes + terrains == 0) return;
            StringBuilder sub = new StringBuilder();
            if (things > 0) { sub.AppendWithComma(things + " thing" + (things > 1 ? "s" : "")); }
            if (recipes > 0) { sub.AppendWithComma($"{recipes} recipe{(recipes > 1 ? "s" : "")} "); }
            if (terrains > 0) { sub.AppendWithComma(terrains + " terrain" + (terrains > 1 ? "s" : "")); }
            report.Append($" (Unlocks {sub}).");
        }

        private static void GatherSuccessDetails(ResearchProjectDef tech, int matchesCount, int thingsCount, int recipesCount, int recipeThingsCount, int recipeSkillsCount, int terrainsCount, List<string> keywords, bool usedPreReq, List<SkillDef> relevantSkills, StringBuilder report)
        {
            string appendReport = report.Length > 0 ? report.ToString() : "";
            report.Clear();
            if (matchesCount > 0) { report.Append($"keyword{(matchesCount > 1 ? "s" : "")}: {keywords.ToStringSafeEnumerable()}"); }
            if (thingsCount > 0) { report.AppendWithComma(thingsCount + " thing" + (thingsCount > 1 ? "s" : "")); }
            int weaponsCount = tech.UnlockedWeapons().Count;
            if (weaponsCount > 0) { report.AppendWithComma($"{weaponsCount} weapon{(matchesCount > 1 ? "s" : "")} ({tech.UnlockedWeapons().ToStringSafeEnumerable()})"); }
            if (recipesCount > 0)
            {
                report.AppendWithComma($"{recipesCount} recipe{(recipesCount > 1 ? "s" : "")} ");
                StringBuilder recipeSub = new StringBuilder();
                if (recipeThingsCount > 0) recipeSub.Append($"{recipeThingsCount} thing{(recipeThingsCount > 1 ? "s" : "")}");
                if (recipeSkillsCount > 0) recipeSub.AppendWithComma($"{recipeSkillsCount}  workskill{(recipeSkillsCount > 1 ? "s" : "")}");
                if (recipeSub.Length > 0) report.Append($"({recipeSub})");
                else report.Append("(irrelevant)");
            }
            if (terrainsCount > 0) { report.AppendWithComma(terrainsCount + " terrain" + (terrainsCount > 1 ? "s" : "")); }
            if (usedPreReq) { report.AppendWithComma("follows its pre-requisites"); }
            report.Append($". {(relevantSkills.Count() > 1 ? "Skills are" : "Skill is")} {relevantSkills.ToStringSafeEnumerable()}.");
            if (usedPreReq) report.Append(appendReport);
        }

        private static void NoMatchesWarning(ResearchProjectDef tech, bool usedPreReq, List<SkillDef> relevantSkills, bool success, StringBuilder report)
        {
            string appendReport = report.Length > 0 ? report.ToString() : "";
            if (!success)
            {
                Log.Warning($"[HumanResources] No relevant skills could be calculated for {tech}. It won't be known by anyone.{appendReport}");
                return;
            }
            if (usedPreReq && Prefs.LogVerbose && !FullStartupReport)
            {
                Log.Warning($"[HumanResources] No relevant skills could be calculated for {tech} so it inherited the ones from its pre-requisites: {relevantSkills.ToStringSafeEnumerable()}.{appendReport}");
            }
        }

        private static void RegisterWeapon(this ResearchProjectDef tech, ThingDef weapon)
        {
            if (ShouldLockWeapon(weapon))
            {
                FindTech(tech).Weapons.Add(weapon);
            }
        }
        private static float StuffMarketValueFactor(this ResearchProjectDef tech)
        {
			return (float)Math.Round(Math.Pow(tech.baseCost, 1.0 / 3.0) / 10, 1);
		}

        #endregion

        #region research tree
        public static void DrawExtras(this ResearchProjectDef tech, Rect rect, bool highlighted)
        {
            DrawStorageMarker(tech, rect, highlighted);
            float height = rect.height;
            Vector2 frameOffset = new Vector2(height / 3, rect.y + (height / 3));
            float startPos = rect.x - frameOffset.x;
            if (QueueAvailable && IsQueued(tech)) startPos += DrawQueueAssignment(tech, VFE_Supercomputer, height, frameOffset, startPos);
            tech.DrawPawnAssignments(height, frameOffset, startPos);
        }

        public static void SelectMenu(this ResearchProjectDef tech, bool completed, bool outsideTree = false)
        {
            Find.WindowStack.FloatMenu?.Close(false);
            if (HarmonyPatches.SemiRandom && !completed && !outsideTree) return;
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            if (tech.TechprintRequirementMet)
            {
                options = (from opt in GeneratePawnRestrictionOptions(tech, completed)
                           select opt.option).ToList();
                if (QueueAvailable && !IsQueued(tech)) options.Add(SelectforVanillaResearch(tech));
            }
            else
            {
                int count = ModLister.RoyaltyInstalled ? tech.techprintCount : 0; //not using 1.2's property to maintain 1.1 compatibility
                options.Add(new FloatMenuOption("InsufficientTechprintsApplied".Translate(tech.TechprintsApplied, count), null)
                {
                    Disabled = true,
                });
            }
            if (!options.Any()) options.Add(new FloatMenuOption("NoOptionsFound".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        public static void DrawPawnAssignments(this ResearchProjectDef tech, float height, Vector2 frameOffset, float startPos, bool reverse = false)
        {
            Vector2 position;
            Vector2 size = new Vector2(height, height);
            float spacing = height / 2;
            using (IEnumerator<Pawn> enumerator = currentPawns.Where(p => HasBeenAssigned(p, tech)).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    position = new Vector2(startPos, frameOffset.y);
                    Rect box = new Rect(position, size);
                    Rect clickBox = new Rect(position.x + frameOffset.x, position.y, size.x - (2 * frameOffset.x), size.y);
                    Pawn pawn = enumerator.Current;
                    GUI.DrawTexture(box, PortraitsCache.Get(pawn, size, default, 1.2f));
                    if (Widgets.ButtonInvisible(clickBox)) pawn.TryGetComp<CompKnowledge>().CancelBranch(tech);
                    TooltipHandler.TipRegion(clickBox, new Func<string>(() => AssignmentStatus(pawn, tech)), tech.GetHashCode());
                    startPos = reverse ? startPos - spacing : startPos + spacing;
                }
            }
        }

        private static float DrawQueueAssignment(ResearchProjectDef tech, ThingDef thingDef, float height, Vector2 frameOffset, float startPos)
        {
            Vector2 position;
            Vector2 size = new Vector2(height, height);
            position = new Vector2(startPos, frameOffset.y);
            Rect box = new Rect(position, size);
            Rect clickBox = new Rect(position.x + frameOffset.x, position.y, size.x - (2 * frameOffset.x), size.y);
            GUI.DrawTexture(box, thingDef.uiIcon);
            if (Widgets.ButtonInvisible(clickBox)) Dequeue(tech);
            TooltipHandler.TipRegion(clickBox, $"{"AssignedToResearch".Translate(thingDef.LabelCap)}\n({"ClickToRemove".Translate()})");
            return height / 2;
        }

        private static void DrawStorageMarker(ResearchProjectDef tech, Rect rect, bool highlighted)
        {
            float height = rect.height;
            float ribbon = ResearchTree_Constants.push.x;
            Vector2 position = new Vector2(rect.xMax, rect.y);
            Color techColor = ResearchTree_Assets.ColorCompleted[tech.techLevel];
            Color shadedColor = highlighted ? ResearchTree_Patches.ShadedColor : ResearchTree_Assets.ColorAvailable[tech.techLevel];
            Color backup = GUI.color;
            if (unlocked.TechsArchived.ContainsKey(tech))
            {
                bool cloud = tech.IsOnline();
                bool book = tech.IsPhysicallyArchived();
                bool twin = cloud && book;
                Vector2 markerSize = new Vector2(ribbon, height);
                Rect box = new Rect(position, markerSize);
                Rect inner = box;
                inner.height = ribbon;
                if (twin)
                {
                    inner.y -= height * 0.08f;
                }
                else
                {
                    inner.y += (height - ribbon) / 2;
                }
                Widgets.DrawBoxSolid(box, shadedColor);
                if (cloud)
                {
                    GUI.DrawTexture(inner.ContractedBy(1f), ContentFinder<Texture2D>.Get("UI/cloud", true));
                    TooltipHandler.TipRegionByKey(inner, "bookInDatabase");
                }
                if (book)
                {
                    if (twin)
                    {
                        float reduction = 0.9f;
                        inner.width *= reduction;
                        inner.height *= reduction;
                        inner.y = box.yMax - inner.height - 1f;
                        inner.x += (ribbon - inner.width) / 2;
                    }
                    var material = TechDefOf.TechBook.graphic.MatSingle;
                    material.color = techColor;
                    Graphics.DrawTexture(inner.ContractedBy(1f), ContentFinder<Texture2D>.Get("Things/Item/book", true), material, 0);
                    TooltipHandler.TipRegionByKey(inner, "bookInLibrary");
                }
            }
            //origin tooltip if necessary
            else if (tech.IsFinished)
            {
                bool fromScenario = unlocked.scenarioTechs.Contains(tech);
                bool fromFaction = unlocked.factionTechs.Contains(tech);
                bool startingTech = fromScenario || fromFaction;
                string source = fromScenario ? Find.Scenario.name : Find.FactionManager.OfPlayer.Name;
                TooltipHandler.TipRegionByKey(rect, "bookFromStart", source);
            }
            GUI.color = backup;
            return;
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
                    WorkTypeDef workType = (completed || known) ? TechDefOf.HR_Learn : WorkTypeDefOf.Research;
                    string header = TechStrings.GetTask(pawn, tech);
                    if (techComp != null && (techComp.homework.NullOrEmpty() || !techComp.homework.Contains(tech)))
                    {
                        if (pawn.WorkTypeIsDisabled(workType))
                        {
                            yield return new Widgets.DropdownMenuElement<Pawn>
                            {
                                option = new FloatMenuOption(string.Format("{0}: {1} ({2})", header, pawn.LabelShortCap, "WillNever".Translate(workType.labelShort.ToLower())), null, MenuOptionPriority.DisabledOption, null, null, 0f, null, null),
                                payload = pawn
                            };
                        }
                        else if (!pawn.workSettings.WorkIsActive(workType))
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

        private static IEnumerable<Pawn> SortedPawns(this ResearchProjectDef tech)
        {
            if (tech.IsFinished) return currentPawns.Where(x => !tech.IsKnownBy(x)).OrderBy(x => x.workSettings.WorkIsActive(TechDefOf.HR_Learn)).ThenByDescending(x => x.skills.GetSkill(SkillDefOf.Intellectual).Level);
            else return currentPawns.OrderBy(x => tech.IsKnownBy(x))/*.ThenBy(x => x.workSettings.WorkIsActive(WorkTypeDefOf.Research)).ThenByDescending(x => x.skills.GetSkill(SkillDefOf.Intellectual).Level)*/;
        }
        #endregion

        #region operational
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
            project.WipeAssignments();
        }

        public static void WipeAssignments(this ResearchProjectDef project)
        {
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

        public static void Ejected(this ResearchProjectDef tech, Thing place, bool hardCopy)
        {
            if (!tech.HasBackup(hardCopy))
            {
                Dictionary<ResearchProjectDef, float> progress = (Dictionary<ResearchProjectDef, float>)progressInfo.GetValue(Find.ResearchManager);
                progress[tech] = 0f;
                unlocked.TechsArchived.Remove(tech);
                Messages.Message("MessageEjectedTech".Translate(tech.label), place, MessageTypeDefOf.TaskCompletion, true);
            }
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

        public static bool HasBackup(this ResearchProjectDef tech, bool hardCopy)
        {
            return hardCopy ? tech.IsOnline() : tech.IsPhysicallyArchived();
        }

        public static float IndividualizedCost(this ResearchProjectDef tech, TechLevel techLevel, float achieved, bool knownSucessor = false)
        {
            float cost = tech.baseCost * tech.CostFactor(techLevel);
            cost *= 1 - achieved;
            if (knownSucessor) cost /= 2;
            return cost;
        }

        public static string IndividualizedCostExplainer(this ResearchProjectDef tech, TechLevel techLevel, float achieved, float finalValue, bool knownSucessor = false)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine($"Base cost: {tech.baseCost.ToStringByStyle(ToStringStyle.Integer)}");
            text.AppendLine($"Personal tech level ({techLevel.ToStringHuman()}): x{tech.CostFactor(techLevel).ToStringPercent()}");
            if (knownSucessor) text.AppendLine($"Known branching tech: x{0.5f.ToStringPercent()}");
            string costLabel = "Cost to learn";
            if (achieved > 0)
            {
                text.AppendLine($"Learned: {achieved.ToStringPercent()}");
                costLabel = "Remaining cost to learn";
            }
            text.Append($"{costLabel}: {finalValue.ToStringByStyle(ToStringStyle.Integer)}");
            return text.ToString();
        }

        public static bool IsKnownBy(this ResearchProjectDef tech, Pawn pawn)
        {
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            var expertise = techComp.expertise;
            if (expertise != null) return expertise.ContainsKey(tech) && techComp.expertise[tech] >= 1f;
            return false;
        }

        public static bool IsOnline(this ResearchProjectDef tech)
        {
            return unlocked.TechsArchived.ContainsKey(tech) && unlocked.TechsArchived[tech] != BackupState.physical;
        }
        
        public static bool IsPhysicallyArchived(this ResearchProjectDef tech)
        {
            return unlocked.TechsArchived.ContainsKey(tech) && unlocked.TechsArchived[tech] != BackupState.digital;
        }

        public static void Learned(this ResearchProjectDef tech, float amount, float recipeCost, Pawn researcher, bool research = false)
        {
            float total = research ? tech.baseCost : recipeCost * tech.StuffCostFactor();
            amount *= research ? ResearchPointsPerWorkTick : StudyPointsPerWorkTick;
            amount *= researcher.GetStatValue(StatDefOf.GlobalLearningFactor, true); //Because, why not?
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

        public static bool RequisitesKnownBy(this ResearchProjectDef tech, Pawn pawn)
        {
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            var expertise = techComp.expertise;
            if (expertise != null)
            {
                //1. test if any descendent is known
                if (expertise.Any(x => x.Value >= 1 && !x.Key.prerequisites.NullOrEmpty() && x.Key.prerequisites.Contains(tech))) return true;
                //2. test if all ancestors are known
                if (!tech.prerequisites.NullOrEmpty()) return tech.prerequisites.All(x => x.IsKnownBy(pawn));
            }
            //3. test if there are any ancestors
            return tech.prerequisites.NullOrEmpty();
        }

        public static void Unlock(this ResearchProjectDef tech, Thing location, bool hardCopy)
        {
            unlocked.Archive(tech, hardCopy);
            if (!tech.IsFinished) tech.CarefullyFinishProject(location);
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
            return FindTech(tech).Weapons;
        }

        public static void Uploaded(this ResearchProjectDef tech, float amount, Thing location)
        {
            if (tech == null)
            {
                Log.Error("Tried to upload a null tech.", false);
                return;
            }
            float num = Find.ResearchManager.GetProgress(tech);
            num += amount;
            Dictionary<ResearchProjectDef, float> progress = (Dictionary<ResearchProjectDef, float>)progressInfo.GetValue(Find.ResearchManager);
            progress[tech] = num;
            if (tech.IsFinished) tech.Unlock(location, false);
        }

        #endregion
    }
}
