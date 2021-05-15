using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HumanResources
{
    using static ResearchTree_Patches;
    public static class ResearchTreeHelper
    {
        public static FloatMenuOption SelectforVanillaResearch(ResearchProjectDef tech)
        {
            if (IsQueued(tech))
            {
                return new FloatMenuOption("CancelResearchWithTheSuperComputer",
                    delegate () { Dequeue(tech); },
                    MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            return new FloatMenuOption("ResearchWithTheSuperComputer",
                delegate () { Enqueue(tech); },
                MenuOptionPriority.Default, null, null, 0f, null, null);
        }

        public static void Enqueue(ResearchProjectDef tech)
        {
            if (!IsQueued(tech))
            {
                EnqueueRange(GetRequiredRecursive(tech, x => !x.IsFinished && !IsQueued(x)));
            }
            //else
            //{
            //    Dequeue(tech);
            //}
        }

        public static List<ResearchProjectDef> GetRequiredRecursive(ResearchProjectDef tech, Predicate<ResearchProjectDef> filter)
        {
            var parents = tech.prerequisites?.Where(x => filter(x));
            if (parents == null) return new List<ResearchProjectDef>();
            var allParents = new List<ResearchProjectDef>(parents);
            foreach (var parent in parents) allParents.AddRange(GetRequiredRecursive(parent, filter));
            return allParents.Distinct().ToList();
        }
    }
}
