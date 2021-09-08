using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace HumanResources
{
    //Checks if pawn knows a weapon before equiping it from inventory.
    [HarmonyPatch(typeof(ThingOwner), "TryAddOrTransfer", new Type[] { typeof(Thing), typeof(bool) })]
    public static class ThingOwner_TryAddOrTransfer
    {
        private static FieldInfo pawnInfo = AccessTools.Field(typeof(Pawn_EquipmentTracker), "pawn");

        public static bool Prefix(Thing item, IThingHolder ___owner)
        {
            if (item.def.IsWeapon && ___owner is Pawn_EquipmentTracker gear)
            {
                Pawn pawn = (Pawn)pawnInfo.GetValue(gear);
                return HarmonyPatches.CheckKnownWeapons(pawn, item);
            }
            return true;
        }
    }
}
