using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    public static class Extension_Pawn
	{
		public static bool CanContribute(this Pawn pawn)
        {
			return pawn.IsColonist || (HarmonyPatches.PrisonLabor && pawn.IsPrisoner);
		}
	}
} 
