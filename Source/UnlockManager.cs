using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace HumanResources
{
    public class UnlockManager : IExposable
    {
        public List<ThingDef> weapons = new List<ThingDef>();

        public void ExposeData()
        {
            Scribe_Collections.Look<ThingDef>(ref weapons, "unlockedWeapons", LookMode.Deep, new object[0]);
            if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.ResolvingCrossRefs || Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RecacheUnlockedWeapons();
            }
        }

        public void RecacheUnlockedWeapons()
        {
            if (weapons.NullOrEmpty())
            {
                UnlockWeapons(ModBaseHumanResources.UniversalWeapons);
                foreach (ResearchProjectDef tech in DefDatabase<ResearchProjectDef>.AllDefs.Where(x => x.IsFinished))
                {
                    UnlockWeapons(tech.UnlockedWeapons());
                }
                Log.Warning("Unlocked weapons recached: " + ModBaseHumanResources.UniversalWeapons.ToStringSafeEnumerable());
            }
        }

        public void UnlockWeapons(IEnumerable<ThingDef> newWeapons)
        {
            weapons.AddRange(newWeapons.Except(weapons).Where(x => x.IsWeapon));
        }
    }
}
