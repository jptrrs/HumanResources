// ResearchNode.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

namespace HumanResources
{
    //Changed in RW1.3
    public class ExpertiseNode
    {
        //pulled from Node
        protected const float Offset = 2f;
        protected bool _largeLabel;
        protected Rect
            _queueRect,
            _rect,
            _labelRect,
            _costLabelRect,
            _costIconRect,
            _iconsRect,
            _lockRect;

        protected bool _rectsSet;
        protected Vector2
            _size = Vector2.zero,
            _pos = Vector2.zero,
            _topLeft = Vector2.zero,
            _right = Vector2.zero,
            _left = Vector2.zero;
        public Rect CostIconRect
        {
            get
            {
                if (!_rectsSet)
                    SetRects();

                return _costIconRect;
            }
        }
        public Rect CostLabelRect
        {
            get
            {
                if (!_rectsSet)
                    SetRects();

                return _costLabelRect;
            }
        }
        public Rect IconsRect
        {
            get
            {
                if (!_rectsSet)
                    SetRects();

                return _iconsRect;
            }
        }
        public Rect LabelRect
        {
            get
            {
                if (!_rectsSet)
                    SetRects();

                return _labelRect;
            }
        }
        public Rect Rect
        {
            get
            {
                if (!_rectsSet)
                    SetRects();

                return _rect;
            }
        }
        public virtual int X
        {
            get => (int)_pos.x;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (Math.Abs(_pos.x - value) < ResearchTree_Constants.Epsilon)
                    return;
                _pos.x = value;
            }
        }
        public virtual float Yf
        {
            get => _pos.y;
            set
            {
                if (Math.Abs(_pos.y - value) < ResearchTree_Constants.Epsilon)
                    return;
                _pos.y = value;
            }
        }
        public virtual string Label { get; }
        public virtual bool Highlighted { get; set; }
        public void SetRects()
        {
            // origin
            _topLeft = new Vector2(
                (X - 1) * (ResearchTree_Constants.NodeSize.x + ResearchTree_Constants.NodeMargins.x),
                (Yf - 1) * (ResearchTree_Constants.NodeSize.y + ResearchTree_Constants.NodeMargins.y));

            SetRects(_topLeft, _size);
        }
        public void SetRects(Vector2 topLeft, Vector2 size)
        {
            _size = size;

            // main rect
            _rect = new Rect(topLeft, size);

            // queue rect
            _queueRect = new Rect(_rect.xMax - ResearchTree_Constants.QueueLabelSize / 2f,
                                   _rect.yMin + (_rect.height - ResearchTree_Constants.QueueLabelSize) / 2f, ResearchTree_Constants.QueueLabelSize,
                                   ResearchTree_Constants.QueueLabelSize);

            // label rect
            _labelRect = new Rect(_rect.xMin + 6f,
                                   _rect.yMin + 3f,
                                   _rect.width * 2f / 3f - 6f,
                                   _rect.height * .5f - 3f);

            // research cost rect
            _costLabelRect = new Rect(_rect.xMin + _rect.width * 2f / 3f,
                                       _rect.yMin + 3f,
                                       _rect.width * 1f / 3f - 16f - 3f,
                                       _rect.height * .5f - 3f);

            // research icon rect
            _costIconRect = new Rect(_costLabelRect.xMax,
                                      _rect.yMin + (_costLabelRect.height - 16f) / 2,
                                      16f,
                                      16f);

            // icon container rect
            _iconsRect = new Rect(_rect.xMin,
                                   _rect.yMin + _rect.height * .5f,
                                   _rect.width,
                                   _rect.height * .5f);

            // lock icon rect
            _lockRect = new Rect(0f, 0f, 32f, 32f);
            _lockRect = _lockRect.CenteredOnXIn(_rect);
            _lockRect = _lockRect.CenteredOnYIn(_rect);

            // see if the label is too big
            _largeLabel = Text.CalcHeight(Label, _labelRect.width) > _labelRect.height;

            // done
            _rectsSet = true;
        }
        public virtual bool IsVisible(Rect visibleRect)
        {
            return !(
                Rect.xMin > visibleRect.xMax ||
                Rect.xMax < visibleRect.xMin ||
                Rect.yMin > visibleRect.yMax ||
                Rect.yMax < visibleRect.yMin);
        }
        //

        public ResearchProjectDef Tech;

        private Pawn Pawn;

        private bool
            knownSucessorSet = false,
            KnownSucessor;

        private static Type pseudoParentType = ResearchTree_Patches.ResearchNodeType();

        private MethodInfo GetResearchTooltipStringInfo = AccessTools.Method(pseudoParentType, "GetResearchTooltipString");

        public virtual Texture2D indicator => Completed ? ContentFinder<Texture2D>.Get("UI/write", true) : ContentFinder<Texture2D>.Get("UI/read");

