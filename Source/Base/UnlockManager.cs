using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace HumanResources
{
    public class UnlockManager : IExposable
    {
        //tracking archived technologies
        public enum backupState { digital, physical, both };
        public Dictionary<ResearchProjectDef, backupState> _techsArchived = new Dictionary<ResearchProjectDef, backupState>();
        public Dictionary<ResearchProjectDef, backupState> TechsArchived => _techsArchived;
        public int libraryFreeSpace;
        public int discoveredCount => stuffByTech.Keys.Where(x => x.IsFinished).EnumerableCount();

        //tracking techology bases
        public Dictionary<ResearchProjectDef, ThingDef> stuffByTech = new Dictionary<ResearchProjectDef, ThingDef>();
        public IEnumerable<ResearchProjectDef> scenarioTechs, factionTechs;
        
        //tracking weapons
        public List<ThingDef> weapons = new List<ThingDef>();
        public bool knowAllStartingWeapons;
        public IEnumerable<ThingDef> startingWeapons;

        //reflection info
        private static FieldInfo 
            ScenPartThingDefInfo = AccessTools.Field(typeof(ScenPart_ThingCount), "thingDef"),
            ScenPartResearchDefInfo = AccessTools.Field(typeof(ScenPart_StartingResearch), "project");

        //Research speed boost by books in store using geometric progression 
        private const float decay = 0.02f;
        private static float ratio = 1 / (1 + decay);
        private const int semiMaxBuff = 10; // research speed max buff for books is 20% 
        public int total => stuffByTech.Count;
        private float geoSum => (float)(Math.Pow(ratio, total) - 1) / (ratio - 1);
        private float quota => semiMaxBuff / geoSum;
        private float linear => semiMaxBuff / total;

        public void Archive(ResearchProjectDef tech, bool hardCopy)
        {
            if (!_techsArchived.ContainsKey(tech))
            {
                _techsArchived.Add(tech, hardCopy ? backupState.physical : backupState.digital);
                if (Prefs.LogVerbose) Log.Message($"[HumanResoruces] Added tech {tech} as {_techsArchived[tech]}");
            }
            else if (_techsArchived[tech] != backupState.both)
            {
                bool currentlyHard = _techsArchived[tech] == backupState.physical;
                if (hardCopy != currentlyHard)
                {
                    _techsArchived[tech] = backupState.both;
                }
            }
            else Log.Error($"[HumanResoruces] Tried to archive {tech}, but both physical and digital copies are already accounted for.");
        }

        public float BookResearchIncrement(int count)
        {
            float term = (float)Math.Pow(ratio, count - 1);
            float sum = quota * (term * ratio - 1) / (ratio - 1);
            float result = sum + linear;
            return result / 100;
        }

        public void ExposeData()
        {
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

        public void RegisterStartingResources()
        {
            knowAllStartingWeapons = Find.Scenario.AllParts.Any(x => x.def.defName == "Rule_knowAllStartingWeapons");
            startingWeapons = Find.Scenario.AllParts.Where(x => typeof(ScenPart_ThingCount).IsAssignableFrom(x.GetType())).Cast<ScenPart_ThingCount>().Select(x => (ThingDef)ScenPartThingDefInfo.GetValue(x)).Where(x => x.IsWeapon).Except(ModBaseHumanResources.UniversalWeapons).ToList();
            scenarioTechs = Find.Scenario.AllParts.Where(x => typeof(ScenPart_StartingResearch).IsAssignableFrom(x.GetType())).Cast<ScenPart_StartingResearch>().Select(x => (ResearchProjectDef)ScenPartResearchDefInfo.GetValue(x));
            Faction playerFaction = Find.FactionManager.OfPlayer;
            if (playerFaction != null && !playerFaction.def.startingResearchTags.NullOrEmpty())
            {
                var tags = playerFaction.def.startingResearchTags;
                factionTechs = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(x => !x.tags.NullOrEmpty() && tags.Intersect(x.tags).Any());
            }
            if (Prefs.LogVerbose)
            {
                if (!startingWeapons.EnumerableNullOrEmpty()) Log.Message($"[HumanResources] Found {startingWeapons.Count()} starting scenario weapons: {startingWeapons.Select(x => x.label).ToStringSafeEnumerable()}");
                if (!scenarioTechs.EnumerableNullOrEmpty()) Log.Message($"[HumanResources] Found {scenarioTechs.Count()} starting scenario techs: {scenarioTechs.Select(x => x.label).ToStringSafeEnumerable()}");
                if (!factionTechs.EnumerableNullOrEmpty()) Log.Message($"[HumanResources] Found {factionTechs.Count()} starting techs for player faction ({playerFaction}): {factionTechs.Select(x => x.label).ToStringSafeEnumerable()}");
            }
            else
            {
                int startingWeaponsTxt = startingWeapons.EnumerableNullOrEmpty() ? 0 : startingWeapons.Count();
                int scenarioTechsTxt = scenarioTechs.EnumerableNullOrEmpty() ? 0 : scenarioTechs.Count();
                int factionTechsTxt = factionTechs.EnumerableNullOrEmpty() ? 0 : factionTechs.Count();
                Log.Message($"[HumanResources] Found {startingWeaponsTxt} weapons, {scenarioTechsTxt} starting techs from the scenario and {factionTechsTxt} from the player faction.");
            }
        }

        public void UnlockWeapons(IEnumerable<ThingDef> newWeapons)
        {
            weapons.AddRange(newWeapons.Except(weapons).Where(x => x.IsWeapon));
        }

    }
}
