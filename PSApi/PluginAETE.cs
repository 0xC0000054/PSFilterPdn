/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
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
    }

    internal sealed class AETEEnums
    {
        public uint type;
        public short count;
        public AETEEnum[] enums;
    }

    internal sealed class AETEEnum
    {
        public string name;
        public uint type;
        public string desc;
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
