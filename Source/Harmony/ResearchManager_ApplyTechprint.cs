using HarmonyLib;
using RimWorld;
using System.Text;
using Verse;

namespace HumanResources
{
    //Diverts Techprint to the pawn's expertise
    [HarmonyPatch(typeof(ResearchManager), "ApplyTechprint")]
    public static class ResearchManager_ApplyTechprint
    {
        public static bool Prefix(ResearchManager __instance, ResearchProjectDef proj, Pawn applyingPawn)
        {
            var expertise = applyingPawn.TryGetComp<CompKnowledge>()?.expertise;
            if (ModLister.RoyaltyInstalled && !expertise.EnumerableNullOrEmpty())
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("LetterTechprintAppliedPartIntro".Translate(proj.Named("PROJECT")));
                stringBuilder.AppendLine();
                if (proj.techprintCount > __instance.GetTechprints(proj))
                {
                    __instance.AddTechprints(proj, 1);
                    if (proj.techprintCount == __instance.GetTechprints(proj))
                    {
                        stringBuilder.AppendLine("LetterTechprintAppliedPartJustUnlocked".Translate(proj.Named("PROJECT")));
                        stringBuilder.AppendLine();
                    }
                    else
                    {
                        stringBuilder.AppendLine("LetterTechprintAppliedPartNotUnlockedYet".Translate(__instance.GetTechprints(proj), proj.techprintCount.ToString(), proj.Named("PROJECT")));
                        stringBuilder.AppendLine();
                    }
                }
                else if (proj.IsKnownBy(applyingPawn))
                {
                    stringBuilder.AppendLine("LetterTechprintAppliedPartAlreadyResearched".Translate(proj.Named("PROJECT")));
                    stringBuilder.AppendLine();
                }
                else
                {
                    float num = 0f;
                    if (expertise.ContainsKey(proj))
                    {
                        num = (1 - expertise[proj]) * 0.5f;
                        expertise[proj] += num;
                    }
                    else
                    {
                        expertise.Add(proj, 0.5f);
                        num = 0.5f;
                    }
                    stringBuilder.AppendLine("LetterTechprintAppliedPartAlreadyUnlocked".Translate(num * 100, proj.Named("PROJECT")));
                    stringBuilder.AppendLine();
                }
                if (applyingPawn != null)
                {
                    stringBuilder.AppendLine("LetterTechprintAppliedPartExpAwarded".Translate(2000.ToString(), SkillDefOf.Intellectual.label, applyingPawn.Named("PAWN")));
                    applyingPawn.skills.Learn(SkillDefOf.Intellectual, 2000f, false);
                }
                if (stringBuilder.Length > 0)
                {
                    Find.LetterStack.ReceiveLetter("LetterTechprintAppliedLabel".Translate(proj.Named("PROJECT")), stringBuilder.ToString().TrimEndNewlines(), LetterDefOf.PositiveEvent, null);
                }
                return false;
            }
            return true;
        }
    }
}
