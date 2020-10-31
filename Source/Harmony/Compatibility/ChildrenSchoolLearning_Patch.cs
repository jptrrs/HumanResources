using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace HumanResources
{
    class ChildrenSchoolLearning_Patch
    {
        public static void Execute(Harmony instance)
        {
            Type SchoolDefOfType = AccessTools.TypeByName("School.SchoolDefOf");
            TechDefOf.HR_Learn = (WorkTypeDef)AccessTools.Field(SchoolDefOfType, "Study").GetValue(new object());

            instance.Patch(AccessTools.Method(typeof(WorkTypeDefsUtility), "GenerateImpliedDefs_PreResolve"),
                new HarmonyMethod(typeof(ChildrenSchoolLearning_Patch), nameof(GenerateImpliedDefs_Prefix)), null, null);
        }

        public static void GenerateImpliedDefs_Prefix()
        {
            AccessTools.Method(typeof(DefDatabase<WorkTypeDef>), "Remove").Invoke(new ModBaseHumanResources(), new object[] { DefDatabase<WorkTypeDef>.GetNamed("HR_Learn") });
        }
    }
}
