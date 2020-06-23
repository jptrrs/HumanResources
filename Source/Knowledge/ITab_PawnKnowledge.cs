using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using Verse.Sound;
using HarmonyLib;
using System;
using System.Reflection;

namespace HumanResources
{
    public class ITab_PawnKnowledge : ITab
    {
        private const int iconSize = 29;
        private const int margin = (int)Constants.Margin;
        private const int rowHeight = 32;
        private const float scrollBarWidth = 17f;
        private const float tabSizeAdjust = 12f;
        private static Vector2 scrollPosition = Vector2.zero;
        private static Vector2 scrollPosition2 = Vector2.zero;
        private int cellWidth = 200;
        private Vector2 nodeSize => Constants.NodeSize;
        private static bool filterWeapons = false;
        private static bool showAvailable = false;
        private static bool fullTechs = false;
        private static bool fullWeapons = false;
        private static bool expandTab => fullTechs | fullWeapons;
        private Vector2 buttonSize = new Vector2(rowHeight, rowHeight);

        public ITab_PawnKnowledge()
        {
            labelKey = "TabKnowledge";
        }

        public override bool IsVisible
        {
            get
            {
                Pawn pawn = PawnToShowInfoAbout;
                return pawn != null && pawn.TryGetComp<CompKnowledge>() != null;
            }
        }

        private Pawn PawnToShowInfoAbout
        {
            get
            {
                Pawn pawn = null;
                if (base.SelPawn != null)
                {
                    pawn = base.SelPawn;
                }
                else
                {
                    Corpse corpse = base.SelThing as Corpse;
                    if (corpse != null)
                    {
                        pawn = corpse.InnerPawn;
                    }
                }
                if (pawn == null)
                {
                    Log.Error("[HumanResources] Knowledge tab found no selected pawn to display.");
                    return null;
                }
                return pawn;
            }
        }

