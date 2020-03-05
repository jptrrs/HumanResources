using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Reflection.Emit;

namespace HumanResources
{
    [HarmonyPatch]
    public class HarmonyReverse
    {
        public ResearchProjectDef project;

        // When reverse patched, StringOperation will contain all the
        // code from the original including the Join() but not the +n
        //
        // Basically
        // var parts = original.Split('-').Reverse().ToArray();
        // return string.Join("", parts)
        //
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ResearchManager), "ResearchPerformed")]
        public static void ResearchPerformed(float amount, Pawn researcher)
        //public static string StringOperation(string original)
        {
            // This inner transpiler will be applied to the original and
            // the result will replace this method
            //
            // That will allow this method to have a different signature
            // than the original and it must match the transpiled result
            //
            //Log.Warning("ReserarchPerformed reverse patch called.");

            //Log.Warning("ReserarchPerformed reverse patch called. amount is " + amount + ", researcher is" + researcher + ", project is" + project);

            //IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            //{
            //    Log.Message("Transpiling ResearchPerformed...");
            //    FieldInfo fieldInfo = AccessTools.Field(typeof(ResearchManager), "currentProj");
            //    int testCount = 0;

            //    var codeInstructions = instructions as CodeInstruction[] ?? instructions.ToArray();
            //    foreach (CodeInstruction instruction in codeInstructions)
            //    {
            //        //Log.Message("procurando linha " + OpCodes.Ldtoken + " " + bedInfo);
            //        if (instruction.opcode == OpCodes.Ldfld && instruction.operand == fieldInfo)
            //        {
            //            testCount++;
            //            //Log.Message("...instruction found, patching " + fieldInfo + " into " + project);
            //            //yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: project);
            //        }
            //        //else
            //        //{
            //        //    yield return instruction;
            //        //}
            //    }


            //    Log.Warning("... instructions located: " + testCount);
            //    return codeInstructions.AsEnumerable();

            //    //return instructions;
            //}


            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo fieldInfo = AccessTools.Field(typeof(ResearchManager), "currentProj");
                var list = Transpilers.Manipulator(instructions,
                    i => i.opcode == OpCodes.Ldfld && i.operand == fieldInfo,
                    i => i.opcode = OpCodes.Ldfld
                    ).ToList();
                //var mJoin = SymbolExtensions.GetMethodInfo(() => string.Join(null, null));
                //var idx = list.FindIndex(item => item.opcode == OpCodes.Call && item.operand as MethodInfo == mJoin);
                //list.RemoveRange(idx + 1, list.Count - (idx + 1));
                Log.Message(list.ToStringSafeEnumerable());
                return list.AsEnumerable();
            }

            //IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            //{
            //    FieldInfo fieldInfo = AccessTools.Field(typeof(ResearchManager), "currentProj");
            //    var list = Transpilers.Manipulator(instructions,
            //             item => item.opcode == OpCodes.Ldarg_1,
            //             item => item.opcode = OpCodes.Ldarg_0
            //             ).ToList();
            //    var mJoin = SymbolExtensions.
                    
            //        GetMethodInfo(() => string.Join(null, null));
            //    var idx = list.FindIndex(item => item.opcode == OpCodes.Call && item.operand as MethodInfo == mJoin);
            //    list.RemoveRange(idx + 1, list.Count - (idx + 1));
            //    return list.AsEnumerable();
            //}

            // make compiler happy
            _ = Transpiler(null);
            //return 0f;
        }
    }
}
