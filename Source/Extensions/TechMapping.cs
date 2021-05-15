using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    public class TechMapping
    {
        public List<Pawn> Assigned = new List<Pawn>();
        public List<SkillDef> Skills = new List<SkillDef>();
        public List<ThingDef> Weapons = new List<ThingDef>();
        public ResearchProjectDef Tech;

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
