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
                    float amount = num;
                    if (__instance.IsProjectVisible(tech) && !tech.IsKnownBy(reader))
                    {
                        tech.Learned(num, amount * factor, reader);
                    }
                }
            }
            return false;
        }
    }
}
