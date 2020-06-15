using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    public static class Extension_ResearchProjectDef
	{
		public static bool CanBeResearchedAt(this ResearchProjectDef researchProjectDef, Building_WorkTable bench, bool ignoreResearchBenchPowerStatus)
		{
			if (researchProjectDef.requiredResearchBuilding != null && bench.def != researchProjectDef.requiredResearchBuilding)
			{
				return false;
			}
			if (!ignoreResearchBenchPowerStatus)
			{
				CompPowerTrader comp = bench.GetComp<CompPowerTrader>();
				if (comp != null && !comp.PowerOn)
				{
					return false;
				}
			}
			if (!researchProjectDef.requiredResearchFacilities.NullOrEmpty<ThingDef>())
			{
				CompAffectedByFacilities affectedByFacilities = bench.TryGetComp<CompAffectedByFacilities>();
				if (affectedByFacilities == null)
				{
					return false;
				}
				List<Thing> linkedFacilitiesListForReading = affectedByFacilities.LinkedFacilitiesListForReading;
				int j;
				int i;
				for (i = 0; i < researchProjectDef.requiredResearchFacilities.Count; i = j + 1)
				{
					if (linkedFacilitiesListForReading.Find((Thing x) => x.def == researchProjectDef.requiredResearchFacilities[i] && affectedByFacilities.IsFacilityActive(x)) == null)
					{
						return false;
					}
					j = i;
				}
			}
			return true;
		}
	}
}
