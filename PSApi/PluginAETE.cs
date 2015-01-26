/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterLoad.PSApi
{
    internal struct AETEParameter
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
        public short major;
        public short minor;
        public short suiteLevel;
        public short suiteVersion;

        public AETEEvent scriptEvent;
    } 
}
