using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace HumanResources
{
    //Prevents pawns from choosing to read a schematics book they already know.
    [HarmonyPatch(typeof(BookUtility), nameof(BookUtility.IsValidBook))]
    public static class BookUtility_IsValidBook
    {
        public static bool Prefix(Thing thing, Pawn pawn)
        {
            if (thing.def != ThingDefOf.Schematic) return true;
            ReadingOutcomeProperties doerProps = thing.TryGetComp<CompBook>()?.Props.doers.First(x => x is BookOutcomeProperties_GainResearch);
            if (doerProps != null && doerProps is BookOutcomeProperties_GainResearch doerPropsResearch)
            {
                return doerPropsResearch.include.Select(x => x.project).Any(y => !y.IsKnownBy(pawn));
            }
            return false;
        }
    }
}
