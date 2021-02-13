using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HumanResources
{
    using static ModBaseHumanResources;

    public static class Extensions
	{
        //Bill
        public static bool Allows(this Bill bill, IEnumerable<ResearchProjectDef> homework)
        {
            var textBooks = homework.Select(x => unlocked.stuffByTech[x]);
            bool result = bill.ingredientFilter.AllowedThingDefs.Intersect(textBooks).Any();
            return result;
        }

        public static bool Allows(this Bill bill, ResearchProjectDef tech)
        {
            return bill.ingredientFilter.Allows(unlocked.stuffByTech[tech]);
        }

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
            return (book.Stuff != null && book.Stuff.IsWithinCategory(TechDefOf.Knowledge)) ? unlocked.stuffByTech.ReverseLookup(book.Stuff) : null;
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

        //Dictionary
        public static TKey ReverseLookup<TKey, TValue>(this IDictionary<TKey, TValue> source, TValue sample)
        {
            if (source.Values.Contains(sample))
            {
                return source.FirstOrDefault(x => x.Value.Equals(sample)).Key;
            }
            return default(TKey);
        }
    }
} 
