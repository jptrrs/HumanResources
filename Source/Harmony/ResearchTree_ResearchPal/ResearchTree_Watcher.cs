using System;
using Verse;

namespace HumanResources
{
    public static class ResearchTree_Watcher
    {
        //public delegate void Notify();  // delegate: "template" for the handler to be defined on the subscriber class, replaced by .Net's EventHandler

        public static event EventHandler<ResearchProjectDef> TechHovered; // event
        public static event EventHandler<ResearchProjectDef> HoveredOut;

        public static void OnTechHovered(object sender, ResearchProjectDef tech) //if event is not null then call delegate
        {
            TechHovered?.Invoke(sender, tech);
        }

        public static void OnHoverOut(object sender, ResearchProjectDef tech)
        {
            HoveredOut?.Invoke(sender, tech);
        }


    }
}
