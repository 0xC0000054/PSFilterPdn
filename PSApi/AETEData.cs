using System.Collections.Generic;
namespace PSFilterLoad.PSApi
{

    [System.Serializable()]
    public sealed class AETEData
    {
        private Dictionary<uint, short> flagList;

        public Dictionary<uint, short> FlagList
        {
            get
            {
                return flagList;
            }
        }

        internal AETEData(PluginAETE enumAETE)
        {
            this.flagList = new Dictionary<uint, short>();
            foreach (var item in enumAETE.events)
            {
                foreach (AETEParm parm in item.parms)
                {
                    if (!flagList.ContainsKey(parm.key))
                    {
                        flagList.Add(parm.key, parm.flags);
                    }
                }
            }
        }
    } 
    

}
