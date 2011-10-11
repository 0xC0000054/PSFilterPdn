using System.Collections.Generic;
namespace PSFilterLoad.PSApi
{

#if PSSDK4
    internal struct AETEParm
    {
        public string name;
        public uint key;
        public uint type;
        public string desc;
        public short flags;
    }

    internal struct AETEEnums
    {
        public uint type;
        public short count;
        public AETEEnum[] enums;
    }

    internal struct AETEEnum
    {
        public string name;
        public uint type;
        public string desc;
    }

    internal struct AETEEvent
    {
        public string vendor;
        public string desc;
        public int evntClass;
        public int type;
        public uint replyType;
        public uint parmType;
        public short flags;
        public AETEParm[] parms;
        public AETEEnums[] enums;
    }

    internal sealed class PluginAETE
    {
        public short version;
        public short lang;
        public string suiteID;
        public short suiteLevel;
        public short suiteVersion;

        public AETEEvent[] events;
    }

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
