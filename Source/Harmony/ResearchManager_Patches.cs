using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HumanResources
{
    //Unlocks weapons when any tech is discovered.
    [HarmonyPatch(typeof(ResearchManager))]
    public static class ResearchManager_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ResearchManager.FinishProject), new Type[] { typeof(ResearchProjectDef), typeof(bool), typeof(Pawn), typeof(bool) })]
        public static void FinishProject_Postfix(ResearchProjectDef proj)
        {
            var weapons = proj.UnlockedWeapons();
            if (weapons.Count > 0)
            {
                ModBaseHumanResources.unlocked.UnlockWeapons(weapons);
                if (Prefs.LogVerbose) Log.Message("[HumanResources] " + proj + " discovered, unlocked weapons: " + weapons.ToStringSafeEnumerable());
            }
            var unlockMgr = ModBaseHumanResources.unlocked;
            if (ResearchTreeHelper.QueueAvailable && !unlockMgr.TechsArchived.ContainsKey(proj)) unlockMgr.Archive(proj, false);
        }

        //Guarantees the same happens on debug command.
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ResearchManager.DebugSetAllProjectsFinished))]
        public static void DebugSetAllProjectsFinished_Postfix(Dictionary<ResearchProjectDef, float> ___progress)
        {
            foreach (ResearchProjectDef proj in ___progress.Select(x => x.Key)) FinishProject_Postfix(proj);
        }
    }
}
