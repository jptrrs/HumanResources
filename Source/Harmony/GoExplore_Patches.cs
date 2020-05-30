using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HumanResources
{
    public static class GoExplore_Patches
    {
        public static void Execute(Harmony instance)
        {
            Type ResearchRequestType = AccessTools.TypeByName("LetsGoExplore.WorldObject_ResearchRequestLGE");
            instance.Patch(AccessTools.Method(ResearchRequestType, "Outcome_Success", new Type[] { typeof(Caravan) }),
                new HarmonyMethod(typeof(GoExplore_Patches), nameof(Outcome_Success_Prefix)), null, null);
            instance.Patch(AccessTools.Method(ResearchRequestType, "Outcome_Triumph", new Type[] { typeof(Caravan) }),
                new HarmonyMethod(typeof(GoExplore_Patches), nameof(Outcome_Triumph_Prefix)), null, null);
        }

        public static bool Outcome_Success_Prefix(WorldObject __instance, Caravan caravan, ref IntRange ___SuccessFactionRelationOffset, ref FloatRange ___SuccessResearchAmount)
        {
            int factionRelationGain = ___SuccessFactionRelationOffset.RandomInRange;
            __instance.Faction.TryAffectGoodwillWith(Faction.OfPlayer, factionRelationGain, false, false, null, null);
            Pawn researcher = BestCaravanPawnUtility.FindPawnWithBestStat(caravan, StatDefOf.ResearchSpeed, null);
            ResearchProjectDef researchProjectDef = ApplyPointsToExpertise(___SuccessResearchAmount.RandomInRange, researcher);
            string discoveredTech = "No available research :(";
            if (researchProjectDef != null)
            {
                discoveredTech = researchProjectDef.LabelCap;
            }
            Find.LetterStack.ReceiveLetter("LetterLabelResearchRequest_SuccessLGE".Translate(), GetAltLetterText("LetterResearchRequest_SuccessHR".Translate(__instance.Faction.Name, Mathf.RoundToInt((float)factionRelationGain), discoveredTech), discoveredTech, caravan), LetterDefOf.PositiveEvent, caravan, null, null, null, null);
            return false;
        }

        public static bool Outcome_Triumph_Prefix(WorldObject __instance, Caravan caravan, ref IntRange ___TriumphFactionRelationOffset, ref FloatRange ___TriumphResearchAmount)
        {
            int factionRelationGain = ___TriumphFactionRelationOffset.RandomInRange;
            __instance.Faction.TryAffectGoodwillWith(Faction.OfPlayer, factionRelationGain, false, false, null, null);
            Pawn researcher = BestCaravanPawnUtility.FindPawnWithBestStat(caravan, StatDefOf.ResearchSpeed, null);
            ResearchProjectDef researchProjectDef = ApplyPointsToExpertise(___TriumphResearchAmount.RandomInRange, researcher);
            string discoveredTech = "No available research :(";
            if (researchProjectDef != null)
            {
                discoveredTech = researchProjectDef.LabelCap;
            }
            Find.LetterStack.ReceiveLetter("LetterLabelResearchRequest_TriumphLGE".Translate(), GetAltLetterText("LetterResearchRequest_TriumphHR".Translate(__instance.Faction.Name, Mathf.RoundToInt((float)factionRelationGain), discoveredTech), discoveredTech, caravan), LetterDefOf.PositiveEvent, caravan, null, null, null, null);
            return false;
        }

        public static ResearchProjectDef ApplyPointsToExpertise(float points, Pawn pawn)
        {
            ResearchManager researchManager = Find.ResearchManager;
            ResearchProjectDef result;
            if (!researchManager.AnyProjectIsAvailable) result = null;
            else
            {
                IEnumerable<ResearchProjectDef> source = from x in DefDatabase<ResearchProjectDef>.AllDefsListForReading
                                                         where x.CanStartNow
                                                         select x;
                source.TryRandomElementByWeight((ResearchProjectDef x) => 1f / x.baseCost, out result);
                points *= 12f; //stangely, that number is 121f on the mod source. I'm assuming that's a typo.
                float total = result.baseCost;
                CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
                if (techComp != null)
                {
                    Dictionary<ResearchProjectDef, float> expertise = techComp.expertise;
                    float num = result.GetProgress(expertise);
                    num += points / total;
                    expertise[result] = (num > 1) ? 1 : num;
                }
            }
            return result;
        }

        private static string GetAltLetterText(string baseText, string techName, Caravan caravan)
        {
            string text = baseText;
            Pawn pawn = BestCaravanPawnUtility.FindPawnWithBestStat(caravan, StatDefOf.ResearchSpeed, null);
            bool flag = pawn != null;
            if (flag)
            {
                text = text + "\n\n" + "ResearchRequestXPGainHR".Translate(pawn.LabelShort, techName, 5000f);
            }
            return text;
        }

    }
}
