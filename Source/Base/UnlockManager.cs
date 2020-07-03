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
        public bool knowAllStartingWeapons;
        public IEnumerable<ThingDef> startingWeapons;
        public IEnumerable<ResearchProjectDef> startingTechs;
        public Dictionary<ResearchProjectDef, ThingDef> stuffByTech = new Dictionary<ResearchProjectDef, ThingDef>();
        public Dictionary<ThingDef, ResearchProjectDef> techByStuff = new Dictionary<ThingDef, ResearchProjectDef>();
        private static FieldInfo ScenPartThingDefInfo = AccessTools.Field(typeof(ScenPart_ThingCount), "thingDef");
        private static FieldInfo ScenPartResearchDefInfo = AccessTools.Field(typeof(ScenPart_StartingResearch), "project");

        public void ExposeData()
        {
            Scribe_Collections.Look<ThingDef>(ref weapons, "unlockedWeapons", LookMode.Deep, new object[0]);
            if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.ResolvingCrossRefs || Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RecacheUnlockedWeapons();
                RegisterStartingResources();
            }
        }

        public void RecacheUnlockedWeapons()
        {
            weapons.Clear();
            UnlockWeapons(ModBaseHumanResources.SimpleWeapons);
            foreach (ResearchProjectDef tech in DefDatabase<ResearchProjectDef>.AllDefs.Where(x => x.IsFinished))
            {
                UnlockWeapons(tech.UnlockedWeapons());
            }
            if (Prefs.LogVerbose) Log.Message("[HumanResources] Unlocked weapons recached: " + ModBaseHumanResources.UniversalWeapons.ToStringSafeEnumerable());
        }

        public void UnlockWeapons(IEnumerable<ThingDef> newWeapons)
        {
            weapons.AddRange(newWeapons.Except(weapons).Where(x => x.IsWeapon));
        }

        public void RegisterStartingResources()
        {
            knowAllStartingWeapons = Find.Scenario.AllParts.Where(x => x.def.defName == "Rule_knowAllStartingWeapons").Any();
            startingWeapons = Find.Scenario.AllParts.Where(x => typeof(ScenPart_ThingCount).IsAssignableFrom(x.GetType())).Cast<ScenPart_ThingCount>().Select(x => (ThingDef)ScenPartThingDefInfo.GetValue(x)).Where(x => x.IsWeapon).Except(ModBaseHumanResources.UniversalWeapons).ToList();
            if (Prefs.LogVerbose) Log.Message("[HumanResources] Found " + startingWeapons.Count() + " starting scenario weapons: " + startingWeapons.Select(x => x.label).ToStringSafeEnumerable());
            startingTechs = Find.Scenario.AllParts.Where(x => typeof(ScenPart_StartingResearch).IsAssignableFrom(x.GetType())).Cast<ScenPart_StartingResearch>().Select(x => (ResearchProjectDef)ScenPartResearchDefInfo.GetValue(x));
            if (Prefs.LogVerbose) Log.Message("[HumanResources] Found " + startingTechs.Count() + " starting scenario techs: " + startingTechs.Select(x => x.label).ToStringSafeEnumerable());
            if (!Prefs.LogVerbose) Log.Message("[HumanResources] Found " + startingWeapons.Count() + " weapons and " + startingTechs.Count() + " techs on the starting scenario.");
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
