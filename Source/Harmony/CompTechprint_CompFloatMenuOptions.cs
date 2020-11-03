using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace HumanResources
{
    //[HarmonyPatch(typeof(CompTechprint), "CompFloatMenuOptions", new Type[] { typeof(Pawn) })]
    public static class CompTechprint_CompFloatMenuOptions
    {
        public static bool Prefix(ThingComp __instance, Pawn selPawn, ref IEnumerable<FloatMenuOption> __result)
        {
            if (!(selPawn.WorkTypeIsDisabled(WorkTypeDefOf.Research) || selPawn.WorkTagIsDisabled(WorkTags.Intellectual)) && selPawn.CanReach(__instance.parent, PathEndMode.ClosestTouch, Danger.Some, false, TraverseMode.ByPawn) && selPawn.CanReserve(__instance.parent, 1, -1, null, false))
            {
                List<FloatMenuOption> modified = new List<FloatMenuOption>();
                Thing thing = GenClosest.ClosestThingReachable(selPawn.Position, selPawn.Map, ThingRequest.ForGroup(ThingRequestGroup.ResearchBench), PathEndMode.InteractionCell, TraverseParms.For(selPawn, Danger.Some, TraverseMode.ByPawn, false), 9999f, (Thing t) => t is Building_ResearchBench && selPawn.CanReserve(t, 1, -1, null, false), null, 0, -1, false, RegionType.Set_Passable, false);
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