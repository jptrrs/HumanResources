using RimWorld;
using Verse;

namespace HumanResources
{
    [DefOf]
    public static class TechDefOf
    {
        public static RecipeDef
            LearnTech,
            LearnTechDigital,
            DocumentTech,
            DocumentTechDigital,
            ScanBook,
            TrainWeaponShooting,
            TrainWeaponMelee,
            PracticeWeaponShooting,
            PracticeWeaponMelee,
            ExperimentWeaponShooting,
            ExperimentWeaponMelee;
        public static ThingCategoryDef
            Knowledge,
            WeaponsRanged,
            WeaponsMelee;
        //public static StuffCategoryDef
        //    Neolithic,
        //    Medieval,
        //    Industrial,
        //    Spacer,
        //    Ultra,
        //    Archotech,
        //    Anomaly;
        public static ThingDef
            TechBook,
            TechDrive,
            UnfinishedTechBook,
            HardWeapons,
            EasyWeapons,
            //LowTechCategories,
            //HiTechCategories,
            //HiTechCutoff,
            NetworkTerminal,
            NetworkServer;
        public static WorkTypeDef HR_Learn;
        public static JoyGiverDef
            Play_Shooting,
            Play_MartialArts;
    }
}
