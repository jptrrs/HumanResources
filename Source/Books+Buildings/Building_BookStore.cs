using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace HumanResources
{
    using static ModBaseHumanResources;

    //Borrowed from Jecrell's RimWriter
    public class Building_BookStore : Building, IStoreSettingsParent, IHaulDestination, IThingHolder
    {
        public List<Thing> borrowed = new List<Thing>();
        public ThingOwner innerContainer;
        protected static int dynamicCapacityInt;
        protected CompStorageGraphic compStorageGraphic = null;
        protected StorageSettings storageSettings;
        public Building_BookStore()
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
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

        public virtual int dynamicCapacity
        {
            get
            {
                if (dynamicCapacityInt == 0)
                {
                    dynamicCapacityInt = Math.Max(TechTracker.totalBooks / 20, CompStorageGraphic.Props.countFullCapacity);
                }
                return dynamicCapacityInt;
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
        public virtual bool Accepts(Thing thing)
        {
            if (thing.IsValidBook())
            {
                bool allowed = storageSettings.AllowedToAccept(thing.Stuff);
                bool fits = innerContainer.Count < dynamicCapacity;
                bool duplicate = thing.TryGetTech().IsPhysicallyArchived();
                return allowed && fits && !duplicate;
            }
            return false;
        }

        public virtual void CheckBookIn(Thing book)
        {
            //Log.Message($"checking book in, borrowed={borrowed.Contains(book)}");
            var tech = book.TryGetTech();
            if (tech != null) tech.Unlock(this, true);
            if (!borrowed.Contains(book)) unlocked.libraryFreeSpace--;
            else borrowed.Remove(book);
            CompStorageGraphic.UpdateGraphics();
        }

        public virtual void CheckBookOut(Thing book, bool misplaced = false)
        {
            // This was fun!
            // 0. normal use                            -> eject,   release -> not leased,  not misplaced,  update.
            // 1. book taken, ongoing scan              -> hold,    keep    -> leased,      not misplaced,  update.
            // 2. failed scan finish, book missing      -> eject,   release -> leased,      misplaced,      no update.
            // 3. failed scan finish, book returned     -> eject,   keep    -> not leased,  misplaced,      no update.
            // 4. sucessful scan finish, book missing   -> eject,   release -> leased,      misplaced,      no update. 
            // 5. sucessful scan finish, book returned  -> eject,   keep    -> not leased,  misplaced,      no update.

            var tech = book.TryGetTech();
            bool leased = borrowed.Contains(book);
            bool release = leased == misplaced;
            bool hold = leased && !misplaced;
            //Log.Message($"checking book out: {(leased ? "leased," : "")} {(misplaced? "misplaced,":"")} => {(release? "release," : "no release")} {(hold? "hold":"eject")}");
            if (release)
            {
                unlocked.libraryFreeSpace++;
                borrowed.Remove(book);
            }
            if (!hold) tech.Ejected(this, true);
            if (!misplaced) CompStorageGraphic.UpdateGraphics();
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

        public override void DeSpawn(DestroyMode mode)
        {
            if (innerContainer.Count > 0)
            {
                innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near, delegate (Thing t, int i) { t.TryGetTech().Ejected(this, true); });
            }
            unlocked.libraryFreeSpace -= dynamicCapacity;
            base.DeSpawn(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", new object[] { this });
            Scribe_Deep.Look(ref storageSettings, "storageSettings", new object[] { this });
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
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

        public override string GetInspectString()
        {
            StringBuilder s = new StringBuilder();
            string baseStr = base.GetInspectString();
            if (baseStr != "") s.AppendLine(baseStr);
            if (innerContainer.Count == 0) s.AppendLine("BookStoreEmpty".Translate());
            else s.AppendLine("BookStoreCapacity".Translate(innerContainer.Count, dynamicCapacity.ToString()));
            if (Prefs.DevMode) s.AppendLine("Free space remaining in library: " + unlocked.libraryFreeSpace);
            return s.ToString().TrimEndNewlines();
        }

        public StorageSettings GetParentStoreSettings()
        {
            return def.building.fixedStorageSettings;
        }

        public StorageSettings GetStoreSettings()
        {
            return storageSettings;
        }

        public override void PostMake()
        {
            base.PostMake();
            storageSettings = new StorageSettings(this);
            if (def.building.defaultStorageSettings != null)
            {
                storageSettings.CopyFrom(def.building.defaultStorageSettings);
            }
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            unlocked.libraryFreeSpace += dynamicCapacity - innerContainer.Count;
            foreach (Thing book in innerContainer)
            {
                book.TryGetTech()?.Unlock(this, true);
            }
            this.TryGetComp<CompStorageGraphic>().UpdateGraphics();
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public bool TryDrop(Thing item, bool forbid = true)
        {
            if (innerContainer.Contains(item))
            {
                Thing outThing;
                innerContainer.TryDrop(item, ThingPlaceMode.Near, out outThing);
                ResearchProjectDef tech = outThing.TryGetTech();
                if (forbid) outThing.SetForbidden(true);
                CompStorageGraphic.UpdateGraphics();
                return true;
            }
            return false;
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
    }
}
