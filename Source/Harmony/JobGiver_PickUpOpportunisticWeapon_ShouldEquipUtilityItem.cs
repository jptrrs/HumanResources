using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace HumanResources
{
    //Checks if pawn knows a utility item before equiping it via AI initiative.
    [HarmonyPatch(typeof(JobGiver_PickUpOpportunisticWeapon), "ShouldEquipUtilityItem", new Type[] { typeof(Thing), typeof(Pawn) })]
    public static class JobGiver_PickUpOpportunisticWeapon_ShouldEquipUtilityItem
    {
        public static bool Prefix(Thing thing, Pawn pawn)
        {
            //if (pawn.Faction?.IsPlayer == true && pawn.RaceProps.Humanlike && pawn.TryGetComp<CompKnowledge>() != null) return HarmonyPatches.CheckKnownWeapons(pawn, thing);
            if (pawn.TechBound()) return HarmonyPatches.CheckKnownWeapons(pawn, thing);
            else return true;
        }
    }
}
