using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace HumanResources
{
    //checks for project pre-requisites and if finished, then triggers finishproject. Triggered by: ApplyKnowledge, OnReadingTick
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.ApplyKnowledge), new Type[] { typeof(ResearchProjectDef), typeof(float), typeof(float) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
    public static class ResearchManager_ApplyKnowledge_inner
    {
        public static void Prefix(ResearchProjectDef project, float amount, out float remainder)
        {
            ResearchManager_ApplyKnowledge.LearnAnomalyTech(project, amount, out remainder);
        }
    }
}
