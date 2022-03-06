using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;

namespace HumanResources
{
    class PUAH_Patch
    {
        private static Type 
            patchType = AccessTools.TypeByName("PickUpAndHaul.JobDriver_UnloadYourHauledInventory"),
            CompHauledToInventoryType = AccessTools.TypeByName("PickUpAndHaul.CompHauledToInventory");
        private static MethodInfo
            FirstUnloadableThingInfo = AccessTools.Method(patchType, "FirstUnloadableThing", new Type[] { typeof(Pawn) }),
            GetHashSetInfo = AccessTools.Method(CompHauledToInventoryType, "GetHashSet"),
            TryGetCompHauledToInventoryInfo = AccessTools.Method(typeof(ThingCompUtility), "TryGetComp", new Type[] { typeof(Thing)}).MakeGenericMethod(new Type[] { CompHauledToInventoryType });
        private static PropertyInfo ThingCountThingInfo = AccessTools.Property(typeof(ThingCount), "Thing");
        private static FieldInfo countToDropInfo = AccessTools.Field(patchType, "_countToDrop");

        public static void Execute(Harmony instance)
        {
            instance.Patch(AccessTools.Method(patchType, "MakeNewToils"),
                new HarmonyMethod(typeof(PUAH_Patch), nameof(MakeNewToils_Prefix)), new HarmonyMethod(typeof(PUAH_Patch), nameof(MakeNewToils_Postfix)), null);
        }

        public static void MakeNewToils_Prefix(JobDriver __instance, Pawn ___pawn, out bool __state)
        {
            //If target A is a bookstore, clones it as target B, forcing it to be considered as a container further down the line. Also, sets up countToDrop.
            var job = __instance.job;
            var targetA = job.GetTarget(TargetIndex.A);
            var targetB = job.GetTarget(TargetIndex.B);
            if (targetB.HasThing || !targetA.HasThing || !(targetA.Thing is Building_BookStore shelf))
            {
                __state = false;
                return;
            }
            job.SetTarget(TargetIndex.B, shelf);
            countToDropInfo.SetValue(__instance, 1);
            __state = true;
        }

        public static IEnumerable<Toil> MakeNewToils_Postfix(IEnumerable<Toil> toils, JobDriver __instance, Pawn ___pawn, bool __state)
        {
            //If necessary, replaces the second toil so it doesn't mess with the target B. 
            var job = __instance.job;
            int i = 0;
            foreach (var toil in toils)
            {
                int n = i;
                if (!__state || i != 1)
                {
                    yield return toil;
                    i++;
                    continue;
                }
                yield return new Toil()
                {
                    initAction = () =>
                    {
                        var unloadableThing = (ThingCount)FirstUnloadableThingInfo.Invoke(__instance, new object[] { ___pawn });
                        var takenToInventory = TryGetCompHauledToInventoryInfo.Invoke(__instance, new object[] { ___pawn });
                        HashSet<Thing> carriedThing = (HashSet<Thing>)GetHashSetInfo.Invoke(takenToInventory, new object[] { });
                        if (unloadableThing.Count == 0 && carriedThing.Count == 0)
                        {
                            job.GetCachedDriver(___pawn).EndJobWith(JobCondition.Succeeded);
                        }
                        if (unloadableThing.Count != 0)
                        {
                            Thing item = (Thing)ThingCountThingInfo.GetValue(unloadableThing);
                            if (item != null || item.def == TechDefOf.TechBook)
                            {
                                job.SetTarget(TargetIndex.A, item);
                            }
                        }
                    }
                };
                i++;
            }
        }
    }
}
