using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;

namespace HumanResources
{
    public static class Royalty_Patches
    {
        public static void Execute(Harmony instance)
        {
            instance.Patch(AccessTools.Method(typeof(CompTechprint), "CompFloatMenuOptions"),
                new HarmonyMethod(typeof(Royalty_Patches), nameof(CompFloatMenuOptions_Prefix)), null, null);
            instance.Patch(AccessTools.Method(typeof(JobDriver_ApplyTechprint), "TryMakePreToilReservations"),
                new HarmonyMethod(typeof(Royalty_Patches), nameof(TryMakePreToilReservations_Prefix)), null, null);
        }

        private static PropertyInfo TechprintInfo = AccessTools.Property(typeof(JobDriver_ApplyTechprint), "Techprint");

        public static bool CompFloatMenuOptions_Prefix(JobDriver_ApplyTechprint __instance, bool errorOnFailed, ref bool __result)
        {
            __result = __instance.pawn.Reserve(__instance.job.GetTarget(TargetIndex.A).Thing, __instance.job, 1, -1, null, errorOnFailed) && __instance.pawn.Reserve((Thing)TechprintInfo.GetValue(__instance), __instance.job, 1, -1, null, errorOnFailed);
            return false;
        }

        public static bool TryMakePreToilReservations_Prefix(ThingComp __instance, Pawn selPawn, ref IEnumerable<FloatMenuOption> __result)
        {
            if (!(selPawn.WorkTypeIsDisabled(WorkTypeDefOf.Research) || selPawn.WorkTagIsDisabled(WorkTags.Intellectual)) && selPawn.CanReach(__instance.parent, PathEndMode.ClosestTouch, Danger.Some, false, TraverseMode.ByPawn) && selPawn.CanReserve(__instance.parent, 1, -1, null, false))
            {
                Log.Warning("Ei!");
                List<FloatMenuOption> modified = new List<FloatMenuOption>();
                Thing thing = GenClosest.ClosestThingReachable(selPawn.Position, selPawn.Map, ThingRequest.ForGroup(ThingRequestGroup.ResearchBench), PathEndMode.InteractionCell, TraverseParms.For(selPawn, Danger.Some, TraverseMode.ByPawn, false), 9999f, (Thing t) => t is Building_WorkTable && selPawn.CanReserve(t, 1, -1, null, false), null, 0, -1, false, RegionType.Set_Passable, false);
                Job job = null;
                if (thing != null)
                {
                    job = JobMaker.MakeJob(JobDefOf.ApplyTechprint);
                    job.targetA = thing;
                    job.targetB = __instance.parent;
                    job.targetC = thing.Position;
                }
                modified.Add(new FloatMenuOption("ApplyTechprint".Translate(__instance.parent.Label).CapitalizeFirst(), delegate ()
                {
                    if (job == null)
                    {
                        Messages.Message("MessageNoResearchBenchForTechprint".Translate(), MessageTypeDefOf.RejectInput, true);
                        return;
                    }
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }, MenuOptionPriority.Default, null, null, 0f, null, null));
                __result = modified.AsEnumerable();
                return false;
            }
            return true;
        }
    }
}
