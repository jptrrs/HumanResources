// ResearchNode.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace HumanResources
{
    public class ExpertiseNode : Node
    {
        public ResearchProjectDef Research;

        private Pawn Pawn;

        private object pseudoParent;

        private static Type pseudoParentType = ResearchTree_Patches.ResearchNodeType();

        private MethodInfo GetResearchTooltipStringInfo = AccessTools.Method(pseudoParentType, "GetResearchTooltipString");

        public ExpertiseNode(ResearchProjectDef research, Pawn pawn)
        {
            Pawn = pawn;
            Research = research;
            _pos = new Vector2(0, research.researchViewY + 1);
            pseudoParent = Activator.CreateInstance(pseudoParentType, research);
        }

        public override Color Color
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

        public override bool Completed => Research.IsFinished;
        public override bool Available => !Research.IsFinished && (DebugSettings.godMode || BuildingPresent());

        public override Color EdgeColor
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

        public override void Draw(Rect visibleRect, bool forceDetailedMode = false)
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
                    //progressBarRect.xMin += Research.ProgressPercent * progressBarRect.width;
                    progressBarRect.xMin += techComp.expertise[Research] * progressBarRect.width;
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
                    Text.Font = GameFont.Medium;
                    Widgets.Label(Rect, Research.LabelCap);
                }

                // draw research cost and icon
                if (detailedMode)
                {
                    Text.Anchor = TextAnchor.UpperRight;
                    Text.Font = Research.CostApparent > 1000000 ? GameFont.Tiny : GameFont.Small;
                    Widgets.Label(CostLabelRect, Research.CostApparent.ToStringByStyle( ToStringStyle.Integer ));
                    GUI.DrawTexture(CostIconRect, !Completed && !Available ? ResearchTree_Assets.Lock : ResearchTree_Assets.ResearchIcon, ScaleMode.ScaleToFit);
                }

                Text.WordWrap = true;

                // attach description and further info to a tooltip
                TooltipHandler.TipRegion(Rect, GetResearchTooltipString, Research.GetHashCode());
                if (!BuildingPresent())
                {
                    string languageKey = null;
                    if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing == "fluffy.researchtree"))
                    {
                        languageKey = "Fluffy.ResearchTree.MissingFacilities";
                    }
                    else
                    {
                        languageKey = "ResearchPal.MissingFacilities";
                    }
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
        }

        public List<ThingDef> MissingFacilities()
        {
            return ResearchTree_Patches.MissingFacilities(Research);
        }

        public void DrawAt(Vector2 pos, Rect visibleRect, bool forceDetailedMode = false)
        {
            SetRects(pos);
            Draw(visibleRect, forceDetailedMode);
            SetRects();
        }

        private string GetResearchTooltipString() => (string)GetResearchTooltipStringInfo.Invoke(pseudoParent, new object[] { });
    }
}