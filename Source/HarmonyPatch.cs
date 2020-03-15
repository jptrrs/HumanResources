using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Reflection;
using System.Diagnostics;

namespace HumanResources
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static Type patchType = typeof(HarmonyPatches);

        static HarmonyPatches()
        {
            Harmony.DEBUG = true;

            var harmony = new Harmony("JPT_HumanResources");
            //var harmony = HarmonyInstance.Create("JPT_HumanResources");

            //template:
            //harmonyInstance.Patch(original: AccessTools.Method(type: typeof(?), name: "?"),
            //prefix: null, postfix: new HarmonyMethod(type: patchType, name: nameof(?)), transpiler: null);

            harmony.Patch(AccessTools.Method(typeof(Dialog_BillConfig), "DoWindowContents", new Type[] { typeof(Rect) }),
                new HarmonyMethod(patchType, nameof(DoWindowContents_Prefix)), new HarmonyMethod(patchType, nameof(DoWindowContents_Postfix)), null);

            harmony.Patch(AccessTools.Method(typeof(ThingFilterUI), "DoThingFilterConfigWindow"),
                new HarmonyMethod(patchType, nameof(DoThingFilterConfigWindow_Prefix), new Type[] { typeof(ThingFilter), typeof(int) }), new HarmonyMethod(patchType, nameof(BallKeeper_Postfix)), null);

            harmony.Patch(AccessTools.Method(typeof(Listing_TreeThingFilter), "Visible", new Type[] { typeof(ThingDef) }),
                null, new HarmonyMethod(patchType, nameof(Visible_Postfix)), null);

            harmony.Patch(AccessTools.Method(typeof(WorkGiver_ConstructFinishFrames), "JobOnThing", new Type[] { typeof(Pawn), typeof(Thing), typeof(bool) }),
                new HarmonyMethod(patchType, nameof(ConstructFinishFrames_JobOnThing_Prefix)), null, null);

            harmony.Patch(AccessTools.Method(typeof(Pawn_JobTracker), "TryTakeOrderedJob", new Type[] { typeof(Job), typeof(JobTag) }),
                new HarmonyMethod(patchType, nameof(TryTakeOrderedJob_Prefix)), null, null);

            harmony.Patch(AccessTools.Method(typeof(JobGiver_PickUpOpportunisticWeapon), "ShouldEquip", new Type[] { typeof(Thing), typeof(Pawn) }),
                new HarmonyMethod(patchType, nameof(ShouldEquip_Prefix)), null, null);

            harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders", new Type[] { typeof(Vector3), typeof(Pawn), typeof(List<FloatMenuOption>) }),
                null, new HarmonyMethod(patchType, nameof(AddHumanlikeOrders_Postfix)), null);

            harmony.Patch(AccessTools.Method(typeof(Bill), "PawnAllowedToStartAnew", new Type[] { typeof(Pawn) }),
                new HarmonyMethod(patchType, nameof(PawnAllowedToStartAnew_Prefix)), null, null);

            harmony.Patch(AccessTools.Method(typeof(WorkGiver_GrowerSow), "JobOnCell", new Type[] { typeof(Pawn), typeof(IntVec3), typeof(bool) }),
                null, new HarmonyMethod(patchType, nameof(JobOnCell_Postfix)), null);

            harmony.Patch(AccessTools.Method(typeof(ResearchManager), "FinishProject", new Type[] { typeof(ResearchProjectDef), typeof(bool), typeof(Pawn) }),
                null, new HarmonyMethod(patchType, nameof(FinishProject_Postfix)), null);

            harmony.Patch(AccessTools.Method(typeof(ResearchManager), "DebugSetAllProjectsFinished"),
                null, new HarmonyMethod(patchType, nameof(DebugSetAllProjectsFinished_Postfix)), null);

            harmony.Patch(AccessTools.Method(typeof(ThingOwner), "NotifyAdded"),
                null, new HarmonyMethod(patchType, nameof(NotifyAdded_Postfix)), null);

            //Kills all starting research 
            //harmony.Patch(AccessTools.Method(typeof(Game), "InitNewGame"),
            //    new HarmonyMethod(patchType, nameof(InitNewGame_Prefix)), null, null);

        }

        //public static bool Patch_Inhibitor_Prefix(/*MethodBase __originalMethod*/)
        //{
        //    //Log.Message(__originalMethod.Name + " was inhibited");
        //    return false;
        //}

        public static void NotifyAdded_Postfix(Thing item, IThingHolder ___owner)
        {
            if (___owner is Building_BookStore bookStore && item.Stuff != null && item.Stuff.IsWithinCategory(DefDatabase<ThingCategoryDef>.GetNamed("Knowledge")))
            {
                ResearchProjectDef project = ModBaseHumanResources.unlocked.techByStuff[item.Stuff];
                bookStore.CompStorageGraphic.UpdateGraphics();
                project.CarefullyFinishProject(bookStore);
            }
        }
        
        public static void DoWindowContents_Prefix(Dialog_BillConfig __instance)
        {
            FieldInfo billInfo = AccessTools.Field(typeof(Dialog_BillConfig), "bill");
            Bill_Production bill = billInfo.GetValue(__instance) as Bill_Production;
            if (bill.recipe.defName == "Tech_Document") DocumentRecipe = true;
            if (bill.recipe.defName == "Tech_Learn") LearnRecipe = true;
        }

        public static void DoWindowContents_Postfix()
        {
            LearnRecipe = false;
            DocumentRecipe = false;
        }

        private static bool LearnRecipe;
        private static bool DocumentRecipe;

        public static void DoThingFilterConfigWindow_Prefix(ThingFilter parentFilter, int openMask)
        {
            if (parentFilter != null && parentFilter.AllowedDefCount > 0 && parentFilter.AllowedThingDefs.All(x => x.IsWithinCategory(DefDatabase<ThingCategoryDef>.GetNamed("Knowledge"))))
            {
                openMask = 2;
                Ball = true;
            }
        }

        public static void BallKeeper_Postfix()
        {
            Ball = false;
        }

        private static bool Ball;
        
        public static void Visible_Postfix(ThingDef td, ref bool __result)
        {
            if (Ball && td.IsWithinCategory(DefDatabase<ThingCategoryDef>.GetNamed("Knowledge")))
            {
                if (DocumentRecipe) __result = !ModBaseHumanResources.unlocked.techByStuff[td].IsFinished;
                else if (LearnRecipe) __result = ModBaseHumanResources.unlocked.techByStuff[td].IsFinished;
                else __result = true;
            }
        }

        public static bool ConstructFinishFrames_JobOnThing_Prefix(Pawn pawn, Thing t)
        {
            var requisites = t.def.entityDefToBuild.researchPrerequisites;
            if (!requisites.NullOrEmpty())
            {
                JobFailReason.Is("DoesntKnowHowToBuild".Translate(pawn,t.def.entityDefToBuild.label));
                return pawn.GetComp<CompKnowledge>().expertise.Any(x => requisites.Contains(x));
            }
            return true;
        }

        public static bool ShouldEquip_Prefix(Thing newWep, Pawn pawn)
        {
            if (pawn.Faction.IsPlayer) return CheckKnownWeapons(pawn, newWep);
            else return true;
        }

        public static bool TryTakeOrderedJob_Prefix(Job job, Pawn ___pawn)
        {
            if (___pawn.Faction.IsPlayer && job.def == JobDefOf.Equip) return CheckKnownWeapons(___pawn, job.targetA.Thing);
            else return true;
        }

        public static void JobOnCell_Postfix(Pawn pawn, ThingDef ___wantedPlantDef, ref Job __result)
        {
            if (__result != null)
            {
                var knownPlants = pawn.GetComp<CompKnowledge>().knownPlants;
                Log.Warning(pawn + "'s plant knowledge: " + knownPlants);
                bool flag = true;
                if (!knownPlants.EnumerableNullOrEmpty()) flag = knownPlants.Contains(___wantedPlantDef);
                else flag = false;
                if (!flag)
                {
                    
                    JobFailReason.Is("DoesntKnowThisPlant".Translate(pawn, ___wantedPlantDef));
                    __result = null;
                }
            }
        }

        public static void AddHumanlikeOrders_Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);
            ThingWithComps equipment = null;
            List<Thing> thingList = c.GetThingList(pawn.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i].TryGetComp<CompEquippable>() != null)
                {
                    equipment = (ThingWithComps)thingList[i];
                    break;
                }
            }
            if (equipment != null && equipment.def.IsWeapon && !CheckKnownWeapons(pawn, equipment))
            {
                string labelShort = equipment.LabelShort;
                FloatMenuOption item = new FloatMenuOption("CannotEquip".Translate(labelShort) + " (" + "UnknownWeapon".Translate(pawn) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                opts.RemoveAt(opts.FindIndex(x => x.Label.Contains("Equip".Translate(labelShort))));
                opts.Add(item);
            }
        }

        private static bool CheckKnownWeapons(Pawn pawn, Thing thing)
        {
            var knownWeapons = pawn.GetComp<CompKnowledge>().knownWeapons;
            bool result = true;
            if (!knownWeapons.EnumerableNullOrEmpty()) result = knownWeapons.Contains(thing.def);
            else result = false;
            return result;
        }

        public static bool PawnAllowedToStartAnew_Prefix(Pawn p, RecipeDef ___recipe)
        {
            var expertise = p.GetComp<CompKnowledge>().expertise;
            if (___recipe.researchPrerequisite != null && !expertise.Contains(___recipe.researchPrerequisite))
            {
                JobFailReason.Is("DoesntKnowHowToCraft".Translate(p,___recipe.label));
                return false;
            }
            else return true;
        }

        public static void FinishProject_Postfix(ResearchProjectDef proj)
        {
            var weapons = proj.UnlockedWeapons();
            if (weapons.Count > 0)
            {
                ModBaseHumanResources.unlocked.UnlockWeapons(weapons);
                Log.Message("[HumanResources] " + proj + " discovered, unlocked weapons: " + weapons.ToStringSafeEnumerable());
                //Log.Message("[HumanResources] Currently unlocked weapons: " + ModBaseHumanResources.unlocked.weapons.Count());
            }
        }

        public static void DebugSetAllProjectsFinished_Postfix(Dictionary<ResearchProjectDef, float> ___progress)
        {
            foreach (ResearchProjectDef proj in ___progress.Select(x => x.Key)) FinishProject_Postfix(proj);
        }

        public static void InitNewGame_Prefix()
        {
            Find.FactionManager.OfPlayer.def.startingResearchTags.Clear();
            Log.Message("[HumanResources] Starting a new game, player faction has been stripped of all starting research.");
        }
    }
}
