using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using System;

namespace HumanResources
{
    public class ITab_PawnKnowledge : ITab
    {
        private const int iconSize = 29;
        private const int margin = (int)Constants.Margin;
        private const int rowHeight = 32;
        private const float scrollBarWidth = 17f;
        private const float tabSizeAdjust = 12f;
        private static bool commomWeapons = false;
        private static bool fullTechs = false;
        private static bool fullWeapons = false;
        private static bool meleeWeapons = true;
        private static bool rangedWeapons = true;
        private static Vector2 scrollPosition = Vector2.zero;
        private static Vector2 scrollPosition2 = Vector2.zero;
        private static bool showAvailable = false;
        private Vector2 buttonSize = new Vector2(24f, 24f);
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

        private static bool expandTab => fullTechs | fullWeapons;
        private Vector2 nodeSize => Constants.NodeSize;
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
            Rect canvas = new Rect(margin, 2 * margin, size.x - margin - 1f, size.y - 2 * margin);
            string expandTT = expandTab ? "Collapse" : "Expand";
            GUI.BeginGroup(canvas);

            float firstColumnWidth = canvas.width;

            //Left Column
            if (!fullWeapons)
            {
                float titlebarWidth = firstColumnWidth;
                if (!fullTechs)
                {
                    firstColumnWidth = nodeSize.x + scrollBarWidth + margin;
                    titlebarWidth = firstColumnWidth - margin;
                }
                Rect leftColumn = new Rect(canvas.x, canvas.y, firstColumnWidth, canvas.height);
                Text.Font = GameFont.Medium;
                Rect titleRect = new Rect(leftColumn.x, leftColumn.y, titlebarWidth, Text.LineHeight);
                Widgets.Label(titleRect, "TabKnowledgeTitle".Translate());
                DrawToggle(titleRect.max.x, leftColumn.y, expandTT, ref fullTechs, ContentFinder<Texture2D>.Get("UI/expand_left", true), ContentFinder<Texture2D>.Get("UI/expand_right", true), true);
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
                        float next = baselineX;
                        next = DrawToggle(next, baselineY, "ShowAvailable", ref showAvailable, ContentFinder<Texture2D>.Get("UI/available", true));
                        next = DrawToggle(next, baselineY, "ShowCompact", ref Constants.showCompact, ContentFinder<Texture2D>.Get("UI/compact", true));
                    }
                }
            }

            //Right Column
            if (!fullTechs)
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
                //Text.Font = GameFont.Tiny;
                float rightBaselineX = rightColumn.max.x - margin;
                float rightBaselineY = scrollrect.max.y + padding;
                float next = rightBaselineX;
                next = DrawToggle(next, rightBaselineY, "ShowCommon", ref commomWeapons, ContentFinder<Texture2D>.Get("UI/commomWeapons", true), null, true);
                next = DrawToggle(next, rightBaselineY, "ShowMelee", ref meleeWeapons, ContentFinder<Texture2D>.Get("UI/melee", true), null, true);
                next = DrawToggle(next, rightBaselineY, "ShowRanged", ref rangedWeapons, ContentFinder<Texture2D>.Get("UI/ranged", true), null, true);
            }
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

        private static Func<ThingDef, bool> weaponsFilter = (x) =>
         {
             bool valid = !x.menuHidden;
             bool commom = commomWeapons ? true : !x.weaponTags.NullOrEmpty();
             bool melee = meleeWeapons ? true : !x.IsMeleeWeapon;
             bool ranged = rangedWeapons ? true : !x.IsRangedWeapon;
             return valid & commom & melee & ranged;
         };

        protected override void UpdateSize()
        {
            base.UpdateSize();
            Vector2 margins = new Vector2(17f, 17f) * 2f;
            Vector2 defaultSize = CharacterCardUtility.PawnCardSize(PawnToShowInfoAbout) - new Vector2(tabSizeAdjust, 0f);
            Vector2 expandedSize = new Vector2(ResearchTree_Tree.RelevantTechLevels.Count() * (nodeSize.x + margin) - margin, defaultSize.y) ;
            size = expandTab ? expandedSize+ margins : defaultSize+ margins;
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
            PrintCell(thing.LabelCap, row, next, shift, width - (iconSize + margin/2), thing.description);
        }
        private void DrawTechColorBar(float w, int x)
        {
            Rect rowRect = new Rect(x * w, 0, w - margin, margin - 2);
            GUI.DrawTexture(rowRect, ResearchTree_Assets.ButtonActive);
        }

        private float DrawToggle(float posX, float posY, string tooltip, ref bool toggle, Texture2D textOn, Texture2D texOff = null, bool left = false)
        {
            float startPos = left ? posX - rowHeight : posX;
            Vector2 position = new Vector2(startPos, posY);
            Rect box = new Rect(position, buttonSize);
            var curFont = Text.Font;
            Text.Font = GameFont.Tiny;
            TooltipHandler.TipRegionByKey(box, tooltip);
            Text.Font = curFont;
            Color color = toggle? Color.white : Color.grey;
            Texture2D swap = toggle ? textOn : texOff;
            Texture2D face = texOff != null ? swap : textOn;
            if (Widgets.ButtonImage(box, face, color)) toggle = !toggle;
            return left ? posX - rowHeight : posX + rowHeight;
        }

        private void PrintCell(string content, int row, float x, int shift, float width = rowHeight, string tooltip = "")
        {
            Rect rect = new Rect(x, (rowHeight * row) + shift + 3, width, rowHeight - 3);
            Widgets.Label(rect, content);
            if (!string.IsNullOrEmpty(tooltip)) TooltipHandler.TipRegion(rect, tooltip);
        }
    }
}
