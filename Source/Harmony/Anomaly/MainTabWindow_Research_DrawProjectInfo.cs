using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HumanResources
{
    [HarmonyPatch(typeof(MainTabWindow_Research), nameof(MainTabWindow_Research.DrawProjectInfo))]
    public static class MainTabWindow_Research_DrawProjectInfo
    {
        public static bool Prefix(MainTabWindow_Research __instance, Rect rect, ResearchProjectDef ___selectedProject)
        {
            DrawProjectInfoModified(__instance, rect, ___selectedProject);
            return false;
        }

        public static void DrawProjectInfoModified(MainTabWindow_Research window, Rect rect, ResearchProjectDef tech)
        {
            int num = (ModsConfig.AnomalyActive && window.curTabInt == ResearchTabDefOf.Anomaly) ? 2 : 1;
            float num2 = (num > 1) ? (75f * (float)num) : 100f;
            Rect workingRect = rect;
            workingRect.yMin = rect.yMax - num2;
            workingRect.yMax = rect.yMax;
            Rect menuRect = workingRect;
            Rect boxRect = workingRect;
            boxRect.y = workingRect.y - 30f;
            boxRect.height = 28f;
            workingRect = workingRect.ContractedBy(10f);
            workingRect.y += 5f;
            Text.Font = GameFont.Medium;
            Widgets.Label(boxRect, "Assignments".Translate());
            Text.Font = GameFont.Small;
            Rect startButRect = new Rect
            {
                y = boxRect.y - 55f - 10f,
                height = 55f,
                x = rect.center.x - rect.width / 4f,
                width = rect.width / 2f + 20f
            };
            Widgets.DrawMenuSection(menuRect);
            //Research Progress replaced with pawn assignments
            float height = workingRect.height;
            Vector2 frameOffset = new Vector2(0, workingRect.y);
            float startPos = workingRect.x - height/4;
            if (Widgets.ButtonText(startButRect, "Assign".Translate(), true, true, true, null))
            {
                tech.SelectMenu(false, true);
            }
            tech.DrawAssignmentsArray(height, frameOffset, startPos);
            //Dev options
            if (Prefs.DevMode && !Find.ResearchManager.IsCurrentProject(window.selectedProject) && !window.selectedProject.IsFinished)
            {
                Text.Font = GameFont.Tiny;
                if (Widgets.ButtonText(new Rect(rect.xMax - 120f, boxRect.y, 120f, 25f), "Debug: Finish now", true, true, true, null))
                {
                    Find.ResearchManager.SetCurrentProject(window.selectedProject);
                    Find.ResearchManager.FinishProject(window.selectedProject, false, null, true);
                }
                Text.Font = GameFont.Small;
            }
            if (Prefs.DevMode && !window.selectedProject.TechprintRequirementMet)
            {
                Text.Font = GameFont.Tiny;
                if (Widgets.ButtonText(new Rect(rect.xMax - 300f, boxRect.y, 170f, 25f), "Debug: Apply techprint", true, true, true, null))
                {
                    Find.ResearchManager.ApplyTechprint(window.selectedProject, null);
                    SoundDefOf.TechprintApplied.PlayOneShotOnCamera(null);
                }
                Text.Font = GameFont.Small;
            }
            float iconSize = 50f;
            Vector2 iconPos = new Vector2(rect.xMax - iconSize, rect.y);
            Rect displacedTxt = rect;
            if (tech.DrawStorageMarker(iconPos, iconSize, iconSize, false, false))
            {
                displacedTxt.width -= iconSize;
            };
            float y = 0f;
            window.DrawProjectPrimaryInfo(displacedTxt, ref y);
            window.DrawProjectScrollView(new Rect(0f, y, rect.width, 0f)
            {
                yMax = startButRect.yMin - 10f
            });
        }

    }
}


