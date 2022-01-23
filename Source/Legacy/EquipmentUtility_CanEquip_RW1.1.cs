using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace HumanResources
{
    //Changed in RW 1.2
    //Checks if the pawn knows a weapon before equiping it.
    [HarmonyPatch(typeof(EquipmentUtility), "CanEquip", new Type[] { typeof(Thing), typeof(Pawn), typeof(string) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out})]
    public static class EquipmentUtility_CanEquip
    {
        public static bool Prefix(Thing thing, Pawn pawn, out string cantReason)
        {
            ThingWithComps equipment = null;
            if (thing.TryGetComp<CompEquippable>() != null) equipment = thing as ThingWithComps;
            if (pawn.Faction != null && pawn.Faction.IsPlayer && pawn.RaceProps.Humanlike && equipment != null && equipment.def.IsWeapon && !HarmonyPatches.CheckKnownWeapons(pawn, equipment))
            {
                cantReason = ModBaseHumanResources.unlocked.weapons.Contains(equipment.def) ? "UnknownWeapon".Translate() : "EvilWeapon".Translate();
                return false;
            }
            cantReason = null;
            return true;
        }
    }
}
