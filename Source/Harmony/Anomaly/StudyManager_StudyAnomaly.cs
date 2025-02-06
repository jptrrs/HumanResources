using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace HumanResources
{
    //used by anomaly to trigger applyKnowledge, has pawn info
    [HarmonyPatch(typeof(StudyManager), nameof(StudyManager.StudyAnomaly), new Type[] { typeof(Thing), typeof(Pawn), typeof(float), typeof(KnowledgeCategoryDef) })]
    public static class StudyManager_StudyAnomaly
    {
        public static void Prefix(Pawn studier)
        {
            ResearchManager_ApplyKnowledge.Pawn = studier;
        }

        public static void Postfix()
        {
            ResearchManager_ApplyKnowledge.Pawn = null;
        }
    }
}
