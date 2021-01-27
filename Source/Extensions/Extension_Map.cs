using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HumanResources
{
    public static class Extension_Map
	{
		public static bool ServerAvailable(this Map map)
        {
            var servers = map.listerBuildings.AllBuildingsColonistOfClass<Building_NetworkServer>();
            if (!servers.EnumerableNullOrEmpty())
                return servers.Where(x => x.TryGetComp<CompPowerTrader>().PowerOn).Any();
            return false;
        }
	}
} 
