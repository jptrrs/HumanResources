using HarmonyLib;
using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace HumanResources
{
    public static class FluffyBreakdowns_Patches
    {
        public static void Execute(Harmony instance)
        {
            Type WorkGiverMaintenanceType = AccessTools.TypeByName("Fluffy_Breakdowns.WorkGiver_Maintenance");
            instance.Patch(AccessTools.Method(WorkGiverMaintenanceType, "JobOnThing"),
                null, new HarmonyMethod(typeof(FluffyBreakdowns_Patches), nameof(JobOnThing_Postfix)), null);
        }

        public static void JobOnThing_Postfix(Pawn pawn, Thing thing, ref Job __result)
        {
            if (!pawn.TechBound()) return;
            var requisites = thing.def.entityDefToBuild?.researchPrerequisites;
            if (requisites.NullOrEmpty() || requisites.All(x => x.IsKnownBy(pawn))) return;
            var missing = requisites.Where(x => !x.IsKnownBy(pawn));
            string preReqText = (missing.Count() > 1) ? missing.Select(x => x.label).ToStringSafeEnumerable() : missing.FirstOrDefault().label;
            JobFailReason.Is("DoesntKnowHowToRepair".Translate(pawn, thing.def.label, preReqText));
            __result = null;
        }

    }
}
