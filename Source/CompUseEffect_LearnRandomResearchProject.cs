using RimWorld;
using System;
using Verse;

namespace HumanResources
{
	public class CompUseEffect_LearnRandomResearchProject : CompUseEffect
	{
		public override void DoEffect(Pawn usedBy)
		{
			base.DoEffect(usedBy);
			ResearchProjectDef currentProj = Find.ResearchManager.currentProj;
			if (currentProj != null)
			{
				CompKnowledge techComp = usedBy.TryGetComp<CompKnowledge>();
				if (techComp != null) techComp.LearnTech(currentProj);
			}
		}

		public override bool CanBeUsedBy(Pawn p, out string failReason)
		{
			if (Find.ResearchManager.currentProj == null)
			{
				failReason = "NoActiveResearchProjectToFinish".Translate();
				return false;
			}
			failReason = null;
			return true;
		}
	}
}
