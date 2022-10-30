using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    public class PawnBackgroundUtility
    {
        public static List<string>
            //leading and trailing spaces are on purpose, for ex.: " industrial" excludes "pre-industrial"
            spacerHints = new List<string> { "coreworld", "cryptosleep", "deep space", "galactic", "genetic", "glitterworld", "imperial", "marine", "of space", "orbital", "outer rim", "pilot", "planetary", "prosthetic", "research", "robot", "rocket", "spaceship", "star system", "starship", "stellar", "virtual", " worlds" },
            spacerSecondaryHints = new List<string> { "corporation", "navy", "planet", "space", " ship", "stars" },
            industrialHints = new List<string> { "army", "bullet", "college", "computer", "corporate", "doctor", "engineer", " industrial", "infantry", "machine", "medic", "midworld", "nuclear", "police", "pop idol", "science", "scientist", "sniper", "surgery", "university", "urbworld", "video" },
            medievalHints = new List<string> { "blacksmith", "caravan", "coliseum", "court", "estate", "farm", "feudal", "herder", " house", "lord", "medieval", "monastery", "noble", "plague", "royal", "title", "village" },
            tribalHints = new List<string> { " cave ", "caveworld", "digger", "feral", "iceworld", "tribal", "tribe", "wild" };

        public static Dictionary<string, TechLevel> TechLevelByBackstory = new Dictionary<string, TechLevel>();

        public static void BuildCache()
        {
            foreach (var backstory in DefDatabase<BackstoryDef>.AllDefs)//(var bs in BackstoryDatabase.allBackstories)
            {
                //TechLevelByBackstory.Add(backstory.Key, InferTechLevelfromBackstory(backstory.Value));
                TechLevelByBackstory.Add(backstory.defName, InferTechLevelfromBackstory(backstory));
            }
        }

        public static TechLevel InferTechLevelfromBackstory(BackstoryDef backstory)
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