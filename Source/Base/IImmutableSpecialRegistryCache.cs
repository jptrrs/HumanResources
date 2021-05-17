using System;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    public interface IImmutableSpecialRegistryCache
    {
        IEnumerable<ThingDef> AllWeapons { get; }
        
    }
}