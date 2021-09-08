using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    public class TechMapping
    {
        public List<SkillDef> Skills = new List<SkillDef>();
        public List<ThingDef> Weapons = new List<ThingDef>();
        public ResearchProjectDef Tech;
        public ThingDef Stuff;

        public TechMapping(ResearchProjectDef tech)
        {
            Tech = tech;
        }

        public static implicit operator ResearchProjectDef(TechMapping techMapping)
        {
            return techMapping.Tech;
        }
    }
}
