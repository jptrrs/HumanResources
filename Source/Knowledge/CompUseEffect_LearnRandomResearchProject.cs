using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace HumanResources
{
    public class CompUseEffect_LearnRandomResearchProject : CompUseEffect
    {
        private static PropertyInfo ResearchableInfo = AccessTools.Property(typeof(ResearchProjectDef), "PlayerHasAnyAppropriateResearchBench");

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            CompKnowledge techComp = usedBy.TryGetComp<CompKnowledge>();
            var candidates = techComp.homework.Where(x => !x.IsFinished && (x.requiredResearchBuilding == null || (bool)ResearchableInfo.GetValue(x)));
            List<ResearchProjectDef> means = new List<ResearchProjectDef>();
            foreach (ResearchProjectDef tech in candidates)
            {
                if (tech.prerequisites != null)
                {
                    means.AddRange(candidates.Where(x => tech.prerequisites.Contains(x)));
                }
            }
            ResearchProjectDef result = candidates.Except(means).RandomElement();
            if (techComp.LearnTech(result) && result.prerequisites != null)
            {
                techComp.homework.RemoveAll(x => result.prerequisites.Contains(x));
            }
        }

        public override bool CanBeUsedBy(Pawn p, out string failReason)
        {
            failReason = null;
            CompKnowledge techComp = p.TryGetComp<CompKnowledge>();
            if (techComp != null)
            {
                if (!techComp.homework.NullOrEmpty())
                {
                    return techComp.homework.Any(x => !x.IsFinished);
                }
            }
            failReason = "NoActiveResearchProjectToFinish".Translate();
            return false;
        }
    }
}