        protected override void FillTab()
        {
            float padding = Mathf.Max(margin, 10f);
            Rect canvas = new Rect(margin, padding, size.x - margin - 1f, size.y - padding);
            GUI.BeginGroup(canvas);

            //float columnWidth = nodeSize.x + scrollBarWidth + margin;
            float firstColumnWidth = canvas.width;

            //Left Column
            if (!fullWeapons)
            {
                if (!fullTechs) firstColumnWidth = nodeSize.x + scrollBarWidth + margin;
                Rect leftColumn = new Rect(canvas.x, canvas.y, firstColumnWidth, canvas.height);
                Text.Font = GameFont.Medium;
                Rect titleRect = new Rect(leftColumn.x, leftColumn.y, leftColumn.width, Text.LineHeight);
                Widgets.Label(titleRect, "TabKnowledgeTitle".Translate());
                Text.Font = GameFont.Small;
                var expertise = PawnToShowInfoAbout.TryGetComp<CompKnowledge>()?.expertise;
                if (!expertise.EnumerableNullOrEmpty())
                {
                    var unknownList = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(x => x.IsFinished).Except(expertise.Keys);
                    bool unknownEnabled = !unknownList.EnumerableNullOrEmpty();
                    var currentList = (unknownEnabled && showAvailable) ? unknownList : expertise.Keys;
                    var orderedList = fullTechs ? currentList.OrderBy(x => x.techLevel) : currentList.OrderByDescending(x => x.techLevel);
                    var expertiseList = orderedList.ThenBy(x => x.label).Select(x => new ExpertiseNode(x, PawnToShowInfoAbout)).ToList();
                    float viewHeight = (nodeSize.y + margin) * expertiseList.Count();
                    Rect viewRect = new Rect(0f, 0f, nodeSize.x, viewHeight);
                    Rect scrollrect = new Rect(leftColumn.x, titleRect.yMax + margin, leftColumn.width - margin, leftColumn.height - titleRect.height - rowHeight - margin - padding * 2 - 2f);
                    Widgets.BeginScrollView(scrollrect, ref scrollPosition, viewRect);
                    var pos = new Vector2(0f, 0f);
                    if (!showAvailable | unknownEnabled)
                    {
                        int columnBreak = (int)expertiseList.First().Research.techLevel;
                        for (int i = 0; i < expertiseList.Count && pos.x + nodeSize.x < leftColumn.xMax; i++)
                        {
                            var node = expertiseList[i];
                            if (fullTechs && (int)node.Research.techLevel != columnBreak)
                            {
                                pos.x += nodeSize.x + margin;
                                pos.y = 0f;
                                //columnBreak++;
                                columnBreak = (int)node.Research.techLevel;
                            }
                            var rect = new Rect(pos.x, pos.y, nodeSize.x, nodeSize.y);
                            node.DrawAt(pos, rect, Constants.showCompact);
                            pos.y += nodeSize.y + margin;
                        }
                    }
                    if (Event.current.type == EventType.Layout)
                    {
                        viewHeight = size.y;
                    }
                    Widgets.EndScrollView();
                    if (unknownEnabled)
                    {
                        //Text.Font = GameFont.Tiny;
                        float baselineX = leftColumn.x;
                        float baselineY = scrollrect.max.y + padding;
                        float nextPos = baselineX;
                        DrawToggle(nextPos, baselineY, "ShowAvailable", ref showAvailable, ContentFinder<Texture2D>.Get("UI/available_on", true), ContentFinder<Texture2D>.Get("UI/available_off", true), out nextPos);
                        DrawToggle(nextPos, baselineY, "ShowCompact", ref Constants.showCompact, ContentFinder<Texture2D>.Get("UI/compact_on", true), ContentFinder<Texture2D>.Get("UI/compact_off", true), out nextPos);
                        DrawToggle(nextPos, baselineY, "Expand", ref fullTechs, ContentFinder<Texture2D>.Get("UI/expand_on", true), ContentFinder<Texture2D>.Get("UI/expand_off", true), out nextPos);
                    }
                }
            }

            //Right Column
            if (!fullTechs)
            {
                if (fullWeapons) firstColumnWidth = 0f;
                Rect rightColumn = new Rect(canvas.x + firstColumnWidth, canvas.y, canvas.width - firstColumnWidth, canvas.height);
                Text.Font = GameFont.Medium;
                Rect titleRect2 = new Rect(rightColumn.x, rightColumn.y, rightColumn.width, Text.LineHeight);
                if (!fullWeapons)
                {
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.LowerLeft;
                }
                Widgets.Label(titleRect2, "TabKnowledgeWeapons".Translate() + ":");
                if (fullWeapons) Text.Font = GameFont.Small;
                else Text.Anchor = TextAnchor.UpperLeft;
                Rect scrollrect2 = new Rect(rightColumn.x, titleRect2.yMax + margin, rightColumn.width - margin, rightColumn.height - titleRect2.height - rowHeight - margin - padding * 2 - 2f);
                var knownWeapons = PawnToShowInfoAbout.TryGetComp<CompKnowledge>()?.knownWeapons;
                if (!knownWeapons.NullOrEmpty())
                {
                    var filteredKnownWeapons = knownWeapons.Where(x => !x.menuHidden);
                    var noCommomWeapons = filteredKnownWeapons.Where(x => !x.weaponTags.NullOrEmpty());
                    var filteredWeapons = filterWeapons ? filteredKnownWeapons : noCommomWeapons;
                    var weaponsList = filteredWeapons.OrderBy(x => x.techLevel).ThenBy(x => x.IsMeleeWeapon).ThenBy(x => x.label).ToList();
                    float viewHeight2 = rowHeight * weaponsList.Count();
                    cellWidth = (int)(scrollrect2.width - scrollBarWidth);
                    Rect viewRect2 = new Rect(0f, 0f, cellWidth, viewHeight2);
                    Widgets.BeginScrollView(scrollrect2, ref scrollPosition2, viewRect2);
                    float rowWidth = fullWeapons ? nodeSize.x + margin: viewRect2.width;
                    int columnBreak = (int)weaponsList.First().techLevel;
                    int row = 0;
                    int col = 0;
                    foreach (ThingDef item in weaponsList)
                    {
                        GUI.color = ResearchTree_Assets.ColorCompleted[item.techLevel];
                        if (fullWeapons)
                        {
                            if (item == weaponsList[0]) DrawTechColorBar(rowWidth, col);
                            if ((int)item.techLevel != columnBreak)
                            {
                                row = 0;
                                col++;
                                columnBreak = (int)item.techLevel;
                                DrawTechColorBar(rowWidth, col);
                            }
                        }
                        DrawRow(item, row, rowWidth, col);
                        row++;
                    }
                    if (Event.current.type == EventType.Layout)
                    {
                        viewHeight2 = size.y;
                    }

                    Widgets.EndScrollView();
                }
                //Text.Font = GameFont.Tiny;
                float rightBaselineX = rightColumn.max.x - margin;
                float rightBaselineY = scrollrect2.max.y + padding;
                float nextPos = rightBaselineX;
                DrawToggle(nextPos, rightBaselineY, "Expand", ref fullWeapons, ContentFinder<Texture2D>.Get("UI/expand_on", true), ContentFinder<Texture2D>.Get("UI/expand_off", true), out nextPos, true);
                DrawToggle(nextPos, rightBaselineY, "ShowCommon", ref filterWeapons, ContentFinder<Texture2D>.Get("UI/commomWeapons_on", true), ContentFinder<Texture2D>.Get("UI/commomWeapons_off", true), out nextPos, true);
                DrawToggle(nextPos, rightBaselineY, "ShowCommon", ref filterWeapons, ContentFinder<Texture2D>.Get("UI/commomWeapons_on", true), ContentFinder<Texture2D>.Get("UI/commomWeapons_off", true), out nextPos, true);
            }
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            Vector2 margins = new Vector2(17f, 17f) * 2f;
            Vector2 defaultSize = CharacterCardUtility.PawnCardSize(PawnToShowInfoAbout) - new Vector2(tabSizeAdjust, 0f);
            Vector2 expandedSize = new Vector2(ResearchTree_Tree.RelevantTechLevels.Count() * (nodeSize.x + margin) - margin, defaultSize.y) ;
            size = expandTab ? expandedSize+ margins : defaultSize+ margins;
        }

