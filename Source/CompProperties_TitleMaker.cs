using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace HumanResources
{
    public class CompProperties_TitleMaker : CompProperties
    {
        public CompProperties_TitleMaker()
        {
            compClass = typeof(CompTitleMaker);
        }
    }
}
