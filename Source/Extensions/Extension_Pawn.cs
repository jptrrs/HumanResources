using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    public static class Extension_Pawn
	{
		public static bool TechBound(this Pawn pawn)
        {
			return (pawn.IsColonist || (HarmonyPatches.PrisonLabor && pawn.IsPrisoner)) && pawn.TryGetComp<CompKnowledge>() != null;
		}
	}
} 
