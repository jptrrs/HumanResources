using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace HumanResources
{
    //Checks if pawn knows a weapon before equiping it via AI initiative.
    [HarmonyPatch(typeof(JobGiver_PickUpOpportunisticWeapon), "ShouldEquipWeapon", new Type[] { typeof(Thing), typeof(Pawn) })]
    public static class JobGiver_PickUpOpportunisticWeapon_ShouldEquipWeapon
    {
        public static bool Prefix(Thing newWep, Pawn pawn)
        {
            //if (pawn.Faction?.IsPlayer == true && pawn.RaceProps.Humanlike && pawn.TryGetComp<CompKnowledge>() != null) return HarmonyPatches.CheckKnownWeapons(pawn, newWep);
            if (pawn.TechBound()) return HarmonyPatches.CheckKnownWeapons(pawn, newWep);
            else return true;
        }
    }
}
