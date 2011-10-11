using System.Collections.Generic;
namespace PSFilterLoad.PSApi
{

#if PSSDK4
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
                for (int f = 0; f < item.parms.Length; f++)
                {
                    AETEParm parm = item.parms[f];
                    if (!flagList.ContainsKey(parm.key))
                    {
                        flagList.Add(parm.key, parm.flags);
                    }
                }
            }
        }
    } 
#endif
    

}
