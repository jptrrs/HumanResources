using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HumanResources
{
    public static class Extensions
    {
        //Bill
        public static bool Allows(this Bill bill, IEnumerable<ResearchProjectDef> homework)
        {
            var textBooks = homework.Select(x => TechTracker.FindTech(x).Stuff);
            bool result = bill.ingredientFilter.AllowedThingDefs.Intersect(textBooks).Any();
            return result;
        }

        public static bool Allows(this Bill bill, ResearchProjectDef tech)
        {
            return bill.ingredientFilter.Allows(TechTracker.FindTech(tech).Stuff);
        }

        //Map
        public static bool ServerAvailable(this Map map)
        {
            var servers = map.listerBuildings.AllBuildingsColonistOfDef(TechDefOf.NetworkServer);
            if (!servers.EnumerableNullOrEmpty())
                return servers.Any(x => x.TryGetComp<CompPowerTrader>().PowerOn);
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
            return (book.Stuff != null && book.Stuff.IsWithinCategory(TechDefOf.Knowledge)) ? TechTracker.FindTech(book.Stuff) : null;
        }

        //ThingDef
        public static bool IsEasy(this ThingDef weapon)
        {
            return weapon.weaponTags.NullOrEmpty() || weapon.weaponTags.Any(tag => TechDefOf.EasyWeapons.weaponTags.Contains(tag));
        }

        public static bool IsSingleUseWeapon(this ThingDef weapon)
        {
            return !weapon.thingSetMakerTags.NullOrEmpty() && weapon.thingSetMakerTags.Any(tag => TechDefOf.EasyWeapons.thingSetMakerTags.Contains(tag));
        }

        public static bool ExemptIfSingleUse(this ThingDef weapon)
        {
            return !ModBaseHumanResources.RequireTrainingForSingleUseWeapons && weapon.IsSingleUseWeapon();
        }

        public static bool NotThatHard(this ThingDef weapon)
        {
            return weapon.ExemptIfSingleUse() || weapon.IsEasy() || ModBaseHumanResources.MountedWeapons.Contains(weapon);
        }

        public static ThingDef GetTurretGun(this ThingDef thing)
        {
            return thing.building?.turretGunDef;
        }

        public static bool IsMannable(this ThingDef def)
        {
            return def.hasInteractionCell && def.HasComp(typeof(CompMannable));
        }

        ////Dictionary
        //public static TKey ReverseLookup<TKey, TValue>(this IDictionary<TKey, TValue> source, TValue sample)
        //{
        //    if (source.Values.Contains(sample))
        //    {
        //        return source.FirstOrDefault(x => x.Value.Equals(sample)).Key;
        //    }
        //    return default(TKey);
        //}
    }
}
