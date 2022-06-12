using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace HumanResources
{
    //Changed in RW 1.3 (TechBound now includes slaves)
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

        public static bool IsValidBook(this Thing item)
        {
            return item.def == TechDefOf.TechBook && item.Stuff != null && item.Stuff.IsWithinCategory(TechDefOf.Knowledge);
        }

        //ThingDef
        public static bool IsEasy(this ThingDef weapon)
        {
            return weapon.weaponTags.NullOrEmpty() || weapon.weaponTags.Any(tag => TechDefOf.EasyWeapons.weaponTags.Contains(tag)) || !(weapon.IsWithinCategory(TechDefOf.WeaponsMelee) || weapon.IsWithinCategory(TechDefOf.WeaponsRanged));
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

        //BookStore
        public static Toil DepositHauledBook(this Building_BookStore shelf)
        {
            var toil = new Toil();
            Pawn actor = toil.actor;
            toil.initAction = delegate
            {
                //Log.Message("DepositHauledBook started");

                Thing book = actor.carryTracker.CarriedThing;
                if (actor.carryTracker.CarriedThing == null)
                {
                    Log.Error($"[HumanResources] {actor} tried to place a book on shelf but is not hauling anything.");
                    return;
                }
                if (!shelf.Accepts(book))
                {
                    Log.Error($"[HumanResources] {actor} tried to place a book in {shelf}, but it won't accept it.");
                    return;
                }
                bool flag = false;
                if (book.holdingOwner != null)
                {
                    //Log.Message("DepositHauledBook case 1");
                    book.holdingOwner.TryTransferToContainer(book, shelf.TryGetInnerInteractableThingOwner(), book.stackCount, true);
                    flag = true;
                }
                else
                {
                    //Log.Message("DepositHauledBook case 2");
                    flag = shelf.TryGetInnerInteractableThingOwner().TryAdd(book, true);
                }
                actor.carryTracker.innerContainer.Remove(book);
                actor.jobs.EndCurrentJob(JobCondition.Succeeded, true, true);
            };
            toil.FailOn(() => shelf == null);
            return toil;
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
