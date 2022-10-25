using RimWorld;
using Verse;
using Verse.AI;

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
            LearningUtility.WeaponTrainingAnimation(pawn, pawn.jobs.curJob.GetTarget(TargetIndex.A), verbToUse, ticksSpentAlready);
            base.WatchTickAction();
        }
    }
}
