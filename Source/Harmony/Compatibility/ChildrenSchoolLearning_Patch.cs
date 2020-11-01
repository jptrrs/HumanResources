using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace HumanResources
{
    class ChildrenSchoolLearning_Patch
    {
        private static FieldInfo PawnTableDefInfo = AccessTools.Field(typeof(PawnTable), "def"); 

        public static void Execute(Harmony instance)
        {
            Type SchoolDefOfType = AccessTools.TypeByName("School.SchoolDefOf");
            TechDefOf.HR_Learn = (WorkTypeDef)AccessTools.Field(SchoolDefOfType, "Study").GetValue(new object());
            AccessTools.Method(typeof(DefDatabase<PawnColumnDef>), "Remove").Invoke(instance, new object[] { DefDatabase<PawnColumnDef>.AllDefs.Where(x => x.workType == DefDatabase<WorkTypeDef>.GetNamed("HR_Learn")).FirstOrDefault() });
            AccessTools.Method(typeof(DefDatabase<WorkTypeDef>), "Remove").Invoke(instance, new object[] { DefDatabase<WorkTypeDef>.GetNamed("HR_Learn") });
            instance.Patch(AccessTools.Method(typeof(MainTabWindow_Work), "CreateTable"), null, new HarmonyMethod(typeof(ChildrenSchoolLearning_Patch), nameof(CreateTable_Postfix)), null);
        }

        public static void CreateTable_Postfix(PawnTable __result)
        {
            PawnTableDef def = (PawnTableDef)PawnTableDefInfo.GetValue(__result);
            int idx = def.columns.FindIndex(x => x.workType != null && x.workType.defName == "HR_Learn");
            for (int i = idx + 1; i < def.columns.Count; i++)
            {
                def.columns[i].moveWorkTypeLabelDown = !def.columns[i].moveWorkTypeLabelDown;
            }
            def.columns.RemoveAt(idx);
        }
    }
}
