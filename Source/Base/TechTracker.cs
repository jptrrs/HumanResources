using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HumanResources
{
    public static class TechTracker
    {
        private static List<SkillMapping> _skills;
        private static List<TechMapping> _techs;

        public static List<SkillMapping> Skills
        {
            get
            {
                if (_skills.NullOrEmpty())
                {
                    _skills = new List<SkillMapping>();
                    foreach (SkillDef def in DefDatabase<SkillDef>.AllDefs)
                    {
                        _skills.Add(new SkillMapping(def));
                    }
                }
                return _skills;
            }
        }

        private static List<TechMapping> Techs
        {
            get
            {
                if (_techs.NullOrEmpty())
                {
                    _techs = new List<TechMapping>();
                    foreach (ResearchProjectDef def in DefDatabase<ResearchProjectDef>.AllDefs)
                    {
                        _techs.Add(new TechMapping(def));
                    }
                }
                return _techs;
            }
        }
        public static SkillMapping FindSkill<T>(T query) where T : class
        {
            return FindSkills(query).FirstOrDefault();
        }

        public static IEnumerable<SkillMapping> FindSkills(Predicate<SkillMapping> query)
        {
            return FindSkills<Predicate<SkillMapping>>(query);
        }

        public static IEnumerable<SkillMapping> FindSkills<T>(T query) where T : class
        {
            if (typeof(T) == typeof(Predicate<SkillMapping>))
            {
                Predicate<SkillMapping> lookup = query as Predicate<SkillMapping>;
                return Skills.Where(x => lookup(x));
            }
            if (typeof(T) == typeof(SkillDef))
            {
                return Skills.Where(x => x.Skill == query);
            }
            if (typeof(T) == typeof(ResearchProjectDef))
            {
                return Skills.Where(x => x.Techs.Any(y => y == query));
            }
            Log.Error($"[HumanResources] Can't find a skill by {query.GetType()}!");
            return null;
        }

        public static TechMapping FindTech<T>(T query) where T : class
        {
            return FindTechs(query).FirstOrDefault();
        }

        public static IEnumerable<TechMapping> FindTechs(Predicate<TechMapping> query)
        {
            return FindTechs<Predicate<TechMapping>>(query);
        }

        public static IEnumerable<TechMapping> FindTechs<T>(T query) where T : class
        {
            if (typeof(T) == typeof(Predicate<TechMapping>))
            {
                Predicate<TechMapping> lookup = query as Predicate<TechMapping>;
                return Techs.Where(x => lookup(x));
            }
            if (typeof(T) == typeof(SkillDef))
            {
                return Techs.Where(x => x.Skills.Any(y => y == query));
            }
            if (typeof(T) == typeof(ThingDef))
            {
                if (query is ThingDef t && t.IsWithinCategory(TechDefOf.Knowledge))
                {
                    return Techs.Where(x => x.Stuff == query);
                }
                return Techs.Where(x => x.Weapons.Any(y => y == query));
            }
            if (typeof(T) == typeof(ResearchProjectDef))
            {
                return Techs.Where(x => x.Tech == query);
            }
            Log.Error($"[HumanResources] Can't find a tech by {query.GetType()}!");
            return null;
        }

        public static IEnumerable<SkillDef> GetSkillBiasAndReset()
        {
            foreach (var skill in FindSkills(x => x.relevant))
            {
                yield return skill;
                skill.relevant = false;
            }
        }

        public static bool GetSkillRelevance(SkillDef skill)
        {
            return FindSkill(skill).relevant;
        }

        public static void LinkTechAndSkills(ResearchProjectDef tech, List<SkillDef> relevantSkills)
        {
            FindTech(tech).Skills.AddRange(relevantSkills);
            foreach (var skill in relevantSkills)
            {
                FindSkill(skill).Techs.Add(tech);
            }
        }

        public static void SetSkillRelevance(SkillDef skill, bool value)
        {
            FindSkill(skill).relevant = value;
        }
    }
}
