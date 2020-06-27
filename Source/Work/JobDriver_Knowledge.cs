using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace HumanResources
{
    public class JobDriver_Knowledge : JobDriver_DoBill
    {
		protected Thing desk
		{
			get
			{
				return job.GetTarget(TargetIndex.A).Thing;
			}
		}

		protected CompKnowledge techComp
		{
			get
			{
				return pawn.TryGetComp<CompKnowledge>();
			}
		}

		protected ResearchProjectDef project;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Pawn pawn = this.pawn;
			LocalTargetInfo target = desk;
			Job job = this.job;
			bool result;
			if (pawn.Reserve(target, job, 1, -1, null, errorOnFailed))
			{
				result = pawn.Reserve(target, job, 1, -1, null, errorOnFailed);
			}
			else result = false;
			return result;
		}

		public void Notify_IterationCompleted(Pawn billDoer, Bill_Production bill)
		{
			if (bill.repeatMode == BillRepeatModeDefOf.RepeatCount)
			{
				if (bill.repeatCount > 0)
				{
					bill.repeatCount--;
				}
				if (bill.repeatCount == 0)
				{
					Messages.Message("MessageBillComplete".Translate(bill.LabelCap), desk, MessageTypeDefOf.TaskCompletion, true);
				}
			}
		}
	}
}
