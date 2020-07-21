using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using UnityEngine;
using Verse;

namespace HumanResources
{
    public static class ResearchTree_Patches
    {
        private static string ModName = "";
        private static NotImplementedException stubMsg = new NotImplementedException("ResearchTree reverse patch");

        public static Type TestType;
        public static Type ResearchNodeType() => AccessTools.TypeByName(ModName + ".ResearchNode");
        public static Type AssetsType() => AccessTools.TypeByName(ModName + ".Assets");
        public static Type TreeType() => AccessTools.TypeByName(ModName + ".Tree");
        public static Type NodeType() => AccessTools.TypeByName(ModName + ".Node");
        public static Type DummyNodeType() => AccessTools.TypeByName(ModName + ".DummyNode");
        public static Type MainTabType() => AccessTools.TypeByName(ModName + ".MainTabWindow_ResearchTree");

        public static void Execute(Harmony instance, string modName)
        {
            ModName = modName;

            //ResearchProjectDef_Extensions
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetUnlockDefsAndDescs"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(GetUnlockDefsAndDescs)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetRecipesUnlocked"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(GetRecipesUnlocked)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetThingsUnlocked"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(GetThingsUnlocked)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetPlantsUnlocked"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(GetPlantsUnlocked)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:Ancestors"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Ancestors)))).Patch();

            //ResearchNode
            instance.CreateReversePatcher(AccessTools.Method(ResearchNodeType(), "BuildingPresent", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(BuildingPresent)))).Patch();
            instance.Patch(AccessTools.Method(ResearchNodeType(), "BuildingPresent", new Type[] { typeof(ResearchProjectDef) }),
                null, new HarmonyMethod(typeof(ResearchTree_Patches), nameof(BuildingPresent_Postfix)));
            instance.CreateReversePatcher(AccessTools.Method(ResearchNodeType(), "MissingFacilities", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(MissingFacilities)))).Patch();
            instance.Patch(AccessTools.Method(ResearchNodeType(), "MissingFacilities", new Type[] { typeof(ResearchProjectDef) }),
                null, new HarmonyMethod(typeof(ResearchTree_Patches), nameof(MissingFacilities_Postfix)));
            instance.Patch(AccessTools.Constructor(ResearchNodeType(), new Type[] { typeof(ResearchProjectDef) }),
                null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ResearchNode_Postfix))));

            //TEST
            instance.Patch(AccessTools.Method(ResearchNodeType(), "Draw"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Draw_Prefix))));
            instance.Patch(AccessTools.Constructor(MainTabType(), new Type[] { }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(MainTabWindow_Prefix))));

            _buildingPresentCache = AccessTools.Field(ResearchNodeType(), "_buildingPresentCache").GetValue(ResearchNodeType()) as Dictionary<ResearchProjectDef, bool>;
            _missingFacilitiesCache = AccessTools.Field(ResearchNodeType(), "_missingFacilitiesCache").GetValue(ResearchNodeType()) as Dictionary<ResearchProjectDef, List<ThingDef>>;

            //Def_Extensions
            instance.CreateReversePatcher(AccessTools.Method(modName + ".Def_Extensions:DrawColouredIcon"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(DrawColouredIcon)))).Patch();

            //MainTabWindow_ResearchTree
            instance.Patch(AccessTools.Method(MainTabType(), "DoWindowContents"),
                null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(DoWindowContents_Postfix))));
            if (modName != "ResearchPal")
            {
                instance.Patch(AccessTools.Method(MainTabType(), "Notify_TreeInitialized"),
                    null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TreeInitialized_Postfix))));
            }

            //Tree
            instance.Patch(AccessTools.Method(TreeType(), "PopulateNodes"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(PopulateNodes_Prefix))),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(PopulateNodes_Postfix))));
            if (modName == "ResearchPal")
            {
                instance.Patch(AccessTools.Method(TreeType(), "Initialize"),
                    null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TreeInitialized_Postfix))));
            }
        }

        public static List<Pair<Def, string>> GetUnlockDefsAndDescs(ResearchProjectDef research, bool dedupe = true) { throw stubMsg; }
        public static bool BuildingPresent(ResearchProjectDef research) { throw stubMsg; }
        public static List<ThingDef> MissingFacilities(ResearchProjectDef research) { throw stubMsg; }
        public static void DrawColouredIcon(this Def def, Rect canvas) { throw stubMsg; }
        public static IEnumerable<RecipeDef> GetRecipesUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static IEnumerable<ThingDef> GetThingsUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static IEnumerable<ThingDef> GetPlantsUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static List<ResearchProjectDef> Ancestors(this ResearchProjectDef research) { throw stubMsg; }

        private static Dictionary<ResearchProjectDef, bool> _buildingPresentCache;
        private static Dictionary<ResearchProjectDef, List<ThingDef>> _missingFacilitiesCache;

        public static void BuildingPresent_Postfix(ResearchProjectDef research, ref bool __result)
        {
            if (__result == true)
                return;

            bool flag = false;
            if (research.requiredResearchBuilding != null)
            {
                flag = Find.Maps.SelectMany((Map map) => map.listerBuildings.allBuildingsColonist).OfType<Building_WorkTable>().Any((Building_WorkTable b) => research.Alt_CanBeResearchedAt(b));
            }
            if (flag)
            {
                flag = research.Ancestors().All(new Func<ResearchProjectDef, bool>(BuildingPresent));
            }

            if (flag)
            {
                _buildingPresentCache.Remove(research);
                _buildingPresentCache.Add(research, flag);
            }

            __result = flag;
        }

        public static void MissingFacilities_Postfix(ref ResearchProjectDef research, ref List<ThingDef> __result)
        {
            if (__result.NullOrEmpty()) return;
            List<ThingDef> missing;

            // get list of all researches required before this
            var thisAndPrerequisites = research.Ancestors().Where(rpd => !rpd.IsFinished).ToList();
            thisAndPrerequisites.Add(research);

            // get list of all available research benches
            var availableBenches = Find.Maps.SelectMany(map => map.listerBuildings.allBuildingsColonist).OfType<Building_WorkTable>();
            var availableBenchDefs = availableBenches.Select(b => b.def).Distinct();
            missing = new List<ThingDef>();

            // check each for prerequisites
            foreach (var rpd in thisAndPrerequisites)
            {
                if (rpd.requiredResearchBuilding == null) continue;
                if (!availableBenchDefs.Contains(rpd.requiredResearchBuilding)) missing.Add(rpd.requiredResearchBuilding);
                if (rpd.requiredResearchFacilities.NullOrEmpty()) continue;
                foreach (var facility in rpd.requiredResearchFacilities)
                {
                    if (!availableBenches.Any(b => b.HasFacility(facility))) missing.Add(facility);
                }
            }

            // add to cache
            missing = missing.Distinct().ToList();
            if (missing != _missingFacilitiesCache[research])
            {
                _missingFacilitiesCache.Remove(research);
                _missingFacilitiesCache.Add(research, missing);
            }

            __result = missing;
        }

        public static ResearchProjectDef subjectToShow;
        private static MethodInfo MainTabCenterOnInfo => AccessTools.Method(MainTabType(), "CenterOn", new Type[] { NodeType() });
        private static PropertyInfo TreeNodesListInfo => AccessTools.Property(TreeType(), "Nodes");

        public static void DoWindowContents_Postfix(object __instance)
        {
            if (subjectToShow != null && treeReady)
            {
                int idx = treeNodesResearchCache.IndexOf(subjectToShow);
                MainTabCenterOnInfo.Invoke(__instance, new object[] { treeNodesList[idx] });
                subjectToShow = null;
            }
        }

        private static IList treeNodesList;

        private static bool treeReady = false;

        private static List<ResearchProjectDef> treeNodesResearchCache = new List<ResearchProjectDef>();

        public static void TreeInitialized_Postfix(object __instance)
        {
            treeNodesList = (IList)TreeNodesListInfo.GetValue(__instance);
            treeReady = !treeNodesResearchCache.NullOrEmpty();
        }

        private static bool populating = false;

        private static void PopulateNodes_Prefix()
        {
            populating = true;
        }

        private static void PopulateNodes_Postfix()
        {
            populating = false;
        }

        private static void ResearchNode_Postfix(ResearchProjectDef research)
        {
            if (populating)
            {
                treeNodesResearchCache.Add(research);
            }
        }

        //TEST
        public static void MainTabWindow_Prefix(object __instance)
        {
            MainWindow = __instance;
        }
        public static object MainWindow;

        public static bool Draw_Prefix(object __instance, Rect visibleRect, bool forceDetailedMode = false)
        {
            //Reflection info. Why can't I cache it?
            //Node
            MethodInfo IsVisibleInfo = AccessTools.Method(__instance.GetType().BaseType, "IsVisible");
            PropertyInfo HighlightedInfo = AccessTools.Property(__instance.GetType().BaseType, "Highlighted");
            PropertyInfo RectInfo = AccessTools.Property(__instance.GetType().BaseType, "Rect");
            FieldInfo largeLabelInfo = AccessTools.Field(__instance.GetType().BaseType, "_largeLabel");
            PropertyInfo LabelRectInfo = AccessTools.Property(__instance.GetType().BaseType, "LabelRect");
            PropertyInfo CostLabelRectInfo = AccessTools.Property(__instance.GetType().BaseType, "CostLabelRect");
            PropertyInfo CostIconRectInfo = AccessTools.Property(__instance.GetType().BaseType, "CostIconRect");
            PropertyInfo IconsRectInfo = AccessTools.Property(__instance.GetType().BaseType, "IconsRect");
            //Reserarch Node
            MethodInfo GetMissingRequiredRecursiveInfo = AccessTools.Method(__instance.GetType(), "GetMissingRequiredRecursive");
            PropertyInfo ChildrenInfo = AccessTools.Property(__instance.GetType(), "Children");
            PropertyInfo ColorInfo = AccessTools.Property(__instance.GetType(), "Color");
            PropertyInfo AvailableInfo = AccessTools.Property(__instance.GetType(), "Available");
            PropertyInfo CompletedInfo = AccessTools.Property(__instance.GetType(), "Completed");
            FieldInfo ResearchInfo = AccessTools.Field(__instance.GetType(), "Research");
            MethodInfo GetResearchTooltipStringInfo = AccessTools.Method(__instance.GetType(), "GetResearchTooltipString");
            MethodInfo BuildingPresentInfo = AccessTools.Method(__instance.GetType(), "BuildingPresent");
            //MainTabType
            PropertyInfo InstanceInfo = AccessTools.Property(MainWindow.GetType(), "Instance");
            PropertyInfo ZoomLevelInfo = AccessTools.Property(MainWindow.GetType(), "ZoomLevel");

            Rect rect = (Rect)RectInfo.GetValue(__instance);
            ResearchProjectDef Research = (ResearchProjectDef)ResearchInfo.GetValue(__instance);
            bool available = (bool)AvailableInfo.GetValue(__instance);
            bool completed = (bool)CompletedInfo.GetValue(__instance);
            //End of reflection info.

            if (!(bool)IsVisibleInfo.Invoke(__instance, new object[] { visibleRect }))
            {
                HighlightedInfo.SetValue(__instance, false);
                return false;
            }
            var detailedMode = forceDetailedMode || (float)ZoomLevelInfo.GetValue(InstanceInfo.GetValue(__instance)) < Constants.DetailedModeZoomLevelCutoff;
            var mouseOver = Mouse.IsOver(rect);

            if (Event.current.type == EventType.Repaint)
            {
                //researches that are completed or could be started immediately, and that have the required building(s) available
                GUI.color = mouseOver ? GenUI.MouseoverColor : (Color)ColorInfo.GetValue(__instance);
                if (mouseOver || (bool)HighlightedInfo.GetValue(__instance))
                    GUI.DrawTexture(rect, ResearchTree_Assets.ButtonActive);
                else
                    GUI.DrawTexture(rect, ResearchTree_Assets.Button);

                //grey out center to create a progress bar effect, completely greying out research not started.
                if (available)
                {
                    var progressBarRect = rect.ContractedBy(3f);
                    GUI.color = ResearchTree_Assets.ColorAvailable[Research.techLevel];
                    progressBarRect.xMin += Research.ProgressPercent * progressBarRect.width;
                    GUI.DrawTexture(progressBarRect, BaseContent.WhiteTex);
                }

                HighlightedInfo.SetValue(__instance, false);

                //draw the research label
                if (!completed && !available)
                    GUI.color = Color.grey;
                else
                    GUI.color = Color.white;

                if (detailedMode)
                {
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.WordWrap = false;
                    Text.Font = (bool)largeLabelInfo.GetValue(__instance) ? GameFont.Tiny : GameFont.Small;
                    Widgets.Label((Rect)LabelRectInfo.GetValue(__instance), Research.LabelCap);
                }
                else
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.WordWrap = false;
                    Text.Font = GameFont.Medium;
                    Widgets.Label(rect, Research.LabelCap);
                }

                //draw research cost and icon
                if (detailedMode)
                {
                    Text.Anchor = TextAnchor.UpperRight;
                    Text.Font = Research.CostApparent > 1000000 ? GameFont.Tiny : GameFont.Small;
                    Widgets.Label((Rect)CostLabelRectInfo.GetValue(__instance), Research.CostApparent.ToStringByStyle(ToStringStyle.Integer));
                    GUI.DrawTexture((Rect)CostIconRectInfo.GetValue(__instance), !completed && !available ? ResearchTree_Assets.Lock : ResearchTree_Assets.ResearchIcon,
                                        ScaleMode.ScaleToFit);
                }

                Text.WordWrap = true;

                //attach description and further info to a tooltip
                string root = HarmonyPatches.ResearchPal ? "ResearchPal" : "Fluffy.ResearchTree";
                TooltipHandler.TipRegion(rect, new Func<string>(() => (string)GetResearchTooltipStringInfo.Invoke(__instance, new object[] { })), Research.GetHashCode());
                if (!(bool)BuildingPresentInfo.Invoke(__instance, new object[] { }))
                {
                    string languageKey = root + ".MissingFacilities";
                    TooltipHandler.TipRegion(rect, languageKey.Translate(string.Join(", ", MissingFacilities(Research).Select(td => td.LabelCap).ToArray())));
                }
                else if (!Research.TechprintRequirementMet)
                    TooltipHandler.TipRegion(rect, root + ".MissingTechprints".Translate(Research.TechprintsApplied, Research.techprintCount));

                //draw unlock icons
                if (detailedMode)
                {
                    Rect IconsRect = (Rect)IconsRectInfo.GetValue(__instance);
                    var unlocks = ResearchTree_Patches.GetUnlockDefsAndDescs(Research);
                    for (var i = 0; i < unlocks.Count; i++)
                    {
                        var iconRect = new Rect(
                            IconsRect.xMax - (i + 1) * (Constants.IconSize.x + 4f),
                            IconsRect.yMin + (IconsRect.height - Constants.IconSize.y) / 2f,
                            Constants.IconSize.x,
                            Constants.IconSize.y);

                        if (iconRect.xMin - Constants.IconSize.x < IconsRect.xMin &&
                                i + 1 < unlocks.Count)
                        {
                            //stop the loop if we're about to overflow and have 2 or more unlocks yet to print.
                            iconRect.x = IconsRect.x + 4f;
                            GUI.DrawTexture(iconRect, ResearchTree_Assets.MoreIcon, ScaleMode.ScaleToFit);
                            var tip = string.Join("\n", unlocks.GetRange(i, unlocks.Count - i).Select(p => p.Second).ToArray());
                            TooltipHandler.TipRegion(iconRect, tip);
                            //new TipSignal(tip, Settings.TipID, TooltipPriority.Pawn) );
                            break;
                        }

                        //draw icon
                        unlocks[i].First.DrawColouredIcon(iconRect);

                        //tooltip
                        TooltipHandler.TipRegion(iconRect, unlocks[i].Second);
                    }
                }

                if (mouseOver)
                    //highlight prerequisites if research available
                    if (available)
                    {
                        HighlightedInfo.SetValue(__instance, true);
                        foreach (var prerequisite in (IEnumerable<object>)GetMissingRequiredRecursiveInfo.Invoke(__instance, new object[] { }))
                            HighlightedInfo.SetValue(Convert.ChangeType(prerequisite, ResearchNodeType()), true);
                    }
                    //highlight children if completed
                    else if (completed)
                    {
                        foreach (var child in (IEnumerable<object>)ChildrenInfo.GetValue(__instance))
                            HighlightedInfo.SetValue(Convert.ChangeType(child, ResearchNodeType()), true);
                    }
            }

            //if clicked and not yet finished, queue up this research and all prereqs.
            if (Widgets.ButtonInvisible(rect) /*&& available*/)
            {
                ////LMB is queue operations, RMB is info
                //            if (Event.current.button == 0 && !Research.IsFinished)
                //{
                //    if (!Queue.IsQueued(this))
                //    {
                //        // if shift is held, add to queue, otherwise replace queue
                //        var queue = GetMissingRequiredRecursive()
                //                   .Concat(new List<ResearchNode>(new[] { this }))
                //                   .Distinct();
                //        Queue.EnqueueRange(queue, Event.current.shift);
                //    }
                //    else
                //    {
                //        Queue.Dequeue(this);
                //    }
                SelectMenu();
            }

            //if (DebugSettings.godMode && Prefs.DevMode && Event.current.button == 1 && !Research.IsFinished)
            //{
            //    Find.ResearchManager.FinishProject(Research);
            //    Queue.Notify_InstantFinished();
            //}
            return false;
        }

        private static IEnumerable<Widgets.DropdownMenuElement<Pawn>> GeneratePawnRestrictionOptions()
        {
            WorkTypeDef workGiver = TechDefOf.HR_Learn;
            SkillDef skill = SkillDefOf.Intellectual;
            IEnumerable<Pawn> enumerable = PawnsFinder.AllMaps_FreeColonists.OrderBy(x => x.LabelCap).ThenBy(x => x.workSettings.WorkIsActive(workGiver)).ThenByDescending(x => x.skills.GetSkill(skill).Level);
            using (IEnumerator<Pawn> enumerator = enumerable.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Pawn pawn = enumerator.Current;
                    if (pawn.WorkTypeIsDisabled(TechDefOf.HR_Learn))
                    {
                        yield return new Widgets.DropdownMenuElement<Pawn>
                        {
                            option = new FloatMenuOption(string.Format("{0} ({1})", pawn.LabelShortCap, "WillNever".Translate(workGiver.verb)), null, MenuOptionPriority.Default, null, null, 0f, null, null),
                            payload = pawn
                        };
                    }
                    else if (!pawn.workSettings.WorkIsActive(workGiver))
                    {
                        yield return new Widgets.DropdownMenuElement<Pawn>
                        {
                            option = new FloatMenuOption(string.Format("{0} ({1})", pawn.LabelShortCap, "NotAssigned".Translate()), delegate ()
                            {
                                Log.Message("do something");
                            }, MenuOptionPriority.Default, null, null, 0f, null, null),
                            payload = pawn
                        };
                    }
                    else
                    {
                        yield return new Widgets.DropdownMenuElement<Pawn>
                        {
                            option = new FloatMenuOption(string.Format("{0}", pawn.LabelShortCap), delegate ()
                            {
                                Log.Message("do something");
                            }, MenuOptionPriority.Default, null, null, 0f, null, null),
                            payload = pawn
                        };
                    }
                }
            }
            //IEnumerator<Pawn> enumerator = null;
            yield break;
        }

        public static void SelectMenu()
        {
            Log.Warning("clicked");
            Find.WindowStack.FloatMenu?.Close(false);
            List<FloatMenuOption> options = (from opt in GeneratePawnRestrictionOptions()
                                             select opt.option).ToList<FloatMenuOption>();
            if (!options.Any())
                options.Add(new FloatMenuOption("Fluffy.ResearchTree.NoResearchFound".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}


