using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HumanResources
{
    using static ResearchTree_Patches;
    public static class ResearchTreeHelper
    {
        public static ThingDef VFE_Supercomputer = HarmonyPatches.VFEM ? DefDatabase<ThingDef>.GetNamed("VFE_Supercomputer") : null;
        public static bool QueueAvailable => HarmonyPatches.VFEM && Find.Maps.Any(x => x.listerBuildings.ColonistsHaveBuilding(VFE_Supercomputer));

        public static FloatMenuOption SelectforVanillaResearch(ResearchProjectDef tech)
        {
            return new FloatMenuOption($"{TechStrings.headerResearch}: {VFE_Supercomputer.LabelCap}",
                delegate () { Enqueue(tech); },
                MenuOptionPriority.High, null, null, 0f, null, null);
        }

        public static void Enqueue(ResearchProjectDef tech)
        {
            if (!IsQueued(tech))
            {
                EnqueueRange(GetRequiredRecursive(tech, x => !x.IsFinished && !IsQueued(x)).Concat(tech));
            }
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
