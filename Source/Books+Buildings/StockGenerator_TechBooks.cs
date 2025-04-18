﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HumanResources
{
    public class StockGenerator_TechBooks : StockGenerator_MiscItems
    {
#pragma warning disable 0649
        public List<ThingCategoryDef> excludedCategories;
#pragma warning restore
        public TechLevel minTechLevelGenerate = TechLevel.Neolithic;

        //Restrics stock tech Level to the trader faction's
        public override IEnumerable<Thing> GenerateThings(int forTile, Faction faction = null)
        {
            if (faction?.def.techLevel != null && faction.def.techLevel < maxTechLevelGenerate) maxTechLevelGenerate = faction.def.techLevel;
            return base.GenerateThings(forTile, faction);
        }

        public override Thing MakeThing(ThingDef def, Faction faction = null)
        {
            if (!def.tradeability.TraderCanSell())
            {
                Log.Error("Tried to make non-trader-sellable thing for trader stock: " + def);
                return null;
            }
            ThingDef stuff = null;
            if (def.MadeFromStuff)
            {
                if (!GenStuff.AllowedStuffsFor(def, TechLevel.Undefined).Where(x => AppropriateTechLevel(x)).TryRandomElementByWeight((ThingDef x) => x.stuffProps.commonality, out stuff))
                {
                    stuff = GenStuff.RandomStuffByCommonalityFor(def, TechLevel.Undefined);
                }
            }
            Thing thing = ThingMaker.MakeThing(def, stuff);
            thing.stackCount = 1;
            return thing;
        }

        private bool AppropriateTechLevel(ThingDef x)
        {
            return x.techLevel >= minTechLevelGenerate && x.techLevel <= maxTechLevelGenerate;
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            return thingDef.tradeability != Tradeability.None && thingDef.techLevel <= maxTechLevelBuy && thingDef == TechDefOf.TechBook;
        }

        public override float SelectionWeight(ThingDef thingDef)
        {
            return SelectionWeightMarketValueCurve.Evaluate(thingDef.BaseMarketValue);
        }

        private static readonly SimpleCurve SelectionWeightMarketValueCurve = new SimpleCurve
        {
            {
                new CurvePoint(0f, 1f),
                true
            },
            {
                new CurvePoint(500f, 1f),
                true
            },
            {
                new CurvePoint(1500f, 0.2f),
                true
            },
            {
                new CurvePoint(5000f, 0.1f),
                true
            }
        };
    }
}
