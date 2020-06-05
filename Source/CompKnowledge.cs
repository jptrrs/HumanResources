using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HumanResources
{
    public class CompKnowledge : ThingComp
    {
        public Dictionary<ResearchProjectDef,float> expertise;
        public List<ResearchProjectDef> HomeWork = new List<ResearchProjectDef>();
        public List<ThingDef> proficientPlants;
        public List<ThingDef> proficientWeapons;
        protected static bool isFighter = false;
        protected static bool isShooter = false;
        protected static TechLevel startingTechLevel;
        public List<ThingDef> knownWeapons
        {
            get
            {
                return proficientWeapons.Concat(ModBaseHumanResources.UniversalWeapons).ToList();
            }
        }
        public List<ThingDef> knownPlants
        {
            get
            {
                return proficientPlants.Concat(ModBaseHumanResources.UniversalCrops).ToList();
            }
        }

        private Pawn pawn
        {
            get
            {
                if (parent is Pawn p)
                {
                    return p;
                }
                else
                {
                    Log.Error("[HumanResources] " + parent.Label.CapitalizeFirst() + " is trying to pose as human, but his disguise can't fool us!");
                    return null;
                }
            }
        }

        public static IEnumerable<ResearchProjectDef> GetExpertiseDefsFor(Pawn pawn)
        {
            FactionDef faction = pawn.Faction?.def ?? pawn.kindDef.defaultFactionType;
            startingTechLevel = faction?.techLevel ?? 0;
            if (startingTechLevel == 0) startingTechLevel = InferTechLevelfromBG(pawn);
            SkillRecord highestSkillRecord = pawn.skills.skills.Aggregate((l, r) => l.levelInt > r.levelInt ? l : r);
            SkillDef highestSkill = highestSkillRecord.def;
            SkillDef secondSkill = pawn.skills.skills.Except(highestSkillRecord).Aggregate((l, r) => l.levelInt >= r.levelInt ? l : r).def;
            isFighter = highestSkill == SkillDefOf.Melee;
            isShooter = highestSkill == SkillDefOf.Shooting;
            int fighterHandicap = (isFighter | isShooter) ? 1 : 0;
            int oldBonus = pawn.ageTracker.AgeBiologicalYears > pawn.RaceProps.lifeExpectancy / 2 ? 1 : 0;
            int minSlots = startingTechLevel > TechLevel.Medieval ? 1 : oldBonus;
            int slots = Mathf.Max(minSlots, FactionExpertiseRange(startingTechLevel) - (4 - pawn.ageTracker.CurLifeStageIndex) + oldBonus - fighterHandicap);
            if (slots == 0)
            {
                if (Prefs.LogVerbose) Log.Warning("GetExpertiseDefsFor "+pawn+": no slots, returning null. StartingTechLevel is "+startingTechLevel+", CurLifeStageIndex is "+ pawn.ageTracker.CurLifeStageIndex+", fighterHandicap is "+ fighterHandicap);
                return null;
            }
            bool guru = startingTechLevel < TechLevel.Archotech && highestSkill == SkillDefOf.Intellectual && highestSkillRecord.Level >= Rand.Range(7, 10);
            var filtered = Extension_Research.SkillsByTech.Where(e => e.Key.techLevel.Equals(startingTechLevel));
            if (Prefs.LogVerbose) Log.Warning("GetExpertiseDefsFor " + pawn + ", techLevel is " + startingTechLevel);
            int pass = 0;
            List<KeyValuePair<ResearchProjectDef, List<SkillDef>>> result = new List<KeyValuePair<ResearchProjectDef, List<SkillDef>>>();
            if (guru) startingTechLevel++;
            while (result.Count() < slots)
            {
                var remaining = filtered.Except(result);
                if (remaining.EnumerableNullOrEmpty()) break;
                if (remaining.Any(e => e.Value.Contains(highestSkill)))
                {
                    result.Add(remaining.RandomElementByWeight(entry => TechLikelihoodForSkill(pawn, entry.Value, highestSkill, slots, pass)));
                }
                else if (remaining.Any(e => e.Value.Contains(secondSkill)))
                {
                    result.Add(remaining.RandomElementByWeight(entry => TechLikelihoodForSkill(pawn, entry.Value, secondSkill, slots, pass)));
                }
                else if (remaining.Any())
                {
                    result.Add(remaining.RandomElement());
                }
                pass++;
                if ((guru && pass == 1) | result.NullOrEmpty()) startingTechLevel--;
                if (startingTechLevel == 0) break;
            }
            if (!result.NullOrEmpty())
            {
                return result.Select(e => e.Key).ToList();
            }
            Log.Warning("Couldn't calculate any expertise for " + pawn);
            return null;
        }

        private static TechLevel InferTechLevelfromBG(Pawn pawn)
        {
            Log.Warning("InferTechLevelfromBG starting...");
            if (pawn.story != null)
            {
                string child = (pawn.story.childhood.title + " " + pawn.story.childhood.baseDesc).ToLower();
                bool isAdult = pawn.story.adulthood != null;
                string adult = isAdult ? (pawn.story.adulthood.title + " " + pawn.story.adulthood.baseDesc).ToLower() : "";
                var spacerHints = new List<string> { "glitterworld", "space", "imperial", "robot", "cryptosleep", "star system" };
                foreach (string word in spacerHints)
                {
                    if (child.Contains(word) | (isAdult && adult.Contains(word))) return TechLevel.Spacer;
                }
                var industrialHints = new List<string> { "midworld", " industrial", "urbworld" }; //leading space on " industrial" excludes "pre-industrial"
                foreach (string word in industrialHints)
                {
                    if (child.Contains(word) | (isAdult && adult.Contains(word))) return TechLevel.Industrial;
                }
                var medievalHints = new List<string> { "medieval", "monastery", "court" };
                foreach (string word in medievalHints)
                {
                    if (child.Contains(word) | (isAdult && adult.Contains(word))) return TechLevel.Medieval;
                }
                if ((!pawn.story.childhood.spawnCategories.NullOrEmpty() && pawn.story.childhood.spawnCategories.Contains("Tribal")) | (isAdult && !pawn.story.adulthood.spawnCategories.NullOrEmpty() && pawn.story.adulthood.spawnCategories.Contains("Tribal"))) 
                { 
                    return TechLevel.Neolithic; 
                }
                else
                {
                    var tribalHints = new List<string> { "tribe", "tribal", "digger" };
                    foreach (string word in tribalHints)
                    {
                        if (child.Contains(word) | (isAdult && adult.Contains(word))) return TechLevel.Neolithic;
                    }
                }
            }
            return TechLevel.Industrial;
        }

        public void AcquireExpertise()
        {
            if (expertise == null)
            {
                if (Prefs.LogVerbose) Log.Warning("aquiring expertise for " + pawn+ "...");
                var acquiredExpertise = GetExpertiseDefsFor(pawn);
                if (!acquiredExpertise.EnumerableNullOrEmpty())
                {
                    expertise = acquiredExpertise.ToDictionary(x => x, x => 1f);
                    if (Prefs.LogVerbose) Log.Message("...known techs: " + expertise.Count());
                }
                else
                {
                    Log.Warning("[HumanResources] "+pawn+" spawned without acquiring any expertise.");
                }
                AcquireWeaponKnowledge();
                if (Prefs.LogVerbose) Log.Message("...known weapons: " + proficientWeapons.Count());
                AcquirePlantKnowledge();
                if (Prefs.LogVerbose) Log.Message("...known plants: " + proficientPlants.Count());
            }
        }

        public void AcquirePlantKnowledge()
        {
            if (proficientPlants == null)
            {
                proficientPlants = new List<ThingDef>();
                //proficientPlants.AddRange(ModBaseHumanResources.UniversalCrops);
                if (!expertise.EnumerableNullOrEmpty()) foreach (ResearchProjectDef tech in expertise.Keys) LearnCrops(tech);
            }
        }

        public void AcquireWeaponKnowledge()
        {
            if (proficientWeapons == null)
            {
                proficientWeapons = new List<ThingDef>();
                if (!expertise.EnumerableNullOrEmpty()) foreach (ResearchProjectDef tech in expertise.Keys) LearnWeapons(tech);
                if (isFighter)
                {
                    if (Prefs.LogVerbose) Log.Warning("extra melee weapons for " + pawn + ", techLevel is "+startingTechLevel+"...");
                    foreach (ResearchProjectDef tech in DefDatabase<ResearchProjectDef>.AllDefs.Where(x => x.techLevel == startingTechLevel))
                    {
                        var weapons = tech.UnlockedWeapons().Where(x => x.IsMeleeWeapon);
                        if (weapons.Any()) foreach (ThingDef w in weapons)
                        {
                            proficientWeapons.Add(w);
                            if (Prefs.LogVerbose) Log.Message("added " + w);
                        }
                    }
                }
                if (isShooter)
                {
                    if (Prefs.LogVerbose) Log.Warning("extra ranged weapons for " + pawn + ", techLevel is "+startingTechLevel+"...");
                    foreach (ResearchProjectDef tech in DefDatabase<ResearchProjectDef>.AllDefs.Where(x => x.techLevel == startingTechLevel))
                    {
                        var weapons = tech.UnlockedWeapons().Where(x => x.IsRangedWeapon);
                        if (weapons.Any()) foreach (ThingDef w in weapons)
                        {
                            proficientWeapons.Add(w);
                            if (Prefs.LogVerbose) Log.Message("added " + w);
                        }
                    }
                }
                if (pawn.Faction != null && !pawn.Faction.IsPlayer && pawn.equipment.HasAnything())
                {
                    ThingWithComps weapon = pawn.equipment.Primary;
                    if (!knownWeapons.Contains(weapon.def))
                    {
                        proficientWeapons.Add(weapon.def);
                    }
                }
            }
        }

        public void AssignHomework(IEnumerable<ResearchProjectDef> studyMaterial)
        {
            if (Prefs.LogVerbose) Log.Message("Assigning homework for " + pawn + ", faction is " + pawn.Faction.IsPlayer + ", received " + studyMaterial.Count() + "projects, homework count is " + HomeWork.Count());
            if (pawn.Faction.IsPlayer)
            {
                var expertiseKeys = from x in expertise
                                    where x.Value >= 1f
                                    select x.Key;
                var available = studyMaterial.Except(expertiseKeys).Except(HomeWork);
                if (!available.Any())
                {
                    JobFailReason.Is("AlreadyKnowsTheWholeLibrary".Translate(pawn), null);
                    return;
                }
                if (Prefs.LogVerbose) Log.Message("...Available projects: " + available.ToStringSafeEnumerable());
                var derived = available.Where(t => t.prerequisites != null && t.prerequisites.All(r => expertise.Keys.Contains(r)));
                var starters = available.Where(t => t.prerequisites.NullOrEmpty());
                if (!starters.Any() && !derived.Any())
                {
                    JobFailReason.Is("LacksFundamentalKnowledge".Translate(pawn), null);
                    return;
                }
                HomeWork.AddRange(starters.Concat(derived));
            }
        }

        public override void CompTickRare()
        {
            if (!HomeWork.NullOrEmpty())
            {
                var excess = HomeWork.Except(SelectedAnywhere);
                if (excess.Any())
                {
                    HomeWork.RemoveAll(x => excess.Contains(x));
                    if (Prefs.LogVerbose) Log.Message("Removing " + excess.Count() + " unassigned projects from" + pawn);
                }
            }
        }

        private static IEnumerable<ResearchProjectDef> SelectedAnywhere => Find.Maps.SelectMany(x => x.listerBuildings.AllBuildingsColonistOfClass<Building_WorkTable>()).SelectMany(x => x.billStack.Bills).Where(x => x.UsesKnowledge()).SelectMany(x => x.SelectedTech()).Distinct();

        public void ExposeData()
        {
            ((IExposable)pawn).ExposeData();
        }

        public void LearnCrops(ResearchProjectDef tech)
        {
            proficientPlants.AddRange(tech.UnlockedPlants());
            if (Prefs.LogVerbose) Log.Message(parent + " can cultivate the following plants: " + proficientPlants.ToStringSafeEnumerable());
        }

        public void LearnWeapons(ResearchProjectDef tech)
        {
            proficientWeapons.AddRange(tech.UnlockedWeapons());
            if (Prefs.LogVerbose) Log.Message(parent + " can use the following weapons: " + proficientWeapons.ToStringSafeEnumerable());
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.Saving && expertise != null)
            {
                var e = expertise.Where(x => x.Value > 1f).GetEnumerator();
                while (e.MoveNext())
                {
                    expertise[e.Current.Key] = 1f;
                }
            }
            Scribe_Collections.Look(ref expertise, "Expertise");
            Scribe_Collections.Look(ref proficientWeapons, "proficientWeapons");
            Scribe_Collections.Look(ref proficientPlants, "proficientPlants");
            if (Prefs.LogVerbose) Log.Message("PostExposeData for " + parent + ". proficientWeapons count is " + proficientWeapons.Count());
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            AcquireExpertise();
        }

        private static int FactionExpertiseRange(TechLevel level)
        {
            int i = 0;
            switch (level)
            {
                case TechLevel.Archotech:
                case TechLevel.Spacer:
                case TechLevel.Ultra:
                    i = 1;
                    break;
                case TechLevel.Industrial:
                    i = 2;
                    break;
                case TechLevel.Medieval:
                case TechLevel.Neolithic:
                    i = 3;
                    break;
                default:
                    i = 0;
                    break;
            }
            return i;
        }

        private static float TechLikelihoodForSkill(Pawn pawn, List<SkillDef> skills, SkillDef highestSkill, int slots, int pass)
        {
            List<SkillDef> unskilled = (from x in pawn.skills.skills
                                        where x.Level < 2
                                        select x.def).ToList();
            float chance = ((slots - pass) / slots)*100f;
            if (skills.Contains(highestSkill)) return chance;
            else if (skills.All(s => unskilled.Contains(s))) return (100f - chance) / 10;
            else return 100f - chance;
        }
    }
}
