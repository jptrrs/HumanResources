using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    public static class Extension_ThingDef
	{
		public static bool IsExempted(this ThingDef weapon)
        {
			return ModBaseHumanResources.ExemptSingleUseWeapons && !weapon.thingSetMakerTags.NullOrEmpty() && weapon.thingSetMakerTags.Any(tag => TechDefOf.WeaponsAlwaysBasic.thingSetMakerTags.Contains(tag));
		}

		public static bool NotReallyAWeapon(this ThingDef weapon)
        {
			return weapon.weaponTags.NullOrEmpty() || weapon.weaponTags.Any(tag => TechDefOf.WeaponsAlwaysBasic.weaponTags.Contains(tag));
        }
	}
} 
