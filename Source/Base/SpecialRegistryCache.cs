using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HumanResources
{
    public class SpecialRegistryCache : IImmutableSpecialRegistryCache
    {
        
        private readonly List<ThingDef> _allWeapons = new List<ThingDef>();

        
        public IEnumerable<ThingDef> AllWeapons
        {
            get
            {
                if (ModBaseHumanResources.OptimizationExperimentalWeaponCache)
                    return _allWeapons;
                return DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWeapon);
            }
        }

        public void NotifyAllDefsLoaded()
        {
            //TODO: This is quite ugly. Those setting switches should be event-driver and actually implement strategy pattern.
            if (ModBaseHumanResources.OptimizationExperimentalWeaponCache)
            {
                _allWeapons.Clear();
                _allWeapons.AddRange(DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWeapon));
            }
        }
        
    }
}