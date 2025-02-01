﻿using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace HumanResources
{
    //Changed in RW 1.4
    public static class SimpleSidearms_Patches
    {
        public static void Execute(Harmony instance)
        {
            Type StatCalculatorType = AccessTools.TypeByName("PeteTimesSix.SimpleSidearms.Utilities.StatCalculator");
            instance.Patch(AccessTools.Method(StatCalculatorType, "canCarrySidearmInstance", new Type[] { typeof(ThingWithComps), typeof(Pawn), typeof(string).MakeByRefType() }),
                new HarmonyMethod(typeof(SimpleSidearms_Patches), nameof(canCarrySidearmÌnstance_Prefix)), null, null);

            Type WeaponAssingmentType = AccessTools.TypeByName("PeteTimesSix.SimpleSidearms.Utilities.WeaponAssingment");
            instance.Patch(AccessTools.Method(WeaponAssingmentType, "equipSpecificWeapon"),
                new HarmonyMethod(typeof(SimpleSidearms_Patches), nameof(equipSpecificWeapon_Prefix)), null, null);
        }

        public static bool canCarrySidearmÌnstance_Prefix(ThingWithComps sidearmThing, Pawn pawn, out string errString)
        {
            errString = ModBaseHumanResources.unlocked.weapons.Contains(sidearmThing.def) ? "UnknownWeapon".Translate() : "EvilWeapon".Translate();
            if (pawn.RaceProps.Humanlike && pawn.Faction != null && pawn.Faction.IsPlayer && pawn.TryGetComp<CompKnowledge>() != null) return HarmonyPatches.CheckKnownWeapons(pawn, sidearmThing);
            else return true;
        }

        public static bool equipSpecificWeapon_Prefix(Pawn pawn, ThingWithComps weapon)
        {
            //Thanks to Andy Brenneke for figuring out sometimes SS equips null weapons!
            if (weapon != null && pawn.RaceProps.Humanlike && pawn.Faction != null && pawn.Faction.IsPlayer && pawn.TryGetComp<CompKnowledge>() != null) return HarmonyPatches.CheckKnownWeapons(pawn, weapon);
            else return true;
        }
    }
}
