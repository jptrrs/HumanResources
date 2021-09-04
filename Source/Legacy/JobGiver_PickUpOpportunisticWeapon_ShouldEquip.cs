using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace HumanResources
{
    //Replaced by ShouldEquipUtilityItem & ShouldEquipWeapon in RW 1.3
    //Checks if pawn knows a weapon before equiping it via AI initiative. (Up to RW 1.2)
    [HarmonyPatch(typeof(JobGiver_PickUpOpportunisticWeapon), "ShouldEquip", new Type[] { typeof(Thing), typeof(Pawn) })]
    public static class JobGiver_PickUpOpportunisticWeapon_ShouldEquip
    {
        public static bool Prefix(Thing newWep, Pawn pawn)
        {
            if (pawn.Faction != null && pawn.Faction.IsPlayer && pawn.RaceProps.Humanlike && pawn.TryGetComp<CompKnowledge>() != null) return HarmonyPatches.CheckKnownWeapons(pawn, newWep);
            else return true;
        }
    }
}
