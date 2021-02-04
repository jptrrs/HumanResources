using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace HumanResources
{
    using static ModBaseHumanResources;

    public class CompKnowledge : ThingComp
    {
        public Dictionary<ResearchProjectDef, float> expertise;
        public List<ResearchProjectDef> homework;
        public TechLevel techLevel;
        protected bool
            craftableSet = false,
            isFighter = false,
            isShooter = false;
        protected TechLevel startingTechLevel;
        private static Func<SkillRecord, SkillRecord, SkillRecord> AccessHighestSkill = (A, B) =>
        {
            int a = A.levelInt;
            int b = B.levelInt;
            if (a == b) return (A.passion >= B.passion) ? A : B;
            return (a > b) ? A : B;
        };
        public List<ThingDef>
            fearedWeapons,
            proficientPlants,
            proficientWeapons;
        private List<ThingDef> _craftableWeapons = new List<ThingDef>();
        private List<ResearchProjectDef> _knownTechs;

        public IEnumerable<ThingDef> craftableWeapons
        {
            get
            {
                if (!craftableSet && _craftableWeapons.NullOrEmpty())
                {
                    _craftableWeapons.AddRange(knownTechs.SelectMany(x => x.UnlockedWeapons()));
                    craftableSet = true;
                }
                return _craftableWeapons;
            }
        }

        public List<ThingDef> knownPlants
        {
            get
            {
                return proficientPlants.Concat(UniversalCrops).ToList();
            }
        }

        public IEnumerable<ResearchProjectDef> knownTechs
        {
            get
            {
                if (_knownTechs.NullOrEmpty()) _knownTechs = expertise.Where(x => x.Value >= 1f).Select(x => x.Key).ToList();
                return _knownTechs;
            }
        }

        public List<ThingDef> knownWeapons => proficientWeapons.Concat(UniversalWeapons).Concat(techLevelWeapons).ToList();

        public IEnumerable<ThingDef> techLevelWeapons => SimpleWeapons.Where(x => x.techLevel <= startingTechLevel);

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

        public void AcquireExpertise()
        {
            if (Prefs.LogVerbose) Log.Warning("[HumanResources] Acquiring expertise for " + pawn + "...");
            expertise = new Dictionary<ResearchProjectDef, float>();
            FactionDef faction = pawn.Faction?.def ?? pawn.kindDef.defaultFactionType;
            var acquiredExpertise = GetExpertiseDefsFor(pawn, faction);
            if (!acquiredExpertise.EnumerableNullOrEmpty())
            {
                expertise = acquiredExpertise.Where(x => x != null).ToDictionary(x => x, x => 1f);
                if (Prefs.LogVerbose) Log.Message("... " + pawn.gender.GetPossessive().CapitalizeFirst() + " knowledge is going to be " + expertise.Keys.ToStringSafeEnumerable() + ".");
            }
            else
            {
                Log.Warning("[HumanResources] " + pawn + " spawned without acquiring any expertise.");
            }
            AcquireWeaponKnowledge(faction);
            if (Prefs.LogVerbose && proficientWeapons.Any()) Log.Message("... " + pawn.gender.GetPossessive().CapitalizeFirst() + " weapon proficiency is going to be: " + proficientWeapons.ToStringSafeEnumerable());
            AcquirePlantKnowledge();
            if (Prefs.LogVerbose && proficientPlants.Any()) Log.Message("... " + pawn.gender.GetPronoun().CapitalizeFirst() + " will be able to cultivate the following plants: " + proficientPlants.ToStringSafeEnumerable());
        }

        public void AcquirePlantKnowledge()
        {
            if (proficientPlants == null)
            {
                proficientPlants = new List<ThingDef>();
                if (!expertise.EnumerableNullOrEmpty()) foreach (ResearchProjectDef tech in expertise.Keys) LearnCrops(tech);
            }
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
                if (isPlayer && (FreeScenarioWeapons || unlocked.knowAllStartingWeapons))
                {
                    proficientWeapons.AddRange(unlocked.startingWeapons);
                    if (Prefs.LogVerbose && !proficientWeapons.NullOrEmpty()) stringBuilder.Append(pawn.gender.GetPronoun().CapitalizeFirst() + " gets the scenario starting weapons. ");
                }
                if (isFighter || isShooter)
                {
                    if (Prefs.LogVerbose)
                    {
                        string[] role = isFighter ? new[] { "fighter", "melee" } : new[] { "shooter", "ranged" };
                        stringBuilder.Append(pawn.gender.GetPronoun().CapitalizeFirst() + " is a " + role[0] + " and gets extra " + role[1] + " weapons from ");
                    }
                    if (WeaponPoolIncludesTechLevel || !isPlayer)
                    {
                        foreach (ResearchProjectDef tech in DefDatabase<ResearchProjectDef>.AllDefs.Where(x => TechPool(isPlayer, x, techLevel, faction)))
                        {
                            var weapons = tech.UnlockedWeapons().Where(x => TestIfWeapon(x, isFighter));
                            if (weapons.Any()) foreach (ThingDef w in weapons) proficientWeapons.Add(w);
                        }
                        if (Prefs.LogVerbose) stringBuilder.Append(pawn.gender.GetPossessive().ToLower() + " faction's tech level");
                    }
                    if (isPlayer && WeaponPoolIncludesScenario && !(FreeScenarioWeapons || unlocked.knowAllStartingWeapons))
                    {
                        proficientWeapons.AddRange(unlocked.startingWeapons.Where(x => TestIfWeapon(x, isFighter)));
                        string connector = WeaponPoolIncludesTechLevel ? " and " : "";
                        if (Prefs.LogVerbose) stringBuilder.Append(connector + "the starting scenario");
                    }
                    if (Prefs.LogVerbose) stringBuilder.Append(". ");
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

        public void AddWeaponTrauma(ThingDef weapon)
        {
            if (fearedWeapons == null) fearedWeapons = new List<ThingDef>();
            fearedWeapons.AddDistinct(weapon);
        }

        public void AssignBranch(ResearchProjectDef tech)
        {
            if (homework == null) homework = new List<ResearchProjectDef>();
            homework.Add(tech);
            if (!knownTechs.Contains(tech)) homework.AddRange(GetRequiredRecursive(tech));
        }

        public void CancelBranch(ResearchProjectDef tech)
        {
            foreach (var child in GetDescendentsRecursive(tech)) homework.Remove(child);
            homework.Remove(tech);
        }

        public void ExposeData()
        {
            ((IExposable)pawn).ExposeData();
        }

        public List<ResearchProjectDef> GetDescendentsRecursive(ResearchProjectDef tech)
        {
            var children = homework.Where(x => !x.prerequisites.NullOrEmpty() && x.prerequisites.Contains(tech));
            if (children.EnumerableNullOrEmpty()) return new List<ResearchProjectDef>();
            var allChildren = new List<ResearchProjectDef>(children);
            foreach (var child in children) allChildren.AddRange(GetDescendentsRecursive(child));
            return allChildren.Distinct().ToList();
        }

        public IEnumerable<ResearchProjectDef> GetExpertiseDefsFor(Pawn pawn, FactionDef faction)
        {
            //0. Info for debugging.
            if (Prefs.LogVerbose)
            {
                bool flag = false;
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("... Including technologies from");
                if (TechPoolIncludesTechLevel)
                {
                    stringBuilder.Append(" the faction tech level,");
                    flag = true;
                }
                if (TechPoolIncludesScenario)
                {
                    stringBuilder.Append(" the scenario,");
                    flag = true;
                }
                if (TechPoolIncludesStarting)
                {
                    stringBuilder.Append(" the starting techs");
                    flag = true;
                }
                if (flag) Log.Message(stringBuilder.ToString().TrimEnd(new char[] { ',' }) + ".");
                else Log.Warning("[HumanResources] Empty technology pool!");
            }

            //1. Gather info on that pawn

            //a. tech level
            TechLevel factionTechLevel = faction?.techLevel ?? 0;
            TechLevel childhoodLevel = 0;
            SkillDef childhoodSkill = null;
            bool isPlayer = pawn.Faction?.IsPlayer ?? false;
            techLevel = TechPoolIncludesBackground || !isPlayer ? FindBGTechLevel(pawn, out childhoodLevel, out childhoodSkill) : factionTechLevel;
            TechLevel workingTechLevel = startingTechLevel = techLevel;

            //b. higest skills
            SkillRecord highestSkillRecord = pawn.skills.skills.Aggregate(AccessHighestSkill);
            SkillDef highestSkill = highestSkillRecord.def;
            IEnumerable<SkillRecord> secondCandidates = pawn.skills.skills.Except(highestSkillRecord).Where(x => SkillIsRelevant(x.def, techLevel));
            SkillDef secondSkill = secondCandidates.Aggregate(AccessHighestSkill).def;

            //c. special cases
            isFighter = highestSkill == SkillDefOf.Melee;
            isShooter = highestSkill == SkillDefOf.Shooting;
            int fighterHandicap = (isFighter | isShooter) ? 1 : 0;
            int oldBonus = pawn.ageTracker.AgeBiologicalYears > pawn.RaceProps.lifeExpectancy / 2 ? 1 : 0;
            bool guru = techLevel < TechLevel.Archotech && highestSkill == SkillDefOf.Intellectual && highestSkillRecord.Level >= Rand.Range(7, 10);

            //2. Calculate how many techs he should know
            int minSlots = techLevel > TechLevel.Medieval ? 1 : oldBonus;
            int slots = Mathf.Max(minSlots, FactionExpertiseRange(techLevel) - (4 - pawn.ageTracker.CurLifeStageIndex) + oldBonus - fighterHandicap);
            if (slots == 0)
            {
                if (Prefs.LogVerbose) Log.Warning("... No slots for " + pawn.gender.GetObjective() + ", returning null. (StartingTechLevel is " + techLevel + ", CurLifeStageIndex is " + pawn.ageTracker.CurLifeStageIndex + ", fighterHandicap is " + fighterHandicap + ")");
                return null;
            }

            //3. Distribute knowledge
            if (Prefs.LogVerbose)
            {
                string guruNote = guru ? " (with intellectual bounus)" : "";
                string techlevelText = TechPoolIncludesBackground ?
                    ("techLevels: " + factionTechLevel + " faction, " + childhoodLevel + " childhood, " + techLevel + " background") :
                    "techLevel is " + techLevel;
                Log.Message("... " + slots + " calculated slots (" + techlevelText + guruNote + "; highest relevant skills: " + highestSkill.label + " & " + secondSkill.label + ")");
            }
            bool strict = false;
            bool useChildhood = childhoodSkill != null && TechPoolIncludesBackground && SkillIsRelevant(childhoodSkill, childhoodLevel) && slots > 1;
            var filtered = Extension_Research.SkillsByTech.Where(e => TechPool(isPlayer, e.Key, workingTechLevel, faction, strict));
            int pass = 0;
            List<ResearchProjectDef> result = new List<ResearchProjectDef>();
            if (guru) workingTechLevel++;
            while (result.Count() < slots)
            {
                pass++;
                //Log.Message("DEBUG pass = " + pass + ", techLevel = " + workingTechLevel);
                filtered.ExecuteEnumerable();
                var remaining = filtered.Where(x => !result.Contains(x.Key));
                var test = remaining.Select(x => x.Key).Select(x => x.techLevel).Distinct();
                //Log.Message("DEBUG considered techs are " + test.ToStringSafeEnumerable());
                if (remaining.EnumerableNullOrEmpty()) break;
                SkillDef skill = null;
                if (pass == 1 && remaining.Any(e => e.Value.Contains(highestSkill)))
                {
                    skill = highestSkill;
                }
                else if (pass == 2 && remaining.Any(e => e.Value.Contains(secondSkill)))
                {
                    skill = useChildhood ? childhoodSkill : secondSkill;
                }
                ResearchProjectDef selected = remaining.RandomElementByWeightWithDefault(entry => TechLikelihoodForSkill(pawn, entry.Value, slots, pass, skill), 1f).Key ?? remaining.RandomElement().Key;
                //Log.Message("DEBUG selected: " + selected);
                result.Add(selected);

                //prepare next pass:
                strict = false;
                if ((guru && pass == 1) | result.NullOrEmpty()) workingTechLevel--;
                if (useChildhood)
                {
                    if (pass == 1)
                    {
                        strict = true;
                        workingTechLevel = childhoodLevel;
                    }
                    if (pass == 2) workingTechLevel = techLevel;
                }
                if (workingTechLevel == 0) break;
            }
            if (!result.NullOrEmpty())
            {
                return result;
            }
            Log.Error("[HumanResources] Couldn't calculate any expertise for " + pawn);
            return null;
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

        public void LearnCrops(ResearchProjectDef tech)
        {
            TechLevel asAdult = 0;
            TechLevel asChild = 0;
            childhoodSkill = null;
            try
            {
                if (pawn.story != null)
                {
                    asChild = PawnBackgroundUtility.TechLevelByBackstory[pawn.story.childhood.identifier];
                    if (pawn.story.adulthood != null) asAdult = PawnBackgroundUtility.TechLevelByBackstory[pawn.story.adulthood.identifier];
                    var skillGains = pawn.story.childhood.skillGainsResolved;
                    if (skillGains.Count() > 1) childhoodSkill = skillGains.Aggregate((a, b) => (a.Value >= b.Value) ? a : b).Key;
                    else if (!skillGains.EnumerableNullOrEmpty()) childhoodSkill = skillGains.FirstOrDefault().Key;
                }
                if (asAdult == 0 || asChild == 0)
                {
                    FactionDef faction = pawn.Faction?.def ?? pawn.kindDef?.defaultFactionType;
                    TechLevel fallback = faction?.techLevel ?? TechLevel.Industrial;
                    if (asAdult == 0) asAdult = fallback;
                    if (asChild == 0) asChild = fallback;
                }
            }
            catch (Exception e) // if you are catching err's you might as well explain them.
            {
                Log.Warning("[HumanResources] Error determining tech level from "+pawn+"'s background: " + e.Message);
            }
            childhoodLevel = asChild;
            return asAdult;
        }

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
                        foreach (ResearchProjectDef tech in DefDatabase<ResearchProjectDef>.AllDefs.Where(x => TechPool(isPlayer, x, techLevel, faction)))
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

        public bool LearnTech(ResearchProjectDef tech)
        {
            if (expertise != null)
            {
                if (!expertise.ContainsKey(tech)) expertise.Add(tech, 1f);
                else expertise[tech] = 1f;
                _knownTechs.AddDistinct(tech);
                _craftableWeapons.AddRange(tech.UnlockedWeapons());
                techLevel = (TechLevel)Mathf.Max((int)tech.techLevel, (int)techLevel);
                LearnCrops(tech);
                Messages.Message("MessageStudyComplete".Translate(pawn, tech.LabelCap), pawn, MessageTypeDefOf.TaskCompletion, true);
                return true;
            }
            else
            {
                Log.Warning("[HumanResources] " + pawn + " tried to learn a technology without being able to.");
                return false;
            }
        }

        public void LearnWeapon(ThingDef weapon)
        {
            if (!fearedWeapons.NullOrEmpty() && fearedWeapons.Contains(weapon)) fearedWeapons.Remove(weapon);
            proficientWeapons.Add(weapon);
        }

        public void LearnWeapons(ResearchProjectDef tech)
        {
            proficientWeapons.AddRange(tech.UnlockedWeapons());
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
            Scribe_Values.Look<TechLevel>(ref techLevel, "techLevel", 0);
            Scribe_Values.Look<TechLevel>(ref startingTechLevel, "startingTechLevel", techLevel);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && homework == null) homework = new List<ResearchProjectDef>();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (expertise == null) AcquireExpertise();
            if (techLevel == 0) techLevel = expertise.Keys.Aggregate((a, b) => a.techLevel > b.techLevel ? a : b).techLevel;
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

        private static TechLevel FindBGTechLevel(Pawn pawn, out TechLevel childhoodLevel, out SkillDef childhoodSkill)
        {
            TechLevel asAdult = 0;
            TechLevel asChild = 0;
            childhoodSkill = null;
            try
            {
                if (pawn.story != null)
                {
                    asChild = PawnBackgroundUtility.TechLevelByBackstory[pawn.story.childhood.identifier];
                    if (pawn.story.adulthood != null) asAdult = PawnBackgroundUtility.TechLevelByBackstory[pawn.story.adulthood.identifier];
                    var skillGains = pawn.story.childhood.skillGainsResolved;
                    if (skillGains.Count() > 1) childhoodSkill = skillGains.Aggregate((a, b) => (a.Value >= b.Value) ? a : b).Key;
                    else if (!skillGains.EnumerableNullOrEmpty()) childhoodSkill = skillGains.FirstOrDefault().Key;
                }
                if (asAdult == 0 || asChild == 0)
                {
                    FactionDef faction = pawn.Faction?.def ?? pawn.kindDef?.defaultFactionType;
                    TechLevel fallback = faction?.techLevel ?? TechLevel.Industrial;
                    if (asAdult == 0) asAdult = fallback;
                    if (asChild == 0) asChild = fallback;
                }
            }
            catch (Exception e) // if you are catching err's you might as well explain them.
            {
                Log.Warning("[HumanResources] Error determining tech level from " + pawn + "'s background: " + e.Message);
            }
            childhoodLevel = asChild;
            return asAdult;
        }

        private static bool SkillIsRelevant(SkillDef skill, TechLevel level)
        {
            //bool result = false;
            if (Extension_Research.TechsBySkill.ContainsKey(skill))
            {
                return Extension_Research.TechsBySkill[skill].Any(x => x.techLevel == level);
            }
            //string test = result ? "is":"is NOT";
            //if (!result) Log.Message("DEBUG " + skill + " is NOT relevant for " + level);
            return false;
        }

        private static float TechLikelihoodForSkill(Pawn pawn, List<SkillDef> skills, int slots, int pass, SkillDef highestSkill = null)
        {
            List<SkillDef> unskilled = (from x in pawn.skills.skills
                                        where x.Level < 2
                                        select x.def).ToList();
            float chance = ((slots - pass) / slots) * 100f;
            if (highestSkill != null)
            {
                if (highestSkill != null && skills.Contains(highestSkill)) return chance;
                else if (skills.All(s => unskilled.Contains(s))) return (100f - chance) / 10;
            }
            return 100f - chance;
        }

        private static bool TechPool(bool isPlayer, ResearchProjectDef tech, TechLevel TechLevel, FactionDef faction, bool strict = false)
        {
            if (!isPlayer) return tech.techLevel == TechLevel;
            else
            {
                if ((TechPoolIncludesTechLevel || strict) && tech.techLevel == TechLevel) return true;
                if (!strict)
                {
                    if (TechPoolIncludesScenario && !unlocked.startingTechs.EnumerableNullOrEmpty() && unlocked.startingTechs.Contains(tech)) return true;
                    if (TechPoolIncludesStarting && !faction.startingResearchTags.NullOrEmpty() && faction.startingResearchTags.Any())
                    {
                        foreach (ResearchProjectTagDef tag in faction.startingResearchTags)
                        {
                            return tech.tags?.Contains(tag) ?? false;
                        }
                    }
                }
            }
            return false;
        }
        private bool TestIfWeapon(ThingDef weapon, bool close)
        {
            return close ? weapon.IsMeleeWeapon : weapon.IsRangedWeapon;
        }
    }
}
