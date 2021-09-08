using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace HumanResources
{
    using static ModBaseHumanResources;
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
            libraryPower = TechTracker.BookResearchIncrement(result);
            return result;
        }

        private int DatabaseCount(out float databasePower)
        {
            int result = unlocked.TechsArchived.Count();
            databasePower = TechTracker.BookResearchIncrement(result);
            return result;
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing)
            {
                StringBuilder stringBuilder = new StringBuilder();
                float libraryContribution;
                CompAffectedByFacilities comp = req.Thing.TryGetComp<CompAffectedByFacilities>();
                if (comp != null && comp.LinkedFacilitiesListForReading.Any(x => x is Building_BookStore))
                {
                    stringBuilder.AppendLine("AvailableBooksReport".Translate(LibraryCount(comp.LinkedFacilitiesListForReading, out libraryContribution), comp.LinkedFacilitiesListForReading.Where(x => x is Building_BookStore).Count()) + ": +" + libraryContribution.ToStringPercent());
                }
                else
                {
                    CompNetworkAccess compCloud = req.Thing.TryGetComp<CompNetworkAccess>();
                    if (compCloud != null)
                    {
                        stringBuilder.AppendLine("AvailableOnNetwork".Translate(DatabaseCount(out libraryContribution)) + ": +" + libraryContribution.ToStringPercent());
                    }
                }
                return stringBuilder.ToString().TrimEndNewlines();
            }
            return null;
        }

        public override void TransformValue(StatRequest req, ref float value)
        {
            if (req.HasThing)
            {
                float libraryPower = 0f;
                int libraryCount = 0;
                CompAffectedByFacilities compBooks = req.Thing.TryGetComp<CompAffectedByFacilities>();
                if (compBooks != null && !compBooks.LinkedFacilitiesListForReading.NullOrEmpty())
                {
                    libraryCount += LibraryCount(compBooks.LinkedFacilitiesListForReading, out libraryPower);
                }
                else
                {
                    CompNetworkAccess compCloud = req.Thing.TryGetComp<CompNetworkAccess>();
                    if (compCloud != null)
                    {
                        libraryCount += DatabaseCount(out libraryPower);
                    }
                }
                if (libraryCount > 0)
                {
                    value += libraryPower;
                }
            }
        }

    }
}