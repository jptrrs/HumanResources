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
        protected override bool CanInteractWith(Pawn pawn, Thing t, bool inBed)
        {
            bool flag = false;
            if (pawn.equipment != null && pawn.equipment.Primary != null)
            {
                if (def == TechDefOf.Play_Shooting && pawn.equipment.Primary.def.IsRangedWeapon) flag = true;
                else if (def == TechDefOf.Play_MartialArts && pawn.equipment.Primary.def.IsMeleeWeapon) flag = true;
            }
            if (!flag && def == TechDefOf.Play_MartialArts) flag = true;
            if (flag) return base.CanInteractWith(pawn, t, inBed);
            return false;
        }

        public override bool CanBeGivenTo(Pawn pawn)
        {
            if (ModBaseHumanResources.EnableJoyGiver && pawn.equipment != null && ((def == TechDefOf.Play_Shooting && pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsRangedWeapon) || def == TechDefOf.Play_MartialArts))
            {
                return base.CanBeGivenTo(pawn);
            }
            else return false;
        }
    }
}
