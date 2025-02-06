using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace HumanResources
{
    [HarmonyPatch(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith), new Type[] { typeof(Pawn), typeof(InteractionDef) })]
    public static class Pawn_InteractionsTracker_TryInteractWithBase
    {
        public static void Prefix(Pawn recipient)
        {
            ResearchManager_ApplyKnowledge.Pawn = recipient;
        }
        public static void Postfix()
        {
            ResearchManager_ApplyKnowledge.Pawn = null;
        }
    }
}