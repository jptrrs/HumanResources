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
        }

        public static bool canCarrySidearmÌnstance_Prefix(ThingWithComps sidearmThing, Pawn pawn, out string errString)
        {
            Log.Warning("kicking in");
            errString = ModBaseHumanResources.unlocked.weapons.Contains(sidearmThing.def) ? "UnknownWeapon".Translate(pawn) : "EvilWeapon".Translate(pawn);
            if (pawn.RaceProps.Humanlike && pawn.Faction.IsPlayer && pawn.TryGetComp<CompKnowledge>() != null) return HarmonyPatches.CheckKnownWeapons(pawn, sidearmThing);
            else return true;
        }
    }
}
