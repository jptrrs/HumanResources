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
    public class ExpertiseNode
    {
        //pulled from Node
        protected const float Offset = 2f;
        protected bool _largeLabel;
        protected Vector2 _pos = Vector2.zero;
        protected Rect
            _queueRect,
            _rect,
            _labelRect,
            _costLabelRect,
            _costIconRect,
            _iconsRect,
            _lockRect;

        protected bool _rectsSet;
        protected Vector2 _topLeft = Vector2.zero,
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
                if (Math.Abs(_pos.x - value) < Constants.Epsilon)
                    return;
                _pos.x = value;
            }
        }
        public virtual float Yf
        {
            get => _pos.y;
            set
            {
                if (Math.Abs(_pos.y - value) < Constants.Epsilon)
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
                (X - 1) * (Constants.NodeSize.x + Constants.NodeMargins.x),
                (Yf - 1) * (Constants.NodeSize.y + Constants.NodeMargins.y));

            SetRects(_topLeft);
        }
        public void SetRects(Vector2 topLeft)
        {
            // main rect
            _rect = new Rect(topLeft.x,
                              topLeft.y,
                              Constants.NodeSize.x,
                              Constants.NodeSize.y);

            // queue rect
            _queueRect = new Rect(_rect.xMax - Constants.QueueLabelSize / 2f,
                                   _rect.yMin + (_rect.height - Constants.QueueLabelSize) / 2f, Constants.QueueLabelSize,
                                   Constants.QueueLabelSize);

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

        public ResearchProjectDef Research;

        private Pawn Pawn;

        //private object pseudoParent;

        private static Type pseudoParentType = ResearchTree_Patches.ResearchNodeType();

        private MethodInfo GetResearchTooltipStringInfo = AccessTools.Method(pseudoParentType, "GetResearchTooltipString");

        public virtual Texture2D indicator => Completed ? ContentFinder<Texture2D>.Get("UI/write", true) : ContentFinder<Texture2D>.Get("UI/read");

        public ExpertiseNode(ResearchProjectDef research, Pawn pawn)
        {
            Pawn = pawn;
            Research = research;
            _pos = new Vector2(0, research.researchViewY + 1);
            //pseudoParent = Activator.CreateInstance(pseudoParentType, research);
        }

        public Color Color
        {
            get
            {
                if (Highlighted)
                    return GenUI.MouseoverColor;
                if (Completed)
                    return ResearchTree_Assets.ColorCompleted[Research.techLevel];
                if (Available)
                    return ResearchTree_Assets.ColorAvailable[Research.techLevel];
                return ResearchTree_Assets.ColorUnavailable[Research.techLevel];
            }
        }

        public bool Completed => Research.IsFinished;
        public bool Available => !Research.IsFinished && (DebugSettings.godMode || BuildingPresent());

        public Color EdgeColor
        {
            get
            {
                if (Highlighted)
                    return GenUI.MouseoverColor;
                if (Completed)
                    return ResearchTree_Assets.ColorCompleted[Research.techLevel];
                if (Available)
                    return ResearchTree_Assets.ColorAvailable[Research.techLevel];
                return ResearchTree_Assets.ColorUnavailable[Research.techLevel];
            }
        }

        private CompKnowledge techComp => Pawn.TryGetComp<CompKnowledge>();

        public bool BuildingPresent()
        {
            return ResearchTree_Patches.BuildingPresent(Research);
        }

        public void Draw(Rect visibleRect, bool forceDetailedMode = false)
        {
            if (!IsVisible(visibleRect))
            {
                Highlighted = false;
                return;
            }

            var detailedMode = forceDetailedMode; //|| MainTabWindow_ResearchTree.Instance.ZoomLevel < DetailedModeZoomLevelCutoff;
            var mouseOver = Mouse.IsOver(Rect);
            if (Event.current.type == EventType.Repaint)
            {
                // researches that are completed or could be started immediately, and that have the required building(s) available
                GUI.color = mouseOver ? GenUI.MouseoverColor : Color;

                if (mouseOver || Highlighted)
                    GUI.DrawTexture(Rect, ResearchTree_Assets.ButtonActive);
                else
                    GUI.DrawTexture(Rect, ResearchTree_Assets.Button);

                // grey out center to create a progress bar effect, completely greying out research not started.
                //if (Available)
                //{
                var progressBarRect = Rect.ContractedBy(3f);
                //GUI.color = Assets.ColorAvailable[Research.techLevel];
                GUI.color = Widgets.WindowBGFillColor;
                if (techComp.expertise.ContainsKey(Research))
                {
                    progressBarRect.xMin += techComp.expertise[Research] * progressBarRect.width;
                }
                //else
                //{
                //    progressBarRect.xMin += Research.ProgressPercent * progressBarRect.width;
                //}
                GUI.DrawTexture(progressBarRect, BaseContent.WhiteTex);
                //}

                Highlighted = false;

                // draw the research label
                if (!Completed && !Available)
                    GUI.color = Color.grey;
                else
                    GUI.color = Color.white;

                if (detailedMode)
                {
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.WordWrap = false;
                    Text.Font = _largeLabel ? GameFont.Tiny : GameFont.Small;
                    Widgets.Label(LabelRect, Research.LabelCap);
                }
                else
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.WordWrap = false;
                    Text.Font = GameFont.Small;//GameFont.Medium;
                    Widgets.Label(Rect, Research.LabelCap);
                }

                // draw research cost and icon
                if (detailedMode)
                {
                    Text.Anchor = TextAnchor.UpperRight;
                    Text.Font = Research.CostApparent > 1000000 ? GameFont.Tiny : GameFont.Small;
                    Widgets.Label(CostLabelRect, Research.CostApparent.ToStringByStyle(ToStringStyle.Integer));
                    GUI.DrawTexture(CostIconRect, !Completed && !Available ? ResearchTree_Assets.Lock : ResearchTree_Assets.ResearchIcon, ScaleMode.ScaleToFit);
                }

                Text.WordWrap = true;

                // attach description and further info to a tooltip
                TooltipHandler.TipRegion(Rect, GetResearchTooltipString, Research.GetHashCode());
                if (!BuildingPresent())
                {
                    string root = HarmonyPatches.ResearchPal ? "ResearchPal" : "Fluffy.ResearchTree";
                    string languageKey = root + ".MissingFacilities";
                    TooltipHandler.TipRegion(Rect, languageKey.Translate(string.Join(", ", MissingFacilities().Select(td => td.LabelCap).ToArray())));
                }

                // draw unlock icons
                if (detailedMode)
                {
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
                UpdateAssignment();
                if (Completed && Research.IsKnownBy(Pawn))
                {
                    MainButtonDefOf.Research.Worker.InterfaceTryActivate();
                    ResearchTree_Patches.subjectToShow = Research;
                }
            }

        }

        public List<ThingDef> MissingFacilities()
        {
            return ResearchTree_Patches.MissingFacilities(Research);
        }

        public void DrawAt(Vector2 pos, Rect visibleRect, Rect indicatorRect, bool forceDetailedMode = false)
        {
            SetRects(pos);
            SetMarked(indicatorRect);
            Draw(visibleRect, !forceDetailedMode);
            SetRects();
        }

        private string GetResearchTooltipString()
        {
            var text = new StringBuilder();
            text.AppendLine(Research.description );
            return text.ToString();
        }

        private void SetMarked(Rect rect)
        {
            if (Assigned)
            {
                Texture2D face = Known ? ContentFinder<Texture2D>.Get("UI/write", true) : ContentFinder<Texture2D>.Get("UI/read");
                if (Widgets.ButtonImage(rect, face, false)) UpdateAssignment();
            }
        }

        public virtual bool Assigned 
        {
            get
            {
                if (Pawn.IsColonist && techComp != null) return Pawn.TryGetComp<CompKnowledge>().homework.Contains(Research);
                else return false;
            }
        }

        public virtual bool Known 
        {
            get
            {
                if (Pawn.IsColonist && techComp != null) return Research.IsKnownBy(Pawn);
                else return false;
            }
        }

        private void UpdateAssignment()
        {
            if (techComp != null)
            {
                if (Pawn.IsColonist && techComp != null && !Assigned && ((!techComp.expertise.ContainsKey(Research) || techComp.expertise[Research] < 1f) || !Completed)) techComp.homework.Add(Research);
                else techComp.homework.Remove(Research);
            }
        }
    }
}