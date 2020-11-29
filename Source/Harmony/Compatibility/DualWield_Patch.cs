using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    class DualWield_Patch
    {
        public static void Execute(Harmony instance)
        {
            Type Type = AccessTools.TypeByName("DualWield.Harmony.FloatMenuMakerMap_AddHumanlikeOrders");
            instance.Patch(AccessTools.Method(Type, "GetEquipOffHandOption"),
                new HarmonyMethod(typeof(DualWield_Patch), nameof(GetEquipOffHandOption_Prefix)), null, null);
            instance.Patch(AccessTools.Method(Type, "Postfix"),
                null, new HarmonyMethod(typeof(DualWield_Patch), nameof(Postfix_Postfix)), null);
        }

        public static void Postfix_Postfix(ref List<FloatMenuOption> opts)
        {
            opts.RemoveAll(x => x == null);
        }

        public static bool GetEquipOffHandOption_Prefix(Pawn pawn, ThingWithComps equipment)
        {
            if (pawn.RaceProps.Humanlike && pawn.Faction != null && pawn.Faction.IsPlayer && pawn.TryGetComp<CompKnowledge>() != null) return HarmonyPatches.CheckKnownWeapons(pawn, equipment);
            else return true;
        }

    }
}
