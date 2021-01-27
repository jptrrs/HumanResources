using RimWorld;
using Verse;

namespace HumanResources
{
    [DefOf]
    public static class TechDefOf
    {
        public static RecipeDef
            LearnTech,
            DocumentTech,
            DocumentTechDigital,
            ScanBook,
            TrainWeaponShooting,
            TrainWeaponMelee,
            PracticeWeaponShooting,
            PracticeWeaponMelee,
            ExperimentWeaponShooting,
            ExperimentWeaponMelee;
        public static ThingCategoryDef Knowledge;
        public static StuffCategoryDef Technic;
        public static ThingDef 
            TechBook,
            UnfinishedTechBook,
            WeaponsNotBasic,
            WeaponsAlwaysBasic,
            NetworkTerminal,
            NetworkServer;
        public static WorkTypeDef HR_Learn;
        public static JoyGiverDef
            Play_Shooting,
            Play_MartialArts;
    }
}
