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

            listingStandard.CheckboxLabeled("TechPoolModeTitle".Translate(), ref HumanResourcesSettings.TechPoolTitle, "TechPoolModeDesc".Translate());
            listingStandard.CheckboxLabeled("TechPoolIncludesStartingTitle".Translate(), ref HumanResourcesSettings.TechPoolIncludesStarting, "TechPoolModeDesc".Translate());
            listingStandard.CheckboxLabeled("TechPoolIncludesTechLevelTitle".Translate(), ref HumanResourcesSettings.TechPoolIncludesTechLevel, "TechPoolModeDesc".Translate());
            listingStandard.CheckboxLabeled("TechPoolIncludesBackgroundTitle".Translate(), ref HumanResourcesSettings.TechPoolIncludesBackground, "TechPoolModeDesc".Translate());
            listingStandard.CheckboxLabeled("TechPoolIncludesScenarioTitle".Translate(), ref HumanResourcesSettings.TechPoolIncludesScenario, "TechPoolModeDesc".Translate());
            listingStandard.CheckboxLabeled("FreeScenarioWeaponsTitle".Translate(), ref HumanResourcesSettings.FreeScenarioWeapons, "TechPoolModeDesc".Translate());
            listingStandard.CheckboxLabeled("LearnMeleeWeaponsByGroupTitle".Translate(), ref HumanResourcesSettings.LearnMeleeWeaponsByGroup, "TechPoolModeDesc".Translate());
            listingStandard.CheckboxLabeled("LearnRangedWeaponsByGroupTitle".Translate(), ref HumanResourcesSettings.LearnRangedWeaponsByGroup, "TechPoolModeDesc".Translate());
            listingStandard.CheckboxLabeled("RequireTrainingForSingleUseWeaponsTitle".Translate(), ref HumanResourcesSettings.RequireTrainingForSingleUseWeapons, "TechPoolModeDesc".Translate());
            listingStandard.CheckboxLabeled("EnableJoyGiverTitle".Translate(), ref HumanResourcesSettings.EnableJoyGiver, "TechPoolModeDesc".Translate());
            listingStandard.CheckboxLabeled("ResearchSpeedTiedToDifficultyTitle".Translate(), ref HumanResourcesSettings.ResearchSpeedTiedToDifficulty, "TechPoolModeDesc".Translate());
            listingStandard.CheckboxLabeled("StudySpeedTiedToDifficultyTitle".Translate(), ref HumanResourcesSettings.StudySpeedTiedToDifficulty, "TechPoolModeDesc".Translate());

            listingStandard.EnumSelector(ref HumanResourcesSettings.WeaponPoolMode, "WeaponPoolModeTitle".Translate(), "WeaponPoolMode_");

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
