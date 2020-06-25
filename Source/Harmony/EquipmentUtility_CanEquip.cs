using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace HumanResources
{
    //Checks if pawn knows a weapon before equiping it via caravan loadout & other means.
    [HarmonyPatch(typeof(EquipmentUtility), "CanEquip", new Type[] { typeof(Thing), typeof(Pawn), typeof(string) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out})]
    public static class EquipmentUtility_CanEquip
    {
        public static bool Prefix(Thing thing, Pawn pawn, out string cantReason)
        {
            cantReason = "UnknownWeapon".Translate(pawn);
            if (pawn.RaceProps.Humanlike && pawn.TryGetComp<CompKnowledge>() != null) return HarmonyPatches.CheckKnownWeapons(pawn, thing);
            else return true;
        }
    }
}
