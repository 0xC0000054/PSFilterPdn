using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterLoad.PSApi
{
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
        public short classCount;
        public AETEEnums[] enums;
    }

    internal sealed class PluginAETE
    {
        public short version;
        public string suiteID;
        public short suiteLevel;
        public short suiteVersion;

        public AETEEvent[] events;
    } 
}
