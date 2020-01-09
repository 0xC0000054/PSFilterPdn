/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterLoad.PSApi
{
    internal sealed class AETEParameter
    {
        public string name;
        public uint key;
        public uint type;
        public string desc;
        public short flags;

        public AETEParameter(string name, uint key, uint type, string description, short flags)
        {
            this.name = name;
            this.key = key;
            this.type = type;
            desc = description;
            this.flags = flags;
        }
    }

    internal sealed class AETEEnums
    {
        public uint type;
        public short count;
        public AETEEnum[] enums;

        public AETEEnums(uint type, short count, AETEEnum[] enums)
        {
            this.type = type;
            this.count = count;
            this.enums = enums;
        }
    }

    internal sealed class AETEEnum
    {
        public string name;
        public uint type;
        public string desc;

        public AETEEnum(string name, uint type, string description)
        {
            this.name = name;
            this.type = type;
            desc = description;
        }
    }

    internal sealed class AETEEvent
    {
        public string vendor;
        public string desc;
        public int eventClass;
        public int type;
        public uint replyType;
        public uint paramType;
        public short flags;
        public AETEParameter[] parameters;
        public AETEEnums[] enums;
    }

    internal sealed class PluginAETE
    {
        public readonly short major;
        public readonly short minor;
        public readonly short suiteLevel;
        public readonly short suiteVersion;
        public readonly AETEEvent scriptEvent;

        public PluginAETE(short major, short minor, short suiteLevel, short suiteVersion, AETEEvent scriptEvent)
        {
            this.major = major;
            this.minor = minor;
            this.suiteLevel = suiteLevel;
            this.suiteVersion = suiteVersion;
            this.scriptEvent = scriptEvent;
        }
    }
}
