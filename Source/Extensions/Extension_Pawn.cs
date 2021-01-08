using Verse;

namespace HumanResources
{
    public static class Extension_Pawn
	{
		public static bool TechBound(this Pawn pawn)
        {
			return (pawn.IsColonist || (HarmonyPatches.PrisonLabor && pawn.IsPrisoner)) && pawn.TryGetComp<CompKnowledge>() != null;
		}

		public static bool IsGuest(this Pawn pawn)
        {
			return Hospitality_Patches.active && Hospitality_Patches.IsGuestExternal(pawn);
        }
	}
} 
