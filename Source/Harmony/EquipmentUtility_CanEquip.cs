using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace HumanResources
{
    //Checks if the pawn knows a weapon before equiping it. (1.3 only)
    [HarmonyPatch(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip), new Type[] { typeof(Thing), typeof(Pawn), typeof(string), typeof(bool) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
    public static class EquipmentUtility_CanEquip
    {
        public static bool Prefix(Thing thing, Pawn pawn, out string cantReason)
        {
            ThingWithComps equipment = null;
            if (thing.TryGetComp<CompEquippable>() != null) equipment = thing as ThingWithComps;
            if (pawn.Faction?.IsPlayer == true && pawn.RaceProps.Humanlike && equipment?.def.IsWeapon == true && !HarmonyPatches.CheckKnownWeapons(pawn, equipment))
            {
                cantReason = ModBaseHumanResources.unlocked.weapons.Contains(equipment.def) ? "UnknownWeapon".Translate() : "EvilWeapon".Translate();
                return false;
            }
            cantReason = null;
            return true;
        }
    }
}
