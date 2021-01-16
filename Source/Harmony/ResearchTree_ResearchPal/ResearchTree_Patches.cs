using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
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
        private static string ModName = "";
        private static NotImplementedException stubMsg = new NotImplementedException("ResearchTree reverse patch");
        public static Type AssetsType() => AccessTools.TypeByName(ModName + ".Assets");
        public static Type TreeType() => AccessTools.TypeByName(ModName + ".Tree");
        public static Type NodeType() => AccessTools.TypeByName(ModName + ".Node");
        public static Type ResearchNodeType() => AccessTools.TypeByName(ModName + ".ResearchNode");
        public static Type MainTabType() => AccessTools.TypeByName(ModName + ".MainTabWindow_ResearchTree");
        public static Type ConstantsType() => AccessTools.TypeByName(ModName + ".Constants");
        public static Type QueueType() => AccessTools.TypeByName(ModName + ".Queue");

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
                BuildingPresentInfo = AccessTools.Method(ResearchNodeType(), "BuildingPresent", new Type[] { ResearchNodeType() });
                HighlightInfo = AccessTools.Method(ResearchNodeType(), "Highlight");
                UnhighlightInfo = AccessTools.Method(ResearchNodeType(), "Unhighlight");
            }
            else instance.CreateReversePatcher(AccessTools.Method(ResearchNodeType(), "BuildingPresent", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(BuildingPresent)))).Patch(); //fixed
            instance.CreateReversePatcher(AccessTools.Method(ResearchNodeType(), "TechprintAvailable", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TechprintAvailable)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(ResearchNodeType(), "MissingFacilities", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(MissingFacilities)))).Patch();
            if (!altRPal) instance.Patch(AccessTools.Method(ResearchNodeType(), "Draw"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Draw_Prefix))));
            instance.Patch(AccessTools.PropertyGetter(ResearchNodeType(), "Color"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Color_Prefix))));
            if (!altRPal) instance.Patch(AccessTools.PropertyGetter(ResearchNodeType(), "EdgeColor"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(EdgeColor_Prefix)))); //fixed
            instance.Patch(AccessTools.Constructor(ResearchNodeType(), new Type[] { typeof(ResearchProjectDef) }),
                null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ResearchNode_Postfix))));
            instance.Patch(AccessTools.Method(ResearchNodeType(), "GetResearchTooltipString"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(GetResearchTooltipString_Prefix))));

            if (!altRPal) GetMissingRequiredRecursiveInfo = AccessTools.Method(ResearchNodeType(), "GetMissingRequiredRecursive"); //Notfound
            ChildrenInfo = AccessTools.Property(ResearchNodeType(), "Children"); 
            ColorInfo = AccessTools.Property(ResearchNodeType(), "Color");
            if (!altRPal)
            {
                AvailableInfo = AccessTools.Property(ResearchNodeType(), "Available"); //Notfound
                CompletedInfo = AccessTools.Property(ResearchNodeType(), "Completed"); //Notfound
            }
            ResearchInfo = AccessTools.Field(ResearchNodeType(), "Research");
            GetResearchTooltipStringInfo = AccessTools.Method(ResearchNodeType(), "GetResearchTooltipString");

            //Def_Extensions
            instance.CreateReversePatcher(AccessTools.Method(modName + ".Def_Extensions:DrawColouredIcon"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(DrawColouredIcon)))).Patch();
            //fix for tree overlapping search bar on higher UI scales
            instance.Patch(AccessTools.Method(MainTabType(), "SetRects"),
                null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Set_Rects_Postfix))));

            InstanceInfo = AccessTools.Property(MainTabType(), "Instance");
            ZoomLevelInfo = AccessTools.Property(MainTabType(), "ZoomLevel");

            //MainTabWindow_ResearchTree
            instance.Patch(AccessTools.Method(MainTabType(), "DoWindowContents"),
                null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(DoWindowContents_Postfix))));
            if (modName != "ResearchPal")
            {
                instance.Patch(AccessTools.Method(MainTabType(), "Notify_TreeInitialized"),
                    null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TreeInitialized_Postfix))));
            }
            instance.Patch(AccessTools.Method(typeof(Window), "PostClose"),
                    null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Close_Postfix))));
            Type windowNodeType = AltRPal ? ResearchNodeType() : NodeType();
            MainTabCenterOnInfo = AccessTools.Method(MainTabType(), "CenterOn", new Type[] { windowNodeType });

        //Tree
            instance.Patch(AccessTools.Method(TreeType(), "PopulateNodes"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(PopulateNodes_Prefix))),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(PopulateNodes_Postfix))));
            if (modName == "ResearchPal")
            {
                instance.Patch(AccessTools.Method(TreeType(), "Initialize"),
                    null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TreeInitialized_Postfix))));
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

            Harmony.DEBUG = false;
        }

        public static PropertyInfo NodesInfo;
        private static IEnumerable NodesList;
        private static Dictionary<ResearchProjectDef, object> ResearchNodesCache = new Dictionary<ResearchProjectDef, object>();

        //Constants
        public static FieldInfo
            EpsilonInfo,
            DetailedModeZoomLevelCutoffInfo,
            MarginInfo,
            QueueLabelSizeInfo,
            IconSizeInfo,
            NodeMarginsInfo,
            NodeSizeInfo,
            TopBarHeightInfo;

        #region "main patches"

        public static List<Pair<Def, string>> GetUnlockDefsAndDescs(ResearchProjectDef research, bool dedupe = true) { throw stubMsg; }
        public static bool BuildingPresent(ResearchProjectDef research) { throw stubMsg; }
        public static bool TechprintAvailable(ResearchProjectDef research) { throw stubMsg; }
        public static List<ThingDef> MissingFacilities(ResearchProjectDef research) { throw stubMsg; }
        public static void DrawColouredIcon(this Def def, Rect canvas) { throw stubMsg; }
        public static IEnumerable<RecipeDef> GetRecipesUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static IEnumerable<ThingDef> GetThingsUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static IEnumerable<ThingDef> GetPlantsUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static List<ResearchProjectDef> Ancestors(this ResearchProjectDef research) { throw stubMsg; }

        public static ResearchProjectDef subjectToShow;
        private static MethodInfo MainTabCenterOnInfo;
        private static PropertyInfo TreeNodesListInfo => AccessTools.Property(TreeType(), "Nodes");

        private static bool treeReady = false;

        public static void TreeInitialized_Postfix(object __instance)
        {
            treeReady = !ResearchNodesCache.EnumerableNullOrEmpty();
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

        private static void ResearchNode_Postfix(object __instance, ResearchProjectDef research)
        {
            if (populating) ResearchNodesCache.Add(research, __instance);
        }

        private static bool GetResearchTooltipString_Prefix(ResearchProjectDef ___Research, ref string __result)
        {
            var text = new StringBuilder();
            text.AppendLine(___Research.description);
            if (DebugSettings.godMode && !HarmonyPatches.ResearchPal) text.AppendLine("Fluffy.ResearchTree.RClickInstaFinish".Translate()); //There's no corresponding line on ResearchPal, but it works anyway. 
            __result = text.ToString();
            return false;
        }

        public static void DoWindowContents_Postfix(object __instance)
        {
            if (subjectToShow != null && treeReady)
            {
                MainTabCenterOnInfo.Invoke(__instance, new object[] { ResearchNodesCache[subjectToShow] });
                HighlightedProxy(ResearchNodesCache[subjectToShow], true);
                //subjectToShow = null;
            }
        }

        public static void Close_Postfix(object __instance)
        {
            if (__instance.GetType() == MainTabType()) Extension_Research.currentPawnsCache?.Clear();
            if (AltRPal && subjectToShow != null) UnhighlightInfo.Invoke(ResearchNodesCache[subjectToShow], new object[] { 0 });
        }

        public static bool QueueDraw_Prefix(Rect baseCanvas)
        {
            return DrawQueue_Prefix(baseCanvas);
        }

        private static bool DrawQueue_Prefix(Rect canvas)
        {
            float height = canvas.height;
            float frameOffset = height / 4;
            float startPos = canvas.xMax - height - 12f;
            Vector2 size = new Vector2(height + Find.ColonistBar.SpaceBetweenColonistsHorizontal, height - 12f);
            using (IEnumerator<Pawn> enumerator = Find.ColonistBar.GetColonistsInOrder().AsEnumerable().Reverse().GetEnumerator())// PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists.Where(x => x.TryGetComp<CompKnowledge>() != null).Reverse().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Vector2 position = new Vector2(startPos, canvas.y);
                    Rect box = new Rect(position, size);
                    Pawn pawn = enumerator.Current;
                    GUI.DrawTexture(box, PortraitsCache.Get(pawn, size, default, 1.4f));
                    CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
                    if (Mouse.IsOver(box.ContractedBy(12f)))
                    {
                        if (subjectToShow != null) subjectToShow = null;
                        if (!techComp.expertise.EnumerableNullOrEmpty())
                        {
                            foreach (ResearchProjectDef tech in techComp.expertise.Keys)
                            {
                                HighlightedProxy(ResearchNodesCache[tech], true);
                            }
                        }
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
            return false;
        }

        private static void Set_Rects_Postfix(object __instance)
        {
            FieldInfo baseViewRectInfo = AccessTools.Field(MainTabType(), "_baseViewRect");
            baseViewRectInfo.SetValue(__instance, new Rect(
                Window.StandardMargin / Prefs.UIScale,
                (ResearchTree_Constants.TopBarHeight + ResearchTree_Constants.Margin + Window.StandardMargin),
                (Screen.width - Window.StandardMargin * 2f) / Prefs.UIScale,
                ((Screen.height - MainButtonDef.ButtonHeight - Window.StandardMargin * 3) / Prefs.UIScale) - ResearchTree_Constants.TopBarHeight - ResearchTree_Constants.Margin * 2 )
                );
        }

        //Node
        private static MethodInfo IsVisibleInfo;
        private static PropertyInfo
            HighlightedInfo,
            RectInfo,
            LabelRectInfo,
            CostLabelRectInfo,
            CostIconRectInfo,
            IconsRectInfo;
        private static FieldInfo largeLabelInfo;

        //Research Node
        private static MethodInfo
            GetMissingRequiredRecursiveInfo,
            GetResearchTooltipStringInfo;
        private static PropertyInfo 
            ChildrenInfo,
            ColorInfo,
            AvailableInfo,
            CompletedInfo;
        private static FieldInfo ResearchInfo;

        //MainWindow
        private static PropertyInfo 
            InstanceInfo,
            ZoomLevelInfo;

        public static bool Draw_Prefix(object __instance, Rect visibleRect, bool forceDetailedMode = false)
        {
            //Reflected objects
            Rect rect = (Rect)RectInfo.GetValue(__instance);
            ResearchProjectDef Research = (ResearchProjectDef)ResearchInfo.GetValue(__instance);
            bool available = (bool)AvailableInfo.GetValue(__instance);
            bool completed = (bool)CompletedInfo.GetValue(__instance);
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
                GUI.color = mouseOver ? HighlightColor : (Color)ColorInfo.GetValue(__instance);
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
                HighlightedProxy(__instance, subjectToShow == Research);

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
                    if (subjectToShow != null && subjectToShow != Research) subjectToShow = null;

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

            //if clicked and not yet finished, queue up this research and all prereqs.
            if (Widgets.ButtonInvisible(rect))
            {
                //LMB is queue operations, RMB is info
                if (Event.current.button == 0) Research.SelectMenu(completed);
                if (DebugSettings.godMode && Prefs.DevMode && Event.current.button == 1 && !Research.IsFinished)
                {
                    Find.ResearchManager.FinishProject(Research);
                }
            }
            return false;
        }

        private static Color HighlightColor = new Color(1f, 0.85f, 0.2f);
        public static bool EdgeColor_Prefix(object __instance, ref Color __result)
        {
            bool flag = HighlightedProxy(__instance);
            if (flag)
            {
                __result = HighlightColor;
                return false;
            }
            return true;
        }

        public static bool Color_Prefix(object __instance, ref Color __result)
        {
            bool flag = HighlightedProxy(__instance);
            if (flag)
            {
                __result = HighlightColor;
                return false;
            }
            return true;
        }

        #endregion

        #region "VinaLx.ResearchPalForked adaptation"

        public static bool AltRPal = false;


        public static void HighlightedProxy(object node, bool setting)
        {
            //Set
            if (AltRPal) HighlightInfo.Invoke(node, new object[] { 7 });
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

        public static bool Highlighted() { throw stubMsg; }

        private static MethodInfo
            ResearchNodeInfo,
            BuildingPresentInfo,
            HighlightInfo,
            UnhighlightInfo;

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

        #endregion

    }
}


