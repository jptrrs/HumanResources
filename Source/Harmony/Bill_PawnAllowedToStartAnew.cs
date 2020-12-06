using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace HumanResources
{
    //Checks if pawn knows how to make a recipe.
    [HarmonyPatch(typeof(Bill), nameof(Bill.PawnAllowedToStartAnew), new Type[] { typeof(Pawn) })]
    public static class Bill_PawnAllowedToStartAnew
    {
        public static bool Prefix(Pawn p, RecipeDef ___recipe)
        {
            if (p.TechBound())
            {
                var expertise = p.TryGetComp<CompKnowledge>().expertise;
                if (expertise != null)
                {
                    //Look for a required ResearchProjectDef the pawn must know:
                    ResearchProjectDef requisite = null;

                    //If the recipe has it own prerequisite, then that's it:
                    if (___recipe.researchPrerequisite != null)
                    {
                        requisite = ___recipe.researchPrerequisite;
                    }

                    //If not, only for complex recipes, inspect its home buildings. 
                    //In this case, it will only look for the first pre-requisite on a given building's list.
                    else if (!___recipe.recipeUsers.NullOrEmpty() && (___recipe.UsesUnfinishedThing || ___recipe.defName.StartsWith("Make_")))
                    {
                        ThingDef recipeHolder = null;
                        //If any building is free from prerequisites, then that's used. 
                        var noPreReq = ___recipe.recipeUsers.Where(x => x.researchPrerequisites.NullOrEmpty());
                        if (noPreReq.Any())
                        {
                            recipeHolder = noPreReq.FirstOrDefault();
                        }
                        //Otherwise, check each one and choose the one with the cheapest prerequisite.
                        else if (___recipe.recipeUsers.Count() > 1)
                        {
                            recipeHolder = ___recipe.recipeUsers.Aggregate((l, r) => (l.researchPrerequisites.FirstOrDefault().baseCost < r.researchPrerequisites.FirstOrDefault().baseCost) ? l : r);
                        }
                        //Or, if its just one, pick that.
                        else
                        {
                            recipeHolder = ___recipe.recipeUsers.FirstOrDefault();
                        }
                        //At last, define what's the requisite for the selected building.
                        if (recipeHolder != null && !recipeHolder.researchPrerequisites.NullOrEmpty())
                        {
                            requisite = recipeHolder.researchPrerequisites.FirstOrDefault();
                        }
                    }
                    if (requisite != null && !requisite.IsKnownBy(p))
                    {
                        JobFailReason.Is("DoesntKnowHowToCraft".Translate(p, ___recipe.label, requisite.label));
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
