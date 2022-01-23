using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace HumanResources
{
    //Changed in RW 1.3
    //Checks if the pawn knows a weapon before equiping it. (1.2 only)
    [HarmonyPatch(typeof(EquipmentUtility), "CanEquip_NewTmp", new Type[] { typeof(Thing), typeof(Pawn), typeof(string), typeof(bool) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
    public static class EquipmentUtility_CanEquip_NewTmp
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
