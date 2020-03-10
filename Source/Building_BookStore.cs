using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace HumanResources
{
    //Borrowed from Jecrell's RimWriter
    public class Building_BookStore : Building, IThingHolder, IStoreSettingsParent
    {
        protected ThingOwner innerContainer;
        protected StorageSettings storageSettings;
        private CompStorageGraphic compStorageGraphic = null;
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
        
        //public bool TryAccept(Thing thing)
        //{
        //    return true;
        //}

        public bool Accepts(Thing thing)
        {
            bool allowed = storageSettings.AllowedToAccept(thing.Stuff);
            bool fits = innerContainer.Count < CompStorageGraphic.Props.countFullCapacity;
            bool duplicate = ModBaseHumanResources.unlocked.techByStuff[thing.Stuff].IsFinished;//innerContainer.Any(x => x.Stuff == thing.Stuff);
            return allowed && fits && !duplicate;
        }

        public override void PostMake()
        {
            base.PostMake();
            this.storageSettings = new StorageSettings(this);
            if (this.def.building.defaultStorageSettings != null)
            {
                this.storageSettings.CopyFrom(this.def.building.defaultStorageSettings);
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
            if (innerContainer.Count > 0)
            {
                innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near, delegate (Thing t, int i) { ModBaseHumanResources.unlocked.techByStuff[t.Stuff].EjectTech(this); });
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
            //droppedThing = outThing as ThingBook;
            droppedThing = outThing as ThingWithComps;
            return true;
        }
        else
        {
            Log.Warning("Building_InternalStorage : TryDropRandom - failed to get a book.");
        }
            return false;
        }

        public bool TryDrop(Thing item, bool forbid = true)
        {
            if (innerContainer.Contains(item))
            {
                Thing outThing;
                innerContainer.TryDrop(item, ThingPlaceMode.Near, out outThing);
                //ResearchProjectDef tech = ModBaseHumanResources.unlocked.stuffByTech.FirstOrDefault(x => x.Value == outThing.Stuff).Key;
                ResearchProjectDef tech = ModBaseHumanResources.unlocked.techByStuff[outThing.Stuff];
                tech.EjectTech(this);
                if (forbid) outThing.SetForbidden(true);
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
            if (baseStr != "")
                s.AppendLine(baseStr);
            if (innerContainer.Count == 0) s.AppendLine("BookStoreEmpty".Translate());
            else s.AppendLine("BookStoreCapacity".Translate(innerContainer.Count, CompStorageGraphic.Props.countFullCapacity.ToString()));
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
                        ProcessInput();
                    }
                };
            }
        }

        public void ProcessInput()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            Map map = Map;
            if (innerContainer.Count != 0)
            {
                //foreach (ThingBook current in innerContainer)
                foreach (Thing current in innerContainer)
                {
                    string text = current.Label;
                    //if (current.TryGetComp<CompArt>() is CompArt compArt)
                    //    text = TranslatorFormattedStringExtensions.Translate("RimWriter_BookTitle", compArt.Title, compArt.AuthorName);
                    List<FloatMenuOption> menu = list;
                    Func<Rect, bool> extraPartOnGUI = (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, current);
                    menu.Add(new FloatMenuOption(text, delegate { TryDrop(current); }, MenuOptionPriority.Default, null, null, 29f, extraPartOnGUI, null));
                }
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

    }
}
