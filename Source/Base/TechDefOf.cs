using RimWorld;
using Verse;

namespace HumanResources
{
    [DefOf]
    public static class TechDefOf
    {
        static TechDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RecipeDefOf));
        }
        public static RecipeDef LearnTech;
        public static RecipeDef DocumentTech;
        public static RecipeDef ResearchTech;
        public static RecipeDef TrainWeaponShooting;
        public static RecipeDef TrainWeaponMelee;
        public static RecipeDef PracticeShooting;
        public static RecipeDef PracticeMelee;
        public static ThingCategoryDef Knowledge;
        public static StuffCategoryDef Technic;
        public static ThingDef TechBook;
        public static ThingDef UnfinishedTechBook;
    }
}
