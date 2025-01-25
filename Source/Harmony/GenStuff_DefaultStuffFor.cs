using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace HumanResources
{
    //Prevents quirks of the Tech Stuff system from causing errors when the game tries to draw icons for books.
    //[HarmonyPatch(typeof(GenStuff), "DefaultStuffFor", new Type[] { typeof(BuildableDef) })]
    public static class GenStuff_DefaultStuffFor
    {
        public static bool Prefix(BuildableDef bd)
        {
            return !(bd.defName != TechDefOf.TechBook.defName || bd.defName != TechDefOf.TechDrive.defName);
        }
    }
}