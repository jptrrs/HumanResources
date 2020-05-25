using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HumanResources
{
    //Checks if pawn knows a weapon before equiping it, 3/3
    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders", new Type[] { typeof(Vector3), typeof(Pawn), typeof(List<FloatMenuOption>) })]
    class FloatMenuMakerMap_AddHumanlikeOrders
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);
            ThingWithComps equipment = null;
            List<Thing> thingList = c.GetThingList(pawn.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i].TryGetComp<CompEquippable>() != null)
                {
                    equipment = (ThingWithComps)thingList[i];
                    break;
                }
            }
            if (equipment != null && equipment.def.IsWeapon && !HarmonyPatches.CheckKnownWeapons(pawn, equipment))
            {
                string labelShort = equipment.LabelShort;
                string flavoredExplanation = ModBaseHumanResources.unlocked.weapons.Contains(equipment.def) ? "UnknownWeapon".Translate(pawn) : "EvilWeapon".Translate(pawn);
                FloatMenuOption item = new FloatMenuOption("CannotEquip".Translate(labelShort) + " (" + flavoredExplanation + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                opts.RemoveAt(opts.FindIndex(x => x.Label.Contains("Equip".Translate(labelShort))));
                opts.Add(item);
            }
        }
    }
}
