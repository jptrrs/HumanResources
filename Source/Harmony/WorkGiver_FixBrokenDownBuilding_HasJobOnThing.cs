using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace HumanResources
{
    //Checks if pawn knows how to fix a broken building.
    [HarmonyPatch(typeof(WorkGiver_FixBrokenDownBuilding), nameof(WorkGiver_FixBrokenDownBuilding.HasJobOnThing))]
    public static class WorkGiver_FixBrokenDownBuilding_HasJobOnThing
    {
        public static void Postfix(Pawn pawn, Thing t, ref bool __result)
        {
            if (!__result || !pawn.TechBound()) return;
            var requisites = t.def.researchPrerequisites;
            if (requisites.NullOrEmpty() || requisites.All(x => x.IsKnownBy(pawn))) return;
            var missing = requisites.Where(x => !x.IsKnownBy(pawn));
            string preReqText = (missing.Count() > 1) ? missing.Select(x => x.label).ToStringSafeEnumerable() : missing.FirstOrDefault().label;
            JobFailReason.Is("DoesntKnowHowToRepair".Translate(pawn, t.def.label, preReqText));
            __result = false;
        }

    }
}
