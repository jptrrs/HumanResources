using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    public static class Extension_ResearchProjectDef
	{
		public static bool Alt_CanBeResearchedAt(this ResearchProjectDef project, Building_WorkTable bench)
		{
			if (project.requiredResearchBuilding != null && bench.def != project.requiredResearchBuilding)
			{
				return false;
			}
			if (!project.requiredResearchFacilities.NullOrEmpty<ThingDef>())
			{
				CompAffectedByFacilities affectedByFacilities = bench.TryGetComp<CompAffectedByFacilities>();
				if (affectedByFacilities == null)
				{
					return false;
				}
				List<Thing> linkedFacilitiesListForReading = affectedByFacilities.LinkedFacilitiesListForReading;
				int j;
				int i;
				for (i = 0; i < project.requiredResearchFacilities.Count; i = j + 1)
				{
					if (linkedFacilitiesListForReading.Find((Thing x) => x.def == project.requiredResearchFacilities[i] && affectedByFacilities.IsFacilityActive(x)) == null)
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
