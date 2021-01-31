using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace HumanResources
{
    //Borrowed from Jecrell's RimWriter
    public class Building_BookStore : Building, IStoreSettingsParent, IHaulDestination, IThingHolder
    {
        public ThingOwner innerContainer;
        public List<Thing> borrowed = new List<Thing>();
        protected StorageSettings storageSettings;
        protected CompStorageGraphic compStorageGraphic = null;
        protected static int dynamicCapacityInt;

        public virtual int dynamicCapacity
        {
            get
            {
                if (dynamicCapacityInt == 0)
                {
                    dynamicCapacityInt = Math.Max(ModBaseHumanResources.unlocked.total / 20, CompStorageGraphic.Props.countFullCapacity);
                }
                return dynamicCapacityInt;
            }
        }

        public CompStorageGraphic CompStorageGraphic
        {
            get
            {
                if (compStorageGraphic == null)
                {
                    compStorageGraphic = this.TryGetComp<CompStorageGraphic>();
                }
                return compStorageGraphic;
            }
        }

        public override Graphic Graphic
        {
            get
            {
                if (CompStorageGraphic?.CurStorageGraphic != null)
                {
                    return CompStorageGraphic.CurStorageGraphic;
                }
                return base.Graphic;
            }
        }

        public bool StorageTabVisible => true;
        public Building_BookStore()
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public virtual bool Accepts(Thing thing)
        {
            if (thing.def == TechDefOf.TechBook && thing.Stuff != null && thing.Stuff.IsWithinCategory(TechDefOf.Knowledge))
            {
                bool allowed = storageSettings.AllowedToAccept(thing.Stuff);
                bool fits = innerContainer.Count < dynamicCapacity;
                bool duplicate = thing.TryGetTech().IsFinished;
                return allowed && fits && !duplicate;
            }
            return false;
        }

        public override void PostMake()
        {
            base.PostMake();
            storageSettings = new StorageSettings(this);
            if (def.building.defaultStorageSettings != null)
            {
                storageSettings.CopyFrom(this.def.building.defaultStorageSettings);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<ThingOwner>(ref innerContainer, "innerContainer", new object[] { this });
            Scribe_Deep.Look<StorageSettings>(ref storageSettings, "storageSettings", new object[] { this });
        }

        public override void DeSpawn(DestroyMode mode)
        {
            ModBaseHumanResources.unlocked.libraryFreeSpace -= dynamicCapacity - innerContainer.Count;
            if (innerContainer.Count > 0)
            {
                innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near, delegate (Thing t, int i) { t.TryGetTech().Ejected(this); });
            }
            base.DeSpawn(mode);
        }

        public bool TryDropRandom(out Thing droppedThing, bool forbid = false)
        {
            Thing outThing;
            droppedThing = null;
            if (innerContainer.Count > 0)
            {
                innerContainer.TryDrop(innerContainer.RandomElement(), ThingPlaceMode.Near, out outThing);
                if (forbid) outThing.SetForbidden(true);
                droppedThing = outThing as ThingWithComps;
                return true;
            }
            return false;
        }

        public bool TryDrop(Thing item, bool forbid = true)
        {
            if (innerContainer.Contains(item))
            {
                Thing outThing;
                innerContainer.TryDrop(item, ThingPlaceMode.Near, out outThing);
                ResearchProjectDef tech = outThing.TryGetTech();
                //tech.EjectTech(this);
                if (forbid) outThing.SetForbidden(true);
                //ModBaseHumanResources.unlocked.libraryFreeSpace++;
                CompStorageGraphic.UpdateGraphics();
                return true;
            }
            return false;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public StorageSettings GetParentStoreSettings()
        {
            return def.building.fixedStorageSettings;
        }

        public StorageSettings GetStoreSettings()
        {
            return storageSettings;
        }

        public override string GetInspectString()
        {
            StringBuilder s = new StringBuilder();
            string baseStr = base.GetInspectString();
            if (baseStr != "") s.AppendLine(baseStr);
            if (innerContainer.Count == 0) s.AppendLine("BookStoreEmpty".Translate());
            else s.AppendLine("BookStoreCapacity".Translate(innerContainer.Count, dynamicCapacity.ToString()));
            if (Prefs.DevMode) s.AppendLine("Free space remaining in library: " + ModBaseHumanResources.unlocked.libraryFreeSpace);
            return s.ToString().TrimEndNewlines();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
                yield return g;
            if (innerContainer.Count > 0)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "BookStoreRetrieveBook".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", true),
                    defaultDesc = "BookStoreRetrieveBookDesc".Translate(),
                    action = delegate
                    {
                        ContentsMenu();
                    }
                };
            }
        }

        public void ContentsMenu()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            Map map = Map;
            if (innerContainer.Count != 0)
            {
                foreach (Thing current in innerContainer)
                {
                    string text = current.Label;
                    List<FloatMenuOption> menu = list;
                    Func<Rect, bool> extraPartOnGUI = (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, current);
                    menu.Add(new FloatMenuOption(text, delegate { TryDrop(current); }, MenuOptionPriority.Default, null, null, 29f, extraPartOnGUI, null));
                }
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            ModBaseHumanResources.unlocked.libraryFreeSpace += dynamicCapacity - innerContainer.Count;
            this.TryGetComp<CompStorageGraphic>().UpdateGraphics();
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public virtual void CheckBookIn(Thing book)
        {
            var tech = book.TryGetTech();
            if (tech != null)
            {
                if (!tech.IsFinished) tech.CarefullyFinishProject(this);
            }
            if (!borrowed.Contains(book)) ModBaseHumanResources.unlocked.libraryFreeSpace--;
            else borrowed.Remove(book);
            CompStorageGraphic.UpdateGraphics();
        }

        public virtual void CheckBookOut(Thing book, bool misplaced = false)
        {
            // This was fun!
            // 0. normal use                            -> eject,       release     -> not leased,  not misplaced,  ?,        update.
            // 1. book taken, ongoing scan              -> don't eject, keep        -> leased,      not misplaced,  offline,  update.
            // 2. failed scan finish, book missing      -> eject,       release     -> leased,      misplaced,      offline,  no update.
            // 3. failed scan finish, book returned     -> eject,       keep        -> not leased,  misplaced,      offline,  no update.
            // 4. sucessful scan finish, book missing   -> don't eject, release     -> leased,      misplaced,      online,   no update. 
            // 5. sucessful scan finish, book returned  -> don't eject, keep        -> not leased,  misplaced,      online,   no update.

            var tech = book.TryGetTech();
            bool leased = borrowed.Contains(book);
            bool online = ModBaseHumanResources.unlocked.networkDatabase.Contains(tech);
            bool release = leased == misplaced;
            bool eject = misplaced != online;
            if (release)
            {
                ModBaseHumanResources.unlocked.libraryFreeSpace++;
                borrowed.Remove(book);
            }
            if (eject)
            {
                tech.Ejected(this);
            }
            if (!misplaced) CompStorageGraphic.UpdateGraphics();
        }

        public virtual void SignOff(Thing book)
        {

        }
    }
}
