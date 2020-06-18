using RimWorld;
using Verse;

namespace HumanResources
{
	public class StockGenerator_TechBooks : StockGenerator_MiscItems
	{
		public override bool HandlesThingDef(ThingDef td)
		{
			return base.HandlesThingDef(td) && td.defName == "TechBook";
		}

		protected override float SelectionWeight(ThingDef thingDef)
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
