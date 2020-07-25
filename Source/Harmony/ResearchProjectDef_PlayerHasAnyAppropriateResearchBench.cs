using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    //Adapts appropriate research bench check to our modified benches
    //[HarmonyPatch(typeof(ResearchProjectDef), "PlayerHasAnyAppropriateResearchBench", MethodType.Getter)]
    public static class ResearchProjectDef_PlayerHasAnyAppropriateResearchBench
    {
        public static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
			if (!__result)
            {
				List<Map> maps = Find.Maps;
				for (int i = 0; i < maps.Count; i++)
				{
					List<Building> allBuildingsColonist = maps[i].listerBuildings.allBuildingsColonist;
					for (int j = 0; j < allBuildingsColonist.Count; j++)
					{
						Building_WorkTable researchStation = allBuildingsColonist[j] as Building_WorkTable;
						if (researchStation != null && __instance.Alt_CanBeResearchedAt(researchStation))
						{
							__result = true;
						}
					}
				}
			}
		}
	}
}
