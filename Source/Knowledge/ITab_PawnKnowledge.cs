using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace HumanResources
{
    public class ITab_PawnKnowledge : ITab
    {
        private const int
            iconSize = 29,
            rowHeight = 32;
        private const float
            scrollBarWidth = 17f,
            tabSizeAdjust = 12f;
        private static bool
            commonWeapons = false,
            fullTechs = false,
            fullWeapons = false,
            groupTechs = false,
            meleeWeapons = true,
            rangedWeapons = true,
            showAvailable = false,
            showAssignment = false,
            showCompact = false;
        private static int margin = (int)ResearchTree_Constants.Margin;
        private static Vector2
            scrollPosition = Vector2.zero,
            scrollPosition2 = Vector2.zero,
            buttonSize = new Vector2(24f, 24f),
            baseNodeSize = ResearchTree_Constants.NodeSize;
        private static Dictionary<TechLevel, bool> TechLevelVisibility = new Dictionary<TechLevel, bool>();
        private static Func<ThingDef, bool> weaponsFilter = (x) =>
        {
            bool commom = commonWeapons ? true : !x.NotThatHard();
            bool melee = meleeWeapons ? true : !x.IsMeleeWeapon;
            bool ranged = rangedWeapons ? true : !x.IsRangedWeapon;
            return commom & melee & ranged;
        };
        private static FieldInfo SkillBarFillTexInfo = AccessTools.Field(typeof(SkillUI), "SkillBarFillTex");
        private Vector2 viewSize;
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
        private int columns => expandTab ? (groupTechs ? TechTracker.Skills.Count() : maxColumns) : 1;
        private float columnWidth => extendedNodeLength + margin;
        private float extendedNodeLength => nodeSize.x + margin + buttonSize.x;
        private int maxColumns => ResearchTree_Tree.RelevantTechLevels.Count();
        private Vector2 nodeSize => new Vector2(baseNodeSize.x, showCompact ? baseNodeSize.y / 2 : baseNodeSize.y);
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
        private Texture2D SkillBarFillTex => (Texture2D)SkillBarFillTexInfo.GetValue(this);
        private TechLevel IndividualTechLevel => PawnToShowInfoAbout.TryGetComp<CompKnowledge>()?.techLevel ?? Faction.OfPlayer.def.techLevel;


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
            ResearchTree_Patches.Context = PawnToShowInfoAbout.TryGetComp<CompKnowledge>();
        }

        protected override void CloseTab()
        {
            ResearchTree_Patches.Context = null;
            base.CloseTab();
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            Vector2 margins = new Vector2(17f, 17f) * 2f;
            Vector2 defaultSize = CharacterCardUtility.PawnCardSize(PawnToShowInfoAbout) - new Vector2(tabSizeAdjust, 0f);
            Vector2 expandedSize = new Vector2(maxColumns * columnWidth, defaultSize.y);
            size = expandTab ? expandedSize + margins : defaultSize + margins;
        }

        private void breakColumn(ref Vector2 pos)
        {
            pos.x += columnWidth;
            pos.y = 0f;
        }

        private void DrawBase(int row, float w, int col, int shift)
        {
            Rect rowRect = new Rect(col * w, (rowHeight * row) + shift, w - margin, rowHeight);
            if (row > -1 && Mouse.IsOver(rowRect)) GUI.DrawTexture(rowRect, TexUI.HighlightTex);
        }

        private Vector2 DrawGroupedNodes(IEnumerable<ExpertiseNode> expertiseList, float max, ref Vector2 pos)
        {
            Vector2 maxView = pos;
            var techsList = expertiseList.Select(x => x.Tech);
            var skillSet = TechTracker.FindSkills(x => x.Techs.Any(y => techsList.Contains(y))).OrderByDescending(x => PawnToShowInfoAbout.skills.GetSkill(x).Level);
            foreach (var skill in skillSet)
            {
                var filtered = expertiseList.Where(x => skill.Techs.Contains(x.Tech)).ToList();
                if (filtered.NullOrEmpty()) continue;
                var skillRatio = (float)PawnToShowInfoAbout.skills.GetSkill(skill).Level / SkillRecord.MaxLevel;
                Rect groupTitle = DrawGroupTitle(pos, skill, skillRatio, filtered);
                pos.y += groupTitle.height + margin;
                DrawResearchNodes(filtered, max, ref pos, true);
                if (!fullTechs) pos.y += margin;
                if (maxView.y < pos.y) maxView.y = pos.y;
                if (fullTechs) breakColumn(ref pos);
                if (pos.x > maxView.x) maxView.x = fullTechs ? pos.x : pos.x + columnWidth;
            }
            return maxView;
        }

        private Rect DrawGroupTitle(Vector2 pos, SkillDef skill, float skillRatio, IEnumerable<ExpertiseNode> expertiseList)
        {
            Rect groupBar = new Rect(pos.x, pos.y, nodeSize.x, Text.LineHeight);
            GUI.color = Color.grey;
            GUI.DrawTexture(groupBar, TexUI.TitleBGTex);
            GUI.color = Color.white;
            if (Mouse.IsOver(groupBar)) GUI.DrawTexture(groupBar, TexUI.HighlightTex);
            Widgets.FillableBar(groupBar, skillRatio, SkillBarFillTex, null, false);
            TooltipHandler.TipRegionByKey(groupBar, "ClickToGroupAssign");
            if (Widgets.ButtonInvisible(groupBar))
            {
                foreach (var node in expertiseList) node.UpdateAssignment();
            }
            Rect groupTitle = new Rect(groupBar.x + margin, groupBar.y, groupBar.width - margin, groupBar.height);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(groupTitle, skill.LabelCap);
            return groupTitle;
        }

        private float DrawIcons(float x, int row, float w, ThingDef thing, int shift)
        {
            float pos = x * w;
            Rect rect = new Rect(pos, (rowHeight * row) + shift, iconSize, iconSize);
            Widgets.ThingIcon(rect, thing);
            return pos + iconSize + margin / 2;
        }

        private void DrawLeftColumn(float padding, ref Rect canvas, string expandTT, ref float firstColumnWidth)
        {
            float titlebarWidth = firstColumnWidth;
            if (!fullTechs)
            {
                firstColumnWidth = columnWidth + scrollBarWidth;
                titlebarWidth = firstColumnWidth - margin;
            }
            Rect leftColumn = new Rect(canvas.x, canvas.y, firstColumnWidth, canvas.height);
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(leftColumn.x, leftColumn.y, titlebarWidth, Text.LineHeight);
            Widgets.Label(titleRect, "TabKnowledgeTitle".Translate());
            float button = DrawToggle(titleRect.max.x, leftColumn.y, expandTT, ref fullTechs, ContentFinder<Texture2D>.Get("UI/expand_left", true), ContentFinder<Texture2D>.Get("UI/expand_right", true), true);
            if (showAssignment && DrawCommand(button, leftColumn.y, "ClearAssignments".Translate(), ContentFinder<Texture2D>.Get("UI/clearAssignments", true), true))
            {
                PawnToShowInfoAbout.TryGetComp<CompKnowledge>()?.homework.Clear();
            }
            Text.Font = GameFont.Small;
            Vector2 scrollPos = new Vector2(leftColumn.x, titleRect.yMax + margin);
            int scrollColumns = expandTab ? maxColumns : 1;
            float scrollWidth = scrollColumns * columnWidth + scrollBarWidth;// leftColumn.width - margin;
            float scrollHeight = leftColumn.height - titleRect.height - rowHeight - margin - padding * 2 - 2f;
            Rect scrollrect = new Rect(leftColumn.x, titleRect.yMax + margin, scrollWidth, scrollHeight);
            var currentlist = FilteredTechs()?.Where(x => TechLevelVisibility[x.techLevel]);
            if (!currentlist.EnumerableNullOrEmpty())
            {
                var orderedList = fullTechs ? currentlist.OrderBy(x => x.techLevel) : currentlist.OrderByDescending(x => x.techLevel);
                var expertiseList = orderedList.ThenBy(x => x.label).Select(x => new ExpertiseNode(x, PawnToShowInfoAbout));
                Rect viewRect = new Rect(Vector2.zero, viewSize);
                Widgets.BeginScrollView(scrollrect, ref scrollPosition, viewRect, true);
                Vector2 pos = new Vector2(0f, 0f);
                if (groupTechs) viewSize = DrawGroupedNodes(expertiseList, leftColumn.xMax, ref pos);
                else viewSize = DrawResearchNodes(expertiseList, leftColumn.xMax, ref pos);
                Widgets.EndScrollView();
            }
            float baselineX = leftColumn.x;
            float baselineY = scrollrect.max.y + padding;
            float next = baselineX;
            next = DrawToggle(next, baselineY, "ShowCompact", ref showCompact, ContentFinder<Texture2D>.Get("UI/compact", true));
            next = DrawToggle(next, baselineY, "ShowAvailable", ref showAvailable, ContentFinder<Texture2D>.Get("UI/available", true));
            next = DrawToggle(next, baselineY, "ShowAssignment", ref showAssignment, ContentFinder<Texture2D>.Get("UI/assignment", true));
            next = DrawToggle(next, baselineY, "GroupBySkills", ref groupTechs, ContentFinder<Texture2D>.Get("UI/skills", true));
            foreach (TechLevel level in ResearchTree_Tree.RelevantTechLevels)
            {
                next = DrawToggle(next, baselineY, level);
            }
        }

        private Vector2 DrawResearchNodes(IEnumerable<ExpertiseNode> nodeList, float max, ref Vector2 pos, bool groupColumn = false)
        {
            Vector2 maxView = pos;
            int TechLevelBreak = groupColumn ? 0 : (int)nodeList.First().Tech.techLevel;
            foreach (var node in nodeList)
            {
                if (!groupColumn && fullTechs && (int)node.Tech.techLevel != TechLevelBreak)
                {
                    breakColumn(ref pos);
                    TechLevelBreak = (int)node.Tech.techLevel;
                }
                var nodeBox = new Rect(pos, nodeSize);
                var indicatorPos = new Vector2(nodeBox.max.x + margin, pos.y + (nodeBox.height / 2) - (buttonSize.y / 2));
                var indicatorBox = new Rect(indicatorPos, buttonSize);
                node.DrawAt(pos, nodeSize, nodeBox, indicatorBox, IndividualTechLevel, showCompact);
                pos.y += nodeSize.y + margin;
                if (pos.y > maxView.y) maxView.y = pos.y;
                if (indicatorBox.xMax > maxView.x) maxView.x = indicatorBox.xMax;
            }
            return maxView;
        }

        private void DrawRightColumn(float padding, Rect canvas, string expandTT, float firstColumnWidth)
        {
            if (fullWeapons) firstColumnWidth = 0f;
            Rect rightColumn = new Rect(canvas.x + firstColumnWidth + margin, canvas.y, canvas.width - firstColumnWidth - margin, canvas.height);
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
            var knownWeapons = PawnToShowInfoAbout.TryGetComp<CompKnowledge>()?.knownWeapons.Where(weaponsFilter);
            if (!knownWeapons.EnumerableNullOrEmpty())
            {
                var weaponsList = knownWeapons.OrderBy(x => x.techLevel).ThenBy(x => x.IsMeleeWeapon).ThenBy(x => x.label).ToList();
                float viewHeight = rowHeight * weaponsList.Count();
                float viewWidth = (scrollrect.width - scrollBarWidth);
                Rect viewRect = new Rect(0f, 0f, viewWidth, viewHeight);
                Widgets.BeginScrollView(scrollrect, ref scrollPosition2, viewRect);
                float rowWidth = fullWeapons ? nodeSize.x + margin : viewRect.width;
                DrawWeaponsList(weaponsList, rowWidth);
                if (Event.current.type == EventType.Layout)
                {
                    viewHeight = size.y;
                }
                Widgets.EndScrollView();
            }
            float rightBaselineX = rightColumn.max.x - margin;
            float rightBaselineY = scrollrect.max.y + padding;
            float next = rightBaselineX;
            next = DrawToggle(next, rightBaselineY, "ShowCommon", ref commonWeapons, ContentFinder<Texture2D>.Get("UI/commomWeapons", true), null, true);
            next = DrawToggle(next, rightBaselineY, "ShowRanged", ref rangedWeapons, ContentFinder<Texture2D>.Get("UI/ranged", true), null, true);
            next = DrawToggle(next, rightBaselineY, "ShowMelee", ref meleeWeapons, ContentFinder<Texture2D>.Get("UI/melee", true), null, true);
        }

        private void DrawRow(ThingDef thing, int row, float width, int col)
        {
            int shift = fullWeapons ? margin : 0;
            DrawBase(row, width, col, shift);
            float next = col;
            next = DrawIcons(next, row, width, thing, shift);
            string tooltip = Prefs.DevMode ? thing.weaponTags?.ToStringSafeEnumerable() : thing.description;
            PrintCell(thing.LabelCap, row, next, shift, width - (iconSize + margin / 2), tooltip);
        }

        private void DrawTechColorBar(float w, int x)
        {
            Rect rowRect = new Rect(x * w, 0, w - margin, margin - 2);
            GUI.DrawTexture(rowRect, ResearchTree_Assets.ButtonActive);
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
            Color color = toggle ? baseColor : Color.grey;
            Texture2D swap = toggle ? textOn : texOff;
            Texture2D face = texOff != null ? swap : textOn;
            if (Widgets.ButtonImage(box, face, color)) toggle = !toggle;
            return left ? posX - rowHeight : posX + rowHeight;
        }

        private float DrawToggle(float posX, float posY, TechLevel techLevel)
        {
            bool toggle = TechLevelVisibility[techLevel];
            var bump = buttonSize.y / 5;
            Vector2 position = new Vector2(posX, posY - bump);
            Rect box = new Rect(position, buttonSize);
            Color color = toggle ? Color.Lerp(ResearchTree_Assets.ColorCompleted[techLevel], Widgets.WindowBGFillColor, 0.2f) : ResearchTree_Assets.ColorAvailable[techLevel];
            if (Widgets.ButtonImage(box, ContentFinder<Texture2D>.Get("UI/dot", true), color, ResearchTree_Assets.ColorCompleted[techLevel])) TechLevelVisibility[techLevel] = !TechLevelVisibility[techLevel];
            string tip = techLevel.ToStringHuman().CapitalizeFirst();
            if (techLevel == IndividualTechLevel)
            {
                tip += $"\n(" + "CurrentTechLevelFor".Translate(PawnToShowInfoAbout) + ")";
                DrawTechLevelIndicator(posX, box.yMax);
            }
            var curFont = Text.Font;
            Text.Font = GameFont.Tiny;
            TooltipHandler.TipRegion(box, tip);
            Text.Font = curFont;
            return posX + rowHeight;
        }

        private void DrawTechLevelIndicator(float posX, float posY)
        {
            var size = buttonSize / 3;
            posX += size.x;
            Vector2 position = new Vector2(posX, posY);
            Rect box = new Rect(position, size);
            GUI.color = Color.white;
            GUI.DrawTexture(box, ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarker", true));
        }

        private bool DrawCommand(float posX, float posY, string tooltip, Texture2D face, bool left = false, Color? c = null)
        {
            float startPos = left ? posX - rowHeight : posX;
            Vector2 position = new Vector2(startPos, posY);
            Rect box = new Rect(position, buttonSize);
            var curFont = Text.Font;
            Text.Font = GameFont.Tiny;
            TooltipHandler.TipRegionByKey(box, tooltip);
            Text.Font = curFont;
            Color color = c ?? Color.grey;
            return Widgets.ButtonImage(box, face, color);
        }

        private void DrawWeaponsList(List<ThingDef> weaponsList, float rowWidth)
        {
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
        }

        private IEnumerable<ResearchProjectDef> FilteredTechs()
        {
            var techComp = PawnToShowInfoAbout.TryGetComp<CompKnowledge>();
            if (techComp == null) return null;
            var expertise = techComp.expertise;
            if (showAvailable) return DefDatabase<ResearchProjectDef>.AllDefsListForReading.Except(expertise.Keys).Where(x => x.IsFinished);
            var homework = techComp.homework;
            if (showAssignment) return homework != null ? homework.AsEnumerable() : null;
            else return expertise.Keys;
        }

        private void PrintCell(string content, int row, float x, int shift, float width = rowHeight, string tooltip = "")
        {
            Rect rect = new Rect(x, (rowHeight * row) + shift + 3, width, rowHeight - 3);
            Widgets.Label(rect, content);
            if (!string.IsNullOrEmpty(tooltip)) TooltipHandler.TipRegion(rect, tooltip);
        }
    }
}
