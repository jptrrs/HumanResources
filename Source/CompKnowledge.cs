using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HumanResources
{
    public class CompKnowledge : ThingComp
    {
        public List<ResearchProjectDef> expertise;
        public List<ResearchProjectDef> HomeWork = new List<ResearchProjectDef>();
        public List<ThingDef> knownPlants;
        public List<ThingDef> knownWeapons;

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
            var filtered = Research_Extension.SkillsByTech.Where(e => e.Key.techLevel.Equals(techLevel));
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
                expertise = new List<ResearchProjectDef>();
                expertise.AddRange(GetExpertiseDefsFor(pawn));
                AcquireWeaponKnowledge();
                AcquirePlantKnowledge();
            }
        }

        public void AcquirePlantKnowledge()
        {
            if (knownPlants == null)
            {
                knownPlants = new List<ThingDef>();
                knownPlants.AddRange(ModBaseHumanResources.UniversalCrops);
                foreach (ResearchProjectDef tech in expertise) LearnCrops(tech);
            }
        }

        public void AcquireWeaponKnowledge()
        {
            if (knownWeapons == null)
            {
                knownWeapons = new List<ThingDef>();
                knownWeapons.AddRange(ModBaseHumanResources.UniversalWeapons);
                foreach (ResearchProjectDef tech in expertise) LearnWeapons(tech);
            }
        }
        public void AssignHomework(List<ResearchProjectDef> studyMaterial)
        {
            Log.Message("Assingning homework for " + pawn + ", faction is " + pawn.Faction.IsPlayer + ", received " + studyMaterial.Count() + "projects, homework count is " + HomeWork.Count() + ", " + studyMaterial.Except(expertise).Except(HomeWork).Count() + " are relevant");
            if (pawn.Faction.IsPlayer)
            {
                IEnumerable<ResearchProjectDef> excess = HomeWork.Except(studyMaterial);
                if (excess.Count() > 0)
                {
                    //Log.Message("Removing " + excess.Count() + " unassigned projects from" + pawn);
                    HomeWork.RemoveAll(x => excess.Contains(x));
                }
                var available = studyMaterial.Except(expertise).Except(HomeWork);
                Log.Warning("...Available projects: " + available.ToStringSafeEnumerable());
                var derived = available.Where(t => t.prerequisites != null && t.prerequisites.All(r => expertise.Contains(r)));
                var starters = available.Where(t => t.prerequisites.NullOrEmpty());
                List<ResearchProjectDef> nextProjects = starters.Concat(derived).ToList();
                HomeWork.AddRange(nextProjects);
            }
        }

        public void ExposeData()
        {
            ((IExposable)pawn).ExposeData();
        }

        public void LearnCrops(ResearchProjectDef tech)
        {
            knownPlants.AddRange(tech.UnlockedPlants());
            //Log.Message(parent + " can cultivate the followin plants: " + knownPlants.ToStringSafeEnumerable());
        }

        public void LearnWeapons(ResearchProjectDef tech)
        {
            knownWeapons.AddRange(tech.UnlockedWeapons());
            //Log.Message(parent + " can use the following weapons: " + knownWeapons.ToStringSafeEnumerable());
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Collections.Look(ref expertise, "Expertise");
            Scribe_Collections.Look(ref knownWeapons, "KnownWeapons");
            Scribe_Collections.Look(ref knownPlants, "KnownPlants");
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
