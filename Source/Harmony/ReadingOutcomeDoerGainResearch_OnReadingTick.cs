using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    //Diverts schematics books to the pawn's expertise
    [HarmonyPatch(typeof(ReadingOutcomeDoerGainResearch), nameof(ReadingOutcomeDoerGainResearch.OnReadingTick))]
    public static class ReadingOutcomeDoerGainResearch_OnReadingTick
    {
        public static bool Prefix(ReadingOutcomeDoerGainResearch __instance, Pawn reader, float factor, Dictionary<ResearchProjectDef, float> ___values)
        {
            if (reader?.TryGetComp<CompKnowledge>() != null)
            {
                foreach (KeyValuePair<ResearchProjectDef, float> tuple in ___values)
                {
                    ResearchProjectDef loadedTech;
                    float num;
                    tuple.Deconstruct(out loadedTech, out num);
                    ResearchProjectDef tech = loadedTech;
                    float amount = num * factor;
                    amount *= 10; // Adjusted to 1/5 the speed of studying at the ideal desk if the book is of good quality. A legendary book would yield ~1/3 speed.
                    if (__instance.IsProjectVisible(tech) && !tech.IsKnownBy(reader))
                    {
                        tech.Learned(amount, TechDefOf.LearnTech.workAmount, reader);
                    }
                }
            }
            return false;
        }
    }
}

//BOOK QUALITY/VALUE CORRELATION (for those with one tech only)
//Awful         20 / Hour - 0.008
//Poor          30 / Hour - 0.012
//Normal        40 / Hour - 0.016
//Good          50 / Hour - 0.020
//Excellent     60 / Hour - 0.024
//Masterwork    70 / Hour - 0.028
//Legendary     80 / Hour - 0.032