        public ExpertiseNode(ResearchProjectDef research, Pawn pawn)
        {
            Pawn = pawn;
            Tech = research;
            _pos = new Vector2(0, research.researchViewY + 1);
        }

        public Color Color
        {
            get
            {
                if (Highlighted)
                    return GenUI.MouseoverColor;
                if (Completed)
                    return ResearchTree_Assets.ColorCompleted[Tech.techLevel];
                return ResearchTree_Assets.ColorAvailable[Tech.techLevel];
            }
        }

        public bool Completed => Tech.IsFinished;

        public Color EdgeColor
        {
            get
            {
                if (Highlighted)
                    return GenUI.MouseoverColor;
                if (Completed)
                    return ResearchTree_Assets.ColorCompleted[Tech.techLevel];
                return ResearchTree_Assets.ColorAvailable[Tech.techLevel];
            }
        }

        private CompKnowledge techComp => Pawn.TryGetComp<CompKnowledge>();

        public bool BuildingPresent()
        {
            return ResearchTree_Patches.BuildingPresentProxy(Tech);
        }

        public bool TechprintAvailable()
        {
            return ResearchTree_Patches.TechprintAvailable(Tech);
        }

        public void Draw(Rect visibleRect, TechLevel pawnTechLevel, bool forceDetailedMode = false)
        {
            if (!IsVisible(visibleRect))
            {
                Highlighted = false;
                return;
            }

            var detailedMode = forceDetailedMode;
            var mouseOver = Mouse.IsOver(Rect);
            if (Event.current.type == EventType.Repaint)
            {
                // for later:
                float achieved = 0;

                // researches that are completed or could be started immediately, and that have the required building(s) available
                GUI.color = mouseOver ? GenUI.MouseoverColor : Color;
                if (mouseOver || Highlighted) GUI.DrawTexture(Rect, ResearchTree_Assets.ButtonActive);
                else GUI.DrawTexture(Rect, ResearchTree_Assets.Button);

                // grey out center to create a progress bar effect, completely greying out research not started.
                var progressBarRect = Rect.ContractedBy(3f);
                GUI.color = Widgets.WindowBGFillColor;
                if (techComp.expertise.ContainsKey(Tech))
                {
                    achieved = techComp.expertise[Tech];
                    progressBarRect.xMin += achieved * progressBarRect.width;
                }
                GUI.DrawTexture(progressBarRect, BaseContent.WhiteTex);
                Highlighted = false;

                // draw the research label
                GUI.color = Color.white;

                if (detailedMode)
                {
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.WordWrap = false;
                    Text.Font = _largeLabel ? GameFont.Tiny : GameFont.Small;
                    Widgets.Label(LabelRect, Tech.LabelCap);
                }
                else
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.WordWrap = false;
                    Text.Font = GameFont.Small;//GameFont.Medium;
                    Widgets.Label(Rect, Tech.LabelCap);
                }

                // draw research cost and icon
                if (detailedMode)
                {
                    DrawStats(pawnTechLevel, achieved);
                }

                Text.WordWrap = true;

                // attach description and further info to a tooltip
                string root = HarmonyPatches.ResearchPal ? "ResearchPal" : "Fluffy.ResearchTree";
                TooltipHandler.TipRegion(Rect, GetResearchTooltipString, Tech.GetHashCode());
                if (!BuildingPresent())
                {
                    string languageKey = root + ".MissingFacilities";
                    TooltipHandler.TipRegion(Rect, languageKey.Translate(string.Join(", ", MissingFacilities().Select(td => td.LabelCap).ToArray())));
                }
                else if (!TechprintAvailable())
                {
                    string languageKey = root + ".MissingTechprints";
                    TooltipHandler.TipRegion(Rect, languageKey.Translate(Tech.TechprintsApplied, Tech.techprintCount));
                }

                // draw unlock icons
                if (detailedMode)
                {
                    var unlocks = ResearchTree_Patches.GetUnlockDefsAndDescs(Tech);
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
                            // stop the loop if we're about to overflow and have 2 or more unlocks yet to print.
                            iconRect.x = IconsRect.x + 4f;
                            GUI.DrawTexture(iconRect, ResearchTree_Assets.MoreIcon, ScaleMode.ScaleToFit);
                            var tip = string.Join("\n", unlocks.GetRange(i, unlocks.Count - i).Select(p => p.Second).ToArray());
                            TooltipHandler.TipRegion(iconRect, tip);
                            // new TipSignal( tip, Settings.TipID, TooltipPriority.Pawn ) );
                            break;
                        }

                        // draw icon
                        unlocks[i].First.DrawColouredIcon(iconRect);

                        // tooltip
                        TooltipHandler.TipRegion(iconRect, unlocks[i].Second);
                    }
                }
            }

            if (Widgets.ButtonInvisible(Rect, true))
            {
                if (Event.current.button == 0)
                {
                    UpdateAssignment();
                    if (Completed && Tech.IsKnownBy(Pawn))
                    {
                        Messages.Message("TechAlreadyKnown".Translate(Tech), Pawn, MessageTypeDefOf.RejectInput, false);
                    }
                }
                if (Event.current.button == 1)
                {
                    MainButtonDefOf.Research.Worker.InterfaceTryActivate();
                    ResearchTree_Patches.Interest = Tech;
                }
                Event.current.Use();
            }
        }

        public List<ThingDef> MissingFacilities()
        {
            return ResearchTree_Patches.MissingFacilities(Tech);
        }

        private void DrawStats(TechLevel pawnTechLevel, float achieved)
        {
            Texture icon = null;
            string iconTip = null;
            if (achieved < 1)
            {
                float cost = Tech.IndividualizedCost(pawnTechLevel, achieved, KnownSucessor);
                Text.Anchor = TextAnchor.UpperRight;
                Text.Font = cost > 1000000 ? GameFont.Tiny : GameFont.Small;
                Widgets.Label(CostLabelRect, cost.ToStringByStyle(ToStringStyle.Integer));
                string costTip = Tech.IndividualizedCostExplainer(pawnTechLevel, achieved, cost, KnownSucessor);
                TooltipHandler.TipRegion(CostLabelRect, costTip);
                icon = ResearchTree_Assets.ResearchIcon;
                iconTip = costTip;
            }
            else if (!Completed)
            {
                icon = ContentFinder<Texture2D>.Get("UI/exclamation", true);
                iconTip = "MasteredButNotDocumented".Translate(Pawn);
            }
            else
            {
                icon = ContentFinder<Texture2D>.Get("UI/check", true);
                iconTip = "MasteredAndArchived".Translate(Pawn);
            }
            GUI.DrawTexture(CostIconRect, icon, ScaleMode.ScaleToFit);
            TooltipHandler.TipRegion(CostIconRect, iconTip);


        }

        public void DrawAt(Vector2 pos, Vector2 size, Rect visibleRect, Rect indicatorRect, TechLevel pawnTechLevel, bool forceDetailedMode = false)
        {
            SetRects(pos, size);
            SetMarked(indicatorRect);
            Draw(visibleRect, pawnTechLevel, !forceDetailedMode);
            SetRects();
        }

        private string GetResearchTooltipString()
        {
            var text = new StringBuilder();
            text.AppendLine(Tech.description);
            text.AppendLine();
            var leftClick = AppropriateLeftClickTip();
            if (!leftClick.NullOrEmpty()) text.AppendLine(leftClick.Translate());
            text.AppendLine("RightClickToTree".Translate());
            return text.ToString();
        }

        private string AppropriateLeftClickTip()
        {
            if (techComp != null && !techComp.homework.NullOrEmpty() && techComp.homework.Contains(Tech)) return "ClickToUnassign";
            var finished = Tech.IsFinished;
            var known = Tech.IsKnownBy(Pawn);
            if (!finished && known) return "ClickToAssignForDocumentation";
            if (finished && !known) return "ClickToAssignForStudying";
            return null;
        }

        private void SetMarked(Rect rect)
        {
            if (Assigned)
            {
                Texture2D face;
                string assignment = null;
                if (Known)
                {
                    face = ContentFinder<Texture2D>.Get("UI/write", true);
                    assignment = TechStrings.headerWrite;
                }
                else if (Tech.IsFinished)
                {
                    face = ContentFinder<Texture2D>.Get("UI/read");
                    assignment = TechStrings.headerRead;
                }
                else
                {
                    face = ContentFinder<Texture2D>.Get("UI/research");
                    assignment = TechStrings.headerResearch;
                }
                TooltipHandler.TipRegionByKey(rect, assignment);
                if (Widgets.ButtonImage(rect, face)) UpdateAssignment();
            }
        }

        public virtual bool Assigned 
        {
            get
            {
                if (Pawn.TechBound() && !techComp.homework.NullOrEmpty()) return techComp.homework.Contains(Tech);
                else return false;
            }
        }

        public virtual bool Known 
        {
            get
            {
                if (Pawn.TechBound()) return Tech.IsKnownBy(Pawn);
                else return false;
            }
        }

        public void UpdateAssignment()
        {
            if (techComp != null)
            {
                if (Pawn.TechBound() && !Assigned && ((!techComp.expertise.ContainsKey(Tech) || techComp.expertise[Tech] < 1f) || !Completed)) techComp.AssignBranch(Tech);
                else if (Assigned) techComp.CancelBranch(Tech);
            }
        }
    }
}