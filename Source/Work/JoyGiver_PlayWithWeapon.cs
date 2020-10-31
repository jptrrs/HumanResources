using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace HumanResources
{
    class JoyGiver_PlayWithWeapon : JoyGiver_InteractBuildingInteractionCell
    {
        public override bool CanBeGivenTo(Pawn pawn)
        {
            string equipment = pawn.equipment != null ? "ok" : "bad";
            Log.Warning("DEBUG equipment is " + equipment+ ", def is "+def.label);
            if (pawn.equipment != null && ((def == TechDefOf.PlayShooting && pawn.equipment.Primary.def.IsRangedWeapon) || def == TechDefOf.PlayMartialArts))
            {
                return base.CanBeGivenTo(pawn);
            }
            else return false;
        }
    }
}
