using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace HumanResources
{
    public class ITab_PawnKnowledge : ITab
    {
        private const int 
            iconSize = 29,
            rowHeight = 32;
        private static int margin = (int)ResearchTree_Constants.Margin;
        private const float 
            scrollBarWidth = 17f,
            tabSizeAdjust = 12f;
        private static bool 
            commomWeapons = false,
            fullTechs = false,
            fullWeapons = false,
            meleeWeapons = true,
            rangedWeapons = true,
            showAvailable = false,
            showAssignment = false,
            showCompact = false;
        private static Vector2 
            scrollPosition = Vector2.zero,
            scrollPosition2 = Vector2.zero,
            buttonSize = new Vector2(24f, 24f),
            baseNodeSize = ResearchTree_Constants.NodeSize;
        private static Dictionary<TechLevel, bool> TechLevelVisibility = new Dictionary<TechLevel, bool>();

        public ITab_PawnKnowledge()
        {
            labelKey = "TabKnowledge";
            if (TechLevelVisibility.EnumerableNullOrEmpty())
            {
                foreach (TechLevel level in ResearchTree_Tree.RelevantTechLevels)
                {
                    TechLevelVisibility.Add(level, true);
                }
            }
        }

        public override bool IsVisible
        {
            get
            {
                Pawn pawn = PawnToShowInfoAbout;
                return pawn != null && pawn.TryGetComp<CompKnowledge>() != null;
            }
        }

        private static bool expandTab => fullTechs | fullWeapons;
        private Vector2 nodeSize => new Vector2(baseNodeSize.x, showCompact ? baseNodeSize.y / 2 : baseNodeSize.y);
        private float extendedNodeLength => nodeSize.x + margin + buttonSize.x;
        private Pawn PawnToShowInfoAbout
        {
            get
            {
                Pawn pawn = null;
                if (SelPawn != null)
                {
                    pawn = SelPawn;
                }
                else
                {
                    Corpse corpse = SelThing as Corpse;
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
            Rect canvas = new Rect(margin, 2 * margin, size.x - margin - 1f, size.y - 2 * margin);
            string expandTT = expandTab ? "Collapse" : "Expand";
            GUI.BeginGroup(canvas);
            float firstColumnWidth = canvas.width;
            if (!fullWeapons) DrawLeftColumn(padding, ref canvas, expandTT, ref firstColumnWidth);
            if (!fullTechs) DrawRightColumn(padding, canvas, expandTT, firstColumnWidth);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

        private void DrawLeftColumn(float padding, ref Rect canvas, string expandTT, ref float firstColumnWidth)
        {
            float titlebarWidth = firstColumnWidth;
            if (!fullTechs)
            {
                firstColumnWidth = extendedNodeLength + scrollBarWidth + margin;
                titlebarWidth = firstColumnWidth - margin;
            }
            Rect leftColumn = new Rect(canvas.x, canvas.y, firstColumnWidth, canvas.height);
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(leftColumn.x, leftColumn.y, titlebarWidth, Text.LineHeight);
            Widgets.Label(titleRect, "TabKnowledgeTitle".Translate());
            DrawToggle(titleRect.max.x, leftColumn.y, expandTT, ref fullTechs, ContentFinder<Texture2D>.Get("UI/expand_left", true), ContentFinder<Texture2D>.Get("UI/expand_right", true), true);
            Text.Font = GameFont.Small;
            Rect scrollrect = new Rect(leftColumn.x, titleRect.yMax + margin, leftColumn.width - margin, leftColumn.height - titleRect.height - rowHeight - margin - padding * 2 - 2f);
            var currentList = FilteredTechs();
            if (!currentList.EnumerableNullOrEmpty())
            {
                var selectedList = fullTechs ? currentList : currentList.Where(x => TechLevelVisibility[x.techLevel]);
                if (!selectedList.EnumerableNullOrEmpty())
                {
                    var orderedList = fullTechs ? selectedList.OrderBy(x => x.techLevel) : selectedList.OrderByDescending(x => x.techLevel);
                    var expertiseList = orderedList.ThenBy(x => x.label).Select(x => new ExpertiseNode(x, PawnToShowInfoAbout)).ToList();
                    float viewHeight = (nodeSize.y + margin) * expertiseList.Count();
                    Rect viewRect = new Rect(0f, 0f, extendedNodeLength, viewHeight);
                    Widgets.BeginScrollView(scrollrect, ref scrollPosition, viewRect);
                    var pos = new Vector2(0f, 0f);
                    int columnBreak = (int)expertiseList.First().Tech.techLevel;
                    for (int i = 0; i < expertiseList.Count && pos.x + extendedNodeLength < leftColumn.xMax; i++)
                    {
                        var node = expertiseList[i];
                        if (fullTechs && (int)node.Tech.techLevel != columnBreak)
                        {
                            pos.x += extendedNodeLength + margin;
                            pos.y = 0f;
                            columnBreak = (int)node.Tech.techLevel;
                        }
                        var nodeBox = new Rect(pos, nodeSize);
                        var indicatorPos = new Vector2(nodeBox.max.x + margin, pos.y + (nodeBox.height / 2) - (buttonSize.y / 2));
                        var indicatorBox = new Rect(indicatorPos, buttonSize);
                        node.DrawAt(pos, nodeSize, nodeBox, indicatorBox, showCompact);
                        pos.y += nodeSize.y + margin;
                    }
                    if (Event.current.type == EventType.Layout)
                    {
                        viewHeight = size.y;
                    }
                    Widgets.EndScrollView();
                }
            }
            float baselineX = leftColumn.x;
            float baselineY = scrollrect.max.y + padding;
            float next = baselineX;
            next = DrawToggle(next, baselineY, "ShowCompact", ref showCompact, ContentFinder<Texture2D>.Get("UI/compact", true));
            next = DrawToggle(next, baselineY, "ShowAvailable", ref showAvailable, ContentFinder<Texture2D>.Get("UI/available", true));
            next = DrawToggle(next, baselineY, "ShowAssignment", ref showAssignment, ContentFinder<Texture2D>.Get("UI/assignment", true));
            if (!fullTechs)
            {
                foreach (TechLevel level in ResearchTree_Tree.RelevantTechLevels)
                {
                    next = DrawToggle(next, baselineY, level);
                }
            }
        }

        private void DrawRightColumn(float padding, Rect canvas, string expandTT, float firstColumnWidth)
        {
            if (fullWeapons) firstColumnWidth = 0f;
            Rect rightColumn = new Rect(canvas.x + firstColumnWidth, canvas.y, canvas.width - firstColumnWidth, canvas.height);
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(rightColumn.x, rightColumn.y, rightColumn.width, Text.LineHeight);
            if (!fullWeapons)
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.LowerLeft;
            }
            GUI.color = Color.white;
            Widgets.Label(titleRect, "TabKnowledgeWeapons".Translate());
            DrawToggle(titleRect.max.x, rightColumn.y, expandTT, ref fullWeapons, ContentFinder<Texture2D>.Get("UI/expand_left", true), ContentFinder<Texture2D>.Get("UI/expand_right", true), true);
            if (fullWeapons) Text.Font = GameFont.Small;
            else Text.Anchor = TextAnchor.UpperLeft;
            Rect scrollrect = new Rect(rightColumn.x, titleRect.yMax + margin, rightColumn.width - margin, rightColumn.height - titleRect.height - rowHeight - margin - padding * 2 - 2f);
            var knownWeapons = PawnToShowInfoAbout.TryGetComp<CompKnowledge>()?.knownWeapons;
            if (!knownWeapons.NullOrEmpty())
            {
                var filteredWeapons = knownWeapons.Where(weaponsFilter);
                if (!filteredWeapons.EnumerableNullOrEmpty())
                {
                    var weaponsList = filteredWeapons.OrderBy(x => x.techLevel).ThenBy(x => x.IsMeleeWeapon).ThenBy(x => x.label).ToList();
                    float viewHeight = rowHeight * weaponsList.Count();
                    float viewWidth = (scrollrect.width - scrollBarWidth);
                    Rect viewRect = new Rect(0f, 0f, viewWidth, viewHeight);
                    Widgets.BeginScrollView(scrollrect, ref scrollPosition2, viewRect);
                    float rowWidth = fullWeapons ? nodeSize.x + margin : viewRect.width;
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
                        viewHeight = size.y;
                    }
                    Widgets.EndScrollView();
                }
            }
            float rightBaselineX = rightColumn.max.x - margin;
            float rightBaselineY = scrollrect.max.y + padding;
            float next = rightBaselineX;
            next = DrawToggle(next, rightBaselineY, "ShowCommon", ref commomWeapons, ContentFinder<Texture2D>.Get("UI/commomWeapons", true), null, true);
            next = DrawToggle(next, rightBaselineY, "ShowRanged", ref rangedWeapons, ContentFinder<Texture2D>.Get("UI/ranged", true), null, true);
            next = DrawToggle(next, rightBaselineY, "ShowMelee", ref meleeWeapons, ContentFinder<Texture2D>.Get("UI/melee", true), null, true);
        }


        private IEnumerable<ResearchProjectDef> FilteredTechs()
        {
            var techComp = PawnToShowInfoAbout.TryGetComp<CompKnowledge>();
            if (techComp != null)
            {
                var expertise = techComp.expertise;
                if (showAvailable) return DefDatabase<ResearchProjectDef>.AllDefsListForReading.Except(expertise.Keys).Where(x => x.IsFinished);
                var homework = techComp.homework;
                if (showAssignment) return homework != null ? homework.AsEnumerable() : null;
                else return expertise.Keys;
            }
            return null;
        }

        private static Func<ThingDef, bool> weaponsFilter = (x) =>
        {
            bool valid = !x.menuHidden;
            bool commom = commomWeapons ? true : !x.NotReallyAWeapon();
            bool melee = meleeWeapons ? true : !x.IsMeleeWeapon;
            bool ranged = rangedWeapons ? true : !x.IsRangedWeapon;
            return valid & commom & melee & ranged;
        };

        protected override void UpdateSize()
        {
            base.UpdateSize();
            Vector2 margins = new Vector2(17f, 17f) * 2f;
            Vector2 defaultSize = CharacterCardUtility.PawnCardSize(PawnToShowInfoAbout) - new Vector2(tabSizeAdjust, 0f);
            Vector2 expandedSize = new Vector2(ResearchTree_Tree.RelevantTechLevels.Count() * (/*nodeSize.x*/extendedNodeLength + margin) - margin, defaultSize.y) ;
            size = expandTab ? expandedSize + margins : defaultSize + margins;
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

        private void DrawRow(ThingDef thing, int row, float width, int col)
        {
            int shift = fullWeapons ? margin : 0;
            DrawBase(row, width, col, shift);
            float next = col;
            next = DrawIcons(next, row, width, thing, shift);
            string tooltip = Prefs.DevMode ? thing.weaponTags?.ToStringSafeEnumerable() : thing.description;
            PrintCell(thing.LabelCap, row, next, shift, width - (iconSize + margin/2), tooltip);
        }

        private void DrawTechColorBar(float w, int x)
        {
            Rect rowRect = new Rect(x * w, 0, w - margin, margin - 2);
            GUI.DrawTexture(rowRect, ResearchTree_Assets.ButtonActive);
        }

        private void PrintCell(string content, int row, float x, int shift, float width = rowHeight, string tooltip = "")
        {
            Rect rect = new Rect(x, (rowHeight * row) + shift + 3, width, rowHeight - 3);
            Widgets.Label(rect, content);
            if (!string.IsNullOrEmpty(tooltip)) TooltipHandler.TipRegion(rect, tooltip);
        }

        private float DrawToggle(float posX, float posY, string tooltip, ref bool toggle, Texture2D textOn, Texture2D texOff = null, bool left = false, Color? c = null)
        {
            float startPos = left ? posX - rowHeight : posX;
            Vector2 position = new Vector2(startPos, posY);
            Rect box = new Rect(position, buttonSize);
            var curFont = Text.Font;
            Text.Font = GameFont.Tiny;
            TooltipHandler.TipRegionByKey(box, tooltip);
            Text.Font = curFont;
            Color baseColor = c ?? Color.white;
            Color color = toggle? baseColor : Color.grey;
            Texture2D swap = toggle ? textOn : texOff;
            Texture2D face = texOff != null ? swap : textOn;
            if (Widgets.ButtonImage(box, face, color)) toggle = !toggle;
            return left ? posX - rowHeight : posX + rowHeight;
        }

        private float DrawToggle(float posX, float posY, TechLevel techLevel)
        {
            bool toggle = TechLevelVisibility[techLevel];
            Vector2 position = new Vector2(posX, posY);
            Rect box = new Rect(position, buttonSize);
            var curFont = Text.Font;
            Text.Font = GameFont.Tiny;
            TooltipHandler.TipRegionByKey(box, techLevel.ToStringHuman());
            Text.Font = curFont;
            Color color = toggle ? Color.Lerp(ResearchTree_Assets.ColorCompleted[techLevel], Widgets.WindowBGFillColor, 0.2f) : ResearchTree_Assets.ColorAvailable[techLevel];
            if (Widgets.ButtonImage(box, ContentFinder<Texture2D>.Get("UI/dot", true), color, ResearchTree_Assets.ColorCompleted[techLevel])) TechLevelVisibility[techLevel] = !TechLevelVisibility[techLevel];
            return posX + rowHeight;
        }
    }
}
