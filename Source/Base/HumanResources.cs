using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace HumanResources
{
    public class HumanResources : Mod
    {
        private HumanResourcesSettings settings;

        public HumanResources(ModContentPack content) : base(content)
        {
            settings = GetSettings<HumanResourcesSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            //listingStandard.CheckboxLabeled("TechPoolModeTitle".Translate(), ref HumanResourcesSettings.TechPoolTitle, "TechPoolModeDesc".Translate());
            listingStandard.Label((string)"TechPoolModeTitle".Translate(), tooltip: (string)"TechPoolModeDesc".Translate());
            listingStandard.CheckboxLabeled((string)"TechPoolIncludesStartingTitle".Translate(), ref HumanResourcesSettings.TechPoolIncludesStarting, (string)"TechPoolIncludesStartingDesc".Translate());
            listingStandard.CheckboxLabeled((string)"TechPoolIncludesTechLevelTitle".Translate(), ref HumanResourcesSettings.TechPoolIncludesTechLevel, (string)"TechPoolIncludesTechLevelDesc".Translate());
            listingStandard.CheckboxLabeled((string)"TechPoolIncludesBackgroundTitle".Translate(), ref HumanResourcesSettings.TechPoolIncludesBackground, (string)"TechPoolIncludesBackgroundDesc".Translate());
            listingStandard.CheckboxLabeled((string)"TechPoolIncludesScenarioTitle".Translate(), ref HumanResourcesSettings.TechPoolIncludesScenario, (string)"TechPoolIncludesScenarioDesc".Translate());
            listingStandard.CheckboxLabeled((string)"FreeScenarioWeaponsTitle".Translate(), ref HumanResourcesSettings.FreeScenarioWeapons, (string)"FreeScenarioWeaponsDesc".Translate());
            listingStandard.CheckboxLabeled((string)"LearnMeleeWeaponsByGroupTitle".Translate(), ref HumanResourcesSettings.LearnMeleeWeaponsByGroup, (string)"LearnMeleeWeaponsByGroupDesc".Translate());
            listingStandard.CheckboxLabeled((string)"LearnRangedWeaponsByGroupTitle".Translate(), ref HumanResourcesSettings.LearnRangedWeaponsByGroup, (string)"LearnRangedWeaponsByGroupDesc".Translate());
            listingStandard.CheckboxLabeled((string)"RequireTrainingForSingleUseWeaponsTitle".Translate(), ref HumanResourcesSettings.RequireTrainingForSingleUseWeapons, (string)"RequireTrainingForSingleUseWeaponsDesc".Translate());
            listingStandard.CheckboxLabeled((string)"EnableJoyGiverTitle".Translate(), ref HumanResourcesSettings.EnableJoyGiver, (string)"EnableJoyGiverDesc".Translate());
            listingStandard.CheckboxLabeled((string)"ResearchSpeedTiedToDifficultyTitle".Translate(), ref HumanResourcesSettings.ResearchSpeedTiedToDifficulty, (string)"ResearchSpeedTiedToDifficultyDesc".Translate());
            listingStandard.CheckboxLabeled((string)"StudySpeedTiedToDifficultyTitle".Translate(), ref HumanResourcesSettings.StudySpeedTiedToDifficulty, (string)"StudySpeedTiedToDifficultyDesc".Translate());

            listingStandard.EnumSelector(ref HumanResourcesSettings.WeaponPoolMode, (string)"WeaponPoolModeTitle".Translate(), "WeaponPoolMode_", valueTooltipPostfix: String.Empty);

            listingStandard.End();

            //"WeaponPoolModeTitle".Translate(), ref HumanResourcesSettings.WeaponPoolMode, "TechPoolModeDesc".Translate()


            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Human Resources";
        }
    }
}
