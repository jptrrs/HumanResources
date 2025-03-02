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
            Rect rect2 = rect;
            rect2.yMin = rect.yMax - num2;
            rect2.yMax = rect.yMax;
            Rect rect3 = rect2;
            Rect rect4 = rect2;
            rect4.y = rect2.y - 30f;
            rect4.height = 28f;
            rect2 = rect2.ContractedBy(10f);
            rect2.y += 5f;
            Text.Font = GameFont.Medium;
            Widgets.Label(rect4, "Assignments".Translate());
            Text.Font = GameFont.Small;
            Rect startButRect = new Rect
            {
                y = rect4.y - 55f - 10f,
                height = 55f,
                x = rect.center.x - rect.width / 4f,
                width = rect.width / 2f + 20f
            };
            Widgets.DrawMenuSection(rect3);
            //Research Progress
            //if (ModsConfig.AnomalyActive && window.curTabInt == ResearchTabDefOf.Anomaly)
            //{
            //    Rect rect5 = rect2;
            //    rect5.height = rect2.height / 2f;
            //    Rect rect6 = rect5;
            //    rect6.yMin = rect2.yMax - rect5.height;
            //    rect6.yMax = rect2.yMax;
            //    ResearchProjectDef project = Find.ResearchManager.GetProject(KnowledgeCategoryDefOf.Basic);
            //    ResearchProjectDef project2 = Find.ResearchManager.GetProject(KnowledgeCategoryDefOf.Advanced);
            //    if (project == null && project2 == null)
            //    {
            //        using (new TextBlock(TextAnchor.MiddleCenter))
            //        {
            //            Widgets.Label(rect2, "NoProjectSelected".Translate());
            //            goto IL_285;
            //        }
            //    }
            //    float prefixWidth = DefDatabase<KnowledgeCategoryDef>.AllDefs.Max((KnowledgeCategoryDef x) => Text.CalcSize(x.LabelCap + ":").x);
            //    window.DrawProjectProgress(rect5, project, KnowledgeCategoryDefOf.Basic.LabelCap, prefixWidth);
            //    window.DrawProjectProgress(rect6, project2, KnowledgeCategoryDefOf.Advanced.LabelCap, prefixWidth);
            //}
            //else
            //{
            //ResearchProjectDef project3 = Find.ResearchManager.GetProject(null);
            //if (project3 == null)
            //{
            //    using (new TextBlock(TextAnchor.MiddleCenter))
            //    {
            //        Widgets.Label(rect2, "NoProjectSelected".Translate());
            //        goto IL_285;
            //    }
            //}
            //IL_285:
            float height = rect2.height;
            Vector2 frameOffset = new Vector2(0, rect2.y);
            float startPos = rect2.x - height/4;
            if (Widgets.ButtonText(startButRect, "Research".Translate(), true, true, true, null))
            {
                tech.SelectMenu(false, true);
            }
            tech.DrawAssignmentsArray(height, frameOffset, startPos);

            //Start Button + Dev options
            //window.DrawStartButton(startButRect);
            if (Prefs.DevMode && !Find.ResearchManager.IsCurrentProject(window.selectedProject) && !window.selectedProject.IsFinished)
            {
                Text.Font = GameFont.Tiny;
                if (Widgets.ButtonText(new Rect(rect.xMax - 120f, rect4.y, 120f, 25f), "Debug: Finish now", true, true, true, null))
                {
                    Find.ResearchManager.SetCurrentProject(window.selectedProject);
                    Find.ResearchManager.FinishProject(window.selectedProject, false, null, true);
                }
                Text.Font = GameFont.Small;
            }
            if (Prefs.DevMode && !window.selectedProject.TechprintRequirementMet)
            {
                Text.Font = GameFont.Tiny;
                if (Widgets.ButtonText(new Rect(rect.xMax - 300f, rect4.y, 170f, 25f), "Debug: Apply techprint", true, true, true, null))
                {
                    Find.ResearchManager.ApplyTechprint(window.selectedProject, null);
                    SoundDefOf.TechprintApplied.PlayOneShotOnCamera(null);
                }
                Text.Font = GameFont.Small;
            }
            float y = 0f;
            window.DrawProjectPrimaryInfo(rect, ref y);
            window.DrawProjectScrollView(new Rect(0f, y, rect.width, 0f)
            {
                yMax = startButRect.yMin - 10f
            });
        }



    }
}


