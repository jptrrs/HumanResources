using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HumanResources
{
    //Borrowed from Jecrell's RimWriter
    [StaticConstructorOnStartup]
    internal class ITabButton
    {
        public static readonly Texture2D Drop = ContentFinder<Texture2D>.Get("UI/Buttons/Drop", true);
    }

    public class ITab_Inventory : ITab
    {
        private Vector2 scrollPosition = Vector2.zero;

        private float scrollViewHeight;

        private static readonly Color 
            ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f),
            HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private const float 
            ThingIconSize = 28f,
            ThingRowHeight = 28f,
            ThingLeftX = 36f,
            StandardLineHeight = 22f,
            TopPadding = 20f;

        private static List<Thing> workingInvList = new List<Thing>();

        public override bool IsVisible
        {
            get
            {
                return selStorage.TryGetInnerInteractableThingOwner().Count > 0;
            }
        }

        private Building_BookStore selStorage
        {
            get
            {
                if (SelThing != null && SelThing is Building_BookStore bld)
                {
                    return bld;
                }
                return null;
            }
        }

        private bool CanControl
        {
            get
            {
                return selStorage.Spawned && selStorage.Faction == Faction.OfPlayer;
            }
        }

        public ITab_Inventory()
        {
            size = new Vector2(460f, 450f);
            labelKey = "TabCatalogue";
        }

        public override void FillTab() // FillTab access became Protected in 1.4
        {
            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 20f, size.x, size.y - 20f);
            Rect rect2 = rect.ContractedBy(10f);
            Rect position = new Rect(rect2.x, rect2.y, rect2.width, rect2.height);
            GUI.BeginGroup(position);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 0f, position.width, position.height);
            Rect viewRect = new Rect(0f, 0f, position.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);
            float num = 0f;
            if (selStorage.TryGetInnerInteractableThingOwner() is ThingOwner t)
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Inventory".Translate());
                workingInvList.Clear();
                workingInvList.AddRange(t.OrderBy(x => x.Stuff.techLevel).ThenBy(x => x.Stuff.label));
                for (int i = 0; i < workingInvList.Count; i++)
                {
                    DrawThingRow(ref num, viewRect.width, workingInvList[i], true);
                }
            }
            if (Event.current.type == EventType.Layout)
            {
                scrollViewHeight = num + 30f;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawThingRow(ref float y, float width, Thing thing, bool canRetrieve = false)
        {
            Rect rect = new Rect(0f, y, width, 28f);
            Widgets.InfoCardButton(rect.width - 24f, y, thing);
            rect.width -= 24f;
            if (CanControl)
            {
                Rect rect2 = new Rect(rect.width - 24f, y, 24f, 24f);
                TooltipHandler.TipRegion(rect2, "DropThing".Translate());
                if (canRetrieve && Widgets.ButtonImage(rect2, ITabButton.Drop))
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    InterfaceDrop(thing);
                }
                rect.width -= 24f;
            }
            Rect rect4 = rect;
            rect4.xMin = rect4.xMax - 60f;

            CaravanThingsTabUtility.DrawMass(thing, rect4);
            rect.width -= 60f;
            if (Mouse.IsOver(rect))
            {
                GUI.color = HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), thing, 1f);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            Rect rect5 = new Rect(36f, y, rect.width - 36f, rect.height);
            string text = thing.LabelCap;
            Text.WordWrap = false;
            Widgets.Label(rect5, text.Truncate(rect5.width, null));
            Text.WordWrap = true;
            string text2 = thing.LabelCap;
            if (thing.def.useHitPoints)
            {
                string text3 = text2;
                text2 = string.Concat(new object[]
                {
                    text3,
                    "\n",
                    thing.HitPoints,
                    " / ",
                    thing.MaxHitPoints
                });
            }
            TooltipHandler.TipRegion(rect, text2);
            y += 28f;
        }

        private void InterfaceDrop(Thing t)
        {
            ThingWithComps thingWithComps = t as ThingWithComps;
            selStorage.TryDrop(t);
        }

    }
}
