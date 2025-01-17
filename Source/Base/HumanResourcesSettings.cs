using Verse;

namespace HumanResources
{
    public enum FactionWeaponPool { Both, TechLevel, Scenario }

    public class HumanResourcesSettings : ModSettings
    {

        public static bool
            TechPoolIncludesStarting = true,
            TechPoolIncludesTechLevel = true,
            TechPoolIncludesBackground,
            TechPoolIncludesScenario = true,
            FreeScenarioWeapons,
            LearnMeleeWeaponsByGroup,
            LearnRangedWeaponsByGroup = true,
            RequireTrainingForSingleUseWeapons,
            EnableJoyGiver = true,
            ResearchSpeedTiedToDifficulty = true,
            StudySpeedTiedToDifficulty = true,
            FullStartupReport;

        public static FactionWeaponPool WeaponPoolMode;
        public static bool WeaponPoolIncludesScenario => WeaponPoolMode != FactionWeaponPool.TechLevel;
        public static bool WeaponPoolIncludesTechLevel => WeaponPoolMode < FactionWeaponPool.Scenario;

        public override void ExposeData()
        {
            //Scribe_Values.Look(ref TechPoolTitle, "TechPoolTitle", false);
            Scribe_Values.Look<bool>(ref TechPoolIncludesStarting, "TechPoolIncludesStarting", true);
            Scribe_Values.Look<bool>(ref TechPoolIncludesTechLevel, "TechPoolIncludesTechLevel", true);
            Scribe_Values.Look<bool>(ref TechPoolIncludesBackground, "TechPoolIncludesBackground", false);
            Scribe_Values.Look<bool>(ref TechPoolIncludesScenario, "TechPoolIncludesScenario", true);
            Scribe_Values.Look<bool>(ref FreeScenarioWeapons, "FreeScenarioWeapons", false);
            Scribe_Values.Look<bool>(ref LearnMeleeWeaponsByGroup, "LearnMeleeWeaponsByGroup", false);
            Scribe_Values.Look<bool>(ref LearnRangedWeaponsByGroup, "LearnRangedWeaponsByGroup", true);
            Scribe_Values.Look<bool>(ref RequireTrainingForSingleUseWeapons, "RequireTrainingForSingleUseWeapons", false);
            Scribe_Values.Look<bool>(ref EnableJoyGiver, "EnableJoyGiver", true);
            Scribe_Values.Look<bool>(ref ResearchSpeedTiedToDifficulty, "ResearchSpeedTiedToDifficulty", true);
            Scribe_Values.Look<bool>(ref StudySpeedTiedToDifficulty, "StudySpeedTiedToDifficulty", true);
            Scribe_Values.Look<bool>(ref FullStartupReport, "FullStartupReport", false);
            Scribe_Values.Look<FactionWeaponPool>(ref WeaponPoolMode, "WeaponPoolMode", FactionWeaponPool.Scenario);
            //Scribe_Values.Look(ref IndividualTechsReport, "IndividualTechsReport");
            base.ExposeData();
        }




    }
}
