using RimWorld;
using Verse;

namespace HumanResources
{
    public class CompTitleMaker : ThingComp
    {
        public override string TransformLabel(string label)
        {
            string title = null;
            string tech = parent.Stuff.stuffProps.stuffAdjective;
            switch (parent.Stuff.techLevel)
            {
                case TechLevel.Archotech:
                case TechLevel.Ultra:
                    title = "BookDatabase".Translate(tech);
                    break;
                case TechLevel.Spacer:
                    title = "BookTheory".Translate(tech);
                    break;
                case TechLevel.Industrial:
                    title = "BookManual".Translate(tech);
                    break;
                case TechLevel.Medieval:
                    title = "BookCompendium".Translate(tech);
                    break;
                default:
                    title = "Book".Translate(tech);
                    break;
            }
            return title;
        }
    }
}
