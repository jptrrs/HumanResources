using System.Collections.Generic;
using System.Xml;
using Verse;

namespace JPTools
{
    //Thanks to Garthor & dninemfive! From the Rimworld Discord server.
    public class PatchOperationFindModById : PatchOperation
    {
#pragma warning disable CS0649
        private List<string> mods;
        private PatchOperation match, nomatch;
#pragma warning restore CS0649

        protected override bool ApplyWorker(XmlDocument xml)
        {
            bool flag = false;
            for (int i = 0; i < mods.Count; i++)
            {
                if (ModLister.GetActiveModWithIdentifier(mods[i]) != null)
                {
                    flag = true;
                    break;
                }
            }
            if (flag)
            {
                if (match != null)
                {
                    return match.Apply(xml);
                }
            }
            else if (nomatch != null)
            {
                return nomatch.Apply(xml);
            }
            return true;
        }
    }
}
