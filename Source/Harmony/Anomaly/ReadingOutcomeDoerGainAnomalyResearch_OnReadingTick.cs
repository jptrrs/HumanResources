using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace HumanResources
{
    [HarmonyPatch(typeof(ReadingOutcomeDoerGainAnomalyResearch), nameof(ReadingOutcomeDoerGainAnomalyResearch.OnReadingTick), new Type[] { typeof(Pawn), typeof(float) })]
    public static class ReadingOutcomeDoerGainAnomalyResearch_OnReadingTick
    {
        public static void Prefix(Pawn reader)
        {
            ResearchManager_ApplyKnowledge.Pawn = reader;
        }
        public static void Postfix()
        {
            ResearchManager_ApplyKnowledge.Pawn = null;
        }
    }
}


