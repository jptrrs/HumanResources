using HarmonyLib;
using RimWorld;
using System;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace HumanResources
{
    [HarmonyPatch(typeof(ResearchManager))]
    public static class ResearchManager_ApplyKnowledge
    {
        public static Pawn Pawn;
        private static CompKnowledge Comp => Pawn.TryGetComp<CompKnowledge>();

        //determines project from category then triggers next step. Triggered by: StudyAnomaly, TryInteractWith
        [HarmonyPatch(nameof(ResearchManager.ApplyKnowledge), new Type[] { typeof(KnowledgeCategoryDef), typeof(float) })]
        public static bool Prefix(ResearchManager __instance, KnowledgeCategoryDef category, float amount)
        {
            if (!ModLister.CheckAnomaly("Knowledge") || amount <= 0f) return false;
            LearnAnomalyTechRecursive(category, amount);
            return false;
        }

        //checks for project pre-requisites and if finished, then triggers finishproject. Triggered by: ApplyKnowledge, OnReadingTick
        [HarmonyPatch(nameof(ResearchManager.ApplyKnowledge), new Type[] { typeof(ResearchProjectDef), typeof(float), typeof(float) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
        public static void Prefix(ResearchProjectDef project, float amount, out float remainder)
        {
            LearnAnomalyTech(project, amount, out remainder);
        }

        private static void LearnAnomalyTechRecursive(KnowledgeCategoryDef category, float amount)
        {
            ResearchProjectDef project = Comp?.homework.Find(x => x.knowledgeCategory == category);
            if (project == null) return;
            bool flag = false;
            if (project != null)
            {
                float num;
                if (LearnAnomalyTech(project, amount, out num) && num > 0f)
                {
                    amount = num;
                    flag = true;
                }
            }
            else flag = true;
            if (flag && category.overflowCategory != null)
            {
                LearnAnomalyTechRecursive(category.overflowCategory, amount);
            }
        }

        public static bool LearnAnomalyTech(ResearchProjectDef project, float amount, out float remainder)
        {
            var expertise = Comp?.expertise;
            if (Comp == null) goto end;
            float num;
            if (expertise.TryGetValue(project, out num))
            {
                expertise[project] = num + amount;
            }
            else
            {
                expertise.Add(project, amount);
            }
            bool finished = expertise[project] >= project.Cost;
            if (project.PrerequisitesCompleted && finished) //This means trying to learn anything beyond what's alredy documented will waste the effort!
            {
                remainder = expertise[project] - project.knowledgeCost;
                //expertise[project] = Mathf.Min(expertise[project], project.Cost);
                Comp.LearnTech(project);
                return true;
            }
            end:
            remainder = 0f;
            return false;
        }
    }
}
