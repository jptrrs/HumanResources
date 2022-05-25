using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    //Checks if the pawn knows a artillery piece before manning it
    [HarmonyPatch(typeof(CompMannable), nameof(CompMannable.CompFloatMenuOptions), new Type[] { typeof(Pawn) })]
    public static class CompMannable_CompFloatMenuOptions
    {
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> options, Pawn pawn, ThingWithComps ___parent)
        {
            if (___parent is Building_TurretGun turret)
            {
                ThingDef gundDef = turret.gun.def;
                foreach (var entry in options)
                {
                    if (entry.action != null && entry.orderInPriority == 0 && pawn.TechBound() && !HarmonyPatches.CheckKnownWeapons(pawn, gundDef))
                    {
                        yield return new FloatMenuOption($"{"CannotManThing".Translate(___parent.LabelShort, ___parent)} ({"EvilWeapon".Translate()})", null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0)
                        {
                            Disabled = true,
                        };
                    }
                    else yield return entry;
                }
            }
            else foreach (var entry in options) yield return entry;
        }
    }
}
