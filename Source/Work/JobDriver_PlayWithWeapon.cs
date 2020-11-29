using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HumanResources
{
    class JobDriver_PlayWithWeapon : JobDriver_WatchBuilding
    {
        protected int ticksSpentAlready = 0;
        protected Verb verbToUse;

        public override void ExposeData()
        {
            Scribe_References.Look<Verb>(ref verbToUse, "verbToUse");
            base.ExposeData();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            verbToUse = pawn.TryGetAttackVerb(pawn.equipment?.Primary, true);
            return base.TryMakePreToilReservations(errorOnFailed);
        }

        protected override void WatchTickAction()
        {
            ticksSpentAlready++;

            //pawn posture
            LocalTargetInfo target = pawn.jobs.curJob.GetTarget(TargetIndex.A);
            Stance_Cooldown stance = pawn.stances.curStance as Stance_Cooldown;
            if (stance != null) stance.ticksLeft++;
            else pawn.stances.SetStance(new Stance_Cooldown(2, target, verbToUse));

            //sound:
            if (verbToUse.verbProps != null && verbToUse.verbProps.warmupTime > 0)
            {
                if ((ticksSpentAlready % verbToUse.verbProps.AdjustedFullCycleTime(verbToUse, pawn).SecondsToTicks()) == 0)
                {
                    if (verbToUse.verbProps.soundCast != null)
                    {
                        verbToUse.verbProps.soundCast.PlayOneShot(new TargetInfo(base.pawn.Position, base.pawn.Map, false));
                    }
                    if (verbToUse.verbProps.soundCastTail != null)
                    {
                        verbToUse.verbProps.soundCastTail.PlayOneShotOnCamera(base.pawn.Map);
                    }
                }
            }
            base.WatchTickAction();
        }
    }
}
