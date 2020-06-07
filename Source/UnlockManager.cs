using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HumanResources
{
    public class UnlockManager : IExposable
    {       
        public List<ThingDef> weapons = new List<ThingDef>();
        public IEnumerable<ThingDef> startingWeapons;
        public Dictionary<ResearchProjectDef, ThingDef> stuffByTech = new Dictionary<ResearchProjectDef, ThingDef>();
        public Dictionary<ThingDef, ResearchProjectDef> techByStuff = new Dictionary<ThingDef, ResearchProjectDef>();
        

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
                if (Prefs.LogVerbose) Log.Message("[HumanResources] Unlocked weapons recached: " + ModBaseHumanResources.UniversalWeapons.ToStringSafeEnumerable());
            }
        }

        public void UnlockWeapons(IEnumerable<ThingDef> newWeapons)
        {
            weapons.AddRange(newWeapons.Except(weapons).Where(x => x.IsWeapon));
        }

        public void RegisterStartingWeapons()
        {
            FieldInfo ScenPartThingDefInfo = AccessTools.Field(typeof(ScenPart_ThingCount), "thingDef");
            startingWeapons = Find.Scenario.AllParts.Where(x => typeof(ScenPart_ThingCount).IsAssignableFrom(x.GetType())).Cast<ScenPart_ThingCount>().Select(x => (ThingDef)ScenPartThingDefInfo.GetValue(x)).Where(x => x.IsWeapon).Except(ModBaseHumanResources.UniversalWeapons).ToList();
            Log.Message("[HumanResources] Starting scenario weapons: " + startingWeapons.Count() + " " + startingWeapons.Select(x => x.label).ToStringSafeEnumerable());
        }

        private const float decay = 0.02f;
        private static float ratio = 1 / (1 + decay);
        private const int semiMaxBuff = 10; // research speed max buff for books is 20% 
        private int total => techByStuff.Count;
        private float geoSum => (float) (Math.Pow(ratio, total)-1) / (ratio - 1);
        private float quota => semiMaxBuff / geoSum;
        private float linear => semiMaxBuff / total;

        public float BookResearchIncrement(int count)
        {
            float term = (float)Math.Pow(ratio, count - 1);
            float sum = quota * (term * ratio - 1) / (ratio - 1);
            float result = sum + linear;
            return result/100;
        }
    }
}
