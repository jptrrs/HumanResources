using System.Linq;
using RimWorld;
using Verse;

namespace HumanResources
{
    public static class Extensions
	{
        //Map
        public static bool ServerAvailable(this Map map)
        {
            var servers = map.listerBuildings.AllBuildingsColonistOfDef(TechDefOf.NetworkServer);
            if (!servers.EnumerableNullOrEmpty())
                return servers.Where(x => x.TryGetComp<CompPowerTrader>().PowerOn).Any();
            return false;
        }

        //Pawn
        public static bool TechBound(this Pawn pawn)
        {
            return (pawn.IsColonist || (HarmonyPatches.PrisonLabor && pawn.IsPrisoner)) && pawn.TryGetComp<CompKnowledge>() != null;
        }

        public static bool IsGuest(this Pawn pawn)
        {
            return Hospitality_Patches.active && Hospitality_Patches.IsGuestExternal(pawn);
        }

        //Thing
        public static ResearchProjectDef TryGetTech(this Thing book)
        {
            return (book.Stuff != null && book.Stuff.IsWithinCategory(TechDefOf.Knowledge)) ? ModBaseHumanResources.unlocked.techByStuff[book.Stuff] : null;
        }

        //ThingDef
        public static bool IsExempted(this ThingDef weapon)
        {
            return !ModBaseHumanResources.RequireTrainingForSingleUseWeapons && !weapon.thingSetMakerTags.NullOrEmpty() && weapon.thingSetMakerTags.Any(tag => TechDefOf.WeaponsAlwaysBasic.thingSetMakerTags.Contains(tag));
        }

        public static bool NotReallyAWeapon(this ThingDef weapon)
        {
            return weapon.weaponTags.NullOrEmpty() || weapon.weaponTags.Any(tag => TechDefOf.WeaponsAlwaysBasic.weaponTags.Contains(tag));
        }
    }
} 
