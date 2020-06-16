using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace HumanResources
{
    public class StatPart_AvailableBooks : StatPart
    {
        private int LibraryCount(List<Thing> library, out float libraryPower)
        {
            int result = 0;
            foreach (Thing t in library)
            {
                if (t is Building_BookStore shelf)
                {
                    result += shelf.innerContainer.Count;
                }
            }
            libraryPower = ModBaseHumanResources.unlocked.BookResearchIncrement(result);
            return result;
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing)
            {
                CompAffectedByFacilities comp = req.Thing.TryGetComp<CompAffectedByFacilities>();
                if (comp != null && comp.LinkedFacilitiesListForReading.Any(x => x is Building_BookStore))
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    float libraryContribution;
                    stringBuilder.AppendLine("AvailableBooksReport".Translate(LibraryCount(comp.LinkedFacilitiesListForReading, out libraryContribution), comp.LinkedFacilitiesListForReading.Where(x => x is Building_BookStore).Count()) + ": +" + libraryContribution.ToStringPercent());
                    return stringBuilder.ToString().TrimEndNewlines();
                }
            }
            return null;
        }

        public override void TransformValue(StatRequest req, ref float value)
        {
            if (req.HasThing)
            {
                CompAffectedByFacilities comp = req.Thing.TryGetComp<CompAffectedByFacilities>();
                if (comp != null && !comp.LinkedFacilitiesListForReading.NullOrEmpty())
                {
                    float libraryPower;
                    int libraryCount = LibraryCount(comp.LinkedFacilitiesListForReading, out libraryPower);
                    if (libraryCount > 0)
                    {
                        value += libraryPower;
                    }
                }
            }
        }
            
    }
}