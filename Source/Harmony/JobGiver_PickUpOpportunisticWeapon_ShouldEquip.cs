using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace HumanResources
{
    //Checks if pawn knows a weapon before equiping it, 2/3
    [HarmonyPatch(typeof(JobGiver_PickUpOpportunisticWeapon), "ShouldEquip", new Type[] { typeof(Thing), typeof(Pawn) })]
    public static class JobGiver_PickUpOpportunisticWeapon_ShouldEquip
    {
        public static bool Prefix(Thing newWep, Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike && pawn.Faction.IsPlayer && pawn.TryGetComp<CompKnowledge>() != null) return HarmonyPatches.CheckKnownWeapons(pawn, newWep);
            else return true;
        }
    }
}
