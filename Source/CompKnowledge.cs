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
        //public List<ResearchProjectDef> expertise;
        public Dictionary<ResearchProjectDef,float> expertise;
        public List<ResearchProjectDef> HomeWork = new List<ResearchProjectDef>();
        public List<ThingDef> proficientPlants;
        public List<ThingDef> proficientWeapons;
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
            TechLevel techLevel = pawn.Faction.def.techLevel;
            int slots = Mathf.Max(0, FactionExpertiseRange(techLevel) - (4 - pawn.ageTracker.CurLifeStageIndex));
            if (slots == 0) return null;
            SkillRecord highestSkillRecord = pawn.skills.skills.Aggregate((l, r) => l.levelInt > r.levelInt ? l : r);
            SkillDef highestSkill = highestSkillRecord.def;
            SkillDef secondSkill = pawn.skills.skills.Except(highestSkillRecord).Aggregate((l, r) => l.levelInt > r.levelInt ? l : r).def;
            bool guru = techLevel < TechLevel.Archotech && highestSkill == SkillDefOf.Intellectual && highestSkillRecord.Level >= Rand.Range(7, 10);
            var filtered = Extension_Research.SkillsByTech.Where(e => e.Key.techLevel.Equals(techLevel));
            //Log.Warning("GetExpertiseDefsFor: " + pawn + "'s techLevel is " + techLevel);
            int pass = 0;
            List<KeyValuePair<ResearchProjectDef, List<SkillDef>>> result = new List<KeyValuePair<ResearchProjectDef, List<SkillDef>>>();
            if (guru) techLevel++;
            while (result.Count() < slots)
            {
                var remaining = filtered.Except(result);
                if (remaining.Any(e => e.Value.Contains(highestSkill)))
                {
                    result.Add(remaining.RandomElementByWeight(entry => TechLikelihoodForSkill(pawn, entry.Value, highestSkill, slots, pass)));
                }
                else if (remaining.Any(e => e.Value.Contains(secondSkill)))
                {
                    result.Add(remaining.RandomElementByWeight(entry => TechLikelihoodForSkill(pawn, entry.Value, secondSkill, slots, pass)));
                }
                else
                {
                    result.Add(remaining.RandomElement());
                }
                pass++;
                if (guru && pass == 1) techLevel--;
            }
            return result.Select(e => e.Key).ToList();
        }

        public static IEnumerable<ResearchProjectDef> RandomExpertiseDefFor(FactionDef factionType)
        {
            IEnumerable<ResearchProjectDef> source = from tech in DefDatabase<ResearchProjectDef>.AllDefs
                                                     where tech.techLevel.Equals(factionType.techLevel)
                                                     select tech;
            IEnumerable<ResearchProjectDef> result = source.ToList().TakeRandom(FactionExpertiseRange(factionType.techLevel));
            return result;
        }

        public void AcquireExpertise()
        {
            if (expertise == null)
            {
                //expertise = new List<ResearchProjectDef>();
                //expertise.AddRange(GetExpertiseDefsFor(pawn));
                expertise = GetExpertiseDefsFor(pawn).ToDictionary(x => x, x => 1f);
                AcquireWeaponKnowledge();
                AcquirePlantKnowledge();
            }
        }

        public void AcquirePlantKnowledge()
        {
            if (proficientPlants == null)
            {
                proficientPlants = new List<ThingDef>();
                //proficientPlants.AddRange(ModBaseHumanResources.UniversalCrops);
                foreach (ResearchProjectDef tech in expertise.Keys) LearnCrops(tech);
            }
        }

        public void AcquireWeaponKnowledge()
        {
            if (proficientWeapons == null)
            {
                //Log.Message("Determining initial weapon list for " + pawn + " with expertise count = " + expertise.Count+"...");
                proficientWeapons = new List<ThingDef>();
                //proficientWeapons.AddRange(ModBaseHumanResources.UniversalWeapons);
                foreach (ResearchProjectDef tech in expertise.Keys) LearnWeapons(tech);
                //string test = (proficientWeapons != null) ? "ok" : "bad";
                //Log.Message("... proficientWeapons is " + test + ", counts " + proficientWeapons.Count);
            }
        }

        public void AssignHomework(IEnumerable<ResearchProjectDef> studyMaterial)
        {
            //Log.Message("Assigning homework for " + pawn + ", faction is " + pawn.Faction.IsPlayer + ", received " + studyMaterial.Count() + "projects, homework count is " + HomeWork.Count()/* + ", " + studyMaterial.Except(expertise).Except(HomeWork).Count() + " are relevant"*/);
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
                //Log.Warning("...Available projects: " + available.ToStringSafeEnumerable());
                var derived = available.Where(t => t.prerequisites != null && t.prerequisites.All(r => expertise.Keys.Contains(r)));
                var starters = available.Where(t => t.prerequisites.NullOrEmpty());
                if (!starters.Any() && !derived.Any())
                {
                    JobFailReason.Is("LacksFundamentalKnowledge".Translate(pawn), null);
                    return;
                }
                //List<ResearchProjectDef> nextProjects = starters.Concat(derived).ToList();
                //HomeWork.AddRange(nextProjects);
                HomeWork.AddRange(starters.Concat(derived));
            }
        }

        public override void CompTickRare()
        {
            if (!HomeWork.NullOrEmpty())
            {
                //var selectedAnywhere = Find.map .CurrentMap.listerBuildings.AllBuildingsColonistOfClass<Building_WorkTable>().SelectMany(x => x.billStack.Bills).Where(x => x.recipe.defName.StartsWith("Tech_")).SelectMany(x => x.SelectedTech()).Distinct();
                var excess = HomeWork.Except(SelectedAnywhere);
                if (excess.Any())
                {
                    HomeWork.RemoveAll(x => excess.Contains(x));
                    //Log.Message("Removing " + excess.Count() + " unassigned projects from" + pawn);
                }
            }
        }

        private static IEnumerable<ResearchProjectDef> SelectedAnywhere => Find.Maps.SelectMany(x => x.listerBuildings.AllBuildingsColonistOfClass<Building_WorkTable>()).SelectMany(x => x.billStack.Bills).Where(x => x.UsesKnowledge()/*x.recipe.defName.StartsWith("Tech_")*/).SelectMany(x => x.SelectedTech()).Distinct();

        public void ExposeData()
        {
            ((IExposable)pawn).ExposeData();
        }

        public void LearnCrops(ResearchProjectDef tech)
        {
            proficientPlants.AddRange(tech.UnlockedPlants());
            //Log.Message(parent + " can cultivate the followin plants: " + proficientPlants.ToStringSafeEnumerable());
        }

        public void LearnWeapons(ResearchProjectDef tech)
        {
            proficientWeapons.AddRange(tech.UnlockedWeapons());
            //Log.Message(parent + " can use the following weapons: " + proficientWeapons.ToStringSafeEnumerable());
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref expertise, "Expertise");
            Scribe_Collections.Look(ref proficientWeapons, "proficientWeapons");
            Scribe_Collections.Look(ref proficientPlants, "proficientPlants");
            //Log.Message("PostExposeData for " + parent + ". proficientWeapons count is " + proficientWeapons.Count());
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
