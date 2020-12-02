using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HumanResources
{
    public class PawnBackgroundUtility
    {
        public static Dictionary<string, TechLevel> TechLevelByBackstory = new Dictionary<string, TechLevel>();

        public static List<string>
            spacerHints = new List<string> { "glitterworld", "space", "imperial", "robot", "cryptosleep", "star system", "starship", "planetary", "genetic", "outer rim", "orbital", "prosthetic", "virtual", "stellar", "pilot", "coreworld", "research", "worlds" },
            spacerSecondaryHints = new List<string> { "stars", "planet" },
            industrialHints = new List<string> { "midworld", " industrial", "urbworld", "corporate", "computer", "surgery", "video", "doctor", "medic", "drug", "infantry", "army", "sniper", "bullet", "nuclear", "machine", "pop idol", "science", "scientist", "university", "college", "engineer", "police" }, //leading space on " industrial" excludes "pre-industrial"
            medievalHints = new List<string> { "medieval", "feudal", "monastery", "court", "royal", "lord", "plague", "coliseum", "caravan", "farm", "title", "herder", "blacksmith", "noble", "village", " house" }, //leading space on " house" also on purpose.
            tribalHints = new List<string> { "tribe", "tribal", "digger", "caveworld", "feral", "wild", "iceworld" };

        public static void BuildCache()
        {
            foreach (var bs in BackstoryDatabase.allBackstories)
            {
                TechLevelByBackstory.Add(bs.Key, InferTechLevelfromBackstory(bs.Value));
            }
        }

        public static TechLevel InferTechLevelfromBackstory(Backstory backstory)
        {
            string text = (backstory.untranslatedTitle + " " + backstory.untranslatedTitleShort + " " + backstory.untranslatedDesc).ToLower();
            foreach (string word in spacerHints)
            {
                if (text.Contains(word)) return TechLevel.Spacer;
            }
            foreach (string word in industrialHints)
            {
                if (text.Contains(word)) return TechLevel.Industrial;
            }
            foreach (string word in medievalHints)
            {
                if (text.Contains(word)) return TechLevel.Medieval;
            }
            if (!backstory.spawnCategories.NullOrEmpty() && backstory.spawnCategories.Contains("Tribal"))
            {
                return TechLevel.Neolithic;
            }
            else
            {
                foreach (string word in tribalHints)
                {
                    if (text.Contains(word)) return TechLevel.Neolithic;
                }
                foreach (string word in spacerSecondaryHints)
                {
                    if (text.Contains(word)) return TechLevel.Spacer;
                }
            }
            return TechLevel.Undefined;
        }

    }
}
