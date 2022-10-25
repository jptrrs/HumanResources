using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HumanResources
{
    public enum FactionWeaponPool { Both, TechLevel, Scenario }

    public class HumanResourcesSettings : ModSettings
    {

        public static bool
            TechPoolTitle,
            TechPoolIncludesStarting,
            TechPoolIncludesTechLevel,
            TechPoolIncludesBackground,
            TechPoolIncludesScenario,
            FreeScenarioWeapons,
            LearnMeleeWeaponsByGroup,
            LearnRangedWeaponsByGroup,
            RequireTrainingForSingleUseWeapons,
            EnableJoyGiver,
            ResearchSpeedTiedToDifficulty,
            StudySpeedTiedToDifficulty,
            FullStartupReport;
        //IndividualTechsReport; // Not sure what this does, not referenced anywhere

        public static FactionWeaponPool WeaponPoolMode;
        public static bool WeaponPoolIncludesScenario => WeaponPoolMode != FactionWeaponPool.TechLevel;
        public static bool WeaponPoolIncludesTechLevel => WeaponPoolMode < FactionWeaponPool.Scenario;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref TechPoolTitle, "TechPoolTitle", false);
            Scribe_Values.Look(ref TechPoolIncludesStarting, "TechPoolIncludesStarting", true);
            Scribe_Values.Look(ref TechPoolIncludesTechLevel, "TechPoolIncludesTechLevel", true);
            Scribe_Values.Look(ref TechPoolIncludesBackground, "TechPoolIncludesBackground", false);
            Scribe_Values.Look(ref TechPoolIncludesScenario, "TechPoolIncludesScenario", true);
            Scribe_Values.Look(ref FreeScenarioWeapons, "FreeScenarioWeapons", false);
            Scribe_Values.Look(ref LearnMeleeWeaponsByGroup, "LearnMeleeWeaponsByGroup", false) ;
            Scribe_Values.Look(ref LearnRangedWeaponsByGroup, "LearnRangedWeaponsByGroup", true);
            Scribe_Values.Look(ref RequireTrainingForSingleUseWeapons, "RequireTrainingForSingleUseWeapons", false);
            Scribe_Values.Look(ref EnableJoyGiver, "EnableJoyGiver", true);
            Scribe_Values.Look(ref ResearchSpeedTiedToDifficulty, "ResearchSpeedTiedToDifficulty", true);
            Scribe_Values.Look(ref StudySpeedTiedToDifficulty, "StudySpeedTiedToDifficulty", true);
            Scribe_Values.Look(ref FullStartupReport, "FullStartupReport", false);
            Scribe_Values.Look<FactionWeaponPool>(ref WeaponPoolMode, "WeaponPoolMode", FactionWeaponPool.Scenario);
            //Scribe_Values.Look(ref IndividualTechsReport, "IndividualTechsReport");
            base.ExposeData();
        }


        

    }
}