        private void DrawRow(ThingDef thing, int row, float width, int col)
        {
            int shift = fullWeapons ? margin : 0;
            DrawBase(row, width, col, shift);
            float next = col;
            next = DrawIcons(next, row, width, thing, shift);
            PrintCell(thing.LabelCap, row, next, shift, width - (iconSize + margin/2), thing.description);
        }

        private void DrawBase(int row, float w, int col, int shift)
        {
            Rect rowRect = new Rect(col * w, (rowHeight * row) + shift, w - margin, rowHeight);
            if (row > -1 && Mouse.IsOver(rowRect)) GUI.DrawTexture(rowRect, TexUI.HighlightTex);
        }

        private float DrawIcons(float x, int row, float w, ThingDef thing, int shift)
        {
            float pos = x * w;
            Rect rect = new Rect(pos, (rowHeight * row) + shift, iconSize, iconSize);
            Widgets.ThingIcon(rect, thing);
            return pos + iconSize + margin / 2;
        }

        private void PrintCell(string content, int row, float x, int shift, float width = rowHeight, string tooltip = "")
        {
            Rect rect = new Rect(x, (rowHeight * row) + shift + 3, width, rowHeight - 3);
            Widgets.Label(rect, content);
            if (!string.IsNullOrEmpty(tooltip)) TooltipHandler.TipRegion(rect, tooltip);
        }

        private void DrawTechColorBar(float w, int x)
        {
            Rect rowRect = new Rect(x * w, 0, w - margin, margin - 2);
            GUI.DrawTexture(rowRect, ResearchTree_Assets.ButtonActive);
        }

        private void DrawToggle(float posX, float posY, string tooltip, ref bool toggle, Texture2D imgOn, Texture2D imgOff, out float move, bool left = false)
        {
            float startPos = left ? posX - rowHeight : posX;
            Vector2 position = new Vector2(startPos, posY);
            Rect button = new Rect(position, buttonSize);
            TooltipHandler.TipRegionByKey(button, tooltip);
            Widgets.Checkbox(position, ref toggle, 24, false, false, imgOn, imgOff);
            move = left ? posX - rowHeight : posX + rowHeight;
        }
    }
}
