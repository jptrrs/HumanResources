using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace HumanResources
{
    //Checks if pawn knows how to repair something.
    [HarmonyPatch(typeof(RepairUtility), nameof(RepairUtility.PawnCanRepairEver), new Type[] { typeof(Pawn), typeof(Thing) })]
    public static class RepairUtility_PawnCanRepairEver
    {
        public static void Postfix(Pawn pawn, Thing t, ref bool __result)
        {
            if (__result && pawn.TechBound())
            {
                var requisites = t.def.researchPrerequisites;
                if (requisites.NullOrEmpty()) return;
                __result = requisites.All(x => x.IsKnownBy(pawn));
                if (!__result)
                {
                    var missing = requisites.Where(x => !x.IsKnownBy(pawn));
                    string preReqText = (missing.Count() > 1) ? missing.Select(x => x.label).ToStringSafeEnumerable() : missing.FirstOrDefault().label;
                    JobFailReason.Is("DoesntKnowHowToRepair".Translate(pawn, t.def.label, preReqText));
                }
            }
        }
    }
}
