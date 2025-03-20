using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace HumanResources
{
    class SemiRandom_Patch
    {
        private static bool Act = false;
        private static ResearchProjectDef selectedProject;

        public static void Execute(Harmony instance)
        {
            string root = "CM_Semi_Random_Research";
            Type Type = AccessTools.TypeByName(root+".MainTabWindow_NextResearch");
            instance.Patch(AccessTools.Method(Type, "DrawRightColumn"),
                new HarmonyMethod(typeof(SemiRandom_Patch), nameof(DrawRightColumn_Prefix)), new HarmonyMethod(typeof(SemiRandom_Patch), nameof(DrawRightColumn_Postfix)), null);
            MethodInfo CanStartMethod = AccessTools.Method(AccessTools.TypeByName(root + ".ResearchProjectDefExtensions"), "CanStartProject");
            if (CanStartMethod != null)
            {
                instance.Patch(AccessTools.Method("CM_Semi_Random_Research.ResearchProjectDefExtensions:CanStartProject"),
                    new HarmonyMethod(typeof(SemiRandom_Patch), nameof(CanStartProject_Prefix)), null, null);
                instance.Patch(AccessTools.Method(typeof(Widgets), "ButtonText", new Type[] { typeof(Rect), typeof(string), typeof(bool), typeof(bool), typeof(bool) }),
                    null, new HarmonyMethod(typeof(SemiRandom_Patch), nameof(ButtonText_Postfix)), null);
            }
            else
            {
                instance.Patch(AccessTools.PropertyGetter(typeof(ResearchProjectDef), "CanStartNow"),
                    new HarmonyMethod(typeof(SemiRandom_Patch), nameof(CanStarNow_Prefix)), null, null);
                instance.Patch(AccessTools.Method(typeof(Widgets), "ButtonText", new Type[] { typeof(Rect), typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(TextAnchor) }),
                    null, new HarmonyMethod(typeof(SemiRandom_Patch), nameof(ButtonText_Postfix)), null);
            }
            instance.Patch(AccessTools.Method(Type, "DrawResearchButton"),
                null, new HarmonyMethod(typeof(SemiRandom_Patch), nameof(DrawResearchButton_Postfix)), null);
        }

        public static void DrawRightColumn_Prefix(object __instance)
        {
            Act = true;
        }

        public static void DrawRightColumn_Postfix()
        {
            Act = false;
            if (selectedProject != null) selectedProject = null;
        }

        private static void CanStartProject_Prefix(ResearchProjectDef researchProject)
        {
            selectedProject = researchProject;
        }

        private static void CanStarNow_Prefix(ResearchProjectDef __instance)
        {
            selectedProject = __instance;
        }

        private static bool ButtonText_Postfix(bool result)
        {
            if (!result || !Act || selectedProject == null) return result;
            selectedProject.SelectMenu(false, true);
            return false;
        }

        public static void DrawResearchButton_Postfix(Rect drawRect, ResearchProjectDef projectDef)
        {
            float height = drawRect.height / 1.5f;
            Vector2 frameOffset = new Vector2(0, drawRect.yMax - height * 0.75f);
            float startPos = drawRect.xMax - height / 2;
            projectDef.DrawAssignmentsArray(height, frameOffset, startPos, true);
        }

    }
}
