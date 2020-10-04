using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace HumanResources
{
    public static class SimpleSidearms_Patches
    {
        public static void Execute(Harmony instance)
        {
            Type StatCalculatorType = AccessTools.TypeByName("SimpleSidearms.utilities.StatCalculator");
            MethodBase canCarrySidearmBase = AccessTools.Method(StatCalculatorType, "canCarrySidearmInstance", new Type[] { typeof(ThingWithComps), typeof(Pawn), typeof(string).MakeByRefType() });
            instance.Patch(AccessTools.Method(StatCalculatorType, "canCarrySidearmInstance", new Type[] { typeof(ThingWithComps), typeof(Pawn), typeof(string).MakeByRefType() }),
                new HarmonyMethod(typeof(SimpleSidearms_Patches), nameof(canCarrySidearmÌnstance_Prefix)), null, null);
            
            Type WeaponAssingmentType = AccessTools.TypeByName("SimpleSidearms.utilities.WeaponAssingment");
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
            if (pawn.RaceProps.Humanlike && pawn.Faction != null && pawn.Faction.IsPlayer && pawn.TryGetComp<CompKnowledge>() != null) return HarmonyPatches.CheckKnownWeapons(pawn, weapon);
            else return true;
        }
    }
}
