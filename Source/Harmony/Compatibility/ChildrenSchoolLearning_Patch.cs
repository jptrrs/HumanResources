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
        private static WorkTypeDef supressedDef = DefDatabase<WorkTypeDef>.GetNamed("HR_Learn");

        public static void Execute(Harmony instance)
        {
            Type SchoolDefOfType = AccessTools.TypeByName("School.SchoolDefOf");
            TechDefOf.HR_Learn = (WorkTypeDef)AccessTools.Field(SchoolDefOfType, "Study").GetValue(new object());
            TechDefOf.HR_Learn.alwaysStartActive = true;
            supressedDef.visible = false;
            AccessTools.Method(typeof(DefDatabase<PawnColumnDef>), "Remove").Invoke(instance, new object[] { DefDatabase<PawnColumnDef>.AllDefs.Where(x => x.workType == supressedDef).FirstOrDefault() });
            AccessTools.Method(typeof(DefDatabase<WorkTypeDef>), "Remove").Invoke(instance, new object[] { supressedDef });
            instance.Patch(AccessTools.Method(typeof(PawnTable), "PawnTableOnGUI"), new HarmonyMethod(typeof(ChildrenSchoolLearning_Patch), nameof(Prefix)), null);
        }

        [HarmonyBefore("fluffy.worktab")]
        public static void Prefix(object __instance, PawnTableDef ___def)
        {
            int idx = ___def.columns.FindIndex(x => x.workType == supressedDef);
            if (idx >= 0)
            {
                for (int i = idx + 1; i < ___def.columns.Count; i++)
                {
                    ___def.columns[i].moveWorkTypeLabelDown = !___def.columns[i].moveWorkTypeLabelDown;
                }
                ___def.columns.RemoveAt(idx);
            }
        }
    }
}
