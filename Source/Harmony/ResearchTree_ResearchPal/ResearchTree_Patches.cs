using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace HumanResources
{
    public static class ResearchTree_Patches
    {
        #region "variables"
        public static object MainTabInstance;
        public static ResearchProjectDef interest;
        private static Color BrightColor = new Color(1f, 0.85f, 0.2f); //yellow
        private static Color ShadedColor = new Color(0.72f, 0.57f, 0.13f); //mustard
        private static Color VariantColor = new Color(1f, 0.6f, 0.08f); //light orange
        private static string ModName = "";
        private static bool
            populating = false,
            treeReady = false;

        private static Dictionary<ResearchProjectDef, object> ResearchNodesCache = new Dictionary<ResearchProjectDef, object>();
        private static NotImplementedException stubMsg = new NotImplementedException("ResearchTree reverse patch");
        #endregion

        #region "reflection info"
        //Constants:
        public static FieldInfo
            EpsilonInfo,
            DetailedModeZoomLevelCutoffInfo,
            MarginInfo,
            QueueLabelSizeInfo,
            IconSizeInfo,
            NodeMarginsInfo,
            NodeSizeInfo,
            TopBarHeightInfo;

        //Tree:
        public static PropertyInfo NodesInfo;

        private static PropertyInfo
        //ResearchNode:
            ChildrenInfo,
            ColorInfo,
            AvailableInfo,
            HighlightedInfo,
            RectInfo,
            LabelRectInfo,
            CostLabelRectInfo,
            CostIconRectInfo,
            IconsRectInfo,
        //MainWindow:
            InstanceInfo,
            ZoomLevelInfo;

        private static MethodInfo
        //ResearchNode:
            GetMissingRequiredRecursiveInfo,
            GetResearchTooltipStringInfo,
        //Node:
            IsVisibleInfo,
            MainTabCenterOnInfo,
        //Tree:
            HandleFixedHighlightInfo,
            StopFixedHighlightsInfo;

        private static FieldInfo
        //Node:
            largeLabelInfo,
            ResearchInfo,
        //ResearchNode:
            isMatchedInfo,
        //MainTabWindow_ResearchTree
            searchActiveInfo,
        //Assets
            NormalHighlightColorInfo,
            HoverPrimaryColorInfo,
            FixedPrimaryColorInfo;

        public static Type AssetsType() => AccessTools.TypeByName(ModName + ".Assets");
        public static Type ConstantsType() => AccessTools.TypeByName(ModName + ".Constants");
        public static Type MainTabType() => AccessTools.TypeByName(ModName + ".MainTabWindow_ResearchTree");
        public static Type NodeType() => AccessTools.TypeByName(ModName + ".Node");
        public static Type QueueType() => AccessTools.TypeByName(ModName + ".Queue");
        public static Type ResearchNodeType() => AccessTools.TypeByName(ModName + ".ResearchNode");
        public static Type TreeType() => AccessTools.TypeByName(ModName + ".Tree");
        #endregion

        #region "main patches"

        public static void Execute(Harmony instance, string modName, bool altRPal = false)
        {
            Harmony.DEBUG = true;
            ModName = modName;
            AltRPal = altRPal;

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
            if (altRPal) ResearchNodeInfo = AccessTools.Method(modName + ".ResearchProjectDef_Extensions:ResearchNode");

            //Node
            IsVisibleInfo = AccessTools.Method(NodeType(), "IsVisible");
            if (altRPal)
            {
                instance.CreateReversePatcher(AccessTools.Method(modName + ".Node:Highlighted"),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Highlighted)))).Patch();
            }
            else HighlightedInfo = AccessTools.Property(NodeType(), "Highlighted");
            RectInfo = AccessTools.Property(NodeType(), "Rect");
            largeLabelInfo = AccessTools.Field(NodeType(), "_largeLabel");
            LabelRectInfo = AccessTools.Property(NodeType(), "LabelRect");
            CostLabelRectInfo = AccessTools.Property(NodeType(), "CostLabelRect");
            CostIconRectInfo = AccessTools.Property(NodeType(), "CostIconRect");
            IconsRectInfo = AccessTools.Property(NodeType(), "IconsRect");

            //ResearchNode
            if (altRPal)
            {
                instance.Patch(AccessTools.Method(ResearchNodeType(), "HandleDragging", new Type[] { typeof(bool) }),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(HandleDragging_Prefix))));
                instance.Patch(AccessTools.Method(ResearchNodeType(), "LeftClick"),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(LeftClick_Prefix))));
                HighlightInfo = AccessTools.Method(ResearchNodeType(), "Highlight");
                BuildingPresentInfo = AccessTools.Method(ResearchNodeType(), "BuildingPresent", new Type[] { ResearchNodeType() });
                isMatchedInfo = AccessTools.Field(ResearchNodeType(), "isMatched");
            }
            else
            {
                instance.CreateReversePatcher(AccessTools.Method(ResearchNodeType(), "BuildingPresent", new Type[] { typeof(ResearchProjectDef) }),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(BuildingPresent)))).Patch();
                instance.Patch(AccessTools.Method(ResearchNodeType(), "Draw"),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Draw_Prefix))));
                instance.Patch(AccessTools.PropertyGetter(ResearchNodeType(), "EdgeColor"),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(EdgeColor_Prefix))));
                GetMissingRequiredRecursiveInfo = AccessTools.Method(ResearchNodeType(), "GetMissingRequiredRecursive");
                AvailableInfo = AccessTools.Property(ResearchNodeType(), "Available");
            }
            instance.CreateReversePatcher(AccessTools.Method(ResearchNodeType(), "TechprintAvailable", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TechprintAvailable)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(ResearchNodeType(), "MissingFacilities", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(MissingFacilities)))).Patch();
            instance.Patch(AccessTools.PropertyGetter(ResearchNodeType(), "Color"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Color_Prefix))));
            instance.Patch(AccessTools.Constructor(ResearchNodeType(), new Type[] { typeof(ResearchProjectDef) }),
                null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ResearchNode_Postfix))));
            instance.Patch(AccessTools.Method(ResearchNodeType(), "GetResearchTooltipString"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(GetResearchTooltipString_Prefix))));
            ChildrenInfo = AccessTools.Property(ResearchNodeType(), "Children");
            ColorInfo = AccessTools.Property(ResearchNodeType(), "Color");
            ResearchInfo = AccessTools.Field(ResearchNodeType(), "Research");
            GetResearchTooltipStringInfo = AccessTools.Method(ResearchNodeType(), "GetResearchTooltipString");

            //Def_Extensions
            instance.CreateReversePatcher(AccessTools.Method(modName + ".Def_Extensions:DrawColouredIcon"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(DrawColouredIcon)))).Patch();

            //MainTabWindow_ResearchTree
            if (AltRPal)
            {
                searchActiveInfo = AccessTools.Field(MainTabType(), "_searchActive");
            }
            else
            {
                ZoomLevelInfo = AccessTools.Property(MainTabType(), "ZoomLevel");

                //fix for tree overlapping search bar on higher UI scales
                instance.Patch(AccessTools.Method(MainTabType(), "SetRects"),
                    null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Set_Rects_Postfix))));
            };
            if (modName != "ResearchPal")
            {
                instance.Patch(AccessTools.Method(MainTabType(), "Notify_TreeInitialized"),
                    null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TreeInitialized_Postfix))));
            }
            instance.Patch(AccessTools.Method(MainTabType(), "DoWindowContents"),
                null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(DoWindowContents_Postfix))));
            instance.Patch(AccessTools.Method(typeof(Window), "PostClose"),
                null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Close_Postfix))));
            InstanceInfo = AccessTools.Property(MainTabType(), "Instance");
            Type windowNodeType = AltRPal ? ResearchNodeType() : NodeType();
            MainTabCenterOnInfo = AccessTools.Method(MainTabType(), "CenterOn", new Type[] { windowNodeType });

            //Tree
            instance.Patch(AccessTools.Method(TreeType(), "PopulateNodes"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(PopulateNodes_Prefix))),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(PopulateNodes_Postfix))));
            if (modName == "ResearchPal")
            {
                string initializer = AltRPal ? "InitializeLayout" : "Initialize";
                instance.Patch(AccessTools.Method(TreeType(), initializer),
                    null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TreeInitialized_Postfix))));
                if (AltRPal)
                {
                    HandleFixedHighlightInfo = AccessTools.Method(TreeType(), "HandleFixedHighlight");
                    StopFixedHighlightsInfo = AccessTools.Method(TreeType(), "StopFixedHighlights");
                }
            }
            NodesInfo = AccessTools.Property(TreeType(), "Nodes");

            //Queue
            if (altRPal)
            {
                instance.Patch(AccessTools.Method(QueueType(), "Draw"),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(QueueDraw_Prefix))));
            }
            else instance.Patch(AccessTools.Method(QueueType(), "DrawQueue"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(DrawQueue_Prefix))));

            //Constants
            EpsilonInfo = AccessTools.Field(ConstantsType(), "Epsilon");
            DetailedModeZoomLevelCutoffInfo = AccessTools.Field(ConstantsType(), "DetailedModeZoomLevelCutoff");
            MarginInfo = AccessTools.Field(ConstantsType(), "Margin");
            QueueLabelSizeInfo = AccessTools.Field(ConstantsType(), "QueueLabelSize");
            IconSizeInfo = AccessTools.Field(ConstantsType(), "IconSize");
            NodeMarginsInfo = AccessTools.Field(ConstantsType(), "NodeMargins");
            NodeSizeInfo = AccessTools.Field(ConstantsType(), "NodeSize");
            TopBarHeightInfo = AccessTools.Field(ConstantsType(), "TopBarHeight");
            if (altRPal) TopBarHeightInfo.SetValue(instance, ResearchTree_Constants.NodeSize.y * 0.6f + 2 * ResearchTree_Constants.Margin);

            //Assets
            if (altRPal)
            {
                NormalHighlightColorInfo = AccessTools.Field(AssetsType(), "NormalHighlightColor"); //default blue
                HoverPrimaryColorInfo = AccessTools.Field(AssetsType(), "HoverPrimaryColor"); //violet
                FixedPrimaryColorInfo = AccessTools.Field(AssetsType(), "FixedPrimaryColor"); //cyan
                NormalHighlightColorInfo.SetValue(instance, ShadedColor); //mustard
                HoverPrimaryColorInfo.SetValue(instance, BrightColor); //yellow
                FixedPrimaryColorInfo.SetValue(instance, VariantColor); //light orange
            }
            Harmony.DEBUG = false;
        }

        public static List<ResearchProjectDef> Ancestors(this ResearchProjectDef research) { throw stubMsg; }

        public static bool BuildingPresent(ResearchProjectDef research) { throw stubMsg; }

        public static void Close_Postfix(object __instance)
        {
            if (__instance.GetType() == MainTabType()) Extension_Research.currentPawnsCache?.Clear();
            MainTabInstance = null;
        }

        public static bool Color_Prefix(object __instance, ref Color __result)
        {
            bool flag = HighlightedProxy(__instance);
            if (flag)
            {
                __result = BrightColor;
                return false;
            }
            return true;
        }

        private static void DeInterest()
        {
            if (AltRPal) StopFixedHighlightsInfo.Invoke(MainTabInstance, new object[] { });
            if (interest != null) interest = null;
        }

        public static void DoWindowContents_Postfix(object __instance)
        {
            if (MainTabInstance == null) MainTabInstance = __instance;
            if (interest != null)
            {
                if (treeReady)
                {
                    MainTabCenterOnInfo.Invoke(__instance, new object[] { ResearchNodesCache[interest] });
                    if (AltRPal) HandleFixedHighlightInfo.Invoke(__instance, new object[] { ResearchNodesCache[interest] });
                    else HighlightedProxy(ResearchNodesCache[interest], true);
                }
                else Log.Warning("[HumanResources] Locate tech on the Research Tab failed: the tree isn't ready");
            }
        }

        public static bool Draw_Prefix(object __instance, Rect visibleRect, bool forceDetailedMode = false)
        {
            //Reflected objects
            Rect rect = (Rect)RectInfo.GetValue(__instance);
            ResearchProjectDef Research = (ResearchProjectDef)ResearchInfo.GetValue(__instance);
            bool available = (bool)AvailableInfo.GetValue(__instance);
            bool completed = Research.IsFinished; //(bool)CompletedInfo.GetValue(__instance); //simplified
            //End of reflection info.

            if (!(bool)IsVisibleInfo.Invoke(__instance, new object[] { visibleRect }))
            {
                HighlightedProxy(__instance, false);
                return false;
            }
            var detailedMode = forceDetailedMode || (float)ZoomLevelInfo.GetValue(InstanceInfo.GetValue(__instance)) < ResearchTree_Constants.DetailedModeZoomLevelCutoff;
            var mouseOver = Mouse.IsOver(rect);

            if (Event.current.type == EventType.Repaint)
            {
                //researches that are completed or could be started immediately, and that have the required building(s) available
                GUI.color = mouseOver ? BrightColor : (Color)ColorInfo.GetValue(__instance);
                if (mouseOver || HighlightedProxy(__instance)) GUI.DrawTexture(rect, ResearchTree_Assets.ButtonActive);
                else GUI.DrawTexture(rect, ResearchTree_Assets.Button);

                //grey out center to create a progress bar effect, completely greying out research not started.
                if (available)
                {
                    var progressBarRect = rect.ContractedBy(3f);
                    GUI.color = ResearchTree_Assets.ColorAvailable[Research.techLevel];
                    progressBarRect.xMin += Research.ProgressPercent * progressBarRect.width;
                    GUI.DrawTexture(progressBarRect, BaseContent.WhiteTex);
                }
                HighlightedProxy(__instance, interest == Research);

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
                if (!BuildingPresentProxy(Research))
                {
                    string languageKey = root + ".MissingFacilities";
                    TooltipHandler.TipRegion(rect, languageKey.Translate(string.Join(", ", MissingFacilities(Research).Select(td => td.LabelCap).ToArray())));
                }
                else if (!ResearchTree_Patches.TechprintAvailable(Research))
                {
                    string languageKey = root + ".MissingTechprints";
                    TooltipHandler.TipRegion(rect, languageKey.Translate(Research.TechprintsApplied, Research.techprintCount));
                }


                //draw unlock icons
                if (detailedMode)
                {
                    Rect IconsRect = (Rect)IconsRectInfo.GetValue(__instance);
                    var unlocks = ResearchTree_Patches.GetUnlockDefsAndDescs(Research);
                    for (var i = 0; i < unlocks.Count; i++)
                    {
                        var iconRect = new Rect(
                            IconsRect.xMax - (i + 1) * (ResearchTree_Constants.IconSize.x + 4f),
                            IconsRect.yMin + (IconsRect.height - ResearchTree_Constants.IconSize.y) / 2f,
                            ResearchTree_Constants.IconSize.x,
                            ResearchTree_Constants.IconSize.y);

                        if (iconRect.xMin - ResearchTree_Constants.IconSize.x < IconsRect.xMin &&
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
                {
                    if (interest != null && interest != Research) DeInterest();

                    //highlight prerequisites if research available
                    if (available)
                    {
                        HighlightedProxy(__instance, true);
                        foreach (var prerequisite in (IEnumerable<object>)GetMissingRequiredRecursiveInfo.Invoke(__instance, new object[] { }))
                            HighlightedProxy(Convert.ChangeType(prerequisite, ResearchNodeType()), true);
                    }
                    //highlight children if completed
                    else if (completed)
                    {
                        foreach (var child in (IEnumerable<object>)ChildrenInfo.GetValue(__instance))
                            HighlightedProxy(Convert.ChangeType(child, ResearchNodeType()), true);
                    }
                }
            }

            Research.DrawAssignments(rect);

            if (Widgets.ButtonInvisible(rect))
            {
                //CUSTOM: replaced queue operations for assignment menu
                if (Event.current.button == 0) Research.SelectMenu(completed);
                if (DebugSettings.godMode && Prefs.DevMode && Event.current.button == 1 && !Research.IsFinished)
                {
                    Find.ResearchManager.FinishProject(Research);
                }
            }
            return false;
        }

        public static void DrawColouredIcon(this Def def, Rect canvas) { throw stubMsg; }

        public static bool EdgeColor_Prefix(object __instance, ref Color __result)
        {
            bool flag = HighlightedProxy(__instance);
            if (flag)
            {
                __result = BrightColor;
                return false;
            }
            return true;
        }

        public static IEnumerable<ThingDef> GetPlantsUnlocked(this ResearchProjectDef research) { throw stubMsg; }

        public static IEnumerable<RecipeDef> GetRecipesUnlocked(this ResearchProjectDef research) { throw stubMsg; }

        public static IEnumerable<ThingDef> GetThingsUnlocked(this ResearchProjectDef research) { throw stubMsg; }

        public static List<Pair<Def, string>> GetUnlockDefsAndDescs(ResearchProjectDef research, bool dedupe = true) { throw stubMsg; }

        public static List<ThingDef> MissingFacilities(ResearchProjectDef research) { throw stubMsg; }

        public static bool TechprintAvailable(ResearchProjectDef research) { throw stubMsg; }

        public static void TreeInitialized_Postfix(object __instance)
        {
            treeReady = !ResearchNodesCache.EnumerableNullOrEmpty();
        }

        private static bool DrawQueue_Prefix(object __instance, Rect canvas)
        {
            if (AltRPal)
            {
                canvas.xMax += 130f + 2 * ResearchTree_Constants.Margin; //keep an eye on his MainTabWindow_ResearchTree.DrawTopBar method for changes to this number
                canvas = canvas.ExpandedBy(ResearchTree_Constants.Margin);
            }
            float padding = 12f;
            float spacing = Find.ColonistBar.SpaceBetweenColonistsHorizontal;
            float height = canvas.height;
            float startPos = canvas.xMax - height - padding;
            Vector2 size = new Vector2(height + spacing, height - padding);
            Vector2 innerSize = new Vector2(height - padding, height - padding);
            IEnumerable<object> expertiseDisplay = new object[] { };
            bool displayActive = false;
            using (IEnumerator<Pawn> enumerator = Find.ColonistBar.GetColonistsInOrder().Where(x => x.TechBound()).AsEnumerable().Reverse().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Vector2 position = new Vector2(startPos, canvas.y);
                    Rect box = new Rect(position, size);
                    Rect innerBox = new Rect(position.x + spacing, position.y, size.x - spacing - 2 * padding, size.y);
                    Pawn pawn = enumerator.Current;
                    GUI.DrawTexture(box, PortraitsCache.Get(pawn, size, default, 1.4f));
                    CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
                    if (Mouse.IsOver(innerBox))
                    {
                        DeInterest();
                        ReflectKnowledge(__instance, techComp, out expertiseDisplay);
                        displayActive = true;
                    }
                    if (!techComp.homework.NullOrEmpty())
                    {
                        StringBuilder homeworkSummary = new StringBuilder();
                        homeworkSummary.AppendLine("AssignedTo".Translate(pawn));
                        foreach (var tech in techComp.homework)
                        {
                            homeworkSummary.AppendLine("- " + TechStrings.GetTask(pawn, tech) + " " + tech.label);
                        }
                        TooltipHandler.TipRegionByKey(box, homeworkSummary.ToString());
                    }
                    Vector2 pos = new Vector2(box.center.x, box.yMax);
                    GenMapUI.DrawPawnLabel(pawn, pos, 1f, box.width, null, GameFont.Tiny, false, true);
                    startPos -= height;
                }
            }
            if (AltRPal)
            {
                if (displayActive)
                { 
                    UpdateMatches(expertiseDisplay);
                    expertiseDisplayed = true;
                }
                else if (expertiseDisplayed)
                {
                    ToggleSearch(false);
                    expertiseDisplayed = false;
                }
            }
            return false;
        }

        private static bool GetResearchTooltipString_Prefix(ResearchProjectDef ___Research, ref string __result)
        {
            var text = new StringBuilder();
            text.AppendLine(___Research.description);
            if (DebugSettings.godMode && !HarmonyPatches.ResearchPal) text.AppendLine("Fluffy.ResearchTree.RClickInstaFinish".Translate()); //There's no corresponding line on ResearchPal, but it works anyway. 
            __result = text.ToString();
            return false;
        }

        private static void PopulateNodes_Postfix()
        {
            populating = false;
        }

        private static void PopulateNodes_Prefix()
        {
            populating = true;
        }

        private static void ReflectKnowledge(object instance, CompKnowledge techComp, out IEnumerable<object> expertiseDisplay)
        {
            Find.WindowStack.FloatMenu?.Close(false);
            bool valid = !techComp.expertise.EnumerableNullOrEmpty();
            expertiseDisplay = new object[] { };
            if (AltRPal)
            {
                ToggleSearch(true);
                if (valid) expertiseDisplay = from e in ResearchNodesCache
                                              where techComp.expertise.Keys.Contains(e.Key)
                                              select e.Value;
            }
            else if (valid)
            {
                foreach (ResearchProjectDef tech in techComp.expertise.Keys)
                {
                    HighlightedProxy(ResearchNodesCache[tech], true);
                }
            }
        }

        private static void ResearchNode_Postfix(object __instance, ResearchProjectDef research)
        {
            if (populating) ResearchNodesCache.Add(research, __instance);
        }

        private static void Set_Rects_Postfix(object __instance)
        {
            FieldInfo baseViewRectInfo = AccessTools.Field(MainTabType(), "_baseViewRect");
            baseViewRectInfo.SetValue(__instance, new Rect(
                Window.StandardMargin / Prefs.UIScale,
                (ResearchTree_Constants.TopBarHeight + ResearchTree_Constants.Margin + Window.StandardMargin),
                (Screen.width - Window.StandardMargin * 2f) / Prefs.UIScale,
                ((Screen.height - MainButtonDef.ButtonHeight - Window.StandardMargin * 3) / Prefs.UIScale) - ResearchTree_Constants.TopBarHeight - ResearchTree_Constants.Margin * 2)
                );
        }

        private static void UpdateMatches(IEnumerable<object> expertiseDisplay)
        {
            foreach (object node in ResearchNodesCache.Values)
            {
                isMatchedInfo.SetValue(node, expertiseDisplay.Contains(node));
            }
        }
        #endregion

        #region "VinaLx.ResearchPalForked adaptation"

        public static bool 
            AltRPal = false,
            expertiseDisplayed = false;

        private static MethodInfo
        //ResearchProjectDef_Extensions:
            ResearchNodeInfo,
        //ResearchNode:
            BuildingPresentInfo,
            HighlightInfo;

        private static bool searchActive
        {
            get
            {
                if (MainTabInstance != null)
                {
                    return (bool)searchActiveInfo.GetValue(MainTabInstance);
                }
                return false;
            }
            set
            {
                if (MainTabInstance != null)
                {
                    searchActiveInfo.SetValue(MainTabInstance, value);
                }
            }
        }

        public static bool BuildingPresentProxy(ResearchProjectDef research)
        {
            if (AltRPal && ResearchNodeInfo != null)
            {
                object rnode = ResearchNodeInfo.Invoke(research, new object[] { research });
                return (bool)BuildingPresentInfo.Invoke(rnode, new object[] { rnode });
            }
            else if (AltRPal)
            {
                Log.Error("[HumanResources] Error adapting to ResearchPal-Forked: null ResearchNodeInfo");
            }
            return BuildingPresent(research);
        }

        //public static void Draw_Alt_Prefix(object __instance)
        //{
        //    Rect rect = (Rect)RectInfo.GetValue(__instance);
        //    ResearchProjectDef Research = (ResearchProjectDef)ResearchInfo.GetValue(__instance);
        //    Research.DrawAssignments(rect);
        //}

        public static bool HandleDragging_Prefix(object __instance, bool mouseOver, bool ____available)
        {
            ResearchProjectDef Research = (ResearchProjectDef)ResearchInfo.GetValue(__instance);
            Rect rect = (Rect)RectInfo.GetValue(__instance);
            Research.DrawAssignments(rect);
            if (Widgets.ButtonInvisible(rect)) //Not 'Event.current.type == EventType.MouseDown' or it would overlap the assignments click boxes.
            {
                bool completed = Research.IsFinished;
                if (Event.current.button == 0) Research.SelectMenu(completed);
                if (DebugSettings.godMode && Prefs.DevMode && Event.current.button == 1 && !completed)
                {
                    Find.ResearchManager.FinishProject(Research);
                }
            }
            return false;
        }

        public static bool Highlighted() { throw stubMsg; }

        public static void HighlightedProxy(object node, bool setting, int reason = 7 )
        {
            //Set
            if (AltRPal) HighlightInfo.Invoke(node, new object[] { reason });
            else if (HighlightedInfo != null)
            {
                HighlightedInfo.SetValue(node, setting);
            }
        }

        public static bool HighlightedProxy(object node)
        {
            //Get
            if (AltRPal) return Highlighted();
            else if (HighlightedInfo != null)
            {
                return (bool)HighlightedInfo.GetValue(node);
            }
            return false;
        }

        public static bool LeftClick_Prefix()
        {
            return false;
        }

        public static bool QueueDraw_Prefix(object __instance, Rect baseCanvas)
        {
            return DrawQueue_Prefix(__instance, baseCanvas);
        }

        private static void ToggleSearch(bool state)
        {
            if (searchActive != state) searchActive = state;
        }
        #endregion
    }
}


