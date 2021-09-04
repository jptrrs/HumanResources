using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HumanResources
{
	//Changed in RW 1.3
	public class StockGenerator_TechBooks : StockGenerator_MiscItems
	{
#pragma warning disable 0649
		private List<ThingCategoryDef> excludedCategories;
#pragma warning restore

		private TechLevel defaultLevel = TechLevel.Undefined;

		public override IEnumerable<Thing> GenerateThings(int forTile, Faction faction = null)
		{
			if (faction?.def.techLevel != null) defaultLevel = faction.def.techLevel;
			return base.GenerateThings(forTile, faction);
		}

        public override Thing MakeThing(ThingDef def)
		{
			if (!def.tradeability.TraderCanSell())
			{
				Log.Error("Tried to make non-trader-sellable thing for trader stock: " + def, false);
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
			if (!excludedCategories.NullOrEmpty()) return !excludedCategories.Intersect(x.thingCategories).Any();
			else if (defaultLevel != 0) return x.techLevel == defaultLevel;
			else return true;
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
