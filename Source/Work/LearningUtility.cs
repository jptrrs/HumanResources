using Verse;
using Verse.Sound;

namespace HumanResources
{
    class LearningUtility
    {
        public static float warmupSoundFactor = 1.5f;

        public static void WeaponTrainingAnimation(Pawn pawn, LocalTargetInfo target, Verb verbToUse, int ticksSpentAlready)
        {
            Stance_Cooldown stance = pawn.stances.curStance as Stance_Cooldown;
            if (stance != null) stance.ticksLeft++;
            else pawn.stances.SetStance(new Stance_Cooldown(2, target, verbToUse));
            if (verbToUse.verbProps != null && verbToUse.verbProps.warmupTime > 0)
            {
                int warmup = (int)(verbToUse.verbProps.AdjustedFullCycleTime(verbToUse, pawn).SecondsToTicks() * warmupSoundFactor);
                if ((ticksSpentAlready % warmup) == 0)
                {
                    if (verbToUse.verbProps.soundCast != null)
                    {
                        verbToUse.verbProps.soundCast.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
                    }
                    if (verbToUse.verbProps.soundCastTail != null)
                    {
                        verbToUse.verbProps.soundCastTail.PlayOneShotOnCamera(pawn.Map);
                    }
                }
            }
        }
    }
}
