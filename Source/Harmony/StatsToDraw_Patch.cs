using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    using static ModBaseHumanResources;

    //Inserts new entries on a weapon's info report.
    [HarmonyPatch(typeof(StatsReportUtility), "StatsToDraw", new Type[] { typeof(Thing) })]
    public class StatsToDraw_Patch
    {
        private static int runs = 0;
        public static bool Prepare() //Needed because for some strange reason the method is patched twice.
        {
            if (runs > 1)
            {
                runs = 0;
                return false;
            }
            runs++;
            return true;
        }
        public static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> values, Thing thing)
        {
            foreach (StatDrawEntry entry in values)
            {
                yield return entry;
            }
            if (thing.def.IsWeapon)
            {
                string tech = TechTracker.FindTech(thing.def)?.Tech.LabelCap ?? "None".Translate();
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "WeaponAssociatedTech".Translate(), tech, "WeaponAssociatedTechDesc".Translate(), 10000, null, null, false);
                bool known = unlocked.weapons.Contains(thing.def);
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "WeaponKnown".Translate(), known.ToStringYesNo(), "WeaponKnownDesc".Translate(), 9999, null, null, false);
                bool free = SimpleWeapons.Contains(thing.def) || UniversalWeapons.Contains(thing.def) || thing.def.NotThatHard();
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "WeaponRequiresTraining".Translate(), (!free).ToStringYesNo(), "WeaponRequiresTrainingDesc".Translate(), 9998, null, null, false);
            }
        }
    }
}
