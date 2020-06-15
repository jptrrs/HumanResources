using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
	[HarmonyPatch(typeof(ResearchProjectDef))]
	public static class ResearchProjectDef_Patches
	{
		[HarmonyPostfix]
		[HarmonyPatch("CanBeResearchedAt")]
		public static void CanBeResearchedAt_Postfix()
		{
			Log.Warning("One of your Mods called 'CanBeResearchedAt(Building_ResearchBench bench, bool ignoreResearchBenchPowerStatus)', which doesn't work with [JPT] Human Resources. Please share this Log with the Devs of Human Resources");
		}

		[HarmonyPrefix]
		[HarmonyPatch("PlayerHasAnyAppropriateResearchBench", MethodType.Getter)]
		public static bool PlayerHasAnyAppropriateResearchBench_Prefix(ResearchProjectDef __instance, ref bool __result)
		{
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				List<Building> allBuildingsColonist = maps[i].listerBuildings.allBuildingsColonist;
				for (int j = 0; j < allBuildingsColonist.Count; j++)
				{
					Building_WorkTable building_ResearchBench = allBuildingsColonist[j] as Building_WorkTable;
					if (building_ResearchBench != null && __instance.CanBeResearchedAt(building_ResearchBench, true))
					{
						__result = true;
						return false;
					}
				}
			}
			__result = false;
			return false;
		}
	}
}
