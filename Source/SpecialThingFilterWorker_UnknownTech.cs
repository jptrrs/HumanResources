using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HumanResources
{
	public class SpecialThingFilterWorker_UnknownTech : SpecialThingFilterWorker
	{
		public override bool Matches(Thing t)
		{
			//Log.Message("checking Match for " + t);
			return this.CanEverMatch(t.def);
		}

		public override bool CanEverMatch(ThingDef def)
		{
			//string test = ModBaseHumanResources.unlocked.techByStuff[def].IsFinished ? "DONE" : "undone";
			//Log.Message("checking CanEverMatch for " + def + " and its " + test);
			return !ModBaseHumanResources.unlocked.techByStuff[def].IsFinished;
		}

		public override bool AlwaysMatches(ThingDef def)
		{
			//Log.Message("checking AlwaysMatches for " + def);
			return !ModBaseHumanResources.unlocked.techByStuff[def].IsFinished;
		}
	}
}
