using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace HumanResources
{
    public class ITab_PawnKnowledge : ITab
    {
        private const int iconSize = 29;

        private const int margin = (int)Constants.Margin;

        private const int rowHeight = 30;

        private const float scrollBarWidth = 17f;

        private const float tabSizeAdjust = 50f;

        private static Vector2 scrollPosition = Vector2.zero;

        private static Vector2 scrollPosition2 = Vector2.zero;

        private int cellWidth = 200;

        private Vector2 nodeSize = Constants.NodeSize;
        
        private static bool filterWeapons = false;

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
                    //Log.Error("Knowledge tab found no selected pawn to display.");
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

            //Left Column
            Rect leftColumn = new Rect(canvas.x, canvas.y, nodeSize.x + scrollBarWidth + margin, canvas.height);
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(leftColumn.x, leftColumn.y, leftColumn.width, Text.LineHeight);
            Widgets.Label(titleRect, "Knowledge");
            Text.Font = GameFont.Small;
            var expertise = PawnToShowInfoAbout.TryGetComp<CompKnowledge>().expertise;
            if (!expertise.EnumerableNullOrEmpty())
            {
                var expertiseList = expertise.Keys.OrderByDescending(x => x.techLevel).ThenBy(x => x.label).Select(x => new ExpertiseNode(x, PawnToShowInfoAbout)).ToList();
                Rect scrollrect = new Rect(leftColumn.x, titleRect.yMax + margin, leftColumn.width - margin, leftColumn.height - titleRect.height - margin - padding - 2f);
                float viewHeight = (nodeSize.y + margin) * expertiseList.Count();
                Rect viewRect = new Rect(0f, 0f, nodeSize.x/*canvas.width*/, viewHeight);
                Widgets.BeginScrollView(scrollrect, ref scrollPosition, viewRect);
                var pos = new Vector2(0f, 0f);//canvas.min;
                for (int i = 0; i < expertiseList.Count && pos.x + nodeSize.x < leftColumn.xMax; i++)
                {
                    var node = expertiseList[i];
                    var rect = new Rect(pos.x, pos.y, nodeSize.x, nodeSize.y);
                    node.DrawAt(pos, rect, true);
                    pos.y += nodeSize.y + margin;
                }
                if (Event.current.type == EventType.Layout)
                {
                    viewHeight = size.y;
                }
                Widgets.EndScrollView();
            }

            //Right Column
            Rect rightColumn = new Rect(leftColumn.xMax, canvas.y, canvas.width - leftColumn.width, canvas.height);
            //GUI.DrawTexture(rightColumn, TexUI.TitleBGTex);
            Text.Font = GameFont.Medium;
            Rect titleRect2 = new Rect(rightColumn.x, rightColumn.y, rightColumn.width, Text.LineHeight);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(titleRect2, "Weapons Proficiency:");
            Text.Anchor = TextAnchor.UpperLeft;
            Rect scrollrect2 = new Rect(rightColumn.x, titleRect2.yMax + margin, rightColumn.width - margin, rightColumn.height - titleRect2.height - rowHeight - margin - padding - 2f);
            var knownWeapons = PawnToShowInfoAbout.TryGetComp<CompKnowledge>().knownWeapons;
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
                int num = 0;
                foreach (ThingDef item in weaponsList)
                {
                    DrawRow(item, num, viewRect2.width);
                    num++;
                }
                if (Event.current.type == EventType.Layout)
                {
                    viewHeight2 = size.y;
                }
                Widgets.EndScrollView();
            }
            Text.Font = GameFont.Tiny;
            Rect filterRect = new Rect(rightColumn.x, scrollrect2.max.y, rightColumn.width - margin - padding, rowHeight);
            Widgets.CheckboxLabeled(filterRect, "ShowCommon".Translate(), ref filterWeapons, false);
            GUI.EndGroup();
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            size = CharacterCardUtility.PawnCardSize(PawnToShowInfoAbout) + new Vector2(17f, 17f) * 2f - new Vector2(tabSizeAdjust, 0f);
        }

        private void DrawBase(int row, float w)
        {
            Rect rowRect = new Rect(0, rowHeight * row, w, rowHeight);
            if (row > -1 && Mouse.IsOver(rowRect)) GUI.DrawTexture(rowRect, TexUI.HighlightTex);
        }

        private int DrawIcons(int x, int row, ThingDef thing)
        {
            Rect rect = new Rect(x, rowHeight * row, iconSize, iconSize);
            Widgets.ThingIcon(rect, thing);
            return iconSize + margin / 2;
        }

        private void DrawRow(ThingDef thing, int row, float width)
        {
            DrawBase(row, width);
            int next = 0;
            next = DrawIcons(next, row, thing);
            printCell(thing.LabelCap, row, next, cellWidth, thing.description);
        }

        private void printCell(string content, int row, int x, int width = rowHeight, string tooltip = "")
        {
            Rect rect = new Rect(x, rowHeight * row + 3, width, rowHeight - 3);
            Widgets.Label(rect, content);
            if (!string.IsNullOrEmpty(tooltip)) TooltipHandler.TipRegion(rect, tooltip);
        }
    }
}
