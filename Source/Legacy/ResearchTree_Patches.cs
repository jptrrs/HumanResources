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
    //Changed in RW 1.3

    using static ResearchTree_Constants;
    public static class ResearchTree_Patches
    {
        #region "variables"
        public static object MainTabInstance;
        public static ResearchProjectDef Interest;
        public static CompKnowledge Context;
        public static Color
            BrightColor = new Color(1f, 0.85f, 0.2f), //yellow
            ShadedColor = new Color(0.72f, 0.57f, 0.13f), //mustard
            VariantColor = new Color(1f, 0.6f, 0.08f); //light orange
        private static string ModName = "";
        private static bool
            populating = false,
            nodeSizeHacked = false,
            treeReady = false;
        private static Dictionary<ResearchProjectDef, object> ResearchNodesCache = new Dictionary<ResearchProjectDef, object>();
        private static NotImplementedException stubMsg = new NotImplementedException("ResearchTree reverse patch");
        private static Vector2
            oldNodeSize,
            newNodeSize;
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
        //Node
            CostLabelRectInfo,
            CostIconRectInfo,
            EdgeColorInfo,
            LabelRectInfo,
            RectInfo,
            RightInfo,
            XInfo,
        //ResearchNode:
            AvailableInfo,
            ChildrenInfo,
            ColorInfo,
            HighlightedInfo,
            IconsRectInfo,
        //MainWindow:
            InstanceInfo,
            ZoomLevelInfo,
        //Edge:
            InInfo,
            OutInfo;

        private static MethodInfo
        //ResearchNode:
            GetMissingRequiredRecursiveInfo,
            GetResearchTooltipStringInfo,
        //Node:
            IsVisibleInfo,
            InEdgeColorInfo,
        //MainWindow:
            MainTabCenterOnInfo,
        //Tree:
            HandleFixedHighlightInfo,
            StopFixedHighlightsInfo,
        //Edge:
            InResearchInfo,
        //Queue
            IsQueuedInfo,
            EnqueueInfo,
            DequeueInfo;

        private static FieldInfo
        //Node:
            largeLabelInfo,
            ResearchInfo,
            _rightInfo,
        //ResearchNode:
            isMatchedInfo,
        //MainTabWindow_ResearchTree
            searchActiveInfo,
        //Assets
            NormalHighlightColorInfo,
            HoverPrimaryColorInfo,
            FixedPrimaryColorInfo;

        public static Type AssetsType() => AccessTools.TypeByName(ModName + ".Assets");
        public static Type LinesType() => AccessTools.Inner(AssetsType(), "Lines");
        public static Type ConstantsType() => AccessTools.TypeByName(ModName + ".Constants");
        public static Type MainTabType() => AccessTools.TypeByName(ModName + ".MainTabWindow_ResearchTree");
        public static Type NodeType() => AccessTools.TypeByName(ModName + ".Node");
        public static Type QueueType() => AccessTools.TypeByName(ModName + ".Queue");
        public static Type ResearchNodeType() => AccessTools.TypeByName(ModName + ".ResearchNode");
        public static Type TreeType() => AccessTools.TypeByName(ModName + ".Tree");
        public static Type EdgeType<T1, T2>() => AccessTools.TypeByName(ModName + ".Edge`2").MakeGenericType(new Type[] { NodeType(), NodeType() });


        private static Func<Pawn, ResearchProjectDef, bool> HasBeenAssigned = (pawn, tech) =>
        {
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            if (techComp != null && !techComp.homework.NullOrEmpty())
            {
                return techComp.homework.Contains(tech);
            }
            return false;
        };
        #endregion

        #region "patchworks"
        public static void Execute(Harmony instance, string modName, bool altRPal = false)
        {
            //Harmony.DEBUG = true;
            ModName = modName;
            AltRPal = altRPal;
            List<string> FailedFields = new List<string>();
            List<string> FailedProperties = new List<string>();

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
            if (altRPal)
            {
                ResearchNodeInfo = AccessTools.Method(modName + ".ResearchProjectDef_Extensions:ResearchNode");
            }

            //Node
            IsVisibleInfo = AccessTools.Method(NodeType(), "IsVisible");
            RectInfo = GetPropertyOrFeedback(NodeType(), "Rect", ref FailedProperties);
            RightInfo = GetPropertyOrFeedback(NodeType(), "Right", ref FailedProperties);
            _rightInfo = GetFieldOrFeedback(NodeType(), "_right", ref FailedFields);
            largeLabelInfo = GetFieldOrFeedback(NodeType(), "_largeLabel", ref FailedFields);
            LabelRectInfo = GetPropertyOrFeedback(NodeType(), "LabelRect", ref FailedProperties);
            CostLabelRectInfo = GetPropertyOrFeedback(NodeType(), "CostLabelRect", ref FailedProperties);
            CostIconRectInfo = GetPropertyOrFeedback(NodeType(), "CostIconRect", ref FailedProperties);
            IconsRectInfo = GetPropertyOrFeedback(NodeType(), "IconsRect", ref FailedProperties);
            XInfo = GetPropertyOrFeedback(NodeType(), "X", ref FailedProperties);
            if (altRPal)
            {
                instance.CreateReversePatcher(AccessTools.Method(modName + ".Node:Highlighted"),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Highlighted)))).Patch();
                InEdgeColorInfo = AccessTools.Method(modName + ".Node:InEdgeColor");
            }
            else
            {
                EdgeColorInfo = GetPropertyOrFeedback(NodeType(), "EdgeColor", ref FailedProperties);
                HighlightedInfo = GetPropertyOrFeedback(NodeType(), "Highlighted", ref FailedProperties);
            }
            instance.Patch(AccessTools.Method(NodeType(), "SetRects", new Type[] { typeof(Vector2) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Node_SetRects_Prefix))),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Node_SetRects_Postfix))));

            //ResearchNode
            if (altRPal)
            {
                instance.Patch(AccessTools.Method(ResearchNodeType(), "HandleDragging", new Type[] { typeof(bool) }),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(HandleDragging_Prefix))));
                instance.Patch(AccessTools.Method(ResearchNodeType(), "LeftClick"),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(LeftClick_Prefix))));
                instance.Patch(AccessTools.Method(ResearchNodeType(), "TechLevelTooLowTooltip"),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TechLevelTooLowTooltip_Prefix))));
                instance.Patch(AccessTools.Method(ResearchNodeType(), "ShortcutManualTooltip"),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ShortcutManualTooltip_Prefix))),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ShortcutManualTooltip_Postfix))));
                instance.Patch(AccessTools.Method(ResearchNodeType(), "Unhighlight"),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Unhighlight_Prefix))));
                HighlightInfo = AccessTools.Method(ResearchNodeType(), "Highlight");
                BuildingPresentInfo = AccessTools.Method(ResearchNodeType(), "BuildingPresent", new Type[] { ResearchNodeType() });
                isMatchedInfo = GetFieldOrFeedback(ResearchNodeType(), "isMatched", ref FailedFields);
                UnlockItemTooltipInfo = AccessTools.Method(ResearchNodeType(), "UnlockItemTooltip");
            }
            else
            {
                instance.CreateReversePatcher(AccessTools.Method(ResearchNodeType(), "BuildingPresent", new Type[] { typeof(ResearchProjectDef) }),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(BuildingPresent)))).Patch();
                instance.Patch(AccessTools.Method(ResearchNodeType(), "Draw"),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ResearchNode_Draw_Prefix))));
                instance.Patch(AccessTools.PropertyGetter(ResearchNodeType(), "EdgeColor"),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(EdgeColor_Prefix))));
                GetMissingRequiredRecursiveInfo = AccessTools.Method(ResearchNodeType(), "GetMissingRequiredRecursive");
                AvailableInfo = GetPropertyOrFeedback(ResearchNodeType(), "Available", ref FailedProperties);
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
            ChildrenInfo = GetPropertyOrFeedback(ResearchNodeType(), "Children", ref FailedProperties);
            ColorInfo = GetPropertyOrFeedback(ResearchNodeType(), "Color", ref FailedProperties);
            ResearchInfo = GetFieldOrFeedback(ResearchNodeType(), "Research", ref FailedFields);
            GetResearchTooltipStringInfo = AccessTools.Method(ResearchNodeType(), "GetResearchTooltipString");

            //Def_Extensions
            instance.CreateReversePatcher(AccessTools.Method(modName + ".Def_Extensions:DrawColouredIcon"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(DrawColouredIcon)))).Patch();

            //Edge
            instance.Patch(AccessTools.Method(EdgeType<Type, Type>(), "Draw"), null,
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Edge_Draw_Postfix))));
            InInfo = GetPropertyOrFeedback(EdgeType<Type, Type>(), "In", ref FailedProperties);
            OutInfo = GetPropertyOrFeedback(EdgeType<Type, Type>(), "Out", ref FailedProperties);
            if (altRPal) InResearchInfo = AccessTools.Method(EdgeType<Type, Type>(), "InResearch");

            //MainTabWindow_ResearchTree
            if (AltRPal)
            {
                searchActiveInfo = GetFieldOrFeedback(MainTabType(), "_searchActive", ref FailedFields);
            }
            else
            {
                ZoomLevelInfo = GetPropertyOrFeedback(MainTabType(), "ZoomLevel", ref FailedProperties);

                //fix for tree overlapping search bar on higher UI scales
                instance.Patch(AccessTools.Method(MainTabType(), "SetRects"),
                    null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(MainTabWindow_SetRects_Postfix))));
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
            instance.Patch(AccessTools.PropertyGetter(MainTabType(), "TreeRect"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TreeRect_Prefix))));
            InstanceInfo = GetPropertyOrFeedback(MainTabType(), "Instance", ref FailedProperties);
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

            //Queue
            if (altRPal)
            {
                instance.Patch(AccessTools.Method(QueueType(), "DrawS"),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(QueueDraw_Prefix))));
                IsQueuedInfo = AccessTools.Method(QueueType(), "ContainsS");
                AppendSInfo = AccessTools.Method(QueueType(), "AppendS");
                DequeueInfo = AccessTools.Method(QueueType(), "RemoveS");
            }
            else
            {
                instance.Patch(AccessTools.Method(QueueType(), "DrawQueue"),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(DrawQueue_Prefix))));
                IsQueuedInfo = AccessTools.Method(QueueType(), "IsQueued");
                EnqueueInfo = AccessTools.Method(QueueType(), "Enqueue", new Type[] { ResearchNodeType(), typeof(bool) });
                DequeueInfo = AccessTools.Method(QueueType(), "Dequeue");
            }
            instance.Patch(AccessTools.Method(QueueType(), "DrawLabel"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Inhibitor))));

            //Constants
            EpsilonInfo = GetFieldOrFeedback(ConstantsType(), "Epsilon", ref FailedFields);
            DetailedModeZoomLevelCutoffInfo = GetFieldOrFeedback(ConstantsType(), "DetailedModeZoomLevelCutoff", ref FailedFields);
            MarginInfo = GetFieldOrFeedback(ConstantsType(), "Margin", ref FailedFields);
            QueueLabelSizeInfo = GetFieldOrFeedback(ConstantsType(), "QueueLabelSize", ref FailedFields);
            IconSizeInfo = GetFieldOrFeedback(ConstantsType(), "IconSize", ref FailedFields);
            NodeMarginsInfo = GetFieldOrFeedback(ConstantsType(), "NodeMargins", ref FailedFields);
            NodeSizeInfo = GetFieldOrFeedback(ConstantsType(), "NodeSize", ref FailedFields);
            TopBarHeightInfo = GetFieldOrFeedback(ConstantsType(), "TopBarHeight", ref FailedFields);
            if (altRPal) TopBarHeightInfo.SetValue(instance, NodeSize.y * 0.6f + 2 * Margin);

            //Assets
            if (altRPal)
            {
                NormalHighlightColorInfo = GetFieldOrFeedback(AssetsType(), "NormalHighlightColor", ref FailedFields); //default blue
                HoverPrimaryColorInfo = GetFieldOrFeedback(AssetsType(), "HoverPrimaryColor", ref FailedFields); //violet
                FixedPrimaryColorInfo = GetFieldOrFeedback(AssetsType(), "FixedPrimaryColor", ref FailedFields); //cyan
                NormalHighlightColorInfo.SetValue(instance, ShadedColor); //mustard
                HoverPrimaryColorInfo.SetValue(instance, BrightColor); //yellow
                FixedPrimaryColorInfo.SetValue(instance, VariantColor); //light orange
            }

            //Base game
            if (altRPal)
            {
                instance.Patch(AccessTools.Method(typeof(TooltipHandler), "TipRegion", new Type[] { typeof(Rect), typeof(Func<string>), typeof(int) }),
                    new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TooltipHandler_TipRegion_Prefix))));
            }

            if (!FailedFields.NullOrEmpty()) Log.Error($"[HumanResources] Failed to reflect these fields: {FailedFields.ToStringSafeEnumerable()}. Likely an unforseen update on ResearchTree/Pal");
            if (!FailedProperties.NullOrEmpty()) Log.Error($"[HumanResources] Failed to reflect these properties: {FailedProperties.ToStringSafeEnumerable()}. Likely an unforseen update on ResearchTree/Pal");

            //Harmony.DEBUG = false;
        }

        private static FieldInfo GetFieldOrFeedback(Type type, string name, ref List<string> failedList)
        {
            var field = AccessTools.Field(type, name);
            if (field == null) failedList.Add($"{type}.{name}");
            return field;
        }

        private static PropertyInfo GetPropertyOrFeedback(Type type, string name, ref List<string> failedList)
        {
            var property = AccessTools.Property(type, name);
            if (property == null) failedList.Add($"{type}.{name}");
            return property;
        }

        #endregion

        #region "main patches"

        public static bool Inhibitor()
        {
            return false;
        }

        public static List<ResearchProjectDef> Ancestors(this ResearchProjectDef research) { throw stubMsg; }

        public static bool BuildingPresent(ResearchProjectDef research) { throw stubMsg; }

        public static void Close_Postfix(object __instance)
        {
            if (__instance.GetType() == MainTabType()) Extension_Research.currentPawnsCache?.Clear();
            if (nodeSizeHacked)
            {
                NodeSizeInfo.SetValue(__instance, oldNodeSize);
                nodeSizeHacked = false;
            }
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
            if (Interest != null) Interest = null;
        }

        public static void DoWindowContents_Postfix(object __instance)
        {
            if (MainTabInstance == null) MainTabInstance = __instance;
            if (Interest == null) return;
            if (treeReady)
            {
                MainTabCenterOnInfo.Invoke(__instance, new object[] { ResearchNodesCache[Interest] });
                HighlightedProxy(ResearchNodesCache[Interest], true, 4);
                IEnumerable<object> expertiseDisplay = new object[] { };
                ReflectKnowledge(Context, out expertiseDisplay);
                if (!AltRPal) return;
                UpdateMatches(expertiseDisplay);
                expertiseDisplayed = true;
                DeInterest();
            }
            else Log.Warning("[HumanResources] Locate tech on the Research Tab failed: the tree isn't ready");
        }

        public static void DrawColouredIcon(this Def def, Rect canvas) { throw stubMsg; }

        public static bool DrawQueue_Prefix(object __instance, Rect canvas)
        {
            if (AltRPal)
            {
                canvas.xMax += 130f + 2 * Margin; //keep an eye on his MainTabWindow_ResearchTree.DrawTopBar method for changes to this number
                canvas = canvas.ExpandedBy(Margin);
            }
            float padding = 12f;
            float size = canvas.height;
            float startPos = canvas.xMax - (size / 2) - padding;
            var colonists = Find.ColonistBar.GetColonistsInOrder();
            float space = canvas.width - 2 * padding;
            bool excess = (colonists.Count() * size - space) > 0;
            float spacing = excess ? space / colonists.Count() : size;
            Vector2 boxSize = new Vector2(spacing, size - padding);
            IEnumerable<object> expertiseDisplay = Array.Empty<object>();
            bool displayActive = false;
            using (IEnumerator<Pawn> enumerator = colonists.Where(x => x.TechBound()).AsEnumerable().Reverse().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Pawn pawn = enumerator.Current;
                    Vector2 position = new Vector2(startPos, canvas.y);
                    DrawColonistOnTree(padding, size, excess, boxSize, ref expertiseDisplay, ref displayActive, pawn, position);
                    startPos -= spacing;

                    //GUI.DrawTexture(box, PortraitsCache.Get(pawn, size, default, 1.4f));
                    //CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
                    //if (Mouse.IsOver(innerBox))
                    //{
                    //    DeInterest();
                    //    ReflectKnowledge(techComp, out expertiseDisplay);
                    //    displayActive = true;
                    //}
                    //if (!techComp.homework.NullOrEmpty())
                    //{
                    //    StringBuilder homeworkSummary = new StringBuilder();
                    //    homeworkSummary.AppendLine("AssignedTo".Translate(pawn));
                    //    foreach (var tech in techComp.homework)
                    //    {
                    //        homeworkSummary.AppendLine("- " + TechStrings.GetTask(pawn, tech) + " " + tech.label);
                    //    }
                    //    TooltipHandler.TipRegionByKey(box, homeworkSummary.ToString());
                    //}
                    //Vector2 pos = new Vector2(box.center.x, box.yMax);
                    //GenMapUI.DrawPawnLabel(pawn, pos, 1f, box.width, null, GameFont.Tiny, false, true);
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

        private static void DrawColonistOnTree(float padding, float size, bool excess, Vector2 boxSize, ref IEnumerable<object> expertiseDisplay, ref bool displayActive, Pawn pawn, Vector2 position)
        {
            Rect box = new Rect(position, boxSize);
            Rect innerBox = new Rect(position.x + padding, position.y, boxSize.x - 2 * padding, boxSize.y);
            bool mouseOver = Mouse.IsOver(innerBox);
            CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
            if (mouseOver)
            {
                DeInterest();
                ReflectKnowledge(techComp, out expertiseDisplay);
                displayActive = true;
            }
            ColonistHighlight(size, excess, pawn, box, mouseOver, techComp.raisedHand);
            GUI.DrawTexture(box, PortraitsCache.Get(pawn, boxSize, default, 1.4f));
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
        }

        private static void ColonistHighlight(float size, bool excess, Pawn pawn, Rect box, bool mouseOver, bool raisedHand)
        {
            GUI.color = raisedHand ? ShadedColor : GUI.color;
            Texture texture = raisedHand ? BaseContent.WhiteTex : TexUI.HighlightTex;
            if (mouseOver || raisedHand) GUI.DrawTexture(box, texture);
            Vector2 pos = new Vector2(box.center.x, box.yMax);
            if (!excess || mouseOver) GenMapUI.DrawPawnLabel(pawn, pos, 1f, size, null, GameFont.Tiny, false, true);
            GUI.color = Color.white;
        }

        public static bool EdgeColor_Prefix(object __instance, ref Color __result)
        {
            if (HighlightedProxy(__instance))
            {
                __result = BrightColor;
                return false;
            }
            return true;
        }

        public static void Edge_Draw_Postfix(object __instance)
        {
            object origin = InInfo.GetValue(__instance);
            if (origin != null && ResearchNodesCache.Values.Contains(origin))
            {
                object next = OutInfo.GetValue(__instance);
                Vector2 fauxPos = (Vector2)RightInfo.GetValue(origin);
                fauxPos.y -= 2;
                fauxPos -= push;
                Vector2 size = new Vector2(push.x, 4f);
                var line = new Rect(fauxPos, size);
                Color backup = GUI.color;
                GUI.color = AltRPal ? (Color)InEdgeColorInfo.Invoke(next, new object[] { InResearchInfo.Invoke(__instance, new object[] { }) }) : (Color)EdgeColorInfo.GetValue(next);
                GUI.DrawTexture(line, ResearchTree_Assets.EW);
                GUI.color = backup;
            }
        }

        public static IEnumerable<ThingDef> GetPlantsUnlocked(this ResearchProjectDef research) { throw stubMsg; }

        public static IEnumerable<RecipeDef> GetRecipesUnlocked(this ResearchProjectDef research) { throw stubMsg; }

        public static IEnumerable<ThingDef> GetThingsUnlocked(this ResearchProjectDef research) { throw stubMsg; }

        public static bool GetResearchTooltipString_Prefix(ResearchProjectDef ___Research, ref string __result)
        {
            var text = new StringBuilder();
            text.AppendLine(___Research.description);
            if (DebugSettings.godMode && !HarmonyPatches.ResearchPal) text.AppendLine("Fluffy.ResearchTree.RClickInstaFinish".Translate()); //There's no corresponding line on ResearchPal, but it works anyway. 
            __result = text.ToString();
            return false;
        }
       
        public static List<Pair<Def, string>> GetUnlockDefsAndDescs(ResearchProjectDef research, bool dedupe = true) { throw stubMsg; }

        public static void MainTabWindow_SetRects_Postfix(object __instance)
        {
            FieldInfo baseViewRectInfo = AccessTools.Field(MainTabType(), "_baseViewRect");
            baseViewRectInfo.SetValue(__instance, new Rect(
                Window.StandardMargin / Prefs.UIScale,
                TopBarHeight + Margin + Window.StandardMargin,
                (Screen.width - Window.StandardMargin * 2f) / Prefs.UIScale,
                ((Screen.height - MainButtonDef.ButtonHeight - Window.StandardMargin * 3) / Prefs.UIScale) - TopBarHeight - Margin * 2)
                );
        }

        public static List<ThingDef> MissingFacilities(ResearchProjectDef research) { throw stubMsg; }

        public static void Node_SetRects_Prefix(object __instance)
        {
            if (nodeSizeHacked)
            {
                NodeSizeInfo.SetValue(__instance, oldNodeSize);
            }
        }

        public static void Node_SetRects_Postfix(object __instance)
        {
            if (nodeSizeHacked) 
            {
                NodeSizeInfo.SetValue(__instance, newNodeSize);
                Vector2 rightedge = (Vector2)_rightInfo.GetValue(__instance);
                rightedge += push;
                _rightInfo.SetValue(__instance, rightedge);
            }
        }

        public static void PopulateNodes_Postfix()
        {
            populating = false;
        }

        public static void PopulateNodes_Prefix()
        {
            populating = true;
        }

        private static void ReflectKnowledge(CompKnowledge techComp, out IEnumerable<object> expertiseDisplay)
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

        public static bool ResearchNode_Draw_Prefix(object __instance, Rect visibleRect, bool forceDetailedMode = false)
        {
            //Reflected objects
            Rect rect = (Rect)RectInfo.GetValue(__instance);
            ResearchProjectDef Research = (ResearchProjectDef)ResearchInfo.GetValue(__instance);
            bool available = (bool)AvailableInfo.GetValue(__instance);
            bool completed = Research.IsFinished; //simplified
                                                  //

            if (!(bool)IsVisibleInfo.Invoke(__instance, new object[] { visibleRect }))
            {
                HighlightedProxy(__instance, false);
                return false;
            }
            bool detailedMode = forceDetailedMode || (float)ZoomLevelInfo.GetValue(InstanceInfo.GetValue(__instance)) < DetailedModeZoomLevelCutoff;
            bool mouseOver = Mouse.IsOver(rect);
            bool highlighted = HighlightedProxy(__instance);

            if (Event.current.type == EventType.Repaint)
            {
                //researches that are completed or could be started immediately, and that have the required building(s) available
                GUI.color = mouseOver ? BrightColor : (Color)ColorInfo.GetValue(__instance);
                if (mouseOver || highlighted) GUI.DrawTexture(rect, ResearchTree_Assets.ButtonActive);
                else GUI.DrawTexture(rect, ResearchTree_Assets.Button);

                //grey out center to create a progress bar effect, completely greying out research not started.
                if (available)
                {
                    var progressBarRect = rect.ContractedBy(3f);
                    GUI.color = ResearchTree_Assets.ColorAvailable[Research.techLevel];
                    progressBarRect.xMin += Research.ProgressPercent * progressBarRect.width;
                    GUI.DrawTexture(progressBarRect, BaseContent.WhiteTex);
                }
                HighlightedProxy(__instance, Interest == Research);

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
                    Text.Font = Research.baseCost > 1000000 ? GameFont.Tiny : GameFont.Small;
                    Widgets.Label((Rect)CostLabelRectInfo.GetValue(__instance), Research.baseCost.ToStringByStyle(ToStringStyle.Integer));
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
                else if (!TechprintAvailable(Research))
                {
                    string languageKey = root + ".MissingTechprints";
                    TooltipHandler.TipRegion(rect, languageKey.Translate(Research.TechprintsApplied, Research.techprintCount));
                }

                //draw unlock icons
                if (detailedMode)
                {
                    Rect IconsRect = (Rect)IconsRectInfo.GetValue(__instance);
                    var unlocks = GetUnlockDefsAndDescs(Research);
                    for (var i = 0; i < unlocks.Count; i++)
                    {
                        var iconRect = new Rect(
                            IconsRect.xMax - (i + 1) * (IconSize.x + 4f),
                            IconsRect.yMin + (IconsRect.height - IconSize.y) / 2f,
                            IconSize.x,
                            IconSize.y);

                        if (iconRect.xMin - IconSize.x < IconsRect.xMin &&
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
                    ResearchTree_Watcher.OnTechHovered(__instance, Research);
                    if (Interest != null && Interest != Research) DeInterest();

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
                else ResearchTree_Watcher.OnHoverOut(__instance, Research);
            }

            //CUSTOM: a bunch of things on top
            Research.DrawExtras(rect, mouseOver || highlighted);

            if (Widgets.ButtonInvisible(rect))
            {
                //CUSTOM: replaced queue operations for assignment menu
                if (Event.current.button == 0) Research.SelectMenu(completed);
                if (DebugSettings.godMode && Prefs.DevMode && Event.current.button == 1 && !Research.IsFinished)
                {
                    Find.ResearchManager.FinishProject(Research);
                    Research.WipeAssignments();
                }
            }

            return false;
        }

        public static void ResearchNode_Postfix(object __instance, ResearchProjectDef research)
        {
            if (populating && !ResearchNodesCache.ContainsKey(research)) ResearchNodesCache.Add(research, __instance);
        }

        public static bool TechprintAvailable(ResearchProjectDef research) { throw stubMsg; }

        public static void TreeRect_Prefix(object __instance)
        {
            if (!nodeSizeHacked)
            {
                oldNodeSize = (Vector2)NodeSizeInfo.GetValue(__instance);
                newNodeSize = oldNodeSize + push;
                NodeSizeInfo.SetValue(__instance, newNodeSize);
                nodeSizeHacked = true;
            }
        }

        public static void TreeInitialized_Postfix(object __instance)
        {
            treeReady = !ResearchNodesCache.EnumerableNullOrEmpty();
        }

        private static void UpdateMatches(IEnumerable<object> expertiseDisplay)
        {
            foreach (object node in ResearchNodesCache.Values)
            {
                isMatchedInfo.SetValue(node, expertiseDisplay.Contains(node));
            }
        }

        #endregion

        #region oskarpotocki.vfe.mechanoid adaptation
        public static bool IsQueued(ResearchProjectDef tech)
        {
            return (bool)IsQueuedInfo.Invoke(MainTabInstance, new object[] { ResearchNodesCache[tech] });
        }

        public static void Dequeue(ResearchProjectDef tech)
        {
            DequeueInfo.Invoke(MainTabInstance, new object[] { ResearchNodesCache[tech] });
        }

        public static void EnqueueRange(IEnumerable<ResearchProjectDef> techs)
        {
            foreach (var node in techs.OrderBy(x => XInfo.GetValue(ResearchNodesCache[x])).ThenBy(x => x.baseCost).Select(x => ResearchNodesCache[x]))
            {
                if (AltRPal) AppendSInfo.Invoke(null, new object[] { node });
                else EnqueueInfo.Invoke(MainTabInstance, new object[] { node, true });
            }
        }
        #endregion

        #region VinaLx.ResearchPalForked adaptation

        public static bool 
            AltRPal = false,
            expertiseDisplayed = false;

        private static MethodInfo
            ResearchNodeInfo,
            BuildingPresentInfo,
            HighlightInfo,
            AppendSInfo,
            UnlockItemTooltipInfo;

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

        public static bool HandleDragging_Prefix(object __instance, bool mouseOver, bool ____available)
        {
            ResearchProjectDef Research = (ResearchProjectDef)ResearchInfo.GetValue(__instance);
            if (mouseOver) ResearchTree_Watcher.OnTechHovered(__instance, Research);
            Rect rect = (Rect)RectInfo.GetValue(__instance);
            Research.DrawExtras(rect, mouseOver || HighlightedProxy(__instance));
            if (mouseOver && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                bool completed = Research.IsFinished;
                if (Event.current.button == 0) Research.SelectMenu(completed);
                if (DebugSettings.godMode && Prefs.DevMode && Event.current.button == 1 && !completed)
                {
                    Find.ResearchManager.FinishProject(Research);
                    Research.WipeAssignments();
                }
            }
            return false;
        }

        public static bool Highlighted() { throw stubMsg; }

        public static void HighlightedProxy(object node, bool setting, int reason = 7)
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

        public static void Unhighlight_Prefix(object __instance, ResearchProjectDef ___Research)
        {
            ResearchTree_Watcher.OnHoverOut(__instance, ___Research);
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

        //Redacting misleading Tooltips:
        //1. Mark faction level tooltip by making its text = its id.
        public static bool TechLevelTooLowTooltip_Prefix(ResearchProjectDef ___Research, ref string __result)
        {
            __result = (___Research.GetHashCode() + 3).ToString();
            return false;
        }

        //2. Supress marked tooltips when main research tab window is on.
        public static bool TooltipHandler_TipRegion_Prefix(Func<string> textGetter, int uniqueId)
        {
            return MainTabInstance == null || !textGetter.Invoke().Equals(uniqueId.ToString());
        }

        //3. Masking the actual availability of techs when processing the shortcut tips, so there's no queueing.
        public static void ShortcutManualTooltip_Prefix(ref bool ____available, ref bool __state)
        {
            if (ResearchTreeHelper.QueueAvailable) return;
            __state = ____available;
            ____available = false;
        }

        //4. Restoring actual tech availability when we're done.
        public static void ShortcutManualTooltip_Postfix(ref bool ____available, ref bool __state)
        {
            if (ResearchTreeHelper.QueueAvailable) return;
            ____available = __state;
        }

        #endregion
    }
}


