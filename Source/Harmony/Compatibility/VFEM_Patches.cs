using HarmonyLib;
using System;
using System.Text;
using Verse;

namespace HumanResources
{
    public static class VFEM_Patches
    {
        private static Type SupercomputerType = AccessTools.TypeByName("VFEMech.Supercomputer");

        public static void Execute(Harmony instance)
        {
            instance.Patch(AccessTools.Method(SupercomputerType, "Tick"),
                new HarmonyMethod(typeof(VFEM_Patches), nameof(Supercomputer_Tick_Prefix)));

            instance.Patch(AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.GetInspectString)),
                null, new HarmonyMethod(typeof(VFEM_Patches), nameof(Supercomputer_GetInspectString_Postfix)));
        }

        public static void Supercomputer_GetInspectString_Postfix(ThingWithComps __instance, ref string __result)
        {
            if (__instance.def.thingClass != SupercomputerType) return;
            StringBuilder appended = new StringBuilder(__result);
            if (!__instance.Map.listerBuildings.ColonistsHaveBuildingWithPowerOn(TechDefOf.NetworkServer))
            {
                appended.AppendInNewLine("NoAvailableServer".Translate());
                __result = appended.ToString();
                return;
            }
            var current = Find.ResearchManager.currentProj;
            if (current != null)
            {
                appended.AppendInNewLine($"{TechStrings.gerundResearch.CapitalizeFirst()} {current.label}");
                __result = appended.ToString();
                return;
            }
        }

        public static bool Supercomputer_Tick_Prefix(ThingWithComps __instance)
        {
            return Find.TickManager.TicksGame % 2500 != 0 || __instance.Map.listerBuildings.ColonistsHaveBuildingWithPowerOn(TechDefOf.NetworkServer);
        }
    }
}


