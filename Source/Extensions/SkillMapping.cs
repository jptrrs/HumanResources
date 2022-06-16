using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    public class SkillMapping
    {
        public SkillDef Skill;
        public List<ResearchProjectDef> Techs = new List<ResearchProjectDef>();
        public bool relevant;
        private Predicate<ThingDef> criteria;
        private List<string> hints;

        private readonly List<string>
            AnimalHints = new List<string>() { "animal" },
            CraftingHints = new List<string>() { "tool", "armor", "armour", "cloth" },
            IntellectualHints = new List<string>() { "manage" },
            MiningHints = new List<string>() { "scanner", "explosive", "terraform" },
            MedicineHints = new List<string>() { "sterile", "medical", "medicine", "cryptosleep", "prostheses", "implant", "organs", "surgery", "biosculpter", "neural" },
            PlantsHints = new List<string>() { "irrigation", "soil", "hydroponic", "farm" },
            ArtisticHints = new List<string>() { "music", "sculpture" };

        public List<string> Hints
        {
            get
            {
                if (hints == null)
                {
                    hints = new List<string>();
                    if (Skill == SkillDefOf.Animals) hints.AddRange(AnimalHints);
                    else if (Skill == SkillDefOf.Crafting) hints.AddRange(CraftingHints);
                    else if (Skill == SkillDefOf.Intellectual) hints.AddRange(IntellectualHints);
                    else if (Skill == SkillDefOf.Mining) hints.AddRange(MiningHints);
                    else if (Skill == SkillDefOf.Medicine) hints.AddRange(MedicineHints);
                    else if (Skill == SkillDefOf.Plants) hints.AddRange(PlantsHints);
                    else if (Skill == SkillDefOf.Artistic) hints.AddRange(ArtisticHints);
                }
                return hints;
            }
        }

        public Predicate<ThingDef> Criteria
        {
            get
            {
                if (criteria == null)
                {
                    if (Skill == SkillDefOf.Shooting) criteria = (thing) => thing.IsRangedWeapon | thing.designationCategory == DesignationCategoryDefOf.Security;
                    else if (Skill == SkillDefOf.Melee) criteria = (thing) => thing.IsMeleeWeapon | thing.designationCategory == DesignationCategoryDefOf.Security;
                    else if (Skill == SkillDefOf.Construction) criteria = (thing) => thing.BuildableByPlayer;
                    else if (Skill == SkillDefOf.Cooking) criteria = (thing) => thing.ingestible.IsMeal | thing.building.isMealSource;
                    else if (Skill == SkillDefOf.Plants) criteria = (thing) => thing.plant != null;
                    else if (Skill == SkillDefOf.Crafting) criteria = (thing) => thing.IsApparel | thing.IsWeapon;
                    else if (Skill == SkillDefOf.Artistic) criteria = (thing) => thing.IsArt | thing.IsWithinCategory(ThingCategoryDefOf.BuildingsArt);
                    else if (Skill == SkillDefOf.Medicine) criteria = (thing) => thing.IsMedicine | thing.IsDrug;
                }
                return criteria;
            }
        }

        public SkillMapping(SkillDef skill)
        {
            Skill = skill;
        }

        public static implicit operator SkillDef(SkillMapping skillMapping)
        {
            return skillMapping.Skill;
        }

        public static implicit operator bool(SkillMapping skillMapping)
        {
            return skillMapping.relevant;
        }
    }
}
