using RimWorld;
using Verse;
using Verse.AI;

namespace HumanResources
{
    public class JobDriver_Knowledge : JobDriver_DoBill
    {
        protected ResearchProjectDef project;

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

        public override void ExposeData()
        {
            Scribe_Defs.Look<ResearchProjectDef>(ref project, "project");
            base.ExposeData();
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
