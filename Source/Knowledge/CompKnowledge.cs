using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HumanResources
{
    public class CompKnowledge : ThingComp
    {
        public Dictionary<ResearchProjectDef, float> expertise;
        public List<ResearchProjectDef> homework;
        public List<ThingDef> proficientPlants;
        public List<ThingDef> proficientWeapons;
        public List<ThingDef> fearedWeapons;
        protected static bool isFighter = false;
        protected static bool isShooter = false;
        protected static TechLevel startingTechLevel;
        public IEnumerable<ResearchProjectDef> knownTechs => expertise.Where(x => x.Value >= 1f).Select(x => x.Key);

        public List<ThingDef> knownWeapons
        {
            get
            {
                return proficientWeapons.Concat(ModBaseHumanResources.UniversalWeapons).Concat(ModBaseHumanResources.SimpleWeapons.Where(x => x.techLevel <= startingTechLevel)).ToList();
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

        private static Func<SkillRecord, SkillRecord, SkillRecord> AccessHighestSkill = (A, B) =>
        {
            int a = A.levelInt;
            int b = B.levelInt;
            if (a == b) return (A.passion >= B.passion) ? A : B;
            return (a > b) ? A : B;
        };

        private static bool TechPool(bool isPlayer, ResearchProjectDef tech, TechLevel startingTechLevel, FactionDef faction)
        {
            if (!isPlayer)
            {
                if (tech.techLevel == startingTechLevel) return true;
            }
            else
            {
                if (ModBaseHumanResources.TechPoolIncludesTechLevel && tech.techLevel == startingTechLevel) return true;
                if (ModBaseHumanResources.TechPoolIncludesScenario.Value && !ModBaseHumanResources.unlocked.startingTechs.EnumerableNullOrEmpty() && ModBaseHumanResources.unlocked.startingTechs.Contains(tech)) return true;
                if (ModBaseHumanResources.TechPoolIncludesStarting && !faction.startingResearchTags.NullOrEmpty() && faction.startingResearchTags.Any())
                {
                    foreach (ResearchProjectTagDef tag in faction.startingResearchTags)
                    {
                        return tech.tags?.Contains(tag) ?? false;
                    }
                }
            }
            return false;
        }

        public static IEnumerable<ResearchProjectDef> GetExpertiseDefsFor(Pawn pawn, FactionDef faction)
        {
            //0. Info for debugging.
            if (Prefs.LogVerbose)
            {
                bool flag = false;
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("... Including technologies from");
                if (ModBaseHumanResources.TechPoolIncludesTechLevel)
                {
                    stringBuilder.Append(" the faction tech level,");
                    flag = true;
                }
                if (ModBaseHumanResources.TechPoolIncludesScenario)
                {
                    stringBuilder.Append(" the scenario,");
                    flag = true;
                }
                if (ModBaseHumanResources.TechPoolIncludesStarting)
                {
                    stringBuilder.Append(" the starting techs");
                    flag = true;
                }
                if (flag) Log.Message(stringBuilder.ToString().TrimEnd(new char[] { ',' }) + ".");
                else Log.Warning("[HumanResources] Empty technology pool!");
            }

            //1. Gather info on that pawn

            //a. tech level
            startingTechLevel = faction?.techLevel ?? 0;
            if (startingTechLevel == 0) /*startingTechLevel = InferTechLevelfromBG(pawn);*/
            {
                if (pawn.story != null)
                {
                    TechLevel childhoodTechLevel = PawnBackgroundUtility.InferTechLevelfromBackstory(pawn.story.childhood);
                    TechLevel adulthoodTechLevel = pawn.story.adulthood != null ? PawnBackgroundUtility.InferTechLevelfromBackstory(pawn.story.adulthood) : 0;
                    if (childhoodTechLevel == TechLevel.Undefined) childhoodTechLevel = TechLevel.Industrial;
                    if (adulthoodTechLevel == TechLevel.Undefined) adulthoodTechLevel = TechLevel.Industrial;
                    startingTechLevel = (TechLevel)Math.Max((int)childhoodTechLevel, (int)adulthoodTechLevel);
                }
            }

            //b. higest skills
            SkillRecord highestSkillRecord = pawn.skills.skills.Aggregate(AccessHighestSkill);
            SkillDef highestSkill = highestSkillRecord.def;
            SkillDef secondSkill = pawn.skills.skills.Except(highestSkillRecord).Aggregate(AccessHighestSkill).def;

            //c. special cases
            isFighter = highestSkill == SkillDefOf.Melee;
            isShooter = highestSkill == SkillDefOf.Shooting;
            int fighterHandicap = (isFighter | isShooter) ? 1 : 0;
            int oldBonus = pawn.ageTracker.AgeBiologicalYears > pawn.RaceProps.lifeExpectancy / 2 ? 1 : 0;
            bool guru = startingTechLevel < TechLevel.Archotech && highestSkill == SkillDefOf.Intellectual && highestSkillRecord.Level >= Rand.Range(7, 10);

            //2. Calculate how many techs he should know
            int minSlots = startingTechLevel > TechLevel.Medieval ? 1 : oldBonus;
            int slots = Mathf.Max(minSlots, FactionExpertiseRange(startingTechLevel) - (4 - pawn.ageTracker.CurLifeStageIndex) + oldBonus - fighterHandicap);
            if (slots == 0)
            {
                if (Prefs.LogVerbose) Log.Warning("... No slots for " + pawn.gender.GetObjective() + ", returning null. (StartingTechLevel is " + startingTechLevel + ", CurLifeStageIndex is " + pawn.ageTracker.CurLifeStageIndex + ", fighterHandicap is " + fighterHandicap + ")");
                return null;
            }

            //3. Distribute knowledge
            if (Prefs.LogVerbose)
            {
                string guruNote = guru ? " (with intellectual bounus)" : "";
                Log.Message("... " + slots + " calculated slots (techLevel is " + startingTechLevel + guruNote + ", highest skills: " + highestSkill.label + " & " + secondSkill.label + ")");
            }
            bool isPlayer = pawn.Faction?.IsPlayer ?? false;
            var filtered = Extension_Research.SkillsByTech.Where(e => TechPool(isPlayer, e.Key, startingTechLevel, faction));
            int pass = 0;
            List<ResearchProjectDef> result = new List<ResearchProjectDef>();
            if (guru) startingTechLevel++;
            while (result.Count() < slots)
            {
                pass++;
                var remaining = filtered.Where(x => !result.Contains(x.Key));
                if (remaining.EnumerableNullOrEmpty()) break;
                SkillDef skill = null;
                if (pass == 1 && remaining.Any(e => e.Value.Contains(highestSkill)))
                {
                    skill = highestSkill;
                }
                else if (pass == 2 && remaining.Any(e => e.Value.Contains(secondSkill)))
                {
                    skill = secondSkill;
                }
                ResearchProjectDef selected = remaining.RandomElementByWeightWithDefault(entry => TechLikelihoodForSkill(pawn, entry.Value, slots, pass, skill), 1f).Key ?? remaining.RandomElement().Key;
                result.Add(selected);
                if ((guru && pass == 1) | result.NullOrEmpty()) startingTechLevel--;
                if (startingTechLevel == 0) break;
            }
            if (!result.NullOrEmpty())
            {
                return result;
            }
            Log.Error("[HumanResources] Couldn't calculate any expertise for " + pawn);
            return null;
        }

        //private static TechLevel InferTechLevelfromBG(Pawn pawn)
        //{
        //    if (pawn.story != null)
        //    {
        //        string child = (pawn.story.childhood.title + " " + pawn.story.childhood.baseDesc).ToLower();
        //        bool isAdult = pawn.story.adulthood != null;
        //        string adult = isAdult ? (pawn.story.adulthood.title + " " + pawn.story.adulthood.baseDesc).ToLower() : "";
        //        foreach (string word in PawnBackgroundUtility.spacerHints)
        //        {
        //            if (child.Contains(word) | (isAdult && adult.Contains(word))) return TechLevel.Spacer;
        //        }
        //        foreach (string word in PawnBackgroundUtility.industrialHints)
        //        {
        //            if (child.Contains(word) | (isAdult && adult.Contains(word))) return TechLevel.Industrial;
        //        }
        //        foreach (string word in PawnBackgroundUtility.medievalHints)
        //        {
        //            if (child.Contains(word) | (isAdult && adult.Contains(word))) return TechLevel.Medieval;
        //        }
        //        if ((!pawn.story.childhood.spawnCategories.NullOrEmpty() && pawn.story.childhood.spawnCategories.Contains("Tribal")) | (isAdult && !pawn.story.adulthood.spawnCategories.NullOrEmpty() && pawn.story.adulthood.spawnCategories.Contains("Tribal")))
        //        {
        //            return TechLevel.Neolithic;
        //        }
        //        else
        //        {
        //            foreach (string word in PawnBackgroundUtility.tribalHints)
        //            {
        //                if (child.Contains(word) | (isAdult && adult.Contains(word))) return TechLevel.Neolithic;
        //            }
        //        }
        //    }
        //    return TechLevel.Industrial;
        //}

        //public static IEnumerable<ResearchProjectDef> GetAltExpertiseDefsFor(Pawn pawn, FactionDef faction)
        //{
        //    //0. Info for debugging.
        //    if (Prefs.LogVerbose)
        //    {
        //        bool flag = false;
        //        StringBuilder stringBuilder = new StringBuilder();
        //        stringBuilder.Append("... Including technologies from");
        //        if (ModBaseHumanResources.TechPoolIncludesTechLevel)
        //        {
        //            stringBuilder.Append(" the faction tech level,");
        //            flag = true;
        //        }
        //        if (ModBaseHumanResources.TechPoolIncludesScenario)
        //        {
        //            stringBuilder.Append(" the scenario,");
        //            flag = true;
        //        }
        //        if (ModBaseHumanResources.TechPoolIncludesStarting)
        //        {
        //            stringBuilder.Append(" the starting techs");
        //            flag = true;
        //        }
        //        if (flag) Log.Message(stringBuilder.ToString().TrimEnd(new char[] { ',' }) + ".");
        //        else Log.Warning("[HumanResources] Empty technology pool!");
        //    }

        //    //1. Gather info on that pawn

        //    //a. tech level
        //    startingTechLevel = faction?.techLevel ?? 0;
        //    if (startingTechLevel == 0) startingTechLevel = InferTechLevelfromBG(pawn);

        //    //b. higest skills
        //    SkillRecord highestSkillRecord = pawn.skills.skills.Aggregate(AccessHighestSkill);
        //    SkillDef highestSkill = highestSkillRecord.def;
        //    SkillDef secondSkill = pawn.skills.skills.Except(highestSkillRecord).Aggregate(AccessHighestSkill).def;

        //    //c. special cases
        //    isFighter = highestSkill == SkillDefOf.Melee;
        //    isShooter = highestSkill == SkillDefOf.Shooting;
        //    int fighterHandicap = (isFighter | isShooter) ? 1 : 0;
        //    int oldBonus = pawn.ageTracker.AgeBiologicalYears > pawn.RaceProps.lifeExpectancy / 2 ? 1 : 0;
        //    bool guru = startingTechLevel < TechLevel.Archotech && highestSkill == SkillDefOf.Intellectual && highestSkillRecord.Level >= Rand.Range(7, 10);

        //    //2. Calculate how many techs he should know
        //    int minSlots = startingTechLevel > TechLevel.Medieval ? 1 : oldBonus;
        //    int slots = Mathf.Max(minSlots, FactionExpertiseRange(startingTechLevel) - (4 - pawn.ageTracker.CurLifeStageIndex) + oldBonus - fighterHandicap);
        //    if (slots == 0)
        //    {
        //        if (Prefs.LogVerbose) Log.Warning("... No slots for " + pawn.gender.GetObjective() + ", returning null. (StartingTechLevel is " + startingTechLevel + ", CurLifeStageIndex is " + pawn.ageTracker.CurLifeStageIndex + ", fighterHandicap is " + fighterHandicap + ")");
        //        return null;
        //    }

        //    //3. Distribute knowledge
        //    if (Prefs.LogVerbose)
        //    {
        //        string guruNote = guru ? " (with intellectual bounus)" : "";
        //        Log.Message("... " + slots + " calculated slots (techLevel is " + startingTechLevel + guruNote + ", highest skills: " + highestSkill.label + " & " + secondSkill.label + ")");
        //    }
        //    bool isPlayer = pawn.Faction?.IsPlayer ?? false;
        //    var filtered = Extension_Research.SkillsByTech.Where(e => TechPool(isPlayer, e.Key, startingTechLevel, faction));
        //    int pass = 0;
        //    List<ResearchProjectDef> result = new List<ResearchProjectDef>();
        //    if (guru) startingTechLevel++;
        //    while (result.Count() < slots)
        //    {
        //        pass++;
        //        var remaining = filtered.Where(x => !result.Contains(x.Key));
        //        if (remaining.EnumerableNullOrEmpty()) break;
        //        SkillDef skill = null;
        //        if (pass == 1 && remaining.Any(e => e.Value.Contains(highestSkill)))
        //        {
        //            skill = highestSkill;
        //        }
        //        else if (pass == 2 && remaining.Any(e => e.Value.Contains(secondSkill)))
        //        {
        //            skill = secondSkill;
        //        }
        //        ResearchProjectDef selected = remaining.RandomElementByWeightWithDefault(entry => TechLikelihoodForSkill(pawn, entry.Value, slots, pass, skill), 1f).Key ?? remaining.RandomElement().Key;
        //        result.Add(selected);
        //        if ((guru && pass == 1) | result.NullOrEmpty()) startingTechLevel--;
        //        if (startingTechLevel == 0) break;
        //    }
        //    if (!result.NullOrEmpty())
        //    {
        //        return result;
        //    }
        //    Log.Error("[HumanResources] Couldn't calculate any expertise for " + pawn);
        //    return null;
        //}


        public void AcquireExpertise()
        {
            if (Prefs.LogVerbose) Log.Warning("[HumanResources] Acquiring expertise for " + pawn + "...");
            expertise = new Dictionary<ResearchProjectDef, float>();
            FactionDef faction = pawn.Faction?.def ?? pawn.kindDef.defaultFactionType;
            var acquiredExpertise = GetExpertiseDefsFor(pawn, faction);
            if (!acquiredExpertise.EnumerableNullOrEmpty())
            {
                expertise = acquiredExpertise.Where(x => x != null).ToDictionary(x => x, x => 1f);
                if (Prefs.LogVerbose) Log.Message("... "+pawn.gender.GetPossessive().CapitalizeFirst() + " knowledge is going to be " + expertise.Keys.ToStringSafeEnumerable() + ".");
            }
            else
            {
                Log.Warning("[HumanResources] "+pawn+" spawned without acquiring any expertise.");
            }
            AcquireWeaponKnowledge(faction);
            if (Prefs.LogVerbose && proficientWeapons.Any()) Log.Message("... "+ pawn.gender.GetPossessive().CapitalizeFirst() + " weapon proficiency is going to be: " + proficientWeapons.ToStringSafeEnumerable());
            AcquirePlantKnowledge();
            if (Prefs.LogVerbose && proficientPlants.Any()) Log.Message("... "+pawn.gender.GetPronoun().CapitalizeFirst() + " will be able to cultivate the following plants: " + proficientPlants.ToStringSafeEnumerable());
        }

        public void AcquirePlantKnowledge()
        {
            if (proficientPlants == null)
            {
                proficientPlants = new List<ThingDef>();
                if (!expertise.EnumerableNullOrEmpty()) foreach (ResearchProjectDef tech in expertise.Keys) LearnCrops(tech);
            }
        }

        private bool TestIfWeapon(ThingDef weapon, bool close)
        {
            return close ? weapon.IsMeleeWeapon : weapon.IsRangedWeapon;
        }

        public void AcquireWeaponKnowledge(FactionDef faction)
        {
            if (proficientWeapons == null)
            {
                proficientWeapons = new List<ThingDef>();
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("... ");
                if (!expertise.EnumerableNullOrEmpty())
                {
                    foreach (ResearchProjectDef tech in expertise.Keys) LearnWeapons(tech);
                    if (Prefs.LogVerbose && !proficientWeapons.NullOrEmpty()) stringBuilder.Append(pawn.gender.GetPronoun().CapitalizeFirst() + " can craft some weapons. ");
                }
                bool isPlayer = pawn.Faction?.IsPlayer ?? false;
                if (isPlayer && (ModBaseHumanResources.FreeScenarioWeapons || ModBaseHumanResources.unlocked.knowAllStartingWeapons))
                {
                    proficientWeapons.AddRange(ModBaseHumanResources.unlocked.startingWeapons);
                    if (Prefs.LogVerbose && !proficientWeapons.NullOrEmpty()) stringBuilder.Append(pawn.gender.GetPronoun().CapitalizeFirst() + " gets the scenario starting weapons. ");
                }
                if (isFighter || isShooter)
                {
                    if (Prefs.LogVerbose)
                    {
                        string[] role = isFighter ? new [] {"fighter","melee"} : new [] { "shooter","ranged"};
                        stringBuilder.Append(pawn.gender.GetPronoun().CapitalizeFirst() + " is a " + role[0] + " and gets extra " + role[1] + " weapons from ");
                    }
                    if (ModBaseHumanResources.WeaponPoolIncludesTechLevel || !isPlayer)
                    {
                        foreach (ResearchProjectDef tech in DefDatabase<ResearchProjectDef>.AllDefs.Where(x => TechPool(isPlayer, x, startingTechLevel, faction)))
                        {
                            var weapons = tech.UnlockedWeapons().Where(x => TestIfWeapon(x, isFighter));
                            if (weapons.Any()) foreach (ThingDef w in weapons) proficientWeapons.Add(w);
                        }
                        if (Prefs.LogVerbose) stringBuilder.Append(pawn.gender.GetPossessive().ToLower() + " faction's tech level");
                    }
                    if (isPlayer && ModBaseHumanResources.WeaponPoolIncludesScenario && !(ModBaseHumanResources.FreeScenarioWeapons || ModBaseHumanResources.unlocked.knowAllStartingWeapons))
                    {
                        proficientWeapons.AddRange(ModBaseHumanResources.unlocked.startingWeapons.Where(x => TestIfWeapon(x, isFighter)));
                        string connector = ModBaseHumanResources.WeaponPoolIncludesTechLevel ? " and " : "";
                        if (Prefs.LogVerbose) stringBuilder.Append(connector + "the starting scenario");
                    }
                    if(Prefs.LogVerbose) stringBuilder.Append(". ");
                }
                if (pawn.equipment.HasAnything())
                {
                    ThingWithComps weapon = pawn.equipment.Primary;
                    if (!knownWeapons.Contains(weapon.def))
                    {
                        proficientWeapons.Add(weapon.def);
                        if (Prefs.LogVerbose) stringBuilder.Append(pawn.gender.GetPronoun().CapitalizeFirst() + " is using a " + weapon.def.label + ".");
                    }
                }
                proficientWeapons.RemoveDuplicates();
                if (Prefs.LogVerbose && stringBuilder.Length > 4) Log.Message(stringBuilder.ToString());
            }
        }

        public void ExposeData()
        {
            ((IExposable)pawn).ExposeData();
        }

        public void LearnCrops(ResearchProjectDef tech)
        {
            proficientPlants.AddRange(tech.UnlockedPlants());
        }

        public void LearnWeapons(ResearchProjectDef tech)
        {
            proficientWeapons.AddRange(tech.UnlockedWeapons());
        }

        public void LearnWeapon(ThingDef weapon)
        {
            if (!fearedWeapons.NullOrEmpty() && fearedWeapons.Contains(weapon)) fearedWeapons.Remove(weapon);
            proficientWeapons.Add(weapon);
        }

        public void AddWeaponTrauma(ThingDef weapon)
        {
            if (fearedWeapons == null) fearedWeapons = new List<ThingDef>();
            fearedWeapons.AddDistinct(weapon);
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
            Scribe_Collections.Look(ref homework, "homework");
            Scribe_Collections.Look(ref proficientWeapons, "proficientWeapons");
            Scribe_Collections.Look(ref proficientPlants, "proficientPlants");
            Scribe_Collections.Look(ref fearedWeapons, "fearedWeapons");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && homework == null) homework = new List<ResearchProjectDef>();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (expertise == null) AcquireExpertise();
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

        private static float TechLikelihoodForSkill(Pawn pawn, List<SkillDef> skills, int slots, int pass, SkillDef highestSkill = null)
        {
            List<SkillDef> unskilled = (from x in pawn.skills.skills
                                        where x.Level < 2
                                        select x.def).ToList();
            float chance = ((slots - pass) / slots)*100f;
            if (highestSkill != null)
            {
                if (highestSkill != null && skills.Contains(highestSkill)) return chance;
                else if (skills.All(s => unskilled.Contains(s))) return (100f - chance) / 10;
            }
            return 100f - chance;
        }

        //new functions
        public void AssignBranch(ResearchProjectDef tech)
        {
            if (homework == null) homework = new List<ResearchProjectDef>();
            homework.Add(tech);
            if (!knownTechs.Contains(tech)) homework.AddRange(GetRequiredRecursive(tech));
        }

        public List<ResearchProjectDef> GetRequiredRecursive(ResearchProjectDef tech)
        {
            if (expertise.Where(x => x.Value >= 1 && !x.Key.prerequisites.NullOrEmpty() && x.Key.prerequisites.Contains(tech)).Any()) return new List<ResearchProjectDef>();
            var parents = tech.prerequisites?.Where(x => !x.IsKnownBy(pawn));
            if (parents == null) return new List<ResearchProjectDef>();
            var allParents = new List<ResearchProjectDef>(parents);
            foreach (var parent in parents) allParents.AddRange(GetRequiredRecursive(parent));
            return allParents.Distinct().ToList();
        }

        public void CancelBranch(ResearchProjectDef tech)
        {
            foreach (var child in GetDescendentsRecursive(tech)) homework.Remove(child);
            homework.Remove(tech);
        }

        public List<ResearchProjectDef> GetDescendentsRecursive(ResearchProjectDef tech)
        {
            var children = homework.Where(x => !x.prerequisites.NullOrEmpty() && x.prerequisites.Contains(tech));
            if (children.EnumerableNullOrEmpty()) return new List<ResearchProjectDef>();
            var allChildren = new List<ResearchProjectDef>(children);
            foreach (var child in children) allChildren.AddRange(GetDescendentsRecursive(child));
            return allChildren.Distinct().ToList();
        }
    }
}
