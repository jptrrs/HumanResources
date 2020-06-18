using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace HumanResources
{
    public class CompProperties_Knowledge : CompProperties
    {
        public CompProperties_Knowledge()
        {
            compClass = typeof(CompKnowledge);
        }
    }
}
