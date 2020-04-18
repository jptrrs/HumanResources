using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Reflection;
using RimWorld.Planet;

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

            //Tweaks ingredients visibility for knowledge recipes.
            harmony.Patch(AccessTools.Method(typeof(Dialog_BillConfig), "DoWindowContents", new Type[] { typeof(Rect) }),
                new HarmonyMethod(patchType, nameof(DoWindowContents_Prefix)), new HarmonyMethod(patchType, nameof(DoWindowContents_Postfix)), null);
            harmony.Patch(AccessTools.Method(typeof(ThingFilterUI), "DoThingFilterConfigWindow"),
                new HarmonyMethod(patchType, nameof(DoThingFilterConfigWindow_Prefix), new Type[] { typeof(ThingFilter), typeof(int) }), new HarmonyMethod(patchType, nameof(BallKeeper_Postfix)), null);
            harmony.Patch(AccessTools.Method(typeof(Listing_TreeThingFilter), "Visible", new Type[] { typeof(ThingDef) }),
                null, new HarmonyMethod(patchType, nameof(Visible_Postfix)), null);

            //Checks if pawn knows how to build something.
            harmony.Patch(AccessTools.Method(typeof(WorkGiver_ConstructFinishFrames), "JobOnThing", new Type[] { typeof(Pawn), typeof(Thing), typeof(bool) }),
                new HarmonyMethod(patchType, nameof(ConstructFinishFrames_JobOnThing_Prefix)), null, null);

            //Checks if pawn knows a weapon before equiping it.
            harmony.Patch(AccessTools.Method(typeof(Pawn_JobTracker), "TryTakeOrderedJob", new Type[] { typeof(Job), typeof(JobTag) }),
                new HarmonyMethod(patchType, nameof(TryTakeOrderedJob_Prefix)), null, null);
            harmony.Patch(AccessTools.Method(typeof(JobGiver_PickUpOpportunisticWeapon), "ShouldEquip", new Type[] { typeof(Thing), typeof(Pawn) }),
                new HarmonyMethod(patchType, nameof(ShouldEquip_Prefix)), null, null);
            harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders", new Type[] { typeof(Vector3), typeof(Pawn), typeof(List<FloatMenuOption>) }),
                null, new HarmonyMethod(patchType, nameof(AddHumanlikeOrders_Postfix)), null);

            //Checks if pawn knows how to make a recipe.
            harmony.Patch(AccessTools.Method(typeof(Bill), "PawnAllowedToStartAnew", new Type[] { typeof(Pawn) }),
                new HarmonyMethod(patchType, nameof(PawnAllowedToStartAnew_Prefix)), null, null);

            //Checks if pawn knows how to cultivate a crop.
            harmony.Patch(AccessTools.Method(typeof(WorkGiver_GrowerSow), "JobOnCell", new Type[] { typeof(Pawn), typeof(IntVec3), typeof(bool) }),
                null, new HarmonyMethod(patchType, nameof(JobOnCell_Postfix)), null);

            //Unlocks weapons when any tech is discovered.
            harmony.Patch(AccessTools.Method(typeof(ResearchManager), "FinishProject", new Type[] { typeof(ResearchProjectDef), typeof(bool), typeof(Pawn) }),
                null, new HarmonyMethod(patchType, nameof(FinishProject_Postfix)), null);

            //Guarantees the same happens on debug command.
            harmony.Patch(AccessTools.Method(typeof(ResearchManager), "DebugSetAllProjectsFinished"),
                null, new HarmonyMethod(patchType, nameof(DebugSetAllProjectsFinished_Postfix)), null);

            //If a book is added to book shelf, discover corresponding tech.
            harmony.Patch(AccessTools.Method(typeof(ThingOwner), "NotifyAdded"),
                null, new HarmonyMethod(patchType, nameof(NotifyAdded_Postfix)), null);

            //If a book can't be hauled, try IHaulDestination to look for a book shelf.
            //harmony.Patch(AccessTools.Method(typeof(StoreUtility), "TryFindBestBetterStoreCellFor"/*, new Type[] { typeof(Thing), typeof(Pawn), typeof(Map), typeof(StoragePriority), typeof(Faction), typeof(IntVec3), typeof(bool) }*/),
            //    null, new HarmonyMethod(patchType, nameof(TryFindBestBetterStoreCellFor_Postfix)), null);

            //harmony.Patch(AccessTools.Method(typeof(StoreUtility), "TryFindBestBetterNonSlotGroupStorageFor"/*, new Type[] { typeof(Thing), typeof(Pawn), typeof(Map), typeof(StoragePriority), typeof(Faction), typeof(IntVec3), typeof(bool) }*/),
            //    null, new HarmonyMethod(patchType, nameof(TryFindBestBetterNonSlotGroupStorageFor_Postfix)), null);

            //Kills all starting research 
            //harmony.Patch(AccessTools.Method(typeof(Game), "InitNewGame"),
            //    new HarmonyMethod(patchType, nameof(InitNewGame_Prefix)), null, null);

            //harmony.Patch(AccessTools.Method(typeof(WorkGiver_DoBill), "JobOnThing"),
            //    null, new HarmonyMethod(patchType, nameof(JobOnThing_Postfix)), null);

            //harmony.Patch(AccessTools.Method(typeof(JobGiver_Work), "PawnCanUseWorkGiver"),
            //    null, new HarmonyMethod(patchType, nameof(PawnCanUseWorkGiver_Postfix)), null);

            //harmony.Patch(AccessTools.Method(typeof(ThinkNode_PrioritySorter), "TryIssueJobPackage"),
            //    null, new HarmonyMethod(patchType, nameof(TryIssueJobPackage_Postfix)), null);

            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing == "Albion.GoExplore"))
            {
                Log.Message("[HumanResources] Go Explore detected! Integrating...");

                harmony.Patch(AccessTools.Method("LetsGoExplore.WorldObject_ResearchRequestLGE:Notify_CaravanArrived"),
                    new HarmonyMethod(patchType, nameof(Notify_CaravanArrived_Prefix)), new HarmonyMethod(patchType, nameof(Notify_CaravanArrived_Postfix)), null);
                harmony.Patch(AccessTools.Method("LetsGoExplore.WorldObject_ResearchRequestLGE:ApplyPointsToResearch"),
                    new HarmonyMethod(patchType, nameof(ApplyPointsToResearch_Prefix)), null, null);
            }

            //public static bool Patch_Inhibitor_Prefix(/*MethodBase __originalMethod*/)
            //{
            //    //Log.Message(__originalMethod.Name + " was inhibited");
            //    return false;
            //}
        }

        public static void  PawnCanUseWorkGiver_Postfix(Pawn pawn, WorkGiver giver, bool __result)
        {
            
            if (giver is WorkGiver_Scanner && __result)
            {
                //== "TrainWeapon")
                Log.Message("PawnCanUseWorkGiver active for " + pawn+", defName was " + giver.def.defName);
                //bool flag = giver.ShouldSkip(pawn, false);
                //if (!flag) Log.Message("PawnCanUseWorkGiver: " + pawn + " should not skip!");
            }
        }

        private static Pawn GoExploreResearcher;

        public static void Notify_CaravanArrived_Prefix(Caravan caravan)
        {
            GoExploreResearcher = BestCaravanPawnUtility.FindPawnWithBestStat(caravan, StatDefOf.ResearchSpeed, null);
        }

        public static void Notify_CaravanArrived_Postfix(Caravan caravan)
        {
            GoExploreResearcher = null;
        }

        public static bool ApplyPointsToResearch_Prefix(float points, ResearchProjectDef __result)
        {
            ResearchManager researchManager = Find.ResearchManager;
            bool flag = !researchManager.AnyProjectIsAvailable;
            if (flag)
            {
                __result = null;
            }
            else
            {
                IEnumerable<ResearchProjectDef> source = from x in DefDatabase<ResearchProjectDef>.AllDefsListForReading
                                                         where x.CanStartNow
                                                         select x;
                ResearchProjectDef tech;
                source.TryRandomElementByWeight((ResearchProjectDef x) => 1f / x.baseCost, out tech);
                points *= 12f; //stangely, that number is 121f on the mod source. I'm assuming that's a typo.
                float total = tech.baseCost;
                CompKnowledge techComp = GoExploreResearcher.TryGetComp<CompKnowledge>();
                if (techComp != null)
                {
                    Dictionary<ResearchProjectDef, float> expertise = techComp.expertise;
                    float num = tech.GetProgress(expertise);
                    num += points / total;
                    expertise[tech] = (num > 1) ? 1 : num;
                }
                else
                {
                    return true;
                }
                __result = tech;
            }
            return false;
        }

        public static void TryFindBestBetterNonSlotGroupStorageFor_Postfix(Thing t, IHaulDestination haulDestination, ref bool __result)
        {
            if (__result && t.def.defName == "TechBook") Log.Warning("TryFindBestBetterNonSlotGroupStorageFor found " + haulDestination);
        }

        public static void TryFindBestBetterStoreCellFor_Postfix(Thing t, Pawn carrier, Map map, StoragePriority currentPriority, Faction faction, ref IntVec3 foundCell, ref bool __result, bool needAccurateResult = true)
        {
            //Log.Warning("postFixing TryFindBestBetterStoreCellFor:");
            //foundCell = new IntVec3(0, 0, 0);
            if (!__result && carrier.CurJobDef.label == "DocumentTech" && t.def.defName == "TechBook")
            {
                try
                {
                    IHaulDestination haulDestination;
                    //bool test = StoreUtility.TryFindBestBetterStorageFor(t, carrier, map, currentPriority, faction, out foundCell, out haulDestination, needAccurateResult);
                    //Log.Message("postFixing TryFindBestBetterStoreCellFor test is "+test);
                    //__result = StoreUtility.TryFindBestBetterStorageFor(t, carrier, map, currentPriority, faction, out foundCell, out haulDestination, needAccurateResult   );
                    bool alternate = StoreUtility.TryFindBestBetterNonSlotGroupStorageFor(t, carrier, map, currentPriority, faction, out haulDestination);
                    Log.Message("haulDestination found: " + haulDestination+" at "+haulDestination.Position);
                    foundCell = haulDestination.Position;
                    __result = alternate;
                }
                catch (Exception ex)
                {
                }
            }
        }

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
            if (bill.UsesKnowledge())
            {
                if (bill.IsResearch()) FutureTech = true;
                else CurrentTech = true;
            }
            if (bill.IsWeaponsTraining()) WeaponTrainingSelection = true;
        }

        public static void DoWindowContents_Postfix()
        {
            CurrentTech = false;
            FutureTech = false;
            WeaponTrainingSelection = false;
        }

        private static bool CurrentTech;
        private static bool FutureTech;
        private static bool WeaponTrainingSelection;

        public static void DoThingFilterConfigWindow_Prefix(ThingFilter parentFilter, int openMask)
        {
            if (parentFilter != null && parentFilter.AllowedDefCount > 0 && parentFilter.AllowedThingDefs.All(x => x.IsWithinCategory(DefDatabase<ThingCategoryDef>.GetNamed("Knowledge"))))
            {
                openMask = 4;
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
                if (FutureTech) __result = !ModBaseHumanResources.unlocked.techByStuff[td].IsFinished;
                else if (CurrentTech) __result = ModBaseHumanResources.unlocked.techByStuff[td].IsFinished;
                else __result = true;
            }
            else if (WeaponTrainingSelection)
            {
                __result = ModBaseHumanResources.unlocked.weapons.Contains(td);
            }
        }

        public static bool ConstructFinishFrames_JobOnThing_Prefix(Pawn pawn, Thing t)
        {
            if (t.Faction != pawn.Faction)
            {
                return true;
            }
            Frame frame = t as Frame;
            if (frame == null)
            {
                return true;
            }
            if (frame.MaterialsNeeded().Count > 0)
            {
                return true;
            }
            var requisites = t.def.entityDefToBuild.researchPrerequisites;
            if (!requisites.NullOrEmpty())
            {
                string preReqText = (requisites.Count() > 1) ? (string)"MultiplePrerequisites".Translate() : requisites.FirstOrDefault().label;
                JobFailReason.Is("DoesntKnowHowToBuild".Translate(pawn,t.def.entityDefToBuild.label, preReqText));
                return pawn.GetComp<CompKnowledge>().expertise.Any(x => requisites.Contains(x.Key) && x.Value >= 1f);
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
                var requisites = ___wantedPlantDef.researchPrerequisites;
                if (!requisites.NullOrEmpty())
                {
                    var knownPlants = pawn.GetComp<CompKnowledge>().knownPlants;
                    //Log.Warning(pawn + "'s plant knowledge: " + knownPlants);
                    bool flag = true;
                    if (!knownPlants.EnumerableNullOrEmpty()) flag = knownPlants.Contains(___wantedPlantDef);
                    else flag = false;
                    if (!flag)
                    {
                        string preReqText = requisites.Any() ? (string)"MultiplePrerequisites".Translate() : requisites.FirstOrDefault().label;
                        JobFailReason.Is("DoesntKnowThisPlant".Translate(pawn, ___wantedPlantDef, preReqText));
                        __result = null;
                    }
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
                string flavoredExplanation = ModBaseHumanResources.unlocked.weapons.Contains(equipment.def) ? "UnknownWeapon".Translate(pawn) : "EvilWeapon".Translate(pawn);
                FloatMenuOption item = new FloatMenuOption("CannotEquip".Translate(labelShort) + " (" + flavoredExplanation + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
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
            if (expertise != null)
            {
                //Look for a required ResearchProjectDef the pawn must know:
                ResearchProjectDef requisite = null;

                //If the recipe has it own prerequisite, then that's it:
                if (___recipe.researchPrerequisite != null) requisite = ___recipe.researchPrerequisite;

                //If not, only for complex recipes, inspect it's home buildings. 
                //In this case, it will only look for the first pre-requisite on a given building's list.
                else if (!___recipe.recipeUsers.NullOrEmpty() && ___recipe.UsesUnfinishedThing)
                {
                    ThingDef recipeHolder = null;
                    //If any building is free from prerequisites, then that's used. 
                    var noPreReq = ___recipe.recipeUsers.Where(x => x.researchPrerequisites.NullOrEmpty());
                    if (noPreReq.Any()) recipeHolder = noPreReq.FirstOrDefault();
                    //Otherwise, check each one and choose the one with the cheapest prerequisite.
                    else if (___recipe.recipeUsers.Count() > 1)
                    {
                        recipeHolder = ___recipe.recipeUsers.Aggregate((l, r) => (l.researchPrerequisites.FirstOrDefault().baseCost < r.researchPrerequisites.FirstOrDefault().baseCost) ? l : r);
                    }
                    //Or, if its just one, pick that.
                    else recipeHolder = ___recipe.recipeUsers.FirstOrDefault();
                    //At last, define what's the requisite for the selected building.
                    if (recipeHolder != null && !recipeHolder.researchPrerequisites.NullOrEmpty())
                    {
                        requisite = recipeHolder.researchPrerequisites.FirstOrDefault();
                    }
                }
                if (requisite != null && !(expertise.ContainsKey(requisite) && expertise[requisite] >= 1f))
                {
                    JobFailReason.Is("DoesntKnowHowToCraft".Translate(p, ___recipe.label, requisite.label));
                    return false;
                }
            }
            return true;
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